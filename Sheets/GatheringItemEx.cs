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
        public static readonly Dictionary<uint,uint> Items = new()
        {
            {203, 256},  // Grade 1 La Noscean Topsoil
            // {7761, 200},  // Grade 1 Shroud Topsoil   
            // {7764, 201},  // Grade 1 Thanalan Topsoil 
            // {7759, 150},  // Grade 2 La Noscean Topsoil
            // {7762, 209},  // Grade 2 Shroud Topsoil   
            // {7765, 151},  // Grade 2 Thanalan Topsoil 
            // {10092, 210}, // Black Limestone          
            // {10094, 177}, // Little Worm              
            // {10097, 133}, // Yafaemi Wildgrass        
            // {12893, 295}, // Dark Chestnut            
            // {15865, 30},  // Firelight Seeds          
            // {15866, 39},  // Icelight Seeds           
            // {15867, 21},  // Windlight Seeds          
            // {15868, 31},  // Earthlight Seeds         
            // {15869, 25},  // Levinlight Seeds         
            // {15870, 14},  // Waterlight Seeds
            // {12534, 285}, // Mythrite Ore             
            // {12535, 353}, // Hardsilver Ore           
            // {12537, 286}, // Titanium Ore             
            // {12579, 356}, // Birch Log                
            // {12878, 297}, // Cyclops Onion            
            // {12879, 298}, // Emerald Beans            
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