using Signum.Entities;
using Signum.Utilities;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        static readonly ThreadVariable<ImmutableStack<(PropertyRoute pr, ModifiableEntity? mod, PrimaryKey? rowId)>?> currentPropertyRoute = Statics.ThreadVariable<ImmutableStack<(PropertyRoute pr, ModifiableEntity? mod, PrimaryKey ? rowId) >?>("jsonPropertyRoute");

        public static (PropertyRoute pr, ModifiableEntity? mod, PrimaryKey? rowId)? CurrentPropertyRouteAndEntity
        {
            get { return currentPropertyRoute.Value?.Peek(); }
        }

        public static IRootEntity? FindCurrentRootEntity()
        {
            return currentPropertyRoute.Value?.FirstOrDefault(a => a.mod is IRootEntity).mod as IRootEntity;
        }

        public static PrimaryKey? FindCurrentRowId()
        {
            return currentPropertyRoute.Value?.Where(a => a.rowId != null).FirstOrDefault().rowId;
        }

        public static IDisposable SetCurrentPropertyRouteAndEntity((PropertyRoute, ModifiableEntity?, PrimaryKey? rowId) pair)
        {
            var old = currentPropertyRoute.Value;

            currentPropertyRoute.Value = (old ?? ImmutableStack<(PropertyRoute pr, ModifiableEntity? mod, PrimaryKey? rowId)>.Empty).Push(pair);

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
