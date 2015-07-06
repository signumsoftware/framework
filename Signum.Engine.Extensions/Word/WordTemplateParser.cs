using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Mailing;
using Signum.Engine.Templating;
using Signum.Engine.Translation;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Entities.UserAssets;
using Signum.Entities.Word;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Signum.Engine.Word
{
    public class WordTemplateParser
    {
        public List<TemplateError> Errors = new List<TemplateError>();
        QueryDescription queryDescription;
        ScopedDictionary<string, ValueProviderBase> variables = new ScopedDictionary<string, ValueProviderBase>(null);
        public readonly Type SystemWordTemplateType;
        WordprocessingDocument document;

        public WordTemplateParser(WordprocessingDocument document, QueryDescription queryDescription, Type systemWordTemplateType)
        {
            this.queryDescription = queryDescription;
            this.SystemWordTemplateType = systemWordTemplateType;
            this.document = document;
        }

        public void ParseDocument()
        {
            foreach (var p in document.RecursivePartsRootElements())
            {
                var paragraphs = p.Descendants<Paragraph>();

                foreach (var par in paragraphs)
                {
                    string text = par.ChildElements.OfType<Run>().ToString(r => GetText(r), "");

                    var matches = TemplateUtils.KeywordsRegex.Matches(text).Cast<Match>().ToList();

                    if (matches.Any())
                    {
                        List<ElementInfo> infos = GetElementInfos(par.ChildElements);

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

                            Run startRun = (Run)start.Element;

                            if (start.Interval.Min < interval.Min)
                            {
                                Run firstRunPart = new Run { RunProperties = startRun.RunProperties.Try(r => (RunProperties)r.CloneNode(true)) };
                                firstRunPart.AppendChild(new Text { Text = start.Text.Substring(0, m.Index - start.Interval.Min), Space = SpaceProcessingModeValues.Preserve });
                                par.Append(firstRunPart);
                            }

                            par.Append(new MatchNode(m) { RunProperties = startRun.RunProperties.Try(r => (RunProperties)r.CloneNode(true)) });

                            ElementInfo end = start;
                            while (end.Interval.Max < interval.Max) //Ignore
                                end = stack.Pop();

                            if (interval.Max < end.Interval.Max) //End
                            {
                                Run endRun = (Run)end.Element;

                                var textPart = end.Text.Substring(interval.Max - end.Interval.Min);
                                Run endRunPart = new Run { RunProperties = endRun.RunProperties.Try(r => (RunProperties)r.CloneNode(true)) };
                                endRunPart.AppendChild(new Text { Text = textPart, Space = SpaceProcessingModeValues.Preserve });

                                stack.Push(new ElementInfo
                                {
                                    Element = endRunPart,
                                    Text = textPart,
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
            }
        }



        private static List<ElementInfo> GetElementInfos(IEnumerable<OpenXmlElement> childrens)
        {
            var infos = childrens.Select(c => new ElementInfo { Element = c, Text = c is Run ? GetText((Run)c) : null }).ToList();

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
            public string Text;
            public OpenXmlElement Element;
            public Interval<int> Interval;

            public override string ToString()
            {
                return Interval + " " + Element.LocalName + (Text == null ? null : (": '" + Text + "'"));
            }
        }

        private static string GetText(Run r)
        {
            return r.ChildElements.OfType<Text>().SingleOrDefault().Try(t => t.Text) ?? "";
        }

        Stack<BlockContainerNode> stack = new Stack<BlockContainerNode>();

        public void CreateNodes()
        {
            foreach (var root in document.RecursivePartsRootElements())
            {
                var lists = root.Descendants<MatchNode>().ToList();

                foreach (var matchNode in lists)
                {
                    var m = matchNode.Match;

                    var type = m.Groups["type"].Value;
                    var token = m.Groups["token"].Value;
                    var keyword = m.Groups["keyword"].Value;
                    var dec = m.Groups["dec"].Value;
                    
                    switch (keyword)
                    {
                        case "":
                            var tok = TemplateUtils.TokenFormatRegex.Match(token);
                            if (!tok.Success)
                                AddError(true, "{0} has invalid format".FormatWith(token));
                            else
                            {
                                var vp = TryParseValueProvider(type, tok.Groups["token"].Value, dec);

                                var format = tok.Groups["format"].Value.DefaultText(null);

                                matchNode.Parent.ReplaceChild(new TokenNode(vp, format)
                                {
                                    RunProperties = matchNode.RunProperties.TryDo(d => d.Remove()) 
                                }, matchNode);

                                DeclareVariable(vp);
                            }
                            break;
                        case "declare":
                            {
                                var vp = TryParseValueProvider(type, token, dec);

                                matchNode.Parent.ReplaceChild(new DeclareNode(vp, this.AddError)
                                {
                                    RunProperties = matchNode.RunProperties.TryDo(d => d.Remove()) 
                                }, matchNode);

                                DeclareVariable(vp);
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
                                    any = new AnyNode(vp) { AnyToken = new MatchNodePair(matchNode) };
                                }
                                else
                                {
                                    vp = TryParseValueProvider(type, filter.Groups["token"].Value, dec);
                                    var comparer = filter.Groups["comparer"].Value;
                                    var value = filter.Groups["value"].Value;
                                    any = new AnyNode(vp, comparer, value, this.AddError) { AnyToken = new MatchNodePair(matchNode) };
                                }

                                PushBlock(any);

                                DeclareVariable(vp);
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
                                IfNode ifn;
                                ValueProviderBase vp;
                                var filter = TemplateUtils.TokenOperationValueRegex.Match(token);
                                if (!filter.Success)
                                {
                                    vp = TryParseValueProvider(type, token, dec);
                                    ifn = new IfNode(vp) { IfToken = new MatchNodePair(matchNode) };
                                }
                                else
                                {
                                    vp = TryParseValueProvider(type, filter.Groups["token"].Value, dec);
                                    var comparer = filter.Groups["comparer"].Value;
                                    var value = filter.Groups["value"].Value;
                                    ifn = new IfNode(vp, comparer, value, this.AddError) { IfToken = new MatchNodePair(matchNode) };
                                }

                                PushBlock(ifn);

                                DeclareVariable(vp);

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
                                var vp = TryParseValueProvider(type, token, dec);
                                var fn = new ForeachNode(vp) { ForeachToken = new MatchNodePair(matchNode) };
                                stack.Push(fn);

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
            variables = new ScopedDictionary<string, ValueProviderBase>(variables);
        }

        T PopBlock<T>() where T : BlockContainerNode
        {
            if (stack.IsEmpty())
            {
                AddError(true, "No {0} has been opened".FormatWith(BlockContainerNode.UserString(typeof(T))));
                return null;
            }

            BlockContainerNode n = stack.Pop();
            if (n == null || !(n is T))
            {
                AddError(true, "Unexpected '{0}'".FormatWith(BlockContainerNode.UserString(n.Try(p => p.GetType()))));
                return null;
            }

            variables = variables.Previous;
            return (T)n;
        }

        T PeekBlock<T>() where T : BlockContainerNode
        {
            if (stack.IsEmpty())
            {
                AddError(true, "No {0} has been opened".FormatWith(BlockContainerNode.UserString(typeof(T))));
                return null;
            }

            BlockContainerNode n = stack.Peek();
            if (n == null || !(n is T))
            {
                AddError(true, "Unexpected '{0}'".FormatWith(BlockContainerNode.UserString(n.Try(p => p.GetType()))));
                return null;
            }


            variables = variables.Previous;
            variables = new ScopedDictionary<string, ValueProviderBase>(variables);
            return (T)n;
        }

        public ValueProviderBase TryParseValueProvider(string type, string token, string variable)
        {
            return ValueProviderBase.TryParse(type, token, variable, this.SystemWordTemplateType, this.queryDescription, this.variables, this.AddError);
        }


        internal void AddError(bool fatal, string message)
        {
            this.Errors.Add(new TemplateError(fatal, message));
        }


        void DeclareVariable(ValueProviderBase token)
        {
            if (token.Variable.HasText())
            {
                ValueProviderBase t;
                if (variables.TryGetValue(token.Variable, out t))
                {
                    if (!t.Equals(token))
                        AddError(true, "There's already a variable '{0}' defined in this scope".FormatWith(token.Variable));
                }
                else
                {
                    variables.Add(token.Variable, token);
                }
            }
        }

        public void AssertClean()
        {
            foreach (var root in this.document.RecursivePartsRootElements())
            {
                var list = root.Descendants<MatchNode>().ToList();

                if (list.Any())
                    throw new InvalidOperationException("{0} unexpected MatchNode instances found".FormatWith(list.Count));
            }
        }
    }
}
