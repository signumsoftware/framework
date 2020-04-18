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

namespace Signum.Engine.Templating
{
    public static partial class TextTemplateParser
    {
        public static BlockNode Parse(string? text, QueryDescription qd, Type? modelType)
        {
            return new TextTemplateParserImp(text, qd, modelType).Parse();      
        }

        public static BlockNode TryParse(string? text, QueryDescription qd, Type? modelType, out string errorMessage)
        {
            return new TextTemplateParserImp(text, qd, modelType).TryParse(out errorMessage);
        }

        internal class TextTemplateParserImp: ITemplateParser
        {
            string text;
            
            BlockNode mainBlock = null!;
            Stack<BlockNode> stack = null!;
            public ScopedDictionary<string, ValueProviderBase> Variables { get; set; } = null!;
            List<TemplateError> errors = null!;

            public Type? ModelType { get; private set; }

            public QueryDescription QueryDescription { get; private set; }

            public TextTemplateParserImp(string? text, QueryDescription qd, Type? modelType)
            {
                this.text = text ?? "";
                this.QueryDescription = qd ?? throw new ArgumentNullException(nameof(qd));
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

            void DeclareVariable(ValueProviderBase? valueProvider)
            {
                if (valueProvider?.Variable.HasText() == true)
                {
                    if (Variables.ContainsKey(valueProvider!.Variable!))
                        AddError(true, "There is already a variable '{0}' defined in this scope".FormatWith(valueProvider.Variable));

                    Variables.Add(valueProvider.Variable!, valueProvider);
                }
            }

            public BlockNode? PopBlock(Type type)
            {
                if (stack.Count() <= 1)
                {
                    AddError(true, "No {0} has been opened".FormatWith(BlockNode.UserString(type)));
                    return null;
                }
                var n = stack.Pop();
                Variables = Variables.Previous!;
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
                        stack.Peek().Nodes.Add(new LiteralNode(text));
                        stack.Pop();
                        return;
                    }

                    int index = 0;
                    foreach (Match match in matches.Cast<Match>())
                    {
                        if (index < match.Index)
                        {
                            stack.Peek().Nodes.Add(new LiteralNode(text.Substring(index, match.Index - index)));
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
                                    var an = (AnyNode?)PopBlock(typeof(AnyNode))?.owner;
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
                                    ValueProviderBase? vp = ValueProviderBase.TryParse(expr, variable, this);
                                    if (vp is TokenValueProvider tvp && tvp.ParsedToken.QueryToken != null && QueryToken.IsCollection(tvp.ParsedToken.QueryToken.Type))
                                        AddError(false, $"@foreach[{expr}] is a collection, missing 'Element' token at the end");

                                    var fn = new ForeachNode(vp);
                                    stack.Peek().Nodes.Add(fn);
                                    PushBlock(fn.Block);
                                    if(vp != null)
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
                                    var ifn = (IfNode?)PopBlock(typeof(IfNode))?.owner;
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
                        stack.Peek().Nodes.Add(new LiteralNode(text.Substring(lastM.Index + lastM.Length)));

                    stack.Pop();
                }
                catch (Exception e)
                {
                    AddError(true, e.Message);
                }
            }
        }

        public static string Synchronize(string text, SynchronizationContext sc)
        {
            BlockNode node = new TextTemplateParserImp(text, sc.QueryDescription, sc.ModelType).ParseSync();

            node.Synchronize(sc);

            return node.ToString(); 
        }
    }

   
}
