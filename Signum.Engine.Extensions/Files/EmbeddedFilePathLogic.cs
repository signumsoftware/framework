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

namespace Signum.Engine.Files
{
    public static class EmbeddedFilePathLogic
    {
        static Expression<Func<EmbeddedFilePathEntity, FileTypeSymbol, WebImage>> WebImageExpression =
            (efp, ft) => efp == null ? null : new WebImage { FullWebPath = Path.Combine(FileTypeLogic.FileTypes[ft].GetPrefixPair(efp).WebPrefix, efp.Suffix) };
        [ExpressionField]
        public static WebImage WebImage(this EmbeddedFilePathEntity efp, FileTypeSymbol fileType)
        {
            return WebImageExpression.Evaluate(efp, fileType);
        }

        static Expression<Func<EmbeddedFilePathEntity, FileTypeSymbol, WebDownload>> WebDownloadExpression =
           (efp, ft) => efp == null ? null : new WebDownload
           {
               FullWebPath = Path.Combine(FileTypeLogic.FileTypes[ft].GetPrefixPair(efp).WebPrefix, efp.Suffix),
               FileName = efp.FileName
           };
        [ExpressionField]
        public static WebDownload WebDownload(this EmbeddedFilePathEntity fp, FileTypeSymbol fileType)
        {
            return WebDownloadExpression.Evaluate(fp, fileType);
        }


        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => EmbeddedFilePathLogic.Start(null, null)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                FileTypeLogic.Start(sb, dqm);

                EmbeddedFilePathEntity.OnPreSaving += efp =>
                {
                    if(efp.BinaryFile != null) //First time
                    {
                        FileTypeLogic.SaveFile(efp);
                    }
                };

                EmbeddedFilePathEntity.OnPostRetrieved += efp =>
                {
                    efp.SetPrefixPair();
                };
            }
        }

        public static EmbeddedFilePathEntity SetPrefixPair(this EmbeddedFilePathEntity efp)
        {
            using (new EntityCache(EntityCacheType.ForceNew))
                efp.prefixPair = FileTypeLogic.FileTypes.GetOrThrow(efp.FileType).GetPrefixPair(efp);

            return efp;
        }

        public static byte[] GetByteArray(this EmbeddedFilePathEntity efp)
        {
            return efp.BinaryFile ?? File.ReadAllBytes(efp.FullPhysicalPath);
        }

        public static EmbeddedFilePathEntity SaveFile(this EmbeddedFilePathEntity efp)
        {
            FileTypeLogic.SaveFile(efp);
            efp.BinaryFile = null;
            return efp;
        }

        
    }
}
