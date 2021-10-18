using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Bibliography;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.Dashboard;
using Signum.Entities.Mailing;
using Signum.Entities.Workflow;

namespace Signum.Entities.UserAssets
{
    [Serializable]
    public class UserAssetPreviewModel : ModelEntity
    {
        public MList<UserAssetPreviewLineEmbedded> Lines { get; set; } = new MList<UserAssetPreviewLineEmbedded>();
    }

    [Serializable]
    public class UserAssetPreviewLineEmbedded : EmbeddedEntity
    {
        public TypeEntity? Type { get; set; }

        public string Text { get; set; }

        public EntityAction Action { get; set; }

        public bool OverrideEntity { get; set; }

        public Guid Guid { get; set; }

        public ModelEntity? CustomResolution { get; set; }

        [HiddenProperty]
        public bool OverrideVisible
        {
            get { return Action == EntityAction.Different; }
        }

        public override string ToString() => $"{Type} {Action}";
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
        SelectTheXmlFileWithTheUserAssetsThatYouWantToImport,
        SelectTheEntitiesToOverride,
        SucessfullyImported,
        SwitchToValue,
        SwitchToExpression,
    }

    [AutoInit]
    public static class UserAssetPermission
    {
        public static PermissionSymbol UserAssetsToXML;
    }

    public interface IToXmlContext
    {
        Guid Include(IUserAssetEntity content);
        Guid Include(Lite<IUserAssetEntity> content);

        string TypeToName(Lite<TypeEntity> type);
        string TypeToName(TypeEntity type);

        string QueryToName(Lite<QueryEntity> query);
        string PermissionToName(Lite<PermissionSymbol> symbol);

        public XElement GetFullWorkflowElement(WorkflowEntity workflow);
    }

    public interface IFromXmlContext
    {
        public bool IsPreview { get; }

        QueryEntity? TryGetQuery(string queryKey);
        QueryEntity GetQuery(string queryKey);

        PermissionSymbol? TryPermission(string permissionKey);

        Lite<TypeEntity> GetTypeLite(string cleanName);

        TypeEntity GetType(string cleanName);

        ChartScriptSymbol ChartScript(string chartScriptName);

        IUserAssetEntity GetEntity(Guid guid);

        IPartEntity GetPart(IPartEntity old, XElement element);

        DynamicQuery.QueryDescription GetQueryDescription(QueryEntity Query);

        EmailModelEntity GetEmailModel(string fullClassName);
        CultureInfoEntity GetCultureInfoEntity(string cultureName);

        public void SetFullWorkflowElement(WorkflowEntity workflow, XElement element);
    }

    public interface IUserAssetEntity : IEntity
    {
        Guid Guid { get; set; }

        XElement ToXml(IToXmlContext ctx);

        void FromXml(XElement element, IFromXmlContext ctx);
    }

    public static class FromXmlExtensions
    {
        public static void Synchronize<T>(this MList<T> entities, List<XElement>? xElements, Action<T, XElement> syncAction)
            where T : new()
        {
            if (xElements == null)
                xElements = new List<XElement>();

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
                entities.RemoveRange(xElements.Count, entities.Count - xElements.Count);
            }
        }

        public static void Synchronize<T>(this MList<T> oldElements, IEnumerable<T> newElements)
        {
            if (Enumerable.SequenceEqual(oldElements, newElements))
                return;

            oldElements.Clear();
            oldElements.AddRange(newElements);
        }

        public static T? CreateOrAssignEmbedded<T>(this T? embedded, XElement? element, Action<T, XElement> syncAction)
          where T : EmbeddedEntity, new()
        {
            if (element == null)
                return null;

            embedded ??= new T();
            syncAction(embedded, element);
            return embedded;
        }
    }
}
