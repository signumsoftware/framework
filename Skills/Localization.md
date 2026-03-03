# Localization

Messages shown to end users **MUST be localized**. Never use plain string literals for user-facing text.

## C#

Use extension methods from `DescriptionManager`:

| Use case | Example |
|---|---|
| Entity name | `typeof(LabelEntity).NiceName()` / `typeof(LabelEntity).NicePluralName()` |
| Property name | `ReflectionTools.GetPropertyInfo((LabelEntity l) => l.Name).NiceName()` or `pi.NiceName()` |
| Enum value | `LabelState.Active.NiceToString()` |

For custom messages, reuse an existing `Message` enum value first. If none fits, create a new one:

```cs
public enum YourMessage
{
    [Description("My favorite food is {0}")]
    MyFavoriteFoodIs0,
}
```

Use it with: `YourMessage.MyFavoriteFoodIs0.NiceToString("Tom Yum Soup")`

## TypeScript / React

Use methods from `Reflection.ts`:

| Use case | Example |
|---|---|
| Entity name | `LabelEntity.niceName()` / `LabelEntity.nicePluralName()` |
| Property name | `LabelEntity.nicePropertyName(a => a.name)` |
| Enum value | `LabelState.niceToString("Active")` |

For custom messages, define the `Message` enum in C# first, recompile, then use:

```ts
YourMessage.MyFavoriteFoodIs0.niceToString("Tom Yum Soup")
// shortcut for .niceToString().formatWith("Tom Yum Soup")
```

Use `formatHtml` / `joinHtml` to produce React nodes with formatting.
