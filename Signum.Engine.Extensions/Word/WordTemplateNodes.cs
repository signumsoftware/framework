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
using System.Globalization;
using Signum.Utilities.DataStructures;
using System.IO;

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

        internal protected abstract void RenderNode(WordTemplateParameters p);

        internal protected abstract void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables);

        public override string ToString()
        {
            return this.GetType().Name;
        }

        public abstract override OpenXmlElement CloneNode(bool deep);

        public abstract void Synchronize(SyncronizationContext sc);
    }

    public class TokenNode : BaseNode
    {
        public readonly ValueProviderBase ValueProvider;
        public readonly string Format;

        internal TokenNode(ValueProviderBase valueProvider, string format)
        {
            this.ValueProvider = valueProvider;
            this.Format = format;
        }

        internal TokenNode(TokenNode original) : base(original)
        {
            this.ValueProvider = original.ValueProvider;
            this.Format = original.Format;
        }

        public override void FillTokens(List<QueryToken> tokens)
        {
            ValueProvider.FillQueryTokens(tokens);
        }

        internal protected override void RenderNode(WordTemplateParameters p)
        {
            object obj = ValueProvider.GetValue(p);
            string text = obj is Enum ? ((Enum)obj).NiceToString() :
                obj is IFormattable ? ((IFormattable)obj).ToString(Format ?? ValueProvider.Format, p.Culture) :
                obj.TryToString();

            this.ReplaceBy(new Run(this.RunProperties.TryDo(prop => prop.Remove()), new Text(text)));
        }

        protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
        {
            var str = "@" + this.ValueProvider.ToString(variables, Format.HasText() ? (":" + Format) : null);

            this.ReplaceBy(new Run(this.RunProperties.TryDo(prop => prop.Remove()), new Text(str)));
        }

        public override void WriteTo(System.Xml.XmlWriter xmlWriter)
        {
            var tempText = new Text(ValueProvider.ToString());

            this.AppendChild(tempText);
            base.WriteTo(xmlWriter);
            this.RemoveChild(tempText);
        }

        public override OpenXmlElement CloneNode(bool deep)
        {
            return new TokenNode(this);
        }

        public override void Synchronize(SyncronizationContext sc)
        {
            ValueProvider.Synchronize(sc, "@");

            ValueProvider.Declare(sc.Variables);
        }
    }

    public class DeclareNode : BaseNode
    {
        public readonly ValueProviderBase ValueProvider;

        internal DeclareNode(ValueProviderBase valueProvider, Action<bool, string> addError)
        {
            if (!valueProvider.Variable.HasText())
                addError(true, "declare{0} should end with 'as $someVariable'".FormatWith(valueProvider.ToString()));

            this.ValueProvider = valueProvider;
        }

        public DeclareNode(DeclareNode original)
            : base(original)
        {
            this.ValueProvider = original.ValueProvider;
        }

        public override void WriteTo(System.Xml.XmlWriter xmlWriter)
        {
            var tempText = new Text(ValueProvider.ToString() ?? "Error!");

            this.AppendChild(tempText);
            base.WriteTo(xmlWriter);
            this.RemoveChild(tempText);
        }

        public override void FillTokens(List<QueryToken> tokens)
        {
        }

        public override OpenXmlElement CloneNode(bool deep)
        {
            return new DeclareNode(this);
        }

        protected internal override void RenderNode(WordTemplateParameters p)
        {
            this.Remove();
        }

        protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
        {
            string str = "@declare" + ValueProvider.ToString(variables, null);

            this.ReplaceBy(new Run(this.RunProperties.TryDo(prop => prop.Remove()), new Text(str)));

            ValueProvider.Declare(variables);
        }

        public override void Synchronize(SyncronizationContext sc)
        {
            ValueProvider.Synchronize(sc, "@declare");

            ValueProvider.Declare(sc.Variables);
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

        protected internal override void RenderNode(WordTemplateParameters p)
        {
            foreach (var item in this.Descendants<BaseNode>().ToList())
            {
                item.RenderNode(p);
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

        protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
        {
            foreach (var item in this.Descendants<BaseNode>().ToList())
            {
                item.RenderTemplate(variables);
            }
        }

        public override void Synchronize(SyncronizationContext sc)
        {
            foreach (var item in this.Descendants<BaseNode>().ToList())
            {
                item.Synchronize(sc);
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

        protected void NormalizeInterval(ref MatchNodePair first, ref MatchNodePair last, MatchNode errorHintParent)
        {
            if (first.MatchNode == last.MatchNode)
                throw new ArgumentException("first and last are the same node");

            var chainFirst = ((OpenXmlElement)first.MatchNode).Follow(a => a.Parent).Reverse().ToList();
            var chainLast = ((OpenXmlElement)last.MatchNode).Follow(a => a.Parent).Reverse().ToList();

            var result = chainFirst.Zip(chainLast, (f, l) => new { f, l }).First(a => a.f != a.l);
            AssertNotImportant(chainFirst, result.f, errorHintParent, first.MatchNode, last.MatchNode);
            AssertNotImportant(chainLast, result.l, errorHintParent, last.MatchNode, first.MatchNode);

            first.AscendantNode = result.f;
            last.AscendantNode = result.l;
        }

        private void AssertNotImportant(List<OpenXmlElement> chain, OpenXmlElement openXmlElement, MatchNode errorHintParent, MatchNode errorHint1, MatchNode errorHint2)
        {
            var index = chain.IndexOf(openXmlElement);

            for (int i = index; i < chain.Count; i++)
            {
                var current = chain[i];
                var next = i == chain.Count - 1 ? null : chain[i + 1];

                var important = current.ChildElements.Where(c => c != next && IsImportant(c));

                if (important.Any())
                    throw new InvalidOperationException("Node {0} is not at the same level than {1}{2}. Important nodes could be removed close to {0}:\r\n{3}".FormatWith(
                        errorHint1.Match,
                        errorHint2.Match,
                        errorHintParent != errorHint1 && errorHintParent != errorHint2 ? " in " + errorHintParent.Match : "",
                        current.NiceToString()));
            }
        }

        private bool IsImportant(OpenXmlElement c)
        {
            if (c is Paragraph)
                return true;

            if (c is Run)
            {
                var text = c.ChildElements.Where(a => !(a is RunProperties)).Only() as Text;

                if (text != null && string.IsNullOrWhiteSpace(text.Text))
                    return false;

                return true; 
            }

            return false;
        }

        protected static List<OpenXmlElement> NodesBetween(MatchNodePair first, MatchNodePair last)
        {
            var parent = first.CommonParent(last);

            int indexFirst = parent.ChildElements.IndexOf(first.AscendantNode);
            if (indexFirst == -1)
                throw new InvalidOperationException("Element not found");

            int indexLast = parent.ChildElements.IndexOf(last.AscendantNode);
            if (indexLast == -1)
                throw new InvalidOperationException("Element not found");

            var childs = parent.ChildElements.Where((e, i) => indexFirst < i && i < indexLast).ToList();
            return childs;
        }

        
    }

    public class ForeachNode : BlockContainerNode
    {
        public readonly ValueProviderBase ValueProvider;

        public MatchNodePair ForeachToken;
        public MatchNodePair EndForeachToken;

        public BlockNode ForeachBlock;

        public ForeachNode(ValueProviderBase valueProvider)
        {
            this.ValueProvider = valueProvider;
        }

        public ForeachNode(ForeachNode original)
            : base(original)
        {
            this.ValueProvider = original.ValueProvider;
            this.ForeachToken = original.ForeachToken.CloneNode();
            this.EndForeachToken = original.EndForeachToken.CloneNode();
            this.ForeachBlock = (BlockNode)original.ForeachBlock.Try(a => a.CloneNode(true));
        }

        public override void FillTokens(List<QueryToken> tokens)
        {
            ValueProvider.FillQueryTokens(tokens);

            this.ForeachBlock.FillTokens(tokens);
        }

        public override OpenXmlElement CloneNode(bool deep)
        {
            return new ForeachNode(this);
        }

        protected internal override void ReplaceBlock()
        {
            this.NormalizeInterval(ref ForeachToken, ref EndForeachToken, errorHintParent: ForeachToken.MatchNode);

            this.ForeachBlock = new BlockNode();
            this.ForeachBlock.MoveChilds(NodesBetween(ForeachToken, EndForeachToken));

            ForeachToken.AscendantNode.ReplaceBy(this);
            EndForeachToken.AscendantNode.Remove();
        }

        public override void WriteTo(XmlWriter xmlWriter)
        {
            this.AppendChild(this.ForeachBlock);

            base.WriteTo(xmlWriter);

            this.RemoveChild(this.ForeachBlock);
        }

        protected internal override void RenderNode(WordTemplateParameters p)
        {
            var parent = this.Parent;
            int index = parent.ChildElements.IndexOf(this);
            parent.RemoveChild(this);

            List<Tuple<BlockNode, IEnumerable<ResultRow>>> tuples = new List<Tuple<BlockNode, IEnumerable<ResultRow>>>();
            this.ValueProvider.Foreach(p, () =>
            {
                var clone = (BlockNode)this.ForeachBlock.CloneNode(true);

                parent.InsertAt(clone, index++);

                tuples.Add(Tuple.Create(clone, p.Rows));
            });

            var prev = p.Rows;
            foreach (var tuple in tuples)
            {
                using (p.OverrideRows(tuple.Item2))
                    tuple.Item1.RenderNode(p);
            }
        }

        protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
        {
            var parent = this.Parent;
            int index = parent.ChildElements.IndexOf(this);
            this.Remove();
            parent.InsertAt(this.ForeachToken.ReplaceMatchNode("@foreach" + this.ValueProvider.ToString(variables, null)), index++);
            {
                var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                ValueProvider.Declare(newVars);
                this.ForeachBlock.RenderTemplate(newVars);
                parent.MoveChildsAt(ref index, this.ForeachBlock.ChildElements);
            }
            parent.InsertAt(this.EndForeachToken.ReplaceMatchNode("@endforeach"), index++);

        }

        public override void Synchronize(SyncronizationContext sc)
        {
            ValueProvider.Synchronize(sc, "@foreach");

            using (sc.NewScope())
            {
                ValueProvider.Declare(sc.Variables);

                this.ForeachBlock.Synchronize(sc);
            }
        }
    }

    public struct MatchNodePair
    {
        public MatchNodePair(MatchNode matchNode)
        {
            this.MatchNode = matchNode;
            this.AscendantNode = null;
        }

        public MatchNode MatchNode;
        public OpenXmlElement AscendantNode;

        public OpenXmlElement CommonParent(MatchNodePair other)
        {
            if (this.AscendantNode.Parent != other.AscendantNode.Parent)
                throw new InvalidOperationException("Parents do not match");

            return this.AscendantNode.Parent;
        }

        public MatchNodePair CloneNode()
        {
            var clone = this.AscendantNode.CloneNode(true);
            var match = clone.Descendants<MatchNode>().SingleEx();

            return new MatchNodePair(match) { AscendantNode = clone };
        }

        internal OpenXmlElement ReplaceMatchNode(string text)
        {
            var run = new Run(this.MatchNode.RunProperties.TryDo(prop => prop.Remove()), new Text(text));
            if (this.MatchNode == AscendantNode)
                return run;

            this.MatchNode.ReplaceBy(run);
            return AscendantNode;
        }

        public override string ToString()
        {
            return "{0} {1}".FormatWith(MatchNode, AscendantNode);
        }
    }

    public class AnyNode : BlockContainerNode
    {
        public readonly ValueProviderBase ValueProvider;
        public readonly FilterOperation? Operation;
        public string Value;

        public MatchNodePair AnyToken;
        public MatchNodePair NotAnyToken;
        public MatchNodePair EndAnyToken;

        public BlockNode AnyBlock;
        public BlockNode NotAnyBlock;

        public AnyNode(ValueProviderBase valueProvider)
        {
            this.ValueProvider = valueProvider;
        }

        internal AnyNode(ValueProviderBase valueProvider, string operation, string value, Action<bool, string> addError)
        {
            this.ValueProvider = valueProvider;
            this.Operation = FilterValueConverter.ParseOperation(operation);
            this.Value = value;

            ValueProvider.ValidateConditionValue(value, Operation, addError);
        }

        public AnyNode(AnyNode original)
            : base(original)
        {
            this.ValueProvider = original.ValueProvider;
            this.Operation = original.Operation;
            this.Value = original.Value;

            this.AnyToken = original.AnyToken.CloneNode();
            this.NotAnyToken = original.NotAnyToken.CloneNode();
            this.EndAnyToken = original.EndAnyToken.CloneNode();

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
            if (this.NotAnyToken.MatchNode == null)
            {
                this.NormalizeInterval(ref AnyToken, ref EndAnyToken, errorHintParent: AnyToken.MatchNode);

                this.AnyBlock = new BlockNode();
                this.AnyBlock.MoveChilds(NodesBetween(AnyToken, EndAnyToken));

                this.AnyToken.AscendantNode.ReplaceBy(this);
                this.NotAnyToken.AscendantNode.Remove();
            }
            else
            {
                var notAnyToken = this.NotAnyToken;
                this.NormalizeInterval(ref AnyToken, ref notAnyToken, errorHintParent: AnyToken.MatchNode);
                this.NormalizeInterval(ref NotAnyToken, ref EndAnyToken, errorHintParent: AnyToken.MatchNode);

                if (notAnyToken.AscendantNode != NotAnyToken.AscendantNode)
                    throw new InvalidOperationException("Unbalanced tokens");

                this.AnyBlock = new BlockNode();
                this.AnyBlock.MoveChilds(NodesBetween(this.AnyToken, this.NotAnyToken));

                this.NotAnyBlock = new BlockNode();
                this.NotAnyBlock.MoveChilds(NodesBetween(this.NotAnyToken, this.EndAnyToken));

                this.AnyToken.AscendantNode.ReplaceBy(this);
                this.NotAnyToken.AscendantNode.Remove();
                this.EndAnyToken.AscendantNode.Remove();
            }
        }

        public override void FillTokens(List<QueryToken> tokens)
        {
            this.ValueProvider.FillQueryTokens(tokens);

            this.AnyBlock.FillTokens(tokens);
            if (this.NotAnyBlock != null)
                this.NotAnyBlock.FillTokens(tokens);
        }

        protected internal override void RenderNode(WordTemplateParameters p)
        {
            var filtered = this.ValueProvider.GetFilteredRows(p, Operation, Value);

            using (filtered is IEnumerable<ResultRow> ? p.OverrideRows((IEnumerable<ResultRow>)filtered) : null)
            {
                if (filtered.Any())
                {
                    this.ReplaceBy(this.AnyBlock);
                    this.AnyBlock.RenderNode(p);
                }
                else if (NotAnyBlock != null)
                {
                    this.ReplaceBy(this.NotAnyBlock);
                    this.NotAnyBlock.RenderNode(p);
                }
                else
                    this.Parent.RemoveChild(this);
            }
        }

        public override void Synchronize(SyncronizationContext sc)
        {
            this.ValueProvider.Synchronize(sc, "@any");

            if (Operation != null)
                sc.SynchronizeValue(this.ValueProvider.Type, ref Value, Operation == FilterOperation.IsIn);

            using (sc.NewScope())
            {
                this.ValueProvider.Declare(sc.Variables);

                AnyBlock.Synchronize(sc);
            }

            if (NotAnyBlock != null)
            {
                using (sc.NewScope())
                {
                    this.ValueProvider.Declare(sc.Variables);

                    NotAnyBlock.Synchronize(sc);
                }
            }
        }

        protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
        {
            var parent = this.Parent;
            int index = parent.ChildElements.IndexOf(this);
            this.Remove();

            string str = "@any" + this.ValueProvider.ToString(variables, Operation == null ? null : FilterValueConverter.ToStringOperation(Operation.Value) + Value);

            parent.InsertAt(this.AnyToken.ReplaceMatchNode(str), index++);
            {
                var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                ValueProvider.Declare(newVars);
                this.AnyBlock.RenderTemplate(newVars);
                parent.MoveChildsAt(ref index, this.AnyBlock.ChildElements);
            }

            if (this.NotAnyToken.MatchNode != null)
            {
                parent.InsertAt(this.NotAnyToken.ReplaceMatchNode("@notany"), index++);

                var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                ValueProvider.Declare(newVars);
                this.NotAnyBlock.RenderTemplate(newVars);
                parent.MoveChildsAt(ref index, this.NotAnyBlock.ChildElements);
            }

            parent.InsertAt(this.EndAnyToken.ReplaceMatchNode("@endany"), index++);
        }
    }

    public class IfNode : BlockContainerNode
    {
        public readonly ValueProviderBase ValueProvider;

        private FilterOperation? Operation;
        private string Value;

        public MatchNodePair IfToken;
        public MatchNodePair ElseToken;
        public MatchNodePair EndIfToken;

        public BlockNode IfBlock;
        public BlockNode ElseBlock;

        internal IfNode(ValueProviderBase valueProvider)
        {
            this.ValueProvider = valueProvider;
        }

        internal IfNode(ValueProviderBase valueProvider, string operation, string value, Action<bool, string> addError)
        {
            this.ValueProvider = valueProvider;
            this.Operation = FilterValueConverter.ParseOperation(operation);
            this.Value = value;

            ValueProvider.ValidateConditionValue(value, Operation, addError);
        }

        public IfNode(IfNode original)
            : base(original)
        {
            this.ValueProvider = original.ValueProvider;
            this.Operation = original.Operation;
            this.Value = original.Value;

            this.IfToken = original.IfToken.CloneNode();
            this.ElseToken = original.ElseToken.CloneNode();
            this.EndIfToken = original.EndIfToken.CloneNode();

            this.IfBlock = (BlockNode)original.IfBlock.Try(a => a.CloneNode(true));
            this.ElseBlock = (BlockNode)original.ElseBlock.Try(a => a.CloneNode(true));
        }

        public override OpenXmlElement CloneNode(bool deep)
        {
            return new IfNode(this);
        }

        protected internal override void ReplaceBlock()
        {
            if (this.ElseToken.MatchNode == null)
            {
                this.NormalizeInterval(ref IfToken, ref EndIfToken, errorHintParent: IfToken.MatchNode);

                this.IfBlock = new BlockNode();
                this.IfBlock.MoveChilds(NodesBetween(IfToken, EndIfToken));

                IfToken.AscendantNode.ReplaceBy(this);
                EndIfToken.AscendantNode.Remove();
            }
            else
            {
                var elseToken = ElseToken;
                this.NormalizeInterval(ref IfToken, ref elseToken, errorHintParent: IfToken.MatchNode);
                this.NormalizeInterval(ref ElseToken, ref EndIfToken, errorHintParent: IfToken.MatchNode);

                if (elseToken.AscendantNode != ElseToken.AscendantNode)
                    throw new InvalidOperationException("Unbalanced tokens");

                this.IfBlock = new BlockNode();
                this.IfBlock.MoveChilds(NodesBetween(this.IfToken, this.ElseToken));

                this.ElseBlock = new BlockNode();
                this.ElseBlock.MoveChilds(NodesBetween(this.ElseToken, this.EndIfToken));

                this.IfToken.AscendantNode.ReplaceBy(this);
                this.ElseToken.AscendantNode.Remove();
                this.EndIfToken.AscendantNode.Remove();
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
            this.ValueProvider.FillQueryTokens(tokens);

            this.IfBlock.FillTokens(tokens);
            if (this.ElseBlock != null)
                this.ElseBlock.FillTokens(tokens);
        }

        protected internal override void RenderNode(WordTemplateParameters p)
        {
            if (this.ValueProvider.GetCondition(p, Operation, Value))
            {
                this.ReplaceBy(this.IfBlock);
                this.IfBlock.RenderNode(p);
            }
            else if (ElseBlock != null)
            {
                this.ReplaceBy(this.ElseBlock);
                this.ElseBlock.RenderNode(p);
            }
            else
                this.Parent.RemoveChild(this);
        }

        public override void Synchronize(SyncronizationContext sc)
        {
            this.ValueProvider.Synchronize(sc, "@if");

            if (Operation != null)
                sc.SynchronizeValue(this.ValueProvider.Type, ref Value, Operation == FilterOperation.IsIn);

            using (sc.NewScope())
            {
                this.ValueProvider.Declare(sc.Variables);

                IfBlock.Synchronize(sc);
            }

            if (ElseBlock != null)
            {
                using (sc.NewScope())
                {
                    this.ValueProvider.Declare(sc.Variables);

                    ElseBlock.Synchronize(sc);
                }
            }
        }

        protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
        {
            var parent = this.Parent;
            int index = parent.ChildElements.IndexOf(this);
            this.Remove();

            var str = "@if" + this.ValueProvider.ToString(variables, Operation == null ? null : FilterValueConverter.ToStringOperation(Operation.Value) + Value);

            parent.InsertAt(this.IfToken.ReplaceMatchNode(str), index++);
            {
                var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                this.ValueProvider.Declare(newVars);
                this.IfBlock.RenderTemplate(newVars);
                parent.MoveChildsAt(ref index, this.IfBlock.ChildElements);
            }

            if (this.ElseToken.MatchNode != null)
            {
                parent.InsertAt(this.ElseToken.ReplaceMatchNode("@else"), index++);

                var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                this.ValueProvider.Declare(newVars);
                this.ElseBlock.RenderTemplate(newVars);
                parent.MoveChildsAt(ref index, this.ElseBlock.ChildElements);
            }

            parent.InsertAt(this.EndIfToken.ReplaceMatchNode("@endif"), index++);
        }
    }

    public static class OpenXmlElementExtensions
    {
        public static void ReplaceBy(this OpenXmlElement element, OpenXmlElement replacement)
        {
            element.Parent.ReplaceChild(replacement, element);
        }

        public static void MoveChilds(this OpenXmlElement target, IEnumerable<OpenXmlElement> childs)
        {
            foreach (var c in childs.ToList())
            {
                c.Remove();
                target.AppendChild(c);
            }
        }

        public static void MoveChildsAt(this OpenXmlElement target, ref int index, IEnumerable<OpenXmlElement> childs)
        {
            foreach (var c in childs.ToList())
            {
                c.Remove();
                target.InsertAt(c, index++);
            }
        }

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
