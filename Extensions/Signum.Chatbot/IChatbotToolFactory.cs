using Signum.Utilities.Reflection;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Signum.Chatbot;

public interface IChatbotTool
{
    public string Name { get; }
    public string? Description { get; }

    JsonObject ParametersSchema(ChatbotProviderSymbol formatFor);
    Task<string> ExecuteTool(string argumets, CancellationToken ct);
}

public class ChatbotTool : IChatbotTool
{
    public string Name { get; set; }
    public string? Description { get; set; }
    private Delegate Function;

    public ChatbotTool(string name, string description, Delegate function)
    {
        if(function.Method.ReturnType != typeof(string) && 
            function.Method.ReturnType != typeof(Task<string>))
            throw new ArgumentException($"Function {name} must return a string or Task<string>", nameof(function));

        this.Name = name;
        this.Description = description;
        this.Function = function;
    }
    public JsonObject ParametersSchema(ChatbotProviderSymbol formatFor)
    {
        return JSonSchema.ForFunction(Function.Method, formatFor);
    }

    public Task<string> ExecuteTool(string argumets, CancellationToken ct)
    {
        var jsonDoc = JsonDocument.Parse(argumets);
        if (jsonDoc.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Arguments must be a JSON object", nameof(argumets));

        var argsDict = jsonDoc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        
        var parameters = Function.Method.GetParameters();
        var args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (!argsDict.TryGetValue(p.Name!, out var jsonElement))
                throw new ArgumentException($"Missing argument {p.Name}", nameof(argumets));

            args[i] = jsonElement.Deserialize(p.ParameterType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        var result = Function.DynamicInvoke(args);
        if (result is Task<string> taskResult)
            return taskResult;
        return Task.FromResult((string)result!);
    }
}

public static class JSonSchema
{
    public static JsonObject ForFunction(MethodInfo method, ChatbotProviderSymbol formatFor)
    {
        var props = method.GetParameters()
          .ToDictionary(p => p.Name!,
              p => (JsonNode?)FotType(p.ParameterType, p.IsNullable())
              .AddIfNotNull("description", p.GetCustomAttribute<DescriptionAttribute>()?.Description)
              );

        var args = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject(props),
            ["required"] = new JsonArray(method.GetParameters().Select(p => JsonValue.Create(p.Name)).ToArray())
        };

        var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
        if(description == null)
            throw new Exception("Description not set for " + method.MethodSignature());

        if (formatFor.Is(ChatbotProviders.Anthropic))
        {
            return new JsonObject
            {
                ["name"] = method.Name,
                ["description"] = description,
                ["input_schema"] = args
            };
        }
        else
        {
            return new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = method.Name,
                    ["description"] = method.GetCustomAttribute<DescriptionAttribute>()?.Description,
                    ["strict"] = true,
                    ["parameters"] = args
                },
            };
        }   
    }

    static JsonObject FotType(Type type, bool? isNullable) => FotType(type, isNullable, new HashSet<Type>());
    static JsonObject FotType(Type type, bool? isNullable, HashSet<Type> visited)
    {
        type = type.UnNullify();

        JsonNode MaybeNull(string type) => isNullable == true ? new JsonArray(type, "null") : type;

        if (type == typeof(string))
            return new JsonObject { ["type"] = MaybeNull("string") };

        if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            return new JsonObject { ["type"] = MaybeNull("integer") };

        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return new JsonObject { ["type"] = MaybeNull("number") };

        if (type == typeof(bool))
            return new JsonObject { ["type"] = MaybeNull("boolean") };

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return new JsonObject
            {
                ["type"] = MaybeNull("string"),
                ["format"] = "date-time"
            };

        if (type == typeof(DateOnly))
            return new JsonObject
            {
                ["type"] = MaybeNull("string"),
                ["format"] = "date"
            };

        if (type == typeof(TimeOnly))
            return new JsonObject
            {
                ["type"] = MaybeNull("string"),
                ["format"] = "time"
            };

        if (type.IsEnum)
            return new JsonObject
            {
                ["type"] = "string",
                ["enumValues"] = new JsonArray(Enum.GetNames(type).Select(n => JsonValue.Create(n)).ToArray())
            };

        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            var elementType = type.IsArray
                ? type.GetElementType()!
                : type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

            return new JsonObject
            {
                ["type"] = "array",
                ["items"] = FotType(elementType, false, visited)
            };
        }

        // Avoid infinite recursion
        if (visited.Contains(type))
            return new JsonObject { ["type"] = "object" };

        visited.Add(type);

        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, p => (JsonNode?)FotType(p.PropertyType, p.IsNullable(), visited).AddIfNotNull("description", p.GetCustomAttribute<DescriptionAttribute>()?.Description));

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] =  new JsonObject(props)
        };
    }

}
