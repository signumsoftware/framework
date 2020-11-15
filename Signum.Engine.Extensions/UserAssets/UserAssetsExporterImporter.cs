using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Signum.Engine.Basics;
using Signum.Engine.Chart;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.Dashboard;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Entities.Mailing;
using Signum.Engine.Mailing;
using Signum.Entities.Workflow;
using Signum.Engine.Workflow;
using Signum.Engine.Operations.Internal;

namespace Signum.Engine.UserAssets
{
    public static class UserAssetsExporter
    {
        public static Func<XDocument, XDocument>? PreExport = null;

        class ToXmlContext : IToXmlContext
        {
            public Dictionary<Guid, XElement> elements = new Dictionary<Guid, XElement>();
            public Guid Include(IUserAssetEntity content)
            {
                elements.GetOrCreate(content.Guid, () => content.ToXml(this));

                return content.Guid;
            }

            public Guid Include(Lite<IUserAssetEntity> content)
            {
                return this.Include(content.RetrieveAndRemember());
            }

            public string TypeToName(Lite<TypeEntity> type)
            {
                return TypeLogic.GetCleanName(TypeLogic.EntityToType.GetOrThrow(type.RetrieveAndRemember()));
            }
            
            public string TypeToName(TypeEntity type)
            {
                return TypeLogic.GetCleanName(TypeLogic.EntityToType.GetOrThrow(type));
            }

            public string QueryToName(Lite<QueryEntity> query)
            {
                return query.RetrieveAndRemember().Key;
            }

            public string PermissionToName(Lite<PermissionSymbol> symbol)
            {
                return symbol.RetrieveAndRemember().Key;
            }

            public XElement GetFullWorkflowElement(WorkflowEntity workflow)
            {
                var wie = new WorkflowImportExport(workflow);
                return wie.ToXml(this);
            }
        }

        public static byte[] ToXml(params IUserAssetEntity[] entities)
        {
            ToXmlContext ctx = new ToXmlContext();

            foreach (var e in entities)
                ctx.Include(e);

            XDocument doc = new XDocument(
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
        public static Dictionary<string, Type> UserAssetNames = new Dictionary<string, Type>();
        public static Polymorphic<Action<Entity>> SaveEntity = new Polymorphic<Action<Entity>>();
        public static Dictionary<string, Type> PartNames = new Dictionary<string, Type>();
        public static Func<XDocument, XDocument>? PreImport = null;

        class PreviewContext : IFromXmlContext
        {
            public Dictionary<Guid, IUserAssetEntity> entities = new Dictionary<Guid, IUserAssetEntity>();
            public Dictionary<Guid, XElement> elements;

            public Dictionary<Guid, ModelEntity?> customResolutionModel = new Dictionary<Guid, ModelEntity?>();

            public Dictionary<Guid, UserAssetPreviewLineEmbedded> previews = new Dictionary<Guid, UserAssetPreviewLineEmbedded>();

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

                    previews.Add(guid, new UserAssetPreviewLineEmbedded
                    {
                        Text = entity.ToString()!,
                        Type = entity.GetType().ToTypeEntity(),
                        Guid = guid,
                        Action = entity.IsNew ? EntityAction.New :
                                 customResolutionModel.ContainsKey(entity.Guid) ? EntityAction.Different :
                                 GraphExplorer.FromRoot((Entity)entity).Any(a => a.Modified != ModifiedState.Clean) ? EntityAction.Different :
                                 EntityAction.Identical,
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
        }

        public static UserAssetPreviewModel Preview(byte[] doc)
        {
            XDocument document = new MemoryStream(doc).Using(XDocument.Load);
            if (PreImport != null)
                document = PreImport(document);

            PreviewContext ctx = new PreviewContext(document);

            foreach (var item in ctx.elements)
                ctx.GetEntity(item.Key);

            return new UserAssetPreviewModel { Lines = ctx.previews.Values.ToMList() };
        }

        class ImporterContext : IFromXmlContext
        {
            Dictionary<Guid, bool> overrideEntity;
            Dictionary<Guid, IUserAssetEntity> entities = new Dictionary<Guid, IUserAssetEntity>();
            Dictionary<Guid, ModelEntity?> customResolutionModel = new Dictionary<Guid, ModelEntity?>();
            public List<IPartEntity> toRemove = new List<IPartEntity>();
            public Dictionary<Guid, XElement> elements;

            public bool IsPreview => false;

            public ImporterContext(XDocument doc, Dictionary<Guid, bool> overrideEntity, Dictionary<Guid, ModelEntity?> customResolution)
            {
                this.overrideEntity = overrideEntity;
                this.customResolutionModel = customResolution;
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

                    if (entity.IsNew || overrideEntity.ContainsKey(guid))
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

            public IPartEntity GetPart(IPartEntity old, XElement element)
            {
                Type type = PartNames.GetOrThrow(element.Name.ToString());

                var part = old != null && old.GetType() == type ? old : (IPartEntity)Activator.CreateInstance(type)!;

                part.FromXml(element, this);

                if (old != null && part != old)
                    toRemove.Add(old);

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
                    case EntityAction.Different: SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"Override {item.Type} {item.Guid} {item.Text}");
                        item.OverrideEntity = true;
                        break;
                }
            }

            if (!preview.Lines.Any(a => a.OverrideEntity) || SafeConsole.Ask("Override all?"))
            {
                Import(bytes, preview);
                SafeConsole.WriteLineColor(ConsoleColor.Green, $"Imported Succesfully");
            }
        }

        public static void ImportAll(byte[] document)
        {
            Import(document, Preview(document));
        }

        public static void Import(byte[] document, UserAssetPreviewModel preview)
        {
            using (Transaction tr = new Transaction())
            {
                var doc = new MemoryStream(document).Using(XDocument.Load);
                if (PreImport != null)
                    doc = PreImport(doc);

                ImporterContext importer = new ImporterContext(doc,
                    preview.Lines
                    .Where(a => a.Action == EntityAction.Different)
                    .ToDictionary(a => a.Guid, a => a.OverrideEntity),
                    preview.Lines
                    .Where(a => a.Action == EntityAction.Different)
                    .ToDictionary(a => a.Guid, a => a.CustomResolution));

                foreach (var item in importer.elements)
                    importer.GetEntity(item.Key);

                Database.DeleteList(importer.toRemove);

                tr.Commit();
            }

        }

        static readonly GenericInvoker<Func<Guid, IUserAssetEntity>> giRetrieveOrCreate = new GenericInvoker<Func<Guid, IUserAssetEntity>>(
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
}
