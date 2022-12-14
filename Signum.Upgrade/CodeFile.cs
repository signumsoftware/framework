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

    public string FilePath { get; }
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

    public void SafeIfNecessary()
    {
        if (_content == null)
            return;

        if (_content == _originalContent)
            return;

        SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "Modified " + FilePath);
        File.WriteAllText(Path.Combine(this.Uctx.RootFolder, FilePath), _content, encoding!);
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

    string GetIndent(string v)
    {
        return Regex.Match(v, @"^\s*").Value;
    }

    /// <param name="fromLine">Not included</param>
    /// <param name="toLine">Not included</param>
    public void ReplaceBetweenExcluded(Expression<Predicate<string>> fromLine, Expression<Predicate<string>> toLine, string text) =>
        ReplaceBetween(fromLine, +1, toLine, -1, text);

    /// <param name="fromLine">Not included</param>
    /// <param name="toLine">Not included</param>
    public void ReplaceBetweenIncluded(Expression<Predicate<string>> fromLine, Expression<Predicate<string>> toLine, string text) =>
        ReplaceBetween(fromLine, +0, toLine, -0, text);

    public void ReplaceBetween(Expression<Predicate<string>> fromLine, int fromDelta, Expression<Predicate<string>> toLine, int toDelta, string text)
    {
        ProcessLines(lines =>
        {
            var from = lines.FindIndex(fromLine.Compile());
            if (from == -1)
            {
                Warning($"Unable to find a line where {fromLine} to insert after it the text: {text}");
                return false;
            }
            var to = lines.FindIndex(from + 1, toLine.Compile());
            if (to == -1)
            {
                Warning($"Unable to find a line where {toLine} after line {to} to insert before it the text: {text}");
                return false;
            }
            var indent = GetIndent(lines[from]);
            lines.RemoveRange(from + fromDelta, (to + toDelta) - (from + fromDelta) + 1);
            if (text.HasText())
                lines.InsertRange(from + fromDelta, text.Lines().Select(a => IndentAndReplace(a, indent)));
            return true;
        });
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
        var separator = this.Content.Contains("\r\n") ? "\r\n" : "\n";
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

    public void UpdateNpmPackages(string packageJsonBlock)
    {
        var packages = packageJsonBlock.Lines().Select(a => a.Trim()).Where(a => a.HasText()).Select(a => new
        {
            PackageName = a.Before(":").Trim('"'),
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

            if(lines[pos].Trim().StartsWith("}"))
            {
                if (!lines[pos - 1].Trim().EndsWith("{") && lines[pos - 1].Trim().EndsWith(","))
                {
                    lines[pos - 1] = lines[pos - 1].Replace(",", "");
                }
            }

            return true;
        });
    }

    public void UpdateNugetReferences(string xmlSnippets)
    {
        foreach (var line in xmlSnippets.Lines().Where(a=>a.HasText()))
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

            this.Content = doc.ToString(SaveOptions.DisableFormatting);
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

            this.Content = doc.ToString(SaveOptions.DisableFormatting);
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

        this.Content = doc.ToString(SaveOptions.DisableFormatting);

    }
}

