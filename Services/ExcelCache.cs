using AllaganLib.GameSheets.Model;
using AllaganLib.GameSheets.Service;
using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Caches;
using AllaganLib.GameSheets.Sheets.Rows;
using CriticalCommonLib.Crafting;
using Lumina;

namespace CriticalCommonLib.Services;

public class ExcelCache
{
    private readonly SheetManager _sheetManager;
    private readonly CraftingCache _craftingCache;
    private readonly GameData _gameData;

    public ExcelCache(SheetManager sheetManager, CraftingCache craftingCache, GameData gameData)
    {
        _sheetManager = sheetManager;
        _craftingCache = craftingCache;
        _gameData = gameData;
        Service.ExcelCache = this;
    }

    public SheetManager SheetManager => _sheetManager;

    public ItemInfoCache ItemInfoCache => _sheetManager.ItemInfoCache;

    public GameData GameData => _gameData;

    public CraftingCache CraftingCache => _craftingCache;

    private AirshipExplorationPointSheet? airshipExplorationPointSheet;

    public AirshipExplorationPointSheet GetAirshipExplorationPointSheet()
    {
        return airshipExplorationPointSheet ??= _sheetManager.GetSheet<AirshipExplorationPointSheet>();
    }

    private BNpcBaseSheet? bNpcBaseSheet;

    public BNpcBaseSheet GetBNpcBaseSheet()
    {
        return bNpcBaseSheet ??= _sheetManager.GetSheet<BNpcBaseSheet>();
    }

    private BNpcNameSheet? bNpcNameSheet;

    public BNpcNameSheet GetBNpcNameSheet()
    {
        return bNpcNameSheet ??= _sheetManager.GetSheet<BNpcNameSheet>();
    }

    private ClassJobCategorySheet? classJobCategorySheet;

    public ClassJobCategorySheet GetClassJobCategorySheet()
    {
        return classJobCategorySheet ??= _sheetManager.GetSheet<ClassJobCategorySheet>();
    }

    private ClassJobSheet? classJobSheet;

    public ClassJobSheet GetClassJobSheet()
    {
        return classJobSheet ??= _sheetManager.GetSheet<ClassJobSheet>();
    }

    private CompanyCraftPartSheet? companyCraftPartSheet;

    public CompanyCraftPartSheet GetCompanyCraftPartSheet()
    {
        return companyCraftPartSheet ??= _sheetManager.GetSheet<CompanyCraftPartSheet>();
    }

    private CompanyCraftProcessSheet? companyCraftProcessSheet;

    public CompanyCraftProcessSheet GetCompanyCraftProcessSheet()
    {
        return companyCraftProcessSheet ??= _sheetManager.GetSheet<CompanyCraftProcessSheet>();
    }

    private CompanyCraftSequenceSheet? companyCraftSequenceSheet;

    public CompanyCraftSequenceSheet GetCompanyCraftSequenceSheet()
    {
        return companyCraftSequenceSheet ??= _sheetManager.GetSheet<CompanyCraftSequenceSheet>();
    }

    private CompanyCraftSupplyItemSheet? companyCraftSupplyItemSheet;

    public CompanyCraftSupplyItemSheet GetCompanyCraftSupplyItemSheet()
    {
        return companyCraftSupplyItemSheet ??= _sheetManager.GetSheet<CompanyCraftSupplyItemSheet>();
    }

    private ContentFinderConditionSheet? contentFinderConditionSheet;

    public ContentFinderConditionSheet GetContentFinderConditionSheet()
    {
        return contentFinderConditionSheet ??= _sheetManager.GetSheet<ContentFinderConditionSheet>();
    }

    private ContentRouletteSheet? contentRouletteSheet;

    public ContentRouletteSheet GetContentRouletteSheet()
    {
        return contentRouletteSheet ??= _sheetManager.GetSheet<ContentRouletteSheet>();
    }

    private CraftTypeSheet? craftTypeSheet;

    public CraftTypeSheet GetCraftTypeSheet()
    {
        return craftTypeSheet ??= _sheetManager.GetSheet<CraftTypeSheet>();
    }

    private ENpcBaseSheet? eNpcBaseSheet;

    public ENpcBaseSheet GetENpcBaseSheet()
    {
        return eNpcBaseSheet ??= _sheetManager.GetSheet<ENpcBaseSheet>();
    }

    private ENpcResidentSheet? eNpcResidentSheet;

    public ENpcResidentSheet GetENpcResidentSheet()
    {
        return eNpcResidentSheet ??= _sheetManager.GetSheet<ENpcResidentSheet>();
    }

    private EquipRaceCategorySheet? equipRaceCategorySheet;

    public EquipRaceCategorySheet GetEquipRaceCategorySheet()
    {
        return equipRaceCategorySheet ??= _sheetManager.GetSheet<EquipRaceCategorySheet>();
    }

    private EquipSlotCategorySheet? equipSlotCategorySheet;

    public EquipSlotCategorySheet GetEquipSlotCategorySheet()
    {
        return equipSlotCategorySheet ??= _sheetManager.GetSheet<EquipSlotCategorySheet>();
    }

    private FccShopSheet? fccShopSheet;

    public FccShopSheet GetFccShopSheet()
    {
        return fccShopSheet ??= _sheetManager.GetSheet<FccShopSheet>();
    }

    private GatheringItemSheet? gatheringItemSheet;

    public GatheringItemSheet GetGatheringItemSheet()
    {
        return gatheringItemSheet ??= _sheetManager.GetSheet<GatheringItemSheet>();
    }

    private GatheringPointBaseSheet? gatheringPointBaseSheet;

    public GatheringPointBaseSheet GetGatheringPointBaseSheet()
    {
        return gatheringPointBaseSheet ??= _sheetManager.GetSheet<GatheringPointBaseSheet>();
    }

    private GatheringPointSheet? gatheringPointSheet;

    public GatheringPointSheet GetGatheringPointSheet()
    {
        return gatheringPointSheet ??= _sheetManager.GetSheet<GatheringPointSheet>();
    }

    private GatheringTypeSheet? gatheringTypeSheet;

    public GatheringTypeSheet GetGatheringTypeSheet()
    {
        return gatheringTypeSheet ??= _sheetManager.GetSheet<GatheringTypeSheet>();
    }

    private GCScripShopCategorySheet? gcScripShopCategorySheet;

    public GCScripShopCategorySheet GetGCScripShopCategorySheet()
    {
        return gcScripShopCategorySheet ??= _sheetManager.GetSheet<GCScripShopCategorySheet>();
    }

    private GCScripShopItemSheet? gcScripShopItemSheet;

    public GCScripShopItemSheet GetGCScripShopItemSheet()
    {
        return gcScripShopItemSheet ??= _sheetManager.GetSheet<GCScripShopItemSheet>();
    }

    private GCShopSheet? gcShopSheet;

    public GCShopSheet GetGCShopSheet()
    {
        return gcShopSheet ??= _sheetManager.GetSheet<GCShopSheet>();
    }

    private GilShopItemSheet? gilShopItemSheet;

    public GilShopItemSheet GetGilShopItemSheet()
    {
        return gilShopItemSheet ??= _sheetManager.GetSheet<GilShopItemSheet>();
    }

    private GilShopSheet? gilShopSheet;

    public GilShopSheet GetGilShopSheet()
    {
        return gilShopSheet ??= _sheetManager.GetSheet<GilShopSheet>();
    }

    private GrandCompanySheet? grandCompanySheet;

    public GrandCompanySheet GetGrandCompanySheet()
    {
        return grandCompanySheet ??= _sheetManager.GetSheet<GrandCompanySheet>();
    }

    private HWDGathererInspectionSheet? hwdGathererInspectionSheet;

    public HWDGathererInspectionSheet GetHWDGathererInspectionSheet()
    {
        return hwdGathererInspectionSheet ??= _sheetManager.GetSheet<HWDGathererInspectionSheet>();
    }

    private HWDCrafterSupplySheet? hwdCrafterSupplySheet;

    public HWDCrafterSupplySheet GetHWDCrafterSupplySheet()
    {
        return hwdCrafterSupplySheet ??= _sheetManager.GetSheet<HWDCrafterSupplySheet>();
    }

    private ItemSheet? itemSheet;

    public ItemSheet GetItemSheet()
    {
        return itemSheet ??= _sheetManager.GetSheet<ItemSheet>();
    }

    private LevelSheet? levelSheet;

    public LevelSheet GetLevelSheet()
    {
        return levelSheet ??= _sheetManager.GetSheet<LevelSheet>();
    }

    private MapSheet? mapSheet;

    public MapSheet GetMapSheet()
    {
        return mapSheet ??= _sheetManager.GetSheet<MapSheet>();
    }

    private PlaceNameSheet? placeNameSheet;

    public PlaceNameSheet GetPlaceNameSheet()
    {
        return placeNameSheet ??= _sheetManager.GetSheet<PlaceNameSheet>();
    }

    private RecipeSheet? recipeSheet;

    public RecipeSheet GetRecipeSheet()
    {
        return recipeSheet ??= _sheetManager.GetSheet<RecipeSheet>();
    }

    private RetainerTaskNormalSheet? retainerTaskNormalSheet;

    public RetainerTaskNormalSheet GetRetainerTaskNormalSheet()
    {
        return retainerTaskNormalSheet ??= _sheetManager.GetSheet<RetainerTaskNormalSheet>();
    }

    private RetainerTaskRandomSheet? retainerTaskRandomSheet;

    public RetainerTaskRandomSheet GetRetainerTaskRandomSheet()
    {
        return retainerTaskRandomSheet ??= _sheetManager.GetSheet<RetainerTaskRandomSheet>();
    }

    private RetainerTaskSheet? retainerTaskSheet;

    public RetainerTaskSheet GetRetainerTaskSheet()
    {
        return retainerTaskSheet ??= _sheetManager.GetSheet<RetainerTaskSheet>();
    }

    private SpecialShopSheet? specialShopSheet;

    public SpecialShopSheet GetSpecialShopSheet()
    {
        return specialShopSheet ??= _sheetManager.GetSheet<SpecialShopSheet>();
    }

    private SpearfishingItemSheet? spearfishingItemSheet;

    public SpearfishingItemSheet GetSpearfishingItemSheet()
    {
        return spearfishingItemSheet ??= _sheetManager.GetSheet<SpearfishingItemSheet>();
    }

    private SubmarineExplorationSheet? submarineExplorationSheet;

    public SubmarineExplorationSheet GetSubmarineExplorationSheet()
    {
        return submarineExplorationSheet ??= _sheetManager.GetSheet<SubmarineExplorationSheet>();
    }

    private CabinetCategorySheet? cabinetCategorySheet;

    public CabinetCategorySheet GetCabinetCategorySheet()
    {
        return cabinetCategorySheet ??= _sheetManager.GetSheet<CabinetCategorySheet>();
    }

    private CabinetSheet? cabinetSheet;

    public CabinetSheet GetCabinetSheet()
    {
        return cabinetSheet ??= _sheetManager.GetSheet<CabinetSheet>();
    }
}