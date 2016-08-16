using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities.Basics;

namespace Signum.Engine.Basics
{
    public class RequestLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            sb.Include<RequestEntity>().WithQuery(dqm,
                e => new
                {
                    Entity = e,
                    e.Request,
                    e.Values.Count
                });


        }
    }
}
