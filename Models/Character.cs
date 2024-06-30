using System;
using System.Collections.Generic;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Sheets;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
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
        public uint GrandCompanyId;
        private string? _freeCompanyName = "";
        public string? AlternativeName = null;
        public ulong OwnerId;
        
        /// <summary>
        /// The home world ID 
        /// </summary>
        public uint WorldId;
        
        /// <summary>
        /// The active world ID(specifically for players)
        /// </summary>
        public uint ActiveWorldId;
        public CharacterRace Race;
        public CharacterSex Gender;
        private HashSet<ulong>? _owners;
        public ulong HouseId;
        public sbyte WardId;
        public sbyte PlotId;
        public byte DivisionId;
        public short RoomId;
        public uint ZoneId;
        public uint TerritoryTypeId;
        private string? _housingName;
        
        public HashSet<ulong> Owners
        {
            get
            {
                if (_owners == null)
                {
                    _owners = new HashSet<ulong>();
                }

                return _owners;
            }
            set => _owners = value;
        } 

        [JsonIgnore]
        public string FormattedName
        {
            get
            {
                if (CharacterType == CharacterType.Housing)
                {
                    return AlternativeName ?? HousingName;
                }

                if (Name.Trim().Length == 0)
                {
                    return AlternativeName ?? "Unknown";
                }

                return AlternativeName ?? Name;
            }
        }

        [JsonIgnore]
        public HousingZone HousingZone => (HousingZone)ZoneId;

        public PlotSize GetPlotSize() => Plots.GetSize(HousingZone, DivisionId, PlotId, RoomId);

        [JsonIgnore] public bool IsSubdivision => DivisionId == 2 || PlotId is >= 30 or -127;

        [JsonIgnore]
        public string HousingName
        {
            get
            {
                if (_housingName == null)
                {
                    var strings = new List<string>();
                    var ward = WardId + 1;
                    if (ward == 0)
                    {
                        _housingName = String.Empty;
                    }
                    else
                    {

                        var plot = PlotId;
                        var room = RoomId;
                        var division = DivisionId;

                        var zoneName = Territory?.PlaceNameZone.Value?.Name.ToDalamudString().ToString() ?? "Unknown";

                        strings.Add($"{zoneName} -");
                        strings.Add($"Ward {ward}");
                        if (division == 2 || plot is >= 30 or -127) strings.Add($"Subdivision");

                        switch (plot)
                        {
                            case < -1:
                                strings.Add($"Apartment {(room == 0 ? $"Lobby" : $"{room}")}");
                                break;
                            case > -1:
                            {
                                strings.Add($"Plot {plot + 1}");
                                if (room > 0)
                                {
                                    strings.Add($"Room {room}");
                                }

                                break;
                            }
                        }


                        _housingName = string.Join(" ", strings);
                    }
                }

                return _housingName;
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
                    if (HouseId != 0)
                    {
                        _characterType = CharacterType.Housing;
                    }
                    else if (characterIdString.StartsWith("1"))
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
        [JsonIgnore] public WorldEx? ActiveWorld => Service.ExcelCache.GetWorldSheet().GetRow(ActiveWorldId);
        
        [JsonIgnore]
        public ClassJobEx? ActualClassJob => Service.ExcelCache.GetClassJobSheet().GetRow(ClassJob);
        
        [JsonIgnore]
        public TerritoryTypeEx? Territory => Service.ExcelCache.GetTerritoryTypeExSheet().GetRow(TerritoryTypeId);

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

        public unsafe bool UpdateFromCurrentPlayer(IPlayerCharacter playerCharacter, InfoProxyFreeCompany* freeCompanyInfoProxy)
        {
            var hasChanges = false;
            if (playerCharacter.Name.ToString() != Name)
            {
                Name = playerCharacter.Name.ToString();
                hasChanges = true;
            }

            if (playerCharacter.Level != Level)
            {
                Level = playerCharacter.Level;
                hasChanges = true;
            }

            if (playerCharacter.HomeWorld.Id != 0)
            {
                if (playerCharacter.HomeWorld.Id != WorldId)
                {
                    WorldId = playerCharacter.HomeWorld.Id;
                    hasChanges = true;
                }
            }
            if (playerCharacter.CurrentWorld.Id != 0)
            {
                if (playerCharacter.CurrentWorld.Id != ActiveWorldId)
                {
                    ActiveWorldId = playerCharacter.CurrentWorld.Id;
                    hasChanges = true;
                }
            }
            
            var characterRace = (CharacterRace)playerCharacter.Customize[(int)CustomizeIndex.Race];
            if (Race != characterRace)
            {
                Race = characterRace;
                hasChanges = true;
            }

            var characterGender = (CharacterSex)playerCharacter.Customize[(int)CustomizeIndex.Gender] == 0
                ? CharacterSex.Male
                : CharacterSex.Female;
            if (Gender != characterGender)
            {
                Gender = characterGender;
                hasChanges = true;
            }

            if (ClassJob != playerCharacter.ClassJob.Id)
            {
                ClassJob = (byte)playerCharacter.ClassJob.Id;
                hasChanges = true;
            }
            
            if (freeCompanyInfoProxy != null)
            {
                var freeCompanyId = freeCompanyInfoProxy->Id;
                var freeCompanyName = freeCompanyInfoProxy->NameString;
                if (freeCompanyId != FreeCompanyId)
                {
                    FreeCompanyId = freeCompanyId;
                    hasChanges = true;
                }

                if (freeCompanyName != FreeCompanyName)
                {
                    if (freeCompanyId == 0 && freeCompanyName == "" && FreeCompanyName != "")
                    {
                        FreeCompanyName = freeCompanyName;
                        hasChanges = true;
                    }
                    else if (freeCompanyName != FreeCompanyName && freeCompanyId != 0 && freeCompanyName != "")
                    {
                        FreeCompanyName = freeCompanyName;
                        hasChanges = true;
                    }
                }
            }

            return hasChanges;
        }

        public unsafe bool UpdateFromInfoProxyFreeCompany(InfoProxyFreeCompany* infoProxyFreeCompany)
        {
            if (infoProxyFreeCompany == null)
            {
                return false;
            }

            var hasChanges = false;
            if (CharacterId != infoProxyFreeCompany->Id)
            {
                CharacterId = infoProxyFreeCompany->Id;
                hasChanges = true;
            }
            var freeCompanyName = infoProxyFreeCompany->NameString.Trim();

            if (freeCompanyName == "")
            {
                if (Name == "")
                {
                    freeCompanyName = "Unknown FC Name";
                }
                else
                {
                    freeCompanyName = Name;
                }
            }
                
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

            if (WorldId != infoProxyFreeCompany->HomeWorldId)
            {
                WorldId = infoProxyFreeCompany->HomeWorldId;
                hasChanges = true;
            }
            

            return hasChanges;
        }

        public unsafe bool UpdateFromRetainerInformation(RetainerManager.Retainer retainerInformation, IPlayerCharacter currentCharacter, int hireOrder)
        {
            var hasChanges = false;
            if (Gil != retainerInformation.Gil)
            {
                Gil = retainerInformation.Gil;
                hasChanges = true;
            }

            if (HireOrder != hireOrder)
            {
                HireOrder = hireOrder;
                hasChanges = true;
            }
            if (Level != retainerInformation.Level)
            {
                Level = retainerInformation.Level;
                hasChanges = true;
            }
            if (CityId != (byte)retainerInformation.Town)
            {
                CityId = (byte)retainerInformation.Town;
                hasChanges = true;
            }
            if (ClassJob != retainerInformation.ClassJob)
            {
                ClassJob = retainerInformation.ClassJob;
                hasChanges = true;
            }
            if (ItemCount != retainerInformation.ItemCount)
            {
                ItemCount = retainerInformation.ItemCount;
                hasChanges = true;
            }
            if (CharacterId != retainerInformation.RetainerId)
            {
                CharacterId = retainerInformation.RetainerId;
                hasChanges = true;
            }
            var retainerName = retainerInformation.NameString.Trim();
            if (Name != retainerName)
            {
                Name = retainerName;
                hasChanges = true;
            }
            if (RetainerTask != retainerInformation.VentureId)
            {
                RetainerTask = retainerInformation.VentureId;
                hasChanges = true;
            }
            if (SellingCount != retainerInformation.MarketItemCount)
            {
                SellingCount = retainerInformation.MarketItemCount;
                hasChanges = true;
            }
            if (RetainerTaskComplete != retainerInformation.VentureComplete)
            {
                RetainerTaskComplete = retainerInformation.VentureComplete;
                hasChanges = true;
            }

            if (WorldId != currentCharacter.HomeWorld.Id)
            {
                WorldId = currentCharacter.HomeWorld.Id;
                hasChanges = true;
            }

            return hasChanges;
        }

        public unsafe bool UpdateFromCurrentHouse(HousingManager* housingManager,
            IPlayerCharacter currentCharacter, uint zoneId, uint territoryTypeId)
        {
            if (housingManager == null)
            {
                return false;
            }

            var divisionId = (byte)(housingManager->GetCurrentPlot() >= 30 ? 2 : housingManager->GetCurrentDivision());
            var plotId = housingManager->GetCurrentPlot();
            var roomId = housingManager->GetCurrentRoom();
            var wardId = housingManager->GetCurrentWard();
            var worldId = currentCharacter.HomeWorld.Id;
            byte sb1 = (byte)wardId;
            byte sb2 = (byte)plotId;
            ushort sh1 = (ushort)roomId;
            ushort sh2 = (ushort)worldId;
            ushort sh3 = (ushort)zoneId;
            var houseId = ((ulong)sb1 << 56) | ((ulong)sb2 << 48) | ((ulong)sh1 << 32) | ((ulong)sh2 << 16) | sh3;

            var hasChanges = false;
            
            if (divisionId != DivisionId)
            {
                DivisionId = divisionId;
                hasChanges = true;
            }
            
            if (plotId != PlotId)
            {
                PlotId = plotId;
                hasChanges = true;
            }
            
            if (roomId != RoomId)
            {
                RoomId = roomId;
                hasChanges = true;
            }
            
            if (wardId != WardId)
            {
                WardId = wardId;
                hasChanges = true;
            }

            if (worldId != WorldId)
            {
                WorldId = worldId;
                hasChanges = true;
            }

            if (houseId != HouseId)
            {
                HouseId = houseId;
                hasChanges = true;
            }

            if (zoneId != ZoneId)
            {
                ZoneId = zoneId;
                hasChanges = true;
            }

            if (territoryTypeId != TerritoryTypeId)
            {
                TerritoryTypeId = territoryTypeId;
                hasChanges = true;
            }

            if (!Owners.Contains(Service.ClientState.LocalContentId))
            {
                Owners.Add(Service.ClientState.LocalContentId);
            }

            return hasChanges;
        }

        [JsonIgnore]
        public int Icon
        {
            get
            {
                switch (CharacterType)
                {
                    case CharacterType.Character:
                    case CharacterType.Retainer:
                        return ActualClassJob?.Icon ?? 0;
                    case CharacterType.Housing:
                        switch (GetPlotSize())
                        {
                            case PlotSize.Cottage:
                                return 60751;
                            case PlotSize.House:
                                return 60752;
                            case PlotSize.Mansion:
                                return 60753;
                            case PlotSize.Apartment:
                                return 60789;
                            case PlotSize.Room:
                                return 60771;
                        }

                        break;
                    case CharacterType.FreeCompanyChest:
                        return 52478;
                }

                return 0;
            }
        }
    }

    public enum CharacterType
    {
        Character,
        Retainer,
        FreeCompanyChest,
        Housing,
        Unknown,
        Orphaned
    }
}