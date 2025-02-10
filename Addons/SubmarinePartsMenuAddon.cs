using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace CriticalCommonLib.Addons
{
    [StructLayout(LayoutKind.Explicit, Size = 545)]

    public unsafe struct SubmarinePartsMenuAddon
    {
        [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

        public uint Phase
        {
            get
            {
                var textValue = this.AtkUnitBase.AtkValues[8].GetValueAsString();
                var values = textValue.Split('/', StringSplitOptions.TrimEntries);
                if (values.Length > 1)
                {
                    if (uint.TryParse(values[0], out uint result))
                    {
                        return result;
                    }
                }

                return 0;
            }
        }

        public uint ConstructionQuality
        {
            get
            {
                var textValue = this.AtkUnitBase.AtkValues[5].GetValueAsString();
                var values = textValue.Split('/', StringSplitOptions.TrimEntries);
                if (values.Length > 1)
                {
                    if (uint.TryParse(values[0], out uint result))
                    {
                        return result;
                    }
                }

                return 0;
            }
        }

        public SubmarinePartMenuItem? GetItem(byte index)
        {
            var itemId = 12 + index;
            if (this.AtkUnitBase.AtkValues[itemId].Type == ValueType.UInt)
            {
                var qtyPerSet = 60 + index;
                var setsSubmitted = 108 + index;
                var setsRequired = 120 + index;
                return new SubmarinePartMenuItem()
                {
                    ItemId = this.AtkUnitBase.AtkValues[itemId].UInt,
                    QtyPerSet = this.AtkUnitBase.AtkValues[qtyPerSet].UInt,
                    SetsSubmitted = this.AtkUnitBase.AtkValues[setsSubmitted].UInt,
                    SetsRequired = this.AtkUnitBase.AtkValues[setsRequired].UInt,
                };
            }

            return null;
        }
    }
}