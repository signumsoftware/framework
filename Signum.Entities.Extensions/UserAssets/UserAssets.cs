using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.Dashboard;

namespace Signum.Entities.UserAssets
{
    [Serializable]
    public class UserAssetPreviewModel : ModelEntity
    {
        public MList<UserAssetPreviewLine> Lines { get; set; } = new MList<UserAssetPreviewLine>();
    }

    [Serializable]
    public class UserAssetPreviewLine : EmbeddedEntity
    {
        public Type Type { get; set; }

        public string Text { get; set; }

        public EntityAction Action { get; set; }

        public bool OverrideEntity { get; set; }

        public Guid Guid { get; set; }

        [HiddenProperty]
        public bool OverrideVisible
        {
            get { return Action == EntityAction.Different; }
        }
    }

    public enum EntityAction
    {
        Identical,
        Different,
        New,
    }

    public enum UserAssetMessage
    {
        ExportToXml,
        ImportUserAssets,
        ImportPreview,
        SelectTheEntitiesToOverride,
        SucessfullyImported,
    }

    [AutoInit]
    public static class UserAssetPermission
    {
        public static PermissionSymbol UserAssetsToXML;
    }

    public interface IToXmlContext
    {
        Guid Include(IUserAssetEntity content);

        string TypeToName(Lite<TypeEntity> type);

        string QueryToName(Lite<QueryEntity> query);
    }

    public interface IFromXmlContext
    {
        QueryEntity GetQuery(string queryKey);
        Lite<TypeEntity> GetType(string cleanName);

        ChartScriptEntity ChartScript(string chartScriptName);

        IUserAssetEntity GetEntity(Guid guid);

        IPartEntity GetPart(IPartEntity old, XElement element);

        DynamicQuery.QueryDescription GetQueryDescription(QueryEntity Query);
    }

    public interface IUserAssetEntity : IEntity
    {
        Guid Guid { get; set; }

        XElement ToXml(IToXmlContext ctx);

        void FromXml(XElement element, IFromXmlContext ctx);
    }

    public static class FromXmlExtensions
    {
        public static void Syncronize<T>(this MList<T> entities, List<XElement> xElements, Action<T, XElement> syncAction)
            where T : new()
        {
            for (int i = 0; i < xElements.Count; i++)
            {
                T entity;
                if (entities.Count == i)
                {
                    entity = new T();
                    entities.Add(entity);
                }
                else
                    entity = entities[i];

                syncAction(entity, xElements[i]);
            }

            if (entities.Count > xElements.Count)
            {
                entities.RemoveRange(entities.Count - 1, entities.Count - xElements.Count);
            }
        }
    }

}
