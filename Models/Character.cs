using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
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

        public void UpdateFromNetworkRetainerInformation(NetworkRetainerInformation networkRetainerInformation)
        {
            Gil = networkRetainerInformation.gil;
            Level = networkRetainerInformation.level;
            CityId = networkRetainerInformation.cityId;
            ClassJob = networkRetainerInformation.classJob;
            HireOrder = networkRetainerInformation.hireOrder;
            ItemCount = networkRetainerInformation.itemCount;
            CharacterId = networkRetainerInformation.retainerId;
            Name = SeString.Parse(networkRetainerInformation.retainerName).ToString().Trim().Replace("\0", string.Empty);
            RetainerTask = networkRetainerInformation.retainerTask;
            SellingCount = networkRetainerInformation.sellingCount;
            RetainerTaskComplete = networkRetainerInformation.retainerTaskComplete;
        }
    }
}