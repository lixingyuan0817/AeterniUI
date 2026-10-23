namespace AeterniUI.Enums;

/// <summary>
/// Chrome preset for an icon button, applied on top of <see cref="ButtonVariant" />
/// and <see cref="ButtonIntent" />.
/// </summary>
/// <remarks>
/// A tone is the surface, border and ink recipe for a chrome action (a title-bar
/// or row-end control) rather than an emphasis level. It therefore owns the
/// colours that <see cref="ButtonVariant" /> and <see cref="ButtonIntent" /> would
/// otherwise paint; geometry, loading, disabled and focus behaviour stay
/// unchanged. Use the default value to keep the normal button pipeline.
/// </remarks>
public enum IconButtonTone
{
    /// <summary>Uses the regular <see cref="ButtonVariant" /> / <see cref="ButtonIntent" /> colours; the default.</summary>
    Default,

    /// <summary>
    /// Transparent chrome with tertiary ink: low-emphasis title-bar and row-end
    /// actions. Hover and active step the shared state surfaces instead of lifting
    /// the control, and no shadow is painted.
    /// </summary>
    Muted,

    /// <summary>
    /// Destructive chrome tinted with the danger colour: a translucent danger fill
    /// and outline with the danger text ink, without the full strength of a solid
    /// danger button.
    /// </summary>
    DangerTinted
}
