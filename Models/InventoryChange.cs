using System;
using System.Globalization;
using System.Linq;
using AllaganLib.GameSheets.Sheets.Rows;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Interfaces;

using Lumina;
using Lumina.Data;
using Lumina.Excel;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Models;

public class InventoryChange : ICsv, IItem
{
    private readonly InventoryItem.Factory _inventoryItemFactory;

    public delegate InventoryChange FromGameItemFactory(InventoryItem? fromItem, InventoryItem? toItem, InventoryType inventoryType, bool firstLoad);
    public delegate InventoryChange FromProcessedChangeFactory(InventoryItem? fromItem, InventoryItem? toItem, InventoryChangeReason inventoryChangeReason, uint changeSetId);

    public InventoryChange(InventoryItem.Factory inventoryItemFactory, InventoryItem? fromItem, InventoryItem? toItem, InventoryType inventoryType, bool firstLoad)
    {
        _inventoryItemFactory = inventoryItemFactory;
        FromItem = fromItem;
        ToItem = toItem;
        InventoryType = inventoryType;
        FirstLoad = firstLoad;
    }
    public InventoryChange(InventoryItem.Factory inventoryItemFactory, InventoryItem? fromItem, InventoryItem? toItem, InventoryChangeReason inventoryChangeReason, uint changeSetId)
    {
        _inventoryItemFactory = inventoryItemFactory;
        FromItem = fromItem;
        ToItem = toItem;
        InventoryChangeReason = inventoryChangeReason;
        ChangeDate = DateTime.Now;
        ChangeSetId = changeSetId;
    }
    
    public InventoryChange(InventoryItem.Factory inventoryItemFactory)
    {
        _inventoryItemFactory = inventoryItemFactory;
    }

    public bool FirstLoad { get; private set; }
    public InventoryType InventoryType { get;private set; }
    public InventoryItem? FromItem { get;private set; }
    public InventoryItem? ToItem { get;private set; }
    public InventoryChangeReason InventoryChangeReason { get;private set; }
    public DateTime? ChangeDate { get;private set; }
    public uint ChangeSetId { get;private set; }

    private string? _formattedChange;

    public InventoryItem InventoryItem
    {
        get
        {
            var fromItem = FromItem;
            var toItem = ToItem;

            if (fromItem != null && toItem != null)
            {
                if (fromItem.ItemId != 0)
                {
                    return fromItem;
                }

                if (toItem.ItemId != 0)
                {
                    return toItem;
                }
            }

            if (fromItem == null && toItem != null)
            {
                return toItem;
            }

            if (toItem == null && fromItem != null)
            {
                return fromItem;
            }

            if (fromItem != null && fromItem.ItemId != 0)
            {
                return fromItem;
            }

            if (toItem != null && toItem.ItemId != 0)
            {
                return toItem;
            }

            return _inventoryItemFactory.Invoke();
        }
    }

    public string GetFormattedChange()
    {
        _formattedChange = null;
        if (_formattedChange == null)
        {
            switch (InventoryChangeReason)
            {
                case InventoryChangeReason.Added:
                {
                    if (ToItem != null)
                    {
                        _formattedChange = "Gained";
                    }

                    break;
                }
                case InventoryChangeReason.Transferred:
                {
                    if (FromItem != null && ToItem != null)
                    {
                        _formattedChange = "Moved";
                    }

                    break;
                }
                case InventoryChangeReason.Removed:
                {
                    if (FromItem != null)
                    {
                        _formattedChange = "Lost";
                    }

                    break;
                }
                case InventoryChangeReason.Moved:
                {
                    if (ToItem != null && FromItem != null)
                    {
                        _formattedChange = "Moved";
                    }

                    break;
                }
                case InventoryChangeReason.QuantityChanged:
                {
                    if (FromItem != null && ToItem != null)
                    {
                        if (FromItem.Quantity > ToItem.Quantity)
                        {
                            var lost = FromItem.Quantity - ToItem.Quantity;
                            _formattedChange = "Lost";
                        }
                        else
                        {
                            var gained = ToItem.Quantity - FromItem.Quantity;
                            _formattedChange = "Gained";
                        }
                    }

                    break;
                }
                case InventoryChangeReason.MarketPriceChanged:
                {
                    if (FromItem != null && ToItem != null)
                    {
                        if (FromItem.RetainerMarketPrice != ToItem.RetainerMarketPrice)
                        {
                            _formattedChange = "Market Price Updated";
                        }
                    }

                    break;
                }
                case InventoryChangeReason.GearsetsChanged:
                {
                    if (FromItem != null && ToItem != null)
                    {
                        _formattedChange = "Gearsets Changed";
                    }

                    break;
                }
                case InventoryChangeReason.ConditionChanged:
                {
                    if (ToItem != null && FromItem != null)
                    {
                        _formattedChange = "Condition Changed";
                    }

                    break;
                }
                case InventoryChangeReason.SpiritbondChanged:
                {
                    if (ToItem != null && FromItem != null)
                    {
                        _formattedChange = "Spiritbond Changed";
                    }

                    break;
                }
                default:
                {
                    if (ToItem != null && FromItem != null)
                    {
                        _formattedChange = "Modified";
                    }

                    break;
                }
            }

            if (_formattedChange == null)
            {
                _formattedChange = "Unknown";
            }
        }

        return _formattedChange;
    }

    private int? _formattedAmount;
    public int GetFormattedAmount()
    {
        _formattedAmount = null;
        if (_formattedAmount == null)
        {
            switch (InventoryChangeReason)
            {
                case InventoryChangeReason.Added:
                {
                    if (ToItem != null)
                    {
                        _formattedAmount = (int?)ToItem.Quantity;
                    }

                    break;
                }
                case InventoryChangeReason.Transferred:
                {
                    if (FromItem != null && ToItem != null)
                    {
                        var quantity = 0u;
                        if (FromItem.ItemId != 0 && ToItem.ItemId != 0)
                        {
                            if (FromItem.Quantity == ToItem.Quantity)
                            {
                                quantity = FromItem.Quantity;
                            }
                            else
                            {
                                quantity = (uint)Math.Abs((int)FromItem.Quantity - (int)ToItem.Quantity);
                            }
                        }
                        else if (ToItem.ItemId == 0)
                        {
                            quantity = FromItem.Quantity;
                        }
                        else if (FromItem.ItemId == 0)
                        {
                            quantity = ToItem.Quantity;
                        }
                        _formattedAmount = (int?)quantity;
                    }

                    break;
                }
                case InventoryChangeReason.Removed:
                {
                    if (FromItem != null)
                    {
                        _formattedAmount = -(int?)FromItem.Quantity;
                    }

                    break;
                }
                case InventoryChangeReason.Moved:
                {
                    if (ToItem != null && FromItem != null)
                    {
                        _formattedAmount = 0;
                    }

                    break;
                }
                case InventoryChangeReason.QuantityChanged:
                {
                    if (FromItem != null && ToItem != null)
                    {
                        if (FromItem.Quantity > ToItem.Quantity)
                        {
                            var lost = FromItem.Quantity - ToItem.Quantity;
                            _formattedAmount = -(int?)lost;
                        }
                        else
                        {
                            var gained = ToItem.Quantity - FromItem.Quantity;
                            _formattedAmount = (int?)gained;
                        }
                    }

                    break;
                }
                case InventoryChangeReason.MarketPriceChanged:
                {
                    if (FromItem != null && ToItem != null)
                    {
                        if (FromItem.RetainerMarketPrice != ToItem.RetainerMarketPrice)
                        {
                            _formattedAmount = (int?)ToItem.RetainerMarketPrice;
                        }
                    }

                    break;
                }
                case InventoryChangeReason.GearsetsChanged:
                {
                    if (FromItem != null && ToItem != null)
                    {
                        _formattedAmount = 0;
                    }

                    break;
                }
                case InventoryChangeReason.ConditionChanged:
                {
                    if (FromItem != null && ToItem != null)
                    {
                        _formattedAmount = FromItem.Condition - ToItem.Condition;
                    }

                    break;
                }
                case InventoryChangeReason.SpiritbondChanged:
                {
                    if (FromItem != null && ToItem != null)
                    {
                        _formattedAmount = FromItem.Spiritbond - ToItem.Spiritbond;
                    }

                    break;
                }
                default:
                {
                    if (ToItem != null && FromItem != null)
                    {
                        _formattedAmount = 0;
                    }

                    break;
                }
            }

            if (_formattedAmount == null)
            {
                _formattedAmount = 0;
            }
        }

        return _formattedAmount.Value;
    }

    public void FromCsv(string[] lineData)
    {
        FirstLoad = lineData[0] == "Y";
        InventoryType = Enum.Parse<InventoryType>(lineData[1]);
        InventoryChangeReason = Enum.Parse<InventoryChangeReason>(lineData[2]);
        ChangeDate = lineData[3] != "" ? DateTime.Parse(lineData[3], CultureInfo.InvariantCulture) : null;
        ChangeSetId = UInt32.Parse(lineData[4]);
        if (lineData[5] == "null")
        {
            if(lineData[6] != "null")
            {
                var inventoryItem = _inventoryItemFactory.Invoke();
                inventoryItem.FromCsv(lineData.Skip(6).ToArray());
                ToItem = inventoryItem;
            }
        }
        else
        {
            var fromItem = _inventoryItemFactory.Invoke();
            fromItem.FromCsv(lineData.Skip(5).ToArray());
            FromItem = fromItem;

            var toItemStart = fromItem.ToCsv().Length + 5;
            var toItemString = lineData[toItemStart];
            if (toItemString != "null")
            {
                var toItem = _inventoryItemFactory.Invoke();
                toItem.FromCsv(lineData.Skip(toItemStart).ToArray());
                ToItem = toItem;
            }
        }
    }

    public string[] ToCsv()
    {
        string[] strings = new[]
        {
            FirstLoad ? "Y" : "N",
            ((int)InventoryType).ToString(),
            ((int)InventoryChangeReason).ToString(),
            ChangeDate?.ToString(CultureInfo.InvariantCulture) ?? "",
            ChangeSetId.ToString()
        };
        return strings.Concat(FromItem != null ? FromItem.ToCsv() : new []{"null"}).Concat(ToItem != null ? ToItem.ToCsv() : new []{"null"}).ToArray();
    }

    public bool IncludeInCsv()
    {
        return true;
    }

    public void PopulateData(ExcelModule excelModule, Language language)
    {

    }

    public uint ItemId
    {
        get => InventoryItem.ItemId;
        set
        {

        }
    }

    public ItemRow Item => InventoryItem.Item;
}