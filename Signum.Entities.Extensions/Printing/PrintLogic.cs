using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Printing
{
    public static class PrintLogic
    {
        static Expression<Func<PrintPackageEntity, IQueryable<PrintLineEntity>>> LinesExpression =
        e => Database.Query<PrintLineEntity>().Where(a => a.Package.RefersTo(e));
        [ExpressionField]
        public static IQueryable<PrintLineEntity> Lines(this PrintPackageEntity e)
        {
            return LinesExpression.Evaluate(e);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<PrintLineEntity>()
                    .WithQuery(dqm, p => new
                    {
                        Entity = p,
                        p.CreationDate,
                        p.File,
                        p.Package,
                        p.PrintedOn,
                        p.Referred,
                        p.Exception,
                    });


                sb.Include<PrintPackageEntity>()
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name
                    });
            }
        }        
    }

    public class PrintLineGraph : Graph<PrintLineEntity, PrintLineState>
    {
        public static void Register()
        {
            GetState = e => e.State;

            new Execute(PrintLineOperation.Print)
            {
                FromStates = {PrintLineState.ReadyToPrint},
                ToStates = {PrintLineState.Printed},
                
               

            }.Register();


            new Execute(PrintLineOperation.RePrint)
            {

            }.Register();
        }
    }
}

