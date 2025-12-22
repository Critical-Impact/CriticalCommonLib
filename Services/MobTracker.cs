using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using AllaganLib.Shared.Services;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using LuminaSupplemental.Excel.Model;
using Microsoft.Extensions.Logging;

namespace CriticalCommonLib.Services
{
    public class MobTracker : HostedFrameworkService, IMobTracker
    {
        private readonly IGameInteropProvider _gameInteropProvider;
        private readonly IFramework _framework;
        private readonly ILogger<MobTracker> _logger;
        private readonly IClientState _clientState;
        private readonly ExcelSheet<BNpcName> _bNpcNameSheet;
        private readonly ExcelSheet<TerritoryType> _territoryTypeSheet;
        private readonly IObjectTable _objectTable;
        private DateTime? lastScanTime;
        private TimeSpan scanFrequency = TimeSpan.FromSeconds(5);

        public MobTracker(IGameInteropProvider gameInteropProvider, IFramework framework, ILogger<MobTracker> logger,
            IClientState clientState, ExcelSheet<BNpcName> bNpcNameSheet, ExcelSheet<TerritoryType> territoryTypeSheet, IObjectTable objectTable) : base(logger, framework)
        {
            _gameInteropProvider = gameInteropProvider;
            _framework = framework;
            _logger = logger;
            _clientState = clientState;
            _bNpcNameSheet = bNpcNameSheet;
            _territoryTypeSheet = territoryTypeSheet;
            _objectTable = objectTable;
            _logger.LogTrace("Creating {Type} ({This})", GetType().Name, this);

        }


        public override void FrameworkOnUpdate(IFramework framework)
        {
            if (!_enabled)
            {
                return;
            }
            if (lastScanTime != null && lastScanTime + scanFrequency >= DateTime.Now)
            {
                return;
            }
            lastScanTime = DateTime.Now;
            var territory = this._clientState.TerritoryType;
            if (territory == 0)
            {
                return;
            }
            foreach (var gameObject in _objectTable.CharacterManagerObjects)
            {
                if (gameObject is IBattleNpc battleNpc)
                {

                    if (battleNpc.NameId != 0 && battleNpc.BaseId != 0)
                    {
                        AddEntry(battleNpc, territory);
                    }
                }
            }

        }

        private bool _enabled;

        public bool Enabled => _enabled;

        public void Enable()
        {
            _enabled = true;
        }

        public void Disable()
        {
            _enabled = false;
        }

        private Dictionary<uint, Dictionary<uint, List<MobSpawnPosition>>> positions = new();

        public void AddEntry(IBattleChara battleChara, uint territoryTypeId)
        {
            positions.TryAdd(territoryTypeId, new Dictionary<uint, List<MobSpawnPosition>>());
            positions[territoryTypeId].TryAdd(battleChara.NameId, new List<MobSpawnPosition>());
            //Store
            var existingPositions = positions[territoryTypeId][battleChara.NameId];
            if (!existingPositions.Any(c => WithinRange(battleChara.Position, c.Position, maxRange)))
            {
                _logger.LogTrace("Added new mob {BaseId}, {NameId}", battleChara.BaseId, battleChara.NameId);
                existingPositions.Add(new MobSpawnPosition()
                {
                    BNpcBaseId = battleChara.BaseId,
                    BNpcNameId = battleChara.NameId,
                    Position = battleChara.Position,
                    TerritoryTypeId = territoryTypeId,
                    Subtype = battleChara.SubKind,
                });
            }
        }

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

        private const float maxRange = 5.0f;

        private bool WithinRange(Vector3 pointA, Vector3 pointB, float maxRange)
        {
            RectangleF recA = new RectangleF( new PointF(pointA.X - maxRange, pointA.Y - maxRange), new SizeF(maxRange,maxRange));
            RectangleF recB = new RectangleF( new PointF(pointB.X - maxRange, pointB.Y - maxRange), new SizeF(maxRange,maxRange));
            return recA.IntersectsWith(recB);
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

        public void Dispose()
        {
        }
    }
}