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
            }
        }
    }
}
