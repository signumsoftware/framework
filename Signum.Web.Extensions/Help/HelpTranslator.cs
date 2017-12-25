using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.Help;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Help;
using Signum.Utilities;

namespace Signum.Web.Help
{
    public static class HelpTranslator
    {
        public static void AssignTranslatedFullEntity(this EntityHelpEntity entity, CultureInfoEntity fromCulture)
        {
            var fromEntity = Database.Query<EntityHelpEntity>().SingleOrDefaultEx(e => e.Type == entity.Type && e.Culture == fromCulture);

            if(fromEntity != null)
            {
                AssignTranslatedEntity(entity, fromEntity);
            }

            var queries = HelpLogic.TypeToQuery.Value.TryGetC(entity.Type.ToType()).EmptyIfNull().Select(QueryLogic.GetQueryEntity).ToList();

            foreach (var q in queries)
            {
                var fromQuery = Database.Query<QueryHelpEntity>().SingleOrDefaultEx(e => e.Query == q && e.Culture == fromCulture);

                if (fromQuery != null)
                {
                    var query = Database.Query<QueryHelpEntity>().SingleOrDefaultEx(e => e.Query == q && e.Culture == entity.Culture) ??
                        new QueryHelpEntity { Culture = entity.Culture, Query = q };

                    AsignTranslatedQuery(query, fromQuery);
                }
            }
        }

        private static Dictionary<string, string> Translate(HashSet<string> toTranslate, string fromCulture, string toCulture)
        {
            var translated = Translation.TranslationClient.Translator.TranslateBatch(toTranslate.ToList(), fromCulture, toCulture);

            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.AddRange(toTranslate.ToList(), translated);
            return dic;
        }

        static void AssignTranslatedEntity(EntityHelpEntity entity, EntityHelpEntity fromEntity)
        {
            HashSet<string> toTranslate = new HashSet<string>();
            if (!entity.Description.HasText() && fromEntity.Description.HasText())
                toTranslate.Add(fromEntity.Description);

            foreach (var fromProp in fromEntity.Properties)
            {
                var prop = entity.Properties.SingleOrDefaultEx(p => p.Property.Is(fromProp.Property));

                if (prop == null || !prop.Description.HasText())
                    toTranslate.Add(fromProp.Description);
            }

            Dictionary<string, string> dic = Translate(toTranslate, fromEntity.Culture.Name, entity.Culture.Name);

            if (!entity.Description.HasText() && fromEntity.Description.HasText())
                entity.Description = dic.GetOrThrow(fromEntity.Description);

            foreach (var fromProp in fromEntity.Properties)
            {
                var prop = entity.Properties.SingleOrDefaultEx(p => p.Property.Is(fromProp.Property));

                if (prop == null)
                {
                    entity.Properties.Add(new PropertyRouteHelpEmbedded
                    {
                        Property = fromProp.Property,
                        Description = dic.GetOrThrow(fromProp.Description)
                    });
                }
                else if(!prop.Description.HasText())
                {
                    prop.Description = dic.GetOrThrow(fromProp.Description); 
                }
            }

            entity.Execute(EntityHelpOperation.Save);
        }

        static void AsignTranslatedQuery(QueryHelpEntity query, QueryHelpEntity fromQuery)
        {
            HashSet<string> toTranslate = new HashSet<string>();
            if (!query.Description.HasText() && fromQuery.Description.HasText())
                toTranslate.Add(fromQuery.Description);

            foreach (var fromProp in fromQuery.Columns)
            {
                var prop = query.Columns.SingleOrDefaultEx(p => p.ColumnName == fromProp.ColumnName);

                if (prop == null || prop.Description.HasText())
                    toTranslate.Add(fromProp.Description);
            }

            Dictionary<string, string> dic = Translate(toTranslate, fromQuery.Culture.Name, query.Culture.Name);

            if (!query.Description.HasText() && fromQuery.Description.HasText())
                query.Description = dic.GetOrThrow(fromQuery.Description);

            foreach (var fromProp in fromQuery.Columns)
            {
                var col = query.Columns.SingleOrDefaultEx(p => p.ColumnName == fromProp.ColumnName);

                if (col == null)
                {
                    query.Columns.Add(new QueryColumnHelpEmbedded
                    {
                        ColumnName = fromProp.ColumnName,
                        Description = dic.GetOrThrow(fromProp.Description)
                    });
                }
                else if (!col.Description.HasText())
                {
                    col.Description = dic.GetOrThrow(fromProp.Description);
                }
            }

            query.Execute(QueryHelpOperation.Save);
        }
        
        public static void AsignTranslatedNamespace(this NamespaceHelpEntity @namespace, CultureInfoEntity fromCulture)
        {
            var fromNamespace = Database.Query<NamespaceHelpEntity>().SingleEx(n => n.Name == @namespace.Name && n.Culture == fromCulture);

            HashSet<string> toTranslate = new HashSet<string>();
            if (!@namespace.Description.HasText() && fromNamespace.Description.HasText())
                toTranslate.Add(fromNamespace.Description);

            Dictionary<string, string> dic = Translate(toTranslate, fromNamespace.Culture.Name, @namespace.Culture.Name);
            if (!@namespace.Description.HasText() && fromNamespace.Description.HasText())
                @namespace.Description = dic.GetOrThrow(fromNamespace.Description);

            @namespace.Execute(NamespaceHelpOperation.Save);
        }

        public static void AsignTranslatedAppendix(this AppendixHelpEntity appendix, CultureInfoEntity fromCulture)
        {
            var fromAppendix = Database.Query<AppendixHelpEntity>().SingleEx(n => n.UniqueName == appendix.UniqueName && n.Culture == fromCulture);

            HashSet<string> toTranslate = new HashSet<string>();
            if (!appendix.Title.HasText() && fromAppendix.Title.HasText())
                toTranslate.Add(fromAppendix.Title);

            if (!appendix.Description.HasText() && fromAppendix.Description.HasText())
                toTranslate.Add(fromAppendix.Description);

            Dictionary<string, string> dic = Translate(toTranslate, fromAppendix.Culture.Name, appendix.Culture.Name);

            if (!appendix.Title.HasText() && fromAppendix.Title.HasText())
                appendix.Title = dic.GetOrThrow(fromAppendix.Title);

            if (!appendix.Description.HasText() && fromAppendix.Description.HasText())
                appendix.Description = dic.GetOrThrow(fromAppendix.Description);

            appendix.Execute(AppendixHelpOperation.Save);
        }
    }
}
