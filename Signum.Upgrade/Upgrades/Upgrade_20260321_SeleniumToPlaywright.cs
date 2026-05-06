using LibGit2Sharp;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20260321_SeleniumToPlaywright : CodeUpgradeBase
{
    public override string Description => "Convert Selenium tests to Playwright by making methods async";

    //language = regex
    static string identifier = @"[a-zA-Z_][a-zA-Z0-9_.]*";

    //language = regex
    static string methodDeclaraton = @"^(?>(?<prefix>\s*(?:(?:public|private|protected|internal|static|virtual|override|abstract|sealed|async)\s+)*))(?<returnType>{identifier}(?:<[^>]+>)?)\s+(?<methodName>{identifier})(?<genericParams><[^>]+>)?\s*\((?<params>[^)]*)\)\s*$"
            .Replace("{identifier}", identifier);

    static readonly Regex MethodDeclarationRegex = new Regex(methodDeclaraton, RegexOptions.Compiled);


    public override void Execute(UpgradeContext uctx)
    {
        uctx.ChangeCodeFile("Southwind.sln", file =>
        {
            //file.Solution_RemoveProject(@"Framework\Extensions\Signum.Selenium\Signum.Selenium.csproj");
            file.Solution_AddProject(@"Framework\Extensions\Signum.Playwright\Signum.Playwright.csproj", "2.Extensions");
        });

        // Update project references from Selenium to Playwright
        uctx.ChangeCodeFile(@"Southwind.Test.React\Southwind.Test.React.csproj", file =>
        {
            file.Replace("Signum.Selenium", "Signum.Playwright");
            file.RemoveAllLines(a => a.Contains("Selenium.WebDriver"));
        }, WarningLevel.Warning);

        // Update GlobalUsings.cs
        uctx.ChangeCodeFile(@"Southwind.Test.React\Properties\GlobalUsings.cs", file =>
        {
            // Remove Selenium global usings
            file.RemoveAllLines(l => l.Trim() == "global using Signum.Selenium;");
            file.RemoveAllLines(l => l.Trim() == "global using OpenQA.Selenium;");
            file.RemoveAllLines(l => l.Trim() == "global using OpenQA.Selenium.Interactions;");

            // Add Playwright global usings if not already present
            if (!file.Content.Contains("global using Signum.Playwright;"))
            {
                file.InsertAfterLastLine(l => l.StartsWith("global using"), @"global using Signum.Playwright;
global using Signum.Playwright.Frames;
global using Signum.Playwright.Search;
global using Signum.Playwright.LineProxies;
global using Signum.Playwright.ModalProxies;
global using Microsoft.Playwright;
global using System.Threading.Tasks;");
            }
        }, WarningLevel.Warning);

        string? browserCode = null;
        // Convert Browse method in Common.cs
        uctx.ChangeCodeFile(@"Southwind.Test.React\Common.cs", file =>
        {
            file.Replace($"public class {uctx.ApplicationName}TestClass", $"public class {uctx.ApplicationName}TestClass : SignumPlaywrightTestClass, IAsyncLifetime");

            file.ReplaceBetween(
                new(a => a.Contains("public static void Browse"), 0),
                new(a => a.Contains("}"), 0) { SameIdentation = true },
                text =>
                {
                    var match = Regex.Match(text, @"public static void Browse\(string username, Action<(?<br>\w+)> action");

                    var browserName = match.Success ? match.Groups["br"].Value : uctx.ApplicationName + "Browser";

                    return $$"""
                        public async ValueTask InitializeAsync()
                        {
                            Administrator.RestoreSnapshotOrDatabase();

                            using (var c = new HttpClient())
                            {
                                AssertClean200(await c.PostAsync(BaseUrl + "api/cache/invalidateAll", JsonContent.Create(new
                                {
                                    SecretHash = {{uctx.ApplicationName}}Environment.BroadcastSecretHash,
                                })));
                            }
                        }
                        
                        private static readonly Lazy<Task<IBrowser>> DefaultBrowser = new(async () =>
                        {
                            var playwright = await Microsoft.Playwright.Playwright.CreateAsync();

                            string? mode = GetPlaywrightMode();

                            return await GetBrowser(playwright, mode);
                        });

                        public async Task BrowseAsync(string username, Func<{{browserName}}, Task> action)
                        {
                            var browser = await DefaultBrowser.Value;

                            var page = await GetPageAsync(browser, []);

                            var browserProxy = new {{browserName}}(page);

                            try
                            {
                                page.SetDefaultTimeout(10000);
                                await browserProxy.LoginAsync(username, username);
                                CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = await browserProxy.GetCultureFromLoginDropdownAsync();
                                await action(browserProxy);
                            }
                            finally
                            {
                                if (!BrowserProxy.DebugMode)
                                    await page.CloseAsync();
                            }
                        }

                        """;
                }
                );

            file.ReplaceBetween(
                new ReplaceBetweenOption(a => a.Contains($"public class {uctx.ApplicationName}Browser")),
                new ReplaceBetweenOption(a => a.Contains("}")) { SameIdentation = true },
                text =>
                {
                    browserCode = text;
                    return "";
                });
        });

        uctx.MoveFile(@"Southwind.Test.React\Common.cs", @"Southwind.Test.React\SouthwindTestClass.cs");

        if (browserCode != null)
            uctx.CreateCodeFile(@"Southwind.Test.React\SouthwindBrowser.cs", browserCode);

        uctx.ForeachCodeFile(@"*.cs", uctx.TestReactDirectory, file =>
        {
            file.RemoveAllLines(a => a.Contains("using OpenQA"));
            file.ProcessLines(lines =>
            {
                bool changed = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = SimpleTypeReplacements(lines[i]);
                    if (line != lines[i])
                    {
                        lines[i] = line;
                        changed = true;
                    }
                }
                return changed;
            });
        });
        
        ConvertToAsyncPlaywright(uctx, isTest: false);
        ConvertToAsyncPlaywright(uctx, isTest: true);

        uctx.ForeachCodeFile(@"*.cs", uctx.TestReactDirectory, file =>
        {
            file.Replace(new Regex(/*language = regex*/@"async\s+(?<var>{identifier})\s*=>\s+await".Replace("{identifier}", identifier)), "${var} => ");
        });
    }

    private void ConvertToAsyncPlaywright(UpgradeContext uctx, bool isTest)
    {
        // Process all C# files to convert methods to async
        uctx.ForeachCodeFile(@"*.cs", uctx.TestReactDirectory, file =>
        {
            file.ReplaceBetweenAll(
                new(l => MethodDeclarationRegex.IsMatch(l) && !l.Contains("async"), 0),
                new(l => l.Contains("}"), 0) { SameIdentation = true },
                (oldText, ctx) =>
                {
                    var lines = oldText.Lines();

                    if (!(lines is [var methodDeclaration, ..var parametersBraceAndBody, var closeBrace]))
                    {
                        SafeConsole.WriteLineColor(ConsoleColor.Yellow, "Unexpected method format, skipping: " + lines[1]);
                        return oldText;
                    }
                    var pre = ctx.lines[ctx.from - 1];
                    if (isTest != (pre.Contains("[Test]") || pre.Contains("[Fact]")))
                        return oldText;

                    var openBraceIndex = parametersBraceAndBody.IndexOf(a => a.Trim() == "{");

                    if(openBraceIndex == -1)
                    {
                        SafeConsole.WriteLineColor(ConsoleColor.Yellow, "Could not find opening brace, skipping: " + lines[1]);
                        return oldText;
                    }

                    var parameters = parametersBraceAndBody[..openBraceIndex];
                    var openBrace = parametersBraceAndBody[openBraceIndex];
                    var body = parametersBraceAndBody[(openBraceIndex + 1)..];

                    var newBody = body.Select(line => ToAsyncMethod(line)).ToList();

                    if (body.SequenceEqual(newBody))
                        return oldText;

                    var newMethodDeclaration = ConvertMethodSignatureToAsync(methodDeclaration, out var oldMethodName);

                    if(!isTest)
                    {
                        MethodsToConvertAsync.Add(oldMethodName);
                    }

                    string[] newLines = [
                        newMethodDeclaration,
                        ..parameters,
                        openBrace,
                        ..newBody,
                        closeBrace
                        ];

                    var result = newLines.ToString("\n");

                    return result;
                });
        });
    }

    // Methods that should be converted to async
    static readonly HashSet<string> MethodsToConvertAsync = new HashSet<string>
    {
        // Wait Methods
        "WaitPresent",
        "WaitVisible",
        "WaitNotPresent",
        "WaitNotVisible",
        "WaitNoPresent",
        "WaitNoVisible",

        "WaitInitialSearchCompleted",
        "WaitReload",
        // Assert Methods
        "AssertPresent",
        "AssertNotPresent",
        "AssertVisible",
        "AssertNotVisible",
        "AssertElementPresent",
        "AssertElementNotPresent",
        "AssertElementVisible",
        "AssertElementNotVisible",

        // Interaction Methods
        "Click",
        "SafeClick",
        "ButtonClick",
        "DoubleClick",
        "ContextClick",
        "CaptureOnClick",
        "CaptureOnDoubleClick",
        "CaptureManyOnClick",

        // Input Methods
        "Fill",
        "SafeFill",
        "SafeSendKeys",
        "SetValue",
        "GetValue",
        "SetChecked",

        // Selection Methods
        //"Select",
        "SelectLabel",
        "SelectByValue",
        "SelectByPredicate",

        // Navigation Methods
        "ScrollTo",
        "LoseFocus",

        // Attribute/Property Methods
        "GetAttribute",
        "GetAttributeOrThrow",
        "GetId",
        "GetClasses",
        "HasClass",
        "ContainsText",
        "Value",

        // Entity/Frame Methods
        "View",
        "Find",
        "Create",
        "CreateInPlace",
        "Remove",
        "AutoComplete",
        "AutoCompleteBasic",
        "EntityInfo",
        "GetLite",
        "SetLite",


        // Line Container Methods - Value setters
        "CheckboxLineValue",
        "DateTimeLineValue",
        "EnumLineValue",
        "GuidLineValue",
        "NumberLineValue",
        "HtmlLineValue",
        "TextAreaLineValue",
        "TextBoxLineValue",
        "TimeLineValue",
        "AutoLineValue",
        "EntityLineValue",
        "EntityComboValue",
        "WaitRefresh",
        "WaitChanges",
        "WaitLoaded",

        // Line Container Methods - Checks
        "IsVisible",
        "IsPresent",

        // EntityTable Methods
        "CreateRow",
        "LastRow",

        // Custom/Extension Methods (Spitzlei specific)
        "AutoCompleteAdresse",
        "FindTarif",
        "FindAllTarif",
        "SearchPageInFahrterfassung",

        // Modal Methods
        "CaptureModal",
        "OkWaitClosed",
        "ClickWaitClose",
        "Close",

        // Search/Query Methods
        "SearchPage",
        "FramePage",
        "EntityClick",
        "EntityClickInPlace",

        // Operation Methods
        "Execute",
        "ExecuteClick",

        "ConstructFrom",
        "ConstructFromMany",

        // Authentication Methods
        "Login",
        "Logout",
        "GetCurrentUser",
        "SetCurrentCulture",

        "SelectClick",
        "WaitSearchCompleted",
        "WaitInitialSearchCompleted",
        "AddQuickFilter",

        "SelectRow",
        "SelectRow",
        "SelectAllRows",
        "SelectAndCapture",
        "OperationClickCapture",

        "ToggleFilters",

        "EntityContextMenu",
        "OkWaitFrameModal",
        "SelectIndex",
        "GetColumnIndex",
        "AllRows",
        "GetValueUntyped",

    };

    static Dictionary<string, string> CustomMethodReplacements = new Dictionary<string, string>
    {
        { "CapturePopup", "CaptureModalAsync" },
        { "CaptureManyPopup", "CaptureManyModalPopup" },
        { "WaitElementPresent", "WaitPresentAsync" },
        { "WaitElementVisible", "WaitVisibleAsync" },
        { "WaitElementNotPresent", "WaitNoPresentAsync"},
        { "WaitElementNotVisible", "WaitNoVisibleAsync" },
        { "SafeSendKeys", "SafeFillAsync" }
    };

    private string ToAsyncMethod(string line)
    {
        var result = line;

        // Simple type replacements (Selenium -> Playwright)
        result = SimpleAsyncReplacements(result);

        // 1. Browse("xxx", -> await Browse("xxx", async
        result = Regex.Replace(result, @"\bBrowse\s*\(\s*""(?<user>[^""]+)""\s*, ", @"await BrowseAsync(""${user}"", async ", RegexOptions.None);

        // 2. Using( -> .UsingAsync(async
        result = Regex.Replace(result, @"\.Using\s*\(", ".Then(async ", RegexOptions.None);

        result = Regex.Replace(result, @"\.Do\s*\(", ".Then(async ", RegexOptions.None);
        result = Regex.Replace(result, @"\.WaitRefresh\s*\(", ".WaitRefreshAsync(async ", RegexOptions.None);

        // 3. EndUsing( -> .EndUsingAsync(async
        result = Regex.Replace(result, @"\.EndUsing\s*\(", ".Then(async ", RegexOptions.None);

        result = Regex.Replace(result, @"\.CreateAndSelect\s*\(", ".CreateAndSelectAsync(async ", RegexOptions.None);

        result = Regex.Replace(result, @"\.Wait\s*\(", ".WaitAsync(async ", RegexOptions.None);

        result = Regex.Replace(result, @"\.(AsMessageModal|AsSearchModal|AsFrameModal<\w+>|AsAutoLineModal|AsSelectorModal)\(\)", ".Then(a => a.$1())", RegexOptions.None);




        // 4. xxxxx.SomeMethod( -> await xxxxx.SomeMethodAsync(
        // Build a single regex pattern with all method names using alternation (|)


        foreach (var first in Regex.Matches(result, @"\b\w+(?=[<(])").Where(m => MethodsToConvertAsync.Contains(m.Value) || CustomMethodReplacements.ContainsKey(m.Value)).ToList())
        {
            var pattern = new Regex(expr + "?" + $@"(?<method>{first.Value})\s*(?<genericParams><[^>]+>)?\s*\(");

            var newResult = pattern.Replace(result, m =>
            {
                var expr = m.Groups["expr"].Value;
                var method = m.Groups["method"].Value;
                var genericParams = m.Groups["genericParams"].Value;

                var newMethod = CustomMethodReplacements.TryGetC(method) ?? method + "Async";

                if (expr.Trim() == "" && result.Contains("." + m.Value))
                    return $"{newMethod}{genericParams}(";

                return $"await {expr}{newMethod}{genericParams}(";
            });

            if(result == newResult)
            {
                
            }
            else
            {
                result = newResult;
            }
        }

        if (line == result)
            return line;

        return result;
    }

    static Dictionary<string, string> propertyToMethod = new Dictionary<string, string>
    {
        { "Text", "TextContentAsync" },
        { "Displayed", "IsVisibleAsync" },
        { "Enabled", "IsEnabledAsync" },
        { "Selected", "IsCheckedAsync" },
    };

    static Dictionary<string, string> methodToMethod = new Dictionary<string, string>
    {
        { "SendKeys", "FillAsync" },
        { "Click", "ClickAsync" },
        { "GetAttribute", "GetAttributeAsync" },
        { "GetDomAttribute", "GetAttributeAsync" },
        { "GetDomProperty", "GetAttributeAsync" },
    };

    // language=regex
    static string parens = @"(?:\((?>(?:[^()]+|(?<open>\()|(?<-open>\))))*(?(open)(?!))\))";

    // language=regex
    static string generic = @"(?:<[^>]+>)";

    // language=regex
    static string expr = @"(?<expr>(new )?\b{identifier}{generic}?{parens}?(?:!?\.{identifier}{generic}?{parens}?)*!?\.)"
        .Replace("{generic}", generic)
        .Replace("{parens}", parens)
        .Replace("{identifier}", identifier);

    private string SimpleAsyncReplacements(string line)
    {
        var result = line;


        foreach (var kvp in propertyToMethod)
        {
            if (line.Contains(kvp.Key))
                result = Regex.Replace(result, expr + @$"{kvp.Key}\b", @"await ${expr}" + kvp.Value + "()", RegexOptions.None);
        }


        foreach (var kvp in methodToMethod)
        {
            if (line.Contains(kvp.Key))
                result = Regex.Replace(result, expr + @$"{kvp.Key}\(", @"await ${expr}" + kvp.Value + "(", RegexOptions.None);
        }

        // Clear -> Fill with empty string
        if (line.Contains("Clear"))
            result = Regex.Replace(result, expr + @"Clear\(\)", ".FillAsync(\"\")", RegexOptions.None);

        if (line.Contains("Url"))
            result = Regex.Replace(result, expr + @"Url\s+=\s+(?<url>[^;]+);", "await ${expr}GotoAsync(${url});", RegexOptions.None);


        return result;
    }

    private string SimpleTypeReplacements(string line)
    {
        var result = line;



        // Core Selenium types -> Playwright types
        result = Regex.Replace(result, @"\bSelenium\b", "Page", RegexOptions.None);

        result = Regex.Replace(result, @"\bWebElementLocator\b", "ILocator", RegexOptions.None);
        result = Regex.Replace(result, @"\bIWebElement\b", "ILocator", RegexOptions.None);
        result = Regex.Replace(result, @"\bWebDriver\b", "IPage", RegexOptions.None);
        result = Regex.Replace(result, @"\bIWebDriver\b", "IPage", RegexOptions.None);
        result = Regex.Replace(result, @"\bRemoteWebDriver\b", "IPage", RegexOptions.None);

        // Selenium namespace -> Playwright namespace
        result = Regex.Replace(result, @"\busing\s+OpenQA\.Selenium;", "using Microsoft.Playwright;", RegexOptions.None);
        result = Regex.Replace(result, @"\busing\s+OpenQA\.Selenium\.Support\.UI;", "using Microsoft.Playwright;", RegexOptions.None);
        result = Regex.Replace(result, @"\busing\s+Signum\.Selenium\b", "using Signum.Playwright", RegexOptions.None);

        // By locators -> CSS selectors (simple cases)
        result = Regex.Replace(result, @"\bBy\.Id\(([^)]+)\)", "\"#\" + $1", RegexOptions.None);
        result = Regex.Replace(result, @"\bBy\.ClassName\(([^)]+)\)", "\".\" + $1", RegexOptions.None);
        result = Regex.Replace(result, @"\bBy\.CssSelector\(([^)]+)\)", "$1", RegexOptions.None);
        result = Regex.Replace(result, @"\bBy\.Name\(([^)]+)\)", "\"[name=\" + $1 + \"]\"", RegexOptions.None);

        // FindElement -> Locator
        result = Regex.Replace(result, @"\.FindElement\(", ".Locator(", RegexOptions.None);
        result = Regex.Replace(result, @"\.FindElements\(", ".Locator(", RegexOptions.None);

        result = Regex.Replace(result, @"\.GetDriver\(\)", ".Page", RegexOptions.None);

        result = Regex.Replace(result, @"\.(?<method>WaitElementPresent|WaitElementVisible)\s*\(\s*""(?<loc>(?>[^""]*))""\s*\)", @".Locator(""${loc}"").${method}()", RegexOptions.None);

        result = Regex.Replace(result, @"\b(Find|WaitPresent|WaitVisible|WaitElementVisible|WaitElementPresent)\(\)\s*\.\s*(?<method>(Click|SafeSendKeys|CaptureOnClick))\s*\(", "${method}(", RegexOptions.None);

        return result;
    }

    string ConvertMethodSignatureToAsync(string firstLine, out string oldMethodName)
    {
        var match = MethodDeclarationRegex.Match(firstLine);
        if (!match.Success)
            throw new InvalidOperationException("Unexpected method declaration format: " + firstLine);

        var prefix = match.Groups["prefix"].Value;
        var returnType = match.Groups["returnType"].Value;
        var methodName = match.Groups["methodName"].Value;
        var genericParams = match.Groups["genericParams"].Value; // Can be empty string
        var parameters = match.Groups["params"].Value;

        oldMethodName = methodName;

        // Convert return type to async Task or async Task<T>
        string newReturnType;
        if (returnType == "void")
        {
            newReturnType = "async Task";
        }
        else
        {
            newReturnType = $"async Task<{returnType}>";
        }

        // Add "Async" suffix to method name if it doesn't end with "Test"
        string newMethodName = methodName;
        if (!methodName.EndsWith("Test"))
        {
            newMethodName = methodName + "Async";
        }

        // Reconstruct the method signature
        return $"{prefix}{newReturnType} {newMethodName}{genericParams}({parameters})";
    }

}
