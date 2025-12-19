using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Task = System.Threading.Tasks.Task;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;
using System.Linq;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace Signum.TSCBuild
{
    internal sealed class RunTSGenerator
    {
        public const int RunTSGeneratorCommandId = 0x0112;
        public static readonly Guid CommandSet = new Guid("57018ec6-5e1b-4ac7-8226-30120d45e7c0");
        private readonly AsyncPackage package;

        private RunTSGenerator(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var buildTSGenCommandID = new CommandID(CommandSet, RunTSGeneratorCommandId);
            var buildTSGenMenuItem = new OleMenuCommand(this.ExecuteRunTSGenerator, buildTSGenCommandID);
            buildTSGenMenuItem.BeforeQueryStatus += BuildTSGenMenuItem_BeforeQueryStatus;
            commandService.AddCommand(buildTSGenMenuItem);
        }

        private void BuildTSGenMenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var menuCommand = sender as OleMenuCommand;
            menuCommand.Visible = menuCommand.Enabled = IsSingleCsprojSelected();
        }

        private bool IsSingleCsprojSelected()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var monitorSelection = ServiceProvider.GetServiceAsync(typeof(SVsShellMonitorSelection)).GetAwaiter().GetResult() as IVsMonitorSelection;
            if (monitorSelection == null)
                return false;

            monitorSelection.GetCurrentSelection(out var hierarchyPtr, out var itemid, out _, out _);
            if (hierarchyPtr == IntPtr.Zero || itemid == (uint)VSConstants.VSITEMID.Nil)
                return false;

            var hierarchy = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
            if (hierarchy == null)
                return false;

            hierarchy.GetCanonicalName(itemid, out var itemFullPath);
            if (itemFullPath == null || !itemFullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private async void ExecuteRunTSGenerator(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var projectFile = GetSelectedProjectFile();
            if (projectFile == null)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "No .csproj project selected.",
                    "Run TSGenerator",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            await BuildProjectWithTSGeneratorEnabledAsync(projectFile);
        }

        private string GetSelectedProjectFile()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var monitorSelection = ServiceProvider.GetServiceAsync(typeof(SVsShellMonitorSelection)).GetAwaiter().GetResult() as IVsMonitorSelection;
            if (monitorSelection == null)
                return null;

            monitorSelection.GetCurrentSelection(out var hierarchyPtr, out var itemid, out _, out _);
            if (hierarchyPtr == IntPtr.Zero || itemid == (uint)VSConstants.VSITEMID.Nil)
                return null;

            var hierarchy = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
            if (hierarchy == null)
                return null;

            hierarchy.GetCanonicalName(itemid, out var itemFullPath);

            return itemFullPath;
        }

        private async Task BuildProjectWithTSGeneratorEnabledAsync(string projectFile)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var pane = await GetOutputPaneAsync();
                pane.Activate();
                pane.OutputStringThreadSafe("--- Run TSGenerator started ---\n");

                string projectDir = Path.GetDirectoryName(projectFile);

                // Invoke dotnet build with the TSGeneratorDisabled=false property
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "dotnet",
                        //Arguments = $"build \"{projectFile}\" -p:TSGeneratorDisabled=false",
                        Arguments = $"build -p:TSGeneratorDisabled=false",
                        WorkingDirectory = projectDir,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                StringBuilder sb = new StringBuilder();
                var tcs = new TaskCompletionSource<bool>();

                process.OutputDataReceived += (s, e) =>
                {
                    try
                    {
                        if (e.Data != null)
                        {
                            ThreadHelper.JoinableTaskFactory.Run(async () =>
                            {
                                pane.OutputStringThreadSafe(e.Data + "\n");
                                sb.AppendLine(e.Data);
                                await Task.CompletedTask;
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowError(ex);
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                        ThreadHelper.JoinableTaskFactory.Run(() => { pane.OutputStringThreadSafe(e.Data + "\n"); return Task.CompletedTask; });
                };

                process.Exited += (s, e) =>
                {
                    tcs.TrySetResult(true);
                };

                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    pane.OutputStringThreadSafe($"Failed to start 'dotnet': {ex.Message}\n");
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "Could not start 'dotnet'. Ensure the .NET SDK/CLI is installed and available on PATH.",
                        "Run TSGenerator",
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await tcs.Task; // Wait for process to exit

                pane.OutputStringThreadSafe("--- Run TSGenerator finished ---\n");

                if (process.ExitCode != 0)
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "TSGenerator run failed. See Output window for details.",
                        "Run TSGenerator Error",
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task<IVsOutputWindowPane> GetOutputPaneAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var outputWindow = await ServiceProvider.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
            Guid paneGuid = new Guid("c7c7c7c7-c7c7-c7c7-c7c7-c7c7c7c7c7c7"); // Unique GUID for TSGenerator
            string paneTitle = "Run TSGenerator";
            outputWindow.CreatePane(ref paneGuid, paneTitle, 1, 1);
            outputWindow.GetPane(ref paneGuid, out IVsOutputWindowPane pane);
            return pane;
        }

        private void ShowError(Exception ex)
        {
            VsShellUtilities.ShowMessageBox(
                this.package,
                $"An error occurred during TSGenerator run: {ex.Message}\nStackTrace:\n{ex.StackTrace}",
                "Run TSGenerator Error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public static RunTSGenerator Instance { get; private set; }
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new RunTSGenerator(package, commandService);
        }

       
    }
}
