using System;
using System.Globalization;
using System.Linq;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;
using Lumina;
using Lumina.Data;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Models;

public class InventoryChange : ICsv
{
    public InventoryChange()
    {
        
    }
    public InventoryChange(InventoryItem? fromItem, InventoryItem? toItem, InventoryType inventoryType, bool firstLoad)
    {
        FromItem = fromItem;
        ToItem = toItem;
        InventoryType = inventoryType;
        FirstLoad = firstLoad;
    }
    public InventoryChange(InventoryItem? fromItem, InventoryItem? toItem, InventoryChangeReason inventoryChangeReason, uint changeSetId)
    {
        FromItem = fromItem;
        ToItem = toItem;
        InventoryChangeReason = inventoryChangeReason;
        ChangeDate = DateTime.Now;
        ChangeSetId = changeSetId;
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

            return new InventoryItem();
        }
    }

    public string GetFormattedChange()
    {
        if (_formattedChange == null)
        {
            switch (InventoryChangeReason)
            {
                case InventoryChangeReason.Added:
                {
                    if (ToItem != null)
                    {
                        _formattedChange = "Added " + ToItem.Quantity;
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
                        _formattedChange = "Moved " + quantity;
                    }

                    break;
                }
                case InventoryChangeReason.Removed:
                {
                    if (FromItem != null)
                    {
                        _formattedChange = "Removed " + FromItem.Quantity;
                    }

                    break;
                }
                case InventoryChangeReason.Moved:
                {
                    if (ToItem != null && FromItem != null)
                    {
                        _formattedChange = "Moved " + ToItem.FormattedName + " from " + FromItem.FormattedBagLocation + " to " + ToItem.FormattedBagLocation;
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
                            _formattedChange = "Lost " + lost + " " + FromItem.FormattedName;
                        }
                        else
                        {
                            var gained = ToItem.Quantity - FromItem.Quantity;
                            _formattedChange = "Gained " + gained + " " + FromItem.FormattedName;
                        }
                    }

                    break;
                }
                default:
                {
                    if (ToItem != null && FromItem != null)
                    {
                        _formattedChange = "Modified " + ToItem.FormattedName + " because " + InventoryChangeReason.FormattedName();
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
                var inventoryItem = new InventoryItem();
                inventoryItem.FromCsv(lineData.Skip(6).ToArray());
                ToItem = inventoryItem;
            }
        }
        else
        {
            var fromItem = new InventoryItem();
            fromItem.FromCsv(lineData.Skip(5).ToArray());
            FromItem = fromItem;

            var toItemStart = fromItem.ToCsv().Length + 5;
            var toItemString = lineData[toItemStart];
            if (toItemString != "null")
            {
                var toItem = new InventoryItem();
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

    public void PopulateData(GameData gameData, Language language)
    {
         
    }
}