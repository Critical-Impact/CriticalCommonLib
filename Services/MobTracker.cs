using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Services
{
    public class MobTracker : IMobTracker
    {
        private readonly IGameInteropProvider _gameInteropProvider;
        private readonly IFramework _framework;
        private readonly IPluginLog _pluginLog;
        private readonly IClientState _clientState;
        private readonly ExcelSheet<BNpcName> _bNpcNameSheet;
        private readonly ExcelSheet<TerritoryType> _territoryTypeSheet;

        public MobTracker(IGameInteropProvider gameInteropProvider, IFramework framework, IPluginLog pluginLog,
            IClientState clientState, ExcelSheet<BNpcName> bNpcNameSheet, ExcelSheet<TerritoryType> territoryTypeSheet)
        {
            pluginLog.Verbose("Creating {type} ({this})", GetType().Name, this);
            _gameInteropProvider = gameInteropProvider;
            _framework = framework;
            _pluginLog = pluginLog;
            _clientState = clientState;
            _bNpcNameSheet = bNpcNameSheet;
            _territoryTypeSheet = territoryTypeSheet;
            framework.RunOnFrameworkThread(() => { _gameInteropProvider.InitializeFromAttributes(this); });;
        }

        private bool _enabled;

        public bool Enabled => _enabled;

        public void Enable()
        {
            _enabled = true;
            _framework.RunOnFrameworkThread(() =>
            {
                _npcSpawnHook?.Enable();
            });
        }

        public void Disable()
        {
            _enabled = false;
            _framework.RunOnFrameworkThread(() =>
            {
                _npcSpawnHook?.Disable();
            });
        }

        private Dictionary<uint, Dictionary<uint, List<MobSpawnPosition>>> positions = new Dictionary<uint, Dictionary<uint, List<MobSpawnPosition>>>();

        private unsafe delegate void* NpcSpawnData(int* a1, int a2, int* a3);

        [Signature("E8 ?? ?? ?? ?? F6 05 ?? ?? ?? ?? ?? 75 91", DetourName = nameof(NpcSpawnDetour), UseFlags = SignatureUseFlags.Hook)]
        private readonly Hook<NpcSpawnData>? _npcSpawnHook = null;

        public void AddEntry(MobSpawnPosition spawnPosition)
        {
            positions.TryAdd(spawnPosition.TerritoryTypeId, new Dictionary<uint, List<MobSpawnPosition>>());
            positions[spawnPosition.TerritoryTypeId].TryAdd(spawnPosition.BNpcNameId, new List<MobSpawnPosition>());
            //Store
            var existingPositions = positions[spawnPosition.TerritoryTypeId][spawnPosition.BNpcNameId];
            if (!existingPositions.Any(c => WithinRange(spawnPosition.Position, c.Position, maxRange)))
            {
                existingPositions.Add(spawnPosition);
            }
        }

        public void SetEntries(List<MobSpawnPosition> spawnPositions)
        {
            Disable();
            positions = spawnPositions.GroupBy(c => c.TerritoryTypeId).ToDictionary(c => c.Key, c => c.GroupBy(d => d.BNpcNameId).ToDictionary(c => c.Key, c => c.ToList()));
            Enable();
        }

        public List<MobSpawnPosition> GetEntries()
        {
            Disable();
            var newPositions = positions.SelectMany(c => c.Value.SelectMany(d => d.Value.Select(e => e))).ToList();
            Enable();
            return newPositions;
        }

        private const float maxRange = 1.0f;

        private bool WithinRange(Vector3 pointA, Vector3 pointB, float maxRange)
        {
            RectangleF recA = new RectangleF( new PointF(pointA.X - maxRange, pointA.Y - maxRange), new SizeF(maxRange,maxRange));
            RectangleF recB = new RectangleF( new PointF(pointB.X - maxRange, pointB.Y - maxRange), new SizeF(maxRange,maxRange));
            return recA.IntersectsWith(recB);
        }

        private unsafe void* NpcSpawnDetour(int* a1, int seq, int* a3)
        {
            try
            {
                if (a3 != null)
                {
                    var ptr = (IntPtr)a3;
                    var npcSpawnInfo = NetworkDecoder.DecodeNpcSpawn(ptr);
                    var bNpcName = _bNpcNameSheet.GetRowOrDefault(npcSpawnInfo.bNpcName);
                    if (bNpcName != null)
                    {
                        var map = _territoryTypeSheet.GetRowOrDefault(_clientState.TerritoryType)?.Map.ValueNullable;
                        if (map != null)
                        {
                            var newPos = Utils.WorldToMap(npcSpawnInfo.pos, map.Value.SizeFactor,
                                map.Value.OffsetX, map.Value.OffsetY);
                            MobSpawnPosition mobSpawnPosition = new MobSpawnPosition(npcSpawnInfo.bNpcBase,
                                npcSpawnInfo.bNpcName, _clientState.TerritoryType, newPos,
                                npcSpawnInfo.subtype);
                            AddEntry(mobSpawnPosition);
                        }
                    }
                }
                else
                {
                    _pluginLog.Error("a3 is null");
                }
            }
            catch (Exception e)
            {
                _pluginLog.Error(e, "shits broke yo");
            }
            return _npcSpawnHook!.Original(a1, seq, a3);
        }

        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool SaveCsv(string filePath, List<MobSpawnPosition> positions)
        {
            using var fileStream = new FileStream( filePath, FileMode.Create );
            using( StreamWriter reader = new StreamWriter( fileStream ) )
            {
                try
                {
                    using var csvReader = new CSVFile.CSVWriter( reader );
                    csvReader.WriteLine(MobSpawnPosition.GetHeaders());
                    foreach( var position in positions )
                    {
                        var linePosition = position.ToCsv( );
                        csvReader.WriteLine(linePosition);
                    }

                    return true;
                }
                catch( Exception )
                {
                    return false;
                }
            }
        }

        public List<MobSpawnPosition> LoadCsv(string filePath, out bool success)
        {
            success = false;
            if (File.Exists(filePath))
            {
                using var fileStream = new FileStream(filePath, FileMode.Open);
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    try
                    {
                        var csvReader = CSVFile.CSVReader.FromString(reader.ReadToEnd());
                        var items = new List<MobSpawnPosition>();
                        foreach (var line in csvReader.Lines())
                        {
                            MobSpawnPosition item = new MobSpawnPosition();
                            item.FromCsv(line);
                            items.Add(item);
                        }

                        success = true;
                        return items;
                    }
                    catch (Exception)
                    {
                        success = false;
                        return new List<MobSpawnPosition>();
                    }
                }
            }

            return new List<MobSpawnPosition>();
        }


        public void ClearSavedData()
        {

        }

        private void Dispose(bool disposing)
        {
            if(!_disposed && disposing)
            {
                _pluginLog.Verbose("Disposing {type} ({this})", GetType().Name, this);
                _npcSpawnHook?.Dispose();
            }
            _disposed = true;
        }

        ~MobTracker()
        {
#if DEBUG
            // In debug-builds, make sure that a warning is displayed when the Disposable object hasn't been
            // disposed by the programmer.

            if( _disposed == false )
            {
                _pluginLog.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
            }
#endif
            Dispose (true);
        }
    }
}