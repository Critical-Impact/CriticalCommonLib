using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Enums;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class EquipSlotCategoryEx : EquipSlotCategory
    {
        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            Build();
        }

        private HashSet<EquipSlot> _possibleSlots = null!;

        public HashSet<EquipSlot> PossibleSlots
        {
            get => _possibleSlots;
        }

        public HashSet<EquipSlot> BlockedSlots
        {
            get => _blockedSlots;
        }

        private HashSet<EquipSlot>  _blockedSlots = null!;
        
        public bool IsPossibleSlot(EquipSlot slot)
        {
            return _possibleSlots.Contains(slot);
        }

        public bool IsBlockedSlot(EquipSlot slot)
        {
            return _blockedSlots.Contains(slot);
        }

        public bool SimilarSlots(ItemEx item)
        {
            return PossibleSlots.Any(c => item.EquipSlotCategoryEx?.PossibleSlots.Contains(c) ?? false);
        }

        private void Build() {
            var possible = new HashSet<EquipSlot>();
            var blocked = new HashSet<EquipSlot>();
            var equipSlots = new[] { EquipSlot.Body, EquipSlot.Ears, EquipSlot.Feet, EquipSlot.Gloves, EquipSlot.Head, EquipSlot.Legs, EquipSlot.Neck, EquipSlot.Waist, EquipSlot.Wrists, EquipSlot.FingerL, EquipSlot.FingerR, EquipSlot.MainHand, EquipSlot.OffHand, EquipSlot.SoulCrystal };
            foreach (var equipSlot in equipSlots)
            {
                var val = 0;
                switch (equipSlot)
                {
                    case EquipSlot.Body:
                        val = Body;
                        break;
                    case EquipSlot.Ears:
                        val = Ears;
                        break;
                    case EquipSlot.Feet:
                        val = Feet;
                        break;
                    case EquipSlot.Gloves:
                        val = Gloves;
                        break;
                    case EquipSlot.Head:
                        val = Head;
                        break;
                    case EquipSlot.Legs:
                        val = Legs;
                        break;
                    case EquipSlot.Neck:
                        val = Neck;
                        break;
                    case EquipSlot.Waist:
                        val = Waist;
                        break;
                    case EquipSlot.Wrists:
                        val = Wrists;
                        break;
                    case EquipSlot.FingerL:
                        val = FingerL;
                        break;
                    case EquipSlot.FingerR:
                        val = FingerR;
                        break;
                    case EquipSlot.MainHand:
                        val = MainHand;
                        break;
                    case EquipSlot.OffHand:
                        val = OffHand;
                        break;
                    case EquipSlot.SoulCrystal:
                        val = SoulCrystal;
                        break;
                }
                if (val > 0 && !possible.Contains(equipSlot))
                    possible.Add(equipSlot);
                else if (val < 0 && !blocked.Contains(equipSlot))
                    blocked.Add(equipSlot);
            }

            _possibleSlots = possible;
            _blockedSlots = blocked;
        }
    }
}