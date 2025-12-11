using Signum.Utilities;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20251207_SimplifyVSCode : CodeUpgradeBase
{
    public override string Description => "Simplify .vscode configuration file";

    public override void Execute(UpgradeContext uctx)
    {
        // Update .github/copilot-instructions.md to .NET 10.0
        uctx.ChangeCodeFile(@".github/copilot-instructions.md", file =>
        {
            file.Replace("- **.NET Version:** 9.0", "- **.NET Version:** 10.0");
        });

        uctx.ChangeCodeFile("Southwind.Server/appsettings.json", file =>
        {
            file.ReplaceLine(a => a.Contains(
                """
                "Default": "Warning"
                """),
                """
                "Default": "Information",
                "Microsoft.AspNetCore": "Warning"
                """);
        });

        // Update .vscode/launch.json
        uctx.ChangeCodeFile(@".vscode/launch.json", file =>
        {
            if (SafeConsole.Ask("Replace entire .vscode/launch.json with standard version for .NET 10?"))
                file.Content = """
            {
                "version": "0.2.0",
                "configurations": [
                    {
                        "name": "Southwind.Server",
                        "type": "coreclr",
                        "request": "launch",
                        "preLaunchTask": "dotnet build",
                        "program": "${workspaceFolder}/Southwind.Server/bin/Debug/net10.0/Southwind.Server.dll",
                        "cwd": "${workspaceFolder}/Southwind.Server",
                        "console": "integratedTerminal",
                        "serverReadyAction": {
                            "action": "openExternally",
                            "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
                        }
                    },
                    {
                        "name": "Southwind.Server (test)",
                        "type": "coreclr",
                        "request": "launch",
                        "preLaunchTask": "dotnet build",
                        "program": "${workspaceFolder}/Southwind.Server/bin/Debug/net10.0/Southwind.Server.dll",
                        "cwd": "${workspaceFolder}/Southwind.Server",
                        "console": "integratedTerminal",
                        "env": {
                            "ASPNETCORE_ENVIRONMENT": "test"
                        },
                        "serverReadyAction": {
                            "action": "openExternally",
                            "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
                        }
                    },
                    {
                        "name": "Southwind.Server (live)",
                        "type": "coreclr",
                        "request": "launch",
                        "preLaunchTask": "dotnet build",
                        "program": "${workspaceFolder}/Southwind.Server/bin/Debug/net10.0/Southwind.Server.dll",
                        "cwd": "${workspaceFolder}/Southwind.Server",
                        "console": "integratedTerminal",
                        "env": {
                            "ASPNETCORE_ENVIRONMENT": "live"
                        },
                        "serverReadyAction": {
                            "action": "openExternally",
                            "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
                        }
                    },
                    {
                        "name": "Southwind.Terminal",
                        "type": "coreclr",
                        "request": "launch",
                        "preLaunchTask": "dotnet build",
                        "program": "${workspaceFolder}/Southwind.Terminal/bin/Debug/net10.0/Southwind.Terminal.dll",
                        "cwd": "${workspaceFolder}/Southwind.Terminal/bin/Debug/net10.0",
                        "console": "integratedTerminal"
                    },
                    {
                        "name": "Southwind.Terminal (test)",
                        "type": "coreclr",
                        "request": "launch",
                        "preLaunchTask": "dotnet build",
                        "program": "${workspaceFolder}/Southwind.Terminal/bin/Debug/net10.0/Southwind.Terminal.dll",
                        "cwd": "${workspaceFolder}/Southwind.Terminal/bin/Debug/net10.0",
                        "console": "integratedTerminal",
                        "env": {
                            "ASPNETCORE_ENVIRONMENT": "test"
                        }
                    },
                    {
                        "name": "Southwind.Terminal (live)",
                        "type": "coreclr",
                        "request": "launch",
                        "preLaunchTask": "dotnet build",
                        "program": "${workspaceFolder}/Southwind.Terminal/bin/Debug/net10.0/Southwind.Terminal.dll",
                        "cwd": "${workspaceFolder}/Southwind.Terminal/bin/Debug/net10.0",
                        "console": "integratedTerminal",
                        "env": {
                            "ASPNETCORE_ENVIRONMENT": "live"
                        }
                    },
                    {
                        "name": "Signum.Upgrade",
                        "type": "coreclr",
                        "request": "launch",
                        "preLaunchTask": "build_signum_upgrade",
                        "program": "${workspaceFolder}/Framework/Signum.Upgrade/bin/Debug/net10.0/Signum.Upgrade.dll",
                        "cwd": "${workspaceFolder}/Framework/Signum.Upgrade/bin/Debug/net10.0",
                        "console": "integratedTerminal"
                    },
                    {
                        "name": ".NET Core Attach",
                        "type": "coreclr",
                        "request": "attach"
                    }
                ]
            }
            """.Replace("Southwind", uctx.ApplicationName);
        });

        // Update .vscode/tasks.json
        uctx.ChangeCodeFile(@".vscode/tasks.json", file =>
        {
            if (SafeConsole.Ask("Replace entire .vscode/tasks.json with standard version?"))
                file.Content = """
            {
                // See https://go.microsoft.com/fwlink/?LinkId=733558
                // for the documentation about the tasks.json format
                "version": "2.0.0",
                "tasks": [
                    {
                        "label": "dotnet build",
                        "type": "shell",
                        "options": {
                            "cwd": "${workspaceFolder}/"
                        },
                        "windows": {
                            "command": "dotnet build"
                        },
                        "group": "build",
                        "problemMatcher": []
                    },
                    {
                        "label": "yarn tsgo",
                        "type": "shell",
                        "options": {
                            "cwd": "${workspaceFolder}/Southwind.Server"
                        },
                        "windows": {
                            "command": "yarn tsgo -b"
                        },
                        "group": "build",
                        "problemMatcher": []
                    },
                    {
                        "label": "yarn dev",
                        "type": "shell",
                        "options": {
                            "cwd": "${workspaceFolder}/Southwind.Server"
                        },
                        "group": "build",
                        "windows": {
                            "command": "yarn dev"
                        },
                        "problemMatcher": []
                    },
                    {
                        "label": "yarn install",
                        "type": "shell",
                        "options": {
                            "cwd": "${workspaceFolder}/Southwind.Server"
                        },
                        "windows": {
                            "command": "yarn"
                        },
                        "problemMatcher": []
                    }
                ]
            }
            """.Replace("Southwind", uctx.ApplicationName);

        });

        // Simplify .vscode/settings.json
        uctx.ChangeCodeFile(@".vscode/settings.json", file =>
        {
            if (SafeConsole.Ask("Replace entire .vscode/settings.json with standard version?"))
                file.Content = """
            {
                "files.exclude": {
                    "**/.git": true,
                    "**/bin": true,
                    "**/obj": true,
                    "**/node_modules": true,
                    "**/ts_out": true,
                },
                "editor.detectIndentation": false,
                "editor.formatOnSave": true,
                "[javascript]": {
                    "editor.tabSize": 2,
                    "editor.insertSpaces": true
                },
                "[typescript]": {
                    "editor.tabSize": 2,
                    "editor.insertSpaces": true
                },
                "[javascriptreact]": {
                    "editor.tabSize": 2,
                    "editor.insertSpaces": true
                },
                "[typescriptreact]": {
                    "editor.tabSize": 2,
                    "editor.insertSpaces": true
                },
                "[css]": {
                    "editor.tabSize": 2,
                    "editor.insertSpaces": true
                }
            }
            """.Replace("Southwind", uctx.ApplicationName);

        });
    }
}
