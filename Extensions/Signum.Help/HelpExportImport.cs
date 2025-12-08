using Signum.API;
using Signum.Authorization;
using Signum.Engine.Sync;
using Signum.Files;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Signum.Help;

public static class HelpExportImport
{
    public class HelpContent
    {
        public TypeEntity Type { get; set; }
        public string Key { get; set; }
        public CultureInfo Culture { get; set; }
        public FileContent Xml { get; set; }
        public FileContent[] Images { get; set; }
        public Lite<IHelpEntity>? ExistingEntity { get; set; }
        public ImportAction? Action { get; set; }
        public bool Apply { get; set; } = false;
        public ImportStatus Status { get; set; } = ImportStatus.NoChange;
        public string? ImportError { get; set; }

        public HelpImportPreviewLineEmbedded ToPreviewLine()
        {
            return new HelpImportPreviewLineEmbedded
            {
                Type = this.Type,
                Key = this.Key,
                Culture = this.Culture.ToCultureInfoEntity(),
                Text = this.ToString(),
                ExitingEntity = this.ExistingEntity,
                Action = this.Action!.Value,
                Apply = this.Action!.Value == ImportAction.Create,
            };
        }

        public HelpImportReportLineEmbedded ToReportLine()
        {
            return new HelpImportReportLineEmbedded
            {
                Type = this.Type,
                Key = this.Key,
                Culture = this.Culture.ToCultureInfoEntity(),
                Text = this.ToString(),
                ExitingEntity = this.ExistingEntity,
                Action = this.Action!.Value,
                Status = this.Status,
                ActionError = this.ImportError
            };
        }
    }

    public enum ImportExecutionMode
    {
        Preview,
        ApplyPreview,
        Interactive,
        //ApplyDirectly
    }

    private static FileContent[] GetImageContents(IHelpEntity entity) =>
        [.. Database.Query<HelpImageEntity>().Where(a => a.Target.Is(entity))
            .Select(i => new FileContent(i.Id + "." + i.File.FileName, i.File.GetByteArray()))];

    private static byte[] ToBytes(this XDocument doc)
    {
        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }

    private static string RemoveInvalid(string name)
    {
        return Regex.Replace(name, "[" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]", "");
    }


    private static void ImportCreate(HelpContent refContent, ImportExecutionMode mode, Func<ImportStatus> action)
    {
        ImportContent(refContent, mode, ImportAction.Create, () => action());
    }

    private static void ImportUpdate(HelpContent refContent, IHelpEntity entity, ImportExecutionMode mode, 
        Func<bool> modified, Func<ImportStatus> action)
    {
        ImportContent(refContent, mode, 
            modified() ? ImportAction.Override : ImportAction.NoChange, 
            () => action(), entity);
    }

    private static void ImportContent(
        HelpContent content,
        ImportExecutionMode mode,
        ImportAction importAction,
        Func<ImportStatus> executeAction,
        IHelpEntity? entity = null)
    {
        if (mode == ImportExecutionMode.Preview)
        {
            content.Action = importAction;
            content.ExistingEntity = entity?.ToLite();
            return;
        }
        // Interactive or ApplyReivew mode
        if (importAction == ImportAction.NoChange)
        {
            content.Status = ImportStatus.NoChange;
            return;
        }

        if (!content.Apply)
        {
            content.Status = ImportStatus.Skipped;
            SafeConsole.WriteLineColor(ConsoleColor.Yellow, $"{content} skipped");
            return;
        }

        Run();
        return;

        void Run()
        {
            try
            {
                content.Status = executeAction();
            }
            catch (Exception ex)
            {
                content.Status = ImportStatus.Failed;
                content.ImportError = ex.Message;
            }
        }
    }

    public static class AppendixXml
    {
        public static readonly XName _Appendix = "Appendix";
        static readonly XName _Name = "Name";
        static readonly XName _Culture = "Culture";
        static readonly XName _Title = "Title";
        static readonly XName _Description = "Description";

        public static XDocument ToXDocument(AppendixHelpEntity entity)
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                   new XElement(_Appendix,
                       new XAttribute(_Name, entity.UniqueName),
                       new XAttribute(_Culture, entity.Culture.Name),
                       new XAttribute(_Title, entity.Title),
                       entity.Description.HasText() ? new XElement(_Description, entity.Description) : null!
                   )
                );
          }

        public static void Import(List<HelpContent> contents, Replacements rep, ImportExecutionMode mode, ref bool? deleteAll)
        { 
            if (contents.Any(c => c.Type.ToType() != typeof(AppendixHelpEntity)))
                throw new InvalidOperationException("Some contents are not of type Appendix");

            SafeConsole.WriteLineColor(ConsoleColor.White, $" Appendix");
            var groups = contents.GroupToDictionary(c => c.Culture);
            
            foreach (var kv in groups)
            {
                var ci = kv.Key.ToCultureInfoEntity();
                var current = Database.Query<AppendixHelpEntity>().Where(a => a.Culture.Is(ci)).ToList();

                bool? deleteTemp = deleteAll;

                Synchronizer.SynchronizeReplacing(rep, mode == ImportExecutionMode.Interactive ? "Appendix" : null,
                    newDictionary: kv.Value.ToDictionaryEx(c => c.Key),
                    oldDictionary: current.ToDictionaryEx(n => n.UniqueName),
                    createNew: (k, n) =>
                        ImportCreate(n, mode, () =>
                        {
                            XElement element = ParseXML(n);
                            var ah = new AppendixHelpEntity
                            {
                                Culture = ci,
                                UniqueName = element.Attribute(_Name)!.Value,
                                Title = element.Attribute(_Title)!.Value,
                                Description = element.Element(_Description)?.Value
                            }.Execute(AppendixHelpOperation.Save);
                            SafeConsole.WriteColor(ConsoleColor.Green, "  " + ah.UniqueName);

                            ImportImages(ah, n);
                            Console.WriteLine();

                            return ImportStatus.Applied;
                        }),
                    removeOld: (k, o) =>
                        {
                            if (mode == ImportExecutionMode.Interactive && 
                                SafeConsole.Ask(ref deleteTemp, $"Delete Appendix {o.UniqueName} in {o.Culture}?"))
                            {
                                o.Delete(AppendixHelpOperation.Delete);
                                SafeConsole.WriteLineColor(ConsoleColor.Red, "  " + o.UniqueName);
                            }
                        },
                    merge: (k, n, o) =>
                        ImportUpdate(n, o, mode, modified: () =>
                        {
                            XElement element = ParseXML(n);

                            o.Title = element.Attribute(_Title)!.Value;
                            o.Description = element.Element(_Description)?.Value;

                            if (GraphExplorer.IsGraphModified(o))
                                return true;
                            else
                            {
                                SafeConsole.WriteColor(ConsoleColor.DarkGray, "  " + o.UniqueName);
                                return false;
                            }

                        }, action: () =>
                        {
                            o.Execute(AppendixHelpOperation.Save);
                            SafeConsole.WriteColor(ConsoleColor.Yellow, "  " + o.UniqueName);

                            ImportImages(o, n);
                            Console.WriteLine();

                            return ImportStatus.Applied;
                        })
                );
                deleteAll = deleteTemp;
            }
        }

        public static HelpContent ToHelpContent(AppendixHelpEntity entity, bool blobs)
        {
            var key = RemoveInvalid(entity.UniqueName);

            return new HelpContent
            {
                Type = entity.GetType().ToTypeEntity(),
                Key = key,
                Culture = entity.Culture.ToCultureInfo(),
                Xml = new FileContent($"{key}.help", blobs ? ToXDocument(entity).ToBytes() : []),
                Images = blobs ? GetImageContents(entity) : []
            }; 
        }

        private static XElement ParseXML(HelpContent content)
        {
            var ms = new MemoryStream(content.Xml.Bytes);
            XDocument doc = XDocument.Load(ms);
            XElement element = doc.Element(_Appendix)!;

            var uniqueName = element.Attribute(_Name)!.Value;
            if (uniqueName != content.Key)
                throw new InvalidOperationException($"UniqueName attribute ({uniqueName}) does not match with file name ({content.Key})");

            var culture = element.Attribute(_Culture)!.Value;
            if (culture != content.Culture.Name)
                throw new InvalidOperationException($"Culture attribute ({culture}) does not match with folder ({content.Culture.Name})");

            return element;
        }
    }

    public static class NamespaceXml
    {
        public static readonly XName _Namespace = "Namespace";
        static readonly XName _Culture = "Culture";
        static readonly XName _Name = "Name";
        static readonly XName _Title = "Title";
        static readonly XName _Description = "Description";

        public static XDocument ToXDocument(NamespaceHelpEntity entity)
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                   new XElement(_Namespace,
                       new XAttribute(_Name, entity.Name),
                       new XAttribute(_Culture, entity.Culture.Name),
                       entity.Title.HasText() ? new XAttribute(_Title, entity.Title) : null!,
                       entity.Description.HasText() ? new XElement(_Description, entity.Description) : null!
                   )
                );
        }

        public static HelpContent ToHelpContent(NamespaceHelpEntity entity, bool blobs)
        {
            var key = RemoveInvalid(entity.Name);

            return new HelpContent
            {
                Type = entity.GetType().ToTypeEntity(),
                Key = key,
                Culture = entity.Culture.ToCultureInfo(),
                Xml = new FileContent($"{key}.help", blobs ? ToXDocument(entity).ToBytes() : []),
                Images = blobs ? [] : GetImageContents(entity)
            };
        }

        internal static string GetNamespaceName(XDocument document, string fileName)
        {
            if (document.Root!.Name != _Namespace)
                throw new InvalidOperationException("{0} does not have a {1} root".FormatWith(fileName, _Namespace));

            var result = document.Root.Attribute(_Name)?.Value;

            if (!result.HasText())
                throw new InvalidOperationException("{0} does not have a {1} attribute".FormatWith(fileName, _Name));
            return result;
        }

        public static void Import(List<HelpContent> contents, Replacements rep, 
            ImportExecutionMode mode, ref bool? deleteAll)
        {
            if (contents.Any(c => c.Type.ToType() != typeof(NamespaceHelpEntity)))
                throw new InvalidOperationException("Some contents are not of type Namespace");

            SafeConsole.WriteLineColor(ConsoleColor.White, $" Namespace");

            var groups = contents.GroupToDictionary(c => c.Culture);

            foreach (var kv in groups)
            {
                var ci = kv.Key.ToCultureInfoEntity();
                var current = Database.Query<NamespaceHelpEntity>().Where(a => a.Culture.Is(ci)).ToList();

                bool? deleteTemp = deleteAll;

                Synchronizer.SynchronizeReplacing(rep, mode == ImportExecutionMode.Interactive ? "Namespace" : null,
                      newDictionary: kv.Value.ToDictionaryEx(c => c.Key),
                      oldDictionary: current.ToDictionaryEx(n => n.Name),
                      createNew: (k, n) => 
                      ImportCreate(n, mode, () =>
                          {
                              XElement element = ParseXML(n);

                              var nh = new NamespaceHelpEntity
                              {
                                  Culture = ci,
                                  Name = element.Attribute(_Name)!.Value,
                                  Title = element.Attribute(_Title)?.Value,
                                  Description = element.Element(_Description)?.Value
                              }.Execute(NamespaceHelpOperation.Save);
                              SafeConsole.WriteColor(ConsoleColor.Green, "  " + nh.Name);
                              
                              ImportImages(nh, n);                              
                              Console.WriteLine();

                              return ImportStatus.Applied;
                          }),
                      removeOld: (k, o) =>
                          {
                              if (mode == ImportExecutionMode.Interactive && 
                                SafeConsole.Ask(ref deleteTemp, $"Delete Namespace {o.Name} in {o.Culture}?"))
                              {
                                  o.Delete(NamespaceHelpOperation.Delete);
                                  SafeConsole.WriteLineColor(ConsoleColor.Red, "  " + o.Name);
                              }
                          },
                      merge: (k, n, o) =>
                          ImportUpdate(n, o, mode, modified: () =>
                          {
                              XElement element = ParseXML(n);

                              o.Title = element.Attribute(_Title)?.Value;
                              o.Description = element.Element(_Description)?.Value;

                              if (GraphExplorer.IsGraphModified(o))
                                  return true;
                              else
                              {
                                  SafeConsole.WriteColor(ConsoleColor.DarkGray, "  " + o.Name);
                                  return false;
                              }

                          }, action: () =>
                          {
                              o.Execute(NamespaceHelpOperation.Save);
                              SafeConsole.WriteColor(ConsoleColor.Yellow, "  " + o.Name);

                              ImportImages(o, n);
                              Console.WriteLine();

                              return ImportStatus.Applied;
                          })
                );
                deleteAll = deleteTemp;
            }
        }

        private static XElement ParseXML(HelpContent content)
        {
            var ms = new MemoryStream(content.Xml.Bytes);
            XDocument doc = XDocument.Load(ms);
            XElement element = doc.Element(_Namespace)!;

            var uniqueName = element.Attribute(_Name)!.Value;
            if (uniqueName != content.Key)
                throw new InvalidOperationException($"UniqueName attribute ({uniqueName}) does not match with file name ({content.Key})");

            var culture = element.Attribute(_Culture)!.Value;
            if (culture != content.Culture.Name)
                throw new InvalidOperationException($"Culture attribute ({culture}) does not match with folder ({content.Culture.Name})");

            return element;
        }
    }

    public static class QueryXml
    {
        static readonly XName _Name = "Name";
        static readonly XName _Key = "Key";
        static readonly XName _Description = "Description";
        static readonly XName _Culture = "Culture";
        public static readonly XName _Query = "Query";
        static readonly XName _Columns = "Columns";
        static readonly XName _Column = "Column";

        public static XDocument ToXDocument(QueryHelpEntity entity)
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                   new XElement(_Query,
                       new XAttribute(_Key, entity.Query.Key),
                       new XAttribute(_Culture, entity.Culture.Name),
                       entity.Description.HasText() ? new XElement(_Description, entity.Description) : null!,
                        entity.Columns.Any() ?
                           new XElement(_Columns,
                               entity.Columns.Select(c => new XElement(_Column,
                                   new XAttribute(_Name, c.ColumnName),
                                   c.Description!))
                           ) : null!
                       )
                   );
        }

        public static HelpContent ToHelpContent(QueryHelpEntity entity, bool blobs)
        {
            var key = RemoveInvalid(entity.Query.Key);

            return new HelpContent
            {
                Type = entity.GetType().ToTypeEntity(),
                Key = key,
                Culture = entity.Culture.ToCultureInfo(),
                Xml = new FileContent($"{key}.help", blobs ? ToXDocument(entity).ToBytes() : []),
                Images = blobs ? GetImageContents(entity) : []
            };
        }

        public static void Import(List<HelpContent> contents, Replacements rep, 
            ImportExecutionMode mode, ref bool? deleteAll)
        {
            if (contents.Any(c => c.Type.ToType() != typeof(QueryHelpEntity)))
                throw new InvalidOperationException("Some contents are not of type Query");

            SafeConsole.WriteLineColor(ConsoleColor.White, $" Query");

            var groups = contents.GroupToDictionary(c => c.Culture);

            foreach (var kv in groups)
            {
                var ci = kv.Key.ToCultureInfoEntity();
                var current = Database.Query<QueryHelpEntity>().Where(a => a.Culture.Is(ci)).ToList();

                bool? deleteTemp = deleteAll;

                Synchronizer.SynchronizeReplacing(rep, mode == ImportExecutionMode.Interactive ? "Queries" : null,
                      newDictionary: kv.Value.ToDictionaryEx(c => c.Key),
                      oldDictionary: current.ToDictionaryEx(n => n.Query.Key),
                      createNew: (k, n) =>
                          ImportCreate(n, mode, () =>
                          {
                                XElement element = ParseXML(n);

                              var qn = QueryLogic.ToQueryName(k);
                              if (qn == null)
                                  return ImportStatus.Failed;
                              var queryHelp = new QueryHelpEntity
                              {
                                  Culture = ci,
                                  Query = QueryLogic.GetQueryEntity(qn),
                              };

                              ImportXml(queryHelp, element, mode);
                              queryHelp.Execute(QueryHelpOperation.Save);

                              SafeConsole.WriteColor(ConsoleColor.Green, "  " + queryHelp.Query.Key);
                              ImportImages(queryHelp, n);
                              Console.WriteLine();
                              return ImportStatus.Applied;
                          }),
                      removeOld: (k, o) =>
                          {
                              if (mode == ImportExecutionMode.Interactive &&
                                    SafeConsole.Ask(ref deleteTemp, $"Delete Query {o.Query.Key} in {o.Culture}?"))
                              {
                                  o.Delete(QueryHelpOperation.Delete);
                                  SafeConsole.WriteLineColor(ConsoleColor.Red, "  " + o.Query.Key);
                              }
                          },
                      merge: (k, n, o) =>
                          ImportUpdate(n, o, mode, modified: () =>
                          {
                              XElement element = ParseXML(n);

                              o.Culture = ci;
                              o.Description = element.Element(_Description)?.Value;
                              ImportXml(o, element, mode);

                              if (GraphExplorer.IsGraphModified(o))
                                  return true;
                              else
                              {
                                  SafeConsole.WriteColor(ConsoleColor.DarkGray, "  " + o.Query.Key);
                                  return false;
                              }

                          }, action: () =>
                          {
                              o.Execute(QueryHelpOperation.Save);
                              SafeConsole.WriteColor(ConsoleColor.Yellow, "  " + o.Query.Key);

                              ImportImages(o, n);
                              Console.WriteLine();

                              return ImportStatus.Applied;
                          })
                );
                deleteAll = deleteTemp;
            }
        }

        private static XElement ParseXML(HelpContent content)
        {
            var ms = new MemoryStream(content.Xml.Bytes);
            XDocument doc = XDocument.Load(ms);
            XElement element = doc.Element(_Query)!;

            var queryKey = element.Attribute(_Key)!.Value;
            if (queryKey != content.Key)
                throw new InvalidOperationException($"Key attribute ({queryKey}) does not match with file name ({content.Key})");

            var culture = element.Attribute(_Culture)!.Value;
            if (culture != content.Culture.Name)
                throw new InvalidOperationException($"Culture attribute ({culture}) does not match with folder ({content.Culture.Name})");

            return element;
        }

        private static void ImportXml(QueryHelpEntity entity, XElement element, ImportExecutionMode mode)
        {
            entity.Description = element.Element(_Description)?.Value;

            var cols = element.Element(_Columns);
            if (cols != null)
            {
                var queryColumns = QueryLogic.Queries.GetQuery(entity.Query.ToQueryName()).Core.Value.StaticColumns.Select(a => a.Name).ToDictionary(a => a);

                foreach (var item in cols.Elements(_Column))
                {
                    string? name = item.Attribute(_Name)!.Value;

                    name = mode == ImportExecutionMode.Interactive ?
                         SelectInteractive(name, queryColumns, "columns of {0}".FormatWith(entity.Query)) :
                         queryColumns.TryGetC(name);

                    if (name == null)
                        continue;

                    var col = entity.Columns.SingleOrDefaultEx(c => c.ColumnName == name);
                    if (col != null)
                    {
                        col.Description = item.Value;
                    }
                    else
                    {
                        entity.Columns.Add(new QueryColumnHelpEmbedded
                        {
                            ColumnName = name,
                            Description = item.Value
                        });
                    }
                }
            }
        }
    }

    public static class EntityXml
    {
        static readonly XName _CleanName = "CleanName";
        static readonly XName _Name = "Name";
        static readonly XName _Key = "Key";
        static readonly XName _Culture = "Culture";
        public static readonly XName _Entity = "Entity";
        static readonly XName _Description = "Description";
        static readonly XName _Properties = "Properties";
        static readonly XName _Property = "Property";
        static readonly XName _Operations = "Operations";
        static readonly XName _Operation = "Operation";
#pragma warning disable 414
        static readonly XName _Queries = "Queries";
        static readonly XName _Query = "Query";
        static readonly XName _Language = "Language";
#pragma warning restore 414

        public static XDocument ToXDocument(TypeHelpEntity entity)
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(_Entity,
                       new XAttribute(_CleanName, entity.Type.CleanName),
                       new XAttribute(_Culture, entity.Culture.Name),
                       entity.Description.HasText() ? new XElement(_Description, entity.Description) : null!,
                       entity.Properties.Any() ? new XElement(_Properties,
                           entity.Properties.Select(p => new XElement(_Property,
                               new XAttribute(_Name, p.Property.Path),
                               p.Description!))
                       ) : null!,
                       entity.Operations.Any() ? new XElement(_Operations,
                           entity.Operations.Select(o => new XElement(_Operation,
                               new XAttribute(_Key, o.Operation.Key),
                               o.Description!))
                       ) : null!
                   )
               );
        }

        public static HelpContent ToHelpContent(TypeHelpEntity entity, bool blobs)
        {
            var key = RemoveInvalid(entity.Type.CleanName);

            return new HelpContent
            {
                Type = entity.GetType().ToTypeEntity(),
                Key = key,
                Culture = entity.Culture.ToCultureInfo(),
                Xml = new FileContent($"{key}.help", blobs ? ToXDocument(entity).ToBytes() : []),
                Images = blobs ? GetImageContents(entity) : []
            };
        }

        private static void ImportType(TypeHelpEntity entity, XElement element, ImportExecutionMode mode)
        {
            entity.Description = element.Element(_Description)?.Value;

            TypeEntity typeEntity = entity.Type;
            var props = element.Element(_Properties);
            if (props != null)
            {
                var properties = PropertyRouteLogic.RetrieveOrGenerateProperties(typeEntity).Where(pr => ReflectionServer.InTypeScript(pr.ToPropertyRoute())).ToDictionary(a => a.Path);

                foreach (var item in props.Elements(_Property))
                {
                    string name = item.Attribute(_Name)!.Value;

                    var property = mode == ImportExecutionMode.Interactive ?
                        SelectInteractive(name, properties, "properties for {0}".FormatWith(typeEntity.ClassName)) :
                        properties.TryGetC(name);

                    if (property == null)
                        continue;

                    var col = property.IsNew ? null : entity.Properties.SingleOrDefaultEx(c => c.Property.Is(property));
                    if (col != null)
                    {
                        col.Description = item.Value;
                    }
                    else
                    {
                        entity.Properties.Add(new PropertyRouteHelpEmbedded
                        {
                            Property = property,
                            Description = item.Value
                        });
                    }
                }
            }

            var opers = element.Element(_Operations);
            if (opers != null)
            {
                var operations = OperationLogic.TypeOperations(typeEntity.ToType()).ToDictionary(a => a.OperationSymbol.Key, a => a.OperationSymbol);

                foreach (var item in opers.Elements(_Operation))
                {
                    string name = item.Attribute(_Key)!.Value;

                    var operation = mode == ImportExecutionMode.Interactive ? 
                        SelectInteractive(name, operations, "operations for {0}".FormatWith(typeEntity.ClassName)):
                        operations.TryGetC(name); 

                    if (operation == null)
                        continue;

                    var col = entity.Operations.SingleOrDefaultEx(c => c.Operation.Is(operation));
                    if (col != null)
                    {
                        col.Description = item.Value;
                    }
                    else
                    {
                        entity.Operations.Add(new OperationHelpEmbedded
                        {
                            Operation = operation,
                            Description = item.Value
                        });
                    }
                }
            }
        }

        public static string GetEntityFullName(XDocument document, string fileName)
        {
            if (document.Root!.Name != _Entity)
                throw new InvalidOperationException("{0} does not have a {1} root".FormatWith(fileName, _Entity));

            var result = document.Root.Attribute(_CleanName)?.Value;

            if (!result.HasText())
                throw new InvalidOperationException("{0} does not have a {1} attribute".FormatWith(fileName, _CleanName));

            return result;
        }

        public static void Import(List<HelpContent> contents, Replacements rep,
            ImportExecutionMode mode, ref bool? deleteAll)
        {
            if (contents.Any(c => c.Type.ToType() != typeof(TypeHelpEntity)))
                throw new InvalidOperationException("Some contents are not of type Entity");

            SafeConsole.WriteLineColor(ConsoleColor.White, $" Entity");

            var typesByFullName = HelpLogic.AllTypes().ToDictionary(a => TypeLogic.GetCleanName(a)!);

            var groups = contents.GroupToDictionary(c => c.Culture);

            foreach (var kv in groups)
            {
                var ci = kv.Key.ToCultureInfoEntity();
                var current = Database.Query<TypeHelpEntity>().Where(a => a.Culture.Is(ci)).ToList();

                bool? deleteTemp = deleteAll;

                Synchronizer.SynchronizeReplacing(rep, mode == ImportExecutionMode.Interactive ? "TypeHelps" : null,
                      newDictionary: kv.Value.ToDictionaryEx(c => c.Key),
                      oldDictionary: current.ToDictionaryEx(n => n.Type.CleanName),
                      createNew: (k, n) =>
                          ImportCreate(n, mode, () =>
                          {
                              XElement element = ParseXML(n);

                              var cleanName = element.Attribute(_CleanName)!.Value;
                              var typeEntity = typesByFullName.TryGetC(cleanName);
                              if (typeEntity == null)
                              {
                                  n.ImportError = $"Entity type '{cleanName}' not found.";
                                  return ImportStatus.Failed;                                  
                              }

                              var typeHelp = new TypeHelpEntity
                              {
                                  Culture = ci,
                                  Type = typeEntity.ToTypeEntity(),
                              };

                              ImportType(typeHelp, element, mode);
                              typeHelp.Execute(TypeHelpOperation.Save);

                              SafeConsole.WriteColor(ConsoleColor.Green, "  " + typeHelp.Type.CleanName);
                              ImportImages(typeHelp, n);
                              Console.WriteLine();

                              return ImportStatus.Applied;
                          }),
                      removeOld: (k, o) =>
                          {
                              if (mode == ImportExecutionMode.Interactive && 
                                SafeConsole.Ask(ref deleteTemp, $"Delete Entity {o.Type.ClassName} in {o.Culture}?"))
                              {
                                  o.Delete(TypeHelpOperation.Delete);
                                  SafeConsole.WriteLineColor(ConsoleColor.Red, "  " + o.Type.ClassName);
                              }
                          },
                      merge: (k, n, o) =>
                          ImportUpdate(n, o, mode, modified: () =>
                          {
                              XElement element = ParseXML(n);

                              ImportType(o, element, mode);

                              if (GraphExplorer.IsGraphModified(o))
                                  return true;
                              else
                              {
                                  SafeConsole.WriteColor(ConsoleColor.DarkGray, "  " + o.Type.ClassName);
                                  return false;
                              }

                          }, action: () =>
                          {
                              o.Execute(TypeHelpOperation.Save);
                              SafeConsole.WriteColor(ConsoleColor.Yellow, "  " + o.Type.ClassName);

                              ImportImages(o, n);
                              Console.WriteLine();
                              return ImportStatus.Applied;
                          })
                );
                deleteAll = deleteTemp;
            }
        }

        private static XElement ParseXML(HelpContent content)
        {
            var ms = new MemoryStream(content.Xml.Bytes);
            XDocument doc = XDocument.Load(ms);
            XElement element = doc.Element(_Entity)!;

            var uniqueName = element.Attribute(_CleanName)!.Value;
            if (uniqueName != content.Key)
                throw new InvalidOperationException($"UniqueName attribute ({uniqueName}) does not match with file name ({content.Key})");

            var culture = element.Attribute(_Culture)!.Value;
            if (culture != content.Culture.Name)
                throw new InvalidOperationException($"Culture attribute ({culture}) does not match with folder ({content.Culture.Name})");

            return element;
        }
    }

    private static T? SelectInteractive<T>(string str, Dictionary<string, T> dictionary, string context) where T : class
    {
        T? result = dictionary.TryGetC(str);

        if (result != null)
            return result;

        StringDistance sd = new StringDistance();

        var list = dictionary.Keys.Select(s => new { s, lcs = sd.LongestCommonSubsequence(str, s) }).OrderByDescending(s => s.lcs!).Select(a => a.s!).ToList();

        var cs = new ConsoleSwitch<int, string>("{0} has been renamed in {1}".FormatWith(str, context));
        cs.Load(list);

        string? selected = cs.Choose();
        if (selected == null)
            return null;

        return dictionary.GetOrThrow(selected);

    }

    private static HelpContent ToHelpContent(this IHelpEntity entity, bool blobs) => entity switch
    {
        AppendixHelpEntity e => AppendixXml.ToHelpContent(e, blobs),
        NamespaceHelpEntity e => NamespaceXml.ToHelpContent(e, blobs),
        TypeHelpEntity e => EntityXml.ToHelpContent(e, blobs),
        QueryHelpEntity e => QueryXml.ToHelpContent(e, blobs),
        _ => throw new NotSupportedException()
    };

    private static void ImportImages(IHelpEntity entity, HelpContent content)
    {
        var oldImages = Database.Query<HelpImageEntity>().Where(a => a.Target.Is(entity)).ToList();

        Synchronizer.Synchronize(
              newDictionary: content.Images.ToDictionaryEx(i => i.FileName),
              oldDictionary: oldImages.EmptyIfNull().ToDictionaryEx(o => o.Id + "." + o.File.FileName),
              createNew: (k, n) =>
              {
                  Administrator.SaveDisableIdentity(new HelpImageEntity
                  {
                      Target = (entity).ToLite(),
                      File = new FilePathEmbedded(HelpImageFileType.Image, n.FileName.After("."), n.Bytes)
                  }.SetId(Guid.Parse(n.FileName.Before("."))));
                  SafeConsole.WriteColor(ConsoleColor.Green, '.');
              },
              removeOld: (k, o) =>
              {
                  o.Delete();
                  SafeConsole.WriteColor(ConsoleColor.Red, '.');
              },
              merge: (k, n, o) =>
              {
              });
    }

/*    public static string TypesDirectory = "Types";
    public static string QueriesDirectory = "Query";
    public static string OperationsDirectory = "Operation";
    public static string NamespacesDirectory = "Namespace";
    public static string AppendicesDirectory = "Appendix";*/

    #region Export methods

    public static void ExportAll(string path)
    {
        HashSet<CultureInfo> cultures = GetAllCultures(path);

        foreach (var ci in cultures)
        {
            ExportCulture(path, ci);
        }
    }

    private static HashSet<CultureInfo> GetAllCultures(string path)
    {
        HashSet<CultureInfo> cultures =
        [
            .. Database.Query<AppendixHelpEntity>().Select(a => a.Culture).Distinct().ToList().Select(c => c.ToCultureInfo()),
            .. Database.Query<NamespaceHelpEntity>().Select(a => a.Culture).Distinct().ToList().Select(c => c.ToCultureInfo()),
            .. Database.Query<TypeHelpEntity>().Select(a => a.Culture).Distinct().ToList().Select(c => c.ToCultureInfo()),
            .. Database.Query<QueryHelpEntity>().Select(a => a.Culture).Distinct().ToList().Select(c => c.ToCultureInfo()),
        ];
        if (Directory.Exists(path))
            cultures.AddRange(new DirectoryInfo(path).GetDirectories().Select(c => CultureInfo.GetCultureInfo(c.Name)));
        return cultures;
    }

    private static void ExportCulture(string path, CultureInfo ci)
    {
        bool? replace = null;
        bool? delete = null;

        var cie = ci.ToCultureInfoEntity();

        Export(ref replace, ref delete, Path.Combine(path, ci.Name),
            [.. Database.Query<AppendixHelpEntity>().Where(e => e.Culture.Is(cie)).Select(a => a.ToHelpContent(true))]);

        Export(ref replace, ref delete, Path.Combine(path, ci.Name),
            [.. Database.Query<NamespaceHelpEntity>().Where(e => e.Culture.Is(cie)).Select(a => a.ToHelpContent(true))]);

        Export(ref replace, ref delete, Path.Combine(path, ci.Name),
            [.. Database.Query<TypeHelpEntity>().Where(e => e.Culture.Is(cie)).Select(a => a.ToHelpContent(true))]);

        Export(ref replace, ref delete, Path.Combine(path, ci.Name),
            [.. Database.Query<QueryHelpEntity>().Where(e => e.Culture.Is(cie)).Select(a => a.ToHelpContent(true))]);

    }

    private static void Save(this FileContent content, string path)
    {
        Directory.CreateDirectory(path);
        File.WriteAllBytes(path: Path.Combine(path, content.FileName), content.Bytes);
    }

    private static void Save(this FileContent[] contents, string path)
    {
        if (contents.IsEmpty())
            return;
        Directory.CreateDirectory(path);
        foreach (var c in contents)
            c.Save(path);
    }

    private static void Export(ref bool? replace, ref bool? delete, string rootPath, List<HelpContent> should)
    {
        if (should.IsEmpty())
            return;
        var deleteLocal = delete;
        var replaceLocal = replace;

        var typeFolder = should.Select(c => c.Type.CleanName.Before("Help")).Distinct().SingleEx(() => "Distinct HelpContent.Type");

        var fullPath = Path.Combine(rootPath, typeFolder);

        SafeConsole.WriteLineColor(ConsoleColor.Gray, "Exporting to " + fullPath);

         Synchronizer.Synchronize(
             newDictionary: should.ToDictionary(c => c.Key),
             oldDictionary: !Directory.Exists(fullPath) ? new() : Directory.GetFiles(fullPath, "*.help").ToDictionary(a => Path.GetFileNameWithoutExtension(a)),
             createNew: (key, content) =>
             {
                 content.Xml.Save(fullPath);
                 SafeConsole.WriteColor(ConsoleColor.Green, " Created " + content.Key);

                 var imgDirectory = Path.Combine(fullPath, key);
                 content.Images.Save(imgDirectory);

                 Console.WriteLine();
             },
             removeOld: (key, fullName) =>
             {
                 var fileName = Path.GetRelativePath(rootPath, fullName);
                 if (SafeConsole.Ask(ref deleteLocal, "Delete {0}?".FormatWith(fileName)))
                 {
                     File.Delete(fullName);
                     SafeConsole.WriteLineColor(ConsoleColor.Red, " Deleted " + fileName);

                     var imgDirectory = Path.Combine(fullPath, key);
                     if (Directory.Exists(imgDirectory))
                         Directory.Delete(imgDirectory, true);
                 }
             },
             merge: (key, content, fullName) =>
             {
                 var newBytes = content.Xml.Bytes;
                 var oldBytes = File.ReadAllBytes(fullName);

                 if (!MemoryExtensions.SequenceEqual<byte>(newBytes, oldBytes))
                 {
                     if (SafeConsole.Ask(ref replaceLocal, " Override {0}?".FormatWith(key)))
                     {
                         content.Xml.Save(fullPath);
                         SafeConsole.WriteColor(ConsoleColor.Yellow, " Overriden " + content.Key);
                     }
                 }
                 else
                 {
                     SafeConsole.WriteColor(ConsoleColor.DarkGray, " Identical " + key);
                 }

                 var imgDirectory = Path.Combine(fullPath, key);
                 var images = content.Images;
                 if (images.Length > 0)
                 {
                     var currImages = !Directory.Exists(imgDirectory) ? [] : Directory.GetFiles(imgDirectory).ToDictionary(a => Path.GetFileName(a));
                     var shouldImages = images.ToDictionaryEx(a => a.FileName);

                     Synchronizer.Synchronize(
                           newDictionary: shouldImages,
                           oldDictionary: currImages,
                           createNew: (fileName, content) =>
                           {
                               content.Save(imgDirectory);
                               SafeConsole.WriteColor(ConsoleColor.DarkGreen, '.');
                           },
                           removeOld: (imageFileName, imageFullPath) =>
                           {
                               File.Delete(imageFullPath);
                               SafeConsole.WriteColor(ConsoleColor.DarkRed, '.');
                           },
                           merge: (k, n, o) =>
                           {
                           });
                 }
                 else
                 {
                     if (Directory.Exists(imgDirectory))
                         Directory.Delete(imgDirectory, true);
                 }

                 Console.WriteLine();
             });

        delete = deleteLocal;
        replace = replaceLocal;
    }

    public static void ExportAllToZip(string zipFile)
    {
        List<IHelpEntity> all  = 
        [.. Database.Query<AppendixHelpEntity>().Select(a => a),        
         .. Database.Query<NamespaceHelpEntity>().Select(a => a),
         .. Database.Query<TypeHelpEntity>().Select(a => a),
         .. Database.Query<QueryHelpEntity>().Select(a => a)];

        var bytes = ExportToZipBytes(all, "Help");
        File.WriteAllBytes(zipFile, bytes);
    }

    private static void WriteToZip(ZipArchive zip, FileContent content, string path)
    {
        var fileName = Path.Combine(path, content.FileName).Replace(Path.DirectorySeparatorChar, '/');
        var entry = zip.CreateEntry(fileName);
        using var entryStream = entry.Open();
        entryStream.Write(content.Bytes, 0, content.Bytes.Length);
    }

    public static byte[] ExportToZipBytes(List<IHelpEntity> entities, string root)
    {
        var contents = entities.Select(e => e.ToHelpContent(true));

        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var help in contents)
            {
                var typeFolder = help.Type.CleanName.Before("Help");
                var typePath = Path.Combine(root, help.Culture.Name, typeFolder);

                WriteToZip(zip, help.Xml, typePath);

                foreach (var img in help.Images)
                {
                    var imgPath = Path.Combine(typePath, help.Key);
                    WriteToZip(zip, img, imgPath);
                }
            }
        }

        var bytes = output.ToArray();
        return bytes;
    }

    #endregion Export methods

    public static void ImportAll(string path)
    { 
        var contents = LoadContentsFromDisk(path);
        ImportContents(ref contents, ImportExecutionMode.Interactive);
    }

    private static List<HelpContent> LoadContentsFromDisk(string rootPath)
    {
        var helpContents = new List<HelpContent>();

        foreach (var helpFile in Directory.EnumerateFiles(rootPath, "*.help", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(rootPath, helpFile);
            var parts = relativePath.Split(Path.DirectorySeparatorChar);

            if (parts.Length != 3) continue;

            var culture = new CultureInfo(parts[0]);
            var typeFolder = parts[1];
            var typeCleanName = $"{typeFolder}Help";
            var type = TypeLogic.GetType(typeCleanName).ToTypeEntity();
            var key = Path.GetFileNameWithoutExtension(parts[2]);

            var xml = new FileContent(Path.GetFileName(helpFile), File.ReadAllBytes(helpFile));

            var imageDir = Path.Combine(rootPath, culture.Name, typeFolder, key);
            var images = Directory.Exists(imageDir)
                ? [.. Directory.GetFiles(imageDir).Select(path => new FileContent(Path.GetFileName(path), File.ReadAllBytes(path)))]
                : Array.Empty<FileContent>();

            helpContents.Add(new HelpContent
            {
                Type = type,
                Key = key,
                Culture = culture,
                Xml = xml,
                Images = images
            });
        }

        return helpContents;
    }

    private static void ImportContents(ref List<HelpContent> contents, ImportExecutionMode mode, 
        bool? initialDeleteAll = null)
    {
        Replacements rep = [];
        bool? deleteAll = initialDeleteAll;

        var importers = new Dictionary<Type, Action<List<HelpContent>>>
        {
            [typeof(AppendixHelpEntity)] = contents => AppendixXml.Import(contents, rep, mode, ref deleteAll),
            [typeof(NamespaceHelpEntity)] = contents => NamespaceXml.Import(contents, rep, mode, ref deleteAll),
            [typeof(TypeHelpEntity)] = contents => EntityXml.Import(contents, rep, mode, ref deleteAll),
            [typeof(QueryHelpEntity)] = contents => QueryXml.Import(contents, rep, mode, ref deleteAll),
        };

        var types = contents.GroupToDictionary(c => c.Type);
        List<HelpContent> result = [];

        foreach (var type in types)
        {
            if (!importers.TryGetValue(type.Key.ToType(), out var import))
                throw new InvalidOperationException($"Unknown HelpContent Type: {type.Key}");

            import(type.Value);

            result.AddRange(type.Value);
        }
        contents = result;
    }

    public static HelpImportReportModel ImportFromZip(byte[] zipFile, HelpImportPreviewModel model)
    { 
        var contents = LoadContentsFromZip(zipFile);
        contents.ForEach(c =>
        {
            var line = model.Lines.SingleEx(l => l.Culture.ToCultureInfo().Equals(c.Culture) && l.Type.Is(c.Type) && l.Key == c.Key);
            c.Action = line.Action;
            c.Apply = line.Apply == true;

        });

        ImportContents(ref contents, ImportExecutionMode.ApplyPreview, initialDeleteAll: false);

        var lines = contents.Select(c => c.ToReportLine()).ToMList();

        return new HelpImportReportModel
        {
            Lines = lines
        };

    }

    public static HelpImportPreviewModel ImportPreviewFromZip(byte[] zipFile)
    {
        var contents = LoadContentsFromZip(zipFile);
        
        ImportContents(ref contents, ImportExecutionMode.Preview, initialDeleteAll: false);
        
        var lines = contents.Select(c => c.ToPreviewLine()).ToMList();

        return new HelpImportPreviewModel
        {
            Lines = lines
        };
    }

    private static List<HelpContent> LoadContentsFromZip(byte[] zipFile)
    {
        using var input = new MemoryStream(zipFile);
        using var zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);

        var helpContents = new Dictionary<string, HelpContent>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in zip.Entries)
        {
            if (entry.FullName.EndsWith("/")) continue;

            var parts = entry.FullName.Split('/');
            if (parts.Length < 4 || !parts[0].Equals("Help", StringComparison.OrdinalIgnoreCase)) continue;

            var culture = new CultureInfo(parts[1]);
            var typeCleanName = $"{parts[2]}Help";
            var type = TypeLogic.GetType(typeCleanName).ToTypeEntity();

            if (entry.Name.EndsWith(".help", StringComparison.OrdinalIgnoreCase))
            {
                var key = Path.GetFileNameWithoutExtension(entry.Name);
                var contentKey = $"{culture.Name}/{typeCleanName}/{key}";

                using var stream = entry.Open();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);

                helpContents[contentKey] = new HelpContent
                {
                    Type = type,
                    Key = key,
                    Culture = culture,
                    Xml = new FileContent(entry.Name, ms.ToArray()),
                    Images = Array.Empty<FileContent>()
                };
            }
        }

        // Second pass: only attach images to known help entries
        foreach (var entry in zip.Entries)
        {
            if (entry.FullName.EndsWith(".help", StringComparison.OrdinalIgnoreCase) || entry.FullName.EndsWith("/"))
                continue;

            var parts = entry.FullName.Split('/');
            if (parts.Length != 5) continue;

            var culture = parts[1];
            var typeCleanName = $"{parts[2]}Help";
            var type = TypeLogic.GetType(typeCleanName).ToTypeEntity();
            var key = parts[3];
            var contentKey = $"{culture}/{typeCleanName}/{key}";

            if (!helpContents.TryGetValue(contentKey, out var helpContent))
                throw new FileNotFoundException($"Help xml file (.help) for image folder: '{contentKey}' not found.");

            using var stream = entry.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);

            var image = new FileContent(entry.Name, ms.ToArray());
            helpContent.Images = helpContent.Images.Append(image).ToArray();
        }

        return [.. helpContents.Values];
    }

    public static void ImportFromZip(string zipFile)
    {
        using var ms = new FileStream(zipFile, FileMode.Open);
        var contents = LoadContentsFromZip(ms.ReadAllBytes());

        ImportContents(ref contents, ImportExecutionMode.Interactive, initialDeleteAll: false);
    }

    public static void ImportExportHelpMenu()
    {
        using (UserHolder.UserSession(AuthLogic.SystemUser!))
            ImportExportHelp(@"..\..\..\Help");
    }

    public static void ImportExportHelp(string path)
    {
    retry:
        Console.WriteLine("You want to export (e) or import (i) Help? (nothing to exit)");

        switch (Console.ReadLine()!.ToLower())
        {
            case "": return;
            case "e": ExportAll(path); break;
            case "i": ImportAll(path); break;
            case "ez": ExportAllToZip(@"..\..\..\Help.zip"); break;
            case "iz": ImportFromZip(@"..\..\..\Help.zip"); break;
            default:
                goto retry;
        }
    }
}

