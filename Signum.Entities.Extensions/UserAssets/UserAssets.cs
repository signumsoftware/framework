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
        MList<UserAssetPreviewLine> lines = new MList<UserAssetPreviewLine>();
        public MList<UserAssetPreviewLine> Lines
        {
            get { return lines; }
            set { Set(ref lines, value); }
        }
    }

    [Serializable]
    public class UserAssetPreviewLine : EmbeddedEntity
    {
        Type type;
        public Type Type
        {
            get { return type; }
            set { Set(ref type, value); }
        }

        string text;
        public string Text
        {
            get { return text; }
            set { Set(ref text, value); }
        }

        EntityAction action;
        public EntityAction Action
        {
            get { return action; }
            set { Set(ref action, value); }
        }

        bool overrideEntity;
        public bool OverrideEntity
        {
            get { return overrideEntity; }
            set { Set(ref overrideEntity, value); }
        }

        Guid guid;
        public Guid Guid
        {
            get { return guid; }
            set { Set(ref guid, value); }
        }

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

    public static class UserAssetPermission
    {
        public static readonly PermissionSymbol UserAssetsToXML = new PermissionSymbol();
    }

    public interface IToXmlContext
    {
        Guid Include(IUserAssetEntity content);

        string TypeToName(Lite<TypeDN> type);

        string QueryToName(Lite<QueryDN> query);
    }

    public interface IFromXmlContext
    {
        QueryDN GetQuery(string queryKey);
        Lite<TypeDN> GetType(string cleanName);

        ChartScriptDN ChartScript(string chartScriptName);

        IUserAssetEntity GetEntity(Guid guid);

        IPartDN GetPart(IPartDN old, XElement element);

        DynamicQuery.QueryDescription GetQueryDescription(QueryDN Query);
    }

    public interface IUserAssetEntity : IIdentifiable
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
