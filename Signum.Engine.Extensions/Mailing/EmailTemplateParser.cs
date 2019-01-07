using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using Signum.Entities;
using Signum.Entities.Mailing;
using Signum.Entities.UserAssets;
using Signum.Engine.UserAssets;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Utilities.DataStructures;
using Signum.Engine.Templating;
using Signum.Utilities.Reflection;

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

        internal class TemplateWalker: ITemplateParser
        {
            string text;
            
            BlockNode mainBlock;
            Stack<BlockNode> stack;
            public ScopedDictionary<string, ValueProviderBase> Variables { get; set; }
            List<TemplateError> errors;

            public Type ModelType { get; private set; }

            static PropertyInfo piSystemEmail = ReflectionTools.GetPropertyInfo((EmailTemplateEntity e) => e.SystemEmail);
            public PropertyInfo ModelProperty => piSystemEmail;
            public QueryDescription QueryDescription { get; private set; }

            public TemplateWalker(string text, QueryDescription qd, Type modelType)
            {
                if (qd == null)
                    throw new ArgumentNullException(nameof(qd));

                this.text = text ?? "";
                this.QueryDescription = qd;
                this.ModelType = modelType; 
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

            public void AddError(bool fatal, string message)
            {
                errors.Add(new TemplateError(fatal, message)); 
            }

            void DeclareVariable(ValueProviderBase token)
            {
                if (token?.Variable.HasText() == true)
                {
                    if (Variables.ContainsKey(token.Variable))
                        AddError(true, "There's already a variable '{0}' defined in this scope".FormatWith(token.Variable));

                    Variables.Add(token.Variable, token);
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
                Variables = Variables.Previous;
                if (n.owner == null || n.owner.GetType() != type)
                {
                    AddError(true, "Unexpected '{0}'".FormatWith(BlockNode.UserString(n.owner?.GetType())));
                    return null;
                }
                return n;
            }

            public void PushBlock(BlockNode block)
            {
                stack.Push(block);
                Variables = new ScopedDictionary<string, ValueProviderBase>(Variables); 
            }

            void ParseInternal()
            {
                try
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
                        var expr = match.Groups["expr"].Value;
                        var keyword = match.Groups["keyword"].Value;
                        var variable = match.Groups["dec"].Value;
                        switch (keyword)
                        {
                            case "":
                            case "raw":
                                var s = TemplateUtils.SplitToken(expr);
                                if (s == null)
                                    AddError(true, "{0} has invalid format".FormatWith(expr));
                                else
                                {
                                    var t = ValueProviderBase.TryParse(s.Value.Token, variable, this);

                                    stack.Peek().Nodes.Add(new ValueNode(t, s.Value.Format, isRaw: keyword.Contains("raw")));

                                    DeclareVariable(t);
                                }
                                break;
                            case "declare":
                                {
                                    var t = ValueProviderBase.TryParse(expr, variable, this);

                                    stack.Peek().Nodes.Add(new DeclareNode(t, this.AddError));

                                    DeclareVariable(t);
                                }
                                break;
                            case "any":
                                {
                                    ConditionBase cond = TemplateUtils.ParseCondition(expr, variable, this);
                                    AnyNode any = new AnyNode(cond);
                                    stack.Peek().Nodes.Add(any);
                                    PushBlock(any.AnyBlock);
                                    if (cond is ConditionCompare cc)
                                        DeclareVariable(cc.ValueProvider);
                                    break;
                                }
                            case "notany":
                                {
                                    var an = (AnyNode)PopBlock(typeof(AnyNode)).owner;
                                    if (an != null)
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
                                    ValueProviderBase vp = ValueProviderBase.TryParse(expr, variable, this);
                                    var fn = new ForeachNode(vp);
                                    stack.Peek().Nodes.Add(fn);
                                    PushBlock(fn.Block);
                                    vp.IsForeach = true;
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
                                    ConditionBase cond = TemplateUtils.ParseCondition(expr, variable, this);
                                    IfNode ifn = new IfNode(cond, this);
                                    stack.Peek().Nodes.Add(ifn);
                                    PushBlock(ifn.IfBlock);
                                    if (cond is ConditionCompare cc)
                                        DeclareVariable(cc.ValueProvider);
                                    break;
                                }
                            case "else":
                                {
                                    var ifn = (IfNode)PopBlock(typeof(IfNode)).owner;
                                    if (ifn != null)
                                        PushBlock(ifn.CreateElse());
                                    break;
                                }
                            case "endif":
                                {
                                    PopBlock(typeof(IfNode));
                                    break;
                                }
                            default:
                                AddError(true, "'{0}' is deprecated".FormatWith(keyword));
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
                catch (Exception e)
                {
                    AddError(true, e.Message);
                }
            }
        }



        private static string Synchronize(string text, SynchronizationContext sc)
        {
            BlockNode node = new TemplateWalker(text, sc.QueryDescription, sc.ModelType).ParseSync();

            node.Synchronize(sc);

            return node.ToString(); 
        }


        internal static SqlPreCommand ProcessEmailTemplate( Replacements replacements, Table table, EmailTemplateEntity et, StringDistance sd)
        {
            Console.Write(".");
            try
            {
                var queryName = QueryLogic.ToQueryName(et.Query.Key);

                QueryDescription qd = QueryLogic.Queries.QueryDescription(queryName);

                using (DelayedConsole.Delay(() => SafeConsole.WriteLineColor(ConsoleColor.White, "EmailTemplate: " + et.Name)))
                using (DelayedConsole.Delay(() => Console.WriteLine(" Query: " + et.Query.Key)))
                {
                    if (et.From != null && et.From.Token != null)
                    {
                        QueryTokenEmbedded token = et.From.Token;
                        switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, " From", allowRemoveToken: false, allowReCreate: et.SystemEmail != null))
                        {
                            case FixTokenResult.Nothing: break;
                            case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(et, e => e.Name == et.Name);
                            case FixTokenResult.SkipEntity: return null;
                            case FixTokenResult.Fix: et.From.Token = token; break;
                            case FixTokenResult.ReGenerateEntity: return Regenerate(et, replacements, table);
                            default: break;
                        }
                    }

                    if (et.Recipients.Any(a => a.Token != null))
                    {
                        using (DelayedConsole.Delay(() => Console.WriteLine(" Recipients:")))
                        {
                            foreach (var item in et.Recipients.Where(a => a.Token != null).ToList())
                            {
                                QueryTokenEmbedded token = item.Token;
                                switch (QueryTokenSynchronizer.FixToken(replacements, ref token, qd, SubTokensOptions.CanElement, " Recipient", allowRemoveToken: false, allowReCreate: et.SystemEmail != null))
                                {
                                    case FixTokenResult.Nothing: break;
                                    case FixTokenResult.DeleteEntity: return table.DeleteSqlSync(et, e => e.Name == et.Name);
                                    case FixTokenResult.RemoveToken: et.Recipients.Remove(item); break;
                                    case FixTokenResult.SkipEntity: return null;
                                    case FixTokenResult.Fix: item.Token = token; break;
                                    case FixTokenResult.ReGenerateEntity: return Regenerate(et, replacements, table);
                                    default: break;
                                }
                            }
                        }
                    }

                    try
                    {

                        foreach (var item in et.Messages)
                        {
                            SynchronizationContext sc = new SynchronizationContext
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
                            return table.UpdateSqlSync(et, e => e.Name == et.Name, includeCollections: true, comment: "EmailTemplate: " + et.Name);
                    }
                    catch (TemplateSyncException ex)
                    {
                        if (ex.Result == FixTokenResult.SkipEntity)
                            return null;

                        if (ex.Result == FixTokenResult.DeleteEntity)
                            return table.DeleteSqlSync(et, e => e.Name == et.Name);

                        if (ex.Result == FixTokenResult.ReGenerateEntity)
                            return Regenerate(et, replacements, table);

                        throw new UnexpectedValueException(ex.Result);
                    }
                }
            }
            catch (Exception e)
            {
                return new SqlPreCommandSimple("-- Exception on {0}. {1}\r\n{2}".FormatWith(et.BaseToString(), e.GetType().Name, e.Message.Indent(2, '-')));
            }
        }

        internal static SqlPreCommand Regenerate(EmailTemplateEntity et, Replacements replacements, Table table)
        {
            var newTemplate = SystemEmailLogic.CreateDefaultTemplate(et.SystemEmail);

            newTemplate.SetId(et.IdOrNull);
            newTemplate.SetIsNew(false);
            newTemplate.Ticks = et.Ticks; 

            using (replacements?.WithReplacedDatabaseName())
                return table.UpdateSqlSync(newTemplate, e=> e.Name == newTemplate.Name, includeCollections: true, comment: "EmailTemplate Regenerated: " + et.Name);
        }
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
