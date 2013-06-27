using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;
using System.IO;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Authorization;

namespace Signum.Engine.Help
{
    public class QueryColumnHelp
    {
        public string Name;
        public string Info;
        public string UserDescription;
    }

    public class QueryHelp
    {
        public Dictionary<string, QueryColumnHelp> Columns;
        public object Key;
        public string Language;
        public string FileName;
        public string UserDescription = string.Empty;
        public string Info = string.Empty;

        public XDocument ToXDocument()
        {
            if (string.IsNullOrEmpty(UserDescription) && Columns.All(c => string.IsNullOrEmpty(UserDescription)))
                return null;

            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                   new XElement(_Query,
                       new XAttribute(_Key, QueryUtils.GetQueryUniqueKey(Key)),
                       new XAttribute(_Language, Language),
                       UserDescription.HasText() ? new XElement(_Description, UserDescription) : null,
                        Columns.Values.Any(c => c.UserDescription.HasText()) ?
                           new XElement(_Columns,
                               Columns.Values.Where(c => c.UserDescription.HasText())
                               .Select(c => new XElement(_Column,
                                   new XAttribute(_Name, c.Name),
                                   c.UserDescription))
                           ) : null
                       )
                   );
        }

        public QueryHelp Load()
        {
            if (!File.Exists(FileName))
                return this;

            XElement element = XDocument.Load(FileName).Element(_Query);
            object queryName = QueryLogic.ToQueryName(element.Attribute(_Key).Value);

            if (!queryName.Equals(this.Key))
                throw new InvalidOperationException("QueryName should be {0} instead of {1}".Formato(QueryUtils.GetQueryUniqueKey(this.Key), QueryUtils.GetQueryUniqueKey(queryName))); 

            this.UserDescription = element.Element(_Description).TryCC(d => d.Value); 
            
            var cs = element.Element(_Columns);
            if(cs != null)
            {
                string errorMessage = "loading column {0} on Query file (" + FileName + ")"; 
                foreach (var item in cs.Elements(_Column))
	            {
                    this.Columns.GetOrThrow(item.Attribute(_Name).Value, errorMessage ).UserDescription = item.Value;
	            }
            }

            return this;
        }

        internal static string GetQueryFullName(XDocument document)
        {
            if (document.Root.Name == _Query)
                return document.Root.Attribute(_Key).Value;
            return null;
        }

        public static IEnumerable<ColumnDescriptionFactory> GenerateColumns(object key)
        {
            using (AuthLogic.Disable())
            {
                ColumnDescriptionFactory[] columns = DynamicQueryManager.Current.GetQuery(key).Core.Value.StaticColumns;
             
                return columns;
            }
        }

        public static QueryHelp Create(object key)
        {
            return new QueryHelp
            {
                Key = key,
                Language = CultureInfo.CurrentCulture.Name,
                Info = HelpGenerator.GetQueryHelp(DynamicQueryManager.Current.GetQuery(key).Core.Value),
                Columns = GenerateColumns(key).ToDictionary(
                                kvp => kvp.Name,
                                kvp => new QueryColumnHelp() { Name = kvp.Name, Info = HelpGenerator.GetQueryColumnHelp(kvp) }),
                FileName = Path.Combine(Path.Combine(HelpLogic.HelpDirectory, HelpLogic.QueriesDirectory), "{0}.help".Formato(key))
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

        static readonly XName _Name = "Name";
        static readonly XName _Key = "Key";
        static readonly XName _Description = "Description";
        static readonly XName _Language = "Language";
        static readonly XName _Query = "Query";
        static readonly XName _Columns = "Columns";
        static readonly XName _Column = "Column";


        public static void Synchronize(string fileName, string key)
        {
            XDocument loaded = XDocument.Load(fileName);
            XElement loadedQuery = loaded.Element(_Query);
            var created = QueryHelp.Create(QueryLogic.TryToQueryName(key));

            bool changed = false;
            HelpTools.SynchronizeElements(loadedQuery, _Columns, _Columns, _Name, created.Columns, "Columns of {0}".Formato(key),
              (action, column) =>
              {
                  if (!changed)
                  {
                      Console.WriteLine("Synchronized {0} ".Formato(fileName));
                      changed = true;
                  }
                  Console.WriteLine("  Column {0}: {1}".Formato(action, column));
              });

            if (loadedQuery.Element(_Columns) == null && loadedQuery.Element(_Description) == null)
            {
                File.Delete(fileName);

                Console.WriteLine("Deleted {0} -> {1}".Formato(fileName, created.FileName));
            }
            else if (fileName != created.FileName)
            {
                Console.WriteLine("FileNameChanged {0} -> {1}".Formato(fileName, created.FileName));
                File.Delete(fileName);
                loadedQuery.Save(created.FileName);
            }
            else
            {
                loadedQuery.Save(fileName);
            }

            if (changed)
                Console.WriteLine();

        }
    }
}
