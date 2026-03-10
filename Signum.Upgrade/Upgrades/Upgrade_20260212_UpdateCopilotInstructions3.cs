using Signum.Utilities;
using System.IO;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20260212_UpdateCopilotInstructions3 : CodeUpgradeBase
{
    public override string Description => "Simplify AI instruction files and improve AGENTS.md with project-specific structure";

    public override void Execute(UpgradeContext uctx)
    {
        var an = uctx.ApplicationName;

        // Update AGENTS.md with real project structure
        uctx.ChangeCodeFile("AGENTS.md", cf =>
        {
            cf.Content = $"""
# AI Agent Instructions for {an}

**Read [`Framework/AGENTS.md`](Framework/AGENTS.md) first** — it contains all Signum Framework conventions (entities, operations, logic, React components, localization, build system).

This file only covers {an}-specific details.

---

## Project Structure
- **{an}/** — Main library: entities, logic, and React components organized by module.
- **{an}.Server/** — ASP.NET Core host, Vite dev server (port 3000), API controllers.
- **{an}.Terminal/** — Console app for database migrations and data loading.
- **{an}.Test.Logic/** — xUnit tests for business logic.
- **{an}.Test.React/** — Selenium UI tests.
- **{an}.Test.Environment/** — Shared test setup and database configuration.
- **Framework/** — Signum Framework git submodule (do not modify directly from this repo).

## Key Files
- `{an}/Starter.cs` — Central bootstrapping. Registers all framework extensions and app modules via `Start()`.
- `{an}/MainAdmin.tsx` — Imports and starts all module clients.
- `{an}/Layout.tsx` — Main application shell (navbar, sidebar, modals).
- `{an}.Server/Program.cs` — Server entry point, calls `Starter.Start()`.
- `Modules.xml` — Configuration for optional/removable modules.

## Build & Run
- **C#:** `dotnet build {an}/{an}.csproj` (not the entire solution).
- **TypeScript:** `yarn tsgo --build` from the {an} folder.
- **Dev server:** `yarn dev` from {an}.Server (Vite on port 3000).
- **Tests:** `dotnet test {an}.Test.Logic/{an}.Test.Logic.csproj`.
""";
        });

        // Simplify .github/copilot-instructions.md
        uctx.ChangeCodeFile(@".github/copilot-instructions.md", cf =>
        {
            cf.Content = """
# GitHub Copilot Repository Instructions

Read **[`AGENTS.md`](AGENTS.md)** and **[`Framework/AGENTS.md`](Framework/AGENTS.md)** for all project conventions.
""";
        });

        // Simplify .opencode-instructions.md
        uctx.ChangeCodeFile(".opencode-instructions.md", cf =>
        {
            cf.Content = """
# OpenCode Instructions

Read **[`AGENTS.md`](AGENTS.md)** and **[`Framework/AGENTS.md`](Framework/AGENTS.md)** for all project conventions.
""";
        });

        // Simplify CLAUDE.md
        uctx.ChangeCodeFile("CLAUDE.md", cf =>
        {
            cf.Content = """
# Claude Code Instructions

Read **[`AGENTS.md`](AGENTS.md)** and **[`Framework/AGENTS.md`](Framework/AGENTS.md)** for all project conventions.
""";
        });
    }
}
