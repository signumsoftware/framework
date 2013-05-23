using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.ControlPanel;
using Signum.Entities.Reflection;
using Signum.Entities.UserQueries;
using Signum.Services;
using Signum.Utilities;
using Signum.Utilities.Reflection;

namespace Signum.Engine.UserQueries
{
    public static class UserAssetsExporter
    {
        class ToXmlContext : IToXmlContext
        {
            public Dictionary<Guid, XElement> elements = new Dictionary<Guid, XElement>();
            public Guid Include(IUserAssetEntity content)
            {
                elements.GetOrCreate(content.Guid, () => content.ToXml(this));

                return content.Guid;
            }

            public string TypeToName(Lite<TypeDN> type)
            {
                return TypeLogic.GetCleanName(TypeLogic.DnToType.GetOrThrow(type.Retrieve()));
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

            return new MemoryStream().Using(s => { doc.Save(s); return s.ToArray(); });
        }
    }

    public static class UserAssetsImporter
    {
        public static Dictionary<string, Type> ElementNames = new Dictionary<string, Type>();

        class PreviewContext :IFromXmlContext
        {
            public Dictionary<Guid, IUserAssetEntity> entities = new Dictionary<Guid, IUserAssetEntity>();
            public Dictionary<Guid, XElement> elements;
            public Dictionary<Guid, UserAssetPreview> previews = new Dictionary<Guid, UserAssetPreview>();

            public PreviewContext(XDocument doc)
            {
                elements = doc.Element("Entities").Elements().ToDictionary(a => Guid.Parse(a.Attribute("Guid").Value));
            }

            QueryDN IFromXmlContext.GetQuery(string queryName)
            {
                return QueryLogic.RetrieveOrGenerateQuery(queryName);
            }

            public IUserAssetEntity GetEntity(Guid guid)
            {
                return entities.GetOrCreate(guid, () =>
                {
                    var element = elements.GetOrThrow(guid);

                    Type type = ElementNames.GetOrThrow(element.Name.ToString());

                    var entity = giRetrieveOrCreate.GetInvoker(type)(guid);

                    entity.FromXml(element, this);

                    previews.Add(guid, new UserAssetPreview
                    {
                        Text = entity.ToString(),
                        Type = entity.GetType(),
                        Guid = guid,
                        Action = entity.IsNew ? EntityAction.New :
                                 GraphExplorer.FromRoot((IdentifiableEntity)entity).Any(a => a.Modified != ModifiedState.Clean) ? EntityAction.Different :
                                 EntityAction.Identical,
                    });

                    return entity;
                });
            }

            public Lite<TypeDN> NameToType(string cleanName)
            {
                return TypeLogic.TypeToDN.GetOrThrow(TypeLogic.GetType(cleanName)).ToLite();
            }
        }

        public static List<UserAssetPreview> Preview(byte[] doc)
        {
            XDocument document = new MemoryStream(doc).Using(XDocument.Load);

            PreviewContext ctx = new PreviewContext(document);

            foreach (var item in ctx.elements)
                ctx.GetEntity(item.Key);

            return ctx.previews.Values.ToList();
        }

        class ImporterContext : IFromXmlContext
        {
            Dictionary<Guid, bool> overrideEntity;
            Dictionary<Guid, IUserAssetEntity> entities = new Dictionary<Guid, IUserAssetEntity>();
            public Dictionary<Guid, XElement> elements;

            public ImporterContext(XDocument doc, Dictionary<Guid, bool> overrideEntity)
            {
                this.overrideEntity = overrideEntity;
                elements = doc.Element("Entities").Elements().ToDictionary(a => Guid.Parse(a.Attribute("Guid").Value));
            }

            QueryDN IFromXmlContext.GetQuery(string queryName)
            {
                return QueryLogic.RetrieveOrGenerateQuery(queryName);
            }

            public IUserAssetEntity GetEntity(Guid guid)
            {
                return entities.GetOrCreate(guid, () =>
                {
                    var element = elements.GetOrThrow(guid);

                    Type type = ElementNames.GetOrThrow(element.Name.ToString());

                    var entity = giRetrieveOrCreate.GetInvoker(type)(guid);

                    if (entity.IsNew || overrideEntity.ContainsKey(guid))
                    {
                        entity.FromXml(element, this);
                        entity.Save();
                    }

                    return entity;
                });
            }


            public Lite<TypeDN> NameToType(string cleanName)
            {
                return TypeLogic.TypeToDN.GetOrThrow(TypeLogic.GetType(cleanName)).ToLite();
            }
        }

        public static void Import(byte[] document, List<UserAssetPreview> overrideEntities)
        {
            var doc = new MemoryStream(document).Using(XDocument.Load); 

            ImporterContext importer = new ImporterContext(doc,
                overrideEntities
                .Where(a => a.Action == EntityAction.Different)
                .ToDictionary(a => a.Guid, a => a.Override));

            foreach (var item in importer.elements)
                importer.GetEntity(item.Key);
        }

        static readonly GenericInvoker<Func<Guid, IUserAssetEntity>> giRetrieveOrCreate = new GenericInvoker<Func<Guid, IUserAssetEntity>>(
            guid => RetrieveOrCreate<UserQueryDN>(guid));
        static T RetrieveOrCreate<T>(Guid guid) where T : IdentifiableEntity, IUserAssetEntity, new()
        {
            var result = Database.Query<T>().SingleOrDefaultEx(a => a.Guid == guid);

            if (result != null)
                return result;

            return new T { Guid = guid };
        }
    }
}
