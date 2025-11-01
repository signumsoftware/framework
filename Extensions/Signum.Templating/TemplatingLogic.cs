using Signum.API;
using Signum.Eval.TypeHelp;

namespace Signum.Templating;


public static class TemplatingLogic
{
    public static Dictionary<ModelConverterSymbol, Func<ModifiableEntity, ModifiableEntity>> Converters = new Dictionary<ModelConverterSymbol, Func<ModifiableEntity, ModifiableEntity>>();

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        SymbolLogic<ModelConverterSymbol>.Start(sb, () => Converters.Keys);

        TypeHelpLogic.Start(sb);
    }

    public static void Register<F, T>(ModelConverterSymbol modelConverter, Func<F, T> converterFunction)
        where F : ModifiableEntity
        where T : ModifiableEntity
    {
        Converters[modelConverter] = mod => converterFunction((F)mod);
    }

    public static ModifiableEntity Convert(this ModelConverterSymbol converterSymbol, ModifiableEntity entity)
    {
        return Converters.GetOrThrow(converterSymbol)(entity);
    }
}
