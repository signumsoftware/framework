using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Files;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Engine.Files
{
    public static class FileLogic
    {
        public static Func<Lite<FileDN>, string> DownloadFileUrl;

        static Expression<Func<FileDN, WebImage>> WebImageFileExpression =
            f => new WebImage { FullWebPath = DownloadFileUrl(f.ToLite()) };
        [ExpressionField("WebImageFileExpression")]
        public static WebImage WebImage(this FileDN f)
        {
            return WebImageFileExpression.Evaluate(f);
        }

        static Expression<Func<FileDN, WebDownload>> WebDownloadFileExpression =
           f => new WebDownload { FullWebPath = DownloadFileUrl(f.ToLite()) };
        [ExpressionField("WebDownloadFileExpression")]
        public static WebDownload WebDownload(this FileDN f)
        {
            return WebDownloadFileExpression.Evaluate(f);
        }
    

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<FileDN>();

                dqm[typeof(FileDN)] = (from a in Database.Query<FileDN>()
                                               select new
                                               {
                                                   Entity = a,
                                                   a.Id,
                                                   a.FileName,
                                               }).ToDynamic();


                dqm.RegisterExpression((FileDN f) => f.WebImage(), () => typeof(WebImage).NiceName(), "Image");
                dqm.RegisterExpression((FileDN f) => f.WebDownload(), () => typeof(WebDownload).NiceName(), "Download");
            }
        }
    }
}
