using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Files;
using System.ComponentModel;
using Signum.Entities.Templating;
using Signum.Entities.UserQueries;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserAssets;
using System.Xml.Linq;

namespace Signum.Entities.Word;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class WordTemplateEntity : Entity, IUserAssetEntity
{
    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 200)]
    public string Name { get; set; }

    public QueryEntity Query { get; set; }

    public WordModelEntity? Model { get; set; }

    public CultureInfoEntity Culture { get; set; }

    public bool GroupResults { get; set; }

    [PreserveOrder]
    public MList<QueryFilterEmbedded> Filters { get; set; } = new MList<QueryFilterEmbedded>();

    [PreserveOrder]
    public MList<QueryOrderEmbedded> Orders { get; set; } = new MList<QueryOrderEmbedded>();

    [BindParent]
    public TemplateApplicableEval? Applicable { get; set; }

    public bool DisableAuthorization { get; set; }

    public Lite<FileEntity> Template { get; set; }

    [StringLengthValidator(Min = 3, Max = 200), FileNameValidator]
    public string FileName { get; set; }

    public WordTransformerSymbol? WordTransformer { get; set; }

    public WordConverterSymbol? WordConverter { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    public bool IsApplicable(Entity? entity)
    {
        if (Applicable == null)
            return true;

        try
        {
            return Applicable.Algorithm!.ApplicableUntyped(entity);
        }
        catch (Exception e)
        {
            throw new ApplicationException($"Error evaluating Applicable for WordTemplate '{Name}' with entity '{entity}': " + e.Message, e);
        }
    }

    public void ParseData(QueryDescription description)
    {
        var canAggregate = this.GroupResults ? SubTokensOptions.CanAggregate : 0;

        foreach (var f in Filters)
            f.ParseData(this, description, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate);

        foreach (var o in Orders)
            o.ParseData(this, description, SubTokensOptions.CanElement | canAggregate);
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("WordTemplate",
            new XAttribute("Name", Name),
            new XAttribute("Guid", Guid),
            new XAttribute("DisableAuthorization", DisableAuthorization),
            new XAttribute("Query", Query.Key),
            Model?.Let(m => new XAttribute("Model", m.FullClassName)),
            new XAttribute("Culture", Culture.Name),
            new XAttribute("FileName", FileName),
            WordTransformer?.Let(wt => new XAttribute("WordTransformer", wt.Key)),
            WordConverter?.Let(wc => new XAttribute("WordConverter", wc.Key)),
            new XAttribute("GroupResults", GroupResults),
            Filters.IsNullOrEmpty() ? null! : new XElement("Filters", Filters.Select(f => f.ToXml(ctx)).ToList()),
            Orders.IsNullOrEmpty() ? null! : new XElement("Orders", Orders.Select(o => o.ToXml(ctx)).ToList()),
            Applicable?.Let(app => new XElement("Applicable", new XCData(app.Script))),
            ctx.RetrieveLite(Template).Let(t => new XElement("Template",
                new XAttribute("FileName", t.FileName),
                new XCData(Convert.ToBase64String(t.BinaryFile))
            ))
        );
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Guid = Guid.Parse(element.Attribute("Guid")!.Value);
        Name = element.Attribute("Name")!.Value;
        DisableAuthorization = element.Attribute("DisableAuthorization")?.Let(a => bool.Parse(a.Value)) ?? false;

        Query = ctx.GetQuery(element.Attribute("Query")!.Value);
        Model = element.Attribute("Model")?.Let(at => ctx.GetWordModel(at.Value));
        Culture = ctx.GetCultureInfoEntity(element.Attribute("Culture")!.Value);

        FileName = element.Attribute("FileName")!.Value;

        WordTransformer = element.Attribute("WordTransformer")?.Let(at => ctx.GetSymbol<WordTransformerSymbol>(at.Value));
        WordConverter = element.Attribute("WordConverter")?.Let(at => ctx.GetSymbol<WordConverterSymbol>(at.Value));

        GroupResults = bool.Parse(element.Attribute("GroupResults")!.Value);
        Filters.Synchronize(element.Element("Filters")?.Elements().ToList(), (f, x) => f.FromXml(x, ctx));
        Orders.Synchronize(element.Element("Orders")?.Elements().ToList(), (o, x) => o.FromXml(x, ctx));

        Applicable = element.Element("Applicable")?.Let(app => new TemplateApplicableEval { Script = app.Value });
        ParseData(ctx.GetQueryDescription(Query));

        Template = SyncFile(Template == null ? null : ctx.RetrieveLite(Template), element.Element("Template")!);
    }

    private Lite<FileEntity> SyncFile(FileEntity? fileEntity, XElement xElement)
    {
        var fileName = xElement.Attribute("FileName")!.Value!;
        var bytes = Convert.FromBase64String(xElement.Value);

        if (fileEntity != null && fileName == fileEntity.FileName &&
            MemoryExtensions.SequenceEqual<byte>(bytes, fileEntity.BinaryFile))
            return fileEntity.ToLite();

        return new FileEntity
        {
            FileName = fileName,
            BinaryFile = bytes,
        }.ToLiteFat();
    }
}


[AutoInit]
public static class WordTemplateOperation
{
    public static ExecuteSymbol<WordTemplateEntity> Save;
    public static DeleteSymbol<WordTemplateEntity> Delete;
    public static ExecuteSymbol<WordTemplateEntity> CreateWordReport;

    public static ConstructSymbol<WordTemplateEntity>.From<WordModelEntity> CreateWordTemplateFromWordModel;
}

public enum WordTemplateMessage
{
    [Description("Model should be set to use model {0}")]
    ModelShouldBeSetToUseModel0,
    [Description("Type {0} does not have a property with name {1}")]
    Type0DoesNotHaveAPropertyWithName1,
    ChooseAReportTemplate,
    [Description("{0} {1} requires extra parameters")]
    _01RequiresExtraParameters,
    [Description("Select the source of data for your table or chart")]
    SelectTheSourceOfDataForYourTableOrChart,
    [Description("Write this key as Title in the 'Alternative text' of your table or chart")]
    WriteThisKeyAsTileInTheAlternativeTextOfYourTableOrChart,
    NoDefaultTemplateDefined,
    WordReport,
}

[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class WordTransformerSymbol : Symbol
{
    private WordTransformerSymbol() { }

    public WordTransformerSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}

[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class WordConverterSymbol : Symbol
{
    private WordConverterSymbol() { }

    public WordConverterSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}

[AutoInit]
public static class WordTemplatePermission
{
    public static PermissionSymbol GenerateReport;
}

[InTypeScript(true)]
public enum WordTemplateVisibleOn
{
    Single = 1,
    Multiple = 2,
    Query = 4
}


