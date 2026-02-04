using Signum.Utilities;
using System.IO;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20260201_UpdateCopilotInstructions : CodeUpgradeBase
{
    public override string Description => "Update .github/copilot-instructions.md with latest conventions";

    public override void Execute(UpgradeContext uctx)
    {
        var content = """
# GitHub Copilot Repository Instructions

## Project Overview
- **Type:** Signum Framework SPA
- **Main Project:** Agile360/Agile360.csproj
- **.NET Version:** 10.0
- **UI Framework:** React (TypeScript SPA)

## General Guidance
- Use Signum Framework conventions for entities, queries, operations, react components, etc...
- Add minimal comments only if necesary.
- Respect existing folder and module structure: code is organized by feature/module, not by technical concern.
- Framework is a git submodule with shared code.
- Framework\Extensions contains many reusable vertical modules with both C# and TypeScript.

## Language-Specific Guidance

### C#
- The solution is large; avoid compiling the entire solution unless necessary. Prefer compiling only the affected project.
- When running in Visual Studio (not VS Code) use the integrated compiler not dotnet build. 
- Prefer static classes for logic over dependency injection.
- Prefer synchronous logic for methods used by operations or processes.
- Use Signum LINQ provider for queries, not EF or SQL.
- Avoid dependency injection unless ASP.Net extensibility requires it.
- Follow Signum static logic registration patterns.
- Use not nullable reference types, but allow DTOs without default values or constructors (often deserialized).
- Messages for the end user MUST be localized. Use the extension methods in `DescriptionManager`:
	- Entities (e.g., `typeof(LabelEntity).NiceName()` / `typeof(LabelEntity).NicePluralName()`)
	- Properties (e.g., `ReflectionTools.GetPropertyInfo((LabelEntity l) => l.Name).NiceName()` or often just `pi.NiceName()`)
	- Enums (e.g., `LabelState.Active.NiceToString()`)
	- For custom messages, try reusing first, othwerwise you creating a new Message enum like: 
	 ```cs
	 public enum YourMessage
	 {
		 [Description("My favorite food is {0}")]
		 MyFavoriteFoodIs0,
	 }
	 ```
	 Then you can use it like `YourMessage.MyFavoriteFoodIs0.NiceToString("Tom Yum Soup")`).

### TypeScript / React
- The solution is large; avoid compiling the entire solution unless necessary. Prefer compiling only the affected tsconfig using `yarn tsgo --build`.
- If you change code in C#, you can regenerate the TypeScript definitions just compiling the csproj.
- Prioritize React and TypeScript for UI code.
- Use Bootstrap, React-Bootstrap, and Font Awesome icons for UI components.
- Type all props and state (using isolatedDeclarations).
- Use functional React components as simple functions.
- Prefer Signum hooks (e.g., useAPI, useForceUpdate) over state management libraries.
- Use strict mode in TypeScript.
- Allow imperative modification of entities in React components; do not enforce strict immutability.
- Messages for the end user MUST be localized. Use the methods in Reflection.ts
	- Entities (e.g., `LabelEntity.niceName()` / `LabelEntity.nicePluralName()`)
	- Properties (e.g., `LabelEntity.nicePropertyName(a => a.name)`)
	- Enums (e.g., `LabelState.niceToString("Active")`)
	- For custom messages, consider reusing or creating a new Message enum in C# first, recompile C#, then you can use 
       `YourMessage.MyFavoriteFoodIs0.niceToString("Tom Yum Soup")` (a shortcut for `.niceToString().formatWith("Tom Yum Soup")`).
	- You can also use `formatHtml`/`joinHtml` to produce React nodes with formatting.
""";
        var fileName = @".github/copilot-instructions.md";
        if (!File.Exists(uctx.AbsolutePath(fileName)))
            uctx.CreateCodeFile(fileName, content);
        else
            uctx.ChangeCodeFile(fileName, cf => { cf.Content = content; });
    }
}
