using Signum.Engine.Sync;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.UserAssets.TokenMigrations;

/// <summary>
/// Categorises the kind of rename being recorded. Each bucket maps to a typed dict on
/// <see cref="TokenMigrationFile"/>; the <paramref name="subKey"/> (passed to
/// <see cref="TokenMigrationFile.TryGetDictionary"/> / <see cref="TokenMigrationFile.GetOrCreateDictionary"/>)
/// is bucket-specific:
/// <list type="bullet">
/// <item><b>FilterValue</b> — subKey = "queryKey|tokenString"</item>
/// <item><b>Types</b> — no subKey. Covers both Lite type renames in filter values AND query-key
/// translations (a queryKey is essentially a Type clean name / enum ToString, so the same dict
/// serves both).</item>
/// <item><b>Member</b> — subKey = type FullName</item>
/// <item><b>Global</b> — no subKey (flat, top-level dict)</item>
/// </list>
/// </summary>
public enum RenameBucket
{
    FilterValue,
    Types,
    Member,
    Global,
}

/// <summary>
/// Serialized form of token-rename and per-entity decisions captured during sync. Buckets are
/// dispatched through <see cref="RenameBucket"/> so the same handful of helper methods cover every
/// rename category.
/// </summary>
public class TokenMigrationFile
{
    /// <summary>Query-rooted token renames (first segment of a token path). Outer key = queryKey.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, Dictionary<string, string>>? TokensByQuery { get; set; }

    /// <summary>Type-rooted token renames (later segments inside a Type). Outer key = type FullName.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, Dictionary<string, string>>? TokensByType { get; set; }

    /// <summary>Filter-value renames. Outer key = "queryKey|tokenString"; inner dict = oldValue → newValue.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, Dictionary<string, string>>? FilterValues { get; set; }

    /// <summary>
    /// Type renames — also used as query-key renames since a queryKey is conventionally a Type's
    /// clean name (or an enum ToString()). One dict covers both: Lite type renames inside filter
    /// values, AND query renames flushed from <c>QueryLogic.QueryRenamed</c> into a .query.json.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Types { get; set; }

    /// <summary>CLR member renames used by the template parser. Outer key = type FullName.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, Dictionary<string, string>>? Members { get; set; }

    /// <summary>Global-variable renames in templates. Flat dict = oldKey → newKey.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Globals { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<UserAssetEntityAction>? UserAssetActions { get; set; }

    public bool IsEmpty =>
        TokensByQuery.IsNullOrEmpty()
        && TokensByType.IsNullOrEmpty()
        && FilterValues.IsNullOrEmpty()
        && Types.IsNullOrEmpty()
        && Members.IsNullOrEmpty()
        && Globals.IsNullOrEmpty()
        && UserAssetActions.IsNullOrEmpty();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    public static TokenMigrationFile Load(string filePath)
    {
        var json = File.ReadAllText(filePath, Encoding.UTF8);
        return JsonSerializer.Deserialize<TokenMigrationFile>(json, JsonOptions)
            ?? throw new InvalidOperationException("Empty token migration file: " + filePath);
    }

    public void Save(string filePath)
    {
        if (IsEmpty)
            return;

        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(filePath, json, Encoding.UTF8);
        Console.WriteLine("Json file saved in:  " + filePath);
        SafeConsole.WriteLineColor(ConsoleColor.DarkGray, json);
    }

    /// <summary>
    /// Returns the dict for the given bucket/subKey if present, else null. Used by callers walking
    /// history to chain-compose a working lookup across multiple files.
    /// </summary>
    public Dictionary<string, string>? TryGetDictionary(RenameBucket bucket, string? subKey = null)
    {
        return bucket switch
        {
            RenameBucket.FilterValue => FilterValues != null && FilterValues.TryGetValue(subKey ?? throw SubKeyRequired(bucket), out var d) ? d : null,
            RenameBucket.Types => Types is { Count: > 0 } ? Types : null,
            RenameBucket.Member => Members != null && Members.TryGetValue(subKey ?? throw SubKeyRequired(bucket), out var d) ? d : null,
            RenameBucket.Global => Globals is { Count: > 0 } ? Globals : null,
            _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, null),
        };
    }

    /// <summary>
    /// Returns the dict for the given bucket/subKey, creating it if missing. Used by the recording
    /// side to persist new decisions.
    /// </summary>
    public Dictionary<string, string> GetOrCreateDictionary(RenameBucket bucket, string? subKey = null)
    {
        return bucket switch
        {
            RenameBucket.FilterValue => (FilterValues ??= new()).GetOrCreate(subKey ?? throw SubKeyRequired(bucket)),
            RenameBucket.Types => Types ??= new(),
            RenameBucket.Member => (Members ??= new()).GetOrCreate(subKey ?? throw SubKeyRequired(bucket)),
            RenameBucket.Global => Globals ??= new(),
            _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, null),
        };
    }

    static ArgumentNullException SubKeyRequired(RenameBucket bucket) =>
        new("subKey", $"Bucket '{bucket}' requires a non-null subKey.");

    public static string FilterValueSubKey(string queryKey, string tokenString)
        => queryKey + "|" + tokenString;

    internal void LoadTypes(Replacements rep)
    {
        Types = rep.TryGetC(QueryLogic.QueriesKey);
    }
}

public class UserAssetEntityAction
{
    public string EntityType { get; set; } = null!;
    public Guid Guid { get; set; }
    public UserAssetEntityActionType Action { get; set; }
}

public enum UserAssetEntityActionType
{
    Skip,
    Delete,
    Regenerate,
}
