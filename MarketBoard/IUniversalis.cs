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
        PricingResponse? RetrieveMarketBoardPrice(InventoryItem item);
        PricingResponse? RetrieveMarketBoardPrice(uint itemId);
        void SetSaleHistoryLimit(int limit);
        void Initalise();
        void QueuePriceCheck(uint itemId);
        void RetrieveMarketBoardPrices(IEnumerable<uint> itemIds);
    }
}