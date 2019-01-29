using DocumentFormat.OpenXml;
using W = DocumentFormat.OpenXml.Wordprocessing;
using D = DocumentFormat.OpenXml.Drawing;
using S = DocumentFormat.OpenXml.Spreadsheet;
using Signum.Engine.Templating;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Signum.Engine.Word
{
    public interface INodeProvider
    {
        OpenXmlLeafTextElement NewText(string text);
        OpenXmlCompositeElement NewRun(OpenXmlCompositeElement runProps, string text, SpaceProcessingModeValues spaceMode = SpaceProcessingModeValues.Default, bool initialBr = false);
        bool IsRun(OpenXmlElement element);
        bool IsText(OpenXmlElement element);
        string GetText(OpenXmlElement run);
        OpenXmlCompositeElement CastRun(OpenXmlElement element);
        OpenXmlCompositeElement GetRunProperties(OpenXmlCompositeElement run);
        bool IsParagraph(OpenXmlElement element);
        bool IsRunProperties(OpenXmlElement a);
        OpenXmlCompositeElement WrapInRun(OpenXmlElement text);
    }

    public class WordprocessingNodeProvider : INodeProvider
    {
        public OpenXmlCompositeElement CastRun(OpenXmlElement element)
        {
            return (W.Run)element;
        }

        public OpenXmlCompositeElement NewRun(OpenXmlCompositeElement runProps, string text, SpaceProcessingModeValues spaceMode, bool initialBr)
        {
            var textNode = new W.Text(text) {Space = spaceMode};

            var result = new W.Run(runProps, textNode);

            if (initialBr)
                result.InsertBefore(new W.Break(), textNode);

            return result;
        }

        public string GetText(OpenXmlElement run)
        {
            return run.ChildElements.OfType<W.Text>().SingleOrDefault()?.Text ?? "";
        }

        public OpenXmlLeafTextElement NewText(string text)
        {
            return new W.Text(text);
        }

        public bool IsRun(OpenXmlElement a)
        {
            return a is W.Run;
        }

        public bool IsText(OpenXmlElement a)
        {
            return a is W.Text;
        }
        
        public OpenXmlCompositeElement GetRunProperties(OpenXmlCompositeElement run)
        {
            return ((W.Run)run).RunProperties;
        }

        public bool IsParagraph(OpenXmlElement element)
        {
            return element is W.Paragraph;
        }

        public bool IsRunProperties(OpenXmlElement element)
        {
            return element is W.RunProperties;
        }

        public OpenXmlCompositeElement WrapInRun(OpenXmlElement text)
        {
            throw new NotImplementedException();
        }
    }

    public class DrawingNodeProvider : INodeProvider
    {
        public OpenXmlCompositeElement CastRun(OpenXmlElement element)
        {
            return (D.Run)element;
        }

        public OpenXmlCompositeElement NewRun(OpenXmlCompositeElement runProps, string text, SpaceProcessingModeValues spaceMode, bool initialBr)
        {
            var textElement = new D.Text(text);

            var result = new D.Run(runProps, textElement);
            
            if (initialBr)
                result.InsertBefore(new D.Break(), textElement);

            return result;
        }

        public OpenXmlLeafTextElement NewText(string text)
        {
            return new D.Text(text);
        }

        public string GetText(OpenXmlElement run)
        {
            return run.ChildElements.OfType<D.Text>().SingleOrDefault()?.Text ?? "";
        }

        public bool IsRun(OpenXmlElement a)
        {
            return a is D.Run;
        }

        public bool IsText(OpenXmlElement a)
        {
            return a is D.Text;
        }

        public OpenXmlCompositeElement GetRunProperties(OpenXmlCompositeElement run)
        {
            return ((D.Run)run).RunProperties;
        }

        public bool IsParagraph(OpenXmlElement element)
        {
            return element is D.Paragraph;
        }

        public bool IsRunProperties(OpenXmlElement element)
        {
            return element is D.RunProperties;
        }

        public OpenXmlCompositeElement WrapInRun(OpenXmlElement text)
        {
            throw new NotImplementedException();
        }
    }

    internal class SpreadsheetNodeProvider : INodeProvider
    {
        public OpenXmlCompositeElement CastRun(OpenXmlElement element)
        {
            return (S.Run)element;
        }

        public OpenXmlCompositeElement NewRun(OpenXmlCompositeElement runProps, string text, SpaceProcessingModeValues spaceMode, bool initialBr)
        {
            var textElement = new S.Text(text);
            var result = new S.Run(runProps, textElement);
            
            if (initialBr)
                result.InsertBefore(new S.Break(), textElement);

            return result;
        }

        public OpenXmlLeafTextElement NewText(string text)
        {
            return new S.Text(text);
        }

        public string GetText(OpenXmlElement run)
        {
            return run is S.Run r ? r.ChildElements.OfType<S.Text>().SingleOrDefault()?.Text :
                run is S.Text s ? s.Text :
                "";
        }

        public bool IsRun(OpenXmlElement a)
        {
            return a is S.Run;
        }

        public bool IsText(OpenXmlElement a)
        {
            return a is S.Text;
        }

        public OpenXmlCompositeElement GetRunProperties(OpenXmlCompositeElement run)
        {
            return ((S.Run)run).RunProperties;
        }

        public bool IsParagraph(OpenXmlElement element)
        {
            return false;
        }

        public bool IsRunProperties(OpenXmlElement element)
        {
            return element is S.RunProperties;
        }

        public OpenXmlCompositeElement WrapInRun(OpenXmlElement text)
        {
            return new S.Run(text);
        }
    }

    public class MatchNode : AlternateContent
    {
        public INodeProvider NodeProvider;

        OpenXmlCompositeElement runProperties;
        public OpenXmlCompositeElement RunProperties
        {
            get { return this.runProperties; }
            set
            {
                if (value != null && value.Parent != null)
                    throw new InvalidOperationException("Remove it from his parent first");
                this.runProperties = value;
            }
        }

        public Match Match;

        public MatchNode(INodeProvider nodeProvider, Match match)
        {
            this.NodeProvider = nodeProvider;
            this.Match = match;
        }

        internal MatchNode(MatchNode original)
        {
            this.Match = original.Match;

            this.SetAttributes(original.GetAttributes().ToList());
            foreach (var item in original.ChildElements)
            {
                this.AppendChild(item.CloneNode(true));
            }
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
            var tempText = this.NodeProvider.NewText(Match.ToString());

            this.AppendChild(tempText);
            base.WriteTo(xmlWriter);
            this.RemoveChild(tempText);
        }

        public override OpenXmlElement CloneNode(bool deep)
        {
            return new MatchNode(this);
        }

        public override string InnerText => Match.Value;
    }

    public abstract class BaseNode : AlternateContent
    {
        public INodeProvider NodeProvider;

        OpenXmlCompositeElement runProperties;
        public OpenXmlCompositeElement RunProperties
        {
            get { return this.runProperties; }
            set
            {
                if (value != null && value.Parent != null)
                    throw new InvalidOperationException("Remove it from his parent first");
                this.runProperties = value;
            }
        }

        public BaseNode(INodeProvider nodeProvider)
    {
            this.NodeProvider = nodeProvider;
        }

        public BaseNode(BaseNode original)
        {
            this.NodeProvider = original.NodeProvider;
            this.RunProperties = original.RunProperties;
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

        public abstract void Synchronize(SynchronizationContext sc);
    }

    public class TokenNode : BaseNode
    {
        public readonly ValueProviderBase ValueProvider;
        public readonly string Format;

        internal TokenNode(INodeProvider nodeProvider, ValueProviderBase valueProvider, string format): base(nodeProvider)
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
                obj?.ToString();

            if (text != null && text.Contains('\n'))
            {
                var replacements = text.Lines().Select((line, i) => this.NodeProvider.NewRun((OpenXmlCompositeElement)this.RunProperties?.CloneNode(true), line, initialBr: i > 0));

                this.ReplaceBy(replacements);
            }
            else
            {
                this.ReplaceBy(this.NodeProvider.NewRun((OpenXmlCompositeElement)this.RunProperties?.CloneNode(true), text));
            }
        }

        protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
        {
            var str = "@" + this.ValueProvider.ToString(variables, Format.HasText() ? (":" + TemplateUtils.ScapeColon(Format)) : null);

            this.ReplaceBy(this.NodeProvider.NewRun((OpenXmlCompositeElement)this.RunProperties?.CloneNode(true), str));
        }

        public override void WriteTo(System.Xml.XmlWriter xmlWriter)
        {
            var tempText = this.NodeProvider.NewText(ValueProvider?.ToString());

            this.AppendChild(tempText);
            base.WriteTo(xmlWriter);
            this.RemoveChild(tempText);
        }

        public override OpenXmlElement CloneNode(bool deep)
        {
            return new TokenNode(this);
        }

        public override void Synchronize(SynchronizationContext sc)
        {
            ValueProvider.Synchronize(sc, "@");

            ValueProvider.Declare(sc.Variables);
        }

        public override string InnerText => this.ValueProvider?.ToString();
    }

    public class DeclareNode : BaseNode
    {
        public readonly ValueProviderBase ValueProvider;

        internal DeclareNode(INodeProvider nodeProvider, ValueProviderBase valueProvider, Action<bool, string> addError): base(nodeProvider)
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
            var tempText = this.NodeProvider.NewText(ValueProvider?.ToString() ?? "Error!");

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
            if (this.NodeProvider.IsParagraph(this.Parent) && 
                !this.Parent.ChildElements.Any(a => BlockContainerNode.IsImportant(a, NodeProvider) && a != this))
                this.Parent.Remove();
            else
                this.Remove();
        }

        protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
        {
            string str = "@declare" + ValueProvider.ToString(variables, null);

            this.ReplaceBy(this.NodeProvider.NewRun((OpenXmlCompositeElement)this.RunProperties?.CloneNode(true), str));

            ValueProvider.Declare(variables);
        }

        public override void Synchronize(SynchronizationContext sc)
        {
            ValueProvider.Synchronize(sc, "@declare");
        }
    }


    public class BlockNode : BaseNode
    {
        public BlockNode(INodeProvider nodeProvider): base(nodeProvider) { }

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

        public override void Synchronize(SynchronizationContext sc)
        {
            foreach (var item in this.Descendants<BaseNode>().ToList())
            {
                item.Synchronize(sc);
            }
        }
    }

    public abstract class BlockContainerNode : BaseNode
    {
        public BlockContainerNode(INodeProvider nodeProvider): base(nodeProvider) { }

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

                var important = current.ChildElements.Where(c => c != next && IsImportant(c, NodeProvider));

                if (important.Any())
                {
                    string hint = errorHintParent != errorHint1 && errorHintParent != errorHint2 ? " in " + errorHintParent.Match : "";
                    
                    throw new InvalidOperationException($"Node {errorHint1.Match} is not at the same level than {errorHint2.Match}{hint}. Important nodes could be removed in the chain:\r\n\r\n" +
                        chain.Skip(chain.IndexOf(openXmlElement)).Select((a, p) => (a.GetType().Name + " with text:" + a.InnerText).Indent(p * 4)).ToString("\r\n\r\n"));
                }
            }
        }

        public static bool IsImportant(OpenXmlElement c, INodeProvider nodeProvider)
        {
            if (nodeProvider.IsParagraph(c))
                return true;

            if (nodeProvider.IsRun(c))
            {
                var text = c.ChildElements.Where(a => !nodeProvider.IsRunProperties(a)).Only();

                if (nodeProvider.IsText(text) && string.IsNullOrWhiteSpace(nodeProvider.GetText(text)))
                    return false;

                return true; 
            }

            if (c is BaseNode)
                return true;

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

        public ForeachNode(INodeProvider nodeProvider, ValueProviderBase valueProvider) : base(nodeProvider)
        {
            this.ValueProvider = valueProvider;
            valueProvider.IsForeach = true;
        }

        public ForeachNode(ForeachNode original)
            : base(original)
        {
            this.ValueProvider = original.ValueProvider;
            this.ForeachToken = original.ForeachToken.CloneNode();
            this.EndForeachToken = original.EndForeachToken.CloneNode();
            this.ForeachBlock = (BlockNode)original.ForeachBlock?.Let(a => a.CloneNode(true));
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

            this.ForeachBlock = new BlockNode(this.NodeProvider);
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

            this.ValueProvider.Foreach(p, () =>
            {
                var clone = (BlockNode)this.ForeachBlock.CloneNode(true);

                var index = parent.ChildElements.IndexOf(this);

                parent.InsertAt(clone, index);

                clone.RenderNode(p);
            });

            parent.RemoveChild(this);
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

        public override void Synchronize(SynchronizationContext sc)
        {
            ValueProvider.Synchronize(sc, "@foreach");

            using (sc.NewScope())
            {
                ValueProvider.Declare(sc.Variables);

                this.ForeachBlock.Synchronize(sc);
            }
        }

        public override string InnerText => $@"{this.ForeachToken.MatchNode.InnerText}{this.ForeachBlock.InnerText}{this.EndForeachToken.MatchNode.InnerText}";
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
            if (this.AscendantNode != null)
            {
                var ascClone = this.AscendantNode.CloneNode(true);
                var match = ascClone as MatchNode ?? ascClone.Descendants<MatchNode>().SingleEx();

                return new MatchNodePair(match) { AscendantNode = ascClone };
            }
            else if (this.MatchNode != null)
            {
                var clone = this.MatchNode.CloneNode(true);
                return new MatchNodePair((MatchNode)clone);
            }
            else
            {
                return default(MatchNodePair);
            }
        }

        internal OpenXmlElement ReplaceMatchNode(string text)
        {
            var run = this.MatchNode.NodeProvider.NewRun((OpenXmlCompositeElement)this.MatchNode.RunProperties?.CloneNode(true), text);

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
        public readonly ConditionBase Condition;

        public MatchNodePair AnyToken;
        public MatchNodePair NotAnyToken;
        public MatchNodePair EndAnyToken;

        public BlockNode AnyBlock;
        public BlockNode NotAnyBlock;

        public AnyNode(INodeProvider nodeProvider, ConditionBase condition) : base(nodeProvider)
        {
            this.Condition = condition;
        }

        public AnyNode(AnyNode original)
            : base(original)
        {
            this.Condition= original.Condition.Clone();

            this.AnyToken = original.AnyToken.CloneNode();
            this.NotAnyToken = original.NotAnyToken.CloneNode();
            this.EndAnyToken = original.EndAnyToken.CloneNode();

            this.AnyBlock = (BlockNode)original.AnyBlock?.Let(a => a.CloneNode(true));
            this.NotAnyBlock = (BlockNode)original.NotAnyBlock?.Let(a => a.CloneNode(true));
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

                this.AnyBlock = new BlockNode(this.NodeProvider);
                this.AnyBlock.MoveChilds(NodesBetween(AnyToken, EndAnyToken));

                this.AnyToken.AscendantNode.ReplaceBy(this);
                this.EndAnyToken.AscendantNode.Remove();
            }
            else
            {
                var notAnyToken = this.NotAnyToken;
                this.NormalizeInterval(ref AnyToken, ref notAnyToken, errorHintParent: AnyToken.MatchNode);
                this.NormalizeInterval(ref NotAnyToken, ref EndAnyToken, errorHintParent: AnyToken.MatchNode);

                if (notAnyToken.AscendantNode != NotAnyToken.AscendantNode)
                    throw new InvalidOperationException("Unbalanced tokens");

                this.AnyBlock = new BlockNode(this.NodeProvider);
                this.AnyBlock.MoveChilds(NodesBetween(this.AnyToken, this.NotAnyToken));

                this.NotAnyBlock = new BlockNode(this.NodeProvider);
                this.NotAnyBlock.MoveChilds(NodesBetween(this.NotAnyToken, this.EndAnyToken));

                this.AnyToken.AscendantNode.ReplaceBy(this);
                this.NotAnyToken.AscendantNode.Remove();
                this.EndAnyToken.AscendantNode.Remove();
            }
        }

        public override void FillTokens(List<QueryToken> tokens)
        {
            this.Condition.FillQueryTokens(tokens);

            this.AnyBlock.FillTokens(tokens);
            if (this.NotAnyBlock != null)
                this.NotAnyBlock.FillTokens(tokens);
        }

        protected internal override void RenderNode(WordTemplateParameters p)
        {
            var filtered = this.Condition.GetFilteredRows(p);

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

        public override void Synchronize(SynchronizationContext sc)
        {
            this.Condition.Synchronize(sc, "@any");
            
            using (sc.NewScope())
            {
                this.Condition.Declare(sc.Variables);

                AnyBlock.Synchronize(sc);
            }

            if (NotAnyBlock != null)
            {
                using (sc.NewScope())
                {
                    this.Condition.Declare(sc.Variables);

                    NotAnyBlock.Synchronize(sc);
                }
            }
        }

        protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
        {
            var parent = this.Parent;
            int index = parent.ChildElements.IndexOf(this);
            this.Remove();

            string str = "@any" + this.Condition.ToString(variables);

            parent.InsertAt(this.AnyToken.ReplaceMatchNode(str), index++);
            {
                var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                Condition.Declare(newVars);
                this.AnyBlock.RenderTemplate(newVars);
                parent.MoveChildsAt(ref index, this.AnyBlock.ChildElements);
            }

            if (this.NotAnyToken.MatchNode != null)
            {
                parent.InsertAt(this.NotAnyToken.ReplaceMatchNode("@notany"), index++);

                var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                Condition.Declare(newVars);
                this.NotAnyBlock.RenderTemplate(newVars);
                parent.MoveChildsAt(ref index, this.NotAnyBlock.ChildElements);
            }

            parent.InsertAt(this.EndAnyToken.ReplaceMatchNode("@endany"), index++);
        }

        public override string InnerText => $@"{this.AnyToken.MatchNode.InnerText}{this.AnyBlock.InnerText}{this.NotAnyToken.MatchNode?.InnerText}{this.NotAnyBlock?.InnerText}{this.EndAnyToken.MatchNode.InnerText}";
    }


    public class IfNode : BlockContainerNode
    {
        public ConditionBase Condition;


        public MatchNodePair IfToken;
        public MatchNodePair ElseToken;
        public MatchNodePair EndIfToken;

        public BlockNode IfBlock;
        public BlockNode ElseBlock;

        internal IfNode(INodeProvider nodeProvider, ConditionBase condition) : base(nodeProvider)
        {
            this.Condition = condition;
        }

        public IfNode(IfNode original)
            : base(original)
        {
            this.Condition = original.Condition.Clone();

            this.IfToken = original.IfToken.CloneNode();
            this.ElseToken = original.ElseToken.CloneNode();
            this.EndIfToken = original.EndIfToken.CloneNode();

            this.IfBlock = (BlockNode)original.IfBlock?.Let(a => a.CloneNode(true));
            this.ElseBlock = (BlockNode)original.ElseBlock?.Let(a => a.CloneNode(true));
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

                this.IfBlock = new BlockNode(this.NodeProvider);
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

                this.IfBlock = new BlockNode(this.NodeProvider);
                this.IfBlock.MoveChilds(NodesBetween(this.IfToken, this.ElseToken));

                this.ElseBlock = new BlockNode(this.NodeProvider);
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
            this.Condition.FillQueryTokens(tokens);

            this.IfBlock.FillTokens(tokens);
            if (this.ElseBlock != null)
                this.ElseBlock.FillTokens(tokens);
        }

        protected internal override void RenderNode(WordTemplateParameters p)
        {
            if (this.Condition.Evaluate(p))
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

        public override void Synchronize(SynchronizationContext sc)
        {
            this.Condition.Synchronize(sc, "@if");
            
            using (sc.NewScope())
            {
                this.Condition.Declare(sc.Variables);

                IfBlock.Synchronize(sc);
            }

            if (ElseBlock != null)
            {
                using (sc.NewScope())
                {
                    this.Condition.Declare(sc.Variables);

                    ElseBlock.Synchronize(sc);
                }
            }
        }

        protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
        {
            var parent = this.Parent;
            int index = parent.ChildElements.IndexOf(this);
            this.Remove();

            var str = "@if" + this.Condition.ToString(variables);

            parent.InsertAt(this.IfToken.ReplaceMatchNode(str), index++);
            {
                var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                this.Condition.Declare(newVars);
                this.IfBlock.RenderTemplate(newVars);
                parent.MoveChildsAt(ref index, this.IfBlock.ChildElements);
            }

            if (this.ElseToken.MatchNode != null)
            {
                parent.InsertAt(this.ElseToken.ReplaceMatchNode("@else"), index++);

                var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                this.Condition.Declare(newVars);
                this.ElseBlock.RenderTemplate(newVars);
                parent.MoveChildsAt(ref index, this.ElseBlock.ChildElements);
            }

            parent.InsertAt(this.EndIfToken.ReplaceMatchNode("@endif"), index++);
        }

        public override string InnerText => $@"{this.IfToken.MatchNode.InnerText}{this.IfBlock.InnerText}{this.ElseToken.MatchNode?.InnerText}{this.ElseBlock?.InnerText}{this.EndIfToken.MatchNode.InnerText}";
    }

    public static class OpenXmlElementExtensions
    {
        public static void ReplaceBy(this OpenXmlElement element, OpenXmlElement replacement)
        {
            element.Parent.ReplaceChild(replacement, element);
        }

        public static void ReplaceBy(this OpenXmlElement element, IEnumerable<OpenXmlElement> replacements)
        {
            foreach (var r in replacements)
            {
                element.Parent.InsertBefore(r, element);
            }

            element.Parent.RemoveChild(element);
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
