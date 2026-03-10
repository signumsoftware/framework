# Signum Framework - GitHub Copilot Instructions

## Project Overview
- **Type:** Signum Framework SPA
- **UI Framework:** React (TypeScript SPA)

## General Guidance
- Use Signum Framework conventions for entities, queries, operations, react components, etc...
- Add minimal comments only if necessary.
- Respect existing folder and module structure: code is organized by feature/module, not by technical concern.
- Framework\Extensions contains many reusable vertical modules with both C# and TypeScript.

## Skills

Detailed guidance is organized in `Framework/Skills/`. Read the relevant file before working on that area:

| Skill | Description |
|---|---|
| [Localization](Skills/Localization.md) | How to localize user-facing messages in C# and TypeScript/React |
| [ReactTesting](Skills/ReactTesting.md) | How to write Selenium UI tests: proxies, environment setup, waiting, debugging |

## Language-Specific Guidance

### Build System (for Visual Studio COPILOT only!!)
- **ALWAYS use `run_build` tool** instead of `run_command_in_terminal` with `dotnet build`
- The `run_build` tool automatically uses Visual Studio's integrated compiler (much faster than dotnet CLI)
- Only use `dotnet build` in terminal if `run_build` is unavailable or specific CLI flags are required

### C#
- The solution is large; avoid compiling the entire solution unless necessary. Prefer compiling only the affected project.
- Prefer static classes for logic over dependency injection.
- Prefer synchronous logic for methods used by operations or processes.
- Use Signum LINQ provider for queries, not EF or SQL.
- Avoid dependency injection unless ASP.Net extensibility requires it.
- Follow Signum static logic registration patterns.
- Use not nullable reference types, but allow DTOs without default values or constructors (often deserialized).
- Messages for the end user MUST be localized. See [Localization](Skills/Localization.md).

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
- Messages for the end user MUST be localized. See [Localization](Skills/Localization.md).
