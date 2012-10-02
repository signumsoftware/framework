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

        public QueryHelp() { }

        public QueryHelp(object queryKey, string info)
        {
            this.Key = queryKey;
            this.Info = info;
        }

        public QueryHelp(object queryKey, string info, string userDescription)
        {
            this.Key = queryKey;
            this.Info = info;
            this.UserDescription = userDescription;
        }

        public XDocument ToXDocument()
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                   new XElement(_Query,
                       new XAttribute(_Key, QueryUtils.GetQueryUniqueKey(Key)),
                       new XAttribute(_Info, Info),
                       new XAttribute(_Language, Language),
                       new XElement(_Description, UserDescription),
                        Columns.Let(ps => ps == null || ps.Count == 0 ? null :
                           new XElement(_Columns,
                               ps.Select(p => new XElement(_Column,
                                   new XAttribute(_Name, p.Value.Name),
                                   new XAttribute(_Info, p.Value.Info),
                                   p.Value.UserDescription))
                           )
                       )
                   )
                );
        }

        public static QueryHelp Load(XDocument document, string sourceFile)
        {
            XElement element = document.Element(_Query);
            string queryName = element.Attribute(_Key).Value;
            object queryKey = QueryLogic.ToQueryName(queryName);
            return new QueryHelp
            {
                Key = queryKey,
                UserDescription = element.Element(_Description).TryCC(d => d.Value),
                Language = element.Attribute(_Language).Value,
                Info = element.Attribute(_Info).Value,
                FileName = sourceFile,
                Columns = EnumerableExtensions.JoinStrict(
                    element.Element(_Columns).TryCC(ps => ps.Elements(_Column)) ?? new XElement[0],
                    GenerateColumns(queryKey),
                    x => x.Attribute(_Name).Value,
                    pp => pp.Name,
                    (x, pp) => new KeyValuePair<string, QueryColumnHelp>(
                         pp.Name,
                         new QueryColumnHelp{Name = x.Attribute(_Name).Value, Info = x.Attribute(_Info).Value, UserDescription = x.Value}),
                    "loading Columns for {0} Query file ({1})".Formato(queryName, sourceFile)).CollapseDictionary(),
            }; 
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
                ColumnDescriptionFactory[] columns = DynamicQueryManager.Current[key].StaticColumns.Value;
             
                return columns;
            }
        }

        public static QueryHelp Create(object key)
        {
            return new QueryHelp
            {
                Key = key,
                Language = CultureInfo.CurrentCulture.Name,
                Info = HelpGenerator.GetQueryHelp(DynamicQueryManager.Current[key]),
                Columns = GenerateColumns(key).ToDictionary(
                                kvp => kvp.Name,
                                kvp => new QueryColumnHelp() { Name = kvp.Name, Info = HelpGenerator.GetQueryColumnHelp(kvp) })
            };
        }

        public string Save()
        {
            XDocument document = this.ToXDocument();
            string path = DefaultFileName(QueryUtils.GetQueryUniqueKey(Key));
            document.Save(path);
            return path;
        }

        static string DefaultFileName(string key)
        {
            return Path.Combine(
                Path.Combine(HelpLogic.HelpDirectory,HelpLogic.QueriesDirectory), "{0}.help".Formato(key));
        }

        static readonly XName _Name = "Name";
        static readonly XName _Key = "Key";
        static readonly XName _Description = "Description";
        static readonly XName _Language = "Language";
        static readonly XName _Query = "Query";
        static readonly XName _Columns = "Columns";
        static readonly XName _Column = "Column";
        static readonly XName _Info = "Info";


        public static void Synchronize(string fileName, string key)
        {
            XElement loaded = XDocument.Load(fileName).Element(_Query);
            XDocument createdDoc = QueryHelp.Create(QueryLogic.TryToQueryName(key)).ToXDocument();
            XElement created = createdDoc.Element(_Query);


            created.Element(_Description).Value = loaded.Element(_Description).Value;

            bool changed = false;
            Action change = () =>
            {
                if (!changed)
                {
                    Console.WriteLine("Synchronized {0} ".Formato(fileName));
                    changed = true;
                }
            };

            HelpTools.Syncronize(created, loaded, _Columns, _Columns, _Name, "Columns of {0}".Formato(key),
              (k, c, l) =>
              {
                  c.Value = l.Value;
                  return EntityHelp.Distinct(l.Attribute(_Info), c.Attribute(_Info));
              },
              (action, column) =>
              {
                  change();
                  if (action == SyncAction.OrderChanged)
                      Console.WriteLine("  Columns {0}".Formato(action));
                  else
                      Console.WriteLine("  Column {0}: {1}".Formato(action, column));
              });

            string goodFileName = DefaultFileName(key);
            if (fileName != goodFileName)
            {
                Console.WriteLine("FileNameChanged {0} -> {1}".Formato(fileName, goodFileName));
                File.Delete(fileName);
                createdDoc.Save(goodFileName);
            }
            else
            {
                createdDoc.Save(fileName);
            }

            if (changed)
                Console.WriteLine();

        }
    }
}
