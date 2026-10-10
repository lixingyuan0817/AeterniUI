namespace AeterniUI.Enums;

/// <summary>
/// How a <c>FlashCard</c> reveals its back face. <see cref="Click" /> is a
/// keyboard reachable control whose state stays in the component;
/// <see cref="Hover" /> is a pointer-only visual preview that never changes state.
/// </summary>
public enum FlashCardFlipTrigger
{
    Click,
    Hover
}
