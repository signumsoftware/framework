using System.Text.Json;

namespace Signum.Chatbot;

public interface IChatbotToolFactory
{
    List<IChatbotTool> GetTools();
}

public interface IChatbotTool
{
    string Name { get; set; }

    List<object> DescribeTools();

    Task<string> DoExecuteAsync(JsonDocument arguments, CancellationToken token);

}


public class RemoteMCPServer : IChatbotToolFactory
{
    public List<IChatbotTool> GetTools()
    {
        throw new NotImplementedException();
    }
}

public class LocalMCPServer : IChatbotToolFactory
{
    public List<IChatbotTool> GetTools()
    {
        throw new NotImplementedException();
    }
}

public class InternalMCPServer : IChatbotToolFactory
{
    public List<IChatbotTool> GetTools()
    {
        throw new NotImplementedException();
    }

}

public class ModelTool : IChatbotToolFactory
{
    public List<IChatbotTool> GetTools()
    {
        throw new NotImplementedException();
    }

}


public static class JSonSchema
{
    public static object For(Type type) => ForInternal(type, new HashSet<Type>());
    public static object ForInternal(Type type, HashSet<Type> visited)
    {
        if (type == typeof(string))
            return new { type = "string" };

        if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            return new { type = "integer" };

        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return new { type = "number" };

        if (type == typeof(bool))
            return new { type = "boolean" };

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return new { type = "string", format = "date-time" };

        if (type.IsEnum)
            return new
            {
                type = "string",
                enumValues = Enum.GetNames(type)
            };

        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            var elementType = type.IsArray
                ? type.GetElementType()!
                : type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

            return new
            {
                type = "array",
                items = ForInternal(elementType, visited)
            };
        }

        // Avoid infinite recursion
        if (visited.Contains(type))
            return new { type = "object" };

        visited.Add(type);

        // Default: treat as object with properties
        var props = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(
                p => p.Name,
                p => ForInternal(p.PropertyType, visited)
            );

        return new
        {
            type = "object",
            properties = props
        };
    }
}
