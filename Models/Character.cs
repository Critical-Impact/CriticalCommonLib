using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;

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
        public ulong OwnerId;

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