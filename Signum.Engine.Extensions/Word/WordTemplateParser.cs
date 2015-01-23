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
        public static Dictionary<string, Func<GlobalVarContext, object>> GlobalVariables = new Dictionary<string, Func<GlobalVarContext, object>>();


        public List<Error> Errors = new List<Error>();
        QueryDescription queryDescription;
        ScopedDictionary<string, ParsedToken> variables = new ScopedDictionary<string, ParsedToken>(null);
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

                    IEnumerable<Match> matches = TemplateUtils.KeywordsRegex.Matches(text).Cast<Match>().ToList();

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
                                firstRunPart.AppendChild(new Text { Text = start.Text.Substring(0, m.Index - start.Interval.Min) });
                                par.Append(firstRunPart);
                            }

                            par.Append(new MatchNode(m) { RunProperties = startRun.RunProperties.TryDo(r => r.Remove()) });

                            ElementInfo end = start;
                            while (end.Interval.Max < interval.Max) //Ignore
                                end = stack.Pop();

                            if (interval.Max < end.Interval.Max) //End
                            {
                                Run endRun = (Run)end.Element;

                                var textPart = end.Text.Substring(interval.Max - end.Interval.Min);
                                Run endRunPart = new Run { RunProperties = endRun.RunProperties.Try(r => (RunProperties)r.CloneNode(true)) };
                                endRunPart.AppendChild(new Text { Text = textPart });

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

                    var token = m.Groups["token"].Value;
                    var keyword = m.Groups["keyword"].Value;
                    var dec = m.Groups["dec"].Value;

                    switch (keyword)
                    {
                        case "":
                            var tok = TemplateUtils.TokenFormatRegex.Match(token);
                            if (!tok.Success)
                                Errors.Add(new Error(true, "{0} has invalid format".FormatWith(token)));
                            else
                            {
                                var t = TryParseToken(tok.Groups["token"].Value, dec, SubTokensOptions.CanElement);

                                var format = tok.Groups["format"].Value;

                                matchNode.Parent.ReplaceChild(new TokenNode(t, format, this)
                                {
                                    RunProperties = matchNode.RunProperties.TryDo(d => d.Remove())
                                }, matchNode);

                                DeclareVariable(t);
                            }
                            break;
                        case "declare":
                            {
                                var t = TryParseToken(token, dec, SubTokensOptions.CanElement);

                                matchNode.Parent.ReplaceChild(new DeclareNode(t, this), matchNode);

                                DeclareVariable(t);
                            }
                            break;
                        case "model":
                        case "modelraw":
                            {
                                var model = new ModelNode(token, walker: this)
                                {
                                    IsRaw = keyword == "modelraw",
                                    RunProperties = matchNode.RunProperties.TryDo(d => d.Remove())
                                };

                                matchNode.Parent.ReplaceChild(model, matchNode);
                            }
                            break;
                        case "any":
                            {
                                AnyNode any;
                                ParsedToken t;
                                var filter = TemplateUtils.TokenOperationValueRegex.Match(token);
                                if (!filter.Success)
                                {
                                    t = TryParseToken(token, dec, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll);
                                    any = new AnyNode(t, this) { AnyToken = new MatchNodePair(matchNode) };
                                }
                                else
                                {
                                    t = TryParseToken(filter.Groups["token"].Value, dec, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll);
                                    var comparer = filter.Groups["comparer"].Value;
                                    var value = filter.Groups["value"].Value;
                                    any = new AnyNode(t, comparer, value, this) { AnyToken = new MatchNodePair(matchNode) };
                                }

                                PushBlock(any);

                                DeclareVariable(t);
                                break;
                            }
                        case "notany":
                            {
                                var an = PeekBlock<AnyNode>();
                                an.NotAnyToken = new MatchNodePair(matchNode);
                                break;
                            }
                        case "endany":
                            {
                                var an = PopBlock<AnyNode>();
                                an.EndAnyToken = new MatchNodePair(matchNode);

                                an.ReplaceBlock();

                                break;
                            }
                        case "if":
                            {
                                IfNode ifn;
                                ParsedToken t;
                                var filter = TemplateUtils.TokenOperationValueRegex.Match(token);
                                if (!filter.Success)
                                {
                                    t = TryParseToken(token, dec, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll);
                                    ifn = new IfNode(t, this) { IfToken = new MatchNodePair(matchNode) };
                                }
                                else
                                {
                                    t = TryParseToken(filter.Groups["token"].Value, dec, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll);
                                    var comparer = filter.Groups["comparer"].Value;
                                    var value = filter.Groups["value"].Value;
                                    ifn = new IfNode(t, comparer, value, this) { IfToken = new MatchNodePair(matchNode) };
                                }

                                PushBlock(ifn);

                                DeclareVariable(t);

                                break;
                            }
                        case "else":
                            {
                                var an = PeekBlock<IfNode>();
                                an.ElseToken = new MatchNodePair(matchNode);

                                break;
                            }
                        case "endif":
                            {
                                var ifn = PopBlock<IfNode>();
                                ifn.EndIfToken = new MatchNodePair(matchNode);

                                ifn.ReplaceBlock();

                                break;
                            }
                        case "foreach":
                            {
                                var t = TryParseToken(token, dec, SubTokensOptions.CanElement);
                                var fn = new ForeachNode(t) { ForeachToken = new MatchNodePair(matchNode) };
                                stack.Push(fn);

                                DeclareVariable(t);
                                break;
                            }
                        case "endforeach":
                            {
                                var fn = PopBlock<ForeachNode>();
                                fn.EndForeachToken = new MatchNodePair(matchNode);

                                fn.ReplaceBlock();
                                break;
                            }
                    }
                }
            }
        }

        void PushBlock(BlockContainerNode node)
        {
            stack.Push(node);
            variables = new ScopedDictionary<string, ParsedToken>(variables);
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
            variables = new ScopedDictionary<string, ParsedToken>(variables);
            return (T)n;
        }

        private ParsedToken TryParseToken(string tokenString, string variable, SubTokensOptions subTokensOptions)
        {
            string error;
            var result = ParsedToken.TryParseToken(tokenString, variable, subTokensOptions, this.queryDescription, this.variables, out error);
            if (error != null)
                this.Errors.Add(new Error(true, error));
            return result;
        }


        internal void AddError(bool fatal, string message)
        {
            this.Errors.Add(new Error { IsFatal = fatal, Message = message });
        }


        void DeclareVariable(ParsedToken token)
        {
            if (token.Variable.HasText())
            {
                ParsedToken t;
                if (variables.TryGetValue(token.Variable, out t))
                {
                    if (!t.QueryToken.Equals(token.QueryToken))
                        this.Errors.Add(new Error(true, "There's already a variable '{0}' defined in this scope".FormatWith(token.Variable)));
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


    public struct Error
    {
        public Error(bool isFatal, string message)
        {
            this.Message = message;
            this.IsFatal = isFatal;
        }

        public string Message;
        public bool IsFatal;
    }
}
