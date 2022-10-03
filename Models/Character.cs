using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace CriticalCommonLib.Models
{
    public class Character
    {
        public ulong CharacterId;
        public int HireOrder;
        public int ItemCount;
        public uint Gil;
        public uint SellingCount;
        public Byte CityId;
        public Byte ClassJob;
        public uint Level;
        public uint RetainerTask;
        public uint RetainerTaskComplete;
        public string Name = "";
        public string? AlternativeName = null;
        public ulong OwnerId;

        [JsonIgnore]
        public string FormattedName
        {
            get
            {
                return AlternativeName ?? Name;
            }
        }

        public string NameWithClass
        {
            get
            {
                if (ActualClassJob != null)
                {
                    return FormattedName + " (" + ActualClassJob.Name + ")";
                }

                return FormattedName;
            }
        }

        public string NameWithClassAbv
        {
            get
            {
                if (ActualClassJob != null)
                {
                    return FormattedName + " (" + ActualClassJob.Abbreviation + ")";
                }

                return FormattedName;
            }
        }
        
        [JsonIgnore]
        public ClassJob? ActualClassJob => Service.ExcelCache.GetClassJobSheet().GetRow(ClassJob);

        public void UpdateFromCurrentPlayer(PlayerCharacter playerCharacter)
        {
            Name = playerCharacter.Name.ToString();
            Level = playerCharacter.Level;
        }

        public unsafe bool UpdateFromRetainerInformation(RetainerManager.RetainerList.Retainer* retainerInformation)
        {
            var hasChanges = false;
            if (Gil != retainerInformation->Gil)
            {
                Gil = retainerInformation->Gil;
                hasChanges = true;
            }
            if (Level != retainerInformation->Level)
            {
                Level = retainerInformation->Level;
                hasChanges = true;
            }
            if (CityId != (byte)retainerInformation->Town)
            {
                CityId = (byte)retainerInformation->Town;
                hasChanges = true;
            }
            if (ClassJob != retainerInformation->ClassJob)
            {
                ClassJob = retainerInformation->ClassJob;
                hasChanges = true;
            }
            if (ItemCount != retainerInformation->ItemCount)
            {
                ItemCount = retainerInformation->ItemCount;
                hasChanges = true;
            }
            if (CharacterId != retainerInformation->RetainerID)
            {
                CharacterId = retainerInformation->RetainerID;
                hasChanges = true;
            }
            var retainerName = MemoryHelper.ReadSeStringNullTerminated((IntPtr)retainerInformation->Name).ToString().Trim();
            if (Name != retainerName)
            {
                Name = retainerName;
                hasChanges = true;
            }
            if (RetainerTask != retainerInformation->VentureID)
            {
                RetainerTask = retainerInformation->VentureID;
                hasChanges = true;
            }
            if (SellingCount != retainerInformation->MarkerItemCount)
            {
                SellingCount = retainerInformation->MarkerItemCount;
                hasChanges = true;
            }
            if (RetainerTaskComplete != retainerInformation->VentureComplete)
            {
                RetainerTaskComplete = retainerInformation->VentureComplete;
                hasChanges = true;
            }

            return hasChanges;
        }
    }
}