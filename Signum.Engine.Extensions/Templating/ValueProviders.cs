using Signum.Engine.DynamicQuery;
using Signum.Engine.Translation;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Mailing;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Templating
{
    public abstract class ValueProviderBase
    {
        public string Variable { get; set; }

        public abstract object GetValue(TemplateParameters p);

        public abstract void Foreach(TemplateParameters p, Action foreEachElement);

        public abstract string Format { get; }
        
        public abstract Type Type { get; }

        public abstract void FillQueryTokens(List<QueryToken> list);

        public abstract void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables, string afterToken);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ToString(sb, new ScopedDictionary<string, ValueProviderBase>(null), null);
            return sb.ToString();
        }

        public abstract void Synchronize(SyncronizationContext sc, string p);

        public abstract void Declare(ScopedDictionary<string, ValueProviderBase> variables);
    }

    public abstract class TemplateParameters
    {
        public IEntity Entity;
        public CultureInfo Culture;
        public Dictionary<QueryToken, ResultColumn> Columns;
        public IEnumerable<ResultRow> Rows;
        public abstract object GetModel();
    }

    public class TokenValueProvider : ValueProviderBase
    {
        public readonly ParsedToken ParsedToken;

        public TokenValueProvider (ParsedToken token)
        {
            this.ParsedToken = token;
        }

        public override object GetValue(TemplateParameters p)
        {
            if (p.Rows.IsEmpty())
                return null;

            return  p.Rows.DistinctSingle(p.Columns[ParsedToken.QueryToken]);
        }

        public override void Foreach(TemplateParameters p, Action foreEachElement)
        {
            var prevRows = p.Rows;
            var groups = p.Rows.GroupBy(r => r[p.Columns[ParsedToken.QueryToken]]).ToList();
            if (groups.Count == 1 && groups[0].Key == null)
                return;

            foreach (var group in groups)
            {
                p.Rows = group;
            }

            p.Rows = prevRows;
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
            ParsedToken.ToString(sb, variables, afterToken);
        }

        public override void Synchronize(SyncronizationContext sc, string remainingText)
        {
            sc.SynchronizeToken(ParsedToken, remainingText);

            Declare(sc.Variables);
        }

        public override Type Type
        {
            get { return ParsedToken.QueryToken.Type; }
        }

        public override void Declare(ScopedDictionary<string, ValueProviderBase> variables)
        {
            if (Variable.HasText())
                variables.Add(Variable, this);
        }
    }

    public class TranslateInstanceValueProvider : ValueProviderBase
    {
        public readonly ParsedToken ParsedToken;
        public readonly QueryToken EntityToken;
        public readonly PropertyRoute Route;

        public TranslateInstanceValueProvider(ParsedToken token, out string error)
        {
            this.ParsedToken = token;
            Route = token.QueryToken.GetPropertyRoute();
            EntityToken = DeterminEntityToken(token.QueryToken, out error);
        }

        public override object GetValue(TemplateParameters p)
        {
            var entity = (Lite<Entity>)p.Rows.DistinctSingle(p.Columns[EntityToken]);
            var fallback = (string)p.Rows.DistinctSingle(p.Columns[ParsedToken.QueryToken]);

            return entity == null ? null : TranslatedInstanceLogic.TranslatedField(entity, Route, fallback);
        }

        public override void Foreach(TemplateParameters parameters, Action foreEachElement)
        {
            throw new NotImplementedException("{0} can not be used to foreach".FormatWith(typeof(TranslateInstanceValueProvider).Name));
        }

        QueryToken DeterminEntityToken(QueryToken token, out  string error)
        {
            var entityToken = token.Follow(a => a.Parent).FirstOrDefault(a => a.Type.IsLite() || a.Type.IsIEntity());

            if (entityToken == null)
                entityToken = QueryUtils.Parse("Entity", DynamicQueryManager.Current.QueryDescription(token.QueryName), 0);

            if (entityToken.Type.IsAssignableFrom(Route.RootType))
                error = "The entity of {0} ({1}) is not compatible with the property route {2}".FormatWith(token.FullKey(), entityToken.FullKey(), Route.RootType.NiceName());
            else
                error = null;

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
            ParsedToken.ToString(sb, variables, afterToken);

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

        public override void Declare(ScopedDictionary<string, ValueProviderBase> variables)
        {
            if (Variable.HasText())
                variables.Add(Variable, this);
        }
    }

    public class ParsedToken
    {
        public string String;
        public QueryToken QueryToken;

        public static ParsedToken TryParseToken(string tokenString, SubTokensOptions options, QueryDescription qd, ScopedDictionary<string, ParsedToken> variables, out string error)
        {
            error = null;
            ParsedToken result = new ParsedToken { String = tokenString };

            if (tokenString.StartsWith("$"))
            {
                string v = tokenString.TryBefore('.') ?? tokenString;

                ParsedToken token;

                if (!variables.TryGetValue(v, out token))
                {
                    error = "Variable '{0}' is not defined at this scope".FormatWith(v);
                    return result;
                }

                var after = tokenString.TryAfter('.');

                tokenString = token.QueryToken.FullKey() + (after == null ? null : ("." + after));
            }

            try
            {
                result.QueryToken = QueryUtils.Parse(tokenString, qd, options);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            return result;
        }

        internal void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables, string afterToken)
        {
            sb.Append("[");
            sb.Append(QueryToken == null ? String : SimplifyToken(variables, QueryToken.FullKey()));

            if (afterToken.HasItems())
                sb.Append(afterToken);

            sb.Append("]");
        }

        internal string ToString(ScopedDictionary<string, ValueProviderBase> variables, string afterToken)
        {
            StringBuilder sb = new StringBuilder();
            ToString(sb, variables, afterToken);
            return sb.ToString();
        }

        string SimplifyToken(ScopedDictionary<string, ValueProviderBase> variables, string token)
        {
            var pair = (from kvp in variables
                            let tp = kvp.Value as TokenValueProvider
                            where tp != null
                            let fullKey = tp.ParsedToken.QueryToken.FullKey()
                            where token == fullKey || token.StartsWith(fullKey + ".")
                            orderby fullKey.Length descending
                            select new { kvp.Key, fullKey }).FirstOrDefault();

            if (pair.Key.HasText())
            {
                return pair.Key + token.RemoveStart(pair.fullKey.Length);
            }

            return token;
        }
    }

    public class ModelValueProvider : ValueProviderBase
    {
        string fieldOrPropertyChain;
        List<MemberInfo> Members;

        public ModelValueProvider(string fieldOrPropertyChain, Type systemEmail, out string error)
        {
            if (systemEmail == null)
            {
                error = EmailTemplateMessage.SystemEmailShouldBeSetToAccessModel0.NiceToString().FormatWith(fieldOrPropertyChain);
                return;
            }

            this.Members = ParsedModel.GetMembers(systemEmail, fieldOrPropertyChain, out error);
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

        public override void Foreach(TemplateParameters p, Action foreEachElement)
        {
            throw new NotImplementedException();
        }

        public override string Format
        {
            get { return Reflector.FormatString(this.Type); }
        }

        public override Type Type
        {
            get { return Members.Last().ReturningType().Nullify(); }
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
        }

        public override void Declare(ScopedDictionary<string, ValueProviderBase> variables)
        {
            throw new NotImplementedException();
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

        public GlobalValueProvider(string fieldOrPropertyChain, out string error)
        {
            globalKey = fieldOrPropertyChain.TryBefore('.') ?? fieldOrPropertyChain;
            remainingFieldsOrProperties = fieldOrPropertyChain.TryAfter('.');

            var gv = GlobalVariables.TryGetC(globalKey); 

            if (gv == null)
                error = "The global key {0} was not found".FormatWith(globalKey);
            else 
                error = null;
            
            if (fieldOrPropertyChain != null && gv != null)
                this.Members = ParsedModel.GetMembers(gv.Type, remainingFieldsOrProperties, out error);
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

        public override void Foreach(TemplateParameters p, Action foreEachElement)
        {
            throw new NotImplementedException();
        }

        public override string Format
        {
            get { return Reflector.FormatString(Type); }
        }

        public override Type Type
        {
            get { return Members == null ? GlobalVariables[globalKey].Type : Members.Last().ReturningType().Nullify(); }
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
                sb.Append(globalKey);
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
        }

        public override void Declare(ScopedDictionary<string, ValueProviderBase> variables)
        {
            throw new NotImplementedException();
        }
    }
}
