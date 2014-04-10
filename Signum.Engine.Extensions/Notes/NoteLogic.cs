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
using Signum.Engine.Extensions.Basics;

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

        static HashSet<Enum> SystemNoteTypes = new HashSet<Enum>();
        static bool started = false;

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

                new Graph<NoteDN>.ConstructFrom<IdentifiableEntity>(NoteOperation.CreateNoteFromEntity)
                {
                    Construct = (a, _) => new NoteDN{ CreationDate = TimeZoneManager.Now, Target = a.ToLite() }
                }.Register();

                new Graph<NoteDN>.Execute(NoteOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (a, _) => { }
                }.Register();

                dqm.RegisterQuery(typeof(NoteTypeDN), () =>
                    from t in Database.Query<NoteTypeDN>()
                    select new
                    {
                        Entity = t,
                        t.Id,
                        t.Name,
                        t.Key,
                    });

                SymbolLogic<NoteTypeDN>.Start(sb, () => SystemNoteTypes);

                new Graph<NoteTypeDN>.Execute(NoteTypeOperation.Save)
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

                started = true;
            }
        }

        public static void RegisterNoteType(Enum noteType)
        {
            SystemNoteTypes.Add(noteType);
        }


        public static NoteDN CreateNote<T>(this Lite<T> entity, string text, NoteTypeDN noteType,  Lite<UserDN> user = null, string title = null) where T : class, IIdentifiable
        {
            if (started == false)
                return null;

            return new NoteDN
            {               
                CreatedBy = user ?? UserDN.Current.ToLite(),
                Text = text,
                Title = title,
                Target = (Lite<IdentifiableEntity>)Lite.Create(entity.EntityType, entity.Id, entity.ToString()),
                NoteType = noteType
            }.Execute(NoteOperation.Save);
        }
    }
}
