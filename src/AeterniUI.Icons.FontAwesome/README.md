# AeterniUI.Icons.FontAwesome

Font Awesome Free SVG definitions for AeterniUI.

Reference the project together with `AeterniUI`, then use the vendor-neutral
`Icon` component from the core library:

```razor
<Icon Definition="@FontAwesomeIcons.Solid.Plus" />

<Button>
    <StartIcon>
        <Icon Definition="@FontAwesomeIcons.Solid.Plus" />
    </StartIcon>
    Create
    <EndIcon>
        <Icon Definition="@FontAwesomeIcons.Solid.ArrowRight" />
    </EndIcon>
</Button>
```

Data-driven pages can resolve a name at runtime instead of a typed property:

```csharp
if (FontAwesomeIcons.TryGet("arrow-right", out var definition))
{
    // <Icon Definition="definition" />
}
```

`FontAwesomeIcons.Categories` groups the definitions by usage, which suits icon
pickers and documentation pages.

## Coverage and maintenance

The adapter ships a curated Font Awesome Free **7.3.1** Classic Solid set
(currently 311 icons) chosen to cover the component library, the sample project
and common UI semantics. `FontAwesomeIcons.cs` is generated — never edit it by
hand. To change the set, update the `MANIFEST` in
`scripts/generate-fontawesome-icons.mjs` and regenerate:

```bash
node scripts/generate-fontawesome-icons.mjs            # installs the pinned packages on first run
node scripts/generate-fontawesome-icons.mjs --check    # verifies the committed file is current
```

The script resolves every manifest name against the official npm package, so a
renamed or removed upstream icon fails loudly instead of silently shipping stale
geometry.

The adapter emits no Font Awesome CSS, web fonts, or JavaScript runtime, and the
core library never depends on an icon vendor. See `THIRD-PARTY-NOTICES.md` for
license information.
