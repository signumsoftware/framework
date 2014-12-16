using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Templating;
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
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Signum.Engine.Word
{
    public class MatchNode : Run
    {
        public Match Match;

        public MatchNode(Match match)
        {
            this.Match = match;
        }

        public override string ToString()
        {
            return "Match " + Match.ToString();
        }

        public override string LocalName
        {
            get { return this.GetType().Name; }
        }

        public override void WriteTo(System.Xml.XmlWriter xmlWriter)
        {
            var tempText = new Text(Match.ToString());

            this.AppendChild(tempText);
            base.WriteTo(xmlWriter);
            this.RemoveChild(tempText);
        }
    }

    public abstract class BaseNode : Run
    {
        public BaseNode() { }

        public BaseNode(BaseNode original)
        {
            this.SetAttributes(original.GetAttributes().ToList());
            foreach (var item in original.ChildElements)
            {
                this.AppendChild(item.CloneNode(true));
            }
        }

        public abstract void FillTokens(List<QueryToken> tokens);

        public override string LocalName
        {
            get { return this.GetType().Name; }
        }

        internal protected abstract void RenderNode(WordTemplateParameters p, IEnumerable<ResultRow> rows);

        public override string ToString()
        {
            return this.GetType().Name;
        }

        public abstract override OpenXmlElement CloneNode(bool deep);

        abstract void Synchronize(SyncronizationContext sc);
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

        internal TokenNode(TokenNode original) : base(original)
        {
            this.IsRaw = original.IsRaw;
            this.Token = original.Token;
            this.EntityToken = original.EntityToken;
            this.Format = original.Format;
            this.Route = original.Route;
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

        public override void FillTokens(List<QueryToken> tokens)
        {
            tokens.Add(this.Token.QueryToken);
        }

        internal protected override void RenderNode(WordTemplateParameters p, IEnumerable<ResultRow> rows)
        {
            string text;
            if (EntityToken != null)
            {
                var entity = (Lite<Entity>)rows.DistinctSingle(p.Columns[EntityToken]);
                var fallback = (string)rows.DistinctSingle(p.Columns[Token.QueryToken]);

                text = entity == null ? null : TranslatedInstanceLogic.TranslatedField(entity, Route, fallback);
            }
            else
            {
                object obj = rows.DistinctSingle(p.Columns[Token.QueryToken]);
                text = obj is Enum ? ((Enum)obj).NiceToString() :
                    obj is IFormattable ? ((IFormattable)obj).ToString(Format ?? Token.QueryToken.Format, p.CultureInfo) :
                    obj.TryToString();
            }

            this.Parent.ReplaceChild(new Run(this.RunProperties.TryDo(prop => prop.Remove()), new Text(text)), this);
        }

        public override void WriteTo(System.Xml.XmlWriter xmlWriter)
        {
            var tempText = new Text(Token.QueryToken.Try(q => q.FullKey()) ?? "Error!");

            this.AppendChild(tempText);
            base.WriteTo(xmlWriter);
            this.RemoveChild(tempText);
        }

        public override OpenXmlElement CloneNode(bool deep)
        {
            return new TokenNode(this);
        }
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

        public DeclareNode(DeclareNode original) : base(original)
        {
            this.Token = original.Token;
        }

        public override void FillTokens(List<QueryToken> tokens)
        {
        }

        public override OpenXmlElement CloneNode(bool deep)
        {
            return new DeclareNode(this);
        }

        protected internal override void RenderNode(WordTemplateParameters p, IEnumerable<ResultRow> rows)
        {
            this.Remove();
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
            if (walker.SystemWordTemplateType == null)
            {
                walker.AddError(false, WordTemplateMessage.ModelShouldBeSetToUseModel0.NiceToString().FormatWith(fieldOrPropertyChain));
                return;
            }

            this.fieldOrPropertyChain = fieldOrPropertyChain;

            members = new List<MemberInfo>();
            var type = walker.SystemWordTemplateType;
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

        public ModelNode(ModelNode original) : base(original)
        {
            this.IsRaw = original.IsRaw;
            this.fieldOrPropertyChain = original.fieldOrPropertyChain;
            this.members = original.members;
        }

        public override OpenXmlElement CloneNode(bool deep)
        {
            return new ModelNode(this);
        }

        public override void FillTokens(List<QueryToken> tokens)
        {
        }

        protected internal override void RenderNode(WordTemplateParameters p, IEnumerable<ResultRow> rows)
        {
            if (p.SystemWordTemplate == null)
                throw new ArgumentException("There is no system email for the message composition");

            object value = p.SystemWordTemplate;
            foreach (var m in members)
            {
                value = Getter(m, value);
                if (value == null)
                    break;
            }

            this.Parent.ReplaceChild(new Run(this.RunProperties.TryDo(param => param.Remove()), new Text(value.ToString())), this);
        }

        static object Getter(MemberInfo member, object systemEmail)
        {
            var pi = member as PropertyInfo;

            if (pi != null)
                return pi.GetValue(systemEmail, null);

            return ((FieldInfo)member).GetValue(systemEmail);
        }
    }

    public class BlockNode : BaseNode
    {
        public BlockNode() { }

        public BlockNode(BlockNode original) : base(original) { }

        public override OpenXmlElement CloneNode(bool deep)
        {
            return new BlockNode(this);
        }

        public override void FillTokens(List<QueryToken> tokens)
        {
            foreach (var item in this.Descendants<BaseNode>().ToList())
            {
                item.FillTokens(tokens);
            }
        }

        protected internal override void RenderNode(WordTemplateParameters p, IEnumerable<ResultRow> rows)
        {
            foreach (var item in this.Descendants<BaseNode>().ToList())
            {
                item.RenderNode(p, rows);
            }

            var parent = this.Parent;
            int index = parent.ChildElements.IndexOf(this);
            parent.RemoveChild(this);
  
            foreach (var item in this.ChildElements.ToList())
            {
                item.Remove();
                parent.InsertAt(item, index++);
            }   
        }
    }

    public abstract class BlockContainerNode : BaseNode
    {
        public BlockContainerNode() { }

        public BlockContainerNode(BlockContainerNode original) : base(original) { }

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

        public static void MoveTo(IEnumerable<OpenXmlElement> childs, BlockNode target)
        {
            foreach (var c in childs)
            {
                c.Remove();
                target.AppendChild(c);
            }
        }
    }

    public class ForeachNode : BlockContainerNode
    {
        public readonly ParsedToken Token;

        public MatchNode ForeachToken;
        public MatchNode EndForeachToken;

        public BlockNode ForeachBlock;

        public ForeachNode(ParsedToken token)
        {
            this.Token = token;
        }

        public ForeachNode(ForeachNode original)
            : base(original)
        {
            this.Token = original.Token;
            this.ForeachToken = (MatchNode)original.ForeachToken.Try(a => a.CloneNode(true));
            this.EndForeachToken = (MatchNode)original.EndForeachToken.Try(a => a.CloneNode(true));
            this.ForeachBlock = (BlockNode)original.ForeachBlock.Try(a => a.CloneNode(true));
        }

        public override void FillTokens(List<QueryToken> tokens)
        {
            tokens.Add(Token.QueryToken);

            this.ForeachBlock.FillTokens(tokens);
        }

        public override OpenXmlElement CloneNode(bool deep)
        {
            return new ForeachNode(this);
        }

        protected internal override void ReplaceBlock()
        {
            OpenXmlElementPair pair = this.NormalizeSiblings(new OpenXmlElementPair(ForeachToken, EndForeachToken));

            this.ForeachBlock = new BlockNode();
            MoveTo(NodesBetween(pair), this.ForeachBlock);

            pair.CommonParent.ReplaceChild(this, pair.First);
            pair.Last.Remove();
        }

        public override void WriteTo(XmlWriter xmlWriter)
        {
            this.AppendChild(this.ForeachBlock);

            base.WriteTo(xmlWriter);

            this.RemoveChild(this.ForeachBlock);
        }

        protected internal override void RenderNode(WordTemplateParameters p, IEnumerable<ResultRow> rows)
        {
            var groups = rows.GroupBy(r => r[p.Columns[Token.QueryToken]]).ToList();
            if (groups.Count == 1 && groups[0].Key == null)
            {
                this.Remove();
                return;
            }

            var parent = this.Parent;
            int index = parent.ChildElements.IndexOf(this);
            parent.RemoveChild(this);


            List<Tuple<BlockNode, IEnumerable<ResultRow>>> tuples = new List<Tuple<BlockNode, IEnumerable<ResultRow>>>();
            foreach (IEnumerable<ResultRow> group in groups)
            {
                var clone = (BlockNode)this.ForeachBlock.CloneNode(true);

                parent.InsertAt(clone, index++);

                tuples.Add(Tuple.Create(clone, group));
            }

            foreach (var tuple in tuples)
            {
                tuple.Item1.RenderNode(p, tuple.Item2);
            }
        }
    }

    public class AnyNode : BlockContainerNode
    {
        public readonly ParsedToken Token;
        public readonly FilterOperation? Operation;
        public string Value;

        public MatchNode AnyToken;
        public MatchNode NotAnyToken;
        public MatchNode EndAnyToken;

        public BlockNode AnyBlock;
        public BlockNode NotAnyBlock;

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

        public AnyNode(AnyNode original)
            : base(original)
        {
            this.Token = original.Token;
            this.Operation = original.Operation;
            this.Value = original.Value;

            this.AnyToken = (MatchNode)original.AnyToken.Try(a => a.CloneNode(true));
            this.NotAnyToken = (MatchNode)original.NotAnyToken.Try(a => a.CloneNode(true));
            this.EndAnyToken = (MatchNode)original.EndAnyToken.Try(a => a.CloneNode(true));

            this.AnyBlock = (BlockNode)original.AnyBlock.Try(a => a.CloneNode(true));
            this.NotAnyBlock = (BlockNode)original.NotAnyBlock.Try(a => a.CloneNode(true));
        }

        public override OpenXmlElement CloneNode(bool deep)
        {
            return new AnyNode(this);
        }

        public override void WriteTo(System.Xml.XmlWriter xmlWriter)
        {
            this.AppendChild(this.AnyBlock);

            if (this.NotAnyBlock != null)
                this.AppendChild(this.NotAnyBlock);

            base.WriteTo(xmlWriter);

            if (this.NotAnyBlock != null)
                this.RemoveChild(this.NotAnyBlock);

            this.RemoveChild(this.AnyBlock);
        }

        protected internal override void ReplaceBlock()
        {
            if (this.NotAnyToken == null)
            {
                OpenXmlElementPair pair = this.NormalizeSiblings(new OpenXmlElementPair(AnyToken, EndAnyToken));

                this.AnyBlock = new BlockNode();
                MoveTo(NodesBetween(pair), this.AnyBlock);

                pair.CommonParent.ReplaceChild(this, pair.First);
                pair.Last.Remove();
            }
            else
            {
                OpenXmlElementPair pairAny = this.NormalizeSiblings(new OpenXmlElementPair(AnyToken, NotAnyToken));
                OpenXmlElementPair pairNotAny = this.NormalizeSiblings(new OpenXmlElementPair(NotAnyToken, EndAnyToken));

                if (pairAny.Last != pairNotAny.First)
                    throw new InvalidOperationException("Unbalanced tokens");

                this.AnyBlock = new BlockNode();
                MoveTo(NodesBetween(pairAny), this.AnyBlock);

                this.NotAnyBlock = new BlockNode();
                MoveTo(NodesBetween(pairNotAny), this.NotAnyBlock);

                pairAny.CommonParent.ReplaceChild(this, pairAny.First);
                pairAny.Last.Remove();
                pairNotAny.Last.Remove();
            }
        }

        public override void FillTokens(List<QueryToken> tokens)
        {
            tokens.Add(Token.QueryToken);

            this.AnyBlock.FillTokens(tokens);
            if (this.NotAnyBlock != null)
                this.NotAnyBlock.FillTokens(tokens);
        }

        protected internal override void RenderNode(WordTemplateParameters p, IEnumerable<ResultRow> rows)
        {
            var filtered = GetFiltered(p, rows);

            if (filtered.Any())
            {
                this.Parent.ReplaceChild(this.AnyBlock, this);
                this.AnyBlock.RenderNode(p, filtered);
            }
            else if (NotAnyBlock != null)
            {
                this.Parent.ReplaceChild(this.NotAnyBlock, this);
                this.NotAnyBlock.RenderNode(p, filtered);
            }
            else
                this.Parent.RemoveChild(this);
        }

        private IEnumerable<ResultRow> GetFiltered(WordTemplateParameters p, IEnumerable<ResultRow> rows)
        {
            if (Operation == null)
            {
                var column = p.Columns[Token.QueryToken];

                var filtered = rows.Where(r => TemplateUtils.ToBool(r[column])).ToList();

                return filtered;
            }
            else
            {
                object val = FilterValueConverter.Parse(Value, Token.QueryToken.Type, Operation == FilterOperation.IsIn);

                Expression value = Expression.Constant(val, Token.QueryToken.Type);

                ResultColumn col = p.Columns[Token.QueryToken];

                var expression = Signum.Utilities.ExpressionTrees.Linq.Expr((ResultRow rr) => rr[col]);

                Expression newBody = QueryUtils.GetCompareExpression(Operation.Value, Expression.Convert(expression.Body, Token.QueryToken.Type), value, inMemory: true);
                var lambda = Expression.Lambda<Func<ResultRow, bool>>(newBody, expression.Parameters).Compile();

                var filtered = rows.Where(lambda).ToList();

                return filtered;
            }
        }
    }

    public class IfNode : BlockContainerNode
    {
        public readonly ParsedToken Token;

        private FilterOperation? Operation;
        private string Value;

        public MatchNode IfToken;
        public MatchNode ElseToken;
        public MatchNode EndIfToken;

        public BlockNode IfBlock;
        public BlockNode ElseBlock;
        
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

        public IfNode(IfNode original)
            : base(original)
        {
            this.Token = original.Token;
            this.Operation = original.Operation;
            this.Value = original.Value;

            this.IfToken = (MatchNode)original.IfToken.Try(a => a.CloneNode(true));
            this.ElseToken = (MatchNode)original.ElseToken.Try(a => a.CloneNode(true));
            this.EndIfToken = (MatchNode)original.EndIfToken.Try(a => a.CloneNode(true));

            this.IfBlock = (BlockNode)original.IfBlock.Try(a => a.CloneNode(true));
            this.ElseBlock = (BlockNode)original.ElseBlock.Try(a => a.CloneNode(true));
        }

        public override OpenXmlElement CloneNode(bool deep)
        {
            return new IfNode(this);
        }

        protected internal override void ReplaceBlock()
        {
            if (this.ElseToken == null)
            {
                OpenXmlElementPair pair = this.NormalizeSiblings(new OpenXmlElementPair(IfToken, EndIfToken));

                this.IfBlock = new BlockNode();
                MoveTo(NodesBetween(pair), this.IfBlock);

                pair.CommonParent.ReplaceChild(this, pair.First);
                pair.Last.Remove();
            }
            else
            {
                OpenXmlElementPair pairAny = this.NormalizeSiblings(new OpenXmlElementPair(IfToken, ElseToken));
                OpenXmlElementPair pairNotAny = this.NormalizeSiblings(new OpenXmlElementPair(ElseToken, EndIfToken));

                if (pairAny.Last != pairNotAny.First)
                    throw new InvalidOperationException("Unbalanced tokens");

                this.IfBlock = new BlockNode();
                MoveTo(NodesBetween(pairAny), this.IfBlock);

                this.ElseBlock = new BlockNode();
                MoveTo(NodesBetween(pairNotAny), this.ElseBlock);

                pairAny.CommonParent.ReplaceChild(this, pairAny.First);
                pairAny.Last.Remove();
                pairNotAny.Last.Remove();
            }
        }

        public override void WriteTo(System.Xml.XmlWriter xmlWriter)
        {
            this.AppendChild(this.IfBlock);

            if (this.ElseBlock != null)
                this.AppendChild(this.ElseBlock);
         
            base.WriteTo(xmlWriter);
            
            if (this.ElseBlock != null)
                this.RemoveChild(this.ElseBlock);

            this.RemoveChild(this.IfBlock);
        }

        public override void FillTokens(List<QueryToken> tokens)
        {
            tokens.Add(Token.QueryToken);

            this.IfBlock.FillTokens(tokens);
            if (this.ElseBlock != null)
                this.ElseBlock.FillTokens(tokens);
        }

        protected internal override void RenderNode(WordTemplateParameters p, IEnumerable<ResultRow> rows)
        {
            if (GetCondition(p, rows))
            {
                this.Parent.ReplaceChild(this.IfBlock, this);
                this.IfBlock.RenderNode(p, rows);
            }
            else if (ElseBlock != null)
            {
                this.Parent.ReplaceChild(this.ElseBlock, this);
                this.ElseBlock.RenderNode(p, rows);
            }
            else
                this.Parent.RemoveChild(this);
        }


        public bool GetCondition(WordTemplateParameters p, IEnumerable<ResultRow> rows)
        {
            if (this.Operation == null)
                return !rows.IsEmpty() && TemplateUtils.ToBool(rows.DistinctSingle(p.Columns[Token.QueryToken]));
            else
            {
                Expression token = Expression.Constant(rows.DistinctSingle(p.Columns[Token.QueryToken]), Token.QueryToken.Type);

                Expression value = Expression.Constant(FilterValueConverter.Parse(Value, Token.QueryToken.Type, Operation == FilterOperation.IsIn), Token.QueryToken.Type);

                Expression newBody = QueryUtils.GetCompareExpression(Operation.Value, token, value, inMemory: true);
                var lambda = Expression.Lambda<Func<bool>>(newBody).Compile();

                return lambda();
            }
        }
    }
}
