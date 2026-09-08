using System.Reflection;
using AeterniUI.Attributes;
using AeterniUI.Modules;
using Microsoft.JSInterop;

namespace AeterniUI.Services.Impl;

/// <summary>
/// 按组件实例管理 JS module。组件在 OnAfterRenderAsync 首次渲染后调用
/// <see cref="LoadComponentAsync"/>，管理器不会在扫描阶段访问浏览器。
/// </summary>
public sealed class JsModuleManager(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private readonly Dictionary<string, JsModuleInfo> _moduleInfos = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Task<IJSObjectReference?>> _moduleTasks = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private bool _disposed;

    public void ScanComponent(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        var metadata = JsComponentMetadataCache.Get(componentType);
        var modules = metadata.Modules;
        var moduleNames = modules
            .Select(module => RegisterModule(module, componentType.Assembly))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var dependency in metadata.Dependencies)
        {
            var name = dependency.Alias ?? dependency.Path;
            RegisterInfo(new JsModuleInfo
            {
                Name = name,
                Path = ResolveModulePath(dependency.Path, componentType.Assembly),
                IsDependency = true
            });

            foreach (var moduleName in moduleNames)
            {
                AddDependency(moduleName, name);
            }
        }
    }

    /// <summary>在组件完成首次 DOM 渲染后加载它声明的模块及其依赖。</summary>
    public async Task<IReadOnlyDictionary<string, IJSObjectReference>> LoadComponentAsync(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ScanComponent(componentType);

        var attributes = JsComponentMetadataCache.Get(componentType).Modules;
        var names = attributes.Select(attribute => attribute.Name ?? attribute.Path).ToArray();
        var result = new Dictionary<string, IJSObjectReference>(StringComparer.Ordinal);

        foreach (var name in names)
        {
            await LoadGraphAsync(name, result, new HashSet<string>(StringComparer.Ordinal));
        }

        return result;
    }

    /// <summary>
    /// 为 Interactive module 调用约定的 init(dotNetRef, args...)。传入的对象可由 JS 通过
    /// invokeMethodAsync / invokeMethodAsync&lt;T&gt; 回调带有 [JSInvokable] 的组件方法。
    /// </summary>
    public async Task InitializeInteractiveAsync<T>(string moduleName, DotNetObjectReference<T> dotNetRef, params object?[] args)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(dotNetRef);
        var module = await GetModuleAsync(moduleName);
        if (!TryGetModuleInfo(moduleName, out var info) || !info.Interactive || module is null)
        {
            return;
        }

        try
        {
            var initArgs = new object?[args.Length + 1];
            initArgs[0] = dotNetRef;
            Array.Copy(args, 0, initArgs, 1, args.Length);
            await module.InvokeVoidAsync("init", initArgs);
        }
        catch (Exception ex) when (IsJsUnavailableException(ex) || ex is JSException)
        {
            return;
        }
    }

    /// <summary>
    /// Calls the per-component dispose hook for a component's module.
    /// </summary>
    public async Task DisposeModuleAsync(string moduleName, string instanceId)
    {
        ArgumentException.ThrowIfNullOrEmpty(moduleName);
        ArgumentException.ThrowIfNullOrEmpty(instanceId);

        if (!TryGetModuleInfo(moduleName, out _))
        {
            return;
        }

        try
        {
            var module = await GetModuleAsync(moduleName);
            if (module is null)
            {
                return;
            }

            await module.InvokeVoidAsync("dispose", instanceId);
        }
        catch (Exception ex) when (IsJsUnavailableException(ex) || ex is JSException)
        {
        }
    }

    public async Task<IJSObjectReference?> GetModuleAsync(string moduleName)
    {
        ArgumentException.ThrowIfNullOrEmpty(moduleName);
        return IsModuleRegistered(moduleName) ? await LoadModuleAsync(moduleName) : null;
    }

    public async Task InvokeModuleVoidAsync(string moduleName, string functionName, params object?[] args)
    {
        var module = await GetModuleAsync(moduleName);
        if (module is not null)
        {
            try
            {
                await module.InvokeVoidAsync(functionName, args);
            }
            catch (Exception ex) when (IsJsUnavailableException(ex))
            {
            }
        }
    }

    public async Task<T?> InvokeModuleAsync<T>(string moduleName, string functionName, params object?[] args)
    {
        var module = await GetModuleAsync(moduleName);
        if (module is null)
        {
            return default;
        }

        try
        {
            return await module.InvokeAsync<T>(functionName, args);
        }
        catch (Exception ex) when (IsJsUnavailableException(ex))
        {
            return default;
        }
    }

    private string RegisterModule(JsModuleAttribute attribute, Assembly componentAssembly)
    {
        var name = attribute.Name ?? attribute.Path;
        RegisterInfo(new JsModuleInfo
        {
            Name = name,
            Path = ResolveModulePath(attribute.Path, componentAssembly),
            Interactive = attribute.Interactive
        });
        return name;
    }

    private void RegisterInfo(JsModuleInfo info)
    {
        lock (_sync)
        {
            if (_moduleInfos.TryGetValue(info.Name, out var existing))
            {
                if (!string.Equals(existing.Path, info.Path, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"The JS module name '{info.Name}' is registered with different paths.");
                }

                _moduleInfos[info.Name] = new JsModuleInfo
                {
                    Name = info.Name,
                    Path = existing.Path,
                    Interactive = existing.Interactive || info.Interactive,
                    IsDependency = existing.IsDependency && info.IsDependency,
                    Dependencies = existing.Dependencies
                        .Concat(info.Dependencies)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray()
                };
                return;
            }
            _moduleInfos[info.Name] = info;
        }
    }

    private void AddDependency(string moduleName, string dependencyName)
    {
        lock (_sync)
        {
            if (!_moduleInfos.TryGetValue(moduleName, out var info)) return;
            _moduleInfos[moduleName] = new JsModuleInfo { Name = info.Name, Path = info.Path, Interactive = info.Interactive, IsDependency = info.IsDependency, Dependencies = info.Dependencies.Append(dependencyName).Distinct(StringComparer.Ordinal).ToArray() };
        }
    }

    private async Task LoadGraphAsync(
        string name,
        Dictionary<string, IJSObjectReference> loaded,
        HashSet<string> visiting)
    {
        if (loaded.ContainsKey(name))
        {
            return;
        }

        if (!visiting.Add(name))
        {
            throw new InvalidOperationException($"A circular JS module dependency was detected at '{name}'.");
        }

        try
        {
            if (!TryGetModuleInfo(name, out var info))
            {
                return;
            }

            foreach (var dependency in info.Dependencies)
            {
                await LoadGraphAsync(dependency, loaded, visiting);
            }

            var module = await LoadModuleAsync(name);
            if (module is not null)
            {
                loaded[name] = module;
            }
        }
        finally
        {
            visiting.Remove(name);
        }
    }

    private bool IsModuleRegistered(string name)
    {
        lock (_sync)
        {
            return _moduleInfos.ContainsKey(name);
        }
    }

    private bool TryGetModuleInfo(string name, out JsModuleInfo info)
    {
        lock (_sync)
        {
            return _moduleInfos.TryGetValue(name, out info!);
        }
    }

    private Task<IJSObjectReference?> LoadModuleAsync(string name)
    {
        lock (_sync)
        {
            if (_disposed || !_moduleInfos.TryGetValue(name, out var info)) return Task.FromResult<IJSObjectReference?>(null);
            if (_moduleTasks.TryGetValue(name, out var task)) return task;
            task = ImportModuleAsync(info.Path);
            _moduleTasks[name] = task;
            return task;
        }
    }

    private async Task<IJSObjectReference?> ImportModuleAsync(string path)
    {
        try { return await _jsRuntime.InvokeAsync<IJSObjectReference>("import", path); }
        catch (Exception ex) when (IsJsUnavailableException(ex)) { return null; }
    }

    private static string ResolveModulePath(string path, Assembly componentAssembly)
    {
        if (string.IsNullOrWhiteSpace(path) || path.StartsWith('/') || path.StartsWith("http", StringComparison.OrdinalIgnoreCase) || path.Contains("_content", StringComparison.OrdinalIgnoreCase)) return path;
        var assemblyName = componentAssembly.GetName().Name;
        return $"./_content/{assemblyName}/{path.TrimStart('/')}";
    }

    public async ValueTask DisposeAsync()
    {
        Task<IJSObjectReference?>[] moduleTasks;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            moduleTasks = _moduleTasks.Values.ToArray();
        }

        foreach (var task in moduleTasks)
        {
            try
            {
                var module = await task;
                if (module is not null)
                {
                    await module.DisposeAsync();
                }
            }
            catch (Exception ex) when (IsJsUnavailableException(ex) || ex is JSException)
            {
            }
        }

        lock (_sync)
        {
            _moduleTasks.Clear();
            _moduleInfos.Clear();
        }

        GC.SuppressFinalize(this);
    }

    private static bool IsJsUnavailableException(Exception ex) => ex is JSDisconnectedException or InvalidOperationException or TaskCanceledException;

}
