using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Help;
using Signum.Utilities;

namespace Signum.Engine.Help
{
    public static class HelpSearch
    {
        public static SearchResult Search(this AppendixHelp entity, Regex regex)
        {
            {
                Match m = regex.Match(entity.Title.RemoveDiacritics());
                if (m.Success)
                {
                    return new SearchResult(TypeSearchResult.Appendix, entity.UniqueName, entity.Description.Extract(m), null, m, HelpUrls.AppendixUrl(entity.UniqueName));
                }
            }

            {
                Match m = regex.Match(entity.Description.RemoveDiacritics());
                if (m.Success)
                {
                    return new SearchResult(TypeSearchResult.AppendixDescription, entity.UniqueName, entity.Description.Extract(m), null, m, HelpUrls.AppendixUrl(entity.UniqueName));
                }
            }

            return null;
        }


        public static SearchResult Search(this NamespaceHelp entity, Regex regex)
        {
            {
                Match m = regex.Match(entity.Description.RemoveDiacritics());
                if (m.Success)
                {
                    return new SearchResult(TypeSearchResult.NamespaceDescription, entity.Namespace, entity.Description.Extract(m), null, m, HelpUrls.NamespaceUrl(entity.Namespace));
                }
            }

            return null;
        }

        const int etcLength = 300;
        const int lp2 = etcLength / 2;

        public static IEnumerable<SearchResult> Search(this EntityHelp entity, Regex regex)
        {
            Type type = entity.Type;

            //Types
            Match m;
            m = regex.Match(type.NiceName().RemoveDiacritics());
            if (m.Success)
            {
                yield return new SearchResult(TypeSearchResult.Type, type.NiceName(), !string.IsNullOrEmpty(entity.Description) ? entity.Description.Etc(etcLength) : type.NiceName(), type, m, HelpUrls.EntityUrl(type));
                yield break;
            }


            //Types description
            if (entity.Description.HasText())
            {
                // TODO: Some times the rendered Description does not contain the query term and it looks strange. Description should be
                // wiki-parsed and then make the search over this string
                if (m.Success)
                {
                    yield return new SearchResult(TypeSearchResult.TypeDescription, type.NiceName(), entity.Description.Extract(m), type, m, HelpUrls.EntityUrl(type));
                    yield break;
                }
            }

            
            //Properties (key)
            foreach (var p in  entity.Properties.Values)
            {
                if (p.PropertyInfo != null)
                {
                    m = regex.Match(p.PropertyInfo.NiceName().RemoveDiacritics());
                    if (m.Success)
                    {
                        yield return new SearchResult(TypeSearchResult.Property, p.PropertyInfo.NiceName(), p.Info.Etc(etcLength), type, m, HelpUrls.EntityUrl(type) + "#" + "p-" + p.PropertyRoute.PropertyString());
                        continue;
                    }
                }
                else if (p.UserDescription.HasText())
                {
                    m = regex.Match(p.UserDescription.RemoveDiacritics());
                    if (m.Success)
                        yield return new SearchResult(TypeSearchResult.PropertyDescription, p.PropertyInfo == null ? null:  p.PropertyInfo.NiceName(), p.UserDescription.Extract(m), type, m, HelpUrls.EntityUrl(type) + "#" + "p-" + p.PropertyRoute.PropertyString());
                }
            }

            //Queries (key)
            foreach (var p in entity.Queries.Values)
            {
                m = regex.Match(QueryUtils.GetNiceName(p.Key).RemoveDiacritics());
                if (m.Success)
                    yield return new SearchResult(TypeSearchResult.Query, QueryUtils.GetNiceName(p.Key), p.Info.ToString().Etc(etcLength), type, m, HelpUrls.EntityUrl(type) + "#" + "q-" + QueryUtils.GetQueryUniqueKey(p.Key).ToString().Replace(".", "_"));
                else if (p.UserDescription.HasText())
                {
                    m = regex.Match(p.UserDescription.ToString().RemoveDiacritics());
                    if (m.Success)
                        yield return new SearchResult(TypeSearchResult.QueryDescription, QueryUtils.GetNiceName(p.Key), p.UserDescription.Extract(m), type, m, HelpUrls.EntityUrl(type) + "#" + "q-" + QueryUtils.GetQueryUniqueKey(p.Key).ToString().Replace(".", "_"));
                }
            }

            //Operations (key)
            foreach (var op in entity.Operations.Values)
            {
                m = regex.Match(op.OperationSymbol.NiceToString().RemoveDiacritics());
                if (m.Success)
                    yield return new SearchResult(TypeSearchResult.Operation, op.OperationSymbol.NiceToString(), op.Info.ToString().Etc(etcLength), type, m, HelpUrls.EntityUrl(type) + "#o-" + op.OperationSymbol.ToString().Replace('.', '_'));
                else if(op.UserDescription.HasText())
                {
                    m = regex.Match(op.UserDescription.ToString().RemoveDiacritics());
                    if (m.Success)
                        yield return new SearchResult(TypeSearchResult.OperationDescription, op.OperationSymbol.NiceToString(), op.UserDescription.ToString().Extract(m), type, m, HelpUrls.EntityUrl(type) + "#o-" + op.OperationSymbol.ToString().Replace('.', '_'));
                }
            }
        }

    }

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
