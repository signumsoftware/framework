using Signum.Engine.Sync.Postgres;
using Signum.Utilities.Reflection;

namespace Signum.Files;

public enum BigStringMode 
{
    /// <summary>
    /// Only Text column in database
    /// </summary>
    Database, 
    /// <summary>
    /// Only File column in database pointing to a path in the file system / blob storage
    /// </summary>
    File, 
    /// <summary>
    /// Migrating from Text -> File
    /// </summary>
    Migrating_FromDatabase_ToFile,
    /// <summary>
    /// Migrating from File -> Text
    /// </summary>
    Migrating_FromFile_ToDatabase,
}

public class BigStringConfiguration
{
    public BigStringMode Mode { get; private set; }

    public FileTypeSymbol? FileTypeSymbol { get; private set; }

    public BigStringConfiguration(BigStringMode mode, FileTypeSymbol? fileTypeSymbol)
    {
        this.Mode = mode;

        if (this.Mode != BigStringMode.Database && fileTypeSymbol == null)
            throw new ArgumentNullException(nameof(fileTypeSymbol));

        this.FileTypeSymbol = fileTypeSymbol;
    }
}

public static class BigStringLogic
{
    public static Dictionary<PropertyRoute, BigStringConfiguration> Configurations = new Dictionary<PropertyRoute, BigStringConfiguration>();

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            MixinDeclarations.AssertDeclared(typeof(BigStringEmbedded), typeof(BigStringMixin));
            sb.Schema.SchemaCompleted += Schema_SchemaCompleted;
            BigStringMixin.PreSavingAction = PreSaving;
            BigStringMixin.PostRetrievingAction = PostRetrieving;
        }
    }

    public static Dictionary<Type, List<PropertyRoute>> CandidatesByDeclaringType = null!;

    public static void PreSaving(BigStringMixin mixin, PreSavingContext ctx)
    {
        var bs = (BigStringEmbedded)mixin.MainEntity;

        PropertyRoute pr = FindPropertyRoute(bs);

        var config = Configurations.GetOrThrow(pr);

        switch (config.Mode)
        {
            case BigStringMode.Database:
                break;
            case BigStringMode.File:
                if(bs.Modified == ModifiedState.SelfModified)
                {
                    mixin.File = string.IsNullOrEmpty(bs.Text) ? null : new FilePathEmbedded(config.FileTypeSymbol!, pr.PropertyInfo!.Name + ".txt", Encoding.UTF8.GetBytes(bs.Text));
                    ctx.InvalidateGraph();
                }
                break;
            case BigStringMode.Migrating_FromDatabase_ToFile:
                if (bs.Modified == ModifiedState.SelfModified || bs.Text.HasText() && mixin.File == null)
                {
                    mixin.File = string.IsNullOrEmpty(bs.Text) ? null : new FilePathEmbedded(config.FileTypeSymbol!, pr.PropertyInfo!.Name + ".txt", Encoding.UTF8.GetBytes(bs.Text));
                    ctx.InvalidateGraph();
                }
                break;
            case BigStringMode.Migrating_FromFile_ToDatabase:
                if (bs.Modified == ModifiedState.SelfModified || string.IsNullOrEmpty(bs.Text) && mixin.File != null)
                {
                    bs.Text = mixin.File == null ? null : Encoding.UTF8.GetString(mixin.File.GetByteArray());
                    ctx.InvalidateGraph();
                    mixin.File?.TryDeleteFileOnCommit(e => { });
                }
                break;
            default:
                break;
        }
    }

   

    public static void PostRetrieving(BigStringMixin mixin, PostRetrievingContext ctx)
    {
        var bs = (BigStringEmbedded)mixin.MainEntity;

        PropertyRoute pr = FindPropertyRoute(bs);

        var config = Configurations.GetOrThrow(pr);

        switch (config.Mode)
        {
            case BigStringMode.Database:
                break;
            case BigStringMode.File:
                bs.Text = mixin.File == null ? null : Encoding.UTF8.GetString(mixin.File.GetByteArray());
                break;
            case BigStringMode.Migrating_FromDatabase_ToFile:
                if (mixin.File != null)
                    bs.Text = Encoding.UTF8.GetString(mixin.File.GetByteArray());
                break;
            case BigStringMode.Migrating_FromFile_ToDatabase:
                if (bs.Text == null && mixin.File != null)
                    bs.Text = Encoding.UTF8.GetString(mixin.File.GetByteArray());
                break;
            default:
                break;
        }
    }

    static PropertyRoute FindPropertyRoute(BigStringEmbedded bs)
    {
        var parent = bs.GetParentEntity<ModifiableEntity>();

        var pr = CandidatesByDeclaringType.GetOrThrow(parent.GetType()).Single(pr => pr.MatchesEntity(bs) == true);
        return pr;
    }

    static PropertyInfo piText = ReflectionTools.GetPropertyInfo((BigStringEmbedded bs) => bs.Text);
    static PropertyInfo piFile = ReflectionTools.GetPropertyInfo((BigStringMixin bs) => bs.File);

    public static void RegisterAll<T>(SchemaBuilder sb, BigStringConfiguration config)
    where T : Entity
    {
        var routes = PropertyRoute.GenerateRoutes(typeof(T)).Where(a => a.PropertyRouteType == PropertyRouteType.FieldOrProperty && a.Type == typeof(BigStringEmbedded)).ToList();

        foreach (var route in routes)
        {
            Register(sb, route, config);
        }
    }

    public static void Register<T>(SchemaBuilder sb, Expression<Func<T, BigStringEmbedded>> expression, BigStringConfiguration config)
        where T : Entity
    {
        Register(sb, PropertyRoute.Construct(expression), config);
    }

    public static void Register(SchemaBuilder sb, PropertyRoute route, BigStringConfiguration config)
    {
        if (sb.Schema.Tables.ContainsKey(route.RootType))
            throw new InvalidOperationException($"{route.RootType} is already included in the Schema. You need to call BigStringLogic.Register earlier in your Starter.Start method.");

        if(route.PropertyInfo!.GetCustomAttribute<BindParentAttribute>() == null)
            throw new InvalidOperationException($"BigString {route} should have a [BindParentAttribute]."); 

        if (config.Mode == BigStringMode.Database)
            sb.Schema.Settings.FieldAttributes(route.Add(typeof(BigStringMixin)).Add(piFile))!.Add(new IgnoreAttribute());
        else
        {
            if (config.Mode == BigStringMode.File)
                sb.Schema.Settings.FieldAttributes(route.Add(piText))!.Add(new IgnoreAttribute());

            if (config.FileTypeSymbol == null)
                throw new InvalidOperationException($"{config.Mode} requires a FileTypeSymbol");

            giRegisterPreUnsafeDelete.GetInvoker(route.RootType)(sb, route);
        }

        Configurations.Add(route, config);
    }

    static readonly GenericInvoker<Action<SchemaBuilder, PropertyRoute>> giRegisterPreUnsafeDelete = 
        new GenericInvoker<Action<SchemaBuilder, PropertyRoute>>((sb, pr) => RegisterPreUnsafeDelete<Entity>(sb, pr));

    static void RegisterPreUnsafeDelete<T>(SchemaBuilder sb, PropertyRoute route)
        where T : Entity
    {
        sb.Schema.EntityEvents<T>().PreUnsafeDelete += query =>
        {
            var lambda = route.GetLambdaExpression<T, BigStringEmbedded>(false, null);

            var files = query.Select(e => lambda.Evaluate(e).Mixin<BigStringMixin>().File).ToList().NotNull().ToList();

            Transaction.PostRealCommit += dic =>
            {
                foreach (var gr in files.GroupBy(a => a.FileType))
                {
                    gr.Key.GetAlgorithm().DeleteFiles(gr.ToList<IFilePath>());
                }
            };

            return null;
        };
    }


    private static void Schema_SchemaCompleted()
    {
        var routes = Schema.Current.Tables.Values.SelectMany(t => t.FindFields(f => f.FieldType == typeof(BigStringEmbedded))).Select(a => a.Route).ToList();

        Schema s = Schema.Current;

        var result = EnumerableExtensions.JoinStrict(
            Configurations.Keys, 
            routes,
            a => a,
            a => a,
            (a, b) => 0);

        Func<PropertyRoute, string> registerExample = pr => "  BigStringLogic.Register(sb, ({0} a) => a.{1}, new BigStringConfiguration(BigStringMode.XXX, YYY));".FormatWith(pr.RootType.TypeName(), pr.PropertyString().Replace("/", "[0]"));

        var extra = result.Extra.OrderBy(a => a.RootType.FullName).ToString(registerExample, "\n");

        var lacking = result.Missing.OrderBy(a => a.RootType.FullName).ToString(registerExample, "\n"); ;

        if (extra.HasText() || lacking.HasText())
            throw new InvalidOperationException("BigStringLogic's configuration are not synchronized with the Schema. At the beginning of your Starter.Start method you need to...\n" +
                    (extra.HasText() ? ("Remove something like:\n" + extra + "\n\n") : null) +
                    (lacking.HasText() ? ("Add something like:\n" + lacking + "\n\n") : null));

        CandidatesByDeclaringType = Configurations.Keys.GroupToDictionary(a => a.PropertyInfo!.DeclaringType!);

    }

    public static void MigrateBigStrings<T>() where T : Entity => MigrateBigStrings(Database.Query<T>());
    public static void MigrateBigStrings<T>(IQueryable<T> query) where T : Entity
    {
        SafeConsole.WriteLineColor(ConsoleColor.Cyan, "Saving {0} to update BigStrings".FormatWith(typeof(T).TypeName()));

        if (!query.Any())
            return;

        query.Select(a => a.Id).IntervalsOf(100).ProgressForeach(inter => inter.ToString(), (interva) =>
        {
            var list = query.Where(a => interva.Contains(a.Id)).ToList();

            list.SaveList();
        });
    }
}
