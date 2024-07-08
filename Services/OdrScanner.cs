using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CriticalCommonLib.Models;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace CriticalCommonLib.Services
{
    using System.Text.RegularExpressions;
    using Dalamud.Plugin.Services;

    public class OdrScanner : IDisposable
    {
        const byte XOR8 = 0x73;
        const ushort XOR16 = 0x7373;
        const uint XOR32 = 0x73737373;
        
        ICharacterMonitor _characterMonitor;
        private readonly IClientState clientState;
        private readonly IPluginLog pluginLog;
        private SemaphoreSlim? _semaphoreSlim;
        private FileSystemWatcher? _odrWatcher;
        private string? _odrPath;
        private string? _odrDirectory;
        private bool _canRun;
        private InventorySortOrder? _sortOrder;

        public InventorySortOrder? SortOrder
        {
            get
            {
                return _sortOrder;
            }
        }
        
        

        public delegate void SortOrderChangedDelegate(InventorySortOrder sortOrder);

        public event SortOrderChangedDelegate? OnSortOrderChanged; 

        public OdrScanner(ICharacterMonitor monitor, IClientState clientState, IPluginLog pluginLog)
        {
            Service.Log.Verbose("Starting service {type} ({this})", GetType().Name, this);
            _characterMonitor = monitor;
            this.clientState = clientState;
            this.pluginLog = pluginLog;
            clientState.Login += ClientLogin;
            clientState.Logout += ClientLogout;

            _characterMonitor.OnCharacterUpdated += CharacterMonitorOnOnCharacterUpdated;
            if (Service.ClientState.IsLoggedIn)
            {
                NewClient();
            }
        }

        private void ClientLogin()
        {
            NewClient();
        }


        private void CharacterMonitorOnOnCharacterUpdated(Character? character)
        {
            if (Service.ClientState.IsLoggedIn && character != null)
            {
                NewClient();
            }
            else
            {
                ClientLogout();
            }
        }

        private void ClientLogout()
        {
            _canRun = false;
            _sortOrder = null;
            if (_odrWatcher != null)
            {
                _odrWatcher.EnableRaisingEvents = false;
                _odrWatcher.Changed -= OdrWatcherOnChanged;
                _odrWatcher.Dispose();
                _odrPath = null;
                _odrDirectory = null;
                _odrWatcher = null;
            }
            if (_semaphoreSlim != null)
            {
                _semaphoreSlim.Dispose();
                _semaphoreSlim = null;
            }
        }

        private unsafe void NewClient(int counter = 0)
        {
            if (Service.ClientState.LocalContentId == 0)
            {
                Thread.Sleep(50);
                NewClient(++counter);
            }
            if(counter > 100)
            {
                Service.Log.Verbose(DateTimeOffset.Now.ToUnixTimeMilliseconds() + " Failed to retrieve new client id");
                return;
            }
            _canRun = true;
            var framework = Framework.Instance();
            if (framework == null)
            {
                Service.Log.Verbose("Failed to find framework.");
                return;
            }

            var frameWorkPath = framework->UserPathString;
            var userPath = $"FFXIV_CHR{Service.ClientState.LocalContentId:X16}";
            pluginLog.Verbose(frameWorkPath + "/" + userPath);
            pluginLog.Verbose(frameWorkPath);
            pluginLog.Verbose(userPath);
            _odrDirectory = Path.Combine(frameWorkPath, userPath);
            pluginLog.Verbose(_odrDirectory);
            _odrPath = Path.Combine(_odrDirectory, "ITEMODR.DAT");
            if (_semaphoreSlim != null)
            {
                _semaphoreSlim.Dispose();
            }
            _semaphoreSlim = new SemaphoreSlim(1);
            if (_odrWatcher != null)
            {
                _odrWatcher.EnableRaisingEvents = false;
                _odrWatcher.Changed -= OdrWatcherOnChanged;
                _odrWatcher.Dispose();
            }
            _odrWatcher = new FileSystemWatcher(_odrDirectory);
            _odrWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _odrWatcher.Filter = "ITEMODR.DAT";
            _odrWatcher.Changed += OdrWatcherOnChanged;
            _odrWatcher.EnableRaisingEvents = true;
            RequestParseOdr();
        }

        private void OdrWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            RequestParseOdr();
        }

        public void RequestParseOdr()
        {
            if (_semaphoreSlim == null || _disposed)
            {
                return;
            }
            _semaphoreSlim.Wait();
            ParseOdr();
            _semaphoreSlim.Release();
        }

        private void ParseOdr(int counter = 0)
        {
            if (!_canRun)
            {
                return;
            }
            if(counter > 100)
            {
                Service.Log.Debug(DateTimeOffset.Now.ToUnixTimeMilliseconds() + " Itemodr parsing failed hard");
                return;
            }
            try
            {
                var sortOrder = ParseItemOrder();
                if (sortOrder != null)
                {
                    _sortOrder = sortOrder;
                    Service.Framework.RunOnTick(() => { OnSortOrderChanged?.Invoke(sortOrder); });
                    Service.Log.Debug(DateTimeOffset.Now.ToUnixTimeMilliseconds() + " Itemodr reparsed");
                }
                else
                {
                    Thread.Sleep(50);
                    ParseOdr(++counter);
                }
            }
            catch (Exception e)
            {
                Service.Log.Debug(DateTimeOffset.Now.ToUnixTimeMilliseconds() + " Failed to reparse iremodr because " + e.Message);
                Thread.Sleep(50);
                ParseOdr(++counter);
            }            
        }



        void UnexpectedSizeError(int expected, int real)
        {
            throw new Exception("Incorrect Size - Expected " + expected + " but got " + real);
        }

        void UnexpectedIdentifierError(int expected, int real)
        {
            throw new Exception("Unexpected Identifier - Expected " + expected + " but got " + real);
        }

        (int slotIndex, int containerIntex) readSlot(FileStream reader)
        {
            var s = ReadUInt8(reader) ^ XOR8;
            if (s != 4) UnexpectedSizeError(4, s);
            return ( ReadUInt16(reader) ^ XOR16, ReadUInt16(reader) ^ XOR16 );
        }

        List<(int slotIndex, int containerIndex)> readInventory(FileStream reader)
        {
            var s = ReadUInt8(reader) ^ XOR8;
            if (s != 4) UnexpectedSizeError(4, s);
            var slotCount = ReadUInt32(reader) ^ XOR32;

            var inventory = new List<(int slotIndex, int containerIndex)>();
            for (var i = 0; i < slotCount; i++)
            {
                var x = ReadUInt8(reader) ^ XOR8;
                if (x != 0x69) UnexpectedIdentifierError(0x69, x);
                var slot = readSlot(reader);
                inventory.Add(slot);
            }

            return inventory;
        }

        Dictionary<ulong,RetainerSortOrder> readRetainer(FileStream reader)
        {
            var s = ReadUInt8(reader) ^ XOR8;
            if (s != 4) UnexpectedSizeError(4, s);
            var retainerCount = ReadUInt32(reader) ^ XOR32;
            Dictionary<ulong,RetainerSortOrder> retainers = new Dictionary<ulong,RetainerSortOrder>();
            
            for (var i = 0; i < retainerCount; i++)
            {
                var x = ReadUInt8(reader) ^ XOR8;
                if (x != 0x52) UnexpectedIdentifierError(0x52, x);
                var retainer = this.readRetainerSlot(reader);
                retainers.Add(retainer.Id,retainer);
            }
            return retainers;
        }
        
        private RetainerSortOrder readRetainerSlot(FileStream reader) {
            var s = ReadUInt8(reader) ^ XOR8;
            if (s != 8) UnexpectedSizeError(8, s);

            //I know this is terrible but I'm doing it anyway
            var id = ReadUInt8(reader) ^ XOR8;
            var id2 = ReadUInt8(reader) ^ XOR8;
            var id3 = ReadUInt8(reader) ^ XOR8;
            var id4 = ReadUInt8(reader) ^ XOR8;
            var id5 = ReadUInt8(reader) ^ XOR8;
            var id6 = ReadUInt8(reader) ^ XOR8;
            var id7 = ReadUInt8(reader) ^ XOR8;
            var id8 = ReadUInt8(reader) ^ XOR8;
            var actualId = BitConverter.ToUInt64(new byte[]
            {
                (byte) id, (byte) id2, (byte) id3, (byte) id4, (byte) id5, (byte) id6, (byte) id7, (byte) id8
            }, 0);

            var x = ReadUInt8(reader) ^ XOR8;
            if (x != 0x6E) UnexpectedIdentifierError(0x6E, x);
            var inventory = this.readInventory(reader);
            var retainer = new RetainerSortOrder(actualId, inventory);
            return retainer;
        }

        string[] inventoryNames = {
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
            "SaddleBagPremium",
        };

        private InventorySortOrder? ParseItemOrder()
        {
            if (_odrPath == null || !File.Exists(_odrPath) || _disposed)
            {
                return null;
            }
            using (FileStream reader = File.OpenRead(_odrPath))
            {
                Advance(reader, 16);
                Dictionary<ulong, RetainerSortOrder> retainerInventories = new Dictionary<ulong, RetainerSortOrder>();
                Dictionary<string, List<(int slotIndex, int containerIndex)>> normalInventories = new Dictionary<string, List<(int slotIndex, int containerIndex)>>();                

                Advance(reader, 1); // Unknown Byte, Appears to be the main inventory size, but that is 
                var inventoryIndex = 0;
                try
                {
                    while (true)
                    {

                        var identifier = ReadUInt8(reader) ^ XOR8;
                        switch (identifier)
                        {
                            case 0x56:
                                {
                                    // Unknown
                                    Advance(reader, ReadUInt8(reader) ^ XOR8);
                                    break;
                                }
                            case 0x6E:
                                {
                                    // Start of an inventory
                                    var inventory = readInventory(reader);

                                    var i = inventoryIndex++;
                                    if (i >= 0 && inventoryNames.Length > i)
                                    {
                                        var inventoryName = inventoryNames[i];
                                        normalInventories.Add(inventoryName,inventory);
                                    }

                                    break;
                                }

                            case 0x4E:
                                {
                                    var retainers = readRetainer(reader);
                                    retainerInventories = retainers;
                                    break;
                                }

                            case 0x73:
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
                    Service.Log.Verbose(e.Message);
                }

                return new InventorySortOrder(retainerInventories, normalInventories);;
            }
        }

        byte ReadUInt8(FileStream reader)
        {
            return (byte)reader.ReadByte();
        }

        ushort ReadUInt16(FileStream reader) //why? because why not?
        {
            return BitConverter.ToUInt16(new byte[] { (byte)reader.ReadByte(), (byte)reader.ReadByte() }, 0);
        }

        uint ReadUInt32(FileStream reader)
        {
            return BitConverter.ToUInt32(new byte[] { 
                (byte)reader.ReadByte(), (byte)reader.ReadByte(), (byte)reader.ReadByte(), (byte)reader.ReadByte() }, 0);
        }

        void Advance(FileStream reader, int amount)
        {
            reader.Position += amount;
        }
        
        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void Dispose(bool disposing)
        {
            if(!_disposed && disposing)
            {
                clientState.Login -= ClientLogin;
                clientState.Logout -= ClientLogout;
                _characterMonitor.OnCharacterUpdated -= CharacterMonitorOnOnCharacterUpdated;
                _semaphoreSlim?.Dispose();
                if (_odrWatcher != null)
                {
                    _odrWatcher.EnableRaisingEvents = false;
                    _odrWatcher.Changed -= OdrWatcherOnChanged;
                }
                _odrWatcher?.Dispose();
            }
            _disposed = true;         
        }
        
        ~OdrScanner()
        {
#if DEBUG
            // In debug-builds, make sure that a warning is displayed when the Disposable object hasn't been
            // disposed by the programmer.

            if( _disposed == false )
            {
                Service.Log.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
            }
#endif
            Dispose (true);
        }

    }

}
