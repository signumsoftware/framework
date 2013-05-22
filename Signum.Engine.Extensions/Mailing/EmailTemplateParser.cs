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

namespace Signum.Engine.Mailing
{
    public class GlobalVarDispatcher
    {
        public IIdentifiable Entity;
        public CultureInfo Culture;
        public bool IsHtml;
    }

    public static class EmailTemplateParser
    {
        public static Dictionary<string, Func<GlobalVarDispatcher, string>> GlobalVariables = new Dictionary<string, Func<GlobalVarDispatcher, string>>();

        static object DistinctSingle(this IEnumerable<ResultRow> rows, ResultColumn column)
        {
            return rows.Select(r => r[column]).Distinct().SingleEx(() =>
                "Multiple values for column {0}".Formato(column.Column.Token.FullKey()));
        }

        public static readonly Regex TokenRegex = new Regex(@"\@(?<special>(foreach|endforeach|if|endif|global|model|))\[(?<token>[^\]]*)\]");

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
        }

        public class TokenNode : TextNode
        {
            public TokenNode(QueryToken token)
            {
                this.Token = token;
            }

            public readonly QueryToken Token;

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                ResultColumn column = p.Columns[Token];
                object obj = rows.DistinctSingle(column);
                var text = obj is IFormattable ? ((IFormattable)obj).ToString(Token.Format, p.CultureInfo) : obj.TryToString();
                p.StringBuilder.Append(p.IsHtml ? HttpUtility.HtmlEncode(text) : text);
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                list.Add(Token);
            }
        }

        public class GlobalNode : TextNode
        {
            Func<GlobalVarDispatcher, string> globalFunc;

            public GlobalNode(string globalKey, List<string> errors)
            {
                globalFunc = GlobalVariables.TryGet(globalKey, null);
                if (globalFunc == null)
                    errors.Add("The global key {0} was not found".Formato(globalKey));
            }

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                var text = globalFunc(new GlobalVarDispatcher { Entity = p.Entity, Culture = p.CultureInfo, IsHtml = p.IsHtml });
                p.StringBuilder.Append(text);
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                return;
            }
        }

        public class ModelNode : TextNode
        {
            public ModelNode(string fieldOrProperty, Type modelType, List<string> errors)
            {
                var Member = (MemberInfo)modelType.GetField(fieldOrProperty, flags) ??
                       (MemberInfo)modelType.GetProperty(fieldOrProperty, flags);

                if (Member == null)
                    errors.Add(EmailTemplateMessage.TheModel0DoesNotHaveAnyFieldWithTheToken1.NiceToString().Formato(modelType.Name, fieldOrProperty));
            }

            MemberInfo Member;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                if (p.Model == null)
                    throw new ArgumentException("There is not any model for the message composition");

                var value = Getter(p.Model);
                if (p.IsHtml && !(value is System.Web.HtmlString))
                    p.StringBuilder.Append(HttpUtility.HtmlEncode(value.ToString()));
                else
                    p.StringBuilder.Append(value.ToString());
            }

            object Getter(IEmailModel model)
            {
                var pi = Member as PropertyInfo;

                if (pi != null)
                    return pi.GetValue(model, null);

                return ((FieldInfo)Member).GetValue(model);
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                return;
            }
        }

        public class BlockNode : TextNode
        {
            public List<TextNode> Nodes = new List<TextNode>();

            public BlockNode()
            {
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
        }

        public class ForeachNode : BlockNode
        {
            public readonly QueryToken Token;

            public ForeachNode(QueryToken token)
            {
                this.Token = token;
            }

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                var groups = rows.GroupBy(r => rows.DistinctSingle(p.Columns[Token])).ToList();
                if (groups.Count == 1 && groups[0].Key == null)
                    return;
                foreach (var group in groups)
                {
                    base.PrintList(p, group);
                }
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                list.Add(Token);
                base.FillQueryTokens(list);
            }
        }


        public class IfNode : BlockNode
        {
            public readonly QueryToken Token;

            public IfNode(QueryToken token, List<string> errors)
            {
                if (token.Type.UnNullify() != typeof(bool))
                    errors.Add("Token {0} is not a boolean".Formato(token));

                this.Token = token;
            }

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                var value = (bool?)rows.DistinctSingle(p.Columns[Token]);

                if (value == true)
                {
                    base.PrintList(p, rows);
                }
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                list.Add(Token);
                base.FillQueryTokens(list);
            }
        }

        public static BlockNode Parse(string text, Func<string, QueryToken> parseToken, Type modelType)
        {
            BlockNode node;
            var errors = TryParseTemplate(text, parseToken, modelType, out node);
            if (errors.Any())
                throw new FormatException(errors.ToString("\r\n"));
            return node;
        }


        static List<string> TryParseTemplate(string text, Func<string, QueryToken> parseToken, Type modelType, out BlockNode mainBlock)
        {
            List<string> errors = new List<string>();

            var matches = TokenRegex.Matches(text);

            Stack<BlockNode> stack = new Stack<BlockNode>();
            mainBlock = new BlockNode();
            stack.Push(mainBlock);

            Func<string, QueryToken> tryParseToken = token =>
            {
                QueryToken result = null;
                try
                {
                    result = parseToken(token);
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                    return null;
                }
                return result;
            };

            int index = 0;
            foreach (Match match in matches)
            {
                if (index < match.Index)
                {
                    stack.Peek().Nodes.Add(new LiteralNode { Text = text.Substring(index, match.Index - index) });
                }
                var token = match.Groups["token"].Value;
                switch (match.Groups["special"].Value)
                {
                    case "":
                        stack.Peek().Nodes.Add(new TokenNode(tryParseToken(token)));
                        break;
                    case "global":
                        stack.Peek().Nodes.Add(new GlobalNode(token, errors));
                        break;
                    case "model":
                        stack.Peek().Nodes.Add(new ModelNode(token, modelType, errors));
                        break;
                    case "foreach":
                        stack.Push(new ForeachNode(tryParseToken(token)));
                        break;
                    case "endforeach":
                        {
                            if (stack.Count() <= 1)
                            {
                                errors.Add("No 'foreach' has been opened for {0}".Formato(token));
                                break;
                            }
                            var n = stack.Pop();
                            if (n.GetType() != typeof(ForeachNode))
                            {
                                errors.Add("Unexpected {0}".Formato(n.GetType().Name));
                                break;
                            }

                            if (!parseToken(token).Equals(((ForeachNode)n).Token))
                            {
                                errors.Add("Expected 'endforeach' was {0} instead of {1}".Formato(((ForeachNode)n).Token, token));
                            }

                            stack.Peek().Nodes.Add(n);
                        }
                        break;
                    case "if":
                        stack.Push(new IfNode(parseToken(token), errors));
                        break;
                    case "endif":
                        {
                            if (stack.Count() <= 1)
                            {
                                errors.Add("No 'if' has been opened for {0}".Formato(token));
                                break;
                            }
                            var n = stack.Pop();
                            if (n.GetType() != typeof(IfNode))
                            {
                                errors.Add("Unexpected {0}".Formato(n.GetType().Name));
                                break;
                            }

                            if (!parseToken(token).Equals(((IfNode)n).Token))
                            {
                                errors.Add("Expected 'endif' was {0} instead of {1}".Formato(((IfNode)n).Token, token));
                            }

                            stack.Peek().Nodes.Add(n);
                        }
                        break;
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


        internal static Lite<IEmailOwnerDN> GetRecipient(ResultTable table, ResultColumn column)
        {
            return (Lite<IEmailOwnerDN>)(table.Rows.DistinctSingle(column));
        }
    }

    public class EmailTemplateParameters
    {
        public StringBuilder StringBuilder = new StringBuilder();
        public bool IsHtml;
        public CultureInfo CultureInfo;
        public IIdentifiable Entity;
        public IEmailModel Model;
        public Dictionary<QueryToken, ResultColumn> Columns;
    }
}
