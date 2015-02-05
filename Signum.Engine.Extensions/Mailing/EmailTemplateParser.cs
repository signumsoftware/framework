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
using Signum.Engine.Templating;


namespace Signum.Engine.Mailing
{
    public static partial class EmailTemplateParser
    {
        public static BlockNode Parse(string text, QueryDescription qd, Type modelType)
        {
            return new TemplateWalker(text, qd, modelType).Parse();      
        }

        public static BlockNode TryParse(string text, QueryDescription qd, Type modelType, out string errorMessage)
        {
            return new TemplateWalker(text, qd, modelType).TryParse(out errorMessage);
        }

        internal class TemplateWalker
        {
            QueryDescription qd;
            Type modelType;
            string text;


            BlockNode mainBlock;
            Stack<BlockNode> stack;
            ScopedDictionary<string, ValueProviderBase> variables;
            List<TemplateError> errors;

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
                errors.Add(new TemplateError(fatal, message)); 
            }

            void DeclareVariable(ValueProviderBase token)
            {
                if (token.Variable.HasText())
                {
                    if (variables.ContainsKey(token.Variable))
                        AddError(true, "There's already a variable '{0}' defined in this scope".FormatWith(token.Variable));

                    variables.Add(token.Variable, token);
                }
            }

            public BlockNode PopBlock(Type type)
            {
                if (stack.Count() <= 1)
                {
                    AddError(true, "No {0} has been opened".FormatWith(BlockNode.UserString(type)));
                    return null;
                }
                var n = stack.Pop();
                variables = variables.Previous;
                if (n.owner == null || n.owner.GetType() != type)
                {
                    AddError(true, "Unexpected '{0}'".FormatWith(BlockNode.UserString(n.owner.Try(p => p.GetType()))));
                    return null;
                }
                return n;
            }

            public void PushBlock(BlockNode block)
            {
                stack.Push(block);
                variables = new ScopedDictionary<string, ValueProviderBase>(variables); 
            }

            void ParseInternal()
            {
                this.mainBlock = new BlockNode(null);
                this.stack = new Stack<BlockNode>();
                this.errors = new List<TemplateError>(); 
                PushBlock(mainBlock);

                var matches = TemplateUtils.KeywordsRegex.Matches(text);

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
                    var type = match.Groups["type"].Value;
                    var token = match.Groups["token"].Value;
                    var keyword = match.Groups["keyword"].Value;
                    var dec = match.Groups["dec"].Value;
                    switch (keyword)
                    {
                        case "":
                        case "raw":
                            var tok = TemplateUtils.TokenFormatRegex.Match(token);
                            if (!tok.Success)
                                AddError(true, "{0} has invalid format".FormatWith(token));
                            else
                            {
                                var t = TryParseValueProvider(type, tok.Groups["token"].Value, dec);

                                stack.Peek().Nodes.Add(new ValueNode(t, tok.Groups["format"].Value, isRaw: keyword.Contains("raw")));

                                DeclareVariable(t);
                            }
                            break;
                        case "declare":
                            {
                                var t = TryParseValueProvider(type, token, dec);

                                stack.Peek().Nodes.Add(new DeclareNode(t, this.AddError));

                                DeclareVariable(t);
                            }
                            break;
                        case "any":
                            {
                                AnyNode any;
                                ValueProviderBase vp;
                                var filter = TemplateUtils.TokenOperationValueRegex.Match(token);
                                if (!filter.Success)
                                {
                                    vp = TryParseValueProvider(type, token, dec);

                                    any = new AnyNode(vp);
                                }
                                else
                                {
                                    vp = TryParseValueProvider(type, filter.Groups["token"].Value, dec);
                                    var comparer = filter.Groups["comparer"].Value;
                                    var value = filter.Groups["value"].Value;
                                    any = new AnyNode(vp, comparer, value, this.AddError);

                                }
                                stack.Peek().Nodes.Add(any);
                                PushBlock(any.AnyBlock);

                                DeclareVariable(vp);
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
                                ValueProviderBase vp = TryParseValueProvider(type, token, dec);
                                var fn = new ForeachNode(vp);
                                stack.Peek().Nodes.Add(fn);
                                PushBlock(fn.Block);

                                DeclareVariable(vp);
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
                                ValueProviderBase vp;
                                var filter = TemplateUtils.TokenOperationValueRegex.Match(token);
                                if (!filter.Success)
                                {
                                    vp = TryParseValueProvider(type, token, dec);
                                    ifn = new IfNode(vp, this);
                                }
                                else
                                {
                                    vp = TryParseValueProvider(type, filter.Groups["token"].Value, dec);
                                    var comparer = filter.Groups["comparer"].Value;
                                    var value = filter.Groups["value"].Value;
                                    ifn = new IfNode(vp, comparer, value, this.AddError);
                                }
                                stack.Peek().Nodes.Add(ifn);
                                PushBlock(ifn.IfBlock);
                                DeclareVariable(vp);
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
                        default :
                            AddError(false, "'{0}' is deprecated".FormatWith(keyword));
                            break;
                    }
                    index = match.Index + match.Length;
                }

                if (stack.Count != 1)
                    AddError(true, "Last block is not closed: {0}".FormatWith(stack.Peek()));

                var lastM = matches.Cast<Match>().LastOrDefault();
                if (lastM != null && lastM.Index + lastM.Length < text.Length)
                    stack.Peek().Nodes.Add(new LiteralNode { Text = text.Substring(lastM.Index + lastM.Length) });

                stack.Pop();
            }

            public ValueProviderBase TryParseValueProvider(string type, string token, string variable)
            {
                return ValueProviderBase.TryParse(type, token, variable, this.modelType, this.qd, this.variables, this.AddError);
            }
        }

        private static string Synchronize(string text, SyncronizationContext sc)
        {
            BlockNode node = new TemplateWalker(text, sc.QueryDescription, sc.ModelType).ParseSync();

            node.Synchronize(sc);

            return node.ToString(); 
        }


        internal static SqlPreCommand ProcessEmailTemplate( Replacements replacements, Table table, EmailTemplateEntity et, StringDistance sd)
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
                    QueryTokenEntity token = et.From.Token;
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
                        QueryTokenEntity token = item.Token;
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

                try
                {

                    foreach (var item in et.Messages)
                    {
                        SyncronizationContext sc = new SyncronizationContext
                        {
                            ModelType = et.SystemEmail.ToType(),
                            QueryDescription = qd,
                            Replacements = replacements,
                            StringDistance = sd,
                            Variables = new ScopedDictionary<string, ValueProviderBase>(null)
                        };

                        item.Subject = Synchronize(item.Subject, sc);
                        item.Text = Synchronize(item.Text, sc);
                    }

                    using (replacements.WithReplacedDatabaseName())
                        return table.UpdateSqlSync(et, includeCollections: true);
                }
                catch (TemplateSyncException ex)
                {
                    if (ex.Result == FixTokenResult.SkipEntity)
                        return null;

                    if (ex.Result == FixTokenResult.DeleteEntity)
                        return table.DeleteSqlSync(et);

                    throw new InvalidOperationException("Unexcpected {0}".FormatWith(ex.Result));
                }
                finally
                {
                    Console.Clear();
                }
            }
            catch (Exception e)
            {
                return new SqlPreCommandSimple("-- Exception in {0}: {1}".FormatWith(et.BaseToString(), e.Message));
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

    public class EmailTemplateParameters : TemplateParameters
    {
        public EmailTemplateParameters(IEntity entity, CultureInfo culture, Dictionary<QueryToken, ResultColumn> columns, IEnumerable<ResultRow> rows): 
              base(entity, culture, columns, rows)
        { }

        public StringBuilder StringBuilder = new StringBuilder();
        public bool IsHtml;
        public ISystemEmail SystemEmail;

        public override object GetModel()
        {
            if (SystemEmail == null)
                throw new ArgumentException("There is no SystemEmail set");

            return SystemEmail;
        }
    }
}
