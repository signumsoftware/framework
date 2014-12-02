using DocumentFormat.OpenXml;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Signum.Engine.Word
{
    public class MatchNode : Run
    {
        public Match Match;

        public MatchNode(Match match)
        {
            this.Match = match;
        }
    }

    public abstract class BaseNode : Run
    {
        //internal protected abstract void RenderNode(WordTemplateParameters p, IEnumerable<ResultRow> rows);
    }

    public class TokenNode : BaseNode
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
                return "The entity of {0} ({1}) is not compatible with the property route {2}".FormatWith(token.FullKey(), entityToken.FullKey(), Route.RootType.NiceName());

            return null;
        }

        //internal protected override void RenderNode(WordTemplateParameters p, IEnumerable<ResultRow> rows)
        //{
        //    string text;
        //    if (EntityToken != null)
        //    {
        //        var entity = (Lite<Entity>)rows.DistinctSingle(p.Columns[EntityToken]);
        //        var fallback = (string)rows.DistinctSingle(p.Columns[Token.QueryToken]);

        //        text = entity == null ? null : TranslatedInstanceLogic.TranslatedField(entity, Route, fallback);
        //    }
        //    else
        //    {
        //        object obj = rows.DistinctSingle(p.Columns[Token.QueryToken]);
        //        text = obj is Enum ? ((Enum)obj).NiceToString() :
        //            obj is IFormattable ? ((IFormattable)obj).ToString(Format ?? Token.QueryToken.Format, p.CultureInfo) :
        //            obj.TryToString();
        //    }

        //    this.Parent.ReplaceChild(new Run(this.RunProperties, new Text(text)), this);
        //}
    }

    public class DeclareNode : BaseNode
    {
        public readonly ParsedToken Token;

        internal DeclareNode(ParsedToken token, WordTemplateParser walker)
        {
            if (!token.Variable.HasText())
                walker.AddError(true, "declare[{0}] should end with 'as $someVariable'".FormatWith(token));

            this.Token = token;
        }
    }

    public class ModelNode : BaseNode
    {
        public bool IsRaw { get; set; }

        string fieldOrPropertyChain;
        List<MemberInfo> members;

        public const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal ModelNode(string fieldOrPropertyChain, WordTemplateParser walker)
        {
            if (walker.ModelType == null)
            {
                walker.AddError(false, WordTemplateMessage.ModelShouldBeSetToUseModel0.NiceToString().FormatWith(fieldOrPropertyChain));
                return;
            }

            this.fieldOrPropertyChain = fieldOrPropertyChain;

            members = new List<MemberInfo>();
            var type = walker.ModelType;
            foreach (var field in fieldOrPropertyChain.Split('.'))
            {
                var info = (MemberInfo)type.GetField(field, flags) ??
                           (MemberInfo)type.GetProperty(field, flags);

                if (info == null)
                {
                    walker.AddError(false, WordTemplateMessage.Type0DoesNotHaveAPropertyWithName1.NiceToString().FormatWith(type.Name, field));
                    members = null;
                    break;
                }

                members.Add(info);

                type = info.ReturningType();
            }
        }
    }

    public abstract class BlockNode : BaseNode
    {
        public static string UserString(Type type)
        {
            if (type == typeof(ForeachNode))
                return "foreach";

            if (type == typeof(IfNode))
                return "if";

            if (type == typeof(AnyNode))
                return "any";

            return "block";
        }

        protected internal abstract void ReplaceBlock();

        protected OpenXmlElementPair NormalizeSiblings(OpenXmlElementPair tuple)
        {
            if (tuple.First == tuple.Last)
                throw new ArgumentException("first and last are the same node");

            var chainFirst = ((OpenXmlElement)tuple.First).Follow(a => a.Parent).Reverse().ToList();
            var chainLast = ((OpenXmlElement)tuple.Last).Follow(a => a.Parent).Reverse().ToList();

            var result = chainFirst.Zip(chainLast, (f, i) => new OpenXmlElementPair(f, i)).First(a => a.First != a.Last);
            AssertNotImportant(chainFirst, result.First);
            AssertNotImportant(chainLast, result.Last);

            return result;
        }

        public struct OpenXmlElementPair
        {
            public readonly OpenXmlElement First;
            public readonly OpenXmlElement Last;

            public OpenXmlElementPair(OpenXmlElement first, OpenXmlElement last)
            {
                this.First = first;
                this.Last = last;
            }

            public OpenXmlElement CommonParent
            {
                get
                {
                    if (First.Parent != Last.Parent)
                        throw new InvalidOperationException("Parents do not match");

                    return First.Parent;
                }
            }
        }

        private void AssertNotImportant(List<OpenXmlElement> chain, OpenXmlElement openXmlElement)
        {
            var index = chain.IndexOf(openXmlElement);

            for (int i = index; i < chain.Count; i++)
            {
                var current = chain[i];
                var next = i == chain.Count - 1 ? null : chain[i + 1];

                var important = current.ChildElements.Where(c => c != next && IsImportant(c));

                if (important.Any())
                    throw new InvalidOperationException("Some important nodes are being removed:\r\n" + important.ToString(a => a.NiceToString(), "\r\n\r\n"));
            }
        }

        private bool IsImportant(OpenXmlElement c)
        {
            return c is Run || c is Paragraph;
        }

        protected static List<OpenXmlElement> NodesBetween(OpenXmlElementPair pair)
        {
            var parent = pair.CommonParent;

            int indexFirst = parent.ChildElements.IndexOf(pair.First);
            if (indexFirst == -1)
                throw new InvalidOperationException("Element not found");

            int indexLast = parent.ChildElements.IndexOf(pair.Last);
            if (indexLast == -1)
                throw new InvalidOperationException("Element not found");

            var childs = parent.ChildElements.Where((e, i) => indexFirst < i && i < indexLast).ToList();
            return childs;
        }

        public static void MoveTo(IEnumerable<OpenXmlElement> childs, OpenXmlElement target)
        {
            foreach (var c in childs)
            {
                c.Remove();
                target.AppendChild(c);
            }
        }
    }

    public class ForeachNode : BlockNode
    {
        public readonly ParsedToken Token;

        public MatchNode ForeachToken;
        public MatchNode EndForeachToken;

        public ForeachNode(ParsedToken token)
        {
            this.Token = token;
        }

        protected internal override void ReplaceBlock()
        {
            OpenXmlElementPair pair = this.NormalizeSiblings(new OpenXmlElementPair(ForeachToken, EndForeachToken));

            MoveTo(NodesBetween(pair), this);

            pair.CommonParent.ReplaceChild(this, pair.First);
            pair.Last.Remove();
        }

       
    }

    public class AnyNode : BlockNode
    {
        public readonly ParsedToken Token;
        public readonly FilterOperation? Operation;
        public string Value;

        public MatchNode AnyToken;
        public MatchNode NotAnyToken;
        public MatchNode EndAnyToken;

        public OpenXmlElement AnyBlock;
        public OpenXmlElement NotAnyBlock;

        public AnyNode(ParsedToken token, WordTemplateParser parser)
        {
            if (token.QueryToken != null && token.QueryToken.HasAllOrAny())
                parser.AddError(false, "Any {0} can not contains Any or All".FormatWith(token.QueryToken));

            this.Token = token;
        }

        internal AnyNode(ParsedToken token, string operation, string value, WordTemplateParser parser)
        {
            if (token.QueryToken != null && token.QueryToken.HasAllOrAny())
                parser.AddError(false, "Any {0} can not contains Any or All");

            this.Token = token;
            this.Operation = FilterValueConverter.ParseOperation(operation);
            this.Value = value;

            if (Token.QueryToken != null)
            {
                object rubish;
                string error = FilterValueConverter.TryParse(Value, Token.QueryToken.Type, out rubish, Operation == FilterOperation.IsIn);

                if (error.HasText())
                    parser.AddError(false, error);
            }
        }

        protected internal override void ReplaceBlock()
        {
            if (this.NotAnyToken == null)
            {
                OpenXmlElementPair pair = this.NormalizeSiblings(new OpenXmlElementPair(AnyToken, EndAnyToken));

                MoveTo(NodesBetween(pair), this);

                pair.CommonParent.ReplaceChild(this, pair.First);
                pair.Last.Remove();
            }
            else
            {
                OpenXmlElementPair pairAny = this.NormalizeSiblings(new OpenXmlElementPair(AnyToken, NotAnyToken));
                OpenXmlElementPair pairNotAny = this.NormalizeSiblings(new OpenXmlElementPair(NotAnyToken, EndAnyToken));

                if (pairAny.Last != pairNotAny.First)
                    throw new InvalidOperationException("Unbalanced tokens");

                this.AnyBlock = new Paragraph();
                MoveTo(NodesBetween(pairAny), this.AnyBlock);

                this.NotAnyBlock = new Paragraph();
                MoveTo(NodesBetween(pairAny), this.NotAnyBlock);

                pairAny.CommonParent.ReplaceChild(this, pairAny.First);
                pairAny.Last.Remove();
            }
        }
    }

    public class IfNode : BlockNode
    {
        public readonly ParsedToken Token;

        private FilterOperation? Operation;
        private string Value;

        public MatchNode IfToken;
        public MatchNode ElseToken;
        public MatchNode EndIfToken;

        public OpenXmlElement IfBlock;
        public OpenXmlElement ElseBlock;
        
        internal IfNode(ParsedToken token, WordTemplateParser parser)
        {
            this.Token = token;
        }

        internal IfNode(ParsedToken token, string operation, string value, WordTemplateParser walker)
        {
            this.Token = token;
            this.Operation = FilterValueConverter.ParseOperation(operation);
            this.Value = value;

            if (Token.QueryToken != null)
            {
                object rubish;
                string error = FilterValueConverter.TryParse(Value, Token.QueryToken.Type, out rubish, Operation == FilterOperation.IsIn);

                if (error.HasText())
                    walker.AddError(false, error);
            }
        }

        protected internal override void ReplaceBlock()
        {
            if (this.ElseToken == null)
            {
                OpenXmlElementPair pair = this.NormalizeSiblings(new OpenXmlElementPair(IfToken, EndIfToken));

                MoveTo(NodesBetween(pair), this);

                pair.CommonParent.ReplaceChild(this, pair.First);
                pair.Last.Remove();
            }
            else
            {
                OpenXmlElementPair pairAny = this.NormalizeSiblings(new OpenXmlElementPair(IfToken, ElseToken));
                OpenXmlElementPair pairNotAny = this.NormalizeSiblings(new OpenXmlElementPair(ElseToken, EndIfToken));

                if (pairAny.Last != pairNotAny.First)
                    throw new InvalidOperationException("Unbalanced tokens");

                this.IfBlock = new Paragraph();
                MoveTo(NodesBetween(pairAny), this.IfBlock);

                this.ElseBlock = new Paragraph();
                MoveTo(NodesBetween(pairAny), this.ElseBlock);

                pairAny.CommonParent.ReplaceChild(this, pairAny.First);
                pairAny.Last.Remove();
            }
        }
    }
}
