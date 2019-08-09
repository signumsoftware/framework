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
        [AutoExpressionField]
        public static WebImage? WebImage(this FileEntity fp) => 
            As.Expression(() => fp == null ? null! : new WebImage { FullWebPath = fp.FullWebPath() });

        [AutoExpressionField]
        public static WebDownload WebDownload(this FileEntity fp) => 
            As.Expression(() => fp == null ? null! : new WebDownload { FullWebPath = fp.FullWebPath(), FileName = fp.FileName });

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
