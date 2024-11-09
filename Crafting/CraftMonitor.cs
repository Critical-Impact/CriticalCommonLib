using System;
using AllaganLib.GameSheets.Service;
using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Rows;
using CriticalCommonLib.Agents;
using CriticalCommonLib.Services.Ui;
using Dalamud.Plugin.Services;
using InventoryItem = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;

namespace CriticalCommonLib.Crafting
{
    public class CraftMonitor : ICraftMonitor
    {
        private IGameUiManager _gameUiManager;
        private readonly RecipeSheet _recipeSheet;

        public CraftMonitor(IGameUiManager gameUiManager, RecipeSheet recipeSheet)
        {
            this._gameUiManager = gameUiManager;
            this._recipeSheet = recipeSheet;
            gameUiManager.UiVisibilityChanged += this.GameUiManagerOnUiVisibilityChanged;
            Service.Framework.Update += this.FrameworkOnUpdate;
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
                    Service.Framework.RunOnFrameworkThread(() =>
                    {
                        if (this._currentRecipe != null)
                        {
                            Service.Log.Debug("Craft completed");
                            this.CraftCompleted?.Invoke(this._currentItemId.Value, InventoryItem.ItemFlags.HighQuality, this._currentRecipe.Base.AmountResult);
                        }
                        else
                        {
                            Service.Log.Debug("Craft completed");
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
                        Service.Framework.RunOnFrameworkThread(() =>
                        {
                            if (this._currentRecipe != null)
                            {
                                var yield = this._currentRecipe.Base.AmountResult;
                                Service.Log.Debug("Craft completed");
                                this.CraftCompleted?.Invoke(this._currentItemId.Value, InventoryItem.ItemFlags.None,
                                    (simpleAgentNqCompleted - this._nqCompleted.Value) * yield);
                            }
                            else
                            {
                                Service.Log.Debug("Craft completed");
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
                        Service.Framework.RunOnFrameworkThread(() =>
                        {
                            if (this._currentRecipe != null)
                            {
                                var yield = this._currentRecipe.Base.AmountResult;
                                Service.Log.Debug("Craft completed");
                                this.CraftCompleted?.Invoke(this._currentItemId.Value, InventoryItem.ItemFlags.HighQuality,
                                    (simpleAgentHqCompleted - this._hqCompleted.Value) * yield);
                            }
                            else
                            {
                                Service.Log.Debug("Craft completed");
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
                        Service.Log.Debug("Craft failed");
                        Service.Framework.RunOnFrameworkThread(() => { this.CraftFailed?.Invoke(this._currentItemId.Value); });
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

        private void WatchCraft()
        {
            if (this._currentWindow == null || !this._gameUiManager.IsWindowLoaded(this._currentWindow.Value))
            {
                return;
            }

            if (this._currentWindow == WindowName.Synthesis)
            {
                this._agent = this._gameUiManager.GetWindowAsPtr(this._currentWindow.Value.ToString());
                var recipe = this._agent.Recipe;
                this._currentRecipe = this._recipeSheet.GetRowOrDefault(recipe);
                if (this._currentRecipe != null)
                {
                    this._currentRecipeTable = this._currentRecipe.RecipeLevelTable;
                }
                else
                {
                    Service.Log.Error("Could not find correct recipe for given synthesis. ");
                }

                Service.Framework.RunOnFrameworkThread(() => { this.CraftStarted?.Invoke(this._agent.ResultItemId); });
            }
            else
            {
                this._simpleCraftingAgent = this._gameUiManager.GetWindowAsPtr(this._currentWindow.Value.ToString());
                var recipe = this._simpleCraftingAgent.Recipe;

                this._currentRecipe = this._recipeSheet.GetRowOrDefault(recipe);
                if (this._currentRecipe != null)
                {
                    this._currentRecipeTable = this._currentRecipe.RecipeLevelTable;
                }
                else
                {
                    Service.Log.Error("Could not find correct recipe for given synthesis. ");
                }

                Service.Framework.RunOnFrameworkThread(() =>
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
                    Service.Framework.RunOnFrameworkThread(() => { this.CraftFailed?.Invoke(this._currentItemId.Value); });
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
                //_gameUiManager.UiVisibilityChanged -= GameUiManagerOnUiVisibilityChanged;
                Service.Framework.Update -= this.FrameworkOnUpdate;
            }
            this._disposed = true;
        }

        ~CraftMonitor()
        {
#if DEBUG
            // In debug-builds, make sure that a warning is displayed when the Disposable object hasn't been
            // disposed by the programmer.

            if( this._disposed == false )
            {
                Service.Log.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
            }
#endif
            this.Dispose (true);
        }
    }
}