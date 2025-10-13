using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml.Linq;

namespace Signum.Upgrade;

public enum WarningLevel
{
    None,
    Warning,
    Error,
}

public class CodeFile
{
    public WarningLevel WarningLevel { get; set; }

    public CodeFile(string filePath, UpgradeContext uctx)
    {
        this.FilePath = filePath;
        this.Uctx = uctx;
    }

    public override string ToString() => FilePath;

    public string FilePath { get; private set; }
    public string? newFilePath;
    public UpgradeContext Uctx { get; }

    string? _content;
    string? _originalContent;
    public string Content
    {
        get { ReadIfNecessary(); return _content!; }
        set { _content = value; }
    }

    Encoding? encoding;

    private void ReadIfNecessary()
    {
        if (_content == null)
        {
            var bytes = File.ReadAllBytes(Path.Combine(Uctx.RootFolder, FilePath));
            encoding = GetEncoding(FilePath, bytes);
            _originalContent = _content = encoding.GetString(bytes[encoding.Preamble.Length..]);
        }
    }

    public void SaveIfNecessary()
    {
        if (_content != null && _content != _originalContent)
        {
            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "Modified " + FilePath);
            File.WriteAllText(Uctx.AbsolutePath(FilePath), _content, encoding!);
        }

        if(newFilePath != null)
        {
            var newPath = Uctx.AbsolutePathSouthwind(newFilePath);
            var oldPath = Uctx.AbsolutePath(FilePath);

            Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);

            File.Move(oldPath, newPath);

            if (Directory.EnumerateFiles(Path.GetDirectoryName(oldPath)!).IsEmpty())
                Directory.Delete(Path.GetDirectoryName(oldPath)!);

            SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, "Moved " + FilePath + " to " + newFilePath);


            FilePath = newFilePath;
            newFilePath = null;
        }
    }

    internal static Encoding GetEncoding(string filePath, byte[]? bytes)
    {
        if (bytes != null)
            return DetectUTFBOM(bytes);

        switch (Path.GetExtension(filePath))
        {
            case ".csproj":
            case ".cs": return Encoding.UTF8;
            default: return UTF8NoBOM;
        }
    }

    public static Encoding DetectUTFBOM(byte[] bytes)
    {
        if (bytes.Length >= Encoding.UTF8.Preamble.Length && Encoding.UTF8.Preamble.SequenceEqual(bytes[0..Encoding.UTF8.Preamble.Length]))
            return Encoding.UTF8;

        return UTF8NoBOM;
    }

    static readonly Encoding UTF8NoBOM = new UTF8Encoding(false);



    WarningLevel? isFirstWarning = null;
    public void Warning(FormattableString message)
    {
        if (WarningLevel == WarningLevel.None)
            return;

        if (Uctx.HasWarnings != WarningLevel.Error)
            Uctx.HasWarnings = WarningLevel;

        if (isFirstWarning != WarningLevel)
        {
            SafeConsole.WriteLineColor(WarningLevel == WarningLevel.Error ? ConsoleColor.Red : ConsoleColor.Yellow, WarningLevel.ToString().ToUpper() + " in " + this.FilePath);
            isFirstWarning = WarningLevel;
        }

        Console.Write(" ");
        foreach (var (match, after) in new Regex(@"\{(?<num>\d+)\}").SplitAfter(message.Format))
        {
            if (match != null)
                SafeConsole.WriteColor(ConsoleColor.White, message.GetArgument(int.Parse(match.Groups["num"].Value))?.ToString() ?? "");

            Console.Write(after);
        }

        Console.WriteLine();
    }

    public void Replace(string searchFor, string replaceBy)
    {
        var newContent = this.Content.Replace(searchFor, replaceBy);

        if (newContent == this.Content)
            Warning($"Unable to replace '{searchFor}' by '{replaceBy}'");

        this.Content = newContent;
    }

    public void Replace(Regex regex, string replaceBy)
    {
        var newContent = regex.Replace(this.Content, replaceBy);

        if (newContent == this.Content)
            Warning($"Unable to match {regex} to replace it by {replaceBy}");

        this.Content = newContent;
    }

    public void Replace(Regex regex, MatchEvaluator evaluator)
    {
        var newContent = regex.Replace(this.Content, evaluator);

        if (newContent == this.Content)
            Warning($"Unable to match {regex} to replace it");

        this.Content = newContent;
    }

    //Waiting for https://github.com/dotnet/csharplang/issues/287
    public void RemoveAllLines(Expression<Predicate<string>> condition)
    {
        ProcessLines(lines =>
        {
            var res = lines.RemoveAll(condition.Compile());
            if (res == 0)
            {
                Warning($"Unable to find any line where {condition} to remove it");
                return false;
            }

            return true;
        });
    }

    public void InsertAfterFirstLine(Expression<Predicate<string>> condition, string text)
    {
        ProcessLines(lines =>
        {
            var pos = lines.FindIndex(condition.Compile());
            if (pos == -1)
            {
                Warning($"Unable to find line where {condition} the insert after {text}");
                return false;
            }
            var indent = GetIndent(lines[pos]);
            lines.InsertRange(pos + 1, text.Lines().Select(a => IndentAndReplace(a, indent)));
            return true;
        });
    }

    public static string GetIndent(string v)
    {
        return Regex.Match(v, @"^\s*").Value;
    }

    /// <param name="fromLine">Not included</param>
    /// <param name="toLine">Not included</param>
    public void ReplaceBetweenExcluded(Expression<Predicate<string>> fromLine, Expression<Predicate<string>> toLine, string text) =>
        ReplaceBetween(new(fromLine, +1), new(toLine, -1), text);

    /// <param name="fromLine">Not included</param>
    /// <param name="toLine">Not included</param>
    public void ReplaceBetweenIncluded(Expression<Predicate<string>> fromLine, Expression<Predicate<string>> toLine, string text) =>
         ReplaceBetween(new(fromLine, +0), toLine: new(toLine, -0), text);



    public void ReplaceBetween(ReplaceBetweenOption fromLine, ReplaceBetweenOption toLine, string text)
    {
        ProcessLines(lines =>
        {
            var from = fromLine.FindStartIndex(lines);
            if (from == -1)
            {
                Warning($"Unable to find a line where {fromLine.Condition} to insert after it the text: {text}");
                return false;
            }

            var indent = GetIndent(lines[from - fromLine.Delta]);
            var to = toLine.FindEndIndex(lines, from, indent);
            if (to == -1)
            {
                Warning($"Unable to find a line where {toLine.Condition} after line {to} to insert before it the text: {text}");
                return false;
            }
            lines.RemoveRange(from, to - from + 1);
            if (text.HasText())
                lines.InsertRange(from, text.Lines().Select(a => IndentAndReplace(a, indent)));
            return true;
        });
    }

    /// <param name="fromLine">Not included</param>
    /// <param name="toLine">Not included</param>
    public void ReplaceBetweenExcluded(Expression<Predicate<string>> fromLine, Expression<Predicate<string>> toLine, Func<string, string> getText) =>
         ReplaceBetween(new(fromLine, +1), new(toLine, -1), getText);

    /// <param name="fromLine">Included</param>
    /// <param name="toLine">Included</param>
    public void ReplaceBetweenIncluded(Expression<Predicate<string>> fromLine, Expression<Predicate<string>> toLine, Func<string, string> getText) =>
       ReplaceBetween(new(fromLine, +0), toLine: new(toLine, -0), getText);

    public void ReplaceBetween(ReplaceBetweenOption fromLine, ReplaceBetweenOption toLine, Func<string, string> getText)
    {
        ProcessLines(lines =>
        {
            var from = fromLine.FindStartIndex(lines);
            if (from == -1)
            {
                Warning($"Unable to find a line where {fromLine.Condition} to insert text after it");
                return false;
            }

            var indent = GetIndent(lines[from - fromLine.Delta]);
            var to = toLine.FindEndIndex(lines, from, indent);
            if (to == -1)
            {
                Warning($"Unable to find a line where {toLine.Condition} after line {to} to insert text before it");
                return false;
            }
            var oldText = lines.Where((l, i) => i >= from && i <= to).ToList().ToString("\n");
            lines.RemoveRange(from, to - from + 1);

            var text = getText(oldText);
            if (text.HasText())
                lines.InsertRange(from, text.Lines().Select(a => IndentAndReplace(a, "")));

            return true;
        });
    }

    public string GetMethodBody(Expression<Predicate<string>> methodLine) =>
        GetLinesBetween(
            new(methodLine, 2),
            new(s => s.Contains("}"), -1) { SameIdentation = true });


    public void ReplaceMethod(Expression<Predicate<string>> methodLine, string text) =>
        ReplaceBetween(
            new(methodLine, 0),
            new(s => s.Contains("}"), 0) { SameIdentation = true },
            text);


    /// <param name="fromLine">Not included</param>
    /// <param name="toLine">Not included</param>
    public string GetLinesBetweenExcluded(Expression<Predicate<string>> fromLine, Expression<Predicate<string>> toLine) =>
         GetLinesBetween(new(fromLine, +1), new(toLine, -1));

    /// <param name="fromLine">Not included</param>
    /// <param name="toLine">Not included</param>
    public string GetLinesBetweenIncluded(Expression<Predicate<string>> fromLine, Expression<Predicate<string>> toLine) =>
       GetLinesBetween(new(fromLine, +0), toLine: new(toLine, -0));

    public string GetLinesBetween(ReplaceBetweenOption fromLine, ReplaceBetweenOption toLine)
    {
        string text = "";
        ProcessLines(lines =>
        {
            var from = fromLine.FindStartIndex(lines);
            if (from == -1)
            {
                Warning($"Unable to find a line where {fromLine} to extract text");
                return false;
            }

            var indent = GetIndent(lines[from - fromLine.Delta]);

            var to = toLine.FindEndIndex(lines, from, indent);
            if (to == -1)
            {
                Warning($"Unable to find a line where {toLine} after line {to} to extract text");
                return false;
            }
            text = lines.Where((l, i) => i >= from && i <= to).ToList().ToString("\n");
            return true;
        });
        return text;
    }


    public void ReplaceLine(Expression<Predicate<string>> condition, string text)
    {
        ProcessLines(lines =>
        {
            var pos = lines.FindIndex(condition.Compile());
            if (pos == -1)
            {
                Warning($"Unable to find a line where {condition} to replace it by {text}");
                return false;
            }
            var indent = GetIndent(lines[pos]);
            lines.RemoveRange(pos, 1);
            lines.InsertRange(pos, text.Lines().Select(a => IndentAndReplace(a, indent)));
            return true;
        });
    }

    public void InsertBeforeFirstLine(Expression<Predicate<string>> condition, string text)
    {
        ProcessLines(lines =>
        {
            var pos = lines.FindIndex(condition.Compile());
            if (pos == -1)
            {
                Warning($"Unable to find a line where {condition} to insert before {text}");
                return false;
            }
            var indent = GetIndent(lines[pos]);
            lines.InsertRange(pos, text.Lines().Select(a => IndentAndReplace(a, indent)));
            return true;
        });
    }

    private string IndentAndReplace(string a, string indent)
    {
        return a.Replace("Southwind", this.Uctx.ApplicationName)
            .Replace("southwind", this.Uctx.ApplicationName.ToLower())
            .Indent(indent);
    }

    public void InsertAfterLastLine(Expression<Predicate<string>> condition, string text)
    {
        ProcessLines(lines =>
        {
            var pos = lines.FindLastIndex(condition.Compile());
            if (pos == -1)
            {
                Warning($"Unable to find a line where {condition} to insert after {text}");
                return false;
            }
            var indent = GetIndent(lines[pos]);
            lines.InsertRange(pos + 1, text.Lines().Select(a => IndentAndReplace(a, indent)));
            return true;
        });
    }

    public void InsertBeforeLastLine(Expression<Predicate<string>> condition, string text)
    {
        ProcessLines(lines =>
        {
            var pos = lines.FindLastIndex(condition.Compile());
            if (pos == -1)
            {
                Warning($"Unable to find a line where {condition} to insert before {text}");
                return false;
            }
            var indent = GetIndent(lines[pos]);
            lines.InsertRange(pos, text.Lines().Select(a => IndentAndReplace(a, indent)));
            return true;
        });
    }

    public void ProcessLines(Func<List<string>, bool> process)
    {
        var separator = this.Content.Contains("\n") ? "\n" : "\n";
        var lines = Regex.Split(this.Content, "\r?\n").ToList();

        if (process(lines))
        {
            this.Content = lines.ToString(separator);
        }
    }

    private void AssertExtension(params string[] extension)
    {
        var ext = Path.GetExtension(this.FilePath);

        if (!extension.Any(e => e.Equals(ext, StringComparison.InvariantCulture)))
            throw new InvalidOperationException("");
    }

    internal void ReplacPartsInTypeScriptImport(Expression<Func<string, bool>> pathPredicate, Func<string /*path*/, HashSet<string> /*parts*/, HashSet<string>?> importedPartsSelector)
    {
        AssertExtension(".ts", ".tsx");

        var compiled = pathPredicate.Compile();
        ProcessLines(lines =>
        {
            bool changed = false;
            int pos = 0;
            while ((pos = lines.FindIndex(pos, a => a.StartsWith("import ") && !a.StartsWith("import type") && a.Contains(" from ") && compiled(a.After("from").Trim(' ', '\'', '\"', ';')))) != -1)
            {
                var line = lines[pos];
                var importPart = line.After("import").Before("from").Trim();
                string after = line.After("from");

                var parts = importPart.StartsWith('*') ? new[] { importPart.Trim() }.ToHashSet() : importPart.Between("{", "}").SplitNoEmpty(",").Select(a => a.Trim()).ToHashSet();
                var newParts = importedPartsSelector(after.Trim(' ', '\'', '\"', ';'), parts);

                if (newParts != null) {

                    if (newParts.Count == 1 && newParts.SingleEx().StartsWith("*"))
                        lines[pos] = "import " + newParts.SingleEx() + " from" + after;
                    else
                        lines[pos] = "import { " + newParts.ToString(", ") + " } from" + after;
                    changed = true;
                }
                pos++;
            }

            return changed;

        });
    }

    internal void ReplaceAndCombineTypeScriptImports(Expression<Func<string, bool>> pathPredicate, Func<HashSet<string>, HashSet<string>?> importedPartsSelector)
    {
        AssertExtension(".ts", ".tsx");

        var compiled = pathPredicate.Compile();
        ProcessLines(lines =>
        {
            var parts = new HashSet<string>();
            int pos = 0;
            var initialPos = -1;
            var after = (string?)null;
            while ((pos = lines.FindIndex(pos, a => a.StartsWith("import ") && !a.StartsWith("import type") && a.Contains(" from ") && compiled(a.After("from").Trim(' ', '\'', '\"', ';')))) != -1)
            {
                if (initialPos == -1)
                    initialPos = pos;

                var line = lines[pos];
                var importPart = line.After("import").Before("from").Trim();
                if (after == null)
                    after = line.After("from");

                lines.RemoveAt(pos);

                parts.AddRange(importPart.StartsWith('*') ? new[] { importPart.Trim() } : importPart.Between("{", "}").SplitNoEmpty(",").Select(a => a.Trim()));
            }

            if (initialPos == -1)
            {
                Warning($"Unable to find import with path where {pathPredicate}");
                return false;
            }

            var newImports = importedPartsSelector(parts);

            if (newImports != null)
                lines.Insert(initialPos, "import { " + newImports.ToString(", ") + " } from" + after);

            return true;
        });
    }

    public void UpdateNpmPackages(string packageJsonBlock)
    {
        var packages = packageJsonBlock.Lines().Select(a => a.Trim()).Where(a => a.HasText()).Select(a => new
        {
            PackageName = a.Before(":").Trim('"', ' '),
            Version = a.After(":").Trim(',', '"', ' '),
        }).ToList();

        foreach (var v in packages)
        {
            UpdateNpmPackage(v.PackageName, v.Version);
        }
    }


    public void UpdateNpmPackage(string packageName, string version)
    {
        AssertExtension(".json");

        ProcessLines(lines =>
        {

            var pos = lines.FindIndex(a => a.Contains(@$"""{packageName}"""));
            if (pos == -1)
            {
                Warning(@$"Unable to find a line with ""{packageName}"" to upgrade it to {version}");
                return false;
            }
            var indent = GetIndent(lines[pos]);
            lines.RemoveRange(pos, 1);

            var comma = lines[pos].Trim().StartsWith("}") ? "" : ",";
            lines.Insert(pos, IndentAndReplace(@$"""{packageName}"": ""{version}""" + comma, indent));
            return true;
        });
    }

    public void RemoveNpmPackage(string packageName)
    {
        AssertExtension(".json");

        ProcessLines(lines =>
        {
            var pos = lines.FindIndex(a => a.Contains(@$"""{packageName}"""));
            if (pos == -1)
            {
                Warning(@$"Unable to find a line with ""{packageName}"" to remove it");
                return false;
            }
            var indent = GetIndent(lines[pos]);
            lines.RemoveRange(pos, 1);

            if (lines[pos].Trim().StartsWith("}"))
            {
                if (!lines[pos - 1].Trim().EndsWith("{") && lines[pos - 1].Trim().EndsWith(","))
                {
                    lines[pos - 1] = lines[pos - 1].Replace(",", "");
                }
            }

            return true;
        });
    }

    public void AddNpmPackage(string packageName, string version, bool devDependencies = false)
    {
        AssertExtension(".json");

        ProcessLines(lines =>
        {
            var dependencies = devDependencies ? @"""devDependencies""": @"""dependencies""";

            var pos = lines.FindIndex(a => a.Contains(dependencies));
            
            if (pos == -1)
            {
                Warning(@$"Unable to find a line with {dependencies} to remove it");
                return false;
            }
            var indent = GetIndent(lines[pos]);
            if (lines[pos].TrimEnd().EndsWith("},"))
            {
                lines[pos] = lines[pos].Before("},");
                lines.Insert(pos + 1, indent + "},");
            }

            var postEnd = lines.FindIndex(pos, a => a.Contains("},"));


            if (
            !lines[postEnd - 1].TrimEnd().EndsWith(",") &&
            !lines[postEnd - 1].TrimEnd().EndsWith("{")
            )
            {
                lines[postEnd - 1] += ",";
            }

            lines.Insert(postEnd, indent + $@"  ""{packageName}"": ""{version}""");

            return true;
        });
    }

    public void UpdateNugetReferences(string xmlSnippets)
    {
        foreach (var line in xmlSnippets.Lines().Where(a => a.HasText()))
        {
            UpdateNugetReference(line.Between("Include=\"", "\""), line.Between("Version=\"", "\""));
        }
    }

    public void UpdateNugetReference(string packageName, string version)
    {
        AssertExtension(".csproj");

        var doc = XDocument.Parse(this.Content, LoadOptions.PreserveWhitespace);

        var elem = doc.Root!.Elements("ItemGroup").SelectMany(a => a.Elements("PackageReference"))
            .FirstOrDefault(a => a.Attribute("Include")?.Value == packageName);

        if (elem == null)
        {
            Warning($"Unable to find reference to Nuget {packageName} to update it to {version}");
        }
        else
        {
            elem.Attribute("Version")!.Value = version;

            this.Content = doc.ToString(SaveOptions.DisableFormatting).Replace("\n", "\n");
        }
    }

    public void RemoveNugetReference(string packageName)
    {
        AssertExtension(".csproj");

        var doc = XDocument.Parse(this.Content, LoadOptions.PreserveWhitespace);

        var eleme = doc.Root!.Elements("ItemGroup").SelectMany(a => a.Elements("PackageReference"))
            .FirstOrDefault(a => a.Attribute("Include")?.Value == packageName);

        if (eleme == null)
        {
            Warning($"Unable to remove reference to Nuget {packageName} because is not found");
        }
        else
        {
            eleme.Remove();

            this.Content = doc.ToString(SaveOptions.DisableFormatting).Replace("\n", "\n");
        }
    }

    public void AddNugetReference(string packageName, string version)
    {
        AssertExtension(".csproj");

        var doc = XDocument.Parse(this.Content, LoadOptions.PreserveWhitespace);

        var itemGroup = doc.Root!.Elements("ItemGroup").FirstEx();

        itemGroup.Add(new XElement("PackageReference",
            new XAttribute("Include", packageName),
            new XAttribute("Include", version)
        ));

        this.Content = doc.ToString(SaveOptions.DisableFormatting).Replace("\n", "\n");

    }

    public void Solution_RemoveProject(string name, WarningLevel showWarning = WarningLevel.Error)
    {
        var prj = Content.Lines().Where(l => l.Contains("Project(") && l.Contains(name + ".csproj")).SingleOrDefault();

        if (prj == null)
        {
            SafeConsole.WriteLineColor(showWarning == WarningLevel.Error ? ConsoleColor.Red : ConsoleColor.Yellow,
               showWarning.ToString().ToUpper() + " no reference to '" + name + "' found in " + FilePath);

            return;
        }

        var projectId = GuidRegex.Match(prj.After(name)).Groups["id"].Value;

        ReplaceBetweenIncluded(l => l.Contains("Project(") && l.Contains(name + ".csproj"), l => l.Contains("EndProject"), "");

        RemoveAllLines(l => l.Contains(projectId));
    }


    static Regex GuidRegex = new Regex(@"(?<id>[\w]{8}-[\w]{4}-[\w]{4}-[\w]{4}-[\w]{12})");

    public void Solution_AddProject(string projectFile, string? parentFolder, string projecTypeId = "9A19103F-16F7-4668-BE54-9A1E7A4F7556", WarningLevel showWarning = WarningLevel.Error)
    {
        var prjRegex = new Regex(@"[\\]?(?<project>[\.\w]*).csproj");

        var prjName = prjRegex.Match(projectFile).Groups["project"].Value;

        var projectId = Guid.NewGuid().ToString().ToUpper();

        var projectTypeIdUpper = Guid.Parse(projecTypeId).ToString().ToUpper();

        InsertAfterLastLine(l => l.StartsWith("EndProject"), $$"""
                Project("{{{projectTypeIdUpper}}}") = "{{prjName}}", "{{projectFile}}", "{{{projectId}}}"
                EndProject
                """);
        var configs = GetLinesBetweenExcluded(
            l => l.Contains("GlobalSection(SolutionConfigurationPlatforms) = preSolution"),
            l => l.Contains("EndGlobalSection")).Split("\n");

        InsertAfterFirstLine(l => l.Contains("GlobalSection(ProjectConfigurationPlatforms) = postSolution"),
            configs.Select(config => $$"""
                    {{{projectId}}}.{{config.Before("=").Trim()}}.ActiveCfg = {{config.After("=").Trim()}}
                    {{{projectId}}}.{{config.Before("=").Trim()}}.Build.0 = {{config.After("=").Trim()}}
                """).ToString("\n"));

        if (parentFolder != null)
        {
            var parent = this.Content.Lines().Where(l => l.Contains("Project(") && l.Contains(parentFolder)).FirstEx();

            var parentId = GuidRegex.Match(parent.After(parentFolder)).Groups["id"].Value;

            InsertAfterFirstLine(l => l.Contains("GlobalSection(NestedProjects) = preSolution"), $"\t{{{projectId}}} = {{{parentId}}}");
        }
        SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"Project {projectFile} added to solution.");
    }

    public void Solution_AddFolder(string folderName)
    {
        var folderId = Guid.NewGuid().ToString().ToUpper();

        var folderTypeId = Guid.Parse("2150E333-8FDC-42A3-9474-1A3956D46DE8").ToString().ToUpper();

        InsertAfterLastLine(l => l.StartsWith("EndProject"), $$"""
                Project("{{{folderTypeId}}}") = "{{folderName}}", "{{folderName}}", "{{{folderId}}}"
                EndProject
                """);
    }

    public void Solution_SolutionItem(string relativeFilePath, string folderName)
    {
        var folderId = Guid.NewGuid().ToString().ToUpper();

        var folderTypeId = Guid.Parse("2150E333-8FDC-42A3-9474-1A3956D46DE8").ToString().ToUpper();

        ReplaceBetweenExcluded(
            l => l.StartsWith("Project") && l.Contains($"\"{folderName}\""),
            l => l.Trim() == "EndProject", text =>
            {
                if (text.Trim().IsEmpty())
                    return
                    "\tProjectSection(SolutionItems) = preProject\n" +
                    "\t\t" + relativeFilePath + " = " + relativeFilePath + "\n" +
                    "\tEndProjectSection";
                else
                    return text.Replace("\tEndProjectSection", "\t\t" + relativeFilePath + " = " + relativeFilePath + "\n" +
                    "\tEndProjectSection");
            });
    }

    internal IDisposable OverrideWarningLevel(WarningLevel none)
    {
        var oldLevel = this.WarningLevel;
        this.WarningLevel = none;
        return new Disposable(() => this.WarningLevel = oldLevel);
    }

    internal void MoveFile(string newFilePath)
    {
        this.newFilePath = newFilePath;
    }

    public void ReplaceBlock(Expression<Predicate<string>> fromLine, Expression<Predicate<string>> toLine, Func<string, string, string, string> getText)
    {
        ProcessLines(lines =>
        {
            var fromIdx = lines.FindIndex(fromLine.Compile());
            if (fromIdx == -1)
            {
                Warning($"Unable to find a line where {fromLine} to start block replacement");
                return false;
            }
            var fromIndent = GetIndent(lines[fromIdx]);

            var toLineFunc = toLine.Compile();

            var toIdx = lines.FindIndex(fromIdx, s => GetIndent(s) == fromIndent && toLineFunc(s));
            if (toIdx == -1)
            {
                Warning($"Unable to find a line where {toLine} to end block replacement with the same indentation as fromLine");
                return false;
            }

            // Collect initialLines
            var initialLines = new List<string> { lines[fromIdx] };
            int i = fromIdx + 1;
            while (i < toIdx && GetIndent(lines[i]) == fromIndent)
            {
                initialLines.Add(lines[i]);
                i++;
            }

            // Collect endLines
            var endLines = new List<string> { lines[toIdx] };
            int j = toIdx - 1;
            while (j >= i && GetIndent(lines[j]) == fromIndent)
            {
                endLines.Insert(0, lines[j]);
                j--;
            }

            // Collect body
            var body = new List<string>();
            for (int k = i; k <= j; k++)
                body.Add(lines[k]);

            string Unindent(List<string> list)
            {
                return list.Select(a => a.StartsWith(fromIndent) ? a.Substring(fromIndent.Length) : a).ToString("\n");
            }

            // Replace block
            lines.RemoveRange(fromIdx, toIdx - fromIdx + 1);
            var replacement = getText(Unindent(initialLines), Unindent(body), Unindent(endLines));
            if (replacement.HasText())
                lines.InsertRange(fromIdx, replacement.Lines().Select(a => IndentAndReplace(a, fromIndent)));
            return true;
        });
    }
}

public class ReplaceBetweenOption
{
    public Expression<Predicate<string>> Condition;
    public int Delta;
    public bool LastIndex = false;
    public bool SameIdentation = false;

    public ReplaceBetweenOption(Expression<Predicate<string>> condition, int delta = 0)
    {
        this.Condition = condition;
        this.Delta = delta;
    }

   

    internal int FindStartIndex(List<string> lines)
    {
        var cond = Condition.Compile();
        var from = !LastIndex ?
          lines.FindIndex(cond) :
          lines.FindLastIndex(cond);

        if (from == -1)
            return from;

        return from + Delta;
    }

    internal int FindEndIndex(List<string> lines, int startIndex, string indent)
    {
        var cond = Condition.Compile();

        var cond2 = !SameIdentation ? cond :
        s => cond(s) && CodeFile.GetIndent(s) == indent;

        var to = !LastIndex ?
        lines.FindIndex(startIndex, cond2) :
        lines.FindLastIndex(cond2);

        if (to == -1)
            return to;

        return to + Delta;
    }

    public override string ToString() => this.Condition.ToString();
}

