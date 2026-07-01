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

namespace Signum.VSIX
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CompileTypeScript
    {
        public const int CommandId = 0x0100;
        public const int BuildTypeScriptCommandId = 0x0111;
        public static readonly Guid CommandSet = new Guid("57018ec6-5e1b-4ac7-8226-30120d45e7c0");
        private readonly AsyncPackage package;
        private readonly ErrorListProvider errorListProvider;

        private CompileTypeScript(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            errorListProvider = new ErrorListProvider(this.package);

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            var buildTypeScriptCommandID = new CommandID(CommandSet, BuildTypeScriptCommandId);
            var buildTypeScriptMenuItem = new OleMenuCommand(this.ExecuteBuildTypeScript, buildTypeScriptCommandID);
            buildTypeScriptMenuItem.BeforeQueryStatus += BuildTypeScriptMenuItem_BeforeQueryStatus;
            commandService.AddCommand(buildTypeScriptMenuItem);
        }

        private void BuildTypeScriptMenuItem_BeforeQueryStatus(object sender, EventArgs e)
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
            return itemFullPath != null && itemFullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
        }

        private async void ExecuteBuildTypeScript(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var projectFile = GetSelectedProjectFile();
            if (projectFile == null)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "No .csproj project selected.",
                    "Build TypeScript",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }
            await BuildTypeScriptAsync(projectFile);
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

        private async Task<IVsOutputWindowPane> GetTypeScriptOutputPaneAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var outputWindow = await ServiceProvider.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
            Guid paneGuid = new Guid("b6b6b6b6-b6b6-b6b6-b6b6-b6b6b6b6b6b6"); // Unique GUID for TypeScript build
            string paneTitle = "Build TypeScript";
            outputWindow.CreatePane(ref paneGuid, paneTitle, 1, 1);
            outputWindow.GetPane(ref paneGuid, out IVsOutputWindowPane pane);
            return pane;
        }

        private async Task BuildTypeScriptAsync(string projectFile)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Clear previous TypeScript errors before each build
            errorListProvider.Tasks.Clear();

            // Show status bar animation, message, and progress
            IVsStatusbar statusBar = await ServiceProvider.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
            object icon = (short)5;
            statusBar?.SetText("Compiling TypeScript...");
            statusBar?.Animation(1, ref icon); // 1 = start
            uint cookie = 0;
            statusBar?.Progress(ref cookie, 1, "Compiling TypeScript...", 10, 100); // 10%

            // Get output pane and activate
            var pane = await GetTypeScriptOutputPaneAsync();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            pane.Activate();
            pane.OutputStringThreadSafe("--- TypeScript build started ---\n");

            string projectDir = System.IO.Path.GetDirectoryName(projectFile);
            string tsconfigPath = System.IO.Path.Combine(projectDir, "tsconfig.json");
            if (!System.IO.File.Exists(tsconfigPath))
            {
                statusBar?.Animation(0, ref icon);
                statusBar?.Progress(ref cookie, 0, "", 0, 0);
                statusBar?.SetText("");
                pane.OutputStringThreadSafe("No tsconfig.json found in the project directory. TypeScript build cannot proceed.\n");
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "No tsconfig.json found in the project directory. TypeScript build cannot proceed.",
                    "Build TypeScript",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            string yarnPath = @"C:\Program Files (x86)\Yarn\bin\yarn.cmd";
            if (!System.IO.File.Exists(yarnPath))
            {
                statusBar?.Animation(0, ref icon);
                statusBar?.Progress(ref cookie, 0, "", 0, 0);
                statusBar?.SetText("");
                pane.OutputStringThreadSafe("Could not find yarn.cmd at the expected location. Please ensure Yarn is installed.\n");
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "Could not find yarn.cmd at the expected location. Please ensure Yarn is installed.",
                    "Build TypeScript",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = yarnPath,
                    Arguments = "tsc -b",
                    WorkingDirectory = projectDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            StringBuilder sb = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    ThreadHelper.JoinableTaskFactory.Run(() => { pane.OutputStringThreadSafe(e.Data + "\n"); return Task.CompletedTask; });
                    sb.AppendLine(e.Data);
                }
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    ThreadHelper.JoinableTaskFactory.Run(() => { pane.OutputStringThreadSafe(e.Data + "\n"); return Task.CompletedTask; });
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            pane.OutputStringThreadSafe("--- TypeScript build finished ---\n");

            ShowTypeScriptErrorsInErrorList(projectDir, sb.ToString()); // Errors will be shown as before

            // Stop status bar animation and update message and progress
            statusBar?.Animation(0, ref icon); // 0 = stop
            statusBar?.Progress(ref cookie, 1, "Compiling TypeScript...", 100, 100); // 100%
            statusBar?.Progress(ref cookie, 0, "", 0, 0); // remove progress bar
            if (process.ExitCode == 0)
                statusBar?.SetText("TypeScript compilation succeeded.");
            else
                statusBar?.SetText("TypeScript compilation failed.");

            if (process.ExitCode != 0)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "TypeScript build failed. See Error List and Output window for details.",
                    "Build TypeScript Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private void ShowTypeScriptErrorsInErrorList(string projectDirectory, string tscOutput)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            errorListProvider.Tasks.Clear();

            var lines = tscOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            // Updated regex to capture the error code (TSxxxx)
            var regex = new System.Text.RegularExpressions.Regex(@"^(?<file>[^\(]+)\((?<line>\d+),(?<col>\d+)\): (?<level>error|warning) (?<code>TS\d+): (?<message>.*)$");
            foreach (var line in lines)
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    string filePath = Path.Combine(projectDirectory, match.Groups["file"].Value.Trim());
                    int lineNum = int.Parse(match.Groups["line"].Value) - 1;
                    int colNum = int.Parse(match.Groups["col"].Value) - 1;
                    string code = match.Groups["code"].Value;
                    string message = match.Groups["message"].Value;

                    var task = new Microsoft.VisualStudio.Shell.ErrorTask
                    {
                        Document = filePath,
                        Line = lineNum,
                        Column = colNum,
                        Text = $"{code}: {message}",
                        ErrorCategory = match.Groups["level"].Value == "error" ? TaskErrorCategory.Error : TaskErrorCategory.Warning,
                        Category = TaskCategory.BuildCompile,
                        CanDelete = false // Prevent user from deleting build errors
                    };

                    // Attach navigation handler
                    task.Navigate += (s, e) =>
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();
                        VsShellUtilities.OpenDocument(this.package, filePath, Guid.Empty, out _, out _, out var windowFrame);
                        if (windowFrame != null)
                        {
                            windowFrame.Show();
                            var textView = VsShellUtilities.GetTextView(windowFrame);
                            if (textView != null)
                            {
                                textView.SetCaretPos(lineNum, colNum);
                                textView.CenterLines(lineNum, 1);
                            }
                        }
                    };

                    errorListProvider.Tasks.Add(task);
                }
            }
            errorListProvider.Show();
        }

        public static CompileTypeScript Instance { get; private set; }
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new CompileTypeScript(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "CompileTypeScript";
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
