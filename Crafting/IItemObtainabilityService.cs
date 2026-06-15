using System.Collections.Generic;
using AllaganLib.GameSheets.Sheets.Rows;
using Lumina.Excel;

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
    string Description,
    RowRef RowRef
);

public record MissingRequirementGroup(
    ObtainabilityRequirementType Type,
    string Description,
    RowRef RowRef,
    IReadOnlyList<string> AffectedItems
);

public interface IItemObtainabilityService
{
    IReadOnlyList<ObtainabilityRequirement> GetRequirements(ItemRow item, IngredientPreferenceType source, RecipeRow? recipe = null);
}
