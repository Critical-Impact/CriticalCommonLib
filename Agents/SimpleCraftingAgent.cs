using System;
using System.Linq;
using CriticalCommonLib.Addons;

namespace CriticalCommonLib.Agents
{
    //Not really an agent but seems a good as spot as any
    public unsafe class SimpleCraftingAgent
    {
        private const int OffsetCraftingAgent = 368;

        private const int OffsetNqTotal       = 88;
        private const int OffsetHqTotal     = 136;
        private const int OffsetCompleted = 56;
        private const int OffsetResultItemId   = 120;
        private const int OffsetTotal   = 72;

        public  AddonSimpleSynthesis* Pointer;
        private byte*           _agent;

        public byte* Agent
        {
            get
            {
                return _agent;
            }
        }


        public static implicit operator SimpleCraftingAgent(IntPtr ptr)
        {
            var ret = new SimpleCraftingAgent { Pointer = (AddonSimpleSynthesis*)ptr };
            if (ret)
                ret._agent = *(byte**)((byte*)ret.Pointer + OffsetCraftingAgent);

            return ret;
        }

        public static implicit operator bool(SimpleCraftingAgent ptr)
            => ptr.Pointer != null;


        public uint NqCompleted
            => _agent == null ? 0 : *(uint*)(_agent + OffsetNqTotal);

        public uint HqCompleted
            => _agent == null ? 0 : *(uint*)(_agent + OffsetHqTotal);

        public uint TotalCompleted
            => _agent == null ? 0 : *(uint*)(_agent + OffsetCompleted);

        public uint TotalFailed => TotalCompleted - NqCompleted - HqCompleted;

        public int Total
            => _agent == null ? 0 : *(int*)(_agent + OffsetTotal);

        public bool Finished => TotalCompleted == Total;

        public uint ResultItemId
            => _agent == null ? 0 : *(uint*)(_agent + OffsetResultItemId);

        public uint CraftType => (uint) (Service.ClientState.LocalPlayer?.ClassJob.GameData?.DohDolJobIndex ?? 0);
        
        public uint Recipe
        {
            get
            {
                if (Service.ExcelCache.GetRecipeExSheet()
                    .Any(c => c.CraftType.Row == CraftType && c.ItemResult.Row == ResultItemId))
                {
                    return Service.ExcelCache.GetRecipeExSheet()
                        .Single(c => c.CraftType.Row == CraftType && c.ItemResult.Row == ResultItemId).RowId;
                }

                return 0;
            }
        }
    }
}