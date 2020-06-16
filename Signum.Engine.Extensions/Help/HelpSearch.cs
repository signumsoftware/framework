using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Signum.Engine.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Help;
using Signum.Utilities;

namespace Signum.Engine.Help
{
    public static class HelpSearch
    {
        public static SearchResult? Search(this AppendixHelpEntity entity, Regex regex)
        {
            {
                Match m = regex.Match(entity.Title.RemoveDiacritics());
                if (m.Success)
                {
                    return new SearchResult(TypeSearchResult.Appendix, entity.Title, entity.Description.Try(d => d.Etc(etcLength)).DefaultText(entity.Title), m, entity.UniqueName);
                }
            }

            if (entity.Description.HasText())
            {
                Match m = regex.Match(entity.Description.RemoveDiacritics());
                if (m.Success)
                {
                    return new SearchResult(TypeSearchResult.Appendix, entity.Title, entity.Description.Extract(m), m, entity.UniqueName, isDescription: true);
                }
            }

            return null;
        }


        public static SearchResult? Search(this NamespaceHelp nh, Regex regex)
        {
            {
                Match m = regex.Match(nh.Title.RemoveDiacritics());
                if (m.Success)
                {
                    return new SearchResult(TypeSearchResult.Namespace, nh.Title, nh.Description.Try(d => d.Etc(etcLength)).DefaultText(nh.Title), m, nh.Namespace);
                }
            }

            if (nh.Description.HasText())
            {
                Match m = regex.Match(nh.Description.RemoveDiacritics());
                if (m.Success)
                {
                    return new SearchResult(TypeSearchResult.Namespace, nh.Title, nh.Description.Extract(m), m, nh.Namespace, isDescription: true);
                }
            }

            return null;
        }

        const int etcLength = 300;
        const int lp2 = etcLength / 2;

        public static IEnumerable<SearchResult> Search(this TypeHelp th, Regex regex)
        {
            Type type = th.Type;

            //Types
            Match m;
            m = regex.Match(type.NiceName().RemoveDiacritics());
            if (m.Success)
            {
                yield return new SearchResult(TypeSearchResult.Type, type.NiceName(), th.DBEntity?.Description.DefaultText(th.Info).Etc(etcLength), m, TypeLogic.GetCleanName(type));
                yield break;
            }


            //Types description
            if (th.DBEntity != null && th.DBEntity.Description.HasText())
            {
                // TODO: Some times the rendered Description does not contain the query term and it looks strange. Description should be
                // wiki-parsed and then make the search over this string
                if (m.Success)
                {
                    yield return new SearchResult(TypeSearchResult.Type, type.NiceName(), th.DBEntity.Description.Extract(m), m, TypeLogic.GetCleanName(type), isDescription: true);
                    yield break;
                }
            }

            
            //Properties (key)
            foreach (var p in th.Properties.Values)
            {
                {
                    m = regex.Match(p.PropertyInfo.NiceName().RemoveDiacritics());
                    if (m.Success)
                    {
                        yield return new SearchResult(TypeSearchResult.Property, p.PropertyInfo.NiceName(), p.UserDescription.DefaultText(p.Info).Etc(etcLength), m,
                            TypeLogic.GetCleanName(type), p.PropertyRoute.ToString());
                        continue;
                    }
                }

                if (p.UserDescription.HasText())
                {
                    m = regex.Match(p.UserDescription.RemoveDiacritics());
                    if (m.Success)
                        yield return new SearchResult(TypeSearchResult.Property, p.PropertyInfo.NiceName(), p.UserDescription.Extract(m), m,
                            TypeLogic.GetCleanName(type), p.PropertyRoute.ToString(), isDescription: true);
                }
            }

            //Queries (key)
            foreach (var p in th.Queries.Values)
            {
                m = regex.Match(QueryUtils.GetNiceName(p.QueryName).RemoveDiacritics());
                if (m.Success)
                {
                    yield return new SearchResult(TypeSearchResult.Query, QueryUtils.GetNiceName(p.QueryName), p.UserDescription.DefaultText(p.Info).Etc(etcLength), m,
                        TypeLogic.GetCleanName(type), QueryUtils.GetKey(p.QueryName));
                }
                else if (p.UserDescription.HasText())
                {
                    m = regex.Match(p.UserDescription.ToString().RemoveDiacritics());
                    if (m.Success)
                        yield return new SearchResult(TypeSearchResult.Query, QueryUtils.GetNiceName(p.QueryName), p.UserDescription.Extract(m), m,
                            TypeLogic.GetCleanName(type), QueryUtils.GetKey(p.QueryName), isDescription: true);
                }
            }

            //Operations (key)
            foreach (var op in th.Operations.Values)
            {
                m = regex.Match(op.OperationSymbol.NiceToString().RemoveDiacritics());
                if (m.Success)
                {
                    yield return new SearchResult(TypeSearchResult.Operation, op.OperationSymbol.NiceToString(), op.UserDescription.DefaultText(op.Info).Etc(etcLength), m,
                        TypeLogic.GetCleanName(type), op.OperationSymbol.Key);
                }
                else if (op.UserDescription.HasText())
                {
                    m = regex.Match(op.UserDescription.ToString().RemoveDiacritics());
                    if (m.Success)
                        yield return new SearchResult(TypeSearchResult.Operation, op.OperationSymbol.NiceToString(), op.UserDescription.Extract(m), m,
                            TypeLogic.GetCleanName(type), op.OperationSymbol.Key, isDescription: true);
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
        public string Title { get; set; }
        public MatchType MatchType { get; set; }
        public string? Description { get; set; }
        public string Key { get; set; }
        public string? Key2 { get; set; }
        public bool IsDescription { get; set; }

        public SearchResult(TypeSearchResult typeSearchResult, string title, string? description, Match match, string key, string? key2 = null, bool isDescription = false)
        {
            this.Title = title;
            this.TypeSearchResult = typeSearchResult;
            this.Description = description;
            this.Key = key;
            this.Key2 = key2;
            
            this.MatchType = 
                match.Index == 0 && match.Length == title.Length ? MatchType.Total :
                match.Index == 0 ? MatchType.StartsWith :
                MatchType.Contains;

            this.IsDescription = isDescription;
        }
    }
}
