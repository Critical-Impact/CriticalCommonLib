using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;
using Dalamud.Configuration;
using Newtonsoft.Json.Serialization;

namespace CriticalCommonLib.Resolvers
{
    public class AutofacResolver : DefaultContractResolver
    {
        private readonly IComponentContext _componentContext;

        public AutofacResolver(IComponentContext componentContext)
        {
            _componentContext = componentContext;
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
    }
}