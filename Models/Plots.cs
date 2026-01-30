namespace CriticalCommonLib.Models
{
    public enum PlotSize : byte
    {
        Cottage,
        House,
        Mansion,
        Apartment,
        Room,
        Unknown
    }

    public enum HousingZone : int
    {
        Unknown      = 0,
        Mist         = 502,
        Goblet       = 505,
        LavenderBeds = 507,
        Firmament    = 512,
        Shirogane    = 513,
    }

    public static class Plots
    {
        private static readonly PlotSize[] MistData =
        {
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.House,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.House,
        };

        private static readonly PlotSize[] LavenderBedsData =
        {
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.House,
        };

        private static readonly PlotSize[] GobletData =
        {
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Mansion,
        };

        private static readonly PlotSize[] ShiroganeData =
        {
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Mansion,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Mansion,
        };

        private static readonly PlotSize[] FirmamentData =
        {
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Mansion,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.House,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Cottage,
            PlotSize.Mansion
        };

        public static PlotSize GetSize(HousingZone zone,byte division, sbyte plot, short roomId)
        {
            if (plot < 0)
            {
                return PlotSize.Apartment;
            }

            if (roomId != 0)
            {
                return PlotSize.Room;
            }

            plot = (sbyte)(plot - (division == 2 ? 30 : 0));
            if (plot is >= 30 or < 0)
            {
                return PlotSize.Unknown;
            }
            return zone switch
            {
                HousingZone.Mist         => MistData[plot],
                HousingZone.Goblet       => GobletData[plot],
                HousingZone.LavenderBeds => LavenderBedsData[plot],
                HousingZone.Shirogane    => ShiroganeData[plot],
                HousingZone.Firmament    => FirmamentData[plot],
                _                        => PlotSize.Unknown
            };
        }

        public static short GetExternalSlots(this PlotSize zone)
        {
            return zone switch
            {
                PlotSize.Cottage => 20,
                PlotSize.House => 30,
                PlotSize.Mansion => 40,
                _ => 0
            };
        }

        public static short GetInternalSlots(this PlotSize zone)
        {
            return zone switch
            {
                PlotSize.Cottage => 200,
                PlotSize.House => 300,
                PlotSize.Mansion => 400,
                PlotSize.Apartment => 100,
                PlotSize.Room => 100,
                _ => 0
            };
        }
    }
}
