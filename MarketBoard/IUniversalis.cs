using System;

namespace CriticalCommonLib.MarketBoard
{
    public interface IUniversalis : IDisposable
    {
        event Universalis.ItemPriceRetrievedDelegate? ItemPriceRetrieved;
        int QueuedCount { get; }
        void SetSaleHistoryLimit(int limit);
        void QueuePriceCheck(uint itemId, uint worldId);
    }
}