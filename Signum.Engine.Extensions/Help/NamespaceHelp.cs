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
    public class NamespaceHelp
    {
        public string FileName;
        public string Name;
        public string Description;
        public string Language; 

        public XDocument ToXDocument()
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                   new XElement(_Namespace,
                       new XAttribute(_Name, Name),
                       new XAttribute(_Language, Language),
                       new XElement(_Description, Description)
                   )
                );
        }

        public static NamespaceHelp Load(XDocument document, string sourceFile)
        {
            XElement ns = document.Element(_Namespace);
            return new NamespaceHelp
            {
                Name = ns.Attribute(_Name).Value,
                Language = ns.Attribute(_Language).Value,
                Description = ns.Element(_Description).Value,
                FileName = sourceFile,
            }; 
        }

        internal static string GetNamespaceName(XDocument document)
        {
            if (document.Root.Name == _Namespace)
                return document.Root.Attribute(_Name).Value;
            return null;
        }

        public static NamespaceHelp Create(string nameSpace)
        {
            return new NamespaceHelp
            {
                Name = nameSpace,
                Language = CultureInfo.CurrentCulture.Name,
                Description = "",
            };
        }

        public string Save()
        {
            XDocument document = this.ToXDocument();
            string path = DefaultFileName(Name);
            document.Save(path);
            return path;
        }

        static string DefaultFileName(string nameSpace)
        {
            return Path.Combine(
                Path.Combine(HelpLogic.HelpDirectory, HelpLogic.NamespacesDirectory), "{0}.help".Formato(nameSpace));
        }

        static readonly XName _Namespace = "Namespace";
        static readonly XName _Name = "Name";
        static readonly XName _Description = "Description";
        static readonly XName _Language = "Language";

        public static void Synchronize(string fileName, string nameSpace)
        {
            XElement loaded = XDocument.Load(fileName).Element(_Namespace);
            XDocument createdDoc = NamespaceHelp.Create(nameSpace).ToXDocument();
            XElement created = createdDoc.Element(_Namespace);

            string loadedNameSpace = loaded.Attribute(_Name).Value;
            if (nameSpace != loadedNameSpace)
            {
                created.Value = loaded.Value;

                string goodFileName = DefaultFileName(nameSpace);

                Console.WriteLine("FileNameChanged {0} -> {1}".Formato(fileName, goodFileName));
                File.Delete(fileName);
                created.Save(goodFileName);
                Console.WriteLine();
            }

        }

        public IEnumerable<SearchResult> Search(Regex regex)
        {
            Match m = null;

            //Types description
            if (Description.HasText())
            {
                m = regex.Match(Description.RemoveDiacritics());
                if (m.Success)
                {
                    yield return new SearchResult(TypeSearchResult.NamespaceDescription, Name, Description.Extract(m), null, m, HelpLogic.BaseUrl + "/Namespace/" + Name);
                    yield break;
                }
            }
        }
    }
}
