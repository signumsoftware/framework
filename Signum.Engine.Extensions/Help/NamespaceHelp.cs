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

        public NamespaceHelp Load()
        {
            if (!File.Exists(FileName))
                return this;

            XElement ns = XDocument.Load(FileName).Element(_Namespace);
            Description = ns.Element(_Description).TryCC(a => a.Value);

            return this;
        }

        internal static string GetNamespaceName(XDocument document, string fileName)
        {
            if (document.Root.Name != _Namespace)
                throw new InvalidOperationException("{0} does not have a {1} root".Formato(fileName, _Namespace));

            var result = document.Root.Attribute(_Name).TryCC(a => a.Value);

            if (string.IsNullOrEmpty(result))
                throw new InvalidOperationException("{0} does not have a {1} attribute".Formato(fileName, _Name));

            return result;
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

        public bool Save()
        {
            XDocument document = this.ToXDocument();
            if (document == null)
            {
                File.Delete(FileName);
                return false;
            }
            else
            {
                document.Save(FileName);
                return true;
            }
        }


        static readonly XName _Namespace = "Namespace";
        static readonly XName _Name = "Name";
        static readonly XName _Description = "Description";
        static readonly XName _Language = "Language";

        public static void Synchronize(string fileName, XDocument loadedDoc, string nameSpace)
        {
            XElement loadedNs = loadedDoc.Element(_Namespace);

            var created = NamespaceHelp.Create(nameSpace);
            created.Description = loadedNs.Element(_Description).TryCC(a => a.Value);

            if (fileName != created.FileName)
            {
                Console.WriteLine("FileName changed {0} -> {1}".Formato(fileName, created.FileName));
                File.Move(fileName, created.FileName);
            }

            if (!created.Save())
                Console.WriteLine("File deleted {1}".Formato(fileName, created.FileName));
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
