using System.Diagnostics;
using AeterniUI.Enums;
using AeterniUI.Models.Dialog;
using AeterniUI.Services;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Services.Impl;

public sealed class DialogService : IDialogService, IDisposable
{
    private readonly object _sync = new();
    private readonly List<DialogEntry> _dialogs = [];
    private readonly List<DialogEntry> _alerts = [];
    private readonly List<ToastEntry> _toasts = [];
    private ToastPosition _defaultToastPosition = ToastPosition.TopEnd;
    private ToastPosition _defaultAlertPosition = ToastPosition.BottomCenter;
    private TimeSpan _defaultAlertDuration = TimeSpan.FromSeconds(5);
    private TimeSpan _defaultToastDuration = TimeSpan.FromSeconds(5);
    private int _maxToastCount = 5;
    private bool _disposed;

    public DialogService(AeterniUIOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateToastPosition(options.DefaultToastPosition);
        ValidateToastPosition(options.DefaultAlertPosition);

        if (options.MaxToastCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaxToastCount));
        }

        ValidateDuration(options.DefaultAlertDuration, nameof(options.DefaultAlertDuration));
        ValidateDuration(options.DefaultToastDuration, nameof(options.DefaultToastDuration));

        _defaultToastPosition = options.DefaultToastPosition;
        _defaultAlertPosition = options.DefaultAlertPosition;
        _defaultAlertDuration = options.DefaultAlertDuration;
        _defaultToastDuration = options.DefaultToastDuration;
        _maxToastCount = options.MaxToastCount;
    }

    public event EventHandler? Changed;

    public ToastPosition DefaultToastPosition
    {
        get => _defaultToastPosition;
        set
        {
            ValidateToastPosition(value);
            _defaultToastPosition = value;
        }
    }

    public ToastPosition DefaultAlertPosition
    {
        get => _defaultAlertPosition;
        set
        {
            ValidateToastPosition(value);
            _defaultAlertPosition = value;
        }
    }

    public TimeSpan DefaultAlertDuration
    {
        get => _defaultAlertDuration;
        set
        {
            ValidateDuration(value, nameof(value));
            _defaultAlertDuration = value;
        }
    }

    public TimeSpan DefaultToastDuration
    {
        get => _defaultToastDuration;
        set
        {
            ValidateDuration(value, nameof(value));
            _defaultToastDuration = value;
        }
    }

    public int MaxToastCount
    {
        get => _maxToastCount;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The maximum toast count must be greater than zero.");
            }

            List<ToastEntry>? removed = null;
            lock (_sync)
            {
                _maxToastCount = value;
                while (_toasts.Count > _maxToastCount)
                {
                    removed ??= [];
                    removed.Add(_toasts[0]);
                    _toasts.RemoveAt(0);
                }
            }

            CompleteRemovedToasts(removed);
            if (removed is not null)
            {
                NotifyChanged();
            }
        }
    }

    internal IReadOnlyList<DialogEntry> Dialogs
    {
        get
        {
            lock (_sync)
            {
                return _dialogs.ToArray();
            }
        }
    }

    internal IReadOnlyList<DialogEntry> Alerts
    {
        get
        {
            lock (_sync)
            {
                return _alerts.ToArray();
            }
        }
    }

    internal IReadOnlyList<ToastEntry> Toasts
    {
        get
        {
            lock (_sync)
            {
                return _toasts.ToArray();
            }
        }
    }

    public DialogReference Show(RenderFragment content, DialogOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        var entry = new DialogEntry(
            CreateId("dialog"),
            content,
            message: null,
            DialogContentKind.Custom,
            options ?? new DialogOptions(),
            TimeSpan.Zero);

        var reference = new DialogReference(entry.Id, result => CloseDialogAsync(entry.Id, result));
        entry.Reference = reference;

        lock (_sync)
        {
            ThrowIfDisposed();
            _dialogs.Add(entry);
        }

        NotifyChanged();
        return reference;
    }

    public Task<DialogResult> ShowAsync(RenderFragment content, DialogOptions? options = null) =>
        Show(content, options).Result;

    public Task<DialogResult> AlertAsync(string message, AlertOptions? options = null) =>
        AlertAsyncCore(message, position: null, options);

    public Task<DialogResult> AlertAsync(
        string message,
        ToastPosition position,
        AlertOptions? options = null) =>
        AlertAsyncCore(message, position, options);

    private Task<DialogResult> AlertAsyncCore(
        string message,
        ToastPosition? position,
        AlertOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        var alertOptions = options ?? new AlertOptions();
        var resolvedPosition = position ?? alertOptions.Position ?? DefaultAlertPosition;
        ValidateToastPosition(resolvedPosition);
        var duration = ResolveDuration(alertOptions.Duration, DefaultAlertDuration);
        var entry = CreateMessageDialog(
            message,
            alertOptions,
            DialogContentKind.Alert,
            _alerts,
            duration,
            resolvedPosition);
        if (duration > TimeSpan.Zero)
        {
            _ = RunAlertTimerAsync(entry, duration);
        }

        return entry.Reference.Result;
    }

    public async Task<bool> ConfirmAsync(string message, ConfirmOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        var entry = CreateMessageDialog(
            message,
            options ?? new ConfirmOptions(),
            DialogContentKind.Confirm,
            _dialogs,
            TimeSpan.Zero);
        var result = await entry.Reference.Result;
        return result.Confirmed;
    }

    public ToastReference ShowToast(string message, ToastOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return ShowToastCore(message: message, content: null, options: options);
    }

    public ToastReference ShowToast(string message, ToastPosition position, ToastOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return ShowToastCore(
            message: message,
            content: null,
            options: options,
            positionOverride: position);
    }

    public ToastReference ShowToast(RenderFragment content, ToastOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        return ShowToastCore(message: null, content: content, options: options);
    }

    public ToastReference ShowToast(RenderFragment content, ToastPosition position, ToastOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        return ShowToastCore(
            message: null,
            content: content,
            options: options,
            positionOverride: position);
    }

    internal DialogEntry? GetTopDialog()
    {
        lock (_sync)
        {
            return _dialogs.Count == 0 ? null : _dialogs[^1];
        }
    }

    internal DialogEntry? GetTopAlert()
    {
        lock (_sync)
        {
            return _alerts.Count == 0 ? null : _alerts[^1];
        }
    }

    internal IReadOnlyList<ToastEntry> GetToasts(ToastPosition position)
    {
        lock (_sync)
        {
            return _toasts.Where(toast => toast.Position == position).ToArray();
        }
    }

    internal IReadOnlyList<DialogEntry> GetAlerts(ToastPosition position)
    {
        lock (_sync)
        {
            return _alerts.Where(alert => alert.Position == position).ToArray();
        }
    }

    private DialogEntry CreateMessageDialog(
        string message,
        DialogOptions options,
        DialogContentKind kind,
        List<DialogEntry> target,
        TimeSpan duration,
        ToastPosition? position = null)
    {
        var entry = new DialogEntry(
            CreateId("dialog"),
            content: null,
            message,
            kind,
            options,
            duration,
            position);

        entry.Reference = new DialogReference(entry.Id, result => CloseDialogAsync(entry.Id, result));

        lock (_sync)
        {
            ThrowIfDisposed();
            target.Add(entry);
        }

        NotifyChanged();
        return entry;
    }

    private ToastReference ShowToastCore(
        string? message,
        RenderFragment? content,
        ToastOptions? options,
        ToastPosition? positionOverride = null)
    {
        var toastOptions = options ?? new ToastOptions();
        var position = positionOverride ?? toastOptions.Position ?? DefaultToastPosition;
        ValidateToastPosition(position);
        var duration = ResolveDuration(toastOptions.Duration, DefaultToastDuration);

        var entry = new ToastEntry(
            CreateId("toast"),
            message,
            content,
            toastOptions,
            position,
            duration);

        entry.Reference = new ToastReference(entry.Id, () => CloseToastAsync(entry.Id));
        List<ToastEntry>? removed = null;
        lock (_sync)
        {
            ThrowIfDisposed();
            while (_toasts.Count >= MaxToastCount)
            {
                removed ??= [];
                removed.Add(_toasts[0]);
                _toasts.RemoveAt(0);
            }

            _toasts.Add(entry);
        }

        CompleteRemovedToasts(removed);
        NotifyChanged();

        if (duration > TimeSpan.Zero)
        {
            _ = RunToastTimerAsync(entry, duration);
        }

        return entry.Reference;
    }

    private async Task RunAlertTimerAsync(DialogEntry entry, TimeSpan duration)
    {
        // The card border progress is driven entirely by CSS for the full
        // duration, so the timer never re-renders the provider while it runs —
        // it only closes the alert once the time has elapsed. This keeps the
        // ring smooth and prevents stacking re-renders when several notices
        // are visible at once.
        var stopwatch = Stopwatch.StartNew();
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));

        try
        {
            while (await timer.WaitForNextTickAsync(entry.CancellationTokenSource.Token))
            {
                if (duration - stopwatch.Elapsed <= TimeSpan.Zero)
                {
                    await CloseDialogAsync(entry.Id, DialogResult.Dismiss(), suppressCallbackErrors: true);
                    return;
                }
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
        {
        }
    }

    private async Task RunToastTimerAsync(ToastEntry entry, TimeSpan duration)
    {
        // See RunAlertTimerAsync: no per-tick renders, CSS owns the ring.
        var stopwatch = Stopwatch.StartNew();
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));

        try
        {
            while (await timer.WaitForNextTickAsync(entry.CancellationTokenSource.Token))
            {
                if (duration - stopwatch.Elapsed <= TimeSpan.Zero)
                {
                    await CloseToastAsync(entry.Id, suppressCallbackErrors: true);
                    return;
                }
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
        {
        }
    }

    private async Task CloseDialogAsync(
        string id,
        DialogResult result,
        bool suppressCallbackErrors = false)
    {
        DialogEntry? entry;
        lock (_sync)
        {
            entry = _dialogs.FirstOrDefault(dialog => dialog.Id == id);
            entry ??= _alerts.FirstOrDefault(alert => alert.Id == id);
            if (entry is null || entry.IsClosing)
            {
                return;
            }

            entry.MarkClosing();
        }

        NotifyChanged();
        await Task.Delay(TimeSpan.FromMilliseconds(180));

        var closingEntry = entry!;
        lock (_sync)
        {
            var removed = _dialogs.Remove(closingEntry) || _alerts.Remove(closingEntry);
            if (!removed)
            {
                return;
            }

            closingEntry.CancellationTokenSource.Cancel();
            closingEntry.CancellationTokenSource.Dispose();
        }

        closingEntry.Reference.TryComplete(result);
        if (closingEntry.Options is AlertOptions alertOptions)
        {
            await InvokeClosedCallbackAsync(alertOptions.OnClosedAsync, suppressCallbackErrors);
        }
        NotifyChanged();
    }

    private async Task CloseToastAsync(string id, bool suppressCallbackErrors = false)
    {
        ToastEntry? entry;
        lock (_sync)
        {
            entry = _toasts.FirstOrDefault(toast => toast.Id == id);
            if (entry is null || entry.IsClosing)
            {
                return;
            }

            entry.MarkClosing();
        }

        NotifyChanged();
        await Task.Delay(TimeSpan.FromMilliseconds(220));

        var closingEntry = entry!;
        lock (_sync)
        {
            if (!_toasts.Remove(closingEntry))
            {
                return;
            }

            closingEntry.CancellationTokenSource.Cancel();
            closingEntry.CancellationTokenSource.Dispose();
        }

        if (closingEntry.Options.OnClosedAsync is not null)
        {
            await InvokeClosedCallbackAsync(closingEntry.Options.OnClosedAsync, suppressCallbackErrors);
        }

        NotifyChanged();
    }

    private void CompleteRemovedToasts(IEnumerable<ToastEntry>? entries)
    {
        if (entries is null)
        {
            return;
        }

        foreach (var entry in entries)
        {
            entry.CancellationTokenSource.Cancel();
            entry.CancellationTokenSource.Dispose();
            _ = InvokeClosedCallbackAsync(entry.Options.OnClosedAsync, suppressCallbackErrors: true);
        }
    }

    private static async Task InvokeClosedCallbackAsync(
        Func<Task>? callback,
        bool suppressCallbackErrors)
    {
        if (callback is null)
        {
            return;
        }

        if (!suppressCallbackErrors)
        {
            await callback();
            return;
        }

        try
        {
            await callback();
        }
        catch
        {
            // Auto-dismiss callbacks must not become unobserved background task failures.
        }
    }

    private static TimeSpan ResolveDuration(TimeSpan? duration, TimeSpan defaultDuration)
    {
        var resolved = duration ?? defaultDuration;
        ValidateDuration(resolved, nameof(duration));
        return resolved;
    }

    private static void ValidateDuration(TimeSpan duration, string parameterName)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, "The duration cannot be negative.");
        }
    }

    private static void ValidateToastPosition(ToastPosition position)
    {
        if (!Enum.IsDefined(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Unknown toast position.");
        }
    }

    private static string CreateId(string prefix) => $"aeterni-{prefix}-{Guid.NewGuid():N}";

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        List<DialogEntry> dialogs;
        List<DialogEntry> alerts;
        List<ToastEntry> toasts;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            dialogs = [.. _dialogs];
            alerts = [.. _alerts];
            toasts = [.. _toasts];
            _dialogs.Clear();
            _alerts.Clear();
            _toasts.Clear();
        }

        CompleteRemovedToasts(toasts);
        foreach (var dialog in dialogs.Concat(alerts))
        {
            dialog.CancellationTokenSource.Cancel();
            dialog.CancellationTokenSource.Dispose();
            dialog.Reference.TryComplete(DialogResult.Dismiss());

            if (dialog.Options is AlertOptions alertOptions)
            {
                _ = InvokeClosedCallbackAsync(alertOptions.OnClosedAsync, suppressCallbackErrors: true);
            }
        }
    }
}

internal enum DialogContentKind
{
    Custom,
    Alert,
    Confirm
}

internal sealed class DialogEntry(
    string id,
    RenderFragment? content,
    string? message,
    DialogContentKind kind,
    DialogOptions options,
    TimeSpan duration,
    ToastPosition? position = null)
{
    public string Id { get; } = id;

    public RenderFragment? Content { get; } = content;

    public string? Message { get; } = message;

    public DialogContentKind Kind { get; } = kind;

    public DialogOptions Options { get; } = options;

    public ToastPosition? Position { get; } = position;

    public DialogReference Reference { get; set; } = null!;

    public TimeSpan Duration { get; } = duration;

    public bool IsClosing { get; private set; }

    public CancellationTokenSource CancellationTokenSource { get; } = new();

    internal void MarkClosing() => IsClosing = true;
}

internal sealed class ToastEntry(
    string id,
    string? message,
    RenderFragment? content,
    ToastOptions options,
    ToastPosition position,
    TimeSpan duration)
{
    public string Id { get; } = id;

    public string? Message { get; } = message;

    public RenderFragment? Content { get; } = content;

    public ToastOptions Options { get; } = options;

    public ToastPosition Position { get; } = position;

    public ToastReference Reference { get; set; } = null!;

    public CancellationTokenSource CancellationTokenSource { get; } = new();

    public TimeSpan Duration { get; } = duration;

    public bool IsClosing { get; private set; }

    internal void MarkClosing() => IsClosing = true;
}
