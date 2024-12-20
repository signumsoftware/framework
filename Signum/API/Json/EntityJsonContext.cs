using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.API.Json;

public static class EntityJsonContext
{
    public static JsonSerializerOptions FullJsonSerializerOptions;
    static EntityJsonContext()
    {
        var ejcf = new EntityJsonConverterFactory();

        FullJsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IncludeFields = true,
            Converters =
            {
                ejcf,
                new LiteJsonConverterFactory(),
                new MListJsonConverterFactory((pr, root, metadata)=> ejcf.AssertCanWrite(pr, root as ModifiableEntity, metadata)),
                new JsonStringEnumConverter(),
                new ResultTableConverter(),
                new TimeSpanConverter(),
                new DateOnlyConverter(),
                new TimeOnlyConverter()
            }
        };
    }

    static readonly AsyncThreadVariable<SerializationPath?> currentSerializationPath = 
        Statics.ThreadVariable<SerializationPath?>("jsonPropertyRoute");

    public static SerializationPath? CurrentSerializationPath => currentSerializationPath.Value;

    public static IDisposable AddSerializationStep(SerializationStep step)
    {
        var current = currentSerializationPath.Value;

        if(current != null)
        {
            current.Push(step);
            return new Disposable(()=> { current.Pop(); });
        }
        else
        {
            currentSerializationPath.Value = current = new SerializationPath();
            current.Push(step);
            return new Disposable(() => { currentSerializationPath.Value = null; });
        }
    }

    static readonly AsyncThreadVariable<bool> allowDirectMListChangesVariable = Statics.ThreadVariable<bool>("allowDirectMListChanges");

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

public class SerializationPath : Stack<SerializationStep>
{
    public PropertyRoute? CurrentPropertyRoute()
    {
        foreach (var item in this)
        {
            if (item.Route != null)
                return item.Route;
        }
        return null;
    }

    public IRootEntity? CurrentRootEntity()
    {
        foreach (var item in this)
        {
            if (item.RootEntity != null)
                return item.RootEntity;
        }
        return null;
    }

    public PrimaryKey? CurrentRowId()
    {
        foreach (var item in this)
        {
            if (item.RowId != null)
                return item.RowId;
        }
        return null;
    }

    public SerializationMetadata? CurrentSerializationMetadata()
    {
        foreach (var item in this)
        {
            if (item.SerializationMetadata != null)
                return item.SerializationMetadata;
        }
        return null;
    }
}

public readonly struct SerializationStep
{
    public readonly PropertyRoute Route;
    public readonly IRootEntity? RootEntity;
    public readonly PrimaryKey? RowId;
    public readonly SerializationMetadata? SerializationMetadata;

    public SerializationStep(PropertyRoute route, PrimaryKey? rowId = null)
    {
        this.Route = route;
        this.RowId = rowId; 
    }
    public SerializationStep(PropertyRoute route, IRootEntity rootEntity, SerializationMetadata? serializationMetadata)
    {
        Route = route;
        RootEntity = rootEntity;
        SerializationMetadata = serializationMetadata;
    }

    public override string ToString()
    {
        return ", ".Combine(
            Route.ToString(),
            RootEntity == null ? null : "RootEntity = " + RootEntity,
            RowId == null ? null : "RowId = " + RowId,
            SerializationMetadata == null ? null : "SerializationMetadata = " + SerializationMetadata.ToString()
            );
    }
}
