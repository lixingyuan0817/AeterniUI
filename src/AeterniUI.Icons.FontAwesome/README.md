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

The first release contains selected Font Awesome Free Classic Solid icons.
The adapter emits no Font Awesome CSS, web fonts, or JavaScript runtime.
See `THIRD-PARTY-NOTICES.md` for license information.
