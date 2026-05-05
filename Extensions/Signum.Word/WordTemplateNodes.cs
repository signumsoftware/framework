using DocumentFormat.OpenXml;
using W = DocumentFormat.OpenXml.Wordprocessing;
using D = DocumentFormat.OpenXml.Drawing;
using S = DocumentFormat.OpenXml.Spreadsheet;
using Signum.Utilities.DataStructures;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using Signum.DynamicQuery.Tokens;
using Signum.Templating;

namespace Signum.Word;

public interface INodeProvider
{
    OpenXmlLeafTextElement NewText(string text);
    OpenXmlCompositeElement NewRun(OpenXmlCompositeElement? runProps, string? text, SpaceProcessingModeValues? spaceMode = null, bool initialBr = false);
    bool IsRun(OpenXmlElement? element);
    bool IsText(OpenXmlElement? element);
    string GetText(OpenXmlElement run);
    OpenXmlCompositeElement CastRun(OpenXmlElement element);
    OpenXmlCompositeElement GetRunProperties(OpenXmlCompositeElement run);
    bool IsParagraph(OpenXmlElement? element);
    bool IsRunProperties(OpenXmlElement? a);
    OpenXmlCompositeElement WrapInRun(OpenXmlElement text);
}

public class WordprocessingNodeProvider : INodeProvider
{
    public OpenXmlCompositeElement CastRun(OpenXmlElement element)
    {
        return (W.Run)element;
    }

    public OpenXmlCompositeElement NewRun(OpenXmlCompositeElement? runProps, string? text, SpaceProcessingModeValues? spaceMode = null, bool initialBr = false)
    {
        var textNode = new W.Text(text!) { Space = spaceMode };

        var result = new W.Run(runProps!, textNode);

        if (initialBr)
            result.InsertBefore(new W.Break(), textNode);

        return result;
    }

    public string GetText(OpenXmlElement run)
    {
        if (run is W.Text t)
            return t.Text;

        return run.ChildElements.OfType<W.Text>().SingleOrDefault()?.Text ?? "";
    }

    public OpenXmlLeafTextElement NewText(string text)
    {
        return new W.Text(text);
    }

    public bool IsRun(OpenXmlElement? a)
    {
        return a is W.Run;
    }

    public bool IsText(OpenXmlElement? a)
    {
        return a is W.Text;
    }
    
    public OpenXmlCompositeElement GetRunProperties(OpenXmlCompositeElement run)
    {
        return ((W.Run)run).RunProperties!;
    }

    public bool IsParagraph(OpenXmlElement? element)
    {
        return element is W.Paragraph;
    }

    public bool IsRunProperties(OpenXmlElement? element)
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

    public OpenXmlCompositeElement NewRun(OpenXmlCompositeElement? runProps, string? text, SpaceProcessingModeValues? spaceMode = null, bool initialBr = false)
    {
        var textElement = new D.Text(text!);

        var result = new D.Run(runProps!, textElement);
        
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

    public bool IsRun(OpenXmlElement? a)
    {
        return a is D.Run;
    }

    public bool IsText(OpenXmlElement? a)
    {
        return a is D.Text;
    }

    public OpenXmlCompositeElement GetRunProperties(OpenXmlCompositeElement run)
    {
        return ((D.Run)run).RunProperties!;
    }

    public bool IsParagraph(OpenXmlElement? element)
    {
        return element is D.Paragraph;
    }

    public bool IsRunProperties(OpenXmlElement? element)
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

    public OpenXmlCompositeElement NewRun(OpenXmlCompositeElement? runProps, string? text, SpaceProcessingModeValues? spaceMode = null, bool initialBr = false)
    {
        var textElement = new S.Text(text!);
        var result = new S.Run(runProps!, textElement);
        
        if (initialBr)
            result.InsertBefore(new S.Break(), textElement);

        return result;
    }

    public OpenXmlLeafTextElement NewText(string text)
    {
        return new S.Text(text);
    }

    public string GetText(OpenXmlElement? run)
    {
        return run is S.Run r ? r.ChildElements.OfType<S.Text>().SingleOrDefault()?.Text ?? "" :
            run is S.Text s ? s.Text ?? "" :
            "";
    }

    public bool IsRun(OpenXmlElement? a)
    {
        return a is S.Run;
    }

    public bool IsText(OpenXmlElement? a)
    {
        return a is S.Text;
    }

    public OpenXmlCompositeElement GetRunProperties(OpenXmlCompositeElement run)
    {
        return ((S.Run)run).RunProperties!;
    }

    public bool IsParagraph(OpenXmlElement? element)
    {
        return false;
    }

    public bool IsRunProperties(OpenXmlElement? element)
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

    OpenXmlCompositeElement? runProperties;
    public OpenXmlCompositeElement? RunProperties
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
        this.NodeProvider = original.NodeProvider;
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

    OpenXmlCompositeElement? runProperties;
    public OpenXmlCompositeElement? RunProperties
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

    public abstract void Synchronize(TemplateSynchronizationContext sc);
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
        ValueProvider.FillQueryTokens(tokens, forForeach: false);
    }

    protected internal override void RenderNode(WordTemplateParameters p)
    {
        p.CurrentTokenNode = this;
        object? obj = ValueProvider.GetValue(p);
        p.CurrentTokenNode = null;

        if (obj is OpenXmlElement oxe)
        {
            if (oxe is W.Paragraph)
            {
                var par = this.Ancestors().OfType<W.Paragraph>().FirstEx();
                par.InsertAfterSelf(oxe);
                this.Remove();
                if (par.GetFirstChild<W.Run>() == null)
                    par.Remove();
            }
            else
            {
                this.ReplaceBy(oxe);
            }
        }
        else if (obj is IEnumerable<OpenXmlElement> oxes)
        {
            if (oxes.OfType<W.Paragraph>().Any())
            {
                var par = this.Ancestors().OfType<W.Paragraph>().FirstEx();
                foreach (var ox in oxes.Select(p => p as W.Paragraph ?? new W.Paragraph(p)).Reverse())
                {
                    par.InsertAfterSelf(ox);
                }
                this.Remove();
                if (par.GetFirstChild<W.Run>() == null)
                    par.Remove();
            }
            else
            {
                foreach (var ox in oxes.Reverse())
                {
                    this.InsertAfterSelf(ox);
                }
                this.Remove();
            }
        }
        else
        {
            string? text = obj is Enum en ? en.NiceToString() :
                obj is bool b ? (b ? BooleanEnum.True.NiceToString() : BooleanEnum.False.NiceToString()) :
                obj is TimeSpan ts ? ts.ToString(Format?.Replace(":", @"\:") ?? ValueProvider.Format, p.Culture) :
                obj is IFormattable fo ? SafeFormat(fo, Format ?? ValueProvider.Format, p.Culture) :
                obj?.ToString();

            if (text != null && text.Contains('\n'))
            {
                var replacements = text.Lines()
                    .Select((line, i) => NodeProvider.NewRun((OpenXmlCompositeElement?)RunProperties?.CloneNode(true), line, initialBr: i > 0));

                this.ReplaceBy(replacements);
            }
            else
            {
                this.ReplaceBy(NodeProvider.NewRun((OpenXmlCompositeElement?)RunProperties?.CloneNode(true), text));
            }
        }
    }

    static string SafeFormat(IFormattable fo, string? format, IFormatProvider provider)
    {
        try
        {
            return fo.ToString(format, provider);
        }
        catch (FormatException)
        {
            return fo.ToString(null, provider); // Fallback to default formatting
        }
    }

    protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
    {
        var str = "@" + this.ValueProvider.ToString(variables, Format.HasText() ? (":" + TemplateUtils.ScapeColon(Format)) : null);

        this.ReplaceBy(this.NodeProvider.NewRun((OpenXmlCompositeElement?)this.RunProperties?.CloneNode(true), str));
    }

    public override void WriteTo(System.Xml.XmlWriter xmlWriter)
    {
        var tempText = this.NodeProvider.NewText(ValueProvider?.ToString() ?? "");

        this.AppendChild(tempText);
        base.WriteTo(xmlWriter);
        this.RemoveChild(tempText);
    }

    public override OpenXmlElement CloneNode(bool deep)
    {
        return new TokenNode(this);
    }

    public override void Synchronize(TemplateSynchronizationContext sc)
    {
        ValueProvider.Synchronize(sc, "@", false);

        ValueProvider.Declare(sc.Variables);
    }

    public override string InnerText => this.ValueProvider?.ToString() ?? "";
}

public class DeclareNode : BaseNode
{
    public readonly ValueProviderBase ValueProvider;

    internal DeclareNode(INodeProvider nodeProvider, ValueProviderBase valueProvider, Action<bool, string> addError): base(nodeProvider)
    {
        if (valueProvider != null && !valueProvider.Variable.HasText())
            addError(true, "declare {0} should end with 'as $someVariable'".FormatWith(valueProvider.ToString()));

        this.ValueProvider = valueProvider!;
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
            !this.Parent!.ChildElements.Any(a => BlockContainerNode.IsImportant(a, NodeProvider) && a != this))
            this.Parent.Remove();
        else
            this.Remove();
    }

    protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
    {
        string str = "@declare" + ValueProvider.ToString(variables, null);

        this.ReplaceBy(this.NodeProvider.NewRun((OpenXmlCompositeElement?)this.RunProperties?.CloneNode(true), str));

        ValueProvider.Declare(variables);
    }

    public override void Synchronize(TemplateSynchronizationContext sc)
    {
        ValueProvider.Synchronize(sc, "@declare", false);
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

        var parent = this.Parent!;
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

    public override void Synchronize(TemplateSynchronizationContext sc)
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

    public static string UserString(Type? type)
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

    public OpenXmlElement? CommonAncestor;

    protected OpenXmlElement FindCommonAncestor(MatchNode errorHint, params MatchNode[] tokens)
    {
        var chains = tokens.Select(t => ((OpenXmlElement)t).Follow(a => a.Parent).Reverse().ToList()).ToArray();

        int minLen = chains.Min(c => c.Count);
        int divergeAt = minLen;
        for (int i = 0; i < minLen; i++)
        {
            if (chains.Select(c => c[i]).Distinct(ReferenceEqualityComparer.Instance).Count() != 1)
            {
                divergeAt = i;
                break;
            }
        }

        var children = chains.Select(c => c[divergeAt]).ToArray();

        for (int i = 0; i < tokens.Length; i++)
            AssertNotImportant(chains[i], children[i], errorHint, tokens[i], tokens[(i + 1) % tokens.Length]);

        return chains[0][divergeAt - 1];
    }

    protected OpenXmlElement ChildOfAncestor(MatchNode token) =>
        ((OpenXmlElement)token).Follow(a => a.Parent).First(a => a.Parent == CommonAncestor);

    protected List<OpenXmlElement> NodesBetween(MatchNode first, MatchNode last)
    {
        var firstChild = ChildOfAncestor(first);
        var lastChild = ChildOfAncestor(last);

        int indexFirst = CommonAncestor!.ChildElements.IndexOf(firstChild);
        int indexLast = CommonAncestor!.ChildElements.IndexOf(lastChild);

        return CommonAncestor!.ChildElements.Where((e, i) => indexFirst < i && i < indexLast).ToList();
    }

    protected static OpenXmlElement? ReplaceMatchNode(MatchNode token, string text)
    {
        var run = token.NodeProvider.NewRun((OpenXmlCompositeElement?)token.RunProperties?.CloneNode(true), text);
        var container = ((OpenXmlElement)token).Follow(a => a.Parent).Last();
        if (container == token) return run;
        token.ReplaceBy(run);
        return container;
    }

    protected static MatchNode CloneToken(MatchNode token)
    {
        var container = ((OpenXmlElement)token).Follow(a => a.Parent).Last();
        if (container == token)
            return (MatchNode)token.CloneNode(true);
        var containerClone = container.CloneNode(true);
        return containerClone as MatchNode ?? containerClone.Descendants<MatchNode>().SingleEx();
    }

    protected static MatchNode? CloneOptionalToken(MatchNode? token) =>
        token == null ? null : CloneToken(token);

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
                
                throw new InvalidOperationException($"Node {errorHint1.Match} is not at the same level than {errorHint2.Match}{hint}. Important nodes could be removed in the chain:\n\n" +
                    chain.Skip(chain.IndexOf(openXmlElement)).Select((a, p) => (a.GetType().Name + " with text:" + a.InnerText).Indent(p * 4)).ToString("\n\n"));
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

            if (nodeProvider.IsText(text) && string.IsNullOrWhiteSpace(nodeProvider.GetText(text!)))
                return false;

            return true; 
        }

        if (c is BaseNode)
            return true;

        return false;
    }


}

public class ForeachNode : BlockContainerNode
{
    public readonly ValueProviderBase ValueProvider;

    public MatchNode ForeachToken = null!;
    public MatchNode EndForeachToken = null!;

    public BlockNode? ForeachBlock;

    public ForeachNode(INodeProvider nodeProvider, ValueProviderBase valueProvider) : base(nodeProvider)
    {
        this.ValueProvider = valueProvider;
        if (valueProvider != null)
            valueProvider.IsForeach = true;
    }

    public ForeachNode(ForeachNode original)
        : base(original)
    {
        this.ValueProvider = original.ValueProvider;
        this.ForeachToken = CloneToken(original.ForeachToken);
        this.EndForeachToken = CloneToken(original.EndForeachToken);
        this.ForeachBlock = (BlockNode?)original.ForeachBlock?.Let(a => a.CloneNode(true));
    }

    public override void FillTokens(List<QueryToken> tokens)
    {
        ValueProvider.FillQueryTokens(tokens, forForeach: true);

        this.ForeachBlock!.FillTokens(tokens);
    }

    public override OpenXmlElement CloneNode(bool deep)
    {
        return new ForeachNode(this);
    }

    protected internal override void ReplaceBlock()
    {
        CommonAncestor = FindCommonAncestor(ForeachToken, ForeachToken, EndForeachToken);

        this.ForeachBlock = new BlockNode(this.NodeProvider);
        this.ForeachBlock.MoveChilds(NodesBetween(ForeachToken, EndForeachToken));

        ChildOfAncestor(ForeachToken).ReplaceBy(this);
        ChildOfAncestor(EndForeachToken).Remove();
    }

    public override void WriteTo(XmlWriter xmlWriter)
    {
        this.AppendChild(this.ForeachBlock);

        base.WriteTo(xmlWriter);

        this.RemoveChild(this.ForeachBlock!);
    }

    protected internal override void RenderNode(WordTemplateParameters p)
    {
        var parent = this.Parent!;

        this.ValueProvider.Foreach(p, () =>
        {
            var clone = (BlockNode)this.ForeachBlock!.CloneNode(true);

            var index = parent.ChildElements.IndexOf(this);

            parent.InsertAt(clone, index);

            clone.RenderNode(p);
        });

        parent.RemoveChild(this);
    }

    protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
    {
        var parent = this.Parent!;
        int index = parent.ChildElements.IndexOf(this);
        this.Remove();
        parent.InsertAt(ReplaceMatchNode(ForeachToken, "@foreach" + this.ValueProvider.ToString(variables, null))!, index++);
        {
            var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
            ValueProvider.Declare(newVars);
            this.ForeachBlock!.RenderTemplate(newVars);
            parent.MoveChildsAt(ref index, this.ForeachBlock.ChildElements);
        }
        parent.InsertAt(ReplaceMatchNode(EndForeachToken, "@endforeach")!, index++);
    }

    public override void Synchronize(TemplateSynchronizationContext sc)
    {
        using (sc.NewScope())
        {
            ValueProvider.Synchronize(sc, "@foreach", false);

            using (sc.NewScope())
            {
                ValueProvider.Declare(sc.Variables);

                this.ForeachBlock!.Synchronize(sc);
            }
        }
    }

    public override string InnerText => $@"{ForeachToken.InnerText}{ForeachBlock!.InnerText}{EndForeachToken.InnerText}";
}


public class AnyNode : BlockContainerNode
{
    public readonly ConditionBase Condition;

    public MatchNode AnyToken = null!;
    public MatchNode? NotAnyToken;
    public MatchNode EndAnyToken = null!;

    public BlockNode? AnyBlock;
    public BlockNode? NotAnyBlock;

    public AnyNode(INodeProvider nodeProvider, ConditionBase condition) : base(nodeProvider)
    {
        this.Condition = condition;
    }

    public AnyNode(AnyNode original)
        : base(original)
    {
        this.Condition= original.Condition.Clone();

        this.AnyToken = CloneToken(original.AnyToken);
        this.NotAnyToken = CloneOptionalToken(original.NotAnyToken);
        this.EndAnyToken = CloneToken(original.EndAnyToken);

        this.AnyBlock = (BlockNode?)original.AnyBlock?.Let(a => a.CloneNode(true));
        this.NotAnyBlock = (BlockNode?)original.NotAnyBlock?.Let(a => a.CloneNode(true));
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

        this.RemoveChild(this.AnyBlock!);
    }

    protected internal override void ReplaceBlock()
    {
        if (this.NotAnyToken == null)
        {
            CommonAncestor = FindCommonAncestor(AnyToken, AnyToken, EndAnyToken);

            this.AnyBlock = new BlockNode(this.NodeProvider);
            this.AnyBlock.MoveChilds(NodesBetween(AnyToken, EndAnyToken));

            ChildOfAncestor(AnyToken).ReplaceBy(this);
            ChildOfAncestor(EndAnyToken).Remove();
        }
        else
        {
            CommonAncestor = FindCommonAncestor(AnyToken, AnyToken, NotAnyToken, EndAnyToken);

            this.AnyBlock = new BlockNode(this.NodeProvider);
            this.AnyBlock.MoveChilds(NodesBetween(AnyToken, NotAnyToken));

            this.NotAnyBlock = new BlockNode(this.NodeProvider);
            this.NotAnyBlock.MoveChilds(NodesBetween(NotAnyToken, EndAnyToken));

            ChildOfAncestor(AnyToken).ReplaceBy(this);
            ChildOfAncestor(NotAnyToken).Remove();
            ChildOfAncestor(EndAnyToken).Remove();
        }
    }

    public override void FillTokens(List<QueryToken> tokens)
    {
        this.Condition.FillQueryTokens(tokens);

        this.AnyBlock!.FillTokens(tokens);
        if (this.NotAnyBlock != null)
            this.NotAnyBlock.FillTokens(tokens);
    }

    protected internal override void RenderNode(WordTemplateParameters p)
    {
        var filtered = this.Condition.GetFilteredRows(p);

        using (filtered is IEnumerable<ResultRow> rr ? p.QueryContext!.OverrideRows(rr) : null)
        {
            if (filtered.Any())
            {
                this.ReplaceBy(this.AnyBlock!);
                this.AnyBlock!.RenderNode(p);
            }
            else if (NotAnyBlock != null)
            {
                this.ReplaceBy(this.NotAnyBlock);
                this.NotAnyBlock.RenderNode(p);
            }
            else
                this.Parent!.RemoveChild(this);
        }
    }

    public override void Synchronize(TemplateSynchronizationContext sc)
    {
        using (sc.NewScope())
        {
            this.Condition.Synchronize(sc, "@any");

            using (sc.NewScope())
            {
                this.Condition.Declare(sc.Variables);

                AnyBlock!.Synchronize(sc);
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
    }

    protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
    {
        var parent = this.Parent!;
        int index = parent.ChildElements.IndexOf(this);
        this.Remove();

        string str = "@any" + this.Condition.ToString(variables);

        parent.InsertAt(ReplaceMatchNode(this.AnyToken, str)!, index++);
        {
            var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
            Condition.Declare(newVars);
            this.AnyBlock!.RenderTemplate(newVars);
            parent.MoveChildsAt(ref index, this.AnyBlock.ChildElements);
        }

        if (this.NotAnyToken != null)
        {
            parent.InsertAt(ReplaceMatchNode(this.NotAnyToken, "@notany")!, index++);

            var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
            Condition.Declare(newVars);
            this.NotAnyBlock!.RenderTemplate(newVars);
            parent.MoveChildsAt(ref index, this.NotAnyBlock.ChildElements);
        }

        parent.InsertAt(ReplaceMatchNode(this.EndAnyToken, "@endany")!, index++);
    }

    public override string InnerText => $@"{this.AnyToken.InnerText}{this.AnyBlock!.InnerText}{this.NotAnyToken?.InnerText}{this.NotAnyBlock?.InnerText}{this.EndAnyToken.InnerText}";
}


public class IfNode : BlockContainerNode
{
    public MatchNode IfToken = null!;
    public ConditionBase Condition;
    public BlockNode? IfBlock;

    public List<(MatchNode Token, ConditionBase Condition, BlockNode? Block)> ElseIfBranches = new();

    public MatchNode? ElseToken;
    public BlockNode? ElseBlock;

    public MatchNode EndIfToken = null!;

    internal IfNode(INodeProvider nodeProvider, ConditionBase condition) : base(nodeProvider)
    {
        this.Condition = condition;
    }

    public IfNode(IfNode original)
        : base(original)
    {
        this.Condition = original.Condition.Clone();

        this.IfToken = CloneToken(original.IfToken);
        this.ElseToken = CloneOptionalToken(original.ElseToken);
        this.EndIfToken = CloneToken(original.EndIfToken);

        this.IfBlock = (BlockNode?)original.IfBlock?.Let(a => a.CloneNode(true));
        this.ElseBlock = (BlockNode?)original.ElseBlock?.Let(a => a.CloneNode(true));

        this.ElseIfBranches = original.ElseIfBranches
            .Select(b => (CloneToken(b.Token), b.Condition.Clone(), (BlockNode?)b.Block?.Let(a => a.CloneNode(true))))
            .ToList();
    }

    public override OpenXmlElement CloneNode(bool deep)
    {
        return new IfNode(this);
    }

    protected internal override void ReplaceBlock()
    {
        var tokens = AllTokens().ToArray();
        CommonAncestor = FindCommonAncestor(IfToken, tokens);

        this.IfBlock = BlockBetween(tokens[0], tokens[1]);

        for (int i = 0; i < ElseIfBranches.Count; i++)
            ElseIfBranches[i] = (ElseIfBranches[i].Token, ElseIfBranches[i].Condition, BlockBetween(tokens[i + 1], tokens[i + 2]));

        if (ElseToken != null)
            this.ElseBlock = BlockBetween(tokens[^2], tokens[^1]);

        ChildOfAncestor(tokens[0]).ReplaceBy(this);
        for (int i = 1; i < tokens.Length; i++)
            ChildOfAncestor(tokens[i]).Remove();
    }

    IEnumerable<MatchNode> AllTokens()
    {
        yield return IfToken;
        foreach (var (token, _, _) in ElseIfBranches)
            yield return token;
        if (ElseToken != null)
            yield return ElseToken;
        yield return EndIfToken;
    }

    BlockNode BlockBetween(MatchNode first, MatchNode last)
    {
        var block = new BlockNode(this.NodeProvider);
        block.MoveChilds(NodesBetween(first, last));
        return block;
    }

    public override void WriteTo(System.Xml.XmlWriter xmlWriter)
    {
        this.AppendChild(this.IfBlock);
        foreach (var (_, _, block) in ElseIfBranches)
            if (block != null) this.AppendChild(block);
        if (this.ElseBlock != null)
            this.AppendChild(this.ElseBlock);

        base.WriteTo(xmlWriter);

        if (this.ElseBlock != null)
            this.RemoveChild(this.ElseBlock);
        foreach (var (_, _, block) in ElseIfBranches.AsEnumerable().Reverse())
            if (block != null) this.RemoveChild(block);
        this.RemoveChild(this.IfBlock!);
    }

    public override void FillTokens(List<QueryToken> tokens)
    {
        this.Condition.FillQueryTokens(tokens);
        this.IfBlock!.FillTokens(tokens);

        foreach (var (_, cond, block) in ElseIfBranches)
        {
            cond.FillQueryTokens(tokens);
            block?.FillTokens(tokens);
        }

        if (this.ElseBlock != null)
            this.ElseBlock.FillTokens(tokens);
    }

    protected internal override void RenderNode(WordTemplateParameters p)
    {
        if (this.Condition.Evaluate(p))
        {
            this.ReplaceBy(this.IfBlock!);
            this.IfBlock!.RenderNode(p);
        }
        else
        {
            foreach (var (_, cond, block) in ElseIfBranches)
            {
                if (cond.Evaluate(p))
                {
                    this.ReplaceBy(block!);
                    block!.RenderNode(p);
                    return;
                }
            }

            if (ElseBlock != null)
            {
                this.ReplaceBy(this.ElseBlock);
                this.ElseBlock!.RenderNode(p);
            }
            else
                this.Parent!.RemoveChild(this);
        }
    }

    public override void Synchronize(TemplateSynchronizationContext sc)
    {
        using (sc.NewScope())
        {
            this.Condition.Synchronize(sc, "@if");

            using (sc.NewScope())
            {
                this.Condition.Declare(sc.Variables);
                IfBlock!.Synchronize(sc);
            }

            foreach (var (_, cond, block) in ElseIfBranches)
            {
                cond.Synchronize(sc, "@elseif");
                using (sc.NewScope())
                {
                    cond.Declare(sc.Variables);
                    block!.Synchronize(sc);
                }
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
    }

    protected internal override void RenderTemplate(ScopedDictionary<string, ValueProviderBase> variables)
    {
        var parent = this.Parent!;
        int index = parent.ChildElements.IndexOf(this);
        this.Remove();

        parent.InsertAt(ReplaceMatchNode(this.IfToken, "@if" + this.Condition.ToString(variables))!, index++);
        {
            var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
            this.Condition.Declare(newVars);
            this.IfBlock!.RenderTemplate(newVars);
            parent.MoveChildsAt(ref index, this.IfBlock.ChildElements);
        }

        foreach (var (token, cond, block) in ElseIfBranches)
        {
            parent.InsertAt(ReplaceMatchNode(token, "@elseif" + cond.ToString(variables))!, index++);
            var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
            cond.Declare(newVars);
            block!.RenderTemplate(newVars);
            parent.MoveChildsAt(ref index, block!.ChildElements);
        }

        if (this.ElseToken != null)
        {
            parent.InsertAt(ReplaceMatchNode(this.ElseToken, "@else")!, index++);
            var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
            this.Condition.Declare(newVars);
            this.ElseBlock!.RenderTemplate(newVars);
            parent.MoveChildsAt(ref index, this.ElseBlock.ChildElements);
        }

        parent.InsertAt(ReplaceMatchNode(this.EndIfToken, "@endif")!, index++);
    }

    public override string InnerText =>
        $"{this.IfToken.InnerText}{this.IfBlock!.InnerText}" +
        string.Concat(ElseIfBranches.Select(b => b.Token.InnerText + (b.Block?.InnerText ?? ""))) +
        $"{this.ElseToken?.InnerText}{this.ElseBlock?.InnerText}{this.EndIfToken.InnerText}";
}

public static class OpenXmlElementExtensions
{
    public static void ReplaceBy(this OpenXmlElement element, OpenXmlElement replacement)
    {
        element.Parent!.ReplaceChild(replacement, element);
    }

    public static void ReplaceBy(this OpenXmlElement element, IEnumerable<OpenXmlElement> replacements)
    {
        foreach (var r in replacements)
        {
            element.Parent!.InsertBefore(r, element);
        }

        element.Parent!.RemoveChild(element);
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

    public static void CopyTo<T>(this T? source, T target) where T : OpenXmlCompositeElement
    {
        if (source != null)
        {
            foreach (var item in source.ChildElements.EmptyIfNull())
            {
                target.AddChild(item.CloneNode(true));
            }
        }
    }
}
