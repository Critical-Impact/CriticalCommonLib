using CriticalCommonLib.Models;
using Microsoft.Extensions.Hosting;

namespace CriticalCommonLib.Services;

public interface IOdrScanner : IHostedService
{
    event OdrScanner.SortOrderChangedDelegate? OnSortOrderChanged;
    InventorySortOrder? GetSortOrder(ulong characterId);
}