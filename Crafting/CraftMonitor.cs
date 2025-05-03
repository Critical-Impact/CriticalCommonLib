using System;
using System.Linq;
using AllaganLib.GameSheets.Service;
using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Rows;
using CriticalCommonLib.Agents;
using CriticalCommonLib.Services;
using CriticalCommonLib.Services.Ui;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using InventoryItem = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;

namespace CriticalCommonLib.Crafting
{
    public class CraftMonitor : ICraftMonitor
    {
        private IGameUiManager _gameUiManager;
        private readonly RecipeSheet _recipeSheet;
        private readonly IClientState _clientState;
        private readonly IFramework _framework;
        private readonly IPluginLog _pluginLog;
        private readonly ClassJobService _classJobService;
        private readonly ExcelSheet<GathererCrafterLvAdjustTable> _adjustSheet;
        private readonly RecipeLevelTableSheet _recipeLevelTableSheet;

        public CraftMonitor(IGameUiManager gameUiManager, RecipeSheet recipeSheet, IClientState clientState, IFramework framework, IPluginLog pluginLog, ClassJobService classJobService, ExcelSheet<GathererCrafterLvAdjustTable> adjustSheet, RecipeLevelTableSheet recipeLevelTableSheet)
        {
            this._gameUiManager = gameUiManager;
            this._recipeSheet = recipeSheet;
            _clientState = clientState;
            _framework = framework;
            _pluginLog = pluginLog;
            _classJobService = classJobService;
            _adjustSheet = adjustSheet;
            _recipeLevelTableSheet = recipeLevelTableSheet;
            gameUiManager.UiVisibilityChanged += this.GameUiManagerOnUiVisibilityChanged;
            _framework.Update += this.FrameworkOnUpdate;
        }

        public delegate void CraftStartedDelegate(uint itemId);
        public delegate void CraftFailedDelegate(uint itemId);
        public delegate void CraftCompletedDelegate(uint itemId, InventoryItem.ItemFlags flags, uint quantity);
        public event CraftStartedDelegate? CraftStarted;
        public event CraftFailedDelegate? CraftFailed;
        public event CraftCompletedDelegate? CraftCompleted;

        //For normal crafting
        private uint? _progressRequired;
        private uint? _progressMade;
        private bool? _completed;

        //For simple crafting
        private uint? _nqCompleted;
        private uint? _hqCompleted;
        private uint? _failed;

        //For both
        private uint? _currentItemId;

        private void FrameworkOnUpdate(IFramework framework)
        {
            if (this.Agent != null && this.RecipeLevelTable != null && !this.Agent.IsTrialSynthesis)
            {
                if (this._progressMade != this.Agent.Progress)
                {
                    this._progressMade = (uint?) this.Agent.Progress;
                }
                if (this._currentItemId != this.Agent.ResultItemId)
                {
                    this._currentItemId = this.Agent.ResultItemId;
                }

                if (this._progressRequired != this.RecipeLevelTable.ProgressRequired(this.CurrentRecipe))
                {
                    this._progressRequired = this.RecipeLevelTable.ProgressRequired(this.CurrentRecipe);
                }

                if (this._completed == null && this._progressRequired != 0 && this._progressMade == this._progressRequired && this._currentItemId != null)
                {
                    //Need to work out how to know if it was HQ output
                    _framework.RunOnFrameworkThread(() =>
                    {
                        if (this._currentRecipe != null)
                        {
                            _pluginLog.Debug("Craft completed");
                            this.CraftCompleted?.Invoke(this._currentItemId.Value, InventoryItem.ItemFlags.HighQuality, this._currentRecipe.Base.AmountResult);
                        }
                        else
                        {
                            _pluginLog.Debug("Craft completed");
                            this.CraftCompleted?.Invoke(this._currentItemId.Value, InventoryItem.ItemFlags.HighQuality, 1);
                        }
                    });
                    this._completed = true;
                }
            }
            if (this.SimpleAgent != null && this.RecipeLevelTable != null)
            {
                var simpleAgentNqCompleted = Math.Max(0,this.SimpleAgent.NqCompleted);
                var simpleAgentHqCompleted = Math.Max(0,this.SimpleAgent.HqCompleted);
                var simpleAgentFailed = Math.Max(0,this.SimpleAgent.TotalFailed);
                var itemId = this.SimpleAgent.ResultItemId;
                var finished = this.SimpleAgent.Finished;
                if (simpleAgentFailed >= 500)
                {
                    return;
                }
                if (this._currentItemId != itemId)
                {
                    this._currentItemId = itemId;
                }
                if (this._nqCompleted != simpleAgentNqCompleted)
                {
                    if (this._nqCompleted == null)
                    {
                        this._nqCompleted = 0;
                    }
                    else if(this._currentItemId != null)
                    {
                        _framework.RunOnFrameworkThread(() =>
                        {
                            if (this._currentRecipe != null)
                            {
                                var yield = this._currentRecipe.Base.AmountResult;
                                _pluginLog.Debug("Craft completed");
                                this.CraftCompleted?.Invoke(this._currentItemId.Value, InventoryItem.ItemFlags.None,
                                    (simpleAgentNqCompleted - this._nqCompleted.Value) * yield);
                            }
                            else
                            {
                                _pluginLog.Debug("Craft completed");
                                this.CraftCompleted?.Invoke(this._currentItemId.Value, InventoryItem.ItemFlags.None,
                                    simpleAgentNqCompleted - this._nqCompleted.Value);
                            }
                        });
                        this._nqCompleted = simpleAgentNqCompleted;
                    }
                }
                if (this._hqCompleted != simpleAgentHqCompleted)
                {
                    if (this._hqCompleted == null)
                    {
                        this._hqCompleted = 0;
                    }
                    else if(this._currentItemId != null)
                    {
                        _framework.RunOnFrameworkThread(() =>
                        {
                            if (this._currentRecipe != null)
                            {
                                var yield = this._currentRecipe.Base.AmountResult;
                                _pluginLog.Debug("Craft completed");
                                this.CraftCompleted?.Invoke(this._currentItemId.Value, InventoryItem.ItemFlags.HighQuality,
                                    (simpleAgentHqCompleted - this._hqCompleted.Value) * yield);
                            }
                            else
                            {
                                _pluginLog.Debug("Craft completed");
                                this.CraftCompleted?.Invoke(this._currentItemId.Value, InventoryItem.ItemFlags.HighQuality,
                                    simpleAgentHqCompleted - this._hqCompleted.Value);
                            }
                        });
                        this._hqCompleted = simpleAgentHqCompleted;
                    }
                }

                if (this._failed != simpleAgentFailed)
                {
                    if (this._failed == null)
                    {
                        this._failed = 0;
                    }
                    else if(this._currentItemId != null)
                    {
                        _pluginLog.Debug("Craft failed");
                        _framework.RunOnFrameworkThread(() => { this.CraftFailed?.Invoke(this._currentItemId.Value); });
                        this._failed = simpleAgentFailed;
                    }
                }


                if (finished)
                {
                    this._completed = true;
                }
            }
        }

        private void GameUiManagerOnUiVisibilityChanged(WindowName windowname, bool? windowstate)
        {
            if (windowname == WindowName.Synthesis || windowname == WindowName.SynthesisSimple)
            {
                this._currentWindow = windowname;
                if (windowstate == true)
                {
                    this.WatchCraft();
                }
                else
                {
                    this.StopWatchingCraft();
                }
            }
        }

        public CraftingAgent? Agent => this._agent;
        public SimpleCraftingAgent? SimpleAgent => this._simpleCraftingAgent;

        public RecipeRow? CurrentRecipe => this._currentRecipe;
        public RecipeLevelTableRow? RecipeLevelTable => this._currentRecipeTable;

        private CraftingAgent? _agent;
        private SimpleCraftingAgent? _simpleCraftingAgent;

        private RecipeRow? _currentRecipe;
        private RecipeLevelTableRow? _currentRecipeTable;
        private WindowName? _currentWindow;


        public uint CraftType => (uint) (_clientState.LocalPlayer?.ClassJob.ValueNullable?.DohDolJobIndex ?? 0);

        public unsafe uint Recipe
        {
            get
            {

                if (Agent != null && _recipeSheet
                    .Count(c => c.Base.CraftType.RowId == CraftType && c.Base.ItemResult.RowId == Agent.ResultItemId) == 1)
                {
                    return _recipeSheet
                        .Single(c => c.Base.CraftType.RowId == CraftType && c.Base.ItemResult.RowId == Agent.ResultItemId).RowId;
                }

                if (Agent != null)
                {
                    var csRecipeNote = CSRecipeNote.Instance();
                    if (csRecipeNote != null && csRecipeNote->ActiveCraftRecipeId != 0)
                    {
                        return csRecipeNote->ActiveCraftRecipeId;
                    }
                }
                if (SimpleAgent != null && _recipeSheet
                    .Count(c => c.Base.CraftType.RowId == CraftType && c.Base.ItemResult.RowId == SimpleAgent.ResultItemId) == 1)
                {
                    return _recipeSheet
                        .Single(c => c.Base.CraftType.RowId == CraftType && c.Base.ItemResult.RowId == SimpleAgent.ResultItemId).RowId;
                }



                return 0;
            }
        }

        private void WatchCraft()
        {
            if (this._currentWindow == null || !this._gameUiManager.IsWindowLoaded(this._currentWindow.Value))
            {
                return;
            }

            if (this._currentWindow == WindowName.Synthesis)
            {
                this._agent = this._gameUiManager.GetWindowAsPtr(this._currentWindow.Value.ToString());
                var recipe = Recipe;
                this._currentRecipe = this._recipeSheet.GetRowOrDefault(recipe);
                if (this._currentRecipe != null)
                {
                    this._currentRecipeTable = this._currentRecipe.RecipeLevelTable;

                    if (_currentRecipe.Base.Unknown0 != 0)
                    {
                        _pluginLog.Verbose($"Stellar mission recipe level is {_currentRecipe.Base.Unknown0.ToString()}");
                        _pluginLog.Verbose($"Original recipe table is {this._currentRecipeTable?.RowId.ToString() ?? "Not Set"}");
                        var classJob = (ClassJobService.ClassJobList)_currentRecipe.Base.CraftType.RowId;
                        var adjustedJobLevel = Math.Min(_classJobService.GetWksSyncedLevel(classJob), _currentRecipe.Base.Unknown0);
                        _currentRecipeTable = _recipeLevelTableSheet.GetRow(_adjustSheet.GetRow(adjustedJobLevel).Unknown0);
                        _pluginLog.Verbose($"New recipe table is {this._currentRecipeTable?.RowId.ToString() ?? "Not Set"}");

                    }
                }
                else
                {
                    _pluginLog.Error("Could not find correct recipe for given synthesis. ");
                }

                _framework.RunOnFrameworkThread(() => { this.CraftStarted?.Invoke(this._agent.ResultItemId); });
            }
            else
            {
                this._simpleCraftingAgent = this._gameUiManager.GetWindowAsPtr(this._currentWindow.Value.ToString());
                var recipe = Recipe;

                this._currentRecipe = this._recipeSheet.GetRowOrDefault(recipe);
                if (this._currentRecipe != null)
                {
                    this._currentRecipeTable = this._currentRecipe.RecipeLevelTable;
                }
                else
                {
                    _pluginLog.Error("Could not find correct recipe for given synthesis. ");
                }

                _framework.RunOnFrameworkThread(() =>
                {
                    this.CraftStarted?.Invoke(this._simpleCraftingAgent.ResultItemId);
                });
            }
        }

        private void StopWatchingCraft()
        {
            this._currentRecipe = null;
            this._currentRecipeTable = null;
            this._agent = null;
            this._simpleCraftingAgent = null;
            if (this._currentWindow == WindowName.Synthesis)
            {
                if (this._progressRequired != 0 && this._progressMade != this._progressRequired && this._currentItemId != null)
                {
                    _framework.RunOnFrameworkThread(() => { this.CraftFailed?.Invoke(this._currentItemId.Value); });
                    this._progressMade = null;
                    this._progressRequired = null;
                    this._currentItemId = null;
                    this._completed = null;
                }
                else
                {
                    this._progressMade = null;
                    this._progressRequired = null;
                    this._currentItemId = null;
                    this._completed = null;
                }
            }
            else if(this._currentWindow == WindowName.SynthesisSimple)
            {
                this._completed = null;
                this._nqCompleted = null;
                this._hqCompleted = null;
                this._currentItemId = null;
                this._failed = null;
            }

            this._currentWindow = null;
        }

        private bool _disposed;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(!this._disposed && disposing)
            {
                _gameUiManager.UiVisibilityChanged -= GameUiManagerOnUiVisibilityChanged;
                _framework.Update -= this.FrameworkOnUpdate;
            }
            this._disposed = true;
        }
    }
}