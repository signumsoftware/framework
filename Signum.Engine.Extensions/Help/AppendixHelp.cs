using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;
using System.IO;
using Signum.Utilities;
using System.Text.RegularExpressions;

namespace Signum.Engine.Help
{
    public class AppendixHelp
    {
        public string FileName;
        public string Name;
        public string Title;
        public string Description;
        public string Language;

        public XDocument ToXDocument()
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                   new XElement(_Appendix,
                       new XAttribute(_Name, Name),
                       new XAttribute(_Title, Title),
                       new XAttribute(_Language, Language),
                       new XElement(_Description, Description)
                   )
                );
        }

        public static AppendixHelp Load(XDocument document, string sourceFile)
        {
            XElement ns = document.Element(_Appendix);
            return new AppendixHelp
            {
                Name = ns.Attribute(_Name).Value,
                Title = ns.Attribute(_Title).Value,
                Language = ns.Attribute(_Language).Value,
                Description = ns.Element(_Description).Value,
                FileName = sourceFile,
            };
        }

        internal static string GetAppendixName(XDocument document)
        {
            if (document.Root.Name == _Appendix)
                return document.Root.Attribute(_Name).Value;
            return null;
        }

        public void Save()
        {
            XDocument document = this.ToXDocument();
            document.Save(FileName);
        }

        static readonly XName _Appendix = "Appendix";
        static readonly XName _Name = "Name";
        static readonly XName _Title = "Title";
        static readonly XName _Description = "Description";
        static readonly XName _Language = "Language";

        const int etcLength = 300;

        public SearchResult Search(Regex regex)
        {
            if (Description.HasText())
            {
                Match m = regex.Match(Description.RemoveDiacritics());
                if (m.Success)
                {
                    return new SearchResult(TypeSearchResult.AppendixDescription, Name, Description.Extract(m), null, m, HelpLogic.BaseUrl + "/Appendix/" + Name);
                }
            }

            return null;
        }
    }
}
