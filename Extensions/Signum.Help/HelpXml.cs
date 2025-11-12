using Signum.API;
using Signum.Engine.Sync;
using Signum.Files;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

// Note: Avoid `using System.IO` to prevent accidental use in HelpXml.
// Use FileSystemScope for all file system operations instead.
using FS = Signum.Files.FileSystemScope;

namespace Signum.Help;

public static class HelpXml
{
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


        internal static void LoadDirectory(string directory, CultureInfoEntity ci, Dictionary<Lite<IHelpEntity>, List<HelpImageEntity>> images, 
            Replacements rep, ref bool? deleteAll)
        {

            SafeConsole.WriteLineColor(ConsoleColor.White, $" Appendix");

            var current = Database.Query<AppendixHelpEntity>().Where(a => a.Culture.Is(ci)).ToList();

            var files = FS.Directory.Exists(directory) ? FS.Directory.GetFiles(directory, "*.help") : null;

            XElement ParseXML(string path)
            {
                XDocument doc = XDocument.Load(path);
                XElement element = doc.Element(_Appendix)!;

                var uniqueName = element.Attribute(_Name)!.Value;
                if (uniqueName != FS.Path.GetFileNameWithoutExtension(path))
                    throw new InvalidOperationException($"UniqueName attribute ({uniqueName}) does not match with file name ({path})");

                var culture = element.Attribute(_Culture)!.Value;
                if (culture != ci.Name)
                    throw new InvalidOperationException($"Culture attribute ({culture}) does not match with folder ({path})");

                return element;
            }


            bool? deleteTemp = deleteAll;

            Synchronizer.SynchronizeReplacing(rep, "Appendix",
                  newDictionary: files.EmptyIfNull().ToDictionaryEx(o => FS.Path.GetFileNameWithoutExtension(o)),
                  oldDictionary: current.ToDictionaryEx(n => n.UniqueName),
                  createNew: (k, n) =>
                  {
                      XElement element = ParseXML(n);

                      var a = new AppendixHelpEntity
                      {
                          Culture = ci,
                          UniqueName = element.Attribute(_Name)!.Value,
                          Title = element.Attribute(_Title)!.Value,
                          Description = element.Element(_Description)?.Value
                      }.Save();

                      SafeConsole.WriteColor(ConsoleColor.Green, "  " + a.UniqueName);
                      ImportImages(a, n, null);
                      Console.WriteLine();
                  },
                  removeOld: (k, o) =>
                  {
                      if (SafeConsole.Ask(ref deleteTemp, $"Delete Appendix {o.UniqueName} in {o.Culture}?"))
                      {
                          o.Delete();
                          SafeConsole.WriteLineColor(ConsoleColor.Red, "  " + o.UniqueName);
                      }
                  },
                  merge: (k, n, o) =>
                  {
                      XElement element = ParseXML(n);

                      o.Title = element.Attribute(_Title)!.Value;
                      o.Description = element.Element(_Description)?.Value;

                      if (GraphExplorer.IsGraphModified(o))
                      {
                          o.Save();
                          SafeConsole.WriteColor(ConsoleColor.Yellow, "  " + o.UniqueName);
                      }
                      else
                      {
                          SafeConsole.WriteColor(ConsoleColor.DarkGray, "  " + o.UniqueName);
                      }

                      ImportImages(o, n, images.TryGetC(o.ToLite()));
                      Console.WriteLine();
                  });

            deleteAll = deleteTemp;
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

        internal static string GetNamespaceName(XDocument document, string fileName)
        {
            if (document.Root!.Name != _Namespace)
                throw new InvalidOperationException("{0} does not have a {1} root".FormatWith(fileName, _Namespace));

            var result = document.Root.Attribute(_Name)?.Value;

            if (!result.HasText())
                throw new InvalidOperationException("{0} does not have a {1} attribute".FormatWith(fileName, _Name));
            return result;
        }

        internal static void LoadDirectory(string directory, CultureInfoEntity ci, Dictionary<Lite<IHelpEntity>, List<HelpImageEntity>> images, 
            Replacements rep, ref bool? deleteAll)
        {

            SafeConsole.WriteLineColor(ConsoleColor.White, $" Namespace");

            var current = Database.Query<NamespaceHelpEntity>().Where(a => a.Culture.Is(ci)).ToList();

            var files = FS.Directory.Exists(directory) ? FS.Directory.GetFiles(directory, "*.help") : null;

            XElement ParseXML(string path)
            {
                XDocument doc = XDocument.Load(path);
                XElement element = doc.Element(_Namespace)!;

                var uniqueName = element.Attribute(_Name)!.Value;
                if (uniqueName != FS.Path.GetFileNameWithoutExtension(path))
                    throw new InvalidOperationException($"UniqueName attribute ({uniqueName}) does not match with file name ({path})");

                var culture = element.Attribute(_Culture)!.Value;
                if (culture != ci.Name)
                    throw new InvalidOperationException($"Culture attribute ({culture}) does not match with folder ({path})");

                return element;
            }

            bool? deleteTemp = deleteAll;

            Synchronizer.SynchronizeReplacing(rep, "Namespace", 
                  newDictionary: files.EmptyIfNull().ToDictionaryEx(o => FS.Path.GetFileName(o)),
                  oldDictionary: current.ToDictionaryEx(n => n.Name),
                  createNew: (k, n) =>
                  {
                      XElement element = ParseXML(n);

                      var a = new NamespaceHelpEntity
                      {
                          Culture = ci,
                          Name = element.Attribute(_Name)!.Value,
                          Title = element.Attribute(_Title)?.Value,
                          Description = element.Element(_Description)?.Value
                      }.Save();

                      SafeConsole.WriteColor(ConsoleColor.Green, "  " + a.Name);
                      ImportImages(a, n, null);

                      Console.WriteLine();
                  },
                  removeOld: (k, o) =>
                  {
                      if (SafeConsole.Ask(ref deleteTemp, $"Delete Namespace {o.Name} in {o.Culture}?"))
                      {
                          o.Delete();
                          SafeConsole.WriteLineColor(ConsoleColor.Red, "  " + o.Name);
                      }
                  },
                  merge: (k, n, o) =>
                  {
                      XElement element = ParseXML(n);

                      o.Title = element.Attribute(_Title)?.Value;
                      o.Description = element.Element(_Description)?.Value;

                      if (GraphExplorer.IsGraphModified(o))
                      {
                          o.Save();
                          SafeConsole.WriteColor(ConsoleColor.Yellow, "  " + o.Name);
                      }
                      else
                      {
                          SafeConsole.WriteColor(ConsoleColor.DarkGray, "  " + o.Name);
                      }

                      ImportImages(o, n, images.TryGetC(o.ToLite()));

                      Console.WriteLine();
                  });

            deleteAll = deleteTemp;
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

        internal static void LoadDirectory(string directory, CultureInfoEntity ci, Dictionary<Lite<IHelpEntity>, List<HelpImageEntity>> images, 
            Replacements rep, ref bool? deleteAll)
        {
            SafeConsole.WriteLineColor(ConsoleColor.White, $" Query");

            var current = Database.Query<QueryHelpEntity>().Where(a => a.Culture.Is(ci)).ToList();

            var files = FS.Directory.Exists(directory) ? FS.Directory.GetFiles(directory, "*.help") : null;

            XElement ParseXML(string path)
            {
                XDocument doc = XDocument.Load(path);
                XElement element = doc.Element(_Query)!;

                var queryKey = element.Attribute(_Key)!.Value;
                if (queryKey != FS.Path.GetFileNameWithoutExtension(path))
                    throw new InvalidOperationException($"Key attribute ({queryKey}) does not match with file name ({path})");

                var culture = element.Attribute(_Culture)!.Value;
                if (culture != ci.Name)
                    throw new InvalidOperationException($"Culture attribute ({culture}) does not match with folder ({path})");

                return element;
            }

            bool? deleteTemp = deleteAll;

            Synchronizer.SynchronizeReplacing(rep, "Queries",
                  newDictionary: files.EmptyIfNull().ToDictionaryEx(o => FS.Path.GetFileName(o)),
                  oldDictionary: current.ToDictionaryEx(n => n.Query.Key),
                  createNew: (k, n) =>
                  {
                      XElement element = ParseXML(n);
                      var qn = QueryLogic.ToQueryName(k.Before(".help"));
                      var queryHelp = new QueryHelpEntity
                      {
                          Culture = ci,
                          Query = QueryLogic.GetQueryEntity(qn),
                      };

                      ImportXml(queryHelp, element);
                      queryHelp.Save();

                      SafeConsole.WriteColor(ConsoleColor.Green, "  " + queryHelp.Query.Key);

                      ImportImages(queryHelp, n, null);

                      Console.WriteLine();
                  },
                  removeOld: (k, o) =>
                  {
                      if (SafeConsole.Ask(ref deleteTemp, $"Delete Query {o.Query.Key} in {o.Culture}?"))
                      {
                          o.Delete();
                          SafeConsole.WriteLineColor(ConsoleColor.Red, "  " + o.Query.Key);
                      }
                  },
                  merge: (k, n, queryHelp) =>
                  {
                      XElement element = ParseXML(n);

                      queryHelp.Culture = ci;
                      queryHelp.Description = element.Element(_Description)?.Value;
                      ImportXml(queryHelp, element);

                      if (GraphExplorer.IsGraphModified(queryHelp))
                      {
                          queryHelp.Save();
                          SafeConsole.WriteColor(ConsoleColor.Yellow, "  " + queryHelp.Query.Key);
                      }
                      else
                      {
                          SafeConsole.WriteColor(ConsoleColor.DarkGray, "  " + queryHelp.Query.Key);
                      }

                      ImportImages(queryHelp, n, images.TryGetC(queryHelp.ToLite()));

                      Console.WriteLine();
                  });

            deleteAll = deleteTemp;
        }

        private static void ImportXml(QueryHelpEntity entity, XElement element)
        {
            entity.Description = element.Element(_Description)?.Value;

            var cols = element.Element(_Columns);
            if (cols != null)
            {
                var queryColumns = QueryLogic.Queries.GetQuery(entity.Query.ToQueryName()).Core.Value.StaticColumns.Select(a => a.Name).ToDictionary(a => a);

                foreach (var item in cols.Elements(_Column))
                {
                    string? name = item.Attribute(_Name)!.Value;
                    name = SelectInteractive(name, queryColumns, "columns of {0}".FormatWith(entity.Query));

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

        private static void ImportType(TypeHelpEntity entity, XElement element)
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

                    var property = SelectInteractive(name, properties, "properties for {0}".FormatWith(typeEntity.ClassName));
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
                    var operation = SelectInteractive(name, operations, "operations for {0}".FormatWith(typeEntity.ClassName));

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


        internal static void LoadDirectory(string directory, CultureInfoEntity ci, Dictionary<Lite<IHelpEntity>, List<HelpImageEntity>> images, 
            Replacements rep, ref bool? deleteAll)
        {

            SafeConsole.WriteLineColor(ConsoleColor.White, $" Entity");

            var typesByFullName = HelpLogic.AllTypes().ToDictionary(a => TypeLogic.GetCleanName(a)!);

            var current = Database.Query<TypeHelpEntity>().Where(a => a.Culture.Is(ci)).ToList();

            var files = FS.Directory.Exists(directory) ? FS.Directory.GetFiles(directory, "*.help") : null;

            XElement ParseXML(string path)
            {
                XDocument doc = XDocument.Load(path);
                XElement element = doc.Element(_Entity)!;

                var uniqueName = element.Attribute(_CleanName)!.Value;
                if (uniqueName != FS.Path.GetFileNameWithoutExtension(path))
                    throw new InvalidOperationException($"UniqueName attribute ({uniqueName}) does not match with file name ({path})");

                var culture = element.Attribute(_Culture)!.Value;
                if (culture != ci.Name)
                    throw new InvalidOperationException($"Culture attribute ({culture}) does not match with folder ({path})");

                return element;
            }

            bool? deleteTemp = null;

            Synchronizer.SynchronizeReplacing(rep, "Types",
                  newDictionary: files.EmptyIfNull().ToDictionaryEx(o => FS.Path.GetFileNameWithoutExtension(o)),
                  oldDictionary: current.ToDictionaryEx(n => n.Type.CleanName),
                  createNew: (k, path) =>
                  {
                      XElement element = ParseXML(path);
                      var cleanName = element.Attribute(_CleanName)!.Value;
                      var typeEntity = typesByFullName.GetOrThrow(cleanName);

                      var typeHelp = new TypeHelpEntity
                      {
                          Culture = ci,
                          Type = typeEntity.ToTypeEntity(),
                      };

                      ImportType(typeHelp, element);
                      typeHelp.Save();

                      SafeConsole.WriteColor(ConsoleColor.Green, "  " + typeHelp.Type.CleanName);
                      ImportImages(typeHelp, path, null);
                      Console.WriteLine();
                  },
                  removeOld: (k, o) =>
                  {
                      if (SafeConsole.Ask(ref deleteTemp, $"Delete Type {o.Type.CleanName} in {o.Culture}?"))
                      {
                          o.Delete();
                          SafeConsole.WriteLineColor(ConsoleColor.Red, "  " + o.Type.CleanName);
                      }
                  },
                  merge: (k, path, typeEntity) =>
                  {
                      XElement element = ParseXML(path);
                      
                      ImportType(typeEntity, element);

                      if (GraphExplorer.IsGraphModified(typeEntity))
                      {
                          typeEntity.Save();
                          SafeConsole.WriteColor(ConsoleColor.Yellow, "  " + typeEntity.Type.CleanName);
                      }
                      else
                      {
                          SafeConsole.WriteColor(ConsoleColor.DarkGray, "  " + typeEntity.Type.CleanName);
                      }

                      ImportImages(typeEntity, path, images.TryGetC(typeEntity.ToLite()));

                      Console.WriteLine();
                  });

            deleteAll = deleteTemp;
        }
    }

    private static void ImportImages(Entity entity, string filePath, List<HelpImageEntity>? images)
    {

        var imageDir = FS.Path.Combine(FS.Path.GetDirectoryName(filePath)!, FS.Path.GetFileNameWithoutExtension(filePath));
        var newImages = FS.Directory.Exists(imageDir) ? FS.Directory.GetFiles(imageDir) : null;

        Synchronizer.Synchronize(
              newDictionary: newImages.EmptyIfNull().ToDictionaryEx(n => FS.Path.GetFileName(n)),
              oldDictionary: images.EmptyIfNull().ToDictionaryEx(o => o.Id + "." + o.File.FileName),
              createNew: (k, n) =>
              {
                  Administrator.SaveDisableIdentity(new HelpImageEntity
                  {
                      Target = ((IHelpEntity)entity).ToLite(),
                      File = new FilePathEmbedded(HelpImageFileType.Image, FS.Path.GetFileName(n).After("."), FS.File.ReadAllBytes(n))
                  }.SetId(Guid.Parse(FS.Path.GetFileName(n).Before("."))));
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

    public static string TypesDirectory = "Types";
    public static string QueriesDirectory = "Query";
    public static string OperationsDirectory = "Operation";
    public static string NamespacesDirectory = "Namespace";
    public static string AppendicesDirectory = "Appendix";

    public static T? SelectInteractive<T>(string str, Dictionary<string, T> dictionary, string context) where T :class
    {
        T? result = dictionary.TryGetC(str);
        
        if(result != null)
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

    static string RemoveInvalid(string name)
    {
        return Regex.Replace(name, "[" + Regex.Escape(new string(FS.Path.GetInvalidPathChars())) + "]", "");
    }

    public static void ExportAll(string directoryName)
    {
        HashSet<CultureInfo> cultures = GetAllCultures(directoryName);

        foreach (var ci in cultures)
        {
            ExportCulture(directoryName, ci);
        }
    }

    private static HashSet<CultureInfo> GetAllCultures(string directoryName)
    {

        HashSet<CultureInfo> cultures = new HashSet<CultureInfo>();
        cultures.AddRange(Database.Query<AppendixHelpEntity>().Select(a => a.Culture).Distinct().ToList().Select(c => c.ToCultureInfo()));
        cultures.AddRange(Database.Query<NamespaceHelpEntity>().Select(a => a.Culture).Distinct().ToList().Select(c => c.ToCultureInfo()));
        cultures.AddRange(Database.Query<TypeHelpEntity>().Select(a => a.Culture).Distinct().ToList().Select(c => c.ToCultureInfo()));
        cultures.AddRange(Database.Query<QueryHelpEntity>().Select(a => a.Culture).Distinct().ToList().Select(c => c.ToCultureInfo()));
        if (FS.Directory.Exists(directoryName))
            cultures.AddRange(FS.DirectoryInfo.New(directoryName).GetDirectories().Select(c => CultureInfo.GetCultureInfo(c.Name)));
        return cultures;
    }

    public static void ExportCulture(string directoryName, CultureInfo ci)
    {
        bool? replace = null;
        bool? delete = null;
      
        var cie = ci.ToCultureInfoEntity();

        var group = Database.Query<HelpImageEntity>().GroupToDictionary(a => a.Target);

        ExportFolder(ref replace, ref delete, FS.Path.Combine(directoryName, ci.Name, AppendicesDirectory),
            Database.Query<AppendixHelpEntity>().Where(ah => ah.Culture.Is(cie)).ToList(),
            ah => "{0}.help".FormatWith(RemoveInvalid(ah.UniqueName), ah.Culture.Name),
            ah => AppendixXml.ToXDocument(ah),
            ah => group.TryGetC(ah.ToLite()));

        ExportFolder(ref replace, ref delete, FS.Path.Combine(directoryName, ci.Name, NamespacesDirectory),
            Database.Query<NamespaceHelpEntity>().Where(nh => nh.Culture.Is(cie)).ToList(),
            nh => "{0}.help".FormatWith(RemoveInvalid(nh.Name), nh.Culture.Name),
            nh => NamespaceXml.ToXDocument(nh),
            ah => group.TryGetC(ah.ToLite()));

        ExportFolder(ref replace, ref delete, FS.Path.Combine(directoryName, ci.Name, TypesDirectory),
            Database.Query<TypeHelpEntity>().Where(th => th.Culture.Is(cie)).ToList(),
            th => "{0}.help".FormatWith(RemoveInvalid(th.Type.CleanName), th.Culture.Name),
            th => EntityXml.ToXDocument(th),
            ah => group.TryGetC(ah.ToLite()));

        ExportFolder(ref replace, ref delete, FS.Path.Combine(directoryName, ci.Name, QueriesDirectory),
            Database.Query<QueryHelpEntity>().Where(qh => qh.Culture.Is(cie)).ToList(),
            qh => "{0}.help".FormatWith(RemoveInvalid(qh.Query.Key), qh.Culture.Name),
            qh => QueryXml.ToXDocument(qh),
            ah => group.TryGetC(ah.ToLite()));
    }

    private static void SaveXml(this XDocument doc, string fileName)
    {
        using var stream = FS.File.OpenWrite(fileName);
        doc.Save(stream);
    }

    public static void ExportFolder<T>(ref bool? replace, ref bool? delete, string folder, List<T> should,  Func<T, string> fileName, Func<T, XDocument> toXML, Func<T, List<HelpImageEntity>?> getImages)
    {

        if (should.Any() && !FS.Directory.Exists(folder))
            FS.Directory.CreateDirectory(folder);

        var deleteLocal = delete;
        var replaceLocal = replace;

        SafeConsole.WriteLineColor(ConsoleColor.Gray, "Exporting to " + folder);
        Synchronizer.Synchronize(
            newDictionary: should.ToDictionary(fileName),
            oldDictionary: !FS.Directory.Exists(folder) ? new() : FS.Directory.GetFiles(folder).ToDictionary(a => FS.Path.GetFileName(a)),
            createNew: (fileName, entity) =>
            {
                toXML(entity).SaveXml(FS.Path.Combine(folder, fileName));
                SafeConsole.WriteColor(ConsoleColor.Green, " Created " + fileName);

                var images = getImages(entity);
                if (images != null)
                {
                    var imgDirectory = FS.Path.Combine(folder, FS.Path.GetFileNameWithoutExtension(fileName));
                    FS.Directory.CreateDirectory(imgDirectory);

                    foreach (var img in images)
                    {
                        var bla = FS.Path.Combine(imgDirectory, img.Id + "." + img.File.FileName);
                        FS.File.WriteAllBytes(bla, img.File.GetByteArray());
                    }
                }

                Console.WriteLine();
            },
            removeOld: (fileName, fullName) =>
            {
                if (SafeConsole.Ask(ref deleteLocal, "Delete {0}?".FormatWith(fileName)))
                {
                    FS.File.Delete(fullName);
                    SafeConsole.WriteLineColor(ConsoleColor.Red, " Deleted " + fileName);

                    var imgDirectory = FS.Path.Combine(folder, FS.Path.GetFileNameWithoutExtension(fileName));
                    if (FS.Directory.Exists(imgDirectory))
                        FS.Directory.Delete(imgDirectory, true);
                }
            },
            merge: (fileName, entity, fullName) =>
            {
                var xml = toXML(entity);

                var newBytes = new System.IO.MemoryStream().Do(ms => xml.Save(ms)).ToArray();
                var oldBytes = FS.File.ReadAllBytes(fullName);

                if (!MemoryExtensions.SequenceEqual<byte>(newBytes, oldBytes))
                {
                    if (SafeConsole.Ask(ref replaceLocal, " Override {0}?".FormatWith(fileName)))
                    {
                        
                        xml.SaveXml(FS.Path.Combine(folder, fileName));
                        SafeConsole.WriteColor(ConsoleColor.Yellow, " Overriden " + fileName);
                    }
                }
                else
                {
                    SafeConsole.WriteColor(ConsoleColor.DarkGray, " Identical " + fileName);
                }

                var imgDirectory = FS.Path.Combine(folder, FS.Path.GetFileNameWithoutExtension(fileName));
                var images = getImages(entity);
                if (images != null)
                {
                    var currImages = FS.Directory.GetFiles(imgDirectory).ToDictionary(a => FS.Path.GetFileName(a));

                    var shouldImages = images.ToDictionaryEx(a => a.Id + "." + a.File.FileName);

                    var newEntities = new List<T>();
                    Synchronizer.Synchronize(
                          newDictionary: shouldImages,
                          oldDictionary: currImages,
                          createNew: (k, n) =>
                          {
                              FS.File.WriteAllBytes(FS.Path.Combine(imgDirectory, k), n.File.GetByteArray());
                              SafeConsole.WriteColor(ConsoleColor.DarkGreen, '.');
                          },
                          removeOld: (k, o) =>
                          {
                              FS.File.Delete(FS.Path.Combine(imgDirectory, k));
                              SafeConsole.WriteColor(ConsoleColor.DarkRed, '.');
                          },
                          merge: (k, n, o) =>
                          {
                          });
                }
                else
                {
                    if (FS.Directory.Exists(imgDirectory))
                        FS.Directory.Delete(imgDirectory, true);
                }

                Console.WriteLine();
            });

        delete = deleteLocal;
        replace = replaceLocal;
    }

    public static void ImportAll(string directoryName)
    {
        
        var images = Database.Query<HelpImageEntity>().GroupToDictionary(a => a.Target);

        Replacements rep = new Replacements();
        bool? deleteAll = null;
        foreach (var ci in GetAllCultures(directoryName))
        {
            var ciEntity = ci.ToCultureInfoEntity();
            var dirCulture = FS.Path.Combine(directoryName, ci.Name);

            SafeConsole.WriteLineColor(ConsoleColor.White, $"{ciEntity.Name} ({ciEntity.EnglishName})");

            AppendixXml.LoadDirectory(FS.Path.Combine(dirCulture, AppendicesDirectory), ciEntity, images, rep, ref deleteAll);
            NamespaceXml.LoadDirectory(FS.Path.Combine(dirCulture, NamespacesDirectory), ciEntity, images, rep, ref deleteAll);
            EntityXml.LoadDirectory(FS.Path.Combine(dirCulture, TypesDirectory), ciEntity, images, rep, ref deleteAll);
            QueryXml.LoadDirectory(FS.Path.Combine(dirCulture, QueriesDirectory), ciEntity, images, rep, ref deleteAll);

            Console.WriteLine();
        }
    }

    public static void Export(List<IHelpEntity> entities, string directoryName)
    {
        if (entities == null || entities.Count == 0)
            throw new InvalidOperationException("No entities to export");

        var groupedByCulture = entities
            .GroupBy(e => e.Culture)
            .ToDictionary(g => g.Key, g => g.ToList());

        bool? replace = null;
        bool? delete = null;

        var imageGroups = Database.Query<HelpImageEntity>()
                            .Where(hi => entities.Any(t => hi.Target.Is(t)))
                            .GroupToDictionary(a => a.Target);

        foreach (var (cultureEntity, list) in groupedByCulture)
        {
            var ci = cultureEntity.ToCultureInfo();
            var cultureDir = FS.Path.Combine(directoryName, ci.Name);

            var typeHelps = list.OfType<TypeHelpEntity>().ToList();
            if (typeHelps.Any())
            {
                ExportFolder(ref replace, ref delete, FS.Path.Combine(cultureDir, TypesDirectory),
                    typeHelps,
                    th => $"{RemoveInvalid(th.Type.CleanName)}.help",
                    th => EntityXml.ToXDocument(th),
                    th => imageGroups.TryGetC(th.ToLite()));
            }

            var namespaceHelps = list.OfType<NamespaceHelpEntity>().ToList();
            if (namespaceHelps.Any())
            {
                ExportFolder(ref replace, ref delete, FS.Path.Combine(cultureDir, NamespacesDirectory),
                    namespaceHelps,
                    nh => $"{RemoveInvalid(nh.Name)}.help",
                    nh => NamespaceXml.ToXDocument(nh),
                    nh => imageGroups.TryGetC(nh.ToLite()));
            }

            var appendixHelps = list.OfType<AppendixHelpEntity>().ToList();
            if (appendixHelps.Any())
            {
                ExportFolder(ref replace, ref delete, FS.Path.Combine(cultureDir, AppendicesDirectory),
                    appendixHelps,
                    ah => $"{RemoveInvalid(ah.UniqueName)}.help",
                    ah => AppendixXml.ToXDocument(ah),
                    ah => imageGroups.TryGetC(ah.ToLite()));
            }

            var queryHelps = list.OfType<QueryHelpEntity>().ToList();
            if (queryHelps.Any())
            {
                ExportFolder(ref replace, ref delete, FS.Path.Combine(cultureDir, QueriesDirectory),
                    queryHelps,
                    qh => $"{RemoveInvalid(qh.Query.Key)}.help",
                    qh => QueryXml.ToXDocument(qh),
                    qh => imageGroups.TryGetC(qh.ToLite()));
            }
        }
    }

    public static byte[] ExportToZipBytes(List<IHelpEntity> entities)
    {
        using var zip = new ZipBuilderScope("Help");
        Export(entities, "");
        var bytes = zip.GetAllBytes();
        return bytes;
    }

    public static byte[] ExportAllToZipBytes()
    {
        using var zip = new ZipBuilderScope("Help");
        ExportAll("");
        var bytes = zip.GetAllBytes();
        return bytes;
    }

    public static void ExportAllToZipFile(string filePath)
    {
        var bytes = ExportAllToZipBytes();
        FS.File.WriteAllBytes(filePath, bytes);//uses Real FileSystem
    }


    public static void ImportExportHelp()
    {
        ImportExportHelp(@"..\..\..\Help");
    }

    public static void ImportExportHelp(string directoryName)
    {
    retry:
        Console.WriteLine("You want to export (e) or import (i) Help? (nothing to exit)");

        switch (Console.ReadLine()!.ToLower())
        {
            case "": return;
            case "e": ExportAll(directoryName); break;
            case "ez": ExportAllToZipFile(@"..\..\..\Help.zip"); break;
            case "i": ImportAll(directoryName); break;
            default:
                goto retry;
        }
    }
}

public enum ImportAction
{
    Skipped,
    NoChanges,
    Updated,
    Inserted,
}
