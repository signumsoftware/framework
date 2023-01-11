using System.IO;
using System.Xml.Linq;
using Signum.Engine.Chart;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.Dashboard;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Entities.Mailing;
using Signum.Engine.Mailing;
using Signum.Entities.Workflow;
using Signum.Engine.Workflow;
using Signum.Entities.Word;
using Signum.Engine.Word;
using Signum.Utilities;

namespace Signum.Engine.UserAssets;

public static class UserAssetsExporter
{
    public static Func<XDocument, XDocument>? PreExport = null;

    class ToXmlContext : IToXmlContext
    {
        public Dictionary<Guid, XElement> elements = new();
        public Guid Include(IUserAssetEntity content)
        {
            elements.GetOrCreate(content.Guid, () => content.ToXml(this));

            return content.Guid;
        }

        public Guid Include(Lite<IUserAssetEntity> content)
        {
            return this.Include(content.Retrieve());
        }

        public XElement GetFullWorkflowElement(WorkflowEntity workflow)
        {
            var wie = new WorkflowImportExport(workflow);
            return wie.ToXml(this);
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


       if(PreExport!=null)
            doc=PreExport(doc);

        return new MemoryStream().Using(s => { doc.Save(s); return s.ToArray(); });
    }
}

public static class UserAssetsImporter
{
    public static Dictionary<string, Type> UserAssetNames = new();
    public static Polymorphic<Action<Entity>> SaveEntity = new();
    public static Dictionary<string, Type> PartNames = new();
    public static Func<XDocument, XDocument>? PreImport = null;

    class PreviewContext : IFromXmlContext
    {
        public Dictionary<Guid, IUserAssetEntity> entities = new();
        public Dictionary<Guid, XElement> elements;

        public Dictionary<Guid, ModelEntity?> customResolutionModel = new ();
        public Dictionary<Guid, Dictionary<(Lite<Entity> from, PropertyRoute route), Lite<Entity>?>> liteConflicts = new();

        public Dictionary<Guid, UserAssetPreviewLineEmbedded> previews = new();

        public bool IsPreview => true;

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

                var action = entity.IsNew ? EntityAction.New :
                             customResolutionModel.ContainsKey(entity.Guid) ? EntityAction.Different :
                             GraphExplorer.FromRootVirtual((Entity)entity).Any(a => a.Modified != ModifiedState.Clean) ? EntityAction.Different :
                             EntityAction.Identical;

                previews.Add(guid, new UserAssetPreviewLineEmbedded
                {
                    Text = entity.ToString()!,
                    Type = entity.GetType().ToTypeEntity(),
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

        public IPartEntity GetPart(IPartEntity old, XElement element)
        {
            Type type = PartNames.GetOrThrow(element.Name.ToString());

            var part = old != null && old.GetType() == type ? old : (IPartEntity)Activator.CreateInstance(type)!;

            part.FromXml(element, this);

            return part;
        }

        public TypeEntity GetType(string cleanName)
        {
            return TypeLogic.GetType(cleanName).ToTypeEntity();
        }

        public Lite<TypeEntity> GetTypeLite(string cleanName)
        {
            return TypeLogic.GetType(cleanName).ToTypeEntity().ToLite();
        }

        public ChartScriptSymbol ChartScript(string chartScriptName)
        {
            return ChartScriptLogic.Scripts.Keys.SingleEx(cs => cs.Key == chartScriptName || cs.Key.After(".") == chartScriptName);
        }

        public QueryDescription GetQueryDescription(QueryEntity Query)
        {
            return QueryLogic.Queries.QueryDescription(QueryLogic.QueryNames.GetOrThrow(Query.Key));
        }

        public PermissionSymbol? TryPermission(string permissionKey)
        {
            return SymbolLogic<PermissionSymbol>.TryToSymbol(permissionKey);
        }

        public EmailModelEntity GetEmailModel(string fullClassName)
        {
            return EmailModelLogic.GetEmailModelEntity(fullClassName);
        }


        public WordModelEntity GetWordModel(string fullClassName)
        {
            return WordModelLogic.GetWordModelEntity(fullClassName);
        }

        public CultureInfoEntity GetCultureInfoEntity(string cultureName)
        {
            return CultureInfoLogic.GetCultureInfoEntity(cultureName);
        }

        public void SetFullWorkflowElement(WorkflowEntity workflow, XElement element)
        {
            var wie = new WorkflowImportExport(workflow);
            wie.FromXml(element, this);

            if (wie.HasChanges)
            {
                if (wie.ReplacementModel != null)
                {
                    wie.ReplacementModel.NewTasks = wie.Activities.Select(a => new NewTasksEmbedded
                    {
                        BpmnId = a.BpmnElementId,
                        Name = a.GetName()!,
                        SubWorkflow = (a as WorkflowActivityEntity)?.SubWorkflow?.Workflow.ToLite(),
                    }).ToMList();
                }
                this.customResolutionModel.Add(Guid.Parse(element.Attribute("Guid")!.Value), wie.ReplacementModel);
            }
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
        public List<IPartEntity> toRemove = new();
        public Dictionary<Guid, XElement> elements;

        public bool IsPreview => false;

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

                    SaveEntity.Invoke((Entity)entity);
                }

                return entity;
            });
        }

        public Lite<TypeEntity> NameToType(string cleanName)
        {
            return TypeLogic.TypeToEntity.GetOrThrow(TypeLogic.GetType(cleanName)).ToLite();
        }

        public EmailModelEntity GetEmailModel(string fullClassName)
        {
            return EmailModelLogic.GetEmailModelEntity(fullClassName);
        }
        public WordModelEntity GetWordModel(string fullClassName)
        {
            return WordModelLogic.GetWordModelEntity(fullClassName);
        }

        public IPartEntity GetPart(IPartEntity old, XElement element)
        {
            Type type = PartNames.GetOrThrow(element.Name.ToString());

            var part = old != null && old.GetType() == type ? old : (IPartEntity)Activator.CreateInstance(type)!;

            part.FromXml(element, this);


            return part;
        }

        public Lite<TypeEntity> GetTypeLite(string cleanName)
        {
            return TypeLogic.GetType(cleanName).ToTypeEntity().ToLite();
        }

        public ChartScriptSymbol ChartScript(string chartScriptName)
        {
            return ChartScriptLogic.Scripts.Keys.SingleEx(cs => cs.Key == chartScriptName || cs.Key.After(".") == chartScriptName);
        }

        public QueryDescription GetQueryDescription(QueryEntity Query)
        {
            return QueryLogic.Queries.QueryDescription(QueryLogic.QueryNames.GetOrThrow(Query.Key));
        }

        public PermissionSymbol? TryPermission(string permissionKey)
        {
            return SymbolLogic<PermissionSymbol>.TryToSymbol(permissionKey);
        }

        public CultureInfoEntity GetCultureInfoEntity(string cultureName)
        {
            return CultureInfoLogic.GetCultureInfoEntity(cultureName);
        }

        public void SetFullWorkflowElement(WorkflowEntity workflow, XElement element)
        {
            var model = (WorkflowReplacementModel?)this.customResolutionModel.TryGetCN(Guid.Parse(element.Attribute("Guid")!.Value));
            var wie = new WorkflowImportExport(workflow)
            {
                ReplacementModel = model
            };
            wie.FromXml(element, this);
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
        guid => RetrieveOrCreate<UserQueryEntity>(guid));
    static T RetrieveOrCreate<T>(Guid guid) where T : Entity, IUserAssetEntity, new()
    {
        var result = Database.Query<T>().SingleOrDefaultEx(a => a.Guid == guid);

        if (result != null)
            return result;

        return new T { Guid = guid };
    }


    public static void Register<T>(string userAssetName, ExecuteSymbol<T> saveOperation) where T : Entity, IUserAssetEntity =>
        Register<T>(userAssetName, e => e.Execute(saveOperation));

    public static void Register<T>(string userAssetName, Action<T> saveEntity) where T : Entity, IUserAssetEntity
    {
        PermissionAuthLogic.RegisterPermissions(UserAssetPermission.UserAssetsToXML);
        UserAssetNames.Add(userAssetName, typeof(T));
        UserAssetsImporter.SaveEntity.Register(saveEntity);
    }
}
