using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.API.Json;

//https://github.com/dotnet/runtime/issues/53539
public class TimeOnlyConverter : JsonConverter<TimeOnly>
{
    private const string TimeFormat = "HH:mm:ss.FFFFFFF";

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = (string)reader.GetString()!;
        return TimeOnly.ParseExact(str, TimeFormat, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        var str = value.ToString(TimeFormat, CultureInfo.InvariantCulture);
        writer.WriteStringValue(str);
    }
}
