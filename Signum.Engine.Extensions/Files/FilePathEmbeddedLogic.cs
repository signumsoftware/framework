using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities.Files;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Signum.Engine.Files
{
    public static class FilePathEmbeddedLogic
    {
        static Expression<Func<FilePathEmbedded, FileTypeSymbol, WebImage>> WebImageExpression =
            (efp, ft) => efp == null ? null : new WebImage
            {
                FullWebPath = efp.FullWebPath()
            };
        [ExpressionField]
        public static WebImage WebImage(this FilePathEmbedded efp, FileTypeSymbol fileType)
        {
            return WebImageExpression.Evaluate(efp, fileType);
        }

        static Expression<Func<FilePathEmbedded, FileTypeSymbol, WebDownload>> WebDownloadExpression =
           (efp, ft) => efp == null ? null : new WebDownload
           {
               FullWebPath = efp.FullWebPath(),
               FileName = efp.FileName
           };
        [ExpressionField]
        public static WebDownload WebDownload(this FilePathEmbedded fp, FileTypeSymbol fileType)
        {
            return WebDownloadExpression.Evaluate(fp, fileType);
        }
        
        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => FilePathEmbeddedLogic.Start(null)));
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                FileTypeLogic.Start(sb);

                FilePathEmbedded.CloneFunc = fp => new FilePathEmbedded(fp.FileType, fp.FileName, fp.GetByteArray());

                FilePathEmbedded.OnPreSaving += efp =>
                {
                    if(efp.BinaryFile != null) //First time
                    {
                        efp.SaveFile();
                    }
                };

                FilePathEmbedded.CalculatePrefixPair += CalculatePrefixPair;
            }
        }

        static PrefixPair CalculatePrefixPair(this FilePathEmbedded efp)
        {
            using (new EntityCache(EntityCacheType.ForceNew))
                return efp.FileType.GetAlgorithm().GetPrefixPair(efp);
        }

        public static byte[] GetByteArray(this FilePathEmbedded efp)
        {
            return efp.BinaryFile ?? efp.FileType.GetAlgorithm().ReadAllBytes(efp);
        }

        public static Stream OpenRead(this FilePathEmbedded efp)
        {
            return efp.FileType.GetAlgorithm().OpenRead(efp);
        }

        public static FilePathEmbedded SaveFile(this FilePathEmbedded efp)
        {
            var alg = efp.FileType.GetAlgorithm();
            alg.ValidateFile(efp);
            alg.SaveFile(efp);
            efp.BinaryFile = null;
            return efp;
        }

        public static void DeleteFileOnCommit(this FilePathEmbedded efp)
        {
            Transaction.PostRealCommit += dic =>
            {
                efp.FileType.GetAlgorithm().DeleteFiles(new List<IFilePath> { efp });
            };
        }
    }
}
