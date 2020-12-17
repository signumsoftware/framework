using Signum.Entities;
using Signum.Utilities;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Signum.Engine.Json
{
    public static class EntityJsonContext
    {
        public static JsonSerializerOptions FullJsonSerializerOptions;
        static EntityJsonContext()
        {
            var ejcf = new EntityJsonConverterFactory();

            FullJsonSerializerOptions = new JsonSerializerOptions
            {
                IncludeFields = true,
                Converters =
                {
                    ejcf,
                    new MListJsonConverterFactory(ejcf.AssertCanWrite),
                    new LiteJsonConverterFactory(),
                    new JsonStringEnumConverter(),
                    new TimeSpanConverter(),
                    new DateConverter()
                }
            };
        }

        static readonly ThreadVariable<(PropertyRoute pr, ModifiableEntity? mod)?> currentPropertyRoute = Statics.ThreadVariable<(PropertyRoute pr, ModifiableEntity? mod)?>("jsonPropertyRoute");

        public static (PropertyRoute pr, ModifiableEntity? mod)? CurrentPropertyRouteAndEntity
        {
            get { return currentPropertyRoute.Value; }
        }

        public static IDisposable SetCurrentPropertyRouteAndEntity((PropertyRoute, ModifiableEntity?)? route)
        {
            var old = currentPropertyRoute.Value;

            currentPropertyRoute.Value = route;

            return new Disposable(() => { currentPropertyRoute.Value = old; });
        }

        static readonly ThreadVariable<bool> allowDirectMListChangesVariable = Statics.ThreadVariable<bool>("allowDirectMListChanges");

        public static bool AllowDirectMListChanges
        {
            get { return allowDirectMListChangesVariable.Value; }
        }

        public static IDisposable SetAllowDirectMListChanges(bool allowMListDirectChanges)
        {
            var old = allowDirectMListChangesVariable.Value;

            allowDirectMListChangesVariable.Value = allowMListDirectChanges;

            return new Disposable(() => { allowDirectMListChangesVariable.Value = old; });
        }

        

    }
}
