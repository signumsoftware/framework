# Signum Framework - AI Agent Instructions

Instructions for AI coding assistants (GitHub Copilot, OpenCode, Claude, etc.) working on Signum Framework projects.

## Project Overview
- **Type:** Signum Framework SPA (C# backend + React/TypeScript frontend)
- **Architecture:** Vertical modules — each feature has entity, logic, and UI code together, not separated by technical concern.
- **Extensions:** `Framework/Extensions/` contains 40+ reusable modules (Authorization, Files, Workflow, Dashboard, Chart, Mailing, MachineLearning, etc.) each following the same vertical pattern.

## General Guidance
- Use Signum Framework conventions for entities, queries, operations, and React components.
- Add minimal comments only if necessary.
- Respect existing folder and module structure.
- Prefer editing existing files over creating new ones.

---

## Build System

### Visual Studio Copilot Only
- **ALWAYS use `run_build` tool** instead of `run_command_in_terminal` with `dotnet build`.
- The `run_build` tool uses Visual Studio's integrated compiler (much faster).
- Only use `dotnet build` if `run_build` is unavailable.

### CLI Agents (Claude Code, OpenCode, etc.)
- The solution is large; **compile only the affected .csproj**, not the entire solution.
- For TypeScript, compile only the affected tsconfig: `yarn tsgo --build`.
- If you change C# entity code, recompile the .csproj to regenerate TypeScript definitions via TSGenerator.

---

## C# Conventions

### Logic Pattern
- **Static classes for logic**, not dependency injection. Avoid DI unless ASP.NET extensibility requires it.
- Each module has a static `Start(SchemaBuilder sb)` method called from `Starter.cs`.
- Guard against re-registration: `if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod())) return;`
- Prefer **synchronous** logic for methods used by operations or processes.

### Entity Pattern
- Entities inherit from `Entity` (with PrimaryKey) or `EmbeddedEntity` (no PK, owned by parent).
- `ModelEntity` for DTOs without database storage.
- `MixinEntity` to add cross-cutting fields to existing entities.
- Use **nullable reference types**, but allow DTOs without default values or constructors (often deserialized).
- Collections use `MList<T>` (not `List<T>`), decorated with `[NoRepeatValidator]` and optionally `[PreserveOrder]`.
- Computed properties use `[AutoExpressionField]` with `As.Expression(() => ...)`.
- Polymorphic references use `[ImplementedBy(typeof(A), typeof(B))]`.
- Use `Lite<T>` for lazy references to other entities.

### Operations & State Machines
- Operations are declared as static symbols in an `[AutoInit]` static class (e.g., `OrderOperation`).
- Types: `ConstructSymbol<T>.Simple`, `ConstructSymbol<T>.From<F>`, `ExecuteSymbol<T>`, `DeleteSymbol<T>`.
- State machines use `Graph<T>` with `FromStates`/`ToStates` and `StateValidator<T, S>`.
- Register operations via fluent API: `sb.Include<T>().WithSave(Op.Save).WithQuery(...)` or `new Execute(Op.Ship) { ... }.Register()`.

### Queries
- Use Signum LINQ provider, **not** EF or raw SQL.
- Register queries in `Start()` via `sb.Include<T>().WithQuery(...)` or `QueryLogic.Queries.Register(...)`.

### Localization
End-user messages MUST be localized:
- Entities: `typeof(LabelEntity).NiceName()` / `.NicePluralName()`
- Properties: `ReflectionTools.GetPropertyInfo((LabelEntity l) => l.Name).NiceName()` or `pi.NiceName()`
- Enums: `LabelState.Active.NiceToString()`
- Custom messages: create a `Message` enum with `[Description]` attributes:
  ```cs
  public enum YourMessage
  {
      [Description("My favorite food is {0}")]
      MyFavoriteFoodIs0,
  }
  // Usage: YourMessage.MyFavoriteFoodIs0.NiceToString("Tom Yum Soup")
  ```

### Validation
- Use declarative validators: `[StringLengthValidator]`, `[NotNullValidator]`, `[TelephoneValidator]`, etc.
- Entity-level validation via `protected override string? PropertyValidation(PropertyInfo pi)`.
- Child validation via `protected override string? ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi)`.

---

## TypeScript / React Conventions

### General
- Functional React components as simple functions (not arrow components).
- Type all props and state (using `isolatedDeclarations`).
- Use strict mode in TypeScript.
- Use Bootstrap, React-Bootstrap, and Font Awesome for UI.
- Prefer Signum hooks (`useAPI`, `useForceUpdate`) over state management libraries.
- Allow imperative modification of entities; do not enforce strict immutability.

### Module Client Pattern
Each module has a `Client.tsx` with a `start()` function that registers UI settings:
```tsx
export namespace OrdersClient {
  export function start(options: { routes: RouteObject[] }): void {
    Navigator.addSettings(new EntitySettings(OrderEntity, o => import('./Order')));
    Finder.addSettings({ queryName: OrderEntity, ... });
  }
}
```

### Entity Form Components
Use `TypeContext<T>` for type-safe property access:
```tsx
export default function Order(p: { ctx: TypeContext<OrderEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  return (
    <div>
      <AutoLine ctx={ctx.subCtx(o => o.customer)} />
      <AutoLine ctx={ctx.subCtx(o => o.orderDate)} />
      <EntityTable ctx={ctx.subCtx(o => o.details)} />
    </div>
  );
}
```

### Key UI Components
- `AutoLine` — Smart field renderer, selects component based on property type.
- `EntityLine` / `EntityCombo` / `EntityStrip` — Single entity references.
- `EntityDetail` / `EntityRepeater` / `EntityTable` — Embedded entity collections.
- `SearchControl` — Dynamic query results grid.

### Generated TypeScript (TSGenerator)
- `*.ts` files are **auto-generated** from C# entities — do NOT edit them manually.
- They contain interfaces, enums, operation symbols, and query symbols matching C# definitions.
- Regenerate by recompiling the C# project.

### Localization (TypeScript)
- Entities: `LabelEntity.niceName()` / `.nicePluralName()`
- Properties: `LabelEntity.nicePropertyName(a => a.name)`
- Enums: `LabelState.niceToString("Active")`
- Custom messages: create the enum in C# first, recompile, then use `YourMessage.MyFavoriteFoodIs0.niceToString("Tom Yum Soup")`.
- For React nodes with formatting: use `formatHtml` / `joinHtml`.
