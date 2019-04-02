using System;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Files;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine.Basics;

namespace Signum.Engine.Files
{
    public static class FileLogic
    {
        public static Func<Lite<FileEntity>, string> DownloadFileUrl;

        static Expression<Func<FileEntity, WebImage>> WebImageFileExpression =
            f => new WebImage { FullWebPath = DownloadFileUrl(f.ToLite()) };
#pragma warning disable SF0002 // Use ExpressionFieldAttribute in non-trivial method or property
        [ExpressionField("WebImageFileExpression")]
#pragma warning restore SF0002 // Use ExpressionFieldAttribute in non-trivial method or property
        public static WebImage WebImage(this FileEntity f)
        {
            return new WebImage { FullWebPath = DownloadFileUrl(f?.ToLite()) };
        }

        static Expression<Func<FileEntity, WebDownload>> WebDownloadFileExpression =
           f => new WebDownload { FullWebPath = DownloadFileUrl(f.ToLite()) };
#pragma warning disable SF0002 // Use ExpressionFieldAttribute in non-trivial method or property
        [ExpressionField("WebDownloadFileExpression")]
#pragma warning restore SF0002 // Use ExpressionFieldAttribute in non-trivial method or property
        public static WebDownload WebDownload(this FileEntity f)
        {
            return new WebDownload { FullWebPath = DownloadFileUrl(f?.ToLite()) };
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<FileEntity>()
                    .WithQuery(() => a => new
                    {
                        Entity = a,
                        a.Id,
                        a.FileName,
                    });
                
                QueryLogic.Expressions.Register((FileEntity f) => f.WebImage(), () => typeof(WebImage).NiceName(), "Image");
                QueryLogic.Expressions.Register((FileEntity f) => f.WebDownload(), () => typeof(WebDownload).NiceName(), "Download");
            }
        }
    }
}
