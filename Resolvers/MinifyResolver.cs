using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;
using Dalamud.Configuration;
using Newtonsoft.Json.Serialization;

namespace CriticalCommonLib.Resolvers
{
    public class MinifyResolver : DefaultContractResolver
    {
        private readonly IComponentContext _componentContext;

        private Dictionary<string, string> PropertyMappings { get; set; }

        public MinifyResolver(IComponentContext componentContext)
        {
            _componentContext = componentContext;
            this.PropertyMappings = new Dictionary<string, string>
            {
                {"Container", "con"},
                {"Slot", "sl"},
                {"ItemId", "iid"},
                {"Spiritbond", "sb"},
                {"Condition", "cnd"},
                {"Quantity", "qty"},
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

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            // use Autofac to create types that have been registered with it
            if (objectType.GetInterface(nameof(IPluginConfiguration)) == null && _componentContext.IsRegistered(objectType))
            {
                JsonObjectContract contract = ResolveContact(objectType);
                contract.DefaultCreator = () => _componentContext.Resolve(objectType);

                return contract;
            }

            return base.CreateObjectContract(objectType);
        }

        private JsonObjectContract ResolveContact(Type objectType)
        {
            IComponentRegistration registration;
            if (_componentContext.ComponentRegistry.TryGetRegistration(new TypedService(objectType), out registration))
            {
                Type viewType = (registration.Activator as ReflectionActivator)?.LimitType;
                if (viewType != null)
                {
                    return base.CreateObjectContract(viewType);
                }
            }

            return base.CreateObjectContract(objectType);
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            var resolved = PropertyMappings.TryGetValue(propertyName, out var resolvedName);
            return (resolved ? resolvedName : base.ResolvePropertyName(propertyName)) ?? propertyName;
        }
    }
}