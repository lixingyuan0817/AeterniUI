using System.Collections.Concurrent;
using System.Reflection;
using AeterniUI.Attributes;

namespace AeterniUI.Modules;

internal sealed record JsComponentMetadata(
    IReadOnlyList<JsModuleAttribute> Modules,
    IReadOnlyList<JsDependencyAttribute> Dependencies);

internal static class JsComponentMetadataCache
{
    private static readonly ConcurrentDictionary<Type, JsComponentMetadata> Cache = new();

    public static JsComponentMetadata Get(Type componentType) =>
        Cache.GetOrAdd(
            componentType,
            static type =>
            {
                var modules = type.GetCustomAttributes<JsModuleAttribute>(inherit: true).ToArray();
                if (modules.Length > 1)
                {
                    throw new InvalidOperationException(
                        $"Component '{type.FullName}' can declare only one JsModuleAttribute.");
                }

                return new JsComponentMetadata(
                    modules,
                    type.GetCustomAttributes<JsDependencyAttribute>(inherit: true).ToArray());
            });
}
