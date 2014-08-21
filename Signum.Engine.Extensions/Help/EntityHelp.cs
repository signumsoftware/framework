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
        public Dictionary<OperationSymbol, OperationHelp> Operations;
        public Dictionary<object, QueryHelp> Queries
        {
            get { return HelpLogic.State.Value.GetQueryHelps(this.Type).ToDictionary(qh => qh.Key); }
        }
        public string FileName;
        public string Language;

        public static EntityHelp Create(Type type)
        {
            return new EntityHelp
            {
                Type = type,
                Language = CultureInfo.CurrentCulture.Name,
                Description = null,
                FileName = Path.Combine(Path.Combine(HelpLogic.HelpDirectory,HelpLogic.EntitiesDirectory), "{0}.help".Formato(type.FullName)),
                Properties = PropertyRoute.GenerateRoutes(type)
                            .ToDictionary(
                                pp => pp.PropertyString(),
                                pp => new PropertyHelp(pp, HelpGenerator.GetPropertyHelp(pp))),

                Operations = OperationLogic.GetAllOperationInfos(type)
                            .ToDictionary(
                                oi => oi.OperationSymbol,
                                oi => new OperationHelp(oi.OperationSymbol, HelpGenerator.GetOperationHelp(type, oi))),

           
            };
        }

        public XDocument ToXDocument()
        {
            var props =  Properties.Where(a => a.Value.UserDescription.HasText());
            var opers =  Operations.Where(a => a.Value.UserDescription.HasText());

            if (string.IsNullOrEmpty(Description) && !props.Any() && !opers.Any())
                return null;

            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(_Entity, 
                       new XAttribute(_FullName, Type.FullName),
                       new XAttribute(_Language, Language),
                       Description.HasText()? new XElement(_Description, Description) : null,
                       props.Any() ? new XElement(_Properties,
                           props.Select(p => new XElement(_Property, 
                               new XAttribute(_Name, p.Key), 
                               p.Value.UserDescription))
                       ) : null,
                       opers.Any() ? new XElement(_Operations,
                           opers.Select(o => new XElement(_Operation, 
                               new XAttribute(_Key, o.Key),
                               o.Value.UserDescription))
                       ) : null
                   )
               );
        }


        public EntityHelp Load()
        {
            if (!File.Exists(FileName))
                return this;

            XElement element = HelpLogic.LoadAndValidate(FileName).Element(_Entity);

            Description = element.Element(_Description).Try(d => d.Value);

            var ps = element.Element(_Properties);
            if (ps != null)
            {
                string errorMessage = "loading property {0} on Entity file (" + FileName + ")";
                foreach (var item in ps.Elements(_Property))
                {
                    this.Properties.GetOrThrow(item.Attribute(_Name).Value, errorMessage).UserDescription = item.Value;
                }
            }

            var os = element.Element(_Operations);
            if (os != null)
            {
                string errorMessage = "loading property {0} on Entity file (" + FileName + ")";
                var ops = Operations.SelectDictionary(s => s.Key, v => v);
                foreach (var item in os.Elements(_Operation))
                {
                    ops.GetOrThrow(item.Attribute(_Key).Value, errorMessage).UserDescription = item.Value;
                }
            }

            return this;
        }

        public static void Synchronize(string fileName, XDocument loaded, Type type, Func<string, string> syncContent)
        {
            XElement loadedEntity = loaded.Element(_Entity);
            EntityHelp created = EntityHelp.Create(type);

            bool changed = false;
            Action change = () =>
            {
                if (!changed)
                {
                    Console.WriteLine("Synchronized {0} ".Formato(fileName));
                    changed = true;
                }
            };

            created.Description = syncContent(loadedEntity.Element(_Description).Try(a => a.Value));

            HelpTools.SynchronizeElements(loadedEntity, _Properties, _Property, _Name, created.Properties, "Properties of {0}".Formato(type.Name),
                (ph, elem) => ph.UserDescription = syncContent(elem.Value),
                (action, prop) =>
                {
                    change();
                    Console.WriteLine("  Property {0}: {1}".Formato(action, prop));
                });

            HelpTools.SynchronizeElements(loadedEntity, _Operations, _Operation, _Key, created.Operations.SelectDictionary(os=>os.Key, v => v), "Operations of {0}".Formato(type.Name),
                (oh, op) => oh.UserDescription = syncContent(op.Value),
                (action, op) =>
                {
                    change();
                    Console.WriteLine("  Operation {0}: {1}".Formato(action, op));
                });


            if (fileName != created.FileName)
            {
                Console.WriteLine("FileName changed {0} -> {1}".Formato(fileName, created.FileName));
                File.Move(fileName, created.FileName);
            }

            if (!created.Save())
                Console.WriteLine("File deleted {1}".Formato(fileName, created.FileName));
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
                foreach (var kvp in Operations)
                {
                    m = regex.Match(kvp.Key.NiceToString().RemoveDiacritics());
                    if (m.Success)
                        yield return new SearchResult(TypeSearchResult.Operation, kvp.Key.NiceToString(), kvp.Value.ToString().Etc(etcLength), Type, m, HelpLogic.EntityUrl(Type) + "#o-" + kvp.Key.Key.Replace('.', '_'));
                    else
                    {
                        m = regex.Match(kvp.Value.ToString().RemoveDiacritics());
                        if (m.Success)
                            yield return new SearchResult(TypeSearchResult.OperationDescription, kvp.Key.NiceToString(), Extract(kvp.Value.ToString(), m), Type, m, HelpLogic.EntityUrl(Type) + "#o-" + kvp.Key.Key.Replace('.', '_'));
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

        internal static string GetEntityFullName(XDocument document, string fileName)
        {
            if (document.Root.Name != _Entity)
                throw new InvalidOperationException("{0} does not have a {1} root".Formato(fileName, _Entity));

            var result = document.Root.Attribute(_FullName).Try(a => a.Value);

            if (string.IsNullOrEmpty(result))
                throw new InvalidOperationException("{0} does not have a {1} attribute".Formato(fileName, _FullName));

            return result;
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


        public string Info { get; private set; }
        public string UserDescription;
        public PropertyInfo PropertyInfo { get { return PropertyRoute.PropertyInfo; } }
        public PropertyRoute PropertyRoute { get; private set; }

        public override string ToString()
        {
            return Info + (UserDescription.HasText() ? " | " + UserDescription : "");
        }
    }

    public class OperationHelp
    {
        public OperationHelp(OperationSymbol operationSymbol, string info)
        {
            this.OperationSymbol = operationSymbol;
            this.Info = info;
        }

        public OperationSymbol OperationSymbol { get; set; }
        public string Info { get; private set; }
        public string UserDescription;

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
