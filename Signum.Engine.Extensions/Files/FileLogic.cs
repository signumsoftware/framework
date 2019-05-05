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
        static Expression<Func<FileEntity, WebImage>> WebImageExpression =
            fp => fp == null ? null! : new WebImage { FullWebPath = fp.FullWebPath() };
        [ExpressionField]
        public static WebImage? WebImage(this FileEntity fp)
        {
            return WebImageExpression.Evaluate(fp);
        }

        static Expression<Func<FileEntity, WebDownload>> WebDownloadExpression =
           fp => fp == null ? null! : new WebDownload { FullWebPath = fp.FullWebPath(), FileName = fp.FileName };
        [ExpressionField]
        public static WebDownload WebDownload(this FileEntity fp)
        {
            return WebDownloadExpression.Evaluate(fp);
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
