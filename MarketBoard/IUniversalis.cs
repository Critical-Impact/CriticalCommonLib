using System;
using System.Collections.Generic;
using CriticalCommonLib.Models;

namespace CriticalCommonLib.MarketBoard
{
    public interface IUniversalis : IDisposable
    {
        event Universalis.ItemPriceRetrievedDelegate? ItemPriceRetrieved;
        int QueuedCount { get; }
        int SaleHistoryLimit { get; }
        MarketPricing? RetrieveMarketBoardPrice(InventoryItem item, uint worldId);
        MarketPricing? RetrieveMarketBoardPrice(uint itemId, uint worldId);
        void SetSaleHistoryLimit(int limit);
        void Initalise();
        void QueuePriceCheck(uint itemId, uint worldId);
        void RetrieveMarketBoardPrices(IEnumerable<uint> itemIds, uint worldId);
    }
}