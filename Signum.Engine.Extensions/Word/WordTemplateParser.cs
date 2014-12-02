using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Mailing;
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
        List<Error> Errors = new List<Error>();
        QueryDescription queryDescription;
        ScopedDictionary<string, ParsedToken> variables = new ScopedDictionary<string, ParsedToken>(null);
        public Type ModelType;

        public WordTemplateParser(QueryDescription queryDescription, Type modelType)
        {
            this.queryDescription = queryDescription;
            this.ModelType = modelType;
        }

        public void ParseDocument(byte[] template)
        {
            using (var memory = new MemoryStream(template))
            {
                using (WordprocessingDocument wordProcessingDocument = WordprocessingDocument.Open(memory, true))
                {
                    ParseDocument(wordProcessingDocument);

                    CreateStructures(wordProcessingDocument);
                }
            }
        }

        

        void ParseDocument(WordprocessingDocument wordprocessingDocument)
        {  
            var paragraphs = wordprocessingDocument.MainDocumentPart.Document.Descendants<Paragraph>();

            foreach (var par in paragraphs)
            {
                List<RunInfo> runs =
                    (from r in par.ChildElements.OfType<Run>()
                     select new RunInfo
                     {
                         Text = r.ChildElements.OfType<Text>().SingleOrDefault().Try(t => t.Text) ?? "",
                         Run = r,
                     }).ToList();

                string text = runs.Select(r => r.Text).ToString("");

                IEnumerable<Match> matches = TemplateRegex.KeywordsRegex.Matches(text).Cast<Match>().ToList();

                if (matches.Any())
                {
                    int currentPosition = 0;
                    foreach (var item in runs)
                    {
                        item.Index = currentPosition;
                        item.Lenght = item.Text.Length;
                        currentPosition += item.Lenght;
                    }

                    foreach (var m in matches)
                    {
                        int firstChar = m.Index;
                        int lastChar = m.Index + m.Length - 1;
                        RunInfo first = runs.Single(r => r.Contains(firstChar));
                        RunInfo last = runs.Single(r => r.Contains(lastChar));

                        int firstIndex = par.ChildElements.IndexOf(first.Run);
                        int lastIndex = par.ChildElements.IndexOf(last.Run);

                        var selectedRuns = par.ChildElements.Where((r, i) => firstIndex <= i && i <= lastIndex).ToList();

                        foreach (var item in selectedRuns)
                            item.Remove();


                        var index = firstIndex;
                        if (first.Index < m.Index)
                        {
                            Run firstRunPart = new Run { RunProperties = first.Run.RunProperties };
                            firstRunPart.AppendChild(new Text { Text = first.Text.Substring(0, m.Index - first.Index) });
                            par.InsertAt(firstRunPart, index++);
                        }

                        par.InsertAt(new MatchNode(m), index++);

                        if (lastChar < last.Index + last.Lenght - 1)
                        {
                            last.Run.Remove(); //was not included

                            Run lastRunPart = new Run { RunProperties = last.Run.RunProperties };
                            lastRunPart.AppendChild(new Text { Text = first.Text.Substring(lastChar - last.Index) });
                            par.InsertAt(lastRunPart, index++);
                        }
                    }
                }
            }
        }
        
        Stack<BlockNode> stack = new Stack<BlockNode>();

        void CreateStructures(WordprocessingDocument wordProcessingDocument)
        {
            var lists = wordProcessingDocument.MainDocumentPart.Document.Descendants<MatchNode>().ToList();
            
            foreach (var matchNode in lists)
            {
                var m = matchNode.Match;

                var token = m.Groups["token"].Value;
                var keyword = m.Groups["keyword"].Value;
                var dec = m.Groups["dec"].Value;

                switch (keyword)
                {
                    case "":
                    case "raw":
                        var tok = TemplateRegex.TokenFormatRegex.Match(token);
                        if (!tok.Success)
                            Errors.Add(new Error(true, "{0} has invalid format".Formato(token)));
                        else
                        {
                            var t = TryParseToken(tok.Groups["token"].Value, dec, SubTokensOptions.CanElement);

                            var format = tok.Groups["format"].Value;
                            var isRaw = keyword.Contains("raw");

                            matchNode.Parent.ReplaceChild(new TokenNode(t, format, isRaw, this), matchNode);

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
                            var model = new ModelNode(token, walker: this) { IsRaw = keyword == "modelraw" };

                            matchNode.Parent.ReplaceChild(model, matchNode);
                        }
                        break;
                    case "any":
                        {
                            AnyNode any;
                            ParsedToken t;
                            var filter = TemplateRegex.TokenOperationValueRegex.Match(token);
                            if (!filter.Success)
                            {
                                t = TryParseToken(token, dec, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll);
                                any = new AnyNode(t, this) { AnyToken = matchNode };
                            }
                            else
                            {
                                t = TryParseToken(filter.Groups["token"].Value, dec, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll);
                                var comparer = filter.Groups["comparer"].Value;
                                var value = filter.Groups["value"].Value;
                                any = new AnyNode(t, comparer, value, this) { AnyToken = matchNode };
                            }

                            stack.Push(any);

                            DeclareVariable(t);
                            break;
                        }
                    case "notany":
                        {
                            var an = PeekBlock<AnyNode>();
                            an.NotAnyToken = matchNode;
                            break;
                        }
                    case "endany":
                        {
                            var an = PopBlock<AnyNode>();
                            an.EndAnyToken = matchNode;

                            an.ReplaceBlock(); 

                            break;
                        }
                    case "if":
                        {
                            IfNode ifn;
                            ParsedToken t;
                            var filter = TemplateRegex.TokenOperationValueRegex.Match(token);
                            if (!filter.Success)
                            {
                                t = TryParseToken(token, dec, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll);
                                ifn = new IfNode(t, this) { IfToken = matchNode };
                            }
                            else
                            {
                                t = TryParseToken(filter.Groups["token"].Value, dec, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll);
                                var comparer = filter.Groups["comparer"].Value;
                                var value = filter.Groups["value"].Value;
                                ifn = new IfNode(t, comparer, value, this) { IfToken = matchNode };
                            }

                            stack.Push(ifn);

                            DeclareVariable(t);

                            break;
                        }
                    case "else":
                        {
                            var an = PeekBlock<IfNode>();
                            an.ElseToken = matchNode;

                            break;
                        }
                    case "endif":
                        {
                            var ifn = PopBlock<IfNode>();
                            ifn.EndIfToken = matchNode;

                            ifn.ReplaceBlock();

                            break;
                        }
                    case "foreach":
                        {
                            var t = TryParseToken(token, dec, SubTokensOptions.CanElement);
                            var fn = new ForeachNode(t) { ForeachToken = matchNode };
                            stack.Push(fn);
                            
                            DeclareVariable(t);
                            break;
                        }
                    case "endforeach":
                        {
                            var fn = PopBlock<ForeachNode>();
                            fn.EndForeachToken = matchNode;

                            fn.ReplaceBlock();
                            break;
                        }
                }
            }
        }

        public T PopBlock<T>() where T : BlockNode
        {
            if (stack.Count() <= 1)
            {
                AddError(true, "No {0} has been opened".Formato(BlockNode.UserString(typeof(T))));
                return null;
            }
            BlockNode n = stack.Pop();
            variables = variables.Previous;
            if (n == null || !(n is T))
            {
                AddError(true, "Unexpected '{0}'".Formato(BlockNode.UserString(n.Try(p => p.GetType()))));
                return null;
            }
            return (T)n;
        }

        public T PeekBlock<T>() where T : BlockNode
        {
            if (stack.Count() <= 1)
            {
                AddError(true, "No {0} has been opened".Formato(BlockNode.UserString(typeof(T))));
                return null;
            }
            BlockNode n = stack.Peek();
            if (n == null || !(n is T))
            {
                AddError(true, "Unexpected '{0}'".Formato(BlockNode.UserString(n.Try(p => p.GetType()))));
                return null;
            }
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
                if(variables.TryGetValue(token.Variable, out t))
                {
                    if(!t.QueryToken.Equals(token.QueryToken))
                        this.Errors.Add(new Error(true, "There's already a variable '{0}' defined in this scope".Formato(token.Variable)));
                }
                else
                {
                    variables.Add(token.Variable, token);
                }
            }
        }
    }

    class RunInfo
    {
        public string Text;
        public Run Run;
        public int Index;
        public int Lenght;

        internal bool Contains(int index)
        {
            return Index <= index && index < Index + Lenght;
        }
    }

    struct Error
    {
        public Error(bool isFatal, string message)
        {
            this.Message = message;
            this.IsFatal = isFatal;
        }

        public string Message;
        public bool IsFatal;
    }

    public static class OpenXmlElementExtensions
    {
        public static string NiceToString(this OpenXmlElement element)
        {
            using (var sw = new StringWriter())
            using (var xtw = new XmlTextWriter(sw) { Formatting = Formatting.Indented })
            {
                element.WriteTo(xtw);
                return sw.ToString();
            }
        }
    }
}
