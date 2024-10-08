using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CriticalCommonLib.Models;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Misc.UserFileManager;
using Microsoft.Extensions.Hosting;

namespace CriticalCommonLib.Services;

public class OdrScanner : IHostedService, IOdrScanner
{
    private const byte Xor8 = 0x73;
    private const ushort Xor16 = 0x7373;
    private const uint Xor32 = 0x73737373;

    private readonly IFramework _framework;
    private readonly IPluginLog _pluginLog;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IClientState _clientState;
    private bool _initialBootCheck;
    private Hook<UserFileEvent.Delegates.WriteFile>? _writeFileHook;
    private Hook<UserFileEvent.Delegates.ReadFile>? _readFileHook;
    private readonly Dictionary<ulong, InventorySortOrder> _sortOrders;

    public delegate void SortOrderChangedDelegate(InventorySortOrder sortOrder);
    public event SortOrderChangedDelegate? OnSortOrderChanged;

    public OdrScanner(IFramework framework, IPluginLog pluginLog, IGameInteropProvider gameInteropProvider,
        IClientState clientState)
    {
        _framework = framework;
        _pluginLog = pluginLog;
        _gameInteropProvider = gameInteropProvider;
        _clientState = clientState;
        _sortOrders = new Dictionary<ulong, InventorySortOrder>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _pluginLog.Verbose("Starting the ODR scanner.");
        _framework.Update += FrameworkOnUpdate;
        return Task.CompletedTask;
    }

    public InventorySortOrder? GetSortOrder(ulong characterId)
    {
        return _sortOrders.GetValueOrDefault(characterId);
    }

    private unsafe void FrameworkOnUpdate(IFramework framework1)
    {
        var itemOrderModule = ItemOrderModule.Instance();
        if (itemOrderModule == null) return;

        if (_readFileHook == null)
        {
            _readFileHook = _gameInteropProvider.HookFromAddress<UserFileEvent.Delegates.ReadFile>(
                itemOrderModule->UserFileEvent.VirtualTable->ReadFile,
                ReadFile);
            _readFileHook.Enable();
        }

        if (_writeFileHook == null)
        {
            _writeFileHook = _gameInteropProvider.HookFromAddress<UserFileEvent.Delegates.WriteFile>(
                itemOrderModule->UserFileEvent.VirtualTable->WriteFile,
                WriteFile);
            _writeFileHook.Enable();
        }

        if (!_initialBootCheck && _clientState.IsLoggedIn)
        {
            _pluginLog.Verbose("Marking ODR as modified so we can parse it.");
            itemOrderModule->UserFileEvent.HasChanges = true;
            _initialBootCheck = true;
        }
    }

    private unsafe bool ReadFile(UserFileEvent* thisPtr, bool decrypt, byte* ptr, ushort version, uint length)
    {
        var result = _readFileHook!.Original(thisPtr, decrypt, ptr, version, length);
        if (result)
        {
            var buffer = new byte[length];
            Marshal.Copy((IntPtr)ptr, buffer, 0, (int)length);
            _framework.RunOnFrameworkThread(
                () =>
                {
                    try
                    {
                        var sortOrder = ParseItemOrder(buffer, true);
                        _sortOrders[ItemOrderModule.Instance()->CharacterContentId] = sortOrder;
                        OnSortOrderChanged?.Invoke(sortOrder);
                        _pluginLog.Verbose("Parsed the ODR from memory after a read.");
                    }
                    catch (Exception e)
                    {
                        _pluginLog.Error("Failed to parse odr from memory.", e);
                    }
                });
        }

        return result;
    }

    private unsafe uint WriteFile(UserFileEvent* thisPtr, byte* ptr, uint length)
    {
        var result = _writeFileHook!.Original(thisPtr, ptr, length);

        var buffer = new byte[length];
        Marshal.Copy((IntPtr)ptr, buffer, 0, (int)length);
        _framework.RunOnFrameworkThread(
            () =>
            {
                try
                {
                    var sortOrder = ParseItemOrder(buffer);
                    _sortOrders[ItemOrderModule.Instance()->CharacterContentId] = sortOrder;
                    OnSortOrderChanged?.Invoke(sortOrder);
                    _pluginLog.Verbose("Parsed the ODR from memory after a write.");
                }
                catch (Exception e)
                {
                    _pluginLog.Error("Failed to parse odr from memory.", e);
                }
            });
        return result;
    }

    private InventorySortOrder ParseItemOrder(byte[] buffer, bool read = false)
    {
        using (var reader = new MemoryStream(buffer))
        {
            var retainerInventories = new Dictionary<ulong, RetainerSortOrder>();
            var normalInventories =
                new Dictionary<string, List<(int SlotIndex, int ContainerIndex)>>();

            if (!read) Advance(reader, 1); // Unknown Byte, Appears to be the main inventory size, but that is

            var inventoryIndex = 0;
            try
            {
                while (true)
                {
                    var identifier = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
                    _pluginLog.Verbose(identifier.ToString());
                    switch (identifier)
                    {
                        case 0x56:
                        {
                            // Unknown
                            Advance(reader, read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8);
                            break;
                        }
                        case 110:
                        {
                            // Start of an inventory
                            var inventory = ReadInventory(reader, read);

                            var i = inventoryIndex++;
                            if (i >= 0 && _inventoryNames.Length > i)
                            {
                                var inventoryName = _inventoryNames[i];
                                normalInventories.Add(inventoryName, inventory);
                            }

                            break;
                        }

                        case 78:
                        {
                            var retainers = ReadRetainer(reader, read);
                            retainerInventories = retainers;
                            break;
                        }

                        case 115:
                        {
                            return new InventorySortOrder(retainerInventories, normalInventories);
                        }
                        default:
                        {
                            throw new Exception("Unexpected Identifier: " + identifier);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _pluginLog.Verbose(e.Message);
            }

            return new InventorySortOrder(retainerInventories, normalInventories);
        }
    }

    private void UnexpectedSizeError(int expected, int real)
    {
        throw new Exception("Incorrect Size - Expected " + expected + " but got " + real);
    }

    private void UnexpectedIdentifierError(int expected, int real)
    {
        throw new Exception("Unexpected Identifier - Expected " + expected + " but got " + real);
    }

    private (int slotIndex, int containerIntex) ReadSlot(MemoryStream reader, bool read)
    {
        var s = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
        if (s != 4) UnexpectedSizeError(4, s);
        return (read ? ReadUInt16(reader) : ReadUInt16(reader) ^ Xor16,
            read ? ReadUInt16(reader) : ReadUInt16(reader) ^ Xor16);
    }

    private List<(int slotIndex, int containerIndex)> ReadInventory(MemoryStream reader, bool read)
    {
        var s = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
        if (s != 4) UnexpectedSizeError(4, s);
        var slotCount = read ? ReadUInt32(reader) : ReadUInt32(reader) ^ Xor32;

        var inventory = new List<(int slotIndex, int containerIndex)>();
        for (var i = 0; i < slotCount; i++)
        {
            var x = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
            if (x != 0x69) UnexpectedIdentifierError(0x69, x);
            var slot = ReadSlot(reader, read);
            inventory.Add(slot);
        }

        return inventory;
    }

    private Dictionary<ulong, RetainerSortOrder> ReadRetainer(MemoryStream reader, bool read)
    {
        var s = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
        if (s != 4) UnexpectedSizeError(4, s);
        var retainerCount = read ? ReadUInt32(reader) : ReadUInt32(reader) ^ Xor32;
        var retainers = new Dictionary<ulong, RetainerSortOrder>();

        for (var i = 0; i < retainerCount; i++)
        {
            var x = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
            if (x != 0x52) UnexpectedIdentifierError(0x52, x);
            var retainer = ReadRetainerSlot(reader, read);
            retainers.Add(retainer.Id, retainer);
        }

        return retainers;
    }

    private RetainerSortOrder ReadRetainerSlot(MemoryStream reader, bool read)
    {
        var s = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
        if (s != 8) UnexpectedSizeError(8, s);

        //I know this is terrible but I'm doing it anyway
        var id = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
        var id2 = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
        var id3 = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
        var id4 = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
        var id5 = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
        var id6 = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
        var id7 = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
        var id8 = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
        var actualId = BitConverter.ToUInt64(new byte[]
        {
            (byte)id, (byte)id2, (byte)id3, (byte)id4, (byte)id5, (byte)id6, (byte)id7, (byte)id8
        }, 0);

        var x = read ? ReadUInt8(reader) : ReadUInt8(reader) ^ Xor8;
        if (x != 0x6E) UnexpectedIdentifierError(0x6E, x);
        var inventory = ReadInventory(reader, read);
        var retainer = new RetainerSortOrder(actualId, inventory);
        return retainer;
    }

    private string[] _inventoryNames =
    {
        "PlayerInventory",
        "ArmouryMainHand",
        "ArmouryHead",
        "ArmouryBody",
        "ArmouryHands",
        "ArmouryLegs",
        "ArmouryFeet",
        "ArmouryOffHand",
        "ArmouryEars",
        "ArmouryNeck",
        "ArmouryWrists",
        "ArmouryRings",
        "ArmourySoulCrystals",
        "SaddleBag",
        "SaddleBagPremium"
    };


    private byte ReadUInt8(MemoryStream reader)
    {
        return (byte)reader.ReadByte();
    }

    private ushort ReadUInt16(MemoryStream reader) //why? because why not?
    {
        return BitConverter.ToUInt16(new byte[] { (byte)reader.ReadByte(), (byte)reader.ReadByte() }, 0);
    }

    private uint ReadUInt32(MemoryStream reader)
    {
        return BitConverter.ToUInt32(new byte[]
        {
            (byte)reader.ReadByte(), (byte)reader.ReadByte(), (byte)reader.ReadByte(), (byte)reader.ReadByte()
        }, 0);
    }

    private void Advance(MemoryStream reader, int amount)
    {
        reader.Position += amount;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _writeFileHook?.Dispose();
        _readFileHook?.Dispose();
        _framework.Update -= FrameworkOnUpdate;
        return Task.CompletedTask;
    }
}