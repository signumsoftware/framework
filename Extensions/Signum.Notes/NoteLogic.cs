using Signum.Authorization.Rules;
using Signum.Authorization;

namespace Signum.Notes;

public static class NoteLogic
{
    [AutoExpressionField]
    public static IQueryable<NoteEntity> Notes(this Entity ident) => 
        As.Expression(() => Database.Query<NoteEntity>().Where(n => n.Target.Is(ident)));

    static HashSet<NoteTypeSymbol> SystemNoteTypes = new HashSet<NoteTypeSymbol>();
    static bool started = false;

    public static void Start(SchemaBuilder sb, params Type[] registerExpressionsFor)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<NoteEntity>()
            .WithSave(NoteOperation.Save)
            .WithQuery(() => n => new
            {
                Entity = n,
                n.Id,
                n.CreatedBy,
                n.CreationDate,
                n.Title,
                Text = n.Text.Etc(100),
                n.Target
            });

        new Graph<NoteEntity>.ConstructFrom<Entity>(NoteOperation.CreateNoteFromEntity)
        {
            Construct = (a, _) => new NoteEntity{ CreationDate = Clock.Now, Target = a.ToLite() }
        }.Register();

        sb.Include<NoteTypeSymbol>()
            .WithSave(NoteTypeOperation.Save)
            .WithQuery(() => t => new
            {
                Entity = t,
                t.Id,
                t.Name,
                t.Key,
            });

        SemiSymbolLogic<NoteTypeSymbol>.Start(sb, () => SystemNoteTypes);

        if (registerExpressionsFor != null)
        {
            var exp = Signum.Utilities.ExpressionTrees.Linq.Expr((Entity ident) => ident.Notes());
            foreach (var type in registerExpressionsFor)
                QueryLogic.Expressions.Register(new ExtensionInfo(type, exp, exp.Body.Type, "Notes", () => typeof(NoteEntity).NicePluralName()));
        }

        started = true;
    }

    public static void RegisterNoteType(NoteTypeSymbol noteType)
    {
        if (!noteType.Key.HasText())
            throw new InvalidOperationException("noteType must have a key, use MakeSymbol method after the constructor when declaring it");

        SystemNoteTypes.Add(noteType);
    }


    public static NoteEntity? CreateNote<T>(this Lite<T> entity, string text, NoteTypeSymbol noteType, Lite<UserEntity>? user = null, string? title = null) where T : class, IEntity
    {
        if (started == false)
            return null;

        return new NoteEntity
        {               
            CreatedBy = user ?? UserEntity.Current,
            Text = text,
            Title = title,
            Target = (Lite<Entity>)entity,
            NoteType = noteType
        }.Execute(NoteOperation.Save);
    }

    public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
    {
        sb.Schema.Settings.AssertImplementedBy((NoteEntity uq) => uq.CreatedBy, typeof(UserEntity));

        TypeConditionLogic.RegisterCompile<NoteEntity>(typeCondition,
            uq => uq.CreatedBy.Is(UserEntity.Current));
    }
}
