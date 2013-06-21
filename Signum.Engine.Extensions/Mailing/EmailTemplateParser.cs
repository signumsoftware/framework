﻿using System;
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

        public static object DistinctSingle(this IEnumerable<ResultRow> rows, ResultColumn column)
        {
            return rows.Select(r => r[column]).Distinct().SingleEx(() =>
                "Multiple values for column {0}".Formato(column.Column.Token.FullKey()));
        }

        public static readonly Regex TokenRegex = new Regex(@"\@(((?<special>(foreach|if|global|model|))\[(?<token>[^\]\:]*)(\:(?<format>.*))?\])|(?<special>endforeach|else|endif))");

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
                p.StringBuilder.Append(p.IsHtml ? HttpUtility.HtmlEncode(text) : text);
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
            Func<GlobalVarDispatcher, string> globalFunc;
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
                var text = globalFunc(new GlobalVarDispatcher { Entity = p.Entity, Culture = p.CultureInfo, IsHtml = p.IsHtml });
                p.StringBuilder.Append(text);
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
            public ModelNode(string fieldOrProperty, Type modelType, List<string> errors)
            {
                this.member = (MemberInfo)modelType.GetField(fieldOrProperty, flags) ??
                       (MemberInfo)modelType.GetProperty(fieldOrProperty, flags);

                if (member == null)
                    errors.Add(EmailTemplateMessage.TheModel0DoesNotHaveAnyFieldWithTheToken1.NiceToString().Formato(modelType.Name, fieldOrProperty));
            }

            MemberInfo member;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                if (p.SystemEmail == null)
                    throw new ArgumentException("There is not any model for the message composition");

                var value = Getter(p.SystemEmail);
                if (p.IsHtml && !(value is System.Web.HtmlString))
                    p.StringBuilder.Append(HttpUtility.HtmlEncode(value.ToString()));
                else
                    p.StringBuilder.Append(value.ToString());
            }

            object Getter(ISystemEmail systemEmail)
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

            public override string ToString()
            {
                return "block ({0} nodes)".Formato(Nodes.Count);
            }

            public virtual string UserString()
            {
                return "block";
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
                var groups = rows.GroupBy(r => r[p.Columns[Token]]).ToList();
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

            public override string ToString()
            {
                return "foreach {0} ({1} nodes)".Formato(Token.FullKey(), Nodes.Count);
            }

            public override string UserString()
            {
                return "foreach";
            }
        }

        public abstract class ConditionNode : BlockNode
        {
            public readonly QueryToken Token;

            public ConditionNode(QueryToken token, List<string> errors)
            {
                //Commented: Now conditions can be added to objects (null => false, true otherwise)
                //if (token.Type.UnNullify() != typeof(bool))
                //    errors.Add("Token {0} is not a boolean".Formato(token));

                this.Token = token;
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                list.Add(Token);
                base.FillQueryTokens(list);
            }

            protected static bool ToBool(object obj)
            {
                if (obj == null || obj is bool && ((bool)obj) == false)
                    return false;

                return true;
            }
        }

        public class IfNode : ConditionNode
        {
            public IfNode(QueryToken token, List<string> errors)
                : base(token, errors)
            {
            }
           

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                if (ToBool(rows.DistinctSingle(p.Columns[Token])))
                {
                    base.PrintList(p, rows);
                }
            }

            public override string ToString()
            {
                return "if {0} ({1} nodes)".Formato(Token.FullKey(), Nodes.Count);
            }

            public override string UserString()
            {
                return "if";
            }
        }

        public class ElseNode : ConditionNode
        {
            public ElseNode(QueryToken token, List<string> errors)
                : base(token, errors)
            {
            }

            public override void PrintList(EmailTemplateParameters p, IEnumerable<ResultRow> rows)
            {
                if (!ToBool(rows.DistinctSingle(p.Columns[Token])))
                {
                    base.PrintList(p, rows);
                }
            }

            public override string ToString()
            {
                return "else {0} ({1} nodes)".Formato(Token.FullKey(), Nodes.Count);
            }

            public override string UserString()
            {
                return "else";
            }
        }

        public static BlockNode Parse(string text, QueryDescription qd, Type modelType)
        {
            BlockNode node;
            var errors = TryParseTemplate(text, qd, modelType, out node);
            if (errors.Any())
                throw new FormatException(errors.ToString("\r\n"));
            return node;
        }


        static List<string> TryParseTemplate(string text, QueryDescription qd, Type modelType, out BlockNode mainBlock)
        {
            List<string> errors = new List<string>();

            var matches = TokenRegex.Matches(text);

            Stack<BlockNode> stack = new Stack<BlockNode>();
            mainBlock = new BlockNode();
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

            int index = 0;
            foreach (Match match in matches)
            {
                if (index < match.Index)
                {
                    stack.Peek().Nodes.Add(new LiteralNode { Text = text.Substring(index, match.Index - index) });
                }
                var token = match.Groups["token"].Value;
                var special = match.Groups["special"].Value;
                var format = match.Groups["format"].Value; 
                switch (special)
                {
                    case "":
                        stack.Peek().Nodes.Add(new TokenNode(tryParseToken(token), format));
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
                                errors.Add("Unexpected '{0}'".Formato(n.UserString()));
                                break;
                            }

                            stack.Peek().Nodes.Add(n);
                        }
                        break;
                    case "if":
                        stack.Push(new IfNode(tryParseToken(token), errors));
                        break;
                    case "else":
                    case "endif":
                        {
                            if (stack.Count() <= 1)
                            {
                                errors.Add("No 'if' has been opened for {0}".Formato(token));
                                break;
                            }
                            var n = stack.Pop();
                            if (!(n is ConditionNode))
                            {
                                errors.Add("Unexpected {0}".Formato(n.UserString()));
                                break;
                            }

                            stack.Peek().Nodes.Add(n);

                            if(special == "else")
                                stack.Push(new ElseNode(((ConditionNode)n).Token, errors));
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
