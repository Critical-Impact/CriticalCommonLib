using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Models;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class RecipeEx : Recipe
    {
        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            _Ingredients = BuildIngredients(gameData, language);
            ItemResultEx = new LazyRow<ItemEx>(gameData, ItemResult.Row, language);
            CraftTypeEx = new LazyRow<CraftTypeEx>(gameData, CraftType.Row, language);
        }
        
        private RecipeIngredient[] _Ingredients = null!;
        public LazyRow<ItemEx> ItemResultEx = null!;
        public LazyRow<CraftTypeEx> CraftTypeEx = null!;

        public IEnumerable<RecipeIngredient> Ingredients => _Ingredients;

        private RecipeIngredient[] BuildIngredients(GameData gameData, Language language) {
            var ingredients = new List<RecipeIngredient>();

            foreach(var material in UnkData5)
            {
                if (material == null || material.ItemIngredient == 0)
                    continue;

                var count = material.AmountIngredient;
                if (count == 0)
                    continue;

                ingredients.Add(new RecipeIngredient( new LazyRow<ItemEx>(gameData, material.ItemIngredient, language), count));
            }

            return ingredients.ToArray();
        }

        public uint GetRecipeItemAmount(uint itemId)
        {
            return (uint)_Ingredients.Where(c => c.Item.Row == itemId).Sum(c => c.Count);
        }
    }
}