using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Signum.Upgrade
{
    public class CodeFile
    {
        public CodeFile(string filePath, UpgradeContext uctx)
        {
            this.FilePath = filePath;
            this.Uctx = uctx;
        }

        public string FilePath { get; }
        public UpgradeContext Uctx { get; }

        string? _content;
        string? _originalContent; 
        public string Content
        {
            get { ReadIfNecessary(); return _content!; }
            set { _content = value; }
        }

        private void ReadIfNecessary()
        {
            if(_content == null)
            {
                _originalContent = _content = File.ReadAllText(FilePath, GetEncoding(FilePath));
            }
        }

        public void SafeIfNecessary()
        {
            if (_content == null)
                return;

            if (_content == _originalContent)
                return;

            File.WriteAllText(FilePath, _content, GetEncoding(FilePath));
        }

        Encoding GetEncoding(string filePath)
        {
            return Encoding.UTF8;
        }

        public void Replace(string searchFor, string replaceBy, 
            [CallerArgumentExpression("searchFor")] string? searchForMessage = null, [CallerArgumentExpression("searchFor")] string? replaceByMessage = null)
        {
            this.Content = this.Content.Replace(searchFor, replaceBy);
        }

        public void Replace(Regex regex, MatchEvaluator evaluator)
        {
            this.Content = regex.Replace(this.Content, evaluator);
        }



        public void RemoveAllLines(Predicate<string> condition)
        {
            ProcessLines(lines =>
            {
                var res = lines.RemoveAll(condition);
                return true;
            });
        }

        public void InsertAfterFirstLine(Predicate<string> condition, string otherLines)
        {
            ProcessLines(lines =>
            {
                var pos = lines.FindIndex(condition);
                var indent = GetIndent(lines[pos]);
                lines.InsertRange(pos + 1, otherLines.Lines().Select(a => indent + a.Replace("Southwind", this.Uctx.ApplicationName)));
                return true;
            });
        }

        string GetIndent(string v)
        {
            return Regex.Match(v, @"^\s*").Value;
        }

        /// <param name="fromLine">Not included</param>
        /// <param name="toLine">Not included</param>
        public void ReplaceBetween(Predicate<string> fromLine, Predicate<string> toLine, string otherLines)
        {
            ProcessLines(lines =>
            {
                var from = lines.FindIndex(fromLine);
                var to = lines.FindIndex(from + 1, toLine);
                var indent = GetIndent(lines[from]);
                lines.RemoveRange(from +1, to - from -2);
                lines.InsertRange(from + 1, otherLines.Lines().Select(a => indent + a.Replace("Southwind", this.Uctx.ApplicationName)));
                return true;
            });
        }

        public void ReplaceLine(Predicate<string> condition, string otherLines)
        {
            ProcessLines(lines =>
            {
                var from = lines.FindIndex(condition);
                var indent = GetIndent(lines[from]);
                lines.RemoveRange(from, 1);
                lines.InsertRange(from, otherLines.Lines().Select(a => indent + a.Replace("Southwind", this.Uctx.ApplicationName)));
                return true;
            });
        }

        public void InsertBeforeFirstLine(Predicate<string> condition, string otherLines)
        {
            ProcessLines(lines =>
            {
                var pos = lines.FindIndex(condition);
                var indent = GetIndent(lines[pos]);
                lines.InsertRange(pos, otherLines.Lines().Select(a => indent + a.Replace("Southwind", this.Uctx.ApplicationName)));
                return true;
            });
        }

        public void InsertAfterLastLine(Predicate<string> condition, string otherLines)
        {
            ProcessLines(lines =>
            {
                var pos = lines.FindLastIndex(condition);
                var indent = GetIndent(lines[pos]);
                lines.InsertRange(pos + 1, otherLines.Lines().Select(a => indent + a.Replace("Southwind", this.Uctx.ApplicationName)));
                return true;
            });
        }

        public void InsertBeforeLastLine(Predicate<string> condition, string otherLines)
        {
            ProcessLines(lines =>
            {
                var pos = lines.FindLastIndex(condition);
                var indent = GetIndent(lines[pos]);
                lines.InsertRange(pos, otherLines.Lines().Select(a => indent + a.Replace("Southwind", this.Uctx.ApplicationName)));
                return true;
            });
        }

        public void ProcessLines(Func<List<string>, bool> process)
        {
            var separator = this.Content.Contains("\r\n") ? "\r\n" : "\n";
            var lines = this.Content.Split(separator).ToList();

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

        public bool UpdateNugetReference(string packageName, string version)
        {
            AssertExtension(".csproj");

            var doc = XDocument.Parse(this.Content, LoadOptions.PreserveWhitespace);

            var attr = doc.Elements("ItemGroup").SelectMany(a => a.Elements("PackageReference"))
                .FirstOrDefault(a => a.Attribute("Include")?.Value == packageName);

            if (attr == null)
                return false;

            attr.Attribute("Version")!.Value = version;

            this.Content = doc.ToString(SaveOptions.DisableFormatting);

            return true;
        }

        public bool RemoveNugetReference(string packageName)
        {
            AssertExtension(".csproj");

            var doc = XDocument.Parse(this.Content, LoadOptions.PreserveWhitespace);


            var attr = doc.Elements("ItemGroup").SelectMany(a => a.Elements("PackageReference"))
                .FirstOrDefault(a => a.Attribute("Include")?.Value == packageName);

            if (attr == null)
                return false;

            this.Content = doc.ToString(SaveOptions.DisableFormatting);

            return true;
        }

        public bool AddNugetReference(string packageName, string version)
        {
            AssertExtension(".csproj");

            var doc = XDocument.Parse(this.Content, LoadOptions.PreserveWhitespace);

            var itemGroup = doc.Elements("ItemGroup").FirstEx();

            itemGroup.Add(new XElement("PackageReference",
                new XAttribute("Include", packageName),
                new XAttribute("Include", version)
            ));

            this.Content = doc.ToString(SaveOptions.DisableFormatting);

            return true;
        }
    }

}
