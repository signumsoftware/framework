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

namespace Signum.TSCBuild
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

        // --- Progress tracking fields ---
        private int? totalProjects = null;
        private int completedProjects = 0;
        private string lastBuildingProject = null;
        private List<string> projectList = new List<string>();

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
            if (itemFullPath == null || !itemFullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                return false;

            var projectDir = System.IO.Path.GetDirectoryName(itemFullPath);
            var tsconfigPath = System.IO.Path.Combine(projectDir, "tsconfig.json");
            return System.IO.File.Exists(tsconfigPath);
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

        private void ParseProjectsInBuild(List<string> lines)
        {
            projectList.Clear();
            totalProjects = 0;
            bool inProjectsSection = false;
            foreach (var line in lines)
            {
                if (line.Contains("Projects in this build:"))
                {
                    inProjectsSection = true;
                    continue;
                }
                if (inProjectsSection)
                {
                    if (line.Trim().StartsWith("*"))
                    {
                        var project = line.Trim().Substring(1).Trim();
                        projectList.Add(project);
                        totalProjects++;
                    }
                    else if (!string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("*"))
                    {
                        break;
                    }
                }
            }
        }

        // Helper: find the closest ancestor node_modules folder starting at startDir
        private string FindClosestNodeModules(string startDir)
        {
            var dir = startDir;
            while (!string.IsNullOrEmpty(dir))
            {
                var nm = Path.Combine(dir, "node_modules");
                if (Directory.Exists(nm))
                {
                    if (File.Exists(Path.Combine(dir, "yarn.lock")) || File.Exists(Path.Combine(dir, "package-lock.json")))
                        return nm;
                }
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        private async Task BuildTypeScriptAsync(string projectFile)
        {
            try
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
                //statusBar?.Progress(ref cookie, 1, "Compiling TypeScript...", 10, 100); // 10%

                // Get output pane and activate
                var pane = await GetTypeScriptOutputPaneAsync();
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                pane.Activate(); // Ensure pane is activated before any output
                pane.OutputStringThreadSafe("--- TypeScript build started ---\n");

                string projectDir = System.IO.Path.GetDirectoryName(projectFile);
                string tsconfigPath = System.IO.Path.Combine(projectDir, "tsconfig.json");
                if (!System.IO.File.Exists(tsconfigPath))
                {
                    statusBar?.Animation(0, ref icon);
                    statusBar?.Progress(ref cookie, 0, "", 0, 0);
                    statusBar?.SetText("");
                    pane.OutputStringThreadSafe("No tsconfig.json found in the project directory.\n");
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "No tsconfig.json found in the project directory.",
                        "Build TypeScript",
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                var paths = new[] {
                // Yarn global installs
                @"C:\Program Files (x86)\Yarn\bin\yarn.cmd",
                @"C:\Program Files\Yarn\bin\yarn.cmd",
                @"C:\Program Files\nodejs\yarn.cmd",
                // Yarn installed via npm (global)
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Roaming\npm\yarn.cmd"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Local\pmpm\yarn.cmd"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Roaming\pmpm\yarn.cmd"),

                // Node.js global installs
                @"C:\Program Files\nodejs\npm.cmd",
                // Node.js installed via nvm (Node Version Manager)
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Roaming\nvm\nodejs\npm.cmd"),
            };

                var bestPath = paths.FirstOrDefault(p => File.Exists(p));
                if (bestPath == null)
                {
                    statusBar?.Animation(0, ref icon);
                    statusBar?.Progress(ref cookie, 0, "", 0, 0);
                    statusBar?.SetText("");
                    pane.OutputStringThreadSafe($@"Could not find yarn.cmd or npm.cmd the expected location:
{string.Join("\n", paths.Select(a => "* " + a))}
Please ensure Yarn or NPM are installed.
");
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "Could not find yarn.cmd at the expected location. Please ensure Yarn is installed.",
                        "Build TypeScript",
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }



                statusBar?.Progress(ref cookie, 1, "Compiling TypeScript...", (uint)10, (uint)50);

                // Decide which tool to invoke (always call yarn/npm CLI as before)
                string toolName = "tsc"; // default
                var nodeModules = FindClosestNodeModules(projectDir);
                if (nodeModules != null)
                {
                    if (Directory.Exists(Path.Combine(nodeModules, "typescript")))
                        toolName = "tsc";
                    else if (Directory.Exists(Path.Combine(nodeModules, "@typescript", "native-preview")))
                        toolName = "tsgo";
                }

                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = bestPath,
                        Arguments = $"{toolName} -b \"{projectDir}/tsconfig.json\" -v",
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


                                foreach (var line in e.Data.Split('\n'))
                                {
                                    if (line.Contains("is up to date because") ||
                                        line.Contains("is out of date because"))
                                    {
                                        if (totalProjects == null)
                                        {
                                            var lines = sb.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                            ParseProjectsInBuild(new List<string>(lines));
                                            completedProjects = 0;
                                            statusBar?.Progress(ref cookie, 1, "Compiling TypeScript...", (uint)completedProjects, (uint)totalProjects);
                                        }

                                        if (lastBuildingProject != null)
                                        {
                                            completedProjects++;
                                            lastBuildingProject = null;
                                            statusBar?.Progress(ref cookie, 1, "Compiling TypeScript...", (uint)completedProjects, (uint)totalProjects);
                                        }

                                        if (line.Contains("is up to date because"))
                                        {
                                            completedProjects++;
                                            statusBar?.Progress(ref cookie, 1, "Compiling TypeScript...", (uint)completedProjects, (uint)totalProjects);
                                        }
                                    }

                                    var buildingMatch = System.Text.RegularExpressions.Regex.Match(line, @"Building project '(.+?)'");
                                    if (buildingMatch.Success)
                                    {
                                        // Increment for previous project (if not first)

                                        lastBuildingProject = buildingMatch.Groups[1].Value.Replace('\\', '/');
                                        // Show relative path if possible
                                        string relPath = lastBuildingProject;
                                        if (relPath.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase))
                                            relPath = relPath.Substring(projectDir.Length).TrimStart('\\', '/');
                                        statusBar?.SetText($"Building {relPath}");
                                    }
                                }
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

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await tcs.Task; // Wait for process to exit (non-blocking)

                pane.OutputStringThreadSafe("--- TypeScript build finished ---\n");

                ShowTypeScriptErrorsInErrorList(projectDir, sb.ToString()); // Errors will be shown as before

                // Stop status bar animation and update message and progress
                statusBar?.Animation(0, ref icon); // 0 = stop
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
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ShowError(Exception ex)
        {
            VsShellUtilities.ShowMessageBox(
                this.package,
                $@"An error occurred during TypeScript build: {ex.Message}
                    StackTrace: 
                    {ex.StackTrace}",
                "Build TypeScript Error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
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
                    string filePath = Path.Combine(projectDirectory, match.Groups["file"].Value.Trim().Replace("/", @"\"));
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
     
                        // Start a timer to remove all errors in the same file after 10 seconds
                        var timer = new System.Timers.Timer(10000) { AutoReset = false };
                        timer.Elapsed += (sender, args) =>
                        {
                            ThreadHelper.Generic.BeginInvoke(() =>
                            {
                                for (int i = errorListProvider.Tasks.Count - 1; i >= 0; i--)
                                {
                                    var t = errorListProvider.Tasks[i];
                                    if (string.Equals(t.Document, filePath, StringComparison.OrdinalIgnoreCase))
                                    {
                                        errorListProvider.Tasks.Remove(t);
                                    }
                                }
                            });
                            timer.Dispose();
                        };
                        timer.Start();
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
