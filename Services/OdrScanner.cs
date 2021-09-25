using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CriticalCommonLib.Models;
using Dalamud.Game.ClientState;
using Dalamud.Logging;
using Dalamud.Plugin;
using InventoryTools;
using InventoryTools.Structs;

namespace CriticalCommonLib.Services
{
    
    public class OdrScanner : IDisposable
    {
        const byte XOR8 = 0x73;
        const ushort XOR16 = 0x7373;
        const uint XOR32 = 0x73737373;
        
        ClientState _clientState;
        CharacterMonitor _characterMonitor;
        private SemaphoreSlim _semaphoreSlim;
        private string _odrPath;
        private string _odrDirectory;
        private FileSystemWatcher _odrWatcher;
        private bool _canRun = false;
        private InventorySortOrder? _sortOrder;

        public delegate void SortOrderChangedDelegate(InventorySortOrder sortOrder);

        public event SortOrderChangedDelegate OnSortOrderChanged; 

        public OdrScanner(ClientState clientState, CharacterMonitor monitor)
        {
            this._clientState = clientState;
            this._clientState.Login += ClientStateOnOnLogin;
            this._clientState.Logout += ClientStateOnOnLogout;
            _characterMonitor = monitor;
            _characterMonitor.OnCharacterUpdated += CharacterMonitorOnOnCharacterUpdated;
            if (this._clientState.IsLoggedIn)
            {
                NewClient();
            }
        }

        private void CharacterMonitorOnOnCharacterUpdated(Character character)
        {
            if (this._clientState.IsLoggedIn)
            {
                NewClient();
            }
        }

        private void ClientStateOnOnLogout(object sender, EventArgs e)
        {
            _canRun = false;
            _sortOrder = null;
            if (_odrWatcher != null)
            {
                _odrWatcher.EnableRaisingEvents = false;
                _odrWatcher.Dispose();
            }
            if (_semaphoreSlim != null)
            {
                _semaphoreSlim.Dispose();
            }
        }
        
        //Make sure we 
        private void ClientStateOnOnLogin(object sender, EventArgs e)
        {
            NewClient();
        }

        private void NewClient(int counter = 0)
        {
            if (_clientState.LocalContentId == 0)
            {
                Thread.Sleep(50);
                NewClient(++counter);
            }
            if(counter > 100)
            {
                PluginLog.Verbose(DateTimeOffset.Now.ToUnixTimeMilliseconds() + " Failed to retrieve new client id");
                return;
            }
            _canRun = true;
            _odrDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "My Games", "FINAL FANTASY XIV - A Realm Reborn",
                $"FFXIV_CHR{this._clientState.LocalContentId:X16}");
            _odrPath = Path.Combine(_odrDirectory, "ITEMODR.DAT");
            if (_semaphoreSlim != null)
            {
                _semaphoreSlim.Dispose();
            }
            _semaphoreSlim = new SemaphoreSlim(1);
            _semaphoreSlim.Wait();
            ParseItemOrder();
            _semaphoreSlim.Release();
            if (_odrWatcher != null)
            {
                _odrWatcher.EnableRaisingEvents = false;
                _odrWatcher.Dispose();
            }
            _odrWatcher = new FileSystemWatcher(_odrDirectory);
            _odrWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _odrWatcher.Filter = "ITEMODR.DAT";
            _odrWatcher.Changed += OdrWatcherOnChanged;
            _odrWatcher.EnableRaisingEvents = true;
        }

        private void OdrWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            _semaphoreSlim.Wait();
            ParseOdr();
            _semaphoreSlim.Release();
        }

        public void RequestParseOdr()
        {
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
                PluginLog.Verbose(DateTimeOffset.Now.ToUnixTimeMilliseconds() + " Itemodr parsing failed hard");
                return;
            }
            try
            {
                var sortOrder = ParseItemOrder();
                _sortOrder = sortOrder;
                OnSortOrderChanged?.Invoke(sortOrder);
                PluginLog.Verbose(DateTimeOffset.Now.ToUnixTimeMilliseconds() + " Itemodr reparsed");
            }
            catch (Exception e)
            {
                PluginLog.Verbose(DateTimeOffset.Now.ToUnixTimeMilliseconds() + " Failed to reparse iremodr because " + e.Message);
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
            "ArmouryWaist",
            "ArmouryLegs",
            "ArmouryFeet",
            "ArmouryOffHand",
            "ArmouryEars",
            "ArmouryNeck",
            "ArmouryWrists",
            "ArmouryRings",
            "ArmourySoulCrystal",
            "SaddleBag",
            "SaddleBagPremium",
        };

        private InventorySortOrder ParseItemOrder()
        {
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
                    PluginLog.Verbose(e.Message);
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

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _clientState.Login -= ClientStateOnOnLogin;
                _clientState.Logout -= ClientStateOnOnLogout;
                _characterMonitor.OnCharacterUpdated -= CharacterMonitorOnOnCharacterUpdated;
                _semaphoreSlim?.Dispose();
                _odrWatcher?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

    }

}
