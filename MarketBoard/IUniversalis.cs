using System;

namespace CriticalCommonLib.MarketBoard
{
    public interface IUniversalis : IDisposable
    {
        event Universalis.ItemPriceRetrievedDelegate? ItemPriceRetrieved;
        int QueuedCount { get; }
        void SetSaleHistoryLimit(int limit);
        void QueuePriceCheck(uint itemId, uint worldId);
        public DateTime? LastFailure { get; }
        public bool TooManyRequests { get; }
    }
}