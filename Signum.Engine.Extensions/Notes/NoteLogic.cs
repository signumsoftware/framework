using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Engine.Authorization;
using Signum.Engine;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Authorization;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Basics;
using Signum.Engine.Operations;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using Signum.Entities.Notes;

namespace Signum.Engine.Notes
{
    public static class NoteLogic
    {
        static Expression<Func<IdentifiableEntity, IQueryable<NoteDN>>> NotesExpression =
            ident => Database.Query<NoteDN>().Where(n => n.Target.RefersTo(ident));
        public static IQueryable<NoteDN> Notes(this IdentifiableEntity ident)
        {
            return NotesExpression.Evaluate(ident);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, Type[] registerExpressionsFor)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<NoteDN>();
                dqm.RegisterQuery(typeof(NoteDN), () =>
                    from n in Database.Query<NoteDN>()
                    select new
                    {
                        Entity = n,
                        n.Id,
                        n.CreatedBy,
                        n.CreationDate,
                        n.Title,
                        Text = n.Text.Etc(100),
                        n.Target
                    });

                new BasicExecute<NoteDN>(NoteOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (a, _) => { }
                }.Register();



                if (registerExpressionsFor != null)
                {
                    var exp = Signum.Utilities.ExpressionTrees.Linq.Expr((IdentifiableEntity ident) => ident.Notes());
                    foreach (var type in registerExpressionsFor)
                        dqm.RegisterExpression(new ExtensionInfo(type, exp, exp.Body.Type, "Notes", () => typeof(NoteDN).NicePluralName()));
                }
            }
        }
    }
}
