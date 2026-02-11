using Signum.Utilities;
using System.IO;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20260206_UpdateCopilotInstructions2 : CodeUpgradeBase
{
    public override string Description => "Create AGENTS.md and update AI assistant instruction files to reference it";

    public override void Execute(UpgradeContext uctx)
    {
        // Create AGENTS.md with shared instructions
        uctx.CreateCodeFile("AGENTS.md", $"""
# AI Agent Instructions for {uctx.ApplicationName}

This document contains shared instructions for AI coding assistants (GitHub Copilot, OpenCode, etc.) working on the {uctx.ApplicationName} project.

---

## Base Instructions
**This repository uses Signum Framework.** 

For general Signum Framework conventions, **see `Framework/.github/copilot-instructions.md`** first.

The instructions below provide {uctx.ApplicationName}-specific overrides and additions.

---

## Project Overview
- **Project Name:** {uctx.ApplicationName}
- **Main Project:** {uctx.ApplicationName}/{uctx.ApplicationName}.csproj
- **.NET Version:** 10.0
- **Framework:** Signum Framework (git submodule at `Framework/`)

## {uctx.ApplicationName}-Specific Overrides

(Add your {uctx.ApplicationName}-specific coding conventions, patterns, and guidelines here)

---

## Common Patterns

(Document common patterns specific to this project)

---

## Important Notes

(Add any important notes or warnings for AI assistants working on this codebase)
""");

        // Update .github/copilot-instructions.md to reference AGENTS.md
        uctx.ChangeCodeFile(@".github/copilot-instructions.md", cf => { cf.Content = """
# GitHub Copilot Repository Instructions

**For complete AI agent instructions, see [`AGENTS.md`](../AGENTS.md)** in the repository root.

This file references the shared instructions used by all AI coding assistants (GitHub Copilot, OpenCode, etc.) working on this project.
"""; });

        // Create .opencode-instructions.md to reference AGENTS.md
        uctx.CreateCodeFile(".opencode-instructions.md", """
# OpenCode Instructions

**For complete AI agent instructions, see [`AGENTS.md`](AGENTS.md)** in the repository root.

This file references the shared instructions used by all AI coding assistants (GitHub Copilot, OpenCode, etc.) working on this project.
""");

        // Create CLAUDE.md to reference AGENTS.md
        uctx.CreateCodeFile("CLAUDE.md", """
# Claude Instructions

**For complete AI agent instructions, see [`AGENTS.md`](AGENTS.md)** in the repository root.

This file references the shared instructions used by all AI coding assistants (GitHub Copilot, OpenCode, Claude, etc.) working on this project.
""");
    }
}
