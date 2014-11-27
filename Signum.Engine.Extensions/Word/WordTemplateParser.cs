using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Mailing;
using Signum.Engine.Translation;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
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

namespace Signum.Engine.Word
{
    public class WordTemplateParser
    {
        List<Error> Errors = new List<Error>();
        QueryDescription queryDescription;
        ScopedDictionary<string, ParsedToken> variables = new ScopedDictionary<string, ParsedToken>(null);

        public WordTemplateParser(QueryDescription queryDescription)
        {
            this.queryDescription = queryDescription; 
        }

        public WordprocessingDocument ParseDocument(byte[] template)
        {
            using(var memory = new MemoryStream(template))
            {
                using (WordprocessingDocument wordprocessingDocument = WordprocessingDocument.Open(memory, true))
                {
                    ParseDocument(wordprocessingDocument);

                    return wordprocessingDocument;
                }
            }
        }

        private void ParseDocument(WordprocessingDocument wordprocessingDocument)
        {
            var paragraphs = wordprocessingDocument.MainDocumentPart.Document.Descendants<Paragraph>();

            foreach (var par in paragraphs)
            {
                List<RunInfo> runs =
                    (from r in par.ChildElements.OfType<Run>()
                     select new RunInfo
                     {
                         Text = r.ChildElements.OfType<Text>().Single().Text,
                         Run = r,
                     }).ToList();


                int currentPosition = 0;
                foreach (var item in runs)
                {
                    item.Index = currentPosition;
                    item.Lenght = item.Text.Length;
                    currentPosition += item.Lenght;
                }

                string text = runs.Select(r => r.Text).ToString("");

                IEnumerable<Match> matches = TemplateRegex.KeywordsRegex.Matches(text).Cast<Match>();

                foreach (var m in matches)
                {
                    RunInfo first = runs.Single(r => r.Contains(m.Index));
                    RunInfo last = runs.Single(r => r.Contains(m.Index + m.Length));

                    int firstIndex = par.ChildElements.IndexOf(first.Run);
                    int secondIndex = par.ChildElements.IndexOf(first.Run);

                    var selectedRuns = par.ChildElements.Where((r, i) => firstIndex <= i && i < secondIndex).ToList();

                    foreach (var item in selectedRuns)
                        item.Remove();

                    if (first.Index < m.Index)
                    {
                        Run firstRunPart = new Run { RunProperties = first.Run.RunProperties };
                        firstRunPart.AppendChild(new Text { Text = first.Text.Substring(0, m.Index - first.Index) });
                        par.AppendChild(firstRunPart);
                    }

                    var token = m.Groups["token"].Value;
                    var keyword = m.Groups["keyword"].Value;
                    var dec = m.Groups["dec"].Value;

                    switch (token)
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

                                par.AppendChild(new TokenNode(TryParseToken(m.Value, dec, SubTokensOptions.CanElement), format, isRaw, this));

                                DeclareVariable(t);
                            }
                            break;
                        case "declare":
                            {
                                var t = TryParseToken(token, dec, SubTokensOptions.CanElement);

                                par.AppendChild(new DeclareNode(t, this));

                                DeclareVariable(t);
                            }
                            break;
                        case "model":
                        case "modelraw":
                            var model = new ModelNode(token, modelType, walker: this) { IsRaw = keyword == "modelraw" };

                            break;


                    }

                 
                    if (last.Index < m.Index + m.Length)
                    {
                        last.Run.Remove(); //was not included

                        Run lastRunPart = new Run { RunProperties = last.Run.RunProperties };
                        lastRunPart.AppendChild(new Text { Text = first.Text.Substring(m.Index + m.Length - last.Index) });
                        par.AppendChild(lastRunPart);
                    }
                }
            }
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
                if (variables.ContainsKey(token.Variable))
                    this.Errors.Add(new Error(true, "There's already a variable '{0}' defined in this scope".Formato(token.Variable)));

                variables.Add(token.Variable, token);
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
            return Index < index && index < Index + Lenght;
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

    public class TokenNode : Run
    {
        public readonly bool IsRaw;

        public readonly ParsedToken Token;
        public readonly QueryToken EntityToken;
        public readonly string Format;
        public readonly PropertyRoute Route;

        internal TokenNode(ParsedToken token, string format, bool isRaw, WordTemplateParser parser)
        {
            this.Token = token;
            this.Format = format;
            this.IsRaw = isRaw;

            if (token.QueryToken != null && IsTranslateInstanceCanditate(token.QueryToken))
            {
                Route = token.QueryToken.GetPropertyRoute();
                string error = DeterminEntityToken(token.QueryToken, out EntityToken);
                if (error != null)
                    parser.AddError(false, error);
            }
        }

        static bool IsTranslateInstanceCanditate(QueryToken token)
        {
            if (token.Type != typeof(string))
                return false;

            var pr = token.GetPropertyRoute();
            if (pr == null)
                return false;

            if (TranslatedInstanceLogic.RouteType(pr) == null)
                return false;

            return true;
        }

        string DeterminEntityToken(QueryToken token, out QueryToken entityToken)
        {
            entityToken = token.Follow(a => a.Parent).FirstOrDefault(a => a.Type.IsLite() || a.Type.IsIEntity());

            if (entityToken == null)
                entityToken = QueryUtils.Parse("Entity", DynamicQueryManager.Current.QueryDescription(token.QueryName), 0);

            if (entityToken.Type.IsAssignableFrom(Route.RootType))
                return "The entity of {0} ({1}) is not compatible with the property route {2}".Formato(token.FullKey(), entityToken.FullKey(), Route.RootType.NiceName());

            return null;
        }
    }

    public class DeclareNode : Run
    {
        public readonly ParsedToken Token;

        internal DeclareNode(ParsedToken token, WordTemplateParser walker)
        {
            if (!token.Variable.HasText())
                walker.AddError(true, "declare[{0}] should end with 'as $someVariable'".Formato(token));

            this.Token = token;
        }
    }

    public class ModelNode : Run
    {
        public bool IsRaw { get; set; }

        string fieldOrPropertyChain;
        List<MemberInfo> members;
        internal ModelNode(string fieldOrPropertyChain, Type systemEmail, WordTemplateParser walker)
        {
            if (systemEmail == null)
            {
                walker.AddError(false, EmailTemplateMessage.SystemEmailShouldBeSetToAccessModel0.NiceToString().Formato(fieldOrPropertyChain));
                return;
            }

            this.fieldOrPropertyChain = fieldOrPropertyChain;

            members = new List<MemberInfo>();
            var type = systemEmail;
            foreach (var field in fieldOrPropertyChain.Split('.'))
            {
                var info = (MemberInfo)type.GetField(field, flags) ??
                           (MemberInfo)type.GetProperty(field, flags);

                if (info == null)
                {
                    walker.AddError(false, EmailTemplateMessage.Type0DoesNotHaveAPropertyWithName1.NiceToString().Formato(type.Name, field));
                    members = null;
                    break;
                }

                members.Add(info);

                type = info.ReturningType();
            }
        }
    }
}
