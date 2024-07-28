using System;
using System.Collections.Generic;
using System.Globalization;
using CriticalCommonLib.Sheets;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.MarketBoard;

public class MarketPricing  : ICsv
{
    public uint ItemId { get; set; }
    public uint WorldId { get; set; }
    public float AveragePriceNq { get; set; }
    public float AveragePriceHq { get; set; }
    public float MinPriceNq { get; set; }
    public float MinPriceHq { get; set; }
    public int SevenDaySellCount { get; set; }
    
    public int Available { get; set; }
    public DateTime? LastSellDate { get; set; }
    public DateTime LastUpdate { get; set; } = DateTime.Now;
    
    public LazyRow< WorldEx > World;
    public LazyRow< ItemEx > Item;
    
    public RecentHistory[]? recentHistory;
    public Listing[]? listings;
    public void FromCsv(string[] lineData)
    {
        ItemId = uint.Parse( lineData[ 0 ], CultureInfo.InvariantCulture );
        WorldId = uint.Parse( lineData[ 1 ], CultureInfo.InvariantCulture );
        AveragePriceNq = float.Parse( lineData[ 2 ], CultureInfo.InvariantCulture );
        AveragePriceHq = float.Parse( lineData[ 3 ], CultureInfo.InvariantCulture );
        MinPriceNq = float.Parse( lineData[ 4 ], CultureInfo.InvariantCulture );
        MinPriceHq = float.Parse( lineData[ 5 ], CultureInfo.InvariantCulture );
        SevenDaySellCount = int.Parse( lineData[ 6 ], CultureInfo.InvariantCulture );
        LastSellDate = lineData[7] == "" ? null : DateTime.Parse( lineData[ 7 ], CultureInfo.InvariantCulture );
        LastUpdate = DateTime.Parse( lineData[ 8 ], CultureInfo.InvariantCulture );
        Available = int.Parse( lineData[ 9 ], CultureInfo.InvariantCulture );
    }

    public string[] ToCsv()
    {
        List<String> data = new List<string>()
        {
            ItemId.ToString(),
            WorldId.ToString(),
            AveragePriceNq.ToString(CultureInfo.InvariantCulture),
            AveragePriceHq.ToString(CultureInfo.InvariantCulture),
            MinPriceNq.ToString(CultureInfo.InvariantCulture),
            MinPriceHq.ToString(CultureInfo.InvariantCulture),
            SevenDaySellCount.ToString(CultureInfo.InvariantCulture),
            LastSellDate.HasValue ? LastSellDate.Value.ToString(CultureInfo.InvariantCulture) : "",
            LastUpdate.ToString(CultureInfo.InvariantCulture),
            Available.ToString(CultureInfo.InvariantCulture),
        };
        return data.ToArray();
    }

    public bool IncludeInCsv()
    {
        return true;
    }

    public void PopulateData(GameData gameData, Language language)
    {
        World = new LazyRow<WorldEx>(gameData, WorldId, language);
        Item = new LazyRow<ItemEx>(gameData, ItemId, language);
    }
    
    public static MarketPricing FromApi(PricingAPIResponse apiResponse, uint worldId, int saleHistoryLimit)
    {
        MarketPricing response = new MarketPricing();
        response.AveragePriceNq = apiResponse.averagePriceNQ;
        response.AveragePriceHq = apiResponse.averagePriceHQ;
        response.MinPriceHq = apiResponse.minPriceHQ;
        response.MinPriceNq = apiResponse.minPriceNQ;
        response.ItemId = apiResponse.itemID;
        response.Available = apiResponse.listings?.Length ?? 0;
        response.WorldId = worldId;
        response.PopulateData(Service.ExcelCache.GameData, Service.ExcelCache.Language);
        //Not actually saved but persist in memory, might need to look at how much of a memory blow out this could cause
        response.listings = apiResponse.listings;
        response.recentHistory = apiResponse.recentHistory;
        int? realMinPriceHq = null;
        int? realMinPriceNq = null;
        if (apiResponse.listings != null && apiResponse.listings.Length != 0)
        {
            foreach (var listing in apiResponse.listings)
            {
                if (listing.hq)
                {
                    if (realMinPriceHq == null || realMinPriceHq > listing.pricePerUnit)
                    {
                        realMinPriceHq = listing.pricePerUnit;
                    }
                }
                else
                {
                    if (realMinPriceNq == null || realMinPriceNq > listing.pricePerUnit)
                    {
                        realMinPriceNq = listing.pricePerUnit;
                    }
                }
            }

            if (realMinPriceHq != null)
            {
                response.MinPriceHq = realMinPriceHq.Value;
            }

            if (realMinPriceNq != null)
            {
                response.MinPriceNq = realMinPriceNq.Value;
            }
        }
        if (apiResponse.recentHistory != null && apiResponse.recentHistory.Length != 0)
        {
            DateTime? latestDate = null;
            int sevenDaySales = 0;
            foreach (var history in apiResponse.recentHistory)
            {
                var dateTime = DateTimeOffset.FromUnixTimeSeconds(history.timestamp).LocalDateTime;
                if (latestDate == null || latestDate <= dateTime)
                {
                    latestDate = dateTime;
                }

                if (dateTime >= DateTime.Now.AddDays(-saleHistoryLimit))
                {
                    sevenDaySales++;
                }
            }

            response.SevenDaySellCount = sevenDaySales;
            response.LastSellDate = latestDate;

        }
        else
        {
            response.LastSellDate = null;
            response.SevenDaySellCount = 0;
        }

        return response;
    }
}