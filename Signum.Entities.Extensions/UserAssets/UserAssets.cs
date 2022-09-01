using System.ComponentModel;
using System.Xml.Linq;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.Dashboard;
using Signum.Entities.Mailing;
using Signum.Entities.Word;
using Signum.Entities.Workflow;

namespace Signum.Entities.UserAssets;

public class UserAssetPreviewModel : ModelEntity
{
    public MList<UserAssetPreviewLineEmbedded> Lines { get; set; } = new MList<UserAssetPreviewLineEmbedded>();
}


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

    [PreserveOrder, NoRepeatValidator]
    public MList<LiteConflictEmbedded> LiteConflicts { get; set; } = new MList<LiteConflictEmbedded>();

    public override string ToString() => $"{Type} {Action}";
}

public class LiteConflictEmbedded : EmbeddedEntity
{
    public string PropertyRoute { get; set; }

    [ImplementedByAll]
    public Lite<Entity> From { get; set; }

    [ImplementedByAll]
    public Lite<Entity>? To { get;  set; }

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
    [Description("Looks like some entities in {0} do not exist or have a different meanign in this database...")]
    LooksLikeSomeEntitiesIn0DoNotExistsOrHaveADifferentMeaningInThisDatabase,
    [Description("Same selection for all conflicts of {0}")]
    SameSelectionForAllConflictsOf0
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

    public XElement GetFullWorkflowElement(WorkflowEntity workflow);

    public T RetrieveLite<T>(Lite<T> lite) where T : class, IEntity;
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
    WordModelEntity GetWordModel(string fullClassName);
    CultureInfoEntity GetCultureInfoEntity(string cultureName);

    public void SetFullWorkflowElement(WorkflowEntity workflow, XElement element);

    Lite<Entity>? ParseLite(string liteKey, IUserAssetEntity userAsset, PropertyRoute route);

    Lite<T>? ParseLite<E, T>(string liteKey, E entity, Expression<Func<E, Lite<T>?>> property)
        where E : Entity, IUserAssetEntity
        where T : class, IEntity
    {
        return (Lite<T>?)ParseLite(liteKey, entity, PropertyRoute.Construct(property));
    }

    public T RetrieveLite<T>(Lite<T> lite) where T : class, IEntity;

    public T GetSymbol<T>(string value) where T : Symbol;
}


public interface IUserAssetEntity : IEntity
{
    Guid Guid { get; set; }

    XElement ToXml(IToXmlContext ctx);

    void FromXml(XElement element, IFromXmlContext ctx);
}

public interface IHasEntityType
{
    Lite<TypeEntity>? EntityType { get; }
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
