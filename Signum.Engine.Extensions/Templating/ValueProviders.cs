using Microsoft.AspNetCore.Mvc.ModelBinding;
using Signum.Engine.Translation;
using Signum.Entities.Mailing;
using Signum.Entities.Reflection;
using Signum.Entities.UserAssets;
using Signum.Utilities.DataStructures;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Signum.Engine.Templating;

public interface ITemplateParser
{
    Type? ModelType { get; }
    QueryDescription QueryDescription { get; }
    ScopedDictionary<string, ValueProviderBase> Variables { get; }
    void AddError(bool fatal, string error);
}

public abstract class ValueProviderBase
{
    public string? Variable { get; set; }

    public bool IsForeach { get; set; }

    public abstract object? GetValue(TemplateParameters p);

    public abstract string? Format { get; }
    
    public abstract Type? Type { get; }

    public abstract override bool Equals(object? obj);

    public abstract override int GetHashCode();

    public abstract void FillQueryTokens(List<QueryToken> list);

    public string ToStringWithoutBrackets(ScopedDictionary<string, ValueProviderBase> variables)
    {
        StringBuilder sb = new StringBuilder();
        ToStringInternal(sb, variables);
        return sb.ToString();
    }

    public abstract void ToStringInternal(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables);

    public string ToString(ScopedDictionary<string, ValueProviderBase> variables, string? format)
    {
        StringBuilder sb = new StringBuilder();
        ToStringBrackets(sb, variables, format);
        return sb.ToString();
    }

    public void ToStringBrackets(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables, string? format)
    {
        sb.Append("[");

        this.ToStringInternal(sb, variables);

        if (format.HasItems())
            sb.Append(format);

        sb.Append("]");

        if (Variable.HasItems())
            sb.Append(" as " + Variable);
    }

    public override string ToString() => ToString(new ScopedDictionary<string, ValueProviderBase>(null), null);

    public abstract void Synchronize(TemplateSynchronizationContext sc, string remainingText);

    public virtual void Declare(ScopedDictionary<string, ValueProviderBase> variables)
    {
        if (Variable.HasText())
        {
            if (variables.TryGetValue(Variable, out var value))
            {
                if (value != null && value.Equals(this))
                    return;

                else throw new InvalidOperationException("Redeclaring variable " + Variable + " with another value");
            }

            variables.Add(Variable, this);
        } 
    }

    public virtual void Foreach(TemplateParameters p, Action forEachElement)
    {
        var collection = (IEnumerable)this.GetValue(p)!;

        foreach (var item in collection)
        {
            using (p.Scope())
            {
                if (this.Variable != null)
                    p.RuntimeVariables.Add(this.Variable, item!);

                forEachElement();
            }
        }
    }


    public static readonly Regex TypeTokenRegex = new Regex(@"((?<type>[\w]):)?(?<token>.*)");

    public static ValueProviderBase? TryParse(string typeToken, string? variable, ITemplateParser tp)
    {
        var match = TypeTokenRegex.Match(typeToken);

        var type = match.Groups["type"].Value;
        var token = match.Groups["token"].Value;

        switch (type)
        {
            case "":
                {
                    if(token.StartsWith("$"))
                    {
                        string v = token.TryBefore('.') ?? token;

                        if (!tp.Variables.TryGetValue(v, out ValueProviderBase? vp))
                        {
                            tp.AddError(false, "Variable '{0}' is not defined at this scope".FormatWith(v));
                            return null;
                        }

                        if (!(vp is TokenValueProvider))
                            return new ContinueValueProvider(token.TryAfter('.'), vp, tp);
                    }

                    if (ConstantValueProvider.TryParseConstantValue(token, out var val))
                        return new ConstantValueProvider(val, tp);

                    ParsedToken result = ParsedToken.TryParseToken(token, SubTokensOptions.CanElement, tp.QueryDescription, tp.Variables, tp.AddError);

                    if (result.QueryToken != null && TranslateInstanceValueProvider.IsTranslateInstanceCanditate(result.QueryToken))
                        return new TranslateInstanceValueProvider(result, false, tp) { Variable = variable };
                    else
                        return new TokenValueProvider(result, false) { Variable = variable };
                }
            case "q":
                {
                    ParsedToken result = ParsedToken.TryParseToken(token, SubTokensOptions.CanElement, tp.QueryDescription, tp.Variables, tp.AddError);

                    return new TokenValueProvider(result, true) { Variable = variable };
                }
            case "t":
                {
                    ParsedToken result = ParsedToken.TryParseToken(token, SubTokensOptions.CanElement, tp.QueryDescription, tp.Variables, tp.AddError);

                    return new TranslateInstanceValueProvider(result, true, tp) { Variable = variable };
                }
            case "m":
                return new ModelValueProvider(token, tp.ModelType, tp) { Variable = variable };
            case "g":
                return new GlobalValueProvider(token, tp) { Variable = variable };
            case "d":
                return new DateValueProvider(token, tp) { Variable = variable };
            default:
                tp.AddError(false, $"{type} is not a recognized value provider (q:Query, t:Translate, m:Model, g:Global or just blank)");
                return null;
        }
    }

    public void ValidateConditionValue(string valueString, FilterOperation? Operation, Action<bool, string> addError)
    {
        if (Type == null)
            return;

        var result = FilterValueConverter.IsValidExpression(valueString, Type, Operation!.Value.IsList(), null);

        if (result is Result<Type>.Error e)
            addError(false, "Impossible to convert '{0}' to {1}: {2}".FormatWith(valueString, Type.TypeName(), e.ErrorText));
    }
}

public abstract class TemplateParameters
{
    public TemplateParameters(IEntity? entity, CultureInfo culture, Dictionary<QueryToken, ResultColumn> columns, IEnumerable<ResultRow> rows)
    {
        this.Entity = entity;
        this.Culture = culture;
        this.Columns = columns;
        this.Rows = rows;
    }

    public readonly IEntity? Entity;
    public readonly CultureInfo Culture;
    public readonly Dictionary<QueryToken, ResultColumn> Columns;
    public IEnumerable<ResultRow> Rows { get; private set; }

    public ScopedDictionary<string, object> RuntimeVariables = new ScopedDictionary<string, object>(null);

    public abstract object GetModel();

    public IDisposable OverrideRows(IEnumerable<ResultRow> rows)
    {
        var old = this.Rows;
        this.Rows = rows;
        return new Disposable(() => this.Rows = old);
    }

    internal IDisposable Scope()
    {
        var old = RuntimeVariables;
        RuntimeVariables = new ScopedDictionary<string, object>(RuntimeVariables);
        return new Disposable(() => RuntimeVariables = old);
    }
}

/// <summary>
/// like @[Entity.UserName]  or @[q:Entity.UserName]
/// </summary>
public class TokenValueProvider : ValueProviderBase
{
    public readonly ParsedToken ParsedToken;
    public readonly bool IsExplicit;

    public override int GetHashCode() => ParsedToken.GetHashCode();
    public override bool Equals(object? obj) => obj is TokenValueProvider tvp && tvp.ParsedToken.Equals(ParsedToken);

    public TokenValueProvider (ParsedToken token, bool isExplicit)
    {
        this.ParsedToken = token;
        this.IsExplicit = isExplicit;
    }

    public override object? GetValue(TemplateParameters p)
    {
        if (p.Rows.IsEmpty())
            return null;

        return p.Rows.DistinctSingle(p.Columns[ParsedToken.QueryToken!]);
    }

    public override void Foreach(TemplateParameters p, Action forEachElement)
    {
        var col = p.Columns[ParsedToken.QueryToken!];

        foreach (var group in p.Rows.GroupByColumn(col))
        {
            using (p.OverrideRows(group))
                forEachElement();
        }
    }

    public override string? Format
    {
        get { return ParsedToken.QueryToken!.Format; }
    }

    public override void FillQueryTokens(List<QueryToken> list)
    {  
        list.Add(ParsedToken.QueryToken!);
    }

    public override void ToStringInternal(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
    {
        if (this.IsExplicit)
            sb.Append("q:"); 

        sb.Append(this.ParsedToken.ToString(variables));
    }

    public override void Synchronize(TemplateSynchronizationContext sc, string remainingText)
    {
        sc.SynchronizeToken(ParsedToken, remainingText);

        Declare(sc.Variables);
    }

    public override Type? Type
    {
        get { return ParsedToken.QueryToken?.Type; }
    }
}

/// <summary>
/// like @[t:Entity.ProductName]
/// </summary>
public class TranslateInstanceValueProvider : ValueProviderBase
{
    public readonly ParsedToken ParsedToken;
    public readonly QueryToken? EntityToken;
    public readonly PropertyRoute? Route;
    public readonly bool IsExplicit;



    public TranslateInstanceValueProvider(ParsedToken token, bool isExplicit, ITemplateParser tp)
    {
        this.ParsedToken = token;
        this.IsExplicit = isExplicit;
        if (token.QueryToken != null)
        {
            this.Route = token.QueryToken.GetPropertyRoute();
            this.EntityToken = DeterminEntityToken(token.QueryToken, tp.AddError);
        }
    }

    public override object? GetValue(TemplateParameters p)
    {
        var entity = (Lite<Entity>)p.Rows.DistinctSingle(p.Columns[EntityToken!])!;
        var fallback = (string)p.Rows.DistinctSingle(p.Columns[ParsedToken.QueryToken!])!;

        return entity == null ? null : TranslatedInstanceLogic.TranslatedField(entity, Route!, fallback);
    }

    public override void Foreach(TemplateParameters parameters, Action forEachElement)
    {
        throw new NotImplementedException("{0} can not be used to foreach".FormatWith(typeof(TranslateInstanceValueProvider).Name));
    }

    QueryToken DeterminEntityToken(QueryToken token, Action<bool, string> addError)
    {
        var entityToken = token.Follow(a => a.Parent).FirstOrDefault(a => a.Type.IsLite() || a.Type.IsIEntity());

        if (entityToken == null)
            entityToken = QueryUtils.Parse("Entity", QueryLogic.Queries.QueryDescription(token.QueryName), 0);

        if (!entityToken.Type.CleanType().IsAssignableFrom(Route!.RootType))
            addError(false, "The entity of {0} ({1}) is not compatible with the property route {2}".FormatWith(token.FullKey(), entityToken.FullKey(), Route.RootType.NiceName()));

        return entityToken;
    }

    public static bool IsTranslateInstanceCanditate(QueryToken token)
    {
        if (token.Type != typeof(string))
            return false;

        var pr = token.GetPropertyRoute();
        if (pr == null)
            return false;

        if (TranslatedInstanceLogic.RouteType(pr) == null)
            return false;

        return true;
    }

    public override string? Format
    {
        get { return null; }
    }

    public override void FillQueryTokens(List<QueryToken> list)
    {
        list.Add(ParsedToken.QueryToken!);
        list.Add(EntityToken!);
    }

    public override void ToStringInternal(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
    {
        if (this.IsExplicit)
            sb.Append("t:");

        sb.Append(this.ParsedToken.ToString(variables));
    }

    public override void Synchronize(TemplateSynchronizationContext sc, string remainingText)
    {
        sc.SynchronizeToken(ParsedToken, remainingText);

        Declare(sc.Variables);
    }

    public override Type ?Type
    {
        get { return typeof(string); }
    }

    public override int GetHashCode() => ParsedToken.GetHashCode();
    public override bool Equals(object? obj) => obj is TranslateInstanceValueProvider tivp &&
        Equals(tivp.ParsedToken, ParsedToken) &&
        Equals(tivp.EntityToken, EntityToken) &&
        Equals(tivp.Route, Route) &&
        Equals(tivp.IsExplicit, IsExplicit);
}


public class ParsedToken
{
    public string String;
    public QueryToken? QueryToken { get; set; }

    public ParsedToken(string @string)
    {
        String = @string;
    }

    public static ParsedToken TryParseToken(string tokenString, SubTokensOptions options, QueryDescription qd, ScopedDictionary<string, ValueProviderBase> variables, Action<bool, string> addError)
    {
        ParsedToken result = new ParsedToken(tokenString);

        if (tokenString.StartsWith("$"))
        {
            string v = tokenString.TryBefore('.') ?? tokenString;

            if (!variables.TryGetValue(v, out ValueProviderBase? vp))
            {
                addError(false, "Variable '{0}' is not defined at this scope".FormatWith(v));
                return result;
            }

            var tvp = vp as TokenValueProvider;

            if(tvp == null)
            {
                addError(false, "Variable '{0}' is not a token".FormatWith(v));
                return result;
            }

            if (tvp.ParsedToken.QueryToken == null)
            {
                addError(false, "Variable '{0}' is not a correctly parsed".FormatWith(v));
                return result;
            }

            var after = tokenString.TryAfter('.');

            tokenString = tvp.ParsedToken.QueryToken.FullKey() + (after == null ? null : ("." + after));
        }

        try
        {
            result.QueryToken = QueryUtils.Parse(tokenString, qd, options);
        }
        catch (Exception ex)
        {
            addError(false, ex.Message);
        }
        return result;
    }

    public string SimplifyToken(ScopedDictionary<string, ValueProviderBase> variables, string token)
    {
        var pair = (from kvp in variables
                    let tp = kvp.Value as TokenValueProvider
                    where tp != null
                    let fullKey = tp.ParsedToken.QueryToken!.FullKey()
                    where token == fullKey || token.StartsWith(fullKey + ".")
                    orderby fullKey.Length descending
                    select new { kvp.Key, fullKey }).FirstOrDefault();

        if (pair != null)
        {
            return pair.Key + token.RemoveStart(pair.fullKey!.Length); /*CSBUG*/
        }

        return token;
    }

    internal string ToString(ScopedDictionary<string, ValueProviderBase> variables)
    {
        if(QueryToken == null)
            return String;

        return SimplifyToken(variables, QueryToken.FullKey());
    }

    public override int GetHashCode() => (this.QueryToken?.FullKey() ?? this.String).GetHashCode();
    public override bool Equals(object? obj) => obj is ParsedToken pt && Equals(pt.String, String) && Equals(pt.QueryToken, QueryToken);
}


/// <summary>
/// like @[m:CurrentCode] where CurrentCode is:
/// * A property like string CurrrentCode { get; }
/// * A method like string CurrrentCode(TemplateParameters params)
/// </summary>
public class ModelValueProvider : ValueProviderBase
{
    string? fieldOrPropertyChain;
    List<MemberWithArguments>? Members;


    public ModelValueProvider(string fieldOrPropertyChain, Type? modelType, ITemplateParser tp)
    {
        this.fieldOrPropertyChain = fieldOrPropertyChain;
        if (modelType == null)
        {
            tp.AddError(false, EmailTemplateMessage.ImpossibleToAccess0BecauseTheTemplateHAsNo1.NiceToString(fieldOrPropertyChain, "Model"));
            return;
        }

        this.Members = ParsedModel.GetMembers(modelType, fieldOrPropertyChain, tp);
    }

    public override object? GetValue(TemplateParameters p)
    {
        object? value = p.GetModel();
        foreach (var m in Members!)
        {
            value = Getter(m, value, p);
            if (value == null)
                break;
        }

        return value;
    }

    internal static object? Getter(MemberWithArguments mwa, object model, TemplateParameters p)
    {
        try
        {
            if (mwa.Member is PropertyInfo pi)
                return pi.GetValue(model, null);

            if (mwa.Member is MethodInfo mi)
            {
                var arguments = mwa.Arguments == null ? 
                    new object[] { p } :
                    mwa.Arguments.Select(a => a.GetValue(p)).And(p).ToArray();

                return mi.Invoke(model, arguments);
            }

            return ((FieldInfo)mwa.Member).GetValue(model);
        }
        catch (TargetInvocationException e)
        {
            e.InnerException!.PreserveStackTrace();

            throw e.InnerException!;
        }
    }

    public override string? Format
    {
        get { return Reflector.FormatString(this.Type!); }
    }

    public override Type? Type
    {
        get { return Members?.Let(ms => ms.Last().Member.ReturningType().Nullify()); }
    }

    public override void FillQueryTokens(List<QueryToken> list)
    {
        foreach (var item in Members.EmptyIfNull().SelectMany(m => m.Arguments.EmptyIfNull()).NotNull())
        {
            item.FillQueryTokens(list);
        } 
    }

    public override void ToStringInternal(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
    {
        sb.Append("m:");
        sb.Append(Members == null ? fieldOrPropertyChain : Members.ToString(a => a.ToString(variables), "."));
    }

    public override void Synchronize(TemplateSynchronizationContext sc, string remainingText)
    {
        if (Members == null)
        {
            Members = sc.GetMembers(fieldOrPropertyChain!, sc.ModelType!);

            if (Members != null)
                fieldOrPropertyChain = Members.ToString(a => a.ToString(sc.Variables), ".");
        }

        Declare(sc.Variables);
    }

    public override int GetHashCode() => fieldOrPropertyChain?.GetHashCode() ?? 0;
    public override bool Equals(object? obj) => obj is ModelValueProvider mvp && Equals(mvp.fieldOrPropertyChain, fieldOrPropertyChain);
}


/// <summary>
/// like @[g:Now]
/// </summary>
public class GlobalValueProvider : ValueProviderBase
{
    public class GlobalVariable
    {
        public Func<TemplateParameters, object?> GetValue;
        public Type Type;
        public string? Format;

        public GlobalVariable(Func<TemplateParameters, object?> getValue, Type type, string? format)
        {
            GetValue = getValue;
            Type = type;
            Format = format;
        }
    }

    public static Dictionary<string, GlobalVariable> GlobalVariables = new Dictionary<string, GlobalVariable>();

    public static void RegisterGlobalVariable<T>(string key, Func<TemplateParameters, T> globalVariable, string? format = null)
    {
        GlobalVariables.Add(key, new GlobalVariable(a => globalVariable(a), typeof(T), format));
    }


    string globalKey;
    string? remainingFieldsOrProperties;
    List<MemberWithArguments>? Members;

    public GlobalValueProvider(string fieldOrPropertyChain, ITemplateParser tp)
    {
        globalKey = fieldOrPropertyChain.TryBefore('.') ?? fieldOrPropertyChain;
        remainingFieldsOrProperties = fieldOrPropertyChain.TryAfter('.');

        var gv = GlobalVariables.TryGetC(globalKey); 

        if (gv == null)
            tp.AddError(false, "The global key {0} was not found".FormatWith(globalKey));

        if (remainingFieldsOrProperties != null && gv != null)
            this.Members = ParsedModel.GetMembers(gv.Type, remainingFieldsOrProperties, tp);
    }

    public override object? GetValue(TemplateParameters p)
    {
        object? value = GlobalVariables[globalKey].GetValue(p);
        
        if (value == null)
            return null;

        if (Members != null)
        {
            foreach (var m in Members)
            {
                value = ModelValueProvider.Getter(m, value, p);
                if (value == null)
                    break;
            }
        }

        return value;
    }

    public override string? Format
    {
        get
        {
            return Members == null ?
                GlobalVariables.TryGetC(globalKey)?.Format ?? Reflector.FormatString(Type!) :
                Reflector.FormatString(Type!);
        }
    }

    public override Type? Type
    {
        get
        {
            if (remainingFieldsOrProperties.HasText())
                return Members?.Let(ms => ms.Last().Member.ReturningType().Nullify());
            else
                return GlobalVariables.TryGetC(globalKey)?.Type;
        }
    }

    public override void FillQueryTokens(List<QueryToken> list)
    {
        foreach (var item in Members.EmptyIfNull().SelectMany(m => m.Arguments.EmptyIfNull()).NotNull())
        {
            item.FillQueryTokens(list);
        }
    }

    public override void ToStringInternal(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
    {
        sb.Append("g:");
        sb.Append(globalKey);
        if (remainingFieldsOrProperties.HasText())
        {
            sb.Append(".");
            sb.Append(Members == null ? remainingFieldsOrProperties : Members.ToString(a => a.ToString(variables), "."));
        }
    }

    public override void Synchronize(TemplateSynchronizationContext sc, string remainingText)
    {
        globalKey = sc.Replacements.SelectInteractive(globalKey, GlobalVariables.Keys, "Globals", sc.StringDistance) ?? globalKey;

        if(remainingFieldsOrProperties.HasText() && Members == null)
        {
            Members = sc.GetMembers(remainingFieldsOrProperties, GlobalVariables[globalKey].Type);

            if (Members != null)
                remainingFieldsOrProperties = Members.ToString(a => a.ToString(sc.Variables), ".");
        }

        Declare(sc.Variables);
    }

    public override int GetHashCode() => globalKey.GetHashCode() + (remainingFieldsOrProperties?.GetHashCode() ?? 0);
    public override bool Equals(object? obj) => obj is GlobalValueProvider gvp 
        && Equals(gvp.globalKey, globalKey)
        && Equals(gvp.remainingFieldsOrProperties, remainingFieldsOrProperties);
}


/// <summary>
/// Like @[d:yyyy/mm/-1 00:00:00]
/// </summary>
public class DateValueProvider : ValueProviderBase
{
    string? dateTimeExpression; 
    public DateValueProvider(string dateTimeExpression, ITemplateParser tp)
    {
        try
        {
            var obj = dateTimeExpression == null ? Clock.Now: FilterValueConverter.Parse(dateTimeExpression, typeof(DateTime?), isList: false);
            this.dateTimeExpression = dateTimeExpression;
        }
        catch (Exception e)
        {
            tp.AddError(false, $"Invalid expression {dateTimeExpression}: {e.Message}");
        }
    }

    public override Type? Type => typeof(DateTime?);

    public override object? GetValue(TemplateParameters p)
    {
        return dateTimeExpression == null ? Clock.Now : FilterValueConverter.Parse(this.dateTimeExpression, typeof(DateTime?), isList: false);
    }

    public override void FillQueryTokens(List<QueryToken> list)
    {
    }

    public override void ToStringInternal(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
    {
        sb.Append("d:");
        sb.Append(TemplateUtils.ScapeColon(this.dateTimeExpression!));
    }

    public override void Synchronize(TemplateSynchronizationContext sc, string remainingText)
    { 


    }

    public override string? Format => "G";

    public override int GetHashCode() => dateTimeExpression?.GetHashCode() ?? 0;
    public override bool Equals(object? obj) => obj is DateValueProvider gvp
        && Equals(gvp.dateTimeExpression, dateTimeExpression);
}

public class ConstantValueProvider : ValueProviderBase
{
    public object? Value;

    public static bool TryParseConstantValue(string valueExpression, out object? value)
    {
        if(valueExpression.ToLower() == "null")
        {
            value = null;
            return true;
        }

        if(int.TryParse(valueExpression, NumberStyles.Integer, CultureInfo.InvariantCulture, out int a))
        {
            value = a;
            return true;
        }

        if (decimal.TryParse(valueExpression, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal d))
        {
            value = d;
            return  true;
        }

        if(valueExpression.StartsWith('\"') && valueExpression.EndsWith('\"'))
        {
            value = valueExpression.Trim('\"');
            return true;
        }

        if (valueExpression.StartsWith('\'') && valueExpression.EndsWith('\''))
        {
            value = valueExpression.Trim('\'');
            return true;
        }

        value = null;
        return false;
    }

    public ConstantValueProvider(object? value, ITemplateParser tp)
    {
        this.Value = value;
    }

    public override Type? Type => Value?.GetType() ?? typeof(object);

    public override object? GetValue(TemplateParameters p)
    {
        return Value;
    }

    public override void FillQueryTokens(List<QueryToken> list)
    {
    }

    public override void ToStringInternal(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
    {
        var val = Value == null ? "null" :
            Value is int a ? a.ToString(CultureInfo.InvariantCulture) :
            Value is decimal d ? d.ToString(CultureInfo.InvariantCulture) :
            Value is string str ? @"""{str}""" :
            throw new NotImplementedException();

        sb.Append(val);
    }

    public override void Synchronize(TemplateSynchronizationContext sc, string remainingText)
    {


    }

    public override string? Format => null;

    public override int GetHashCode() => Value?.GetHashCode() ?? 0;
    public override bool Equals(object? obj) => obj is ConstantValueProvider gvp
        && Equals(gvp.Value, Value);

}


/// <summary>
/// like @[$line.Product] inside @foreach[m:Lines] as $line
/// </summary>
public class ContinueValueProvider : ValueProviderBase
{
    string? fieldOrPropertyChain;
    List<MemberWithArguments>? Members;
    ValueProviderBase Parent;

    public ContinueValueProvider(string? fieldOrPropertyChain, ValueProviderBase parent,  ITemplateParser tp)
    {
        this.Parent = parent;

        var pt = ParentType();
        if (pt == null)
            tp.AddError(false, $"Impossible to continue with {fieldOrPropertyChain} (parentType is null)");
        else
            this.Members = ParsedModel.GetMembers(pt, fieldOrPropertyChain, tp);
    }

    private Type? ParentType()
    {
        if (Parent.IsForeach)
            return Parent.Type?.ElementType();

        return Parent.Type;
    }

    public override object? GetValue(TemplateParameters p)
    {
        if (!p.RuntimeVariables.TryGetValue(Parent.Variable!, out object? value))
            throw new InvalidOperationException("Variable {0} not found".FormatWith(Parent.Variable));

        foreach (var m in Members!)
        {
            value = Getter(m.Member, value, p);
            if (value == null)
                break;
        }

        return value;
    }

    internal static object? Getter(MemberInfo member, object value, TemplateParameters p)
    {
        try
        {
            if (member is PropertyInfo pi)
                return pi.GetValue(value, null);

            if (member is MethodInfo mi)
                return mi.Invoke(value, new object[] { p });

            return ((FieldInfo)member).GetValue(value);
        }
        catch (TargetInvocationException e)
        {
            e.InnerException!.PreserveStackTrace();

            throw e.InnerException!;
        }
    }

    public override string? Format
    {
        get { return Reflector.FormatString(this.Type!); }
    }

    public override Type? Type
    {
        get { return Members?.Let(ms => ms.Last().Member.ReturningType().Nullify()); }
    }

    public override void FillQueryTokens(List<QueryToken> list)
    {
        foreach (var item in Members.EmptyIfNull().SelectMany(m => m.Arguments.EmptyIfNull()).NotNull())
        {
            item.FillQueryTokens(list);
        }
    }

    public override void ToStringInternal(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
    {
        sb.Append(Parent.Variable);
        sb.Append(".");
        sb.Append(Members == null ? fieldOrPropertyChain : Members.ToString(a => a.ToString(variables)!, "."));
    }

    public override void Synchronize(TemplateSynchronizationContext sc, string remainingText)
    {
        if (Members == null)
        {
            Members = sc.GetMembers(fieldOrPropertyChain!, ParentType()!);

            if (Members != null)
                fieldOrPropertyChain = Members.ToString(a => a.ToString(sc.Variables), ".");
        }

        Declare(sc.Variables);
    }

    public override int GetHashCode() => (fieldOrPropertyChain?.GetHashCode() ?? 0) ^ this.Parent.GetHashCode();
    public override bool Equals(object? obj) => obj is ContinueValueProvider gvp
        && Equals(gvp.fieldOrPropertyChain, fieldOrPropertyChain)
        && Equals(gvp.Parent, Parent);
}
