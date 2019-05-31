using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Templating;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Signum.Engine.Templating
{

    public static class TemplatingLogic
    {
        public static Dictionary<ModelConverterSymbol, Func<ModifiableEntity, ModifiableEntity>> Converters = new Dictionary<ModelConverterSymbol, Func<ModifiableEntity, ModifiableEntity>>();

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                SymbolLogic<ModelConverterSymbol>.Start(sb, () => Converters.Keys);
            }
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
}
