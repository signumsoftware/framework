using System.IO;
using Signum.Utilities.Reflection;

namespace Signum.Files;

public static class FilePathLogic
{

    public static Func<IFilePath?, int> MaxAge = (fp) => 30 * 24 * 60 * 60; //used to set HTTP Cache-Control max-age, one month as default

    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => FilePathLogic.Start(null!)));
    }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        FileTypeLogic.Start(sb);

        sb.Include<FilePathEntity>()
            .WithUniqueIndex(f => new { f.Suffix, f.FileType })
            .WithQuery(() => p => new
            {
                Entity = p,
                p.Id,
                p.FileName,
                p.FileType,
                p.Suffix
            });

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
                            fp.FileType.GetAlgorithm().MoveFile(ofp, fp,true);
                            tr.Commit();
                        }
                    }
                }
            }
        }.Register();

    }

    static readonly Variable<bool> avoidDeleteFiles = Statics.ThreadVariable<bool>("filePathUnsafeMode");

    public static IDisposable? AvoidDeleteFiles()
    {
        if (avoidDeleteFiles.Value) return null;
        avoidDeleteFiles.Value = true;
        return new Disposable(() => avoidDeleteFiles.Value = false);
    }


    public static IDisposable? FilePathLogic_PreUnsafeDelete(IQueryable<FilePathEntity> query)
    {
        if (!avoidDeleteFiles.Value)
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

    public static void FilePath_PreSaving(FilePathEntity fp, PreSavingContext ctx)
    {
        if (fp.Suffix == null && fp.BinaryFile != null)
        {
            var alg = fp.FileType.GetAlgorithm();
            alg.ValidateFile(fp);

            if (SyncFileSave)
                alg.SaveFile(fp);
            else
            {
                var task = alg.SaveFileAsync(fp);

                Transaction.PreRealCommit += data =>
                {
                    //https://medium.com/rubrikkgroup/understanding-async-avoiding-deadlocks-e41f8f2c6f5d
                    var a = fp; //For debugging
                    task.Wait();
                    fp.CleanBinaryFile();
                };
            }
        }
    }

    public static bool SyncFileSave = false;

    public static byte[] GetByteArray(this FilePathEntity fp)
    {
        return fp.BinaryFile ?? fp.FileType.GetAlgorithm().ReadAllBytes(fp);
    }


    public static string? GetAsString(this FilePathEntity fp)
    {
        return  fp.FileType.GetAlgorithm().ReadAsStringUTF8(fp);
    }


    public static Stream OpenRead(this FilePathEntity fp)
    {
        return fp.FileType.GetAlgorithm().OpenRead(fp);
    }
}
