using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using System.Reflection;
using Signum.Engine.Mailing;
using System.Web;
using System.Text.RegularExpressions;
using System.Globalization;
using Signum.Entities;
using Signum.Entities.Mailing;
using Signum.Entities.UserQueries;
using System.Linq.Expressions;


namespace Signum.Engine.Mailing
{
    public class GlobalVarContext
    {
        public IIdentifiable Entity;
        public CultureInfo Culture;
        public bool IsHtml;
        public ISystemEmail SystemEmail;
    }

    public static class EmailTemplateParser
    {
        public static Dictionary<string, Func<GlobalVarContext, object>> GlobalVariables = new Dictionary<string, Func<GlobalVarContext, object>>();

        public static object DistinctSingle(this IEnumerable<ResultRow> rows, ResultColumn column)
        {
            return rows.Select(r => r[column]).Distinct().SingleEx(
                () =>"No values for column {0}".Formato(column.Column.Token.FullKey()),
                () =>"Multiple values for column {0}".Formato(column.Column.Token.FullKey()));
        }

        public static readonly Regex KeywordsRegex = new Regex(@"\@(((?<keyword>(foreach|if|raw|global|model|modelraw|any|))\[(?<token>[^\]]+)\])|(?<keyword>endforeach|else|endif|notany|endany))");

        public static readonly Regex TokenFormatRegex = new Regex(@"(?<token>[^\]\:]+)(\:(?<format>.*))?");
        public static readonly Regex TokenOperationValueRegex = new Regex(@"(?<token>[^\]]+)(?<comparer>(" + FilterValueConverter.OperationRegex + @"))(?<value>[^\]\:]+)");

        public abstract class TextNode
        {
            public abstract void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows);
            public abstract void FillQueryTokens(List<QueryToken> list);
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

            public override string ToString()
            {
                return "literal {0}".Formato(Text.Etc(20));
            }
        }

        public class TokenNode : TextNode
        {
            public bool IsRaw { get; set; }

            public readonly QueryToken Token;
            public readonly string Format;
            public TokenNode(QueryToken token, string format)
            {
                this.Token = token;
                this.Format = format;
            }

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                ResultColumn column = p.Columns[Token];
                object obj = rows.DistinctSingle(column);
                var text = obj is IFormattable ? ((IFormattable)obj).ToString(Format ?? Token.Format, p.CultureInfo) : obj.TryToString();
                p.StringBuilder.Append(p.IsHtml && !IsRaw ? HttpUtility.HtmlEncode(text) : text);
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                list.Add(Token);
            }

            public override string ToString()
            {
                return "token {0}".Formato(Token.FullKey());
            }

       
        }

        public class GlobalNode : TextNode
        {
            Func<GlobalVarContext, object> globalFunc;
            string globalKey;
            public GlobalNode(string globalKey, List<string> errors)
            {
                this.globalKey = globalKey;
                this.globalFunc = GlobalVariables.TryGet(globalKey, null);
                if (globalFunc == null)
                    errors.Add("The global key {0} was not found".Formato(globalKey));
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
                return;
            }

            public override string ToString()
            {
                return "global {0}".Formato(globalKey);
            }
        }

        public class ModelNode : TextNode
        {
            public bool IsRaw { get; set; }

            List<MemberInfo> member;
            public ModelNode(string fieldOrPropertyChain, Type modelType, List<string> errors)
            {
                if (modelType == null)
                {
                    errors.Add(EmailTemplateMessage.SystemEmailShouldBeSetToAccessModel0.NiceToString().Formato(fieldOrPropertyChain));
                    return;
                }

                member = new List<MemberInfo>();
                var type = modelType;
                foreach (var field in fieldOrPropertyChain.Split('.'))
                {
                    var info = (MemberInfo)type.GetField(field, flags) ??
                               (MemberInfo)type.GetProperty(field, flags);

                    if (info == null)
                    {
                        errors.Add(EmailTemplateMessage.Type0DoesNotHaveAPropertyWithName1.NiceToString().Formato(type.Name, field));
                        break;
                    }

                    member.Add(info);

                    type = info.ReturningType();
                }
            }
          
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                if (p.SystemEmail == null)
                    throw new ArgumentException("There is no system email for the message composition");

                object value = p.SystemEmail;
                foreach (var m in member)
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

            public override string ToString()
            {
                return "model {0}".Formato(member);
            }
        }

        public sealed class BlockNode : TextNode
        {
            public readonly List<TextNode> Nodes = new List<TextNode>();

            public readonly TextNode Parent; 

            public BlockNode(TextNode parent)
            {
                this.Parent = parent;
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

            public override string ToString()
            {
                return "block ({0} nodes)".Formato(Nodes.Count);
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
        }

        public class ForeachNode : TextNode
        {
            public readonly QueryToken Token;

            public readonly BlockNode Block;

            public ForeachNode(QueryToken token)
            {
                this.Token = token;
                this.Block = new BlockNode(this);
            }

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                var groups = rows.GroupBy(r => r[p.Columns[Token]]).ToList();
                if (groups.Count == 1 && groups[0].Key == null)
                    return;
                foreach (var group in groups)
                {
                    Block.PrintList(p, group);
                }
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                list.Add(Token);
                Block.FillQueryTokens(list);
            }

            public override string ToString()
            {
                return "foreach {0} ({1} nodes)".Formato(Token.FullKey(), Block.Nodes.Count);
            }
        }

        public class AnyNode : TextNode
        {
            public readonly QueryToken Token;
            public readonly FilterOperation Operation;
            public readonly string Value;

            public readonly BlockNode AnyBlock;
            public BlockNode NotAnyBlock;


            public AnyNode(QueryToken token, string operation, string value, List<string> errors)
            {
                if (token.HasAllOrAny())
                    errors.Add("Where {0} can not contains Any or All");

                this.Token = token;
                this.Operation = FilterValueConverter.ParseOperation(operation);
                this.Value = value;
                object rubish;
                string error = FilterValueConverter.TryParse(Value, Token.Type, out rubish);

                if (error.HasText())
                    errors.Add(error);

                AnyBlock = new BlockNode(this);
            }

            public BlockNode CreateNotAny()
            {
                NotAnyBlock = new BlockNode(this);
                return NotAnyBlock;
            }

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                object val = FilterValueConverter.Parse(Value, Token.Type);

                Expression value = Expression.Constant(val, Token.Type); 

                var col = p.Columns[Token];

                var example = Signum.Utilities.ExpressionTrees.Linq.Expr((ResultRow rr)=>rr[col]);

                var newBody = QueryUtils.GetCompareExpression(Operation, Expression.Convert(example.Body, Token.Type), value, inMemory: true);
                var lambda = Expression.Lambda<Func<ResultRow, bool>>(newBody, example.Parameters).Compile();

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

            public override void FillQueryTokens(List<QueryToken> list)
            {
                list.Add(Token);
                AnyBlock.FillQueryTokens(list);
                if (NotAnyBlock != null)
                    NotAnyBlock.FillQueryTokens(list);
            }

            public override string ToString()
            {
                return " ".CombineIfNotEmpty(
                    "any {0} {1} {2} ({3} nodes)".Formato(Token.FullKey(), FilterValueConverter.ToStringOperation(Operation), Value, AnyBlock.Nodes.Count),
                    NotAnyBlock == null ? null : "notany ({0} nodes)".Formato(NotAnyBlock.Nodes.Count));
            }
        }

        public class IfNode : TextNode
        {
             public readonly QueryToken Token;
             public readonly BlockNode IfBlock;
             public BlockNode ElseBlock;

             public IfNode(QueryToken token, List<string> errors)
             {
                 this.Token = token;
                 this.IfBlock = new BlockNode(this);
             }

             public BlockNode CreateElse()
             {
                 ElseBlock = new BlockNode(this);
                 return ElseBlock;
             }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                list.Add(Token);
                IfBlock.FillQueryTokens(list);
                if (ElseBlock != null)
                    ElseBlock.FillQueryTokens(list);
            }

            protected static bool ToBool(IEnumerable<ResultRow> rows, ResultColumn column)
            {
                if (rows.IsEmpty())
                    return false;

                object obj = rows.DistinctSingle(column);

                if (obj == null || obj is bool && ((bool)obj) == false)
                    return false;

                return true;
            } 

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                if (ToBool(rows, p.Columns[Token]))
                {
                    IfBlock.PrintList(p, rows);
                }
                else if(ElseBlock != null)
                {
                    ElseBlock.PrintList(p, rows);
                }
            }

            public override string ToString()
            {
                return " ".CombineIfNotEmpty("if {0} ({1} nodes)".Formato(Token.FullKey(), IfBlock.Nodes.Count),
                    ElseBlock != null ? "else ({0} nodes)".Formato(ElseBlock.Nodes.Count) : "");
            }
        }

        public static BlockNode Parse(string text, QueryDescription qd, Type modelType)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            if (qd == null)
                throw new ArgumentNullException("qd");

            BlockNode node;
            var errors = TryParseTemplate(text, qd, modelType, out node);
            if (errors.Any())
                throw new FormatException(errors.ToString("\r\n"));
            return node;
        }


        static List<string> TryParseTemplate(string text, QueryDescription qd, Type modelType, out BlockNode mainBlock)
        {
            List<string> errors = new List<string>();

            var matches = KeywordsRegex.Matches(text);

            Stack<BlockNode> stack = new Stack<BlockNode>();
            mainBlock = new BlockNode(null);
            stack.Push(mainBlock);

            if (matches.Count == 0)
            {
                stack.Peek().Nodes.Add(new LiteralNode { Text = text });
                stack.Pop();
                return errors;
            }

            Func<string, QueryToken> tryParseToken = token =>
            {
                QueryToken result = null;
                try
                {
                    result = QueryUtils.Parse(token, qd, false);
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                    return null;
                }
                return result;
            };

            Func<Type, BlockNode> popNode = type =>
            {
                if (stack.Count() <= 1)
                {
                    errors.Add("No {0} has been opened".Formato(BlockNode.UserString(type)));
                    return null;
                }
                var n = stack.Pop();
                if (n.Parent == null || n.Parent.GetType() != type)
                {
                    errors.Add("Unexpected '{0}'".Formato(BlockNode.UserString(n.Parent.TryCC(p => p.GetType()))));
                    return null;
                }
                return n;
            };

            int index = 0;
            foreach (Match match in matches)
            {
                if (index < match.Index)
                {
                    stack.Peek().Nodes.Add(new LiteralNode { Text = text.Substring(index, match.Index - index) });
                }
                var token = match.Groups["token"].Value;
                var keyword = match.Groups["keyword"].Value;
                switch (keyword)
                {
                    case "":
                    case "raw":
                        var tok = TokenFormatRegex.Match(token);
                        if (!tok.Success)
                            errors.Add("{0} has invalid format".Formato(token)); 
                        else
                            stack.Peek().Nodes.Add(new TokenNode(tryParseToken(tok.Groups["token"].Value), tok.Groups["format"].Value){ IsRaw = keyword == "raw"});
                        break;
                    case "global":
                        stack.Peek().Nodes.Add(new GlobalNode(token, errors));
                        break;
                    case "model":
                    case "modelraw":
                        stack.Peek().Nodes.Add(new ModelNode(token, modelType, errors) { IsRaw = keyword == "modelraw" });
                        break;
                    case "any":
                        {
                            var filter = TokenOperationValueRegex.Match(token);
                            if (!filter.Success)
                            {
                                errors.Add("{0} has invalid format".Formato(token));
                            }
                            else
                            {
                                var t = tryParseToken(filter.Groups["token"].Value);
                                var comparer = filter.Groups["comparer"].Value;
                                var value = filter.Groups["value"].Value;
                                var any = new AnyNode(t, comparer, value, errors);
                                stack.Peek().Nodes.Add(any);
                                stack.Push(any.AnyBlock);
                            }
                            break;
                        }
                    case "notany":
                        {
                            var an = (AnyNode)popNode(typeof(AnyNode)).Parent;
                            stack.Push(an.CreateNotAny());
                            break;
                        }
                    case "endany":
                        {
                            popNode(typeof(AnyNode));
                            break;
                        }
                    case "foreach":
                        {
                            var fn = new ForeachNode(tryParseToken(token));
                            stack.Peek().Nodes.Add(fn);
                            stack.Push(fn.Block);
                            break;
                        }
                    case "endforeach":
                        {
                            popNode(typeof(ForeachNode));
                        }
                        break;
                    case "if":
                        {
                            var ifn = new IfNode(tryParseToken(token), errors);
                            stack.Peek().Nodes.Add(ifn);
                            stack.Push(ifn.IfBlock);
                            break;
                        }
                    case "else":
                        {
                            var ifn = (IfNode)popNode(typeof(IfNode)).Parent;
                            stack.Push(ifn.CreateElse());
                            break;
                        }
                    case "endif":
                        {
                            popNode(typeof(IfNode));
                            break;
                        }
                    default:
                        break;
                }
                index = match.Index + match.Length;
            }
            if (stack.Count != 1)
                errors.Add("Last block is not closed: {0}".Formato(stack.Peek()));
            var lastM = matches.Cast<Match>().LastOrDefault();
            if (lastM != null && lastM.Index + lastM.Length < text.Length)
                stack.Peek().Nodes.Add(new LiteralNode { Text = text.Substring(lastM.Index + lastM.Length) });
            stack.Pop();
            return errors;
        }



        internal static void ReplaceToken(EmailTemplateDN et, QueryTokenDN item, QueryTokenDN token)
        {
            foreach (var tok in et.Recipients.Where(r => r.Token == item).ToList())
            {
                tok.Token = token;
            }

            foreach (var m in et.Messages)
            {
                m.Subject = ReplaceTokenText(m.Subject, item, token);
                m.Text = ReplaceTokenText(m.Text, item, token);
            }
        }

        private static string ReplaceTokenText(string text, QueryTokenDN item, QueryTokenDN token)
        {
            var result = KeywordsRegex.Replace(text, m =>
            {
                var gr = m.Groups["token"];

                if (!gr.Success)
                    return m.Value;

                if(!AreSimilar(gr.Value,item.TokenString))
                    return m.Value;

                var newKeyword = m.Value.Substring(0, gr.Index - m.Index)
                    + token.Token.FullKey()
                    + m.Value.Substring(gr.Index + gr.Length - m.Index);

                return newKeyword;
            });

            return result;
        }

        private static bool AreSimilar(string p1, string p2)
        {
            if (p1.StartsWith("Entity."))
                p1 = p1.After("Entity.");

            if (p2.StartsWith("Entity."))
                p2 = p2.After("Entity.");

            return p1 == p2;
        }
    }

    public class EmailTemplateParameters
    {
        public StringBuilder StringBuilder = new StringBuilder();
        public bool IsHtml;
        public CultureInfo CultureInfo;
        public IIdentifiable Entity;
        public ISystemEmail SystemEmail;
        public Dictionary<QueryToken, ResultColumn> Columns;
    }
}
