
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Signum.Utilities.Reflection;
using Signum.Basics;
using Signum.API.Filters;
using Signum.API;

namespace Signum.Eval.TypeHelp;

[ValidateModelFilter]
public class TypeHelpController : ControllerBase
{

    [HttpPost("api/typeHelp/autocompleteEntityCleanType")]
    public List<string> AutocompleteEntityCleanType([Required, FromBody] AutocompleteEntityCleanTypeRequest request)
    {
        Schema s = Schema.Current;
        var types = TypeLogic.NameToType
            .Where(kvp => s.IsAllowed(kvp.Value, true) == null)
            .Select(kvp => kvp.Key).ToList();

        var result = Filter(types, request.query, request.limit);

        return result;
    }

    public class AutocompleteEntityCleanTypeRequest
    {
        public string query;
        public int limit;
    }

    [HttpPost("api/typeHelp/autocompleteType")]
    public List<string> AutocompleteType([Required, FromBody] AutocompleteTypeRequest request) //Not comprehensive, just useful
    {
        var types = GetTypes(request);

        return Filter(types, request.query, request.limit);
    }

    private List<string> Filter(List<string> types, string query, int limit)
    {
        var result = types
            .Where(a => a.StartsWith(query, StringComparison.InvariantCultureIgnoreCase))
            .OrderBy(a => a.Length)
            .ThenBy(a => a)
            .Take(limit).ToList();

        if (result.Count < limit)
            result.AddRange(types.Where(a => a.Contains(query, StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(a => a.Length)
                .ThenBy(a => a)
                .Take(result.Count - limit).ToList());

        return result;
    }

    public class AutocompleteTypeRequest
    {
        public string query;
        public int limit;
        public bool includeBasicTypes;
        public bool includeEntities;
        public bool includeModelEntities;
        public bool includeEmbeddedEntities;
        public bool includeMList;
        public bool includeQueriable;
    }

    public static List<string> AditionalTypes = new List<string>
    {
        "Date",
        "DateTime",
        "TimeSpan",
        "Guid",
    };

    List<string> GetTypes(AutocompleteTypeRequest request)
    {
        List<string> result = new List<string>();
        if (request.includeBasicTypes)
        {
            result.AddRange(CSharpRenderer.BasicTypeNames.Values);
            result.AddRange(AditionalTypes);
        }

        if (request.includeEntities)
        {
            result.AddRange(TypeLogic.TypeToEntity.Keys.Select(a => a.Name));
        }

        if (request.includeModelEntities)
        {
            result.AddRange(TypeHelpLogic.AvailableModelEntities.Value.Select(a => a.Name));
        }

        if (request.includeEmbeddedEntities)
        {
            result.AddRange(TypeHelpLogic.AvailableEmbeddedEntities.Value.Select(a => a.Name));
        }

        if (request.includeMList)
            return Fix(result, "MList", request.query);

        if (request.includeQueriable)
            return Fix(result, "IQueryable", request.query);

        return result;
    }

    List<string> Fix(List<string> result, string token, string query)
    {
        if (query.StartsWith(token))
            return result.Select(a => token + "<" + a + ">").ToList();
        else
        {
            result.Add(token + "<");
            return result;
        }
    }


    [HttpGet("api/typeHelp/{typeName}/{mode}")]
    public TypeHelpTS? GetTypeHelp(string typeName, TypeHelpMode mode)
    {
        Type? type = TypeLogic.TryGetType(typeName);
        if (type == null)
            return null;

        var isEnum = type.IsEnumEntity();

        var members = new List<TypeMemberHelpTS>();

        if (isEnum)
        {
            var enumType = EnumEntity.Extract(type)!;
            var values = EnumEntity.GetValues(enumType).ToList();
            members.AddRange(values.Select(ev => new TypeMemberHelpTS(ev)));
        }
        else
        {
            var routes = PropertyRoute.GenerateRoutes(type);

            var root = TreeHelper.ToTreeC(routes, a => a.Parent).SingleEx();

            members = root.Children
                .Where(a => mode == TypeHelpMode.CSharp || ReflectionServer.InTypeScript(a.Value))
                .Select(pr => new TypeMemberHelpTS(pr, mode)).ToList();

            if (mode == TypeHelpMode.CSharp)
            {
                var expressions = QueryLogic.Expressions.RegisteredExtensions.GetValue(type);

                members.AddRange(expressions.Values.Select(ex => new TypeMemberHelpTS(ex)));
            }
        }

        return new TypeHelpTS
        {
            type = isEnum ? EnumEntity.Extract(type)!.Name : type.Name,
            cleanTypeName = typeName,
            isEnum = isEnum,
            members = members
        };
    }
}

public enum TypeHelpMode
{
    CSharp,
    Typescript
}

public class TypeHelpTS
{
    public string type;
    public string cleanTypeName;
    public bool isEnum;

    public List<TypeMemberHelpTS> members;
}

public class TypeMemberHelpTS
{
    public string? propertyString;
    public string? name;
    public string type;
    public string? cleanTypeName;
    public bool isExpression;
    public bool isEnum;

    public List<TypeMemberHelpTS> subMembers;

    public TypeMemberHelpTS(Node<PropertyRoute> node, TypeHelpMode mode)
    {
        var pr = node.Value;
        propertyString = pr.PropertyString();
        name = mode == TypeHelpMode.Typescript ?
            pr.PropertyInfo?.Name.FirstLower() :
            pr.PropertyInfo?.Name;

        type = mode == TypeHelpMode.Typescript && ReflectionServer.IsId(pr) ?
            PrimaryKey.Type(pr.RootType).Nullify().TypeName() :
            pr.Let(propertyRoute =>
            {
                var typeName = propertyRoute.Type.TypeName();
                var typeNullable = propertyRoute.Type.IsNullable();
                var piNullable = propertyRoute.PropertyInfo?.IsNullable();

                return typeName + (piNullable == true && !typeNullable ? "?" : "");
            });


        isExpression = false;
        isEnum = pr.Type.UnNullify().IsEnum;
        cleanTypeName = GetCleanTypeName(pr.Type.UnNullify().IsEnum ? EnumEntity.Generate(pr.Type.UnNullify()) : pr.Type);
        subMembers = node.Children.Select(a => new TypeMemberHelpTS(a, mode)).ToList();
    }

    public TypeMemberHelpTS(ExtensionInfo ex)
    {
        name = ex.Key;
        type = ex.Type.TypeName();
        isExpression = true;
        isEnum = false;
        cleanTypeName = GetCleanTypeName(ex.Type);
        subMembers = new List<TypeMemberHelpTS>();
    }

    public TypeMemberHelpTS(Enum ev)
    {
        name = ev.ToString();
        type = ev.GetType().TypeName();
        isExpression = false;
        isEnum = true;
        cleanTypeName = null;
        subMembers = new List<TypeMemberHelpTS>();
    }

    string? GetCleanTypeName(Type type)
    {
        type = type.ElementType() ?? type;
        return TypeLogic.TryGetCleanName(type.CleanType());
    }
}
