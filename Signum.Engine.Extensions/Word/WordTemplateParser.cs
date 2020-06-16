using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;
using D = DocumentFormat.OpenXml.Drawing;
using S = DocumentFormat.OpenXml.Spreadsheet;
using Signum.Engine.Templating;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Signum.Entities.Word;
using System.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Word
{
    public class WordTemplateParser : ITemplateParser
    {
        public List<TemplateError> Errors = new List<TemplateError>();
        public QueryDescription QueryDescription { get; private set; }
        public ScopedDictionary<string, ValueProviderBase> Variables { get; private set; } = new ScopedDictionary<string, ValueProviderBase>(null);
        public Type? ModelType { get; private set; }

        OpenXmlPackage document;
        WordTemplateEntity template;

        public WordTemplateParser(OpenXmlPackage document, QueryDescription queryDescription, Type? modelType, WordTemplateEntity template)
        {
            this.QueryDescription = queryDescription;
            this.ModelType = modelType;
            this.document = document;
            this.template = template;
        }

        public void ParseDocument()
        {
            foreach (var part in document.AllParts().Where(p => p.RootElement != null))
            {
                foreach (var item in part.RootElement.Descendants())
                {
                    if (item is W.Paragraph wp)
                        ReplaceRuns(wp, new WordprocessingNodeProvider());

                    if (item is D.Paragraph dp)
                        ReplaceRuns(dp, new DrawingNodeProvider());

                    if (item is S.SharedStringItem s)
                        ReplaceRuns(s, new SpreadsheetNodeProvider());
                }


                TableBinder.ValidateTables(part, this.template, this.Errors);
            }
        }

        private void ReplaceRuns(OpenXmlCompositeElement par, INodeProvider nodeProvider)
        {
            FixNakedText(par, nodeProvider);

            string text = par.ChildElements.Where(a => nodeProvider.IsRun(a)).ToString(r => nodeProvider.GetText(r), "");

            var matches = TemplateUtils.KeywordsRegex.Matches(text).Cast<Match>().ToList();

            if (matches.Any())
            {
                List<ElementInfo> infos = GetElementInfos(par.ChildElements, nodeProvider);

                par.RemoveAllChildren();

                var stack = new Stack<ElementInfo>(infos.AsEnumerable().Reverse());

                foreach (var m in matches)
                {
                    var interval = new Interval<int>(m.Index, m.Index + m.Length);

                    //  [Before][Start][Ignore][Ignore][End]...[Remaining]
                    //              [        Match       ]

                    ElementInfo start = stack.Pop(); //Start
                    while (start.Interval.Max <= interval.Min) //Before
                    {
                        par.Append(start.Element);
                        start = stack.Pop();
                    }

                    var startRun = (OpenXmlCompositeElement)nodeProvider.CastRun(start.Element);

                    if (start.Interval.Min < interval.Min)
                    {
                        var firstRunPart = nodeProvider.NewRun(
                            (OpenXmlCompositeElement?)nodeProvider.GetRunProperties(startRun)?.CloneNode(true),
                             start.Text!.Substring(0, m.Index - start.Interval.Min),
                             SpaceProcessingModeValues.Preserve
                            );
                        par.Append(firstRunPart);
                    }

                    par.Append(new MatchNode(nodeProvider, m) { RunProperties = (OpenXmlCompositeElement?)nodeProvider.GetRunProperties(startRun)?.CloneNode(true) });

                    ElementInfo end = start;
                    while (end.Interval.Max < interval.Max) //Ignore
                        end = stack.Pop();

                    if (interval.Max < end.Interval.Max) //End
                    {
                        var endRun = (OpenXmlCompositeElement)end.Element;

                        var textPart = end.Text!.Substring(interval.Max - end.Interval.Min);
                        var endRunPart = nodeProvider.NewRun(
                            nodeProvider.GetRunProperties(startRun)?.Let(r => (OpenXmlCompositeElement)r.CloneNode(true)),
                            textPart,
                             SpaceProcessingModeValues.Preserve
                            );

                        stack.Push(new ElementInfo(endRunPart, textPart)
                        {
                            Interval = new Interval<int>(interval.Max, end.Interval.Max)
                        });
                    }
                }

                while (!stack.IsEmpty()) //Remaining
                {
                    var pop = stack.Pop();
                    par.Append(pop.Element);
                }
            }
        }

        private void FixNakedText(OpenXmlCompositeElement par, INodeProvider nodeProvider) //Simple Spreadsheets cells
        {
            if (par.ChildElements.Count != 1)
                return;

            var only = par.ChildElements.Only();

            if (!nodeProvider.IsText(only))
                return;

            var text = nodeProvider.GetText(only);
            if (!TemplateUtils.KeywordsRegex.IsMatch(text))
                return;

            par.RemoveChild(only);
            par.AppendChild(nodeProvider.WrapInRun(only));
        }

        private static List<ElementInfo> GetElementInfos(IEnumerable<OpenXmlElement> childrens, INodeProvider nodeProvider)
        {
            var infos = childrens.Select(c => new ElementInfo(c, nodeProvider.IsRun(c) ? nodeProvider.GetText(c) : null)).ToList();

            int currentPosition = 0;
            foreach (ElementInfo ri in infos)
            {
                ri.Interval = new Interval<int>(currentPosition, currentPosition + (ri.Text == null ? 0 : ri.Text.Length));
                currentPosition = ri.Interval.Max;
            }

            return infos;
        }

        class ElementInfo
        {
            public readonly OpenXmlElement Element;
            public readonly string? Text;
            public Interval<int> Interval;

            public ElementInfo(OpenXmlElement element, string? text)
            {
                Element = element;
                Text = text;
            }

            public override string ToString()
            {
                return Interval + " " + Element.LocalName + (Text == null ? null : (": '" + Text + "'"));
            }
        }


        Stack<BlockContainerNode> stack = new Stack<BlockContainerNode>();

        public void CreateNodes()
        {
            foreach (var root in document.AllRootElements())
            {
                var lists = root.Descendants<MatchNode>().ToList();

                foreach (var matchNode in lists)
                {
                    var m = matchNode.Match;

                    var expr = m.Groups["expr"].Value;
                    var keyword = m.Groups["keyword"].Value;
                    var variable = m.Groups["dec"].Value;

                    switch (keyword)
                    {
                        case "":
                            var s = TemplateUtils.SplitToken(expr);
                            if (s == null)
                                AddError(true, "{0} has invalid format".FormatWith(expr));
                            else
                            {
                                var vp = ValueProviderBase.TryParse(s.Value.Token, variable, this);

                                matchNode.Parent.ReplaceChild(new TokenNode(matchNode.NodeProvider, vp!, s.Value.Format!)
                                {
                                    RunProperties = (OpenXmlCompositeElement?)matchNode.RunProperties?.CloneNode(true)
                                }, matchNode);

                                DeclareVariable(vp);
                            }
                            break;
                        case "declare":
                            {
                                var vp = ValueProviderBase.TryParse(expr, variable, this);
                                matchNode.Parent.ReplaceChild(new DeclareNode(matchNode.NodeProvider, vp!, this.AddError)
                                {
                                    RunProperties = (OpenXmlCompositeElement?)matchNode.RunProperties?.CloneNode(true)
                                }, matchNode);

                                DeclareVariable(vp);
                            }
                            break;
                        case "any":
                            {
                                ConditionBase cond = TemplateUtils.ParseCondition(expr, variable, this);
                                AnyNode any = new AnyNode(matchNode.NodeProvider, cond)
                                {
                                    AnyToken = new MatchNodePair(matchNode)
                                };
                                PushBlock(any);

                                if (cond is ConditionCompare cc)
                                    DeclareVariable(cc.ValueProvider);
                                break;
                            }
                        case "notany":
                            {
                                var an = PeekBlock<AnyNode>();
                                if (an != null)
                                {
                                    an.NotAnyToken = new MatchNodePair(matchNode);
                                }
                                break;
                            }
                        case "endany":
                            {
                                var an = PopBlock<AnyNode>();
                                if (an != null)
                                {
                                    an.EndAnyToken = new MatchNodePair(matchNode);

                                    an.ReplaceBlock();
                                }
                                break;
                            }
                        case "if":
                            {
                                var cond = TemplateUtils.ParseCondition(expr, variable, this);
                                IfNode ifn = new IfNode(matchNode.NodeProvider, cond)
                                {
                                    IfToken = new MatchNodePair(matchNode)
                                };
                                PushBlock(ifn);

                                if (cond is ConditionCompare cc)
                                    DeclareVariable(cc.ValueProvider);

                                break;
                            }
                        case "else":
                            {
                                var an = PeekBlock<IfNode>();
                                if (an != null)
                                {
                                    an.ElseToken = new MatchNodePair(matchNode);
                                }
                                break;
                            }
                        case "endif":
                            {
                                var ifn = PopBlock<IfNode>();
                                if (ifn != null)
                                {
                                    ifn.EndIfToken = new MatchNodePair(matchNode);

                                    ifn.ReplaceBlock();
                                }
                                break;
                            }
                        case "foreach":
                            {
                                var vp = ValueProviderBase.TryParse(expr, variable, this);
                                if (vp is TokenValueProvider tvp && tvp.ParsedToken.QueryToken != null && QueryToken.IsCollection(tvp.ParsedToken.QueryToken.Type))
                                    AddError(false, $"@foreach[{expr}] is a collection, missing 'Element' token at the end");

                                var fn = new ForeachNode(matchNode.NodeProvider, vp!) { ForeachToken = new MatchNodePair(matchNode) };
                                PushBlock(fn);

                                DeclareVariable(vp);
                                break;
                            }
                        case "endforeach":
                            {
                                var fn = PopBlock<ForeachNode>();
                                if (fn != null)
                                {
                                    fn.EndForeachToken = new MatchNodePair(matchNode);

                                    fn.ReplaceBlock();
                                }
                                break;
                            }
                        default:
                            AddError(true, "'{0}' is deprecated".FormatWith(keyword));
                            break;
                    }
                }
            }
        }

        void PushBlock(BlockContainerNode node)
        {
            stack.Push(node);
            Variables = new ScopedDictionary<string, ValueProviderBase>(Variables);
        }

        T? PopBlock<T>() where T : BlockContainerNode
        {
            if (stack.IsEmpty())
            {
                AddError(true, "No {0} has been opened".FormatWith(BlockContainerNode.UserString(typeof(T))));
                return null;
            }

            BlockContainerNode n = stack.Pop();
            if (n == null || !(n is T))
            {
                AddError(true, "Unexpected '{0}'".FormatWith(BlockContainerNode.UserString(n?.GetType())));
                return null;
            }

            Variables = Variables.Previous!;
            return (T)n;
        }

        T? PeekBlock<T>() where T : BlockContainerNode
        {
            if (stack.IsEmpty())
            {
                AddError(true, "No {0} has been opened".FormatWith(BlockContainerNode.UserString(typeof(T))));
                return null;
            }

            BlockContainerNode n = stack.Peek();
            if (n == null || !(n is T))
            {
                AddError(true, "Unexpected '{0}'".FormatWith(BlockContainerNode.UserString(n?.GetType())));
                return null;
            }


            Variables = Variables.Previous!;
            Variables = new ScopedDictionary<string, ValueProviderBase>(Variables);
            return (T)n;
        }

        
        public void AddError(bool fatal, string message)
        {
            this.Errors.Add(new TemplateError(fatal, message));
        }


        void DeclareVariable(ValueProviderBase? token)
        {
            if (token?.Variable.HasText() == true)
            {
                if (Variables.TryGetValue(token!.Variable!, out ValueProviderBase t))
                {
                    if (!t.Equals(token))
                        AddError(true, "There is already a variable '{0}' defined in this scope".FormatWith(token.Variable));
                }
                else
                {
                    Variables.Add(token!.Variable!, token);
                }
            }
        }

        public void AssertClean()
        {
            foreach (var root in this.document.AllRootElements())
            {
                var list = root.Descendants<MatchNode>().ToList();

                if (list.Any())
                    throw new InvalidOperationException("{0} unexpected MatchNode instances found: \r\n{1}".FormatWith(list.Count, 
                        list.ToString(d => 
@$"{d.Before()?.InnerText ?? "- None - "}
{d.InnerText} <-- Unexpected
{d.After()?.InnerText ?? "-- None --"}", "\r\n\r\n").Indent(2)));
            }
        }

       
    }

    static class OpenXmlExtensions
    {
        public static OpenXmlElement? Before(this OpenXmlElement element)
        {
            return element.Follow(a => a.Parent).Select(p => p.PreviousSibling()).FirstOrDefault(e => e != null && e.InnerText.HasText());
        }

        public static OpenXmlElement? After(this OpenXmlElement element)
        {
            return element.Follow(a => a.Parent).Select(p => p.NextSibling()).FirstOrDefault(e => e != null && e.InnerText.HasText());
        }
    }
}
