using System.Collections.Generic;
using Newtonsoft.Json.Serialization;

namespace CriticalCommonLib.Resolvers
{
    public class MinifyResolver : DefaultContractResolver
    {
        private Dictionary<string, string> PropertyMappings { get; set; }
        
        public MinifyResolver()
        {
            this.PropertyMappings = new Dictionary<string, string> 
            {
                {"Container", "con"},
                {"Slot", "sl"},
                {"ItemId", "iid"},
                {"Spiritbond", "sb"},
                {"Condition", "cnd"},
                {"Quantity", "qty"},
                {"Stain", "stn"},
                {"Flags", "flgs"},
                {"Materia0", "mat0"},
                {"Materia1", "mat1"},
                {"Materia2", "mat2"},
                {"Materia3", "mat3"},
                {"Materia4", "mat4"},
                {"MateriaLevel0", "matl0"},
                {"MateriaLevel1", "matl1"},
                {"MateriaLevel2", "matl2"},
                {"MateriaLevel3", "matl3"},
                {"MateriaLevel4", "matl4"},
                {"SortedCategory", "soc"},
                {"SortedSlotIndex", "ssi"},
                {"SortedContainer", "sc"},
                {"RetainerId", "retid"},
                {"GlamourId", "glmid"},
            };
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            var resolved = PropertyMappings.TryGetValue(propertyName, out var resolvedName);
            return (resolved ? resolvedName : base.ResolvePropertyName(propertyName)) ?? propertyName;
        }
    }
}