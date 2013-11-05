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
using Signum.Engine.UserQueries;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Utilities.DataStructures;


namespace Signum.Engine.Mailing
{
    public class GlobalVarContext
    {
        public IIdentifiable Entity;
        public CultureInfo Culture;
        public bool IsHtml;
        public ISystemEmail SystemEmail;
    }

    public static partial class EmailTemplateParser
    {
        public static Dictionary<string, Func<GlobalVarContext, object>> GlobalVariables = new Dictionary<string, Func<GlobalVarContext, object>>();

        public static object DistinctSingle(this IEnumerable<ResultRow> rows, ResultColumn column)
        {
            return rows.Select(r => r[column]).Distinct().SingleEx(
                () =>"No values for column {0}".Formato(column.Column.Token.FullKey()),
                () =>"Multiple values for column {0}".Formato(column.Column.Token.FullKey()));
        }

        public static readonly Regex KeywordsRegex = new Regex(@"\@(((?<keyword>(foreach|if|raw|translated|rawtranslated|global|model|modelraw|any|))\[(?<token>[^\]]+)\](\s+as\s+(?<dec>\$\w*))?)|(?<keyword>endforeach|else|endif|notany|endany))");

        public static readonly Regex TokenFormatRegex = new Regex(@"(?<token>[^\]\:]+)(\:(?<format>.*))?");
        public static readonly Regex TokenOperationValueRegex = new Regex(@"(?<token>[^\]]+)(?<comparer>(" + FilterValueConverter.OperationRegex + @"))(?<value>[^\]\:]+)");



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

            Func<string, QueryToken> getVariable = variable =>
            {
                foreach (var item in stack)
                {
                    var result = item.Variables.TryGetC(variable);
                    if (result != null)
                        return result;
                }

                return null;
            };

            Action<string, QueryToken> declareVariable = (variable, queryToken)=>
            {
                if(getVariable(variable) != null)
                    errors.Add("There's already a variable '{0}' defined in this scope".Formato(variable));

                stack.Peek().Variables.Add(variable, queryToken);
            }; 

            Func<string, QueryToken> tryParseToken = tokenString =>
            {
                if (tokenString.StartsWith("$"))
                {
                    string v = tokenString.TryBefore('.') ?? tokenString;

                    QueryToken token = getVariable(v);

                    if (token == null)
                    {
                        errors.Add("Variable '{0}' is not defined at this scope".Formato(v));
                        return null;
                    }

                    var after =  tokenString.TryAfter('.');

                    tokenString = token.FullKey() + (after == null ? null : ("." + after));
                }

                QueryToken result = null;
                try
                {
                    result = QueryUtils.Parse(tokenString, qd, false);
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
                if (n.owner == null || n.owner.GetType() != type)
                {
                    errors.Add("Unexpected '{0}'".Formato(BlockNode.UserString(n.owner.TryCC(p => p.GetType()))));
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
                var dec = match.Groups["dec"].Value;
                switch (keyword)
                {
                    case "":
                    case "raw":
                    case "translated":
                    case "rawtranslated":
                        var tok = TokenFormatRegex.Match(token);
                        if (!tok.Success)
                            errors.Add("{0} has invalid format".Formato(token));
                        else
                        {
                            var t = tryParseToken(tok.Groups["token"].Value);

                            stack.Peek().Nodes.Add(new TokenNode(t, tok.Groups["format"].Value,
                                isRaw: keyword.Contains("raw"),
                                isTranslated: keyword.Contains("translated"), errors: errors));

                            if (dec.HasText())
                                declareVariable(dec, t); 
                        }
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
                            AnyNode any;
                            QueryToken t;
                            var filter = TokenOperationValueRegex.Match(token);
                            if (!filter.Success)
                            {
                                t = tryParseToken(token);
                                any = new AnyNode(t, errors);
                            }
                            else
                            {
                                t = tryParseToken(filter.Groups["token"].Value);
                                var comparer = filter.Groups["comparer"].Value;
                                var value = filter.Groups["value"].Value;
                                any = new AnyNode(t, comparer, value, errors);
                               
                            }
                            stack.Peek().Nodes.Add(any);
                            stack.Push(any.AnyBlock);

                            if (dec.HasText())
                                declareVariable(dec, t); 
                            break;
                        }
                    case "notany":
                        {
                            var an = (AnyNode)popNode(typeof(AnyNode)).owner;
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
                            var t = tryParseToken(token);
                            var fn = new ForeachNode(t);
                            stack.Peek().Nodes.Add(fn);
                            stack.Push(fn.Block);

                            if (dec.HasText())
                                declareVariable(dec, t); 
                            break;
                        }
                    case "endforeach":
                        {
                            popNode(typeof(ForeachNode));
                        }
                        break;
                    case "if":
                        {
                            IfNode ifn;
                            QueryToken t; 
                            var filter = TokenOperationValueRegex.Match(token);
                            if (!filter.Success)
                            {
                                t = tryParseToken(token);
                                ifn = new IfNode(t, errors);
                            }
                            else
                            {
                                t = tryParseToken(filter.Groups["token"].Value);
                                var comparer = filter.Groups["comparer"].Value;
                                var value = filter.Groups["value"].Value;
                                ifn = new IfNode(t, comparer, value, errors);
                            }
                            stack.Peek().Nodes.Add(ifn);
                            stack.Push(ifn.IfBlock);
                            if (dec.HasText())
                                declareVariable(dec, t); 
                            break;
                        }
                    case "else":
                        {
                            var ifn = (IfNode)popNode(typeof(IfNode)).owner;
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

        internal static SqlPreCommand ProcessEmailTemplate(Replacements replacements, Table table, EmailTemplateDN et, StringDistance sd)
        {
            try
            {
                Console.Clear();

                SafeConsole.WriteLineColor(ConsoleColor.White, "EmailTemplate: " + et.Name);
                Console.WriteLine(" Query: " + et.Query.Key);


                var result = (from msg in et.Messages
                              from s in new[] { msg.Subject, msg.Text }
                              from m in KeywordsRegex.Matches(s).Cast<Match>()
                              where m.Groups["token"].Success && !IsToken(m.Groups["keyword"].Value)
                              select new
                              {
                                  isGlobal = IsGlobal(m.Groups["keyword"].Value),
                                  token = m.Groups["token"].Value
                              }).Distinct().ToList();

                foreach (var g in result.Where(a => a.isGlobal).Select(a => a.token).ToHashSet())
                {
                    string s = replacements.SelectInteractive(g, GlobalVariables.Keys, "EmailTemplate Globals", sd);

                    if (s != null && s != g)
                    {
                        ReplaceToken(et, (keyword, oldToken) =>
                        {
                            if (!IsGlobal(keyword))
                                return null;

                            if (oldToken == g)
                                return s;

                            return null;
                        });
                    }
                }

                foreach (var m in result.Where(a => !a.isGlobal).Select(a => a.token).ToHashSet())
                {
                    var type = et.SystemEmail.ToType();

                    string newM = ModelNode.GetNewModel(type, m, replacements, sd);

                    if (newM != m)
                    {
                        ReplaceToken(et, (keyword, oldToken) =>
                        {
                            if (!IsModel(keyword))
                                return null;

                            if (oldToken == m)
                                return newM;

                            return null;
                        });
                    }
                }

                if (et.Tokens.Any(a => a.ParseException != null))
                    using (et.DisableAuthorization ? ExecutionMode.Global() : null)
                    {
                        QueryDescription qd = DynamicQueryManager.Current.QueryDescription(et.Query.ToQueryName());

                        if (et.Tokens.Any())
                        {
                            Console.WriteLine(" Tokens:");
                            foreach (var item in et.Tokens.ToList())
                            {
                                QueryTokenDN token = item;
                                switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, false, "", allowRemoveToken: false))
                                {
                                    case FixTokenResult.Nothing: break;
                                    case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(et);
                                    case FixTokenResult.RemoveToken: throw new InvalidOperationException("Unexpected RemoveToken");
                                    case FixTokenResult.SkipEntity: return null;
                                    case FixTokenResult.Fix:
                                        foreach (var tok in et.Recipients.Where(r => r.Token.TokenString == item.TokenString).ToList())
                                            tok.Token = token;

                                        FixTokenResult? currentResult = null;

                                        ReplaceToken(et, (keyword, oldToken) =>
                                        {
                                            if (!IsToken(keyword) || currentResult.HasValue)
                                                return null;

                                            if (keyword == "" || keyword == "raw" || keyword == "translated" || keyword == "translatedraw")
                                            {
                                                var match = TokenFormatRegex.Match(oldToken);
                                                string tokenPart = match.Groups["token"].Value;
                                                string formatPart = oldToken.RemoveStart(match.Groups["token"].Length);

                                                if (AreSimilar(tokenPart, item.TokenString))
                                                    return token.Token.FullKey() + formatPart;

                                                return null;
                                            }
                                            else if (keyword == "any" || keyword == "if")
                                            {
                                                var match = TokenOperationValueRegex.Match(oldToken);

                                                if (!match.Success)
                                                {
                                                    if (AreSimilar(oldToken, item.TokenString))
                                                        return token.Token.FullKey();

                                                    return null;
                                                }

                                                string tokenPart = match.Groups["token"].Value;
                                                string operationPart = match.Groups["comparer"].Value;
                                                string valuePart = match.Groups["value"].Value;

                                                if (AreSimilar(tokenPart, item.TokenString))
                                                {
                                                    tokenPart = token.Token.FullKey();

                                                    switch (QueryTokenSynchronizer.FixValue(replacements, token.Token.Type, ref valuePart, allowRemoveToken: false))
                                                    {
                                                        case FixTokenResult.Fix:
                                                        case FixTokenResult.Nothing: break;
                                                        case FixTokenResult.DeleteEntity: currentResult = FixTokenResult.DeleteEntity; break;
                                                        case FixTokenResult.SkipEntity: currentResult = FixTokenResult.SkipEntity; break;
                                                    }

                                                    return tokenPart + operationPart + valuePart;
                                                }

                                                return null;
                                            }
                                            else
                                            {
                                                if (AreSimilar(oldToken, item.TokenString))
                                                    return token.Token.FullKey();

                                                return null;
                                            }
                                        });

                                        if (currentResult == FixTokenResult.DeleteEntity)
                                            goto case FixTokenResult.DeleteEntity;
                                        if (currentResult == FixTokenResult.SkipEntity)
                                            goto case FixTokenResult.SkipEntity;

                                        break;
                                    default: break;
                                }
                            }
                        }
                    }

                Console.Clear();

                return table.UpdateSqlSync(et, includeCollections: true);
            }
            catch (Exception e)
            {
                return new SqlPreCommandSimple("-- Exception in {0}: {1}".Formato(et.BaseToString(), e.Message));
            }
        }

        static void ReplaceToken(EmailTemplateDN et, Func<string, string, string> replacer)
        {
            foreach (var m in et.Messages)
            {
                m.Subject = ReplaceTokenText(m.Subject, replacer);
                m.Text = ReplaceTokenText(m.Text, replacer);
            }
        }

        static string ReplaceTokenText(string text, Func<string, string, string> replacer)
        {
            var result = KeywordsRegex.Replace(text, m =>
            {
                var gr = m.Groups["token"];

                if (!gr.Success)
                    return m.Value;

                var rep = replacer(m.Groups["keyword"].Value, m.Groups["token"].Value);

                if (rep == null)
                    return m.Value;

                var newKeyword = m.Value.Substring(0, gr.Index - m.Index)
                    + rep
                    + m.Value.Substring(gr.Index + gr.Length - m.Index);

                return newKeyword;
            });

            return result;
        }

        static bool AreSimilar(string p1, string p2)
        {
            if (p1.StartsWith("Entity."))
                p1 = p1.After("Entity.");

            if (p2.StartsWith("Entity."))
                p2 = p2.After("Entity.");

            return p1 == p2;
        }

        static bool IsModel(string keyword)
        {
            return keyword == "model" || keyword == "modelraw";
        }

        static bool IsGlobal(string keyword)
        {
            return keyword == "global";
        }

        static bool IsToken(string keyword)
        {
            return
                keyword == "" ||
                keyword == "foreach" ||
                keyword == "if" ||
                keyword == "raw" ||
                keyword == "translated" ||
                keyword == "translatedraw" ||
                keyword == "any";
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
