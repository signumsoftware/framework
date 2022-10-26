using Signum.Entities.Basics;
using Signum.Entities.Files;
using Signum.Utilities.Reflection;
using System.Collections;
using System.IO;

namespace Signum.Engine.Files;

public static class FilePathEmbeddedLogic
{
    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => FilePathEmbeddedLogic.Start(null!)));
    }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            FileTypeLogic.Start(sb);

            FilePathEmbedded.CloneFunc = fp => new FilePathEmbedded(fp.FileType, fp.FileName, fp.GetByteArray());

            FilePathEmbedded.OnPreSaving += fpe =>
            {
                if (fpe.BinaryFile != null) //First time
                {
                    if (SyncFileSave)
                        fpe.SaveFile();
                    else
                    {
                        var task = fpe.SaveFileAsync();
                        Transaction.PreRealCommit += data =>
                        {
                            //https://medium.com/rubrikkgroup/understanding-async-avoiding-deadlocks-e41f8f2c6f5d
                            var a = fpe; //For debugging

                            task.Wait();
                        };
                    }
                }
            };

            FilePathEmbedded.CalculatePrefixPair += CalculatePrefixPair;

            sb.Schema.SchemaCompleted += Schema_SchemaCompleted;
        }
    }


    public static bool SyncFileSave = false;

    public static FilePathEmbedded ToFilePathEmbedded(this FileContent fileContent, FileTypeSymbol fileType)
    {
        return new FilePathEmbedded(fileType, fileContent.FileName, fileContent.Bytes);
    }

    private static void Schema_SchemaCompleted()
    {
        foreach (var table in Schema.Current.Tables.Values)
        {
            var fields = table.FindFields(f => f.FieldType == typeof(FilePathEmbedded)).ToList();

            if (fields.Any())
            {
                foreach (var field in fields)
                {
                    giAddBinding.GetInvoker(table.Type)(field.Route);
                }
            }

            giOnSaved.GetInvoker(table.Type)(fields.Select(a => a.Route).ToList());
        }
    }

    static GenericInvoker<Action<List<PropertyRoute>>> giOnSaved = new(prs => OnSaved<Entity>(prs));
    static void OnSaved<T>(List<PropertyRoute> array)
        where T : Entity
    {
        var updaters = array.Select(pr => GetUpdater<T>(pr)).ToList();


        Schema.Current.EntityEvents<T>().Saved += (e, args) =>
        {
            foreach (var update in updaters)
            {
                update(e);
            }
        };
    }

    static Action<T> GetUpdater<T>(PropertyRoute route)
        where T : Entity
    {
        string propertyPath = route.PropertyString();
        string rootType = TypeLogic.GetCleanName(route.RootType);

        var mlistRoute = route.GetMListItemsRoute();
        if (mlistRoute == null)
        {
            var exp = route.GetLambdaExpression<T, FilePathEmbedded?>(true);
            var func = exp.Compile();

            return (e) =>
            {
                var fpe = func(e);
                if (fpe != null)
                {
                    fpe.EntityId = e.Id;
                    fpe.MListRowId = null;
                    fpe.PropertyRoute = route.PropertyString();
                    fpe.RootType = rootType;
                }
            };
        }
        else
        {
            var mlistExpr = mlistRoute.Parent!.GetLambdaExpression<T, IMListPrivate>(true);
            var mlistFunc = mlistExpr.Compile();

            var fileExpr = route.GetLambdaExpression<ModifiableEntity, FilePathEmbedded>(true, mlistRoute);
            var fileFunc = fileExpr.Compile();

            return (e) =>
            {
                var mlist = mlistFunc(e);
                if (mlist != null)
                {
                    var list = (IList)mlist;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var mod = (ModifiableEntity)list[i]!;

                        var fpe = fileFunc(mod);
                        if (fpe != null)
                        {
                            fpe.EntityId = e.Id;
                            fpe.MListRowId = mlist.GetRowId(i);
                            fpe.PropertyRoute = route.PropertyString();
                            fpe.RootType = rootType;
                        }
                    }
                }
            };
        }
    }



    static GenericInvoker<Action<PropertyRoute>> giAddBinding = new(pr => AddBinding<Entity>(pr));
    static void AddBinding<T>(PropertyRoute route)
        where T : Entity
    {
        var entityEvents = Schema.Current.EntityEvents<T>();

        entityEvents.RegisterBinding<PrimaryKey>(route.Add(nameof(FilePathEmbedded.EntityId)),
            () => true,
            (t, rowId) => t.Id,
            (t, rowId, retriever) => t.Id);

        entityEvents.RegisterBinding<PrimaryKey?>(route.Add(nameof(FilePathEmbedded.MListRowId)),
            () => true,
            (t, rowId) => rowId,
            (t, rowId, retriever) => rowId);

        var routeType = TypeLogic.GetCleanName(route.RootType);
        entityEvents.RegisterBinding<string>(route.Add(nameof(FilePathEmbedded.RootType)),
            () => true,
            (t, rowId) => routeType,
            (t, rowId, retriever) => routeType);

        var propertyRoute = route.PropertyString();
        entityEvents.RegisterBinding<string>(route.Add(nameof(FilePathEmbedded.PropertyRoute)),
            () => true,
            (t, rowId) => propertyRoute,
            (t, rowId, retriever) => propertyRoute);
    }

    static PrefixPair CalculatePrefixPair(this FilePathEmbedded fpe)
    {
        using (new EntityCache(EntityCacheType.ForceNew))
            return fpe.FileType.GetAlgorithm().GetPrefixPair(fpe);
    }

    public static byte[] GetByteArray(this FilePathEmbedded fpe)
    {
        return fpe.BinaryFile ?? fpe.FileType.GetAlgorithm().ReadAllBytes(fpe);
    }

    public static FileContent ToFileContent(this FilePathEmbedded fpe)
    {
        return new FileContent(fpe.FileName, fpe.GetByteArray());
    }

    public static Stream OpenRead(this FilePathEmbedded fpe)
    {
        return fpe.FileType.GetAlgorithm().OpenRead(fpe);
    }

    public static FilePathEmbedded SaveFile(this FilePathEmbedded fpe)
    {
        var alg = fpe.FileType.GetAlgorithm();
        alg.ValidateFile(fpe);
        alg.SaveFile(fpe);
        return fpe;
    }


    public static Task SaveFileAsync(this FilePathEmbedded fpe)
    {
        var alg = fpe.FileType.GetAlgorithm();
        alg.ValidateFile(fpe);
        return alg.SaveFileAsync(fpe);
    }

    public static void DeleteFileOnCommit(this FilePathEmbedded fpe)
    {
        Transaction.PostRealCommit += dic =>
        {
            fpe.FileType.GetAlgorithm().DeleteFiles(new List<IFilePath> { fpe });
        };
    }
}
