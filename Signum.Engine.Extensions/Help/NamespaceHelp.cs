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
            if (string.IsNullOrEmpty(Description))
                return null;

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
                FileName = Path.Combine(Path.Combine(HelpLogic.HelpDirectory, HelpLogic.NamespacesDirectory), "{0}.help".Formato(nameSpace))
            };
        }

        public void Save()
        {
            XDocument document = this.ToXDocument();
            if (document == null)
                File.Delete(FileName);
            else
                document.Save(FileName);
        }


        static readonly XName _Namespace = "Namespace";
        static readonly XName _Name = "Name";
        static readonly XName _Description = "Description";
        static readonly XName _Language = "Language";

        public static void Synchronize(string fileName, string nameSpace)
        {
            XDocument loadedDoc = XDocument.Load(fileName);
            XElement loadedNs = loadedDoc.Element(_Namespace);

            var created = NamespaceHelp.Create(nameSpace);
            loadedNs.Attribute(_Name).Value = created.Name;

            if(loadedNs.Element(_Description) == null)
            {
                File.Delete(fileName);
            }
            else if (fileName != created.FileName)
            {
                Console.WriteLine("FileNameChanged {0} -> {1}".Formato(fileName, created.FileName));
                File.Delete(fileName);
                loadedDoc.Save(created.FileName);
                Console.WriteLine();
            }
            else if ()
            {

                Console.WriteLine("FilModified {0}".Formato(fileName));
                loadedDoc.Save(fileName);
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
