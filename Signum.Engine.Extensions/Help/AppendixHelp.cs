using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;
using System.IO;
using Signum.Utilities;

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

        internal static string GetApendixName(XDocument document)
        {
            if (document.Root.Name == _Appendix)
                return document.Root.Attribute(_Name).Value;
            return null;
        }

        public static AppendixHelp Create(string name, string title, string description)
        {
            return new AppendixHelp
            {
                Name = name,
                Title = title,
                Language = CultureInfo.CurrentCulture.Name,
                Description = description,
            };
        }

        static string DefaultFileName(string name)
        {
            return Path.Combine(HelpLogic.HelpDirectory, "{0}.help".Formato(name));
        }

        public string Save()
        {
            XDocument document = this.ToXDocument();
            string path = DefaultFileName(Name);
            document.Save(path);
            return path;
        }

        static readonly XName _Appendix = "Appendix";
        static readonly XName _Name = "Name";
        static readonly XName _Title = "Title";
        static readonly XName _Description = "Description";
        static readonly XName _Language = "Language";
    }
}
