using Microsoft.AspNetCore.Components.Forms;

namespace AeterniUI.Components;

/// <summary>
/// Owns one component's subscription to a cascading <see cref="EditContext"/>.
/// </summary>
/// <remarks>
/// Every form-capable component needs the same three things: subscribe when an
/// <c>EditContext</c> plus a bound expression appear, unsubscribe when either
/// goes away or the component is disposed, and re-render when that context
/// reports a validation-state change. Thirteen components had each grown their
/// own copy of that machinery, and the copies had begun to drift in whitespace
/// and in ordering. The <em>expression</em> side is genuinely component-specific
/// — a value expression, a multi-select expression, or a start/end pair — so it
/// stays with the component; only the subscription mechanics live here.
/// </remarks>
internal sealed class EditContextSubscription(Action onValidationStateChanged)
{
    private readonly Action _onValidationStateChanged = onValidationStateChanged;
    private EditContext? _context;

    /// <summary>
    /// Points the subscription at <paramref name="context" />, subscribing or
    /// unsubscribing as needed. Safe to call on every parameter set: setting the
    /// same context twice keeps the existing subscription.
    /// </summary>
    public void Attach(EditContext? context)
    {
        if (ReferenceEquals(_context, context))
        {
            return;
        }

        Detach();
        _context = context;

        if (_context is not null)
        {
            _context.OnValidationStateChanged += HandleValidationStateChanged;
        }
    }

    /// <summary>
    /// Releases the subscription. Idempotent, so a component can call it both when
    /// its parameters change and when it is disposed.
    /// </summary>
    public void Detach()
    {
        if (_context is null)
        {
            return;
        }

        _context.OnValidationStateChanged -= HandleValidationStateChanged;
        _context = null;
    }

    /// <summary>
    /// Reports a field change to the subscribed context, if there is one.
    /// </summary>
    public void NotifyFieldChanged(in FieldIdentifier identifier) =>
        _context?.NotifyFieldChanged(identifier);

    /// <summary>
    /// Indicates whether the subscribed context currently reports validation
    /// messages for <paramref name="identifier" />. Components use this to decide
    /// whether to render their invalid state.
    /// </summary>
    public bool HasValidationMessages(in FieldIdentifier identifier) =>
        _context?.GetValidationMessages(identifier).Any() == true;

    /// <summary>
    /// The first validation message the subscribed context reports for
    /// <paramref name="identifier" />, or <see langword="null" /> when there is
    /// none.
    /// </summary>
    public string? FirstValidationMessage(in FieldIdentifier identifier) =>
        _context?.GetValidationMessages(identifier).FirstOrDefault();

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs args) =>
        _onValidationStateChanged();
}
