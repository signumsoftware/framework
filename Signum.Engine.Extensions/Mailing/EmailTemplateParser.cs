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
using Signum.Entities.UserAssets;
using System.Linq.Expressions;
using Signum.Engine.UserAssets;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Utilities.DataStructures;
using System.Collections.Concurrent;


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
        class SemiStructuralEqualityComparer : IEqualityComparer<object>
        {
            public static readonly SemiStructuralEqualityComparer Comparer = new SemiStructuralEqualityComparer();

            ConcurrentDictionary<Type, List<Func<object, object>>> Cache = new ConcurrentDictionary<Type, List<Func<object, object>>>();

            public List<Func<object, object>> GetFieldGetters(Type type)
            {
                return Cache.GetOrAdd(type, t =>
                    t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(f => !f.HasAttribute<IgnoreAttribute>())
                    .Select(fi => Signum.Utilities.Reflection.ReflectionTools.CreateGetterUntyped(t, fi)).ToList());
            }

            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                if (x == null || y == null)
                    return x == null && y == null;

                Type t = x.GetType();

                if (IsSimple(t))
                    return x.Equals(y);

                var fields = GetFieldGetters(t);
                for (int i = 0; i < fields.Count; i++)
                {
                    var f = fields[i];
                    if (!Equals(f(x), f(y)))
                        return false;
                }


                return true;
            }

            public int GetHashCode(object obj)
            {
                if (obj == null)
                    return 0;

                Type t = obj.GetType();

                if (IsSimple(t))
                    return obj.GetHashCode();

                int result = 1;

                var fields = GetFieldGetters(t);
                for (int i = 0; i < fields.Count; i++)
                    result ^= GetHashCode(fields[i](obj)) << (i % 8);

                return result;
            }

            static bool IsSimple(Type t)
            {
                return t == typeof(string) || Type.GetTypeCode(t) >= TypeCode.Boolean ||
                    typeof(IIdentifiable).IsAssignableFrom(t) || typeof(Lite<IIdentifiable>).IsAssignableFrom(t) ||
                    typeof(IEquatable<>).MakeGenericType(t).IsAssignableFrom(t);
            }
        }

        public static Dictionary<string, Func<GlobalVarContext, object>> GlobalVariables = new Dictionary<string, Func<GlobalVarContext, object>>();

        public static object DistinctSingle(this IEnumerable<ResultRow> rows, ResultColumn column)
        {
            return rows.Select(r => r[column]).Distinct(SemiStructuralEqualityComparer.Comparer).SingleEx(
                () =>"No values for column {0}".Formato(column.Column.Token.FullKey()),
                () =>"Multiple values for column {0}".Formato(column.Column.Token.FullKey()));
        }

        public static BlockNode Parse(string text, QueryDescription qd, Type modelType)
        {
            return new TemplateWalker(text, qd, modelType).Parse();      
        }

        public static BlockNode TryParse(string text, QueryDescription qd, Type modelType, out string errorMessage)
        {
            return new TemplateWalker(text, qd, modelType).TryParse(out errorMessage);
        }

        struct Error
        {
            public string Message; 
            public bool IsFatal; 
        }

        internal class TemplateWalker
        {
            public static readonly Regex KeywordsRegex = new Regex(@"\@(((?<keyword>(foreach|if|raw|global|model|modelraw|any|declare|))\[(?<token>[^\]]+)\](\s+as\s+(?<dec>\$\w*))?)|(?<keyword>endforeach|else|endif|notany|endany))");

            public static readonly Regex TokenFormatRegex = new Regex(@"(?<token>[^\]\:]+)(\:(?<format>.*))?");
            public static readonly Regex TokenOperationValueRegex = new Regex(@"(?<token>[^\]]+)(?<comparer>(" + FilterValueConverter.OperationRegex + @"))(?<value>[^\]\:]+)");


            QueryDescription qd;
            Type modelType;
            string text;


            BlockNode mainBlock;
            Stack<BlockNode> stack;
            ScopedDictionary<string, ParsedToken> variables;
            List<Error> errors;

            public TemplateWalker(string text, QueryDescription qd, Type modelType)
            {
                if (qd == null)
                    throw new ArgumentNullException("qd");

                this.text = text ?? "";
                this.qd = qd;
                this.modelType = modelType; 
            }

            public BlockNode Parse()
            {
                ParseInternal();
                if (errors.Any())
                    throw new FormatException(errors.ToString("\r\n"));
                return mainBlock;
            }

            public BlockNode ParseSync()
            {
                ParseInternal();

                string fatalErrors = errors.Where(a => a.IsFatal).ToString("\r\n");

                if (fatalErrors.HasText())
                    throw new FormatException(fatalErrors);

                return mainBlock;
            }

            public BlockNode TryParse(out string errorMessages)
            {
                ParseInternal();
                errorMessages = this.errors.ToString(a => a.Message, ", ");
                return mainBlock;
            }

            internal void AddError(bool fatal, string message)
            {
                errors.Add(new Error{ IsFatal = fatal, Message = message}); 
            }

            void DeclareVariable(ParsedToken token)
            {
                if (token.Variable.HasText())
                {
                    if (variables.ContainsKey(token.Variable))
                        AddError(true, "There's already a variable '{0}' defined in this scope".Formato(token.Variable));

                    variables.Add(token.Variable, token);
                }
            }

            ParsedToken TryParseToken(string tokenString, string variable, SubTokensOptions options)
            {
                ParsedToken result = new ParsedToken { String = tokenString, Variable = variable };

                if (tokenString.StartsWith("$"))
                {
                    string v = tokenString.TryBefore('.') ?? tokenString;

                    ParsedToken token;

                    if (!variables.TryGetValue(v, out token))
                    {
                        AddError(false, "Variable '{0}' is not defined at this scope".Formato(v));
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
                    AddError(false, ex.Message);
                }
                return result;
            }

            public BlockNode PopBlock(Type type)
            {
                if (stack.Count() <= 1)
                {
                    AddError(true, "No {0} has been opened".Formato(BlockNode.UserString(type)));
                    return null;
                }
                var n = stack.Pop();
                variables = variables.Previous;
                if (n.owner == null || n.owner.GetType() != type)
                {
                    AddError(true, "Unexpected '{0}'".Formato(BlockNode.UserString(n.owner.Try(p => p.GetType()))));
                    return null;
                }
                return n;
            }

            public void PushBlock(BlockNode block)
            {
                stack.Push(block);
                variables = new ScopedDictionary<string, ParsedToken>(variables); 
            }

            void ParseInternal()
            {
                this.mainBlock = new BlockNode(null);
                this.stack = new Stack<BlockNode>();
                this.errors = new List<Error>(); 
                PushBlock(mainBlock);

                var matches = KeywordsRegex.Matches(text);

                if (matches.Count == 0)
                {
                    stack.Peek().Nodes.Add(new LiteralNode { Text = text });
                    stack.Pop();
                    return;
                }

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
                            var tok = TokenFormatRegex.Match(token);
                            if (!tok.Success)
                                AddError(true, "{0} has invalid format".Formato(token));
                            else
                            {
                                var t = TryParseToken(tok.Groups["token"].Value, dec, SubTokensOptions.CanElement);

                                stack.Peek().Nodes.Add(new TokenNode(t, tok.Groups["format"].Value,
                                    isRaw: keyword.Contains("raw"),
                                    walker: this));

                                DeclareVariable(t);
                            }
                            break;
                        case "declare":
                            {
                                var t = TryParseToken(token, dec, SubTokensOptions.CanElement);

                                stack.Peek().Nodes.Add(new DeclareNode(t, this));

                                DeclareVariable(t);
                            }
                            break;
                        case "global":
                            stack.Peek().Nodes.Add(new GlobalNode(token, walker: this));
                            break;
                        case "model":
                        case "modelraw":
                            stack.Peek().Nodes.Add(new ModelNode(token, modelType, walker: this) { IsRaw = keyword == "modelraw" });
                            break;
                        case "any":
                            {
                                AnyNode any;
                                ParsedToken t;
                                var filter = TokenOperationValueRegex.Match(token);
                                if (!filter.Success)
                                {
                                    t = TryParseToken(token, dec, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll);
                                    any = new AnyNode(t, this);
                                }
                                else
                                {
                                    t = TryParseToken(filter.Groups["token"].Value,  dec, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll);
                                    var comparer = filter.Groups["comparer"].Value;
                                    var value = filter.Groups["value"].Value;
                                    any = new AnyNode(t, comparer, value, this);

                                }
                                stack.Peek().Nodes.Add(any);
                                PushBlock(any.AnyBlock);

                                DeclareVariable(t);
                                break;
                            }
                        case "notany":
                            {
                                var an = (AnyNode)PopBlock(typeof(AnyNode)).owner;
                                PushBlock(an.CreateNotAny());
                                break;
                            }
                        case "endany":
                            {
                                PopBlock(typeof(AnyNode));
                                break;
                            }
                        case "foreach":
                            {
                                var t = TryParseToken(token, dec, SubTokensOptions.CanElement);
                                var fn = new ForeachNode(t);
                                stack.Peek().Nodes.Add(fn);
                                PushBlock(fn.Block);

                                DeclareVariable(t);
                                break;
                            }
                        case "endforeach":
                            {
                                PopBlock(typeof(ForeachNode));
                            }
                            break;
                        case "if":
                            {
                                IfNode ifn;
                                ParsedToken t;
                                var filter = TokenOperationValueRegex.Match(token);
                                if (!filter.Success)
                                {
                                    t = TryParseToken(token, dec, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll);
                                    ifn = new IfNode(t, this);
                                }
                                else
                                {
                                    t = TryParseToken(filter.Groups["token"].Value, dec, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll);
                                    var comparer = filter.Groups["comparer"].Value;
                                    var value = filter.Groups["value"].Value;
                                    ifn = new IfNode(t, comparer, value, this);
                                }
                                stack.Peek().Nodes.Add(ifn);
                                PushBlock(ifn.IfBlock);
                                DeclareVariable(t);
                                break;
                            }
                        case "else":
                            {
                                var ifn = (IfNode)PopBlock(typeof(IfNode)).owner;
                                PushBlock(ifn.CreateElse());
                                break;
                            }
                        case "endif":
                            {
                                PopBlock(typeof(IfNode));
                                break;
                            }
                        default:
                            break;
                    }
                    index = match.Index + match.Length;
                }

                if (stack.Count != 1)
                    AddError(true, "Last block is not closed: {0}".Formato(stack.Peek()));

                var lastM = matches.Cast<Match>().LastOrDefault();
                if (lastM != null && lastM.Index + lastM.Length < text.Length)
                    stack.Peek().Nodes.Add(new LiteralNode { Text = text.Substring(lastM.Index + lastM.Length) });

                stack.Pop();
            }
        }

        private static string Synchronize(string text, SyncronizationContext sc)
        {
            BlockNode node = new TemplateWalker(text, sc.QueryDescription, sc.ModelType).ParseSync();

            node.Synchronize(sc);

            return node.ToString(); 
        }

        public class SyncronizationContext
        {
            public ScopedDictionary<string, ParsedToken> Variables;
            public Type ModelType;
            public Replacements Replacements;
            public StringDistance StringDistance;
            public QueryDescription QueryDescription;

            internal void SynchronizeToken(ParsedToken parsedToken, string remainingText)
            {
                if (parsedToken.QueryToken != null)
                {
                    SafeConsole.WriteColor(parsedToken.QueryToken != null ? ConsoleColor.Gray : ConsoleColor.Red, "  " + parsedToken.QueryToken.FullKey());
                    Console.WriteLine(" " + remainingText);
                }
                else
                {
                    string tokenString = parsedToken.String;

                    if (tokenString.StartsWith("$"))
                    {
                        string v = tokenString.TryBefore('.') ?? tokenString;

                        ParsedToken part;
                        if (!Variables.TryGetValue(v, out part))
                            SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Variable '{0}' not found!".Formato(v));

                        if (part != null && part.QueryToken == null)
                            SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Variable '{0}' is not fixed yet! currently: '{1}'".Formato(v, part.String));

                        var after = tokenString.TryAfter('.');

                        tokenString =
                            (part == null ? "Unknown" :
                            part.QueryToken == null ? part.String :
                            part.QueryToken.FullKey()) + (after == null ? null : ("." + after));
                    }

                    SafeConsole.WriteColor(ConsoleColor.Red, "  " + tokenString);
                    Console.WriteLine(" " + remainingText);

                    QueryToken token;
                    FixTokenResult result = QueryTokenSynchronizer.FixToken(Replacements, tokenString, out token, QueryDescription, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll /*not always*/, remainingText, allowRemoveToken: false);
                    switch (result)
                    {
                        case FixTokenResult.Nothing:
                        case FixTokenResult.Fix:
                            parsedToken.QueryToken = token;
                            parsedToken.String = token.FullKey();
                            break;
                        case FixTokenResult.SkipEntity:
                        case FixTokenResult.RemoveToken:
                            throw new TemplateSyncException(result);
                    }
                }
            }

            public void SynchronizeValue(ParsedToken Token, ref string value, bool isList)
            {
                string val = value;
                FixTokenResult result = QueryTokenSynchronizer.FixValue(Replacements, Token.QueryToken.Type, ref val, allowRemoveToken: false, isList: isList);
                switch (result)
                {
                    case FixTokenResult.Fix:
                    case FixTokenResult.Nothing:
                        value = val;
                        break;
                    case FixTokenResult.SkipEntity:
                    case FixTokenResult.RemoveToken:
                        throw new TemplateSyncException(result);
                }
            }

            public IDisposable NewScope()
            {
                Variables = new ScopedDictionary<string, ParsedToken>(Variables);

                return new Disposable(() => Variables = Variables.Previous);
            }
        }

        public class TemplateSyncException : Exception
        {
            public FixTokenResult Result;

            public TemplateSyncException(FixTokenResult result)
            {
                this.Result = result;
            }
        }


        internal static SqlPreCommand ProcessEmailTemplate( Replacements replacements, Table table, EmailTemplateDN et, StringDistance sd)
        {
            try
            {
                var queryName = QueryLogic.ToQueryName(et.Query.Key);

                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                Console.Clear();

                SafeConsole.WriteLineColor(ConsoleColor.White, "EmailTemplate: " + et.Name);
                Console.WriteLine(" Query: " + et.Query.Key);

                if (et.From != null && et.From.Token != null)
                {
                    QueryTokenDN token = et.From.Token;
                    switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, " From", allowRemoveToken: false))
                    {
                        case FixTokenResult.Nothing: break;
                        case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(et);
                        case FixTokenResult.SkipEntity: return null;
                        case FixTokenResult.Fix: et.From.Token = token; break;
                        default: break;
                    }
                }

                if (et.Recipients.Any(a=>a.Token != null))
                {
                    Console.WriteLine(" Recipients:");
                    foreach (var item in et.Recipients.Where(a => a.Token != null).ToList())
                    {
                        QueryTokenDN token = item.Token;
                        switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, " Recipient"))
                        {
                            case FixTokenResult.Nothing: break;
                            case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(et);
                            case FixTokenResult.RemoveToken: et.Recipients.Remove(item); break;
                            case FixTokenResult.SkipEntity: return null;
                            case FixTokenResult.Fix: item.Token = token; break;
                            default: break;
                        }
                    }
                }

                SyncronizationContext sc = new SyncronizationContext
                {
                     ModelType = et.SystemEmail.ToType(),
                     QueryDescription = qd,
                     Replacements = replacements, 
                     StringDistance = sd
                };

                try
                {

                    foreach (var item in et.Messages)
                    {
                        item.Subject = Synchronize(item.Subject, sc);
                        item.Text = Synchronize(item.Text, sc);
                    }

                    return table.UpdateSqlSync(et, includeCollections: true);
                }
                catch (TemplateSyncException ex)
                {
                    if (ex.Result == FixTokenResult.SkipEntity)
                        return null;

                    if (ex.Result == FixTokenResult.DeleteEntity)
                        return table.DeleteSqlSync(et);

                    throw new InvalidOperationException("Unexcpected {0}".Formato(ex.Result));
                }
                finally
                {
                    Console.Clear();
                }
            }
            catch (Exception e)
            {
                return new SqlPreCommandSimple("-- Exception in {0}: {1}".Formato(et.BaseToString(), e.Message));
            }
        }
    
        //static bool AreSimilar(string p1, string p2)
        //{
        //    if (p1.StartsWith("Entity."))
        //        p1 = p1.After("Entity.");

        //    if (p2.StartsWith("Entity."))
        //        p2 = p2.After("Entity.");

        //    return p1 == p2;
        //}
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
