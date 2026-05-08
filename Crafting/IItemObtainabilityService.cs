using System.Collections.Generic;
using AllaganLib.GameSheets.Sheets.Rows;

namespace CriticalCommonLib.Crafting;

public enum ObtainabilityRequirementType
{
    JobLevel,
    SecretRecipeBook,
    FolkloreTome,
    Specialization
}

public record ObtainabilityRequirement(
    ObtainabilityRequirementType Type,
    bool IsMet,
    string Description
);

public interface IItemObtainabilityService
{
    IReadOnlyList<ObtainabilityRequirement> GetRequirements(ItemRow item, IngredientPreferenceType source, RecipeRow? recipe = null);
}
