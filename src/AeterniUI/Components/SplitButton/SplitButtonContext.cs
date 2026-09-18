namespace AeterniUI.Components.SplitButton;

// Match only the two owned triggers; icon fragments and menu content may contain other buttons.
internal sealed record SplitButtonContext(string PrimaryId, string MenuTriggerId);
