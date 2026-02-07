using Signum.Utilities;
using System.IO;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20260206_UpdateCopilotInstructions2 : CodeUpgradeBase
{
    public override string Description => "Update .github/copilot-instructions.md simplified with reference to framework";

    public override void Execute(UpgradeContext uctx)
    {
        var content = $"""
# GitHub Copilot Repository Instructions

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

""";
        var fileName = @".github/copilot-instructions.md";
        uctx.ChangeCodeFile(fileName, cf => { cf.Content = content; });
    }
}
