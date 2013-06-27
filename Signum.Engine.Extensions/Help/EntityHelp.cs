using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Signum.Entities.Reflection;
using System.Reflection;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Utilities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Maps;
using System.Text.RegularExpressions;
using Signum.Entities.Basics;
using Signum.Entities;
using System.IO;
using System.Globalization;
using Signum.Engine.Operations;

namespace Signum.Engine.Help
{
    public class EntityHelp
    {
        public Type Type;
        public string Description;
        public Dictionary<string, PropertyHelp> Properties;
        public Dictionary<Enum, OperationHelp> Operations;
        public Dictionary<object, QueryHelp> Queries
        {
            get { return HelpLogic.State.Value.GetQueryHelps(this.Type).ToDictionary(qh => qh.Key); }
        }
        public string FileName;
        public string Language;

        public static EntityHelp Create(Type t)
        {
            return new EntityHelp
            {
                Type = t,
                Language = CultureInfo.CurrentCulture.Name,
                Description = "",
                Properties = PropertyRoute.GenerateRoutes(t)
                            .ToDictionary(
                                pp => pp.PropertyString(),
                                pp => new PropertyHelp(pp, HelpGenerator.GetPropertyHelp(pp))),

                Operations = OperationLogic.GetAllOperationInfos(t)
                            .ToDictionary(
                                oi => oi.Key,
                                oi => new OperationHelp(oi.Key, HelpGenerator.GetOperationHelp(t, oi))),

             /*   Queries = DynamicQueryManager.Current.GetQueryNames(t)
                           .ToDictionary(
                                kvp => kvp.Key,
                                kvp => new QueryHelp(kvp.Key, HelpGenerator.GetQueryHelp(t, kvp.Value)))*/
            };
        }

        public XDocument ToXDocument()
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(_Entity, 
                       new XAttribute(_FullName, Type.FullName),
                       new XAttribute(_Language, Language),
                       new XElement(_Description, Description),
                       Properties.Let(ps => ps == null || ps.Count == 0 ? null : 
                           new XElement(_Properties,
                               ps.Select(p => new XElement(_Property, 
                                   new XAttribute(_Name, p.Key), 
                                   new XAttribute(_Info, p.Value.Info), 
                                   p.Value.UserDescription))
                           )
                       ),
                       Operations.Let(os => os == null || os.Count == 0 ? null : 
                           new XElement(_Operations,
                               os.Select(o => new XElement(_Operation, 
                                   new XAttribute(_Key, OperationDN.UniqueKey(o.Key)),
                                   new XAttribute(_Info, o.Value.Info),
                                   o.Value.UserDescription))
                           )
                       )/*,
                       Queries.Map(qs => qs == null || qs.Count == 0 ? null : 
                           new XElement(_Queries,
                               qs.Select(q => new XElement(_Query, 
                                   new XAttribute(_Key, QueryUtils.GetQueryName(q.Key)),
                                   new XAttribute(_Info, q.Value.Info),
                                   q.Value.UserDescription))
                           )
                       )*/
                   )
               );
        }


        public static EntityHelp Load(Type type, XDocument document, string sourceFile)
        {
            XElement element = document.Element(_Entity);

            return new EntityHelp
            {
                Type = type,
                FileName = sourceFile,
                Description = element.Element(_Description).TryCC(d => d.Value),
                Language = element.Attribute(_Language).Value,
                Properties = EnumerableExtensions.JoinStrict(
                    element.Element(_Properties).TryCC(ps => ps.Elements(_Property)) ?? new XElement[0],
                    PropertyRoute.GenerateRoutes(type),
                    x => x.Attribute(_Name).Value,
                    pp => pp.PropertyString(),
                    (x, pp) => new KeyValuePair<string, PropertyHelp>(
                         pp.PropertyString(),
                         new PropertyHelp(pp, x.Attribute(_Info).Value, x.Value)),
                    "loading Properties for {0} Help file ({1})".Formato(type.Name, sourceFile)).CollapseDictionary(),

                Operations = EnumerableExtensions.JoinStrict(
                    element.Element(_Operations).TryCC(os => os.Elements(_Operation)) ?? new XElement[0],
                    OperationLogic.GetAllOperationInfos(type),
                    x => x.Attribute(_Key).Value,
                    oi => OperationDN.UniqueKey(oi.Key),
                    (x, oi) => new KeyValuePair<Enum, OperationHelp>(
                        oi.Key, 
                        new OperationHelp(oi.Key,x.Attribute(_Info).Value, x.Value)),
                    "loading Operations for {0} Help file ({1})".Formato(type.Name, sourceFile)).CollapseDictionary(),

                /*Queries = EnumerableExtensions.JoinStrict(
                    element.Element(_Queries).TryCC(qs => qs.Elements(_Query)) ?? new XElement[0],
                    DynamicQueryManager.Current.GetQueryNames(type),
                    x => x.Attribute(_Key).Value,
                    qn => QueryUtils.GetQueryName(qn.Key),
                    (x, qn) => new KeyValuePair<object, QueryHelp>(
                        qn.Key, 
                        new QueryHelp(qn.Key, x.Attribute(_Info).Value, x.Value)),
                    "Loading Queries for {0} Help file ({1})".Formato(type.Name, sourceFile)).CollapseDictionary()*/
            };
        }

        public static void Synchronize(string fileName, Type type)
        {
            XElement loaded = XDocument.Load(fileName).Element(_Entity);
            var created = EntityHelp.Create(type);

            bool changed = false; 
            Action change = ()=>
            {
                if(!changed)
                {
                    Console.WriteLine("Synchronized {0} ".Formato(fileName));
                    changed= true;
                }
            };

            HelpTools.SynchronizeElements(loaded, _Properties, _Property, _Name, created.Properties, "Properties of {0}".Formato(type.Name),
                (action, prop) =>
                {
                    change();
                    Console.WriteLine("  Property {0}: {1}".Formato(action, prop));
                });

            HelpTools.SynchronizeElements(loaded, _Operations, _Operation, _Key, created.Operations.SelectDictionary(OperationDN.UniqueKey, v => v), "Operations of {0}".Formato(type.Name),
                (action, op) =>
                {
                    change();
                    Console.WriteLine("  Operation {0}: {1}".Formato(action, op));
                });

            string goodFileName = DefaultFileName(type);
            if (fileName != goodFileName)
            {
                Console.WriteLine("FileNameChanged {0} -> {1}".Formato(fileName, goodFileName));
                File.Delete(fileName);
                loaded.Save(goodFileName);
            }
            else
            {
                loaded.Save(fileName);
            }

            if (changed)
                Console.WriteLine();
        }

        internal static bool Distinct(XAttribute a1, XAttribute a2)
        {
            if (a1 == null && a2 == null)
                return true;

            if (a1 == null || a2 == null)
                return false;

            return a1.Value != a2.Value;
        }

        static readonly XName _FullName = "FullName";
        static readonly XName _Name = "Name";
        static readonly XName _Key = "Key";
        static readonly XName _Entity = "Entity";
        static readonly XName _Description = "Description";
        static readonly XName _Properties = "Properties";
        static readonly XName _Property = "Property";
        static readonly XName _Operations = "Operations";
        static readonly XName _Operation = "Operation";
        static readonly XName _Queries = "Queries";
        static readonly XName _Query = "Query";
        static readonly XName _Info = "Info";
        static readonly XName _Language = "Language";

        public string Extract(string s, Match m)
        {
            return Extract(s, m.Index, m.Index + m.Length);
        }

        public string Extract(string s, int low, int high)
        {
            if (s.Length <= etcLength) return s;

            int m = (low + high) / 2;
            int limMin = m - lp2;
            int limMax = m + lp2;
            if (limMin < 0)
            {
                limMin = 0;
                limMax = etcLength;
            }
            if (limMax > s.Length)
            {
                limMax = s.Length;
                limMin = limMax - etcLength;
            }

            return (limMin != 0 ? "..." : "") 
            + s.Substring(limMin, limMax - limMin)
            + (limMax != high ? "..." : "");
        }

        const int etcLength = 300;
        const int lp2 = etcLength / 2;

        public IEnumerable<SearchResult> Search(Regex regex)
        {
            //Types
            Match m;
            m = regex.Match(Type.NiceName().RemoveDiacritics());
            if (m.Success)
            {
                yield return new SearchResult(TypeSearchResult.Type, Type.NiceName(), !string.IsNullOrEmpty(Description) ? Description.Etc(etcLength) : Type.NiceName(), Type, m, HelpLogic.EntityUrl(Type));
                yield break;
            }


            //Types description
            if (Description.HasText())
            {
                // TODO: Some times the rendered Description does not contain the query term and it looks strange. Description should be
                // wiki-parsed and then make the search over this string
                if (m.Success)
                {
                    yield return new SearchResult(TypeSearchResult.TypeDescription, Type.NiceName(), Extract(Description, m), Type, m, HelpLogic.EntityUrl(Type));
                    yield break;
                }
            }

            //Properties (key)
            if (Properties != null)
                foreach (var p in Properties)
                {
                    m = regex.Match(p.Key.RemoveDiacritics());
                    if (m.Success)
                        yield return new SearchResult(TypeSearchResult.Property, p.Key.NiceName(), p.Value.ToString().Etc(etcLength), Type, m, HelpLogic.EntityUrl(Type) + "#" + "p-" + p.Key);
                    else
                    {
                        m = regex.Match(p.Value.ToString().RemoveDiacritics());
                        if (m.Success)
                            yield return new SearchResult(TypeSearchResult.PropertyDescription, p.Key.NiceName(), Extract(p.Value.ToString(), m), Type, m, HelpLogic.EntityUrl(Type) + "#" + "p-" + p.Key);
                    }
                }

            //Queries (key)
            //TODO: Añadir UserDescriptions a las búsquedas + campos
            var queries = Queries.Values;
            if (queries != null)
                foreach (var p in queries)
                {
                    m = regex.Match(QueryUtils.GetNiceName(p.Key).RemoveDiacritics());
                    if (m.Success)
                        yield return new SearchResult(TypeSearchResult.Query, QueryUtils.GetNiceName(p.Key), p.Info.ToString().Etc(etcLength), Type, m, HelpLogic.EntityUrl(Type) + "#" + "q-" + QueryUtils.GetQueryUniqueKey(p.Key).ToString().Replace(".", "_"));
                    else
                    {
                        m = regex.Match(p.Info.ToString().RemoveDiacritics());
                        if (m.Success)
                            yield return new SearchResult(TypeSearchResult.QueryDescription, QueryUtils.GetNiceName(p.Key), Extract(p.Info.ToString(), m), Type, m, HelpLogic.EntityUrl(Type) + "#" + "q-" + QueryUtils.GetQueryUniqueKey(p.Key).ToString().Replace(".", "_"));
                    }
                }

            //Operations (key)
            if (Operations != null)
                foreach (var p in Operations)
                {
                    m = regex.Match(p.Key.NiceToString().RemoveDiacritics());
                    if (m.Success)
                        yield return new SearchResult(TypeSearchResult.Operation, p.Key.NiceToString(), p.Value.ToString().Etc(etcLength), Type, m, HelpLogic.EntityUrl(Type) + "#o-" + OperationDN.UniqueKey(p.Key).Replace('.', '_'));
                    else
                    {
                        m = regex.Match(p.Value.ToString().RemoveDiacritics());
                        if (m.Success)
                            yield return new SearchResult(TypeSearchResult.OperationDescription, p.Key.NiceToString(), Extract(p.Value.ToString(), m), Type, m, HelpLogic.EntityUrl(Type) + "#o-" + OperationDN.UniqueKey(p.Key).Replace('.', '_'));
                    }
                }
        }

        public static string QueryToString(KeyValuePair<object, string> kvp)
        {
            return QueryUtils.GetNiceName(kvp.Key) + " | " + kvp.Value;
        }

        public static string OperationToString(KeyValuePair<Enum, string> kvp)
        {
            return kvp.Key.NiceToString() + " | " + kvp.Value;
        }

        public static string EntityTypeToString(KeyValuePair<Type, EntityHelp> kvp)
        {
            return kvp.Key.NiceName() + " | " + kvp.Value.Description;
        }

        public string Save()
        {
            XDocument document = this.ToXDocument();
            string path = DefaultFileName(this.Type);
            document.Save(path);
            return path;
        }

        static string DefaultFileName(Type type)
        {
            return Path.Combine(Path.Combine(HelpLogic.HelpDirectory,HelpLogic.EntitiesDirectory), "{0}.help".Formato(type.FullName));
        }

        internal static string GetEntityFullName(XDocument document)
        {
            if (document.Root.Name == _Entity)
                return document.Root.Attribute(_FullName).Value;
            return null;
        }
    }

    public class PropertyHelp
    {
        public PropertyHelp(PropertyRoute propertyRoute, string info)
        {
            if(propertyRoute.PropertyRouteType != PropertyRouteType.FieldOrProperty)
                throw new ArgumentException("propertyRoute should be of type Property"); 

            this.PropertyRoute = propertyRoute;
            this.Info = info;
        }

        public PropertyHelp(PropertyRoute propertyRoute, string info, string userDescription)
            : this(propertyRoute, info)
        {
            this.UserDescription = userDescription;
        }

        public string Info { get; private set; }
        public string UserDescription { get; set; }
        public PropertyInfo PropertyInfo { get { return PropertyRoute.PropertyInfo; } }
        public PropertyRoute PropertyRoute { get; private set; }

        public override string ToString()
        {
            return Info + (UserDescription.HasText() ? " | " + UserDescription : "");
        }
    }

    public class OperationHelp
    {
        public OperationHelp(Enum operationKey, string info)
        {
            this.OperationKey = operationKey;
            this.Info = info;
        }

        public OperationHelp(Enum operationKey, string info, string userDescription)
        {
            this.OperationKey = operationKey;
            this.Info = info;
            this.UserDescription = userDescription;
        }

        public Enum OperationKey { get; set; }
        public string Info { get; private set; }
        public string UserDescription { get; set; }

        public override string ToString()
        {
            return Info + (UserDescription.HasText() ? " | " + UserDescription : "");
        }
    }

  /*  public class QueryHelp
    {
        public QueryHelp(object queryKey, string info)
        {
            this.QueryKey = queryKey;
            this.Info = info;
        }

        public QueryHelp(object queryKey, string info, string userDescription)
        {
            this.QueryKey = queryKey;
            this.Info = info;
            this.UserDescription = userDescription;
        }

        public object QueryKey { get; set; }
        public string Info { get; private set; }
        public string UserDescription { get; set; }

        public override string ToString()
        {
            return Info + (UserDescription.HasText() ? " | " + UserDescription : "");
        }
    }*/

    public enum TypeSearchResult
    {
        Type,
        TypeDescription,
        Property,
        PropertyDescription,
        Query,
        QueryDescription,
        Operation,
        OperationDescription,
        Appendix,
        AppendixDescription,
        NamespaceDescription
    }

    public enum MatchType
    {
        Total,
        StartsWith,
        Contains
    }

    public class SearchResult : IComparable<SearchResult>
    {
        public TypeSearchResult TypeSearchResult { get; set; }
        public string ObjectName { get; set; }
        public Type Type { get; set; }
        public Match Match { get; set; }
        public MatchType MatchType { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }

        public string TryObjectName
        {
            get
            {
                if (TypeSearchResult == TypeSearchResult.Type || TypeSearchResult == TypeSearchResult.TypeDescription)
                    return null;
                return ObjectName;
            }
        }

        public SearchResult(TypeSearchResult typeSearchResult, string objectName, string description, Type type, Match match, string link)
        {
            this.ObjectName = objectName;
            this.TypeSearchResult = typeSearchResult;
            this.Description = description;
            this.Type = type;
            this.Match = match;
            this.Link = link;

            if (Match.Index == 0)
            {
                if (Match.Length == objectName.Length)
                    MatchType = MatchType.Total;
                else
                    MatchType = MatchType.StartsWith;
            }
            else
            {
                MatchType = MatchType.Contains;
            }
        }

        public int CompareTo(SearchResult other)
        {
            int result = TypeSearchResult.CompareTo(other.TypeSearchResult);
            
            if (result != 0)
                return result;

            return MatchType.CompareTo(other.MatchType);
        }
    }
}
