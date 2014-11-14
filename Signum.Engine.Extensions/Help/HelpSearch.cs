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
                    return new SearchResult(TypeSearchResult.Appendix, entity.Title, entity.Description.Etc(etcLength).DefaultText(entity.Title), null, m, 
                        HelpUrls.AppendixUrl(entity.UniqueName));
                }
            }

            if (entity.Description.HasText())
            {
                Match m = regex.Match(entity.Description.RemoveDiacritics());
                if (m.Success)
                {
                    return new SearchResult(TypeSearchResult.Appendix, entity.Title, entity.Description.Extract(m), null, m, HelpUrls.AppendixUrl(entity.UniqueName), isDescription:true);
                }
            }

            return null;
        }


        public static SearchResult Search(this NamespaceHelp entity, Regex regex)
        {
            {
                Match m = regex.Match(entity.Title.RemoveDiacritics());
                if (m.Success)
                {
                    return new SearchResult(TypeSearchResult.Namespace, entity.Title, entity.Description.Etc(etcLength).DefaultText(entity.Title), null, m, HelpUrls.NamespaceUrl(entity.Namespace));
                }
            }


            if (entity.Description.HasText())
            {
                Match m = regex.Match(entity.Description.RemoveDiacritics());
                if (m.Success)
                {
                    return new SearchResult(TypeSearchResult.Namespace, entity.Title, entity.Description.Extract(m), null, m, HelpUrls.NamespaceUrl(entity.Namespace), isDescription: true);
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
                yield return new SearchResult(TypeSearchResult.Type, type.NiceName(), entity.Description.DefaultText(entity.Info).Etc(etcLength), type, m, HelpUrls.EntityUrl(type));
                yield break;
            }


            //Types description
            if (entity.Description.HasText())
            {
                // TODO: Some times the rendered Description does not contain the query term and it looks strange. Description should be
                // wiki-parsed and then make the search over this string
                if (m.Success)
                {
                    yield return new SearchResult(TypeSearchResult.Type, type.NiceName(), entity.Description.Extract(m), type, m, HelpUrls.EntityUrl(type), isDescription: true);
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
                        yield return new SearchResult(TypeSearchResult.Property, p.PropertyInfo.NiceName(), p.UserDescription.DefaultText(p.Info).Etc(etcLength), type, m, 
                            HelpUrls.PropertyUrl(p.PropertyRoute));
                        continue;
                    }
                }
                else if (p.UserDescription.HasText())
                {
                    m = regex.Match(p.UserDescription.RemoveDiacritics());
                    if (m.Success)
                        yield return new SearchResult(TypeSearchResult.Property, p.PropertyInfo == null ? null : p.PropertyInfo.NiceName(), p.UserDescription.Extract(m), type, m, 
                            HelpUrls.PropertyUrl(p.PropertyRoute), isDescription: true);
                }
            }

            //Queries (key)
            foreach (var p in entity.Queries.Values)
            {
                m = regex.Match(QueryUtils.GetNiceName(p.QueryName).RemoveDiacritics());
                if (m.Success)
                {
                    yield return new SearchResult(TypeSearchResult.Query, QueryUtils.GetNiceName(p.QueryName), p.UserDescription.DefaultText(p.Info).Etc(etcLength), type, m,
                        HelpUrls.QueryUrl(p.QueryName, type));
                }
                else if (p.UserDescription.HasText())
                {
                    m = regex.Match(p.UserDescription.ToString().RemoveDiacritics());
                    if (m.Success)
                        yield return new SearchResult(TypeSearchResult.Query, QueryUtils.GetNiceName(p.QueryName), p.UserDescription.Extract(m), type, m,
                            HelpUrls.QueryUrl(p.QueryName, type), isDescription: true);
                }
            }

            //Operations (key)
            foreach (var op in entity.Operations.Values)
            {
                m = regex.Match(op.OperationSymbol.NiceToString().RemoveDiacritics());
                if (m.Success)
                {
                    yield return new SearchResult(TypeSearchResult.Operation, op.OperationSymbol.NiceToString(), op.UserDescription.DefaultText(op.Info).Etc(etcLength), type, m,
                        HelpUrls.OperationUrl(type, op.OperationSymbol));
                }
                else if (op.UserDescription.HasText())
                {
                    m = regex.Match(op.UserDescription.ToString().RemoveDiacritics());
                    if (m.Success)
                        yield return new SearchResult(TypeSearchResult.Operation, op.OperationSymbol.NiceToString(), op.UserDescription.Extract(m), type, m,
                            HelpUrls.OperationUrl(type, op.OperationSymbol), isDescription: true);
                }
            }
        }

    }

    

    public enum MatchType
    {
        Total,
        StartsWith,
        Contains
    }

    public class SearchResult
    {
        public TypeSearchResult TypeSearchResult { get; set; }
        public string ObjectName { get; set; }
        public Type Type { get; set; }
        public MatchType MatchType { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public bool IsDescription { get; set; }

        public SearchResult(TypeSearchResult typeSearchResult, string objectName, string description, Type type, Match match, string link, bool isDescription = false)
        {
            this.ObjectName = objectName;
            this.TypeSearchResult = typeSearchResult;
            this.Description = description;
            this.Link = link;
            this.Type = type;
            
            this.MatchType = 
                match.Index == 0 && match.Length == objectName.Length ? MatchType.Total :
                match.Index == 0 ? MatchType.StartsWith :
                MatchType.Contains;

            this.IsDescription = isDescription;
        }
    }
}
