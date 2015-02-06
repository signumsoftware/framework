using Signum.Engine.DynamicQuery;
using Signum.Engine.Translation;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Mailing;
using Signum.Entities.Reflection;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Templating
{
    public abstract class ValueProviderBase
    {
        public string Variable { get; set; }

        public abstract object GetValue(TemplateParameters p);

        public abstract string Format { get; }
        
        public abstract Type Type { get; }

        public abstract void FillQueryTokens(List<QueryToken> list);

        public abstract void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables, string afterToken);

        public string ToString(ScopedDictionary<string, ValueProviderBase> variables, string afterToken)
        {
            StringBuilder sb = new StringBuilder();
            ToString(sb, variables, afterToken);
            return sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ToString(sb, new ScopedDictionary<string, ValueProviderBase>(null), null);
            return sb.ToString();
        }

        public abstract void Synchronize(SyncronizationContext sc, string p);

        public virtual void Declare(ScopedDictionary<string, ValueProviderBase> variables)
        {
            if (Variable.HasText())
                variables.Add(Variable, this);
        }

        public bool GetCondition(TemplateParameters p, FilterOperation? operation, string valueString)
        {
            var obj = this.GetValue(p);

            if (operation == null)
                return ToBool(obj);
            else
            {
                var type = this.Type;

                Expression token = Expression.Constant(obj, type);

                Expression value = Expression.Constant(FilterValueConverter.Parse(valueString, type, operation == FilterOperation.IsIn), type);

                Expression newBody = QueryUtils.GetCompareExpression(operation.Value, token, value, inMemory: true);
                var lambda = Expression.Lambda<Func<bool>>(newBody).Compile();

                return lambda();
            }
        }


        protected static bool ToBool(object obj)
        {
            if (obj == null || obj is bool && ((bool)obj) == false)
                return false;

            return true;
        }


        public virtual void Foreach(TemplateParameters p, Action forEachElement)
        {
            var collection = (IEnumerable)this.GetValue(p);

            foreach (var item in collection)
            {
                forEachElement();
            }
        }

        public virtual IEnumerable<object> GetFilteredRows(TemplateParameters p, FilterOperation? operation, string stringValue)
        {
            var collection = (IEnumerable)this.GetValue(p);

            return collection.Cast<object>();
        }

        public static ValueProviderBase TryParse(string type, string token, string variable, Type modelType, QueryDescription qd, ScopedDictionary<string, ValueProviderBase> variables, Action<bool, string> addError)
        {
            switch (type)
            {
                case "":
                    {
                        ParsedToken result = ParsedToken.TryParseToken(token, SubTokensOptions.CanElement, qd, variables, addError);

                        if (result.QueryToken != null && TranslateInstanceValueProvider.IsTranslateInstanceCanditate(result.QueryToken))
                            return new TranslateInstanceValueProvider(result, false, addError) { Variable = variable };
                        else
                            return new TokenValueProvider(result, false) { Variable = variable };
                    }
                case "q":
                    {
                        ParsedToken result = ParsedToken.TryParseToken(token, SubTokensOptions.CanElement, qd, variables, addError);

                        return new TokenValueProvider(result, true) { Variable = variable };
                    }
                case "t":
                    {
                        ParsedToken result = ParsedToken.TryParseToken(token, SubTokensOptions.CanElement, qd, variables, addError);

                        return new TranslateInstanceValueProvider(result, true, addError) { Variable = variable };
                    }
                case "m":
                    return new ModelValueProvider(token, modelType, addError) { Variable = variable };
                case "g":
                    return new GlobalValueProvider(token, addError) { Variable = variable };
                default:
                    addError(false, "{0} is not a recognized value provider (q:Query, t:Translate, m:Model, g:Global or just blank)");
                    return null;
            }
        }

        public void ValidateConditionValue(string valueString, FilterOperation? Operation, Action<bool, string> addError)
        {
            if (Type == null)
                return;

            object rubish;
            string error = FilterValueConverter.TryParse(valueString, Type, out rubish, Operation == FilterOperation.IsIn);
            
            if (error.HasText())
                addError(false, "Impossible to convert '{0}' to {1}: {2}".FormatWith(valueString, Type.TypeName(), error));
        }
    }

    public abstract class TemplateParameters
    {
        public TemplateParameters(IEntity entity, CultureInfo culture, Dictionary<QueryToken, ResultColumn> columns, IEnumerable<ResultRow> rows)
        {
            this.Entity = entity;
            this.Culture = culture;
            this.Columns = columns;
            this.Rows = rows;
        }

        public readonly IEntity Entity;
        public readonly CultureInfo Culture;
        public readonly Dictionary<QueryToken, ResultColumn> Columns;
        public IEnumerable<ResultRow> Rows { get; private set; }
        public abstract object GetModel();

        public IDisposable OverrideRows(IEnumerable<ResultRow> rows)
        {
            var old = this.Rows;
            this.Rows = rows;
            return new Disposable(() => this.Rows = old);
        }
    }

    public class TokenValueProvider : ValueProviderBase
    {
        public readonly ParsedToken ParsedToken;
        public readonly bool IsExplicit;

        public TokenValueProvider (ParsedToken token, bool isExplicit)
        {
            this.ParsedToken = token;
            this.IsExplicit = isExplicit;
        }

        public override object GetValue(TemplateParameters p)
        {
            if (p.Rows.IsEmpty())
                return null;

            return  p.Rows.DistinctSingle(p.Columns[ParsedToken.QueryToken]);
        }

        public override void Foreach(TemplateParameters p, Action forEachElement)
        {
            var groups = p.Rows.GroupBy(r => r[p.Columns[ParsedToken.QueryToken]]).ToList();
            if (groups.Count == 1 && groups[0].Key == null)
                return;

            foreach (var group in groups)
            {
                using (p.OverrideRows(group))
                    forEachElement();
            }
        }

        public override string Format
        {
            get { return ParsedToken.QueryToken.Format; }
        }

        public override void FillQueryTokens(List<QueryToken> list)
        {
            list.Add(ParsedToken.QueryToken);
        }

        public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables, string afterToken)
        {
            sb.Append("[");
            if (this.IsExplicit)
                sb.Append("q:"); 

            sb.Append(this.ParsedToken.ToString(variables));

            if (afterToken.HasItems())
                sb.Append(afterToken);

            sb.Append("]");

            if (Variable.HasItems())
                sb.Append(" as " + Variable);
        }

        public override void Synchronize(SyncronizationContext sc, string remainingText)
        {
            sc.SynchronizeToken(ParsedToken, remainingText);

            Declare(sc.Variables);
        }

        public override Type Type
        {
            get { return ParsedToken.QueryToken.Try(t => t.Type); }
        }

        public override IEnumerable<object> GetFilteredRows(TemplateParameters p, FilterOperation? operation, string stringValue)
        {
            if (operation == null)
            {
                var column = p.Columns[ParsedToken.QueryToken];

                var filtered = p.Rows.Where(r => ToBool(r[column])).ToList();

                return filtered;
            }
            else
            {
                var type = this.Type;

                object val = FilterValueConverter.Parse(stringValue, type, operation == FilterOperation.IsIn);

                Expression value = Expression.Constant(val, type);

                ResultColumn col = p.Columns[ParsedToken.QueryToken];

                var expression = Signum.Utilities.ExpressionTrees.Linq.Expr((ResultRow rr) => rr[col]);

                Expression newBody = QueryUtils.GetCompareExpression(operation.Value, Expression.Convert(expression.Body, type), value, inMemory: true);
                var lambda = Expression.Lambda<Func<ResultRow, bool>>(newBody, expression.Parameters).Compile();

                var filtered = p.Rows.Where(lambda).ToList();

                return filtered;
            }
        }
    }

    public class TranslateInstanceValueProvider : ValueProviderBase
    {
        public readonly ParsedToken ParsedToken;
        public readonly QueryToken EntityToken;
        public readonly PropertyRoute Route;
        public readonly bool IsExplicit;
        

        public TranslateInstanceValueProvider(ParsedToken token, bool isExplicit, Action<bool, string> addError)
        {
            this.ParsedToken = token;
            this.Route = token.QueryToken.GetPropertyRoute();
            this.IsExplicit = isExplicit;
            this.EntityToken = DeterminEntityToken(token.QueryToken, addError);
        }

        public override object GetValue(TemplateParameters p)
        {
            var entity = (Lite<Entity>)p.Rows.DistinctSingle(p.Columns[EntityToken]);
            var fallback = (string)p.Rows.DistinctSingle(p.Columns[ParsedToken.QueryToken]);

            return entity == null ? null : TranslatedInstanceLogic.TranslatedField(entity, Route, fallback);
        }

        public override void Foreach(TemplateParameters parameters, Action forEachElement)
        {
            throw new NotImplementedException("{0} can not be used to foreach".FormatWith(typeof(TranslateInstanceValueProvider).Name));
        }

        QueryToken DeterminEntityToken(QueryToken token, Action<bool, string> addError)
        {
            var entityToken = token.Follow(a => a.Parent).FirstOrDefault(a => a.Type.IsLite() || a.Type.IsIEntity());

            if (entityToken == null)
                entityToken = QueryUtils.Parse("Entity", DynamicQueryManager.Current.QueryDescription(token.QueryName), 0);

            if (!entityToken.Type.IsAssignableFrom(Route.RootType))
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

        public override string Format
        {
            get { return null; }
        }

        public override void FillQueryTokens(List<QueryToken> list)
        {
            list.Add(ParsedToken.QueryToken);
            list.Add(EntityToken);
        }

        public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables, string afterToken)
        {
            sb.Append("[");
            if (this.IsExplicit)
                sb.Append("t:");

            sb.Append(this.ParsedToken.ToString(variables));

            if (afterToken.HasItems())
                sb.Append(afterToken);

            sb.Append("]");

            if (Variable.HasItems())
                sb.Append(" as " + Variable);
        }

        public override void Synchronize(SyncronizationContext sc, string remainingText)
        {
            sc.SynchronizeToken(ParsedToken, remainingText);

            Declare(sc.Variables);
        }

        public override Type Type
        {
            get { return typeof(string); }
        }
    }

    public class ParsedToken
    {
        public string String;
        public QueryToken QueryToken;

        public static ParsedToken TryParseToken(string tokenString, SubTokensOptions options, QueryDescription qd, ScopedDictionary<string, ValueProviderBase> variables, Action<bool, string> addError)
        {
            ParsedToken result = new ParsedToken { String = tokenString };

            if (tokenString.StartsWith("$"))
            {
                string v = tokenString.TryBefore('.') ?? tokenString;

                ValueProviderBase vp;
                if (!variables.TryGetValue(v, out vp))
                {
                    addError(false, "Variable '{0}' is not defined at this scope".FormatWith(v));
                    return result;
                }

                if(!(vp is TokenValueProvider))
                {
                    addError(false, "Variable '{0}' is not a token".FormatWith(v));
                    return result;
                }

                var after = tokenString.TryAfter('.');

                tokenString = ((TokenValueProvider)vp).ParsedToken.QueryToken.FullKey() + (after == null ? null : ("." + after));
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
                        let fullKey = tp.ParsedToken.QueryToken.FullKey()
                        where token == fullKey || token.StartsWith(fullKey + ".")
                        orderby fullKey.Length descending
                        select new { kvp.Key, fullKey }).FirstOrDefault();

            if (pair != null)
            {
                return pair.Key + token.RemoveStart(pair.fullKey.Length);
            }

            return token;
        }

        internal string ToString(ScopedDictionary<string, ValueProviderBase> variables)
        {
            if(QueryToken == null)
                return String;

            return SimplifyToken(variables, QueryToken.FullKey());
        }
    }

    public class ModelValueProvider : ValueProviderBase
    {
        string fieldOrPropertyChain;
        List<MemberInfo> Members;

        public ModelValueProvider(string fieldOrPropertyChain, Type systemEmail, Action<bool, string> addError)
        {
            if (systemEmail == null)
            {
                addError(false, EmailTemplateMessage.SystemEmailShouldBeSetToAccessModel0.NiceToString().FormatWith(fieldOrPropertyChain));
                return;
            }

            this.Members = ParsedModel.GetMembers(systemEmail, fieldOrPropertyChain, addError);
        }

        public override object GetValue(TemplateParameters p)
        {
            object value = p.GetModel();
            foreach (var m in Members)
            {
                value = Getter(m, value);
                if (value == null)
                    break;
            }

            return value;
        }

        internal static object Getter(MemberInfo member, object systemEmail)
        {
            var pi = member as PropertyInfo;

            if (pi != null)
                return pi.GetValue(systemEmail, null);

            return ((FieldInfo)member).GetValue(systemEmail);
        }

        public override string Format
        {
            get { return Reflector.FormatString(this.Type); }
        }

        public override Type Type
        {
            get { return Members.Try(ms => ms.Last().ReturningType().Nullify()); }
        }

        public override void FillQueryTokens(List<QueryToken> list)
        {
        }

        public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables, string afterToken)
        {
            sb.Append("[m:");
            sb.Append(Members == null ? fieldOrPropertyChain : Members.ToString(a => a.Name, "."));
            sb.Append(afterToken);
            sb.Append("]");

            if (Variable.HasItems())
                sb.Append(" as " + Variable);
        }

        public override void Synchronize(SyncronizationContext sc, string p)
        {
            if (Members == null)
            {
                Members = sc.GetMembers(fieldOrPropertyChain, sc.ModelType);

                if (Members != null)
                    fieldOrPropertyChain = Members.ToString(a => a.Name, ".");
            }

            Declare(sc.Variables);
        }
    }

    public class GlobalValueProvider : ValueProviderBase
    {
        public class GlobalVariable
        {
            public Func<TemplateParameters, object> GetValue;
            public Type Type;
        }

        public static Dictionary<string, GlobalVariable> GlobalVariables = new Dictionary<string, GlobalVariable>();

        public static void RegisterGlobalVariable<T>(string key, Func<TemplateParameters, T> globalVariable)
        {
            GlobalVariables.Add(key, new GlobalVariable
            {
                GetValue = a => globalVariable(a),
                Type = typeof(T),
            });
        }


        string globalKey;
        string remainingFieldsOrProperties;
        List<MemberInfo> Members;

        public GlobalValueProvider(string fieldOrPropertyChain, Action<bool, string> addError)
        {
            globalKey = fieldOrPropertyChain.TryBefore('.') ?? fieldOrPropertyChain;
            remainingFieldsOrProperties = fieldOrPropertyChain.TryAfter('.');

            var gv = GlobalVariables.TryGetC(globalKey); 

            if (gv == null)
                addError(false, "The global key {0} was not found".FormatWith(globalKey));

            if (remainingFieldsOrProperties != null && gv != null)
                this.Members = ParsedModel.GetMembers(gv.Type, remainingFieldsOrProperties, addError);
        }

        public override object GetValue(TemplateParameters p)
        {
            object value = GlobalVariables[globalKey].GetValue(p);
            
            if (value == null)
                return null;

            if (Members != null)
            {
                foreach (var m in Members)
                {
                    value = ModelValueProvider.Getter(m, value);
                    if (value == null)
                        break;
                }
            }

            return value;
        }

        public override string Format
        {
            get { return Reflector.FormatString(Type); }
        }

        public override Type Type
        {
            get
            {
                if (remainingFieldsOrProperties.HasText())
                    return Members.Try(ms => ms.Last().ReturningType().Nullify());
                else
                    return GlobalVariables.TryGetC(globalKey).Try(v => v.Type);
            }
        }

        public override void FillQueryTokens(List<QueryToken> list)
        {
        }

        public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables, string afterToken)
        {
            sb.Append("[g:");
            sb.Append(globalKey);
            if (remainingFieldsOrProperties.HasText())
            {
                sb.Append(".");
                sb.Append(Members == null ? remainingFieldsOrProperties : Members.ToString(a => a.Name, "."));
            }
            sb.Append(afterToken);
            sb.Append("]");

            if (Variable.HasItems())
                sb.Append(" as " + Variable);
        }

        public override void Synchronize(SyncronizationContext sc, string p)
        {
            globalKey = sc.Replacements.SelectInteractive(globalKey, GlobalVariables.Keys, "Globals", sc.StringDistance) ?? globalKey;

            if(remainingFieldsOrProperties.HasText() && Members == null)
            {
                Members = sc.GetMembers(remainingFieldsOrProperties, GlobalVariables[globalKey].Type);

                if (Members != null)
                    remainingFieldsOrProperties = Members.ToString(a => a.Name, ".");
            }

            Declare(sc.Variables);
        }
    }
}
