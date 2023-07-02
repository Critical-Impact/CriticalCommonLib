using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CriticalCommonLib.Extensions;

public static class TypeLoaderExtensions {
    public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly) {
        if (assembly == null) throw new ArgumentNullException("assembly");
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.Where(t => t != null).Select(c => c!);
        }
    }
}