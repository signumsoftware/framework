using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Mailing;
using Signum.Engine.Translation;
using Signum.Engine.UserQueries;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Mailing;
using Signum.Entities.Reflection;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using Signum.Utilities.DataStructures;

namespace Signum.Engine.Mailing
{
    public static partial class EmailTemplateParser
    {
        public class ParsedToken
        {
            public string String;
            public QueryToken QueryToken;
            public string Variable;

            public string SimplifyToken(ScopedDictionary<string, ParsedToken> variables, string token)
            {
                var variable = (from kvp in variables
                                let t = kvp.Value.QueryToken.FullKey()
                                where token == t || token.StartsWith(t + ".")
                                orderby t.Length descending
                                select kvp).FirstOrDefault();

                if (variable.Key.HasText())
                {
                    var fullKey = variable.Value.QueryToken.FullKey();

                    return variable.Key + token.RemoveStart(fullKey.Length);
                }

                return token;
            }

            internal void ToString(StringBuilder sb, ScopedDictionary<string, ParsedToken> variables, string afterToken)
            {
                sb.Append("[");
                sb.Append(QueryToken == null ? String : SimplifyToken(variables, QueryToken.FullKey()));

                if (afterToken.HasItems())
                    sb.Append(afterToken);

                sb.Append("]");

                if (Variable.HasItems())
                    sb.Append(" as " + Variable);
            }

            internal void Declare(ScopedDictionary<string, ParsedToken> newVars)
            {
                if (Variable.HasText())
                    newVars.Add(Variable, this);
            }
        }

        public abstract class TextNode
        {
            public abstract void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows);
            public abstract void FillQueryTokens(List<QueryToken> list);

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                ScopedDictionary<string, ParsedToken> variables = new ScopedDictionary<string, ParsedToken>(null);
                ToString(sb, variables);
                return sb.ToString();
            }

            public abstract void ToString(StringBuilder sb, ScopedDictionary<string, ParsedToken> variables);

            public abstract void Synchronize(SyncronizationContext sc);
        }

        public class LiteralNode : TextNode
        {
            public string Text;

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                p.StringBuilder.Append(Text);
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                return;
            }

            public override void ToString(StringBuilder sb, ScopedDictionary<string, ParsedToken> variables)
            {
                sb.Append(Text);               
            }

            public override void Synchronize(SyncronizationContext sc)
            {
                return;
            }
        }

        public class DeclareNode : TextNode
        {
            public readonly ParsedToken Token;

            internal DeclareNode(ParsedToken token, TemplateWalker walker)
            {
                if (!token.Variable.HasText())
                    walker.AddError(true, "declare[{0}] should end with 'as $someVariable'".Formato(token));

                this.Token = token;
            }

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
            }

            public override void ToString(StringBuilder sb, ScopedDictionary<string, ParsedToken> variables)
            {
                sb.Append("@declare");

                Token.ToString(sb, variables, null);

                Token.Declare(variables);
            }

            public override void Synchronize(SyncronizationContext sc)
            {
                sc.SynchronizeToken(Token, "@declare");

                Token.Declare(sc.Variables);
            }
        }

        public class TokenNode : TextNode
        {
            public readonly bool IsRaw;

            public readonly ParsedToken Token;
            public readonly QueryToken EntityToken; 
            public readonly string Format;
            public readonly PropertyRoute Route;
            internal TokenNode(ParsedToken token, string format, bool isRaw, TemplateWalker walker)
            {
                this.Token = token;
                this.Format = format;
                this.IsRaw = isRaw;

                if (token.QueryToken != null && IsTranslateInstanceCanditate(token.QueryToken))
                {
                    Route = token.QueryToken.GetPropertyRoute();
                    string error = DeterminEntityToken(token.QueryToken, out EntityToken);
                    if (error != null)
                        walker.AddError(false, error);
                }
            }

            static bool IsTranslateInstanceCanditate(QueryToken token)
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

            string DeterminEntityToken(QueryToken token, out QueryToken entityToken)
            {
                entityToken = token.Follow(a => a.Parent).FirstOrDefault(a => a.Type.IsLite() || a.Type.IsIIdentifiable());

                if (entityToken == null)
                    entityToken = QueryUtils.Parse("Entity", DynamicQueryManager.Current.QueryDescription(token.QueryName), 0);

                if (entityToken.Type.IsAssignableFrom(Route.RootType))
                    return "The entity of {0} ({1}) is not compatible with the property route {2}".Formato(token.FullKey(), entityToken.FullKey(), Route.RootType.NiceName());

                return null;
            }

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                string text;
                if (EntityToken != null)
                {
                    var entity = (Lite<IdentifiableEntity>)rows.DistinctSingle(p.Columns[EntityToken]);
                    var fallback = (string)rows.DistinctSingle(p.Columns[Token.QueryToken]);

                    text = entity == null ? null : TranslatedInstanceLogic.TranslatedField(entity, Route, fallback);
                }
                else
                {
                    object obj = rows.DistinctSingle(p.Columns[Token.QueryToken]);
                    text = obj is Enum ? ((Enum)obj).NiceToString() : 
                        obj is IFormattable ? ((IFormattable)obj).ToString(Format ?? Token.QueryToken.Format, p.CultureInfo) :
                        obj.TryToString();
                }
                p.StringBuilder.Append(p.IsHtml && !IsRaw ? HttpUtility.HtmlEncode(text) : text);
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                list.Add(Token.QueryToken);
                if (EntityToken != null)
                    list.Add(EntityToken);
            }

            public override void ToString(StringBuilder sb, ScopedDictionary<string, ParsedToken> variables)
            {
                sb.Append("@");
                if (IsRaw)
                    sb.Append("raw");

                Token.ToString(sb, variables, Format.HasText() ? (":" + Format) : null);
            }

            public override void Synchronize(SyncronizationContext sc)
            {
                sc.SynchronizeToken(Token, IsRaw ? "@raw[]" : "@[]");

                Token.Declare(sc.Variables);
            }
        }

        public class GlobalNode : TextNode
        {
            Func<GlobalVarContext, object> globalFunc;
            string globalKey;
            internal GlobalNode(string globalKey, TemplateWalker walker)
            {
                this.globalKey = globalKey;
                this.globalFunc = EmailTemplateParser.GlobalVariables.TryGet(globalKey, null);
                if (globalFunc == null)
                    walker.AddError(false, "The global key {0} was not found".Formato(globalKey));
            }

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                var text = globalFunc(new GlobalVarContext { Entity = p.Entity, Culture = p.CultureInfo, IsHtml = p.IsHtml, SystemEmail = p.SystemEmail, });
                if (text is IHtmlString)
                    p.StringBuilder.Append(((IHtmlString)text).ToHtmlString());

                p.StringBuilder.Append(p.IsHtml ? HttpUtility.HtmlEncode(text) : text);
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
            }

            public override void ToString(StringBuilder sb, ScopedDictionary<string, ParsedToken> variables)
            {
                sb.AppendFormat("@global[{0}]", globalKey);
            }

            public override void Synchronize(SyncronizationContext sc)
            {
                globalKey = sc.Replacements.SelectInteractive(globalKey, GlobalVariables.Keys, "EmailTemplate Globals", sc.StringDistance) ?? globalKey;
            }
        }

        public class ModelNode : TextNode
        {
            public bool IsRaw { get; set; }

            string fieldOrPropertyChain; 
            List<MemberInfo> members;
            internal ModelNode(string fieldOrPropertyChain, Type systemEmail, TemplateWalker walker)
            {
                if (systemEmail == null)
                {
                    walker.AddError(false, EmailTemplateMessage.SystemEmailShouldBeSetToAccessModel0.NiceToString().Formato(fieldOrPropertyChain));
                    return;
                }

                this.fieldOrPropertyChain = fieldOrPropertyChain;

                members = new List<MemberInfo>();
                var type = systemEmail;
                foreach (var field in fieldOrPropertyChain.Split('.'))
                {
                    var info = (MemberInfo)type.GetField(field, flags) ??
                               (MemberInfo)type.GetProperty(field, flags);

                    if (info == null)
                    {
                        walker.AddError(false, EmailTemplateMessage.Type0DoesNotHaveAPropertyWithName1.NiceToString().Formato(type.Name, field));
                        members = null;
                        break;
                    }

                    members.Add(info);

                    type = info.ReturningType();
                }
            }

            public const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                if (p.SystemEmail == null)
                    throw new ArgumentException("There is no system email for the message composition");

                object value = p.SystemEmail;
                foreach (var m in members)
                {
                    value = Getter(m, value);
                    if (value == null)
                        break;
                }

                if (p.IsHtml && !(value is System.Web.HtmlString) && !IsRaw)
                    p.StringBuilder.Append(HttpUtility.HtmlEncode(value.ToString()));
                else
                    p.StringBuilder.Append(value.ToString());
            }

            static object Getter(MemberInfo member, object systemEmail)
            {
                var pi = member as PropertyInfo;

                if (pi != null)
                    return pi.GetValue(systemEmail, null);

                return ((FieldInfo)member).GetValue(systemEmail);
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                return;
            }

            public override void ToString(StringBuilder sb, ScopedDictionary<string, ParsedToken> variables)
            {
                sb.AppendFormat("@model[{0}]", members == null ? fieldOrPropertyChain : members.ToString(a => a.Name, "."));
            }

            public override void Synchronize(SyncronizationContext sc)
            {
                if (members != null)
                {
                    members = GetNewModel(sc.ModelType, fieldOrPropertyChain, sc.Replacements, sc.StringDistance);

                    if (members != null)
                        fieldOrPropertyChain = members.ToString(a => a.Name, ".");
                }
            }

            internal static List<MemberInfo> GetNewModel(Type type, string fieldOrPropertyChain, Replacements replacements, StringDistance sd)
            {
                List<MemberInfo> fields = new List<MemberInfo>();

                foreach (var field in fieldOrPropertyChain.Split('.'))
                {
                    var allMembers = type.GetFields(flags).Cast<MemberInfo>().Concat(type.GetProperties(flags)).ToDictionary(a => a.Name);
                    
                    string s = replacements.SelectInteractive(field, allMembers.Keys, "Members {0}".Formato(type.FullName), sd);

                    if (s == null)
                        return null;

                    var member = allMembers.GetOrThrow(s);

                    fields.Add(member);

                    type = member.ReturningType();
                }

                return fields;
            }
        }

        public sealed class BlockNode : TextNode
        {
            public readonly List<TextNode> Nodes = new List<TextNode>();

            public readonly TextNode owner;

            public BlockNode(TextNode owner)
            {
                this.owner = owner;
            }

            public string Print(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                this.PrintList(p, rows);
                return p.StringBuilder.ToString();
            }

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                foreach (var node in Nodes)
                {
                    node.PrintList(p, rows);
                }
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                foreach (var node in Nodes)
                {
                    node.FillQueryTokens(list);
                }
            }

            public static string UserString(Type type)
            {
                if (type == typeof(ForeachNode))
                    return "foreach";

                if (type == typeof(IfNode))
                    return "if";

                if (type == typeof(AnyNode))
                    return "any";

                return "block";
            }

            public override void ToString(StringBuilder sb, ScopedDictionary<string, ParsedToken> variables)
            {
                foreach (var n in Nodes)
                {
                    n.ToString(sb, variables);
                }
            }

            public override void Synchronize(SyncronizationContext sc)
            {
                foreach (var item in Nodes)
                    item.Synchronize(sc);
            }
            
        }

        public class ForeachNode : TextNode
        {
            public readonly ParsedToken Token;

            public readonly BlockNode Block;

            public ForeachNode(ParsedToken token)
            {
                this.Token = token;
                this.Block = new BlockNode(this);
            }

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                var groups = rows.GroupBy(r => r[p.Columns[Token.QueryToken]]).ToList();
                if (groups.Count == 1 && groups[0].Key == null)
                    return;
                foreach (var group in groups)
                {
                    Block.PrintList(p, group);
                }
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                list.Add(Token.QueryToken);
                Block.FillQueryTokens(list);
            }

            public override void ToString(StringBuilder sb, ScopedDictionary<string, ParsedToken> variables)
            {
                sb.Append("@foreach");
                Token.ToString(sb, variables, null);
                {
                    var newVars = new ScopedDictionary<string, ParsedToken>(variables);
                    Token.Declare(newVars);
                    Block.ToString(sb, newVars);
                }

                sb.Append("@endforeach");
            }

            public override void Synchronize(SyncronizationContext sc)
            {
                sc.SynchronizeToken(Token, "@foreach[]");

                using (sc.NewScope())
                {
                    Token.Declare(sc.Variables);

                    Block.Synchronize(sc);
                }
            }
        }

        public class AnyNode : TextNode
        {
            public readonly ParsedToken Token;
            public readonly FilterOperation? Operation;
            public string Value;

            public readonly BlockNode AnyBlock;
            public BlockNode NotAnyBlock;

            internal AnyNode(ParsedToken token, TemplateWalker walker)
            {
                if (token.QueryToken != null && token.QueryToken.HasAllOrAny())
                    walker.AddError(false, "Any {0} can not contains Any or All");

                AnyBlock = new BlockNode(this);
            }

            internal AnyNode(ParsedToken token, string operation, string value, TemplateWalker walker)
            {
                if (token.QueryToken != null && token.QueryToken.HasAllOrAny())
                    walker.AddError(false, "Any {0} can not contains Any or All");

                this.Token = token;
                this.Operation = FilterValueConverter.ParseOperation(operation);
                this.Value = value;
                
                if (Token.QueryToken != null)
                {
                    object rubish;
                    string error = FilterValueConverter.TryParse(Value, Token.QueryToken.Type, out rubish, Operation == FilterOperation.IsIn);

                    if (error.HasText())
                        walker.AddError(false, error);
                }

                AnyBlock = new BlockNode(this);
            }

            public BlockNode CreateNotAny()
            {
                NotAnyBlock = new BlockNode(this);
                return NotAnyBlock;
            }

            protected static bool ToBool(object obj)
            {
                if (obj == null || obj is bool && ((bool)obj) == false)
                    return false;

                return true;
            }

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                if (Operation == null)
                {
                    var column = p.Columns[Token.QueryToken];

                    var filtered = rows.Where(r => ToBool(r[column])).ToList();
                    if (filtered.Any())
                    {
                        AnyBlock.PrintList(p, filtered);
                    }
                    else if (NotAnyBlock != null)
                    {
                        NotAnyBlock.PrintList(p, filtered);
                    }
                }
                else
                {
                    object val = FilterValueConverter.Parse(Value, Token.QueryToken.Type, Operation == FilterOperation.IsIn);

                    Expression value = Expression.Constant(val, Token.QueryToken.Type);

                    ResultColumn col = p.Columns[Token.QueryToken];

                    var expression = Signum.Utilities.ExpressionTrees.Linq.Expr((ResultRow rr) => rr[col]);

                    Expression newBody = QueryUtils.GetCompareExpression(Operation.Value, Expression.Convert(expression.Body, Token.QueryToken.Type), value, inMemory: true);
                    var lambda = Expression.Lambda<Func<ResultRow, bool>>(newBody, expression.Parameters).Compile();

                    var filtered = rows.Where(lambda).ToList();
                    if (filtered.Any())
                    {
                        AnyBlock.PrintList(p, filtered);
                    }
                    else if (NotAnyBlock != null)
                    {
                        NotAnyBlock.PrintList(p, filtered);
                    }
                }
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                list.Add(Token.QueryToken);
                AnyBlock.FillQueryTokens(list);
                if (NotAnyBlock != null)
                    NotAnyBlock.FillQueryTokens(list);
            }


            public override void ToString(StringBuilder sb, ScopedDictionary<string, ParsedToken> variables)
            {
                sb.Append("@any");
                Token.ToString(sb, variables, Operation == null ? null : FilterValueConverter.ToStringOperation(Operation.Value) + Value);
                {
                    var newVars = new ScopedDictionary<string, ParsedToken>(variables);
                    Token.Declare(newVars);
                    AnyBlock.ToString(sb, newVars);
                }
                
                if (NotAnyBlock != null)
                {
                    sb.Append("@notany");
                    var newVars = new ScopedDictionary<string, ParsedToken>(variables);
                    Token.Declare(newVars);
                    NotAnyBlock.ToString(sb, newVars);
                }

                sb.Append("@endany");
            }

            public override void Synchronize(SyncronizationContext sc)
            {
                sc.SynchronizeToken(Token, "@any[]");

                if (Operation != null)
                    sc.SynchronizeValue(Token, ref Value, Operation == FilterOperation.IsIn);

                using (sc.NewScope())
                {
                    Token.Declare(sc.Variables);

                    AnyBlock.Synchronize(sc);
                }

                if (NotAnyBlock != null)
                {
                    using (sc.NewScope())
                    {
                        Token.Declare(sc.Variables);

                        NotAnyBlock.Synchronize(sc);
                    }
                }
            }
        }

        public class IfNode : TextNode
        {
            public readonly ParsedToken Token;
            public readonly BlockNode IfBlock;
            public BlockNode ElseBlock;
            private FilterOperation? Operation;
            private string Value;

            internal IfNode(ParsedToken token, TemplateWalker walker)
            {
                this.Token = token;
                this.IfBlock = new BlockNode(this);
            }

            internal IfNode(ParsedToken token, string operation, string value, TemplateWalker walker)
            {
                this.Token = token;
                this.Operation = FilterValueConverter.ParseOperation(operation);
                this.Value = value;

                if (Token.QueryToken != null)
                {
                    object rubish;
                    string error = FilterValueConverter.TryParse(Value, Token.QueryToken.Type, out rubish, Operation == FilterOperation.IsIn);

                    if (error.HasText())
                        walker.AddError(false, error);
                }


                this.IfBlock = new BlockNode(this);
            }

            public BlockNode CreateElse()
            {
                ElseBlock = new BlockNode(this);
                return ElseBlock;
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                list.Add(Token.QueryToken);
                IfBlock.FillQueryTokens(list);
                if (ElseBlock != null)
                    ElseBlock.FillQueryTokens(list);
            }

            protected static bool ToBool(object obj)
            {
                if (obj == null || obj is bool && ((bool)obj) == false)
                    return false;

                return true;
            }

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                if (Operation == null)
                {
                    if (!rows.IsEmpty() &&  ToBool(rows.DistinctSingle(p.Columns[Token.QueryToken])))
                    {
                        IfBlock.PrintList(p, rows);
                    }
                    else if (ElseBlock != null)
                    {
                        ElseBlock.PrintList(p, rows);
                    }
                }
                else
                {
                    Expression token = Expression.Constant(rows.DistinctSingle(p.Columns[Token.QueryToken]), Token.QueryToken.Type);

                    Expression value = Expression.Constant(FilterValueConverter.Parse(Value, Token.QueryToken.Type, Operation == FilterOperation.IsIn), Token.QueryToken.Type);

                    Expression newBody = QueryUtils.GetCompareExpression(Operation.Value,  token, value, inMemory: true);
                    var lambda = Expression.Lambda<Func<bool>>(newBody).Compile();

                    if (lambda())
                    {
                        IfBlock.PrintList(p, rows);
                    }
                    else if (ElseBlock != null)
                    {
                        ElseBlock.PrintList(p, rows);
                    }
                }
            }

            public override void ToString(StringBuilder sb, ScopedDictionary<string, ParsedToken> variables)
            {
                sb.Append("@if");
                Token.ToString(sb, variables, Operation == null ? null : FilterValueConverter.ToStringOperation(Operation.Value) + Value);
                {
                    var newVars = new ScopedDictionary<string, ParsedToken>(variables);
                    Token.Declare(newVars);
                    IfBlock.ToString(sb, newVars);
                }

                if (ElseBlock != null)
                {
                    sb.Append("@else");
                    var newVars = new ScopedDictionary<string, ParsedToken>(variables);
                    Token.Declare(newVars);
                    ElseBlock.ToString(sb, newVars);
                }

                sb.Append("@endif");
            }


            public override void Synchronize(SyncronizationContext sc)
            {
                sc.SynchronizeToken(Token, "@if[]");

                if (Operation != null)
                    sc.SynchronizeValue(Token, ref Value, Operation == FilterOperation.IsIn);

                using (sc.NewScope())
                {
                    Token.Declare(sc.Variables);

                    IfBlock.Synchronize(sc);
                }

                if (ElseBlock != null)
                {
                    using (sc.NewScope())
                    {
                        Token.Declare(sc.Variables);

                        ElseBlock.Synchronize(sc);
                    }
                }
            }
        }
    }
}
