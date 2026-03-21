using Signum.Utilities;
using System;
using System.Collections.Generic;

namespace Signum.Upgrade.Upgrades;

class Upgrade_20260321_SeleniumToPlaywright : CodeUpgradeBase
{
    public override string Description => "Convert Selenium tests to Playwright by making methods async";

    // Regex to match method declarations with named capturing groups
    // Assumes method declaration ends with ) on the same line
    // Excludes generic methods (no < or >)
    static readonly Regex MethodDeclarationRegex = new Regex(
        @"^(?<prefix>\s*(?:(?:public|private|protected|internal|static|virtual|override|abstract|sealed)\s+)*)(?<returnType>\w+)(?<rest>\s+\w+\s*\([^)]*\)\s*)$",
        RegexOptions.Compiled);


    public override void Execute(UpgradeContext uctx)
    {
        // Update project references from Selenium to Playwright
        uctx.ChangeCodeFile(@"Southwind.Test.React\Southwind.Test.React.csproj", file =>
        {
            file.Replace("Signum.Selenium", "Signum.Playwright");
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
global using Signum.Playwright.LineProxies;
global using Signum.Playwright.Search;
global using Microsoft.Playwright;
global using System.Threading.Tasks;");
            }
        }, WarningLevel.Warning);

        // Convert Browse method in Common.cs
        uctx.ChangeCodeFile(@"Southwind.Test.React\Common.cs", file =>
        {
            file.Replace(
    new Regex(@"public static void Browse\(string username, Action<(?<br>\w+)> action)"),
    @"public static async Task BrowseAsync(string username, Func<${br}, Task> action)");

            SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Common.BrowseAsync requires manual convertion! check Southwind code");

        }, WarningLevel.Warning);

        // Process all C# files to convert methods to async
        uctx.ForeachCodeFile(@"*.cs", uctx.TestReactDirectory, file =>
        {
            file.ReplaceBetween(
                new(l => MethodDeclarationRegex.IsMatch(l), 0),
                new(l => l.Contains("}"), 0) { SameIdentation = true },
                oldText =>
                {
                    var lines = oldText.Split('\n').ToList();

                    if (lines.Count < 4)
                        return oldText;

                    if (lines[1].Trim() != "{" || lines[lines.Count - 1].Trim() != "}")
                        return oldText;

                    var body = lines.Skip(2).Take(lines.Count - 3).ToList();

                    var newBody = body.Select(line => ToAsyncMethod(line)).ToList();

                    if (body.SequenceEqual(newBody))
                        return oldText;

                    var methodDeclaration = lines[0];

                    var indent = CodeFile.GetIndent(methodDeclaration);

                    var newMEthodDeclaration = indent + ConvertMethodSignatureToAsync(methodDeclaration);

                    string[] newLines = [
                        newMEthodDeclaration,
                        lines[1],
                        ..newBody,
                        lines[lines.Count - 1]
                        ];

                    return newLines.ToString("\n");
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
        "WaitElementPresent",
        "WaitElementVisible",
        "WaitElementNotPresent",
        "WaitElementNotVisible",
        "WaitInitialSearchCompleted",

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
        "Select",
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

        // Authentication Methods
        "Login",
        "Logout",
        "GetCurrentUser",
        "SetCurrentCulture",

        // Popup/Modal Capture
        "CapturePopup",
        "CaptureManyPopup",
    };


    private string ToAsyncMethod(string line)
    {
        var result = line;

        // Simple type replacements (Selenium -> Playwright)
        result = SimpleTypeReplacements(result);

        // 1. Browse("xxx", -> await Browse("xxx", async
        result = Regex.Replace(result, @"\bBrowse\s*\(", "await BrowseAsync(", RegexOptions.None);

        // 2. Using( -> .UsingAsync(async
        result = Regex.Replace(result, @"\b\.Using\s*\(", ".UsingAsync(async ", RegexOptions.None);

        // 3. EndUsing( -> .EndUsingAsync(async
        result = Regex.Replace(result, @"\b\.EndUsing\s*\(", ".EndUsingAsync(async ", RegexOptions.None);

        // 4. xxxxx.SomeMethod( -> await xxxxx.SomeMethodAsync(
        // Build a single regex pattern with all method names using alternation (|)
        var methodNamesPattern = string.Join("|", MethodsToConvertAsync.Select(Regex.Escape));
        var pattern = $@"(?<expr>(?:\w+|\([^)]*\))+)\.(?<method>{methodNamesPattern})\s*\(";

        result = Regex.Replace(result, pattern, m =>
        {
            var expr = m.Groups["expr"].Value;
            var method = m.Groups["method"].Value;
            return $"await {expr}.{method}Async(";
        });

        return result;
    }

    private string SimpleTypeReplacements(string line)
    {
        var result = line;

        // Core Selenium types -> Playwright types
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

        // Common Selenium properties -> Playwright equivalents
        result = Regex.Replace(result, @"\.Text\b", ".TextContentAsync()", RegexOptions.None);
        result = Regex.Replace(result, @"\.Displayed\b", ".IsVisibleAsync()", RegexOptions.None);
        result = Regex.Replace(result, @"\.Enabled\b", ".IsEnabledAsync()", RegexOptions.None);
        result = Regex.Replace(result, @"\.Selected\b", ".IsCheckedAsync()", RegexOptions.None);

        // SendKeys -> Fill
        result = Regex.Replace(result, @"\.SendKeys\(", ".FillAsync(", RegexOptions.None);

        // Click (simple standalone) -> ClickAsync
        result = Regex.Replace(result, @"\.Click\(\)", ".ClickAsync()", RegexOptions.None);

        // Clear -> Fill with empty string
        result = Regex.Replace(result, @"\.Clear\(\)", ".FillAsync(\"\")", RegexOptions.None);

        // Submit -> PressAsync("Enter")
        result = Regex.Replace(result, @"\.Submit\(\)", ".PressAsync(\"Enter\")", RegexOptions.None);

        // GetAttribute -> GetAttributeAsync
        result = Regex.Replace(result, @"\.GetAttribute\(", ".GetAttributeAsync(", RegexOptions.None);
        result = Regex.Replace(result, @"\.GetDomAttribute\(", ".GetAttributeAsync(", RegexOptions.None);
        result = Regex.Replace(result, @"\.GetDomProperty\(", ".GetAttributeAsync(", RegexOptions.None);

        return result;
    }

    string ConvertMethodSignatureToAsync(string firstLine)
    {
        var match = MethodDeclarationRegex.Match(firstLine);
        if (!match.Success)
            return firstLine;

        var prefix = match.Groups["prefix"].Value;
        var returnType = match.Groups["returnType"].Value;
        var rest = match.Groups["rest"].Value;

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

        // Extract method name and add "Async" suffix if it doesn't end with "Test"
        var methodNameMatch = Regex.Match(rest, @"^(\s+)(\w+)(\s*\([^)]*\)\s*)$");
        if (methodNameMatch.Success)
        {
            var beforeName = methodNameMatch.Groups[1].Value;
            var methodName = methodNameMatch.Groups[2].Value;
            var afterName = methodNameMatch.Groups[3].Value;

            if (!methodName.EndsWith("Test"))
            {
                methodName = methodName + "Async";
            }

            rest = beforeName + methodName + afterName;
        }

        return $"{prefix}{newReturnType}{rest}";
    }

}
