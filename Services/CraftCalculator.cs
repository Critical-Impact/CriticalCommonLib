using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CriticalCommonLib.Crafting;
using CriticalCommonLib.Models;
using FFXIVClientStructs.FFXIV.Client.Game;
using InventoryItem = CriticalCommonLib.Models.InventoryItem;

namespace CriticalCommonLib.Services
{
    public class CraftCalculator
    {
        private readonly TaskFactory _taskFactory;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly HashSet<uint> _itemIdsToProcess;
        private List<InventoryItem> _availableItems;
        private bool _isRunning = false;

        public event EventHandler<CraftingResultEventArgs> CraftingResult;

        public CraftCalculator()
        {
            _taskFactory = new TaskFactory();
            _cancellationTokenSource = new CancellationTokenSource();
            _itemIdsToProcess = new HashSet<uint>();
            _availableItems = new List<InventoryItem>();
        }

        public bool IsRunning => _isRunning;

        public void StartProcessing()
        {
            if (_itemIdsToProcess.Count == 0)
            {
                // No itemIds to process
                return;
            }

            _isRunning = true;
            // Run the processing on a separate task
            _taskFactory.StartNew(ProcessItems, _cancellationTokenSource.Token);
        }

        public void StopProcessing()
        {
            _cancellationTokenSource.Cancel();
        }

        public void CancelProcessing()
        {
            _cancellationTokenSource.Cancel();
            ResetState();
        }

        public void AddItemId(uint itemId)
        {
            _itemIdsToProcess.Add(itemId);
        }

        public void SetAvailableItems(List<InventoryItem> availableItems)
        {
            _availableItems = availableItems;
        }

        private async Task ProcessItems()
        {
            foreach (var itemId in _itemIdsToProcess)
            {
                // Check cancellation token to exit early if requested
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    ResetState();
                    return;
                }

                var craftableQuantity = CalculateTotalCraftable(itemId, _availableItems);
                OnCraftingResult(itemId, craftableQuantity);
            }

            ResetState();
        }

        private uint? CalculateTotalCraftable(uint itemId, List<InventoryItem> availableItems)
        {
            var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(itemId);
            if (itemEx == null || !itemEx.CanBeCrafted)
            {
                return null;
            }

            var craftList = new CraftList();
            craftList.AddCraftItem(itemId, 999);
            craftList.GenerateCraftChildren();

            var characterSources = new Dictionary<uint, List<CraftItemSource>>();
            var externalSources = new Dictionary<uint, List<CraftItemSource>>();
            foreach (var item in availableItems)
            {
                if (!characterSources.ContainsKey(item.ItemId))
                {
                    characterSources.Add(item.ItemId, new List<CraftItemSource>());
                }
                characterSources[item.ItemId].Add(new CraftItemSource(item.ItemId, item.Quantity, item.IsHQ));
            }

            craftList.Update(characterSources, externalSources, true);
            return craftList.CraftItems.First().QuantityCanCraft;
        }

        private void ResetState()
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            _itemIdsToProcess.Clear();
            _isRunning = false;
        }

        private void OnCraftingResult(uint itemId, uint? craftableQuantity)
        {
            CraftingResult?.Invoke(this, new CraftingResultEventArgs(itemId, craftableQuantity));
        }
    }

    public class CraftingResultEventArgs : EventArgs
    {
        public uint ItemId { get; }
        public uint? CraftableQuantity { get; }

        public CraftingResultEventArgs(uint itemId, uint? craftableQuantity)
        {
            ItemId = itemId;
            CraftableQuantity = craftableQuantity;
        }
    }
}
