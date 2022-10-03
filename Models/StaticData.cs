using System.Collections.Generic;

namespace CriticalCommonLib.Models
{
    public static class StaticData
    {
        public static HashSet<uint> NoModelCategories = new()
        {
            33, // Fishing Tackle
            39, // Waist
            62 // Job Soul
        };
        
        
        public static readonly Dictionary<int, ModelHelper?> ModelHelpers = new Dictionary<int, ModelHelper?> {
            {  // Main hand
                0,
                new ModelHelper(
                   "chara/weapon/w{0:D4}/obj/body/b{1:D4}/b{1:D4}.imc",
                   0,
                   "chara/weapon/w{0:D4}/obj/body/b{1:D4}/model/w{0:D4}b{1:D4}.mdl",
                   2)
            },
            {  // Off hand
                1,
                new ModelHelper(
                   "chara/weapon/w{0:D4}/obj/body/b{1:D4}/b{1:D4}.imc",
                   0,
                   "chara/weapon/w{0:D4}/obj/body/b{1:D4}/model/w{0:D4}b{1:D4}.mdl",
                   2)
            },
            {  // Head
                2,
                new ModelHelper(
                   "chara/equipment/e{0:D4}/e{0:D4}.imc",
                   0,
                   "chara/equipment/e{0:D4}/model/c{4:D4}e{0:D4}_met.mdl",
                   1)
            },
            {  // Body
                3,
                new ModelHelper(
                   "chara/equipment/e{0:D4}/e{0:D4}.imc",
                   1,
                   "chara/equipment/e{0:D4}/model/c{4:D4}e{0:D4}_top.mdl",
                   1)
            },
            {  // Hands
                4,
                new ModelHelper(
                   "chara/equipment/e{0:D4}/e{0:D4}.imc",
                   2,
                   "chara/equipment/e{0:D4}/model/c{4:D4}e{0:D4}_glv.mdl",
                   1)
            },
            {  // Waist
                5,
                null
            },
            {  // Legs
                6,
                new ModelHelper(
                   "chara/equipment/e{0:D4}/e{0:D4}.imc",
                   3,
                   "chara/equipment/e{0:D4}/model/c{4:D4}e{0:D4}_dwn.mdl",
                   1)
            },
            {  // Feet
                7,
                new ModelHelper(
                   "chara/equipment/e{0:D4}/e{0:D4}.imc",
                   4,
                   "chara/equipment/e{0:D4}/model/c{4:D4}e{0:D4}_sho.mdl",
                   1)
            },
            {  // Ears
                8,
                new ModelHelper(
                   "chara/accessory/a{0:D4}/a{0:D4}.imc",
                   0,
                   "chara/accessory/a{0:D4}/model/c{4:D4}a{0:D4}_ear.mdl",
                   1)
            },
            {  // Neck
                9,
                new ModelHelper(
                   "chara/accessory/a{0:D4}/a{0:D4}.imc",
                   1,
                   "chara/accessory/a{0:D4}/model/c{4:D4}a{0:D4}_nek.mdl",
                   1)
            },
            {  // Wrists
                10,
                new ModelHelper(
                   "chara/accessory/a{0:D4}/a{0:D4}.imc",
                   2,
                   "chara/accessory/a{0:D4}/model/c{4:D4}a{0:D4}_wrs.mdl",
                   1)
            },
            {  // R.Ring
                11,
                new ModelHelper(
                   "chara/accessory/a{0:D4}/a{0:D4}.imc",
                   3,
                   "chara/accessory/a{0:D4}/model/c{4:D4}a{0:D4}_rir.mdl",
                   1)
            },
            {  // L.Ring
                12,
                new ModelHelper(
                   "chara/accessory/a{0:D4}/a{0:D4}.imc",
                   4,
                   "chara/accessory/a{0:D4}/model/c{4:D4}a{0:D4}_ril.mdl",
                   1)
            },
            {  // Soul Crystal
                13,
                null
            }
        };
    }
}