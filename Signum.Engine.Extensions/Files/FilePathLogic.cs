using Signum.Entities.Files;
using System.IO;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Files;

public static class FilePathLogic
{
    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => FilePathLogic.Start(null!)));
    }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            FileTypeLogic.Start(sb);

            sb.Include<FilePathEntity>()
                .WithQuery(() => p => new
                {
                    Entity = p,
                    p.Id,
                    p.FileName,
                    p.FileType,
                    p.Suffix
                });

            FilePathEntity.CalculatePrefixPair = CalculatePrefixPair;
            sb.Schema.EntityEvents<FilePathEntity>().PreSaving += FilePath_PreSaving;
            sb.Schema.EntityEvents<FilePathEntity>().PreUnsafeDelete += new PreUnsafeDeleteHandler<FilePathEntity>(FilePathLogic_PreUnsafeDelete);

            new Graph<FilePathEntity>.Execute(FilePathOperation.Save)
            {
                CanBeNew = true,
                CanBeModified = true,
                Execute = (fp, _) =>
                {
                    if (!fp.IsNew)
                    {
                        var ofp = fp.ToLite().RetrieveAndRemember();

                        if (fp.FileName != ofp.FileName || fp.Suffix != ofp.Suffix)
                        {
                            using (var tr = new Transaction())
                            {
                                var preSufix = ofp.Suffix.Substring(0, ofp.Suffix.Length - ofp.FileName.Length);
                                fp.Suffix = Path.Combine(preSufix, fp.FileName);
                                fp.Save();
                                fp.FileType.GetAlgorithm().MoveFile(ofp, fp);
                                tr.Commit();
                            }
                        }
                    }
                }
            }.Register();

            sb.AddUniqueIndex<FilePathEntity>(f => new { f.Suffix, f.FileType }); //With mixins, add AttachToUniqueIndexes to field
        }
    }

    static PrefixPair CalculatePrefixPair(FilePathEntity fp)
    {
        using (new EntityCache(EntityCacheType.ForceNew))
           return fp.FileType.GetAlgorithm().GetPrefixPair(fp);
    }

    public static IDisposable? FilePathLogic_PreUnsafeDelete(IQueryable<FilePathEntity> query)
    {
        if (!unsafeMode.Value)
        {
            var list = query.ToList();

            Transaction.PostRealCommit += ud =>
            {
                foreach (var gr in list.GroupBy(f => f.FileType))
                {
                    var alg = gr.Key.GetAlgorithm();
                    alg.DeleteFiles(gr.ToList());
                }
            };
        }

        return null;
    }

    static readonly Variable<bool> unsafeMode = Statics.ThreadVariable<bool>("filePathUnsafeMode");

    public static IDisposable? UnsafeMode()
    {
        if (unsafeMode.Value) return null;
        unsafeMode.Value = true;
        return new Disposable(() => unsafeMode.Value = false);
    }

    public static void FilePath_PreSaving(FilePathEntity fp, PreSavingContext ctx)
    {
        if (fp.IsNew && !unsafeMode.Value)
        {
            var alg = fp.FileType.GetAlgorithm();
            alg.ValidateFile(fp);
            var task = alg.SaveFileAsync(fp);

            Transaction.PreRealCommit += data =>
            {
                //https://medium.com/rubrikkgroup/understanding-async-avoiding-deadlocks-e41f8f2c6f5d
                var a = fp; //For ebuggin
                if (!AvoidWaitingForFileSave)
                    task.Wait();
            };
        }
    }

    public static bool AvoidWaitingForFileSave = false; //Alejandro xUnit deadlock

    public static byte[] GetByteArray(this FilePathEntity fp)
    {
        return fp.BinaryFile ?? fp.FileType.GetAlgorithm().ReadAllBytes(fp);
    }

    public static Stream OpenRead(this FilePathEntity fp)
    {
        return fp.FileType.GetAlgorithm().OpenRead(fp);
    }
}
