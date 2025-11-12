using Signum.Authorization;
using Signum.Utilities.Reflection;
using System.IO;
using System.Xml.Linq;

namespace Signum.UserAssets;

public static class UserAssetsExporter
{
    public static void ToXmlMixin(IUserAssetEntity entity, XElement element, IToXmlContext ctx)
    {
        foreach (var m in ((Entity)entity).Mixins.OfType<IUserAssetMixin>())
        {
            m.ToXml(element, ctx);
        }
    }

    public static Func<XDocument, XDocument>? PreExport = null;

    class ToXmlContext : IToXmlContext
    {
        public Dictionary<Guid, XElement> elements = new();
        public Guid Include(IUserAssetEntity content)
        {
            elements.GetOrCreate(content.Guid, () =>
            {
                var element = content.ToXml(this);
                ToXmlMixin(content, element, this);
                return element;
            });

            return content.Guid;
        }

        public Guid Include(Lite<IUserAssetEntity> content)
        {
            return this.Include(content.Retrieve());
        }

        T IToXmlContext.RetrieveLite<T>(Lite<T> lite)
        {
            return lite.Retrieve();
        }
    }

    public static byte[] ToXml(params IUserAssetEntity[] entities)
    {
        ToXmlContext ctx = new();

        foreach (var e in entities)
            ctx.Include(e);

        XDocument doc = new(
            new XDeclaration("1.0", "UTF8", "yes"),
            new XElement("Entities", 
                ctx.elements.Values));


        if (PreExport != null)
            doc = PreExport(doc);

        return new MemoryStream().Using(s => { doc.Save(s); return s.ToArray(); });
    }


}

public static class UserAssetsImporter
{
    static void FromXmlMixin(IUserAssetEntity entity, XElement element, IFromXmlContext ctx)
    {
        foreach (var m in ((Entity)entity).Mixins.OfType<IUserAssetMixin>())
        {
            m.FromXml(element, ctx);
        }
    }


    public static Dictionary<string, Type> UserAssetNames = new();
    public static Polymorphic<Action<Entity>> SaveEntity = new();
    public static Func<XDocument, XDocument>? PreImport = null;

    class PreviewContext : IFromXmlContext
    {
        public Dictionary<Guid, IUserAssetEntity> entities = new();
        public Dictionary<Guid, XElement> elements;

        public Dictionary<Guid, ModelEntity?> customResolutionModel = new();
        public Dictionary<Guid, Dictionary<(Lite<Entity> from, PropertyRoute route), Lite<Entity>?>> liteConflicts = new();

        public Dictionary<Guid, UserAssetPreviewLineEmbedded> previews = new();

        public bool IsPreview => true;

        public Dictionary<Guid, ModelEntity?> CustomResolutionModel => customResolutionModel;

        public PreviewContext(XDocument doc)
        {
            elements = doc.Element("Entities")!.Elements().ToDictionary(a => Guid.Parse(a.Attribute("Guid")!.Value));
        }

        public QueryEntity GetQuery(string queryKey)
        {
            var qn = QueryLogic.ToQueryName(queryKey);

            return QueryLogic.GetQueryEntity(qn);
        }

        QueryEntity? IFromXmlContext.TryGetQuery(string queryKey)
        {
            var qn = QueryLogic.TryToQueryName(queryKey);

            if (qn == null)
                return null;

            return QueryLogic.GetQueryEntity(qn);
        }

        public IUserAssetEntity GetEntity(Guid guid)
        {
            return entities.GetOrCreate(guid, () =>
            {
                var element = elements.GetOrThrow(guid);

                Type type = UserAssetNames.GetOrThrow(element.Name.ToString());

                var entity = giRetrieveOrCreate.GetInvoker(type)(guid);

                entity.FromXml(element, this);

                FromXmlMixin(entity, element, this);

                var action = entity.IsNew ? EntityAction.New :
                             customResolutionModel.ContainsKey(entity.Guid) ? EntityAction.Different :
                             GraphExplorer.FromRootVirtual((Entity)entity).Any(a => a.Modified != ModifiedState.Clean) ? EntityAction.Different :
                             EntityAction.Identical;

                previews.Add(guid, new UserAssetPreviewLineEmbedded
                {
                    Text = entity.ToString()!,
                    Type = entity.GetType().ToTypeEntity(),
                    EntityType = (entity as IHasEntityType)?.EntityType?.ToType().ToTypeEntity(),
                    Guid = guid,
                    Action = action,
                    OverrideEntity = action == EntityAction.Different,

                    LiteConflicts = liteConflicts.TryGetC(guid).EmptyIfNull().Select(kvp => new LiteConflictEmbedded
                    {
                        PropertyRoute = kvp.Key.route.ToString(),
                        From = kvp.Key.from,
                        To = kvp.Value,
                    }).ToMList(),

                    CustomResolution = customResolutionModel.TryGetCN(entity.Guid),
                });

                return entity;
            });
        }

        public Lite<TypeEntity> NameToType(string cleanName)
        {
            return TypeLogic.TypeToEntity.GetOrThrow(TypeLogic.GetType(cleanName)).ToLite();
        }

        public TypeEntity GetType(string cleanName)
        {
            return TypeLogic.GetType(cleanName).ToTypeEntity();
        }

        public Lite<TypeEntity> GetTypeLite(string cleanName)
        {
            return TypeLogic.GetType(cleanName).ToTypeEntity().ToLite();
        }

        public QueryDescription GetQueryDescription(QueryEntity Query)
        {
            return QueryLogic.Queries.QueryDescription(QueryLogic.QueryNames.GetOrThrow(Query.Key));
        }

        public Lite<Entity>? ParseLite(string liteKey, IUserAssetEntity userAsset, PropertyRoute route)
        {
            var lite = Lite.Parse(liteKey);

            var newLite =
                lite.EntityType == typeof(RoleEntity) && lite.ToString() is string str ? AuthLogic.TryGetRole(str) :
                Database.TryRetrieveLite(lite.EntityType, lite.Id);

            if (newLite == null || lite.ToString() != newLite.ToString() || lite.Id != newLite.Id)
            {
                this.liteConflicts.GetOrCreate(userAsset.Guid)[(lite, route)] = newLite;
            }

            return lite;
        }


        T IFromXmlContext.RetrieveLite<T>(Lite<T> lite)
        {
            return lite.Retrieve();
        }

        public T GetSymbol<T>(string value)
               where T : Symbol
        {
            return SymbolLogic<T>.ToSymbol(value);
        }
    }

    public static UserAssetPreviewModel Preview(byte[] doc)
    {
        XDocument document = new MemoryStream(doc).Using(XDocument.Load);
        if (PreImport != null)
            document = PreImport(document);

        PreviewContext ctx = new(document);

        foreach (var item in ctx.elements)
            ctx.GetEntity(item.Key);

        return new UserAssetPreviewModel { Lines = ctx.previews.Values.ToMList() };
    }

    class ImporterContext : IFromXmlContext
    {
        Dictionary<Guid, bool> overrideEntity;
        Dictionary<Guid, IUserAssetEntity> entities = new();
        Dictionary<Guid, ModelEntity?> customResolutionModel = new();
        Dictionary<Guid, Dictionary<(Lite<Entity> from, PropertyRoute route), Lite<Entity>?>> liteConflicts = new();
        public List<Entity> toRemove = new();
        public Dictionary<Guid, XElement> elements;

        public bool IsPreview => false;

        public Dictionary<Guid, ModelEntity?> CustomResolutionModel => customResolutionModel;

        public ImporterContext(XDocument doc, Dictionary<Guid, bool> overrideEntity, Dictionary<Guid, ModelEntity?> customResolution, Dictionary<Guid, Dictionary<(Lite<Entity> from, PropertyRoute route), Lite<Entity>?>> liteConflicts)
        {
            this.overrideEntity = overrideEntity;
            this.customResolutionModel = customResolution;
            this.liteConflicts = liteConflicts;
            elements = doc.Element("Entities")!.Elements().ToDictionary(a => Guid.Parse(a.Attribute("Guid")!.Value));
        }

        public TypeEntity GetType(string cleanName)
        {
            return TypeLogic.GetType(cleanName).ToTypeEntity();
        }


        QueryEntity IFromXmlContext.GetQuery(string queryKey)
        {
            var qn = QueryLogic.ToQueryName(queryKey);

            return QueryLogic.GetQueryEntity(qn);
        }

        QueryEntity? IFromXmlContext.TryGetQuery(string queryKey)
        {
            var qn = QueryLogic.TryToQueryName(queryKey);

            if (qn == null)
                return null;

            return QueryLogic.GetQueryEntity(qn);
        }

        public IUserAssetEntity GetEntity(Guid guid)
        {
            return entities.GetOrCreate(guid, () =>
            {
                var element = elements.GetOrThrow(guid);

                Type type = UserAssetNames.GetOrThrow(element.Name.ToString());

                var entity = giRetrieveOrCreate.GetInvoker(type)(guid);

                if (entity.IsNew || overrideEntity.TryGet(guid, false))
                {
                    entity.FromXml(element, this);

                    FromXmlMixin(entity, element, this);

                    SaveEntity.Invoke((Entity)entity);
                }

                return entity;
            });
        }

        public Lite<TypeEntity> NameToType(string cleanName)
        {
            return TypeLogic.TypeToEntity.GetOrThrow(TypeLogic.GetType(cleanName)).ToLite();
        }

        public Lite<TypeEntity> GetTypeLite(string cleanName)
        {
            return TypeLogic.GetType(cleanName).ToTypeEntity().ToLite();
        }

        public QueryDescription GetQueryDescription(QueryEntity Query)
        {
            return QueryLogic.Queries.QueryDescription(QueryLogic.QueryNames.GetOrThrow(Query.Key));
        }

        public Lite<Entity>? ParseLite(string liteKey, IUserAssetEntity userAsset, PropertyRoute route)
        {
            var lite = Lite.Parse(liteKey);

            if (this.liteConflicts.TryGetValue(userAsset.Guid, out var dic) && dic.TryGetValue((lite, route), out var alternative))
                return alternative;

            return lite;
        }

        T IFromXmlContext.RetrieveLite<T>(Lite<T> lite)
        {
            return lite.Retrieve();
        }

        public T GetSymbol<T>(string value) where T : Symbol
        {
            throw new NotImplementedException();
        }
    }

    public static void ImportConsole(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);

        var preview = Preview(bytes);
        foreach (var item in preview.Lines)
        {
            switch (item.Action)
            {
                case EntityAction.New: SafeConsole.WriteLineColor(ConsoleColor.Green, $"Create {item.Type} {item.Guid} {item.Text}"); break;
                case EntityAction.Identical: SafeConsole.WriteLineColor(ConsoleColor.DarkGray, $"Identical {item.Type} {item.Guid} {item.Text}"); break;
                case EntityAction.Different: SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"Override {item.Type} {item.Guid} {item.Text}"); break;
            }
        }

        if (!preview.Lines.Any(a => a.OverrideEntity) || SafeConsole.Ask("Override all?"))
        {
            Import(bytes, preview);
            SafeConsole.WriteLineColor(ConsoleColor.Green, $"Imported Succesfully");
        }
    }

    public static void ImportAll(string filePath) => ImportAll(File.ReadAllBytes(filePath));

    public static void ImportAll(byte[] document)
    {
        Import(document, Preview(document));
    }

    public static void Import(byte[] document, UserAssetPreviewModel preview)
    {
            using (var tr = new Transaction())
            {
                var doc = new MemoryStream(document).Using(XDocument.Load);
                if (PreImport != null)
                    doc = PreImport(doc);

                ImporterContext importer = new(doc,
                    overrideEntity: preview.Lines
                    .Where(a => a.Action == EntityAction.Different)
                    .ToDictionary(a => a.Guid, a => a.OverrideEntity),
                    customResolution: preview.Lines
                    .Where(a => a.Action == EntityAction.Different)
                    .ToDictionary(a => a.Guid, a => a.CustomResolution),
                    liteConflicts: preview.Lines
                    .ToDictionary(a => a.Guid, a => a.LiteConflicts.ToDictionary(l => (l.From, PropertyRoute.Parse(l.PropertyRoute)), l => l.To))
                    );

                foreach (var item in importer.elements)
                    importer.GetEntity(item.Key);

                Database.DeleteList(importer.toRemove);

                tr.Commit();
            }

    }

    static readonly GenericInvoker<Func<Guid, IUserAssetEntity>> giRetrieveOrCreate = new(
        guid => RetrieveOrCreate<FakeEntity>(guid));
    static T RetrieveOrCreate<T>(Guid guid) where T : Entity, IUserAssetEntity, new()
    {
        var result = Database.Query<T>().SingleOrDefaultEx(a => a.Guid == guid);

        if (result != null)
            return result;

        return new T { Guid = guid };
    }

    public class FakeEntity : Entity, IUserAssetEntity
    {
        public Guid Guid { get; set; }

        public void FromXml(XElement element, IFromXmlContext ctx) => throw new NotImplementedException();
        public XElement ToXml(IToXmlContext ctx) => throw new NotImplementedException();
    }

    public static void Register<T>(string userAssetName, ExecuteSymbol<T> saveOperation) where T : Entity, IUserAssetEntity =>
        Register<T>(userAssetName, e => e.Execute(saveOperation));

    public static void Register<T>(string userAssetName, Action<T> saveEntity) where T : Entity, IUserAssetEntity
    {
        PermissionLogic.RegisterPermissions(UserAssetPermission.UserAssetsToXML);
        UserAssetNames.Add(userAssetName, typeof(T));
        UserAssetsImporter.SaveEntity.Register(saveEntity);
    }

    public static void SolveAllConflicts(UserAssetPreviewModel preview)
    {
        foreach (var item in preview.Lines)
        {
            foreach (var conf in item.LiteConflicts)
            {
                conf.To = giWithSameToString.GetInvoker(conf.From.EntityType)(conf.From);
            }
        }
    }

    static GenericInvoker<Func<Lite<Entity>, Lite<Entity>?>> giWithSameToString = 
        new GenericInvoker<Func<Lite<Entity>, Lite<Entity>?>>(old => WithSameToString<UserEntity>((Lite<UserEntity>)old));

    public static Lite<T>? WithSameToString<T>(Lite<T> old)
        where T : Entity
    {
        return Database.Query<T>().Where(a => a.ToString() == old.ToString()).Select(a => a.ToLite()).SingleOrDefault();
    }
}
