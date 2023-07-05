using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class GatheringItemEx : GatheringItem
    {
        //GatheringPointBase => GatheringItemId
        public static readonly Dictionary<uint,uint> Items = new()
        {
            {203, 256},  // Grade 1 La Noscean Topsoil, 7758
            {200, 259},// Grade 1 Shroud Topsoil, 7761
            {201, 262},  // Grade 1 Thanalan Topsoil 
            {150, 257},  // Grade 2 La Noscean Topsoil
            {209, 260},  // Grade 2 Shroud Topsoil   
            {151, 263},  // Grade 2 Thanalan Topsoil 
            {210, 289}, // Black Limestone          
            {177, 293}, // Little Worm              
            {133, 295}, // Yafaemi Wildgrass        
            {295, 351}, // Dark Chestnut            
            {30, 410},  // Firelight Seeds          
            {39, 411},  // Icelight Seeds           
            {21, 412},  // Windlight Seeds          
            {31, 413},  // Earthlight Seeds         
            {25, 414},  // Levinlight Seeds         
            {14, 415},  // Waterlight Seeds
            {285, 301}, // Mythrite Ore             
            {353, 313}, // Hardsilver Ore           
            {286, 307}, // Titanium Ore             
            {356, 357}, // Birch Log                
            {297, 347}, // Cyclops Onion            
            {298, 352}, // Emerald Beans            
        };

        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            _gatheringItemPoints =
                new Lazy<List<GatheringPointEx>>(CalculateGatheringItems, LazyThreadSafetyMode.PublicationOnly);
            GatheringPointBases = new LazyRelated<GatheringPointBaseEx, GatheringItemEx>(this, gameData, language,
                exes =>
                {
                    var bases = new Dictionary<uint,List<uint>>();
                    foreach (var gatheringPointBase in exes)
                    {
                        if (Items.ContainsKey(gatheringPointBase.RowId))
                        {
                            var itemId = Items[gatheringPointBase.RowId];
                            bases.TryAdd(itemId, new List<uint>());
                            bases[itemId].Add(gatheringPointBase.RowId);
                        }
                        foreach (var item in gatheringPointBase.Item)
                        {
                            if (item == 0) continue;
                            var itemId = (uint)item;
                            bases.TryAdd(itemId, new List<uint>());
                            bases[itemId].Add(gatheringPointBase.RowId);
                        }
                    }
                    
                    return bases;
                }, "GatheringPointBases");     
        }
        public LazyRelated<GatheringPointBaseEx, GatheringItemEx> GatheringPointBases = null!;
        private Lazy<List<GatheringPointEx>> _gatheringItemPoints = null!;
        
        private List<GatheringPointEx> CalculateGatheringItems()
        {
            if (Service.ExcelCache.GatheringItemToGatheringItemPoint.ContainsKey(RowId))
            {
                return Service.ExcelCache.GatheringItemToGatheringItemPoint[RowId]
                    .Select(c => Service.ExcelCache.GetGatheringPointExSheet().GetRow(c)).Where(c => c != null)
                    .Select(c => c!).ToList();
                ;
            }

            return new List<GatheringPointEx>();
        }

        public Lazy<List<GatheringPointEx>> GatheringItemPoints => _gatheringItemPoints;
    }
}