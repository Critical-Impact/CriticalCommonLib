using System;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Sheets;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace CriticalCommonLib.Models
{
    public class Character
    {
        public ulong CharacterId;
        public ulong FreeCompanyId;
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
        public uint GrandCompanyId = 0;
        private string? _freeCompanyName = "";
        public string? AlternativeName = null;
        public ulong OwnerId;
        public uint WorldId;
        public CharacterRace Race;
        public CharacterSex Gender;

        [JsonIgnore]
        public string FormattedName
        {
            get
            {
                return AlternativeName ?? Name;
            }
        }
        [JsonIgnore]
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
        [JsonIgnore]
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

        private CharacterType? _characterType;
        [JsonIgnore]
        public CharacterType CharacterType
        {
            get
            {
                if (_characterType == null)
                {
                    var characterIdString = CharacterId.ToString();
                    if (characterIdString.StartsWith("1"))
                    {
                        _characterType = CharacterType.Character;
                    }
                    else if (characterIdString.StartsWith("3"))
                    {
                        _characterType = CharacterType.Retainer;
                    }
                    else if (characterIdString.StartsWith("9"))
                    {
                        _characterType = CharacterType.FreeCompanyChest;
                    }
                    else
                    {
                        _characterType = CharacterType.Unknown;
                    }
                }

                return _characterType.Value;
            }
        }

        [JsonIgnore] public WorldEx? World => Service.ExcelCache.GetWorldSheet().GetRow(WorldId);
        
        [JsonIgnore]
        public ClassJob? ActualClassJob => Service.ExcelCache.GetClassJobSheet().GetRow(ClassJob);

        public string FreeCompanyName
        {
            get
            {
                if (_freeCompanyName == null)
                {
                    _freeCompanyName = "";
                }
                return _freeCompanyName;
            }
            set => _freeCompanyName = value;
        }

        public unsafe void UpdateFromCurrentPlayer(PlayerCharacter playerCharacter, InfoProxyFreeCompany* freeCompanyInfoProxy)
        {
            Name = playerCharacter.Name.ToString();
            Level = playerCharacter.Level;
            WorldId = playerCharacter.HomeWorld.Id;
            

            var characterRace = (CharacterRace)playerCharacter.Customize[(int)CustomizeIndex.Race];
            if (Race != characterRace)
            {
                Race = characterRace;
            }

            var characterGender = (CharacterSex)playerCharacter.Customize[(int)CustomizeIndex.Gender] == 0
                ? CharacterSex.Male
                : CharacterSex.Female;
            if (Gender != characterGender)
            {
                Gender = characterGender;
            }

            if (ClassJob != playerCharacter.ClassJob.Id)
            {
                ClassJob = (byte)playerCharacter.ClassJob.Id;
            }
            
            if (freeCompanyInfoProxy != null)
            {
                var freeCompanyId = freeCompanyInfoProxy->ID;
                var freeCompanyName = SeString.Parse(freeCompanyInfoProxy->Name, 22).TextValue;
                if (freeCompanyId != FreeCompanyId)
                {
                    FreeCompanyId = freeCompanyId;
                }

                if (freeCompanyName != FreeCompanyName)
                {
                    FreeCompanyName = freeCompanyName;
                }
            }
        }

        public unsafe bool UpdateFromInfoProxyFreeCompany(InfoProxyFreeCompany* infoProxyFreeCompany)
        {
            if (infoProxyFreeCompany == null)
            {
                return false;
            }

            var hasChanges = false;
            if (CharacterId != infoProxyFreeCompany->ID)
            {
                CharacterId = infoProxyFreeCompany->ID;
                hasChanges = true;
            }
            var freeCompanyName = SeString.Parse(infoProxyFreeCompany->Name, 22).TextValue.Replace("\u0000", "");
            freeCompanyName = freeCompanyName == "" ? "Unknown FC Name" : freeCompanyName;
            if (Name != freeCompanyName)
            {
                Name = freeCompanyName;
                hasChanges = true;
            }
            var grandCompany = (uint)infoProxyFreeCompany->GrandCompany;
            if (GrandCompanyId != grandCompany)
            {
                GrandCompanyId = grandCompany;
                hasChanges = true;
            }

            if (WorldId != infoProxyFreeCompany->HomeWorldID)
            {
                WorldId = infoProxyFreeCompany->HomeWorldID;
                hasChanges = true;
            }
            

            return hasChanges;
        }

        public unsafe bool UpdateFromRetainerInformation(RetainerManager.RetainerList.Retainer* retainerInformation, PlayerCharacter currentCharacter, int hireOrder)
        {
            if (retainerInformation == null)
            {
                return false;
            }
            var hasChanges = false;
            if (Gil != retainerInformation->Gil)
            {
                Gil = retainerInformation->Gil;
                hasChanges = true;
            }

            if (HireOrder != hireOrder)
            {
                HireOrder = hireOrder;
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

            if (WorldId != currentCharacter.HomeWorld.Id)
            {
                WorldId = currentCharacter.HomeWorld.Id;
                hasChanges = true;
            }

            return hasChanges;
        }
    }

    public enum CharacterType
    {
        Character,
        Retainer,
        FreeCompanyChest,
        Unknown,
    }
}