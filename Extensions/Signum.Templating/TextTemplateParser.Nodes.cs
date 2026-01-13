using System.Web;
using Signum.Utilities.DataStructures;
using Microsoft.AspNetCore.Html;
using System.Globalization;
using Signum.DynamicQuery.Tokens;
using System.Text.RegularExpressions;

namespace Signum.Templating;

public static partial class TextTemplateParser
{
    public abstract class TextNode
    {
        public const string EmptyPlaceholder = "(∅)";


        public abstract void PrintList(TextTemplateParameters p);
        public abstract void FillQueryTokens(List<QueryToken> list);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ScopedDictionary<string, ValueProviderBase> variables = new ScopedDictionary<string, ValueProviderBase>(null);
            ToString(sb, variables);
            return sb.ToString();
        }

        public abstract void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables);

        public abstract void Synchronize(TemplateSynchronizationContext sc);
    }

    public class LiteralNode : TextNode
    {
        public string Text;

        public LiteralNode(string text)
        {
            Text = text;
        }

        public override void PrintList(TextTemplateParameters p)
        {
            p.StringBuilder.Append(Text);
        }

        public override void FillQueryTokens(List<QueryToken> list)
        {
            return;
        }

        public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
        {
            sb.Append(Text);               
        }

        public override void Synchronize(TemplateSynchronizationContext sc)
        {
            return;
        }
    }

    public class DeclareNode : TextNode
    {
        public readonly ValueProviderBase? ValueProvider;

        internal DeclareNode(ValueProviderBase? valueProvider, Action<bool, string> addError)
        {
            if (!valueProvider!.Variable.HasText())
                addError(true, "declare[{0}] should end with 'as $someVariable'".FormatWith(valueProvider.ToString()));

            this.ValueProvider = valueProvider;
        }

        public override void PrintList(TextTemplateParameters p)
        {
            if (ValueProvider != null && ValueProvider is not TokenValueProvider && ValueProvider.Variable != null)
            {
                var obj = ValueProvider!.GetValue(p);

                p.RuntimeVariables.Add(ValueProvider.Variable, obj);
            }

            p.StringBuilder.Append(EmptyPlaceholder);
        }

        public override void FillQueryTokens(List<QueryToken> list)
        {
        }

        public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
        {
            sb.Append("@declare");

            ValueProvider!.ToStringBrackets(sb, variables, null);

            ValueProvider.Declare(variables);
        }

        public override void Synchronize(TemplateSynchronizationContext sc)
        {
            ValueProvider!.Synchronize(sc, "@declare", false);
        }
    }

    public class ValueNode : TextNode
    {
        public readonly ValueProviderBase? ValueProvider;
        public readonly bool IsRaw;
        public readonly string? Format;

        internal ValueNode(ValueProviderBase? valueProvider, string? format, bool isRaw)
        {
            this.ValueProvider = valueProvider;
            this.Format = format;
            this.IsRaw = isRaw;
        }

        public override void PrintList(TextTemplateParameters p)
        {
            var obj = ValueProvider!.GetValue(p);

            var text = obj is Enum ? ((Enum)obj).NiceToString() :
                   obj is TimeSpan ts ? ts.ToString((Format ?? ValueProvider.Format)?.Replace(":", @"\:"), p.Culture) :
                   obj is IFormattable ? ((IFormattable)obj).ToString(Format ?? ValueProvider.Format, p.Culture) :
                   obj?.ToString();

            p.StringBuilder.Append(p.IsHtml && !IsRaw && !(obj is HtmlString )? HttpUtility.HtmlEncode(text) : text);
        }

        public override void FillQueryTokens(List<QueryToken> list)
        {
            this.ValueProvider!.FillQueryTokens(list, false);
        }

        public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
        {
            sb.Append("@");
            if (IsRaw)
                sb.Append("raw");

            ValueProvider!.ToStringBrackets(sb, variables, Format.HasText() ? (":" + TemplateUtils.ScapeColon(Format)) : null);
        }

        public override void Synchronize(TemplateSynchronizationContext sc)
        {
            ValueProvider!.Synchronize(sc, IsRaw ? "@raw[]" : "@[]", false);
        }
    }

    public sealed class BlockNode : TextNode
    {
        public readonly List<TextNode> Nodes = new List<TextNode>();

        public readonly TextNode? owner;

        public BlockNode(TextNode? owner)
        {
            this.owner = owner;
        }

        public string Print(TextTemplateParameters p)
        {
            this.PrintList(p);
            var text = p.StringBuilder.ToString();
            var result = p.IsHtml ? CleanHtml(text) : CleanText(text);
            return result;
        }

        static string CleanText(string text)
        {
            var result = LineRegex.Replace(text, "");
            return result.Replace(EmptyPlaceholder, "");
        }

        static Regex TagRegex = new Regex(@"<(?<tag>p|li|tr|td|strong|em)>( |&nbsp;)*\(∅\)( |&nbsp;)*</\k<tag>>");
        static Regex CommentRegex = new Regex(@"<!-- *\(∅\) *-->");
        static Regex LineRegex = new Regex(@"^ *\(∅\) *$\r?\n", RegexOptions.Multiline);
        static string CleanHtml(string text)
        {
            
        retry:
            string newText = TagRegex.Replace(text, EmptyPlaceholder);

            if (newText != text)
            {
                text = newText;
                goto retry;
            }

            var text2 = CommentRegex.Replace(text, EmptyPlaceholder);

            var result = LineRegex.Replace(text2, "");
            result = result.Replace(EmptyPlaceholder, "");
            return result;
        }

   
        public override void PrintList(TextTemplateParameters p)
        {
            foreach (var node in Nodes)
            {
                node.PrintList(p);
            }
        }

        public override void FillQueryTokens(List<QueryToken> list)
        {
            foreach (var node in Nodes)
            {
                node.FillQueryTokens(list);
            }
        }

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

        public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
        {
            foreach (var n in Nodes)
            {
                n.ToString(sb, variables);
            }
        }

        public override void Synchronize(TemplateSynchronizationContext sc)
        {
            foreach (var item in Nodes)
                item.Synchronize(sc);
        }
        
    }

    public class ForeachNode : TextNode
    {
        public readonly ValueProviderBase? ValueProvider;

        public readonly BlockNode Block;

        public ForeachNode(ValueProviderBase? valueProvider)
        {
            this.ValueProvider = valueProvider;
            this.Block = new BlockNode(this);
        }

        public override void PrintList(TextTemplateParameters p)
        {
            p.StringBuilder.Append(EmptyPlaceholder);
            ValueProvider!.Foreach(p, () =>
            {
                Block.PrintList(p);
                p.StringBuilder.Append(EmptyPlaceholder);
            });
        }

        public override void FillQueryTokens(List<QueryToken> list)
        {
                ValueProvider!.FillQueryTokens(list, forForeach: true);
            Block.FillQueryTokens(list);
        }

        public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
        {
            sb.Append("@foreach");
            ValueProvider!.ToStringBrackets(sb, variables, null);
            {
                var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                ValueProvider.Declare(newVars);
                Block.ToString(sb, newVars);
            }
            sb.Append("@endforeach");
        }

        public override void Synchronize(TemplateSynchronizationContext sc)
        {
            using (sc.NewScope())
            {
                ValueProvider!.Synchronize(sc, "@foreach[]", false);

                using (sc.NewScope())
                {
                    ValueProvider.Declare(sc.Variables);

                    Block.Synchronize(sc);
                }
            }
        }
    }

    public class AnyNode : TextNode
    {
        public ConditionBase Condition;

        public readonly BlockNode AnyBlock;
        public BlockNode? NotAnyBlock;

        internal AnyNode(ConditionBase condition)
        {
            this.Condition = condition;
            AnyBlock = new BlockNode(this);
        }

        public BlockNode CreateNotAny()
        {
            NotAnyBlock = new BlockNode(this);
            return NotAnyBlock;
        }

        public override void PrintList(TextTemplateParameters p)
        {
            var filtered = this.Condition.GetFilteredRows(p);

            using (filtered is IEnumerable<ResultRow> rr ? p.QueryContext!.OverrideRows(rr) : null)
            {
                p.StringBuilder.Append(EmptyPlaceholder);
                if (filtered.Any())
                {
                    AnyBlock.PrintList(p);
                }
                else if (NotAnyBlock != null)
                {
                    NotAnyBlock.PrintList(p);
                }
            }
        }

        public override void FillQueryTokens(List<QueryToken> list)
        {
            Condition.FillQueryTokens(list);
            AnyBlock.FillQueryTokens(list);
            if (NotAnyBlock != null)
                NotAnyBlock.FillQueryTokens(list);
        }


        public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
        {
            sb.Append("@any");
            Condition.ToStringBrackets(sb, variables);
            {
                var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                Condition.Declare(newVars);
                AnyBlock.ToString(sb, newVars);
            }
            
            if (NotAnyBlock != null)
            {
                sb.Append("@notany");
                var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                Condition.Declare(newVars);
                NotAnyBlock.ToString(sb, newVars);
            }

            sb.Append("@endany");
        }

        public override void Synchronize(TemplateSynchronizationContext sc)
        {
            using (sc.NewScope())
            {
                Condition.Synchronize(sc, "@any[]");

                using (sc.NewScope())
                {
                    Condition.Declare(sc.Variables);

                    AnyBlock.Synchronize(sc);
                }

                if (NotAnyBlock != null)
                {
                    using (sc.NewScope())
                    {
                        Condition.Declare(sc.Variables);

                        NotAnyBlock.Synchronize(sc);
                    }
                }
            }
        }
    }

    public class IfNode : TextNode
    {
        public readonly ConditionBase Condition;
        public readonly BlockNode IfBlock;
        public BlockNode? ElseBlock;

        internal IfNode(ConditionBase condition, TextTemplateParserImp walker)
        {
            this.Condition = condition;
            this.IfBlock = new BlockNode(this);
        }
        
        public BlockNode CreateElse()
        {
            ElseBlock = new BlockNode(this);
            return ElseBlock;
        }

        public override void FillQueryTokens(List<QueryToken> list)
        {
            this.Condition.FillQueryTokens(list);
            IfBlock.FillQueryTokens(list);
            if (ElseBlock != null)
                ElseBlock.FillQueryTokens(list);
        }

        public override void PrintList(TextTemplateParameters p)
        {
            p.StringBuilder.Append(EmptyPlaceholder);
            if (Condition.Evaluate(p))
            {
                IfBlock.PrintList(p);
            }
            else if (ElseBlock != null)
            {
                ElseBlock.PrintList(p);
            }
            p.StringBuilder.Append(EmptyPlaceholder);
        }

        public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
        {
            sb.Append("@if[");
            Condition.ToStringInternal(sb, variables);
            sb.Append("]");
            {
                var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                Condition.Declare(newVars);
                IfBlock.ToString(sb, newVars);
            }

            if (ElseBlock != null)
            {
                sb.Append("@else");
                var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                Condition.Declare(newVars);
                ElseBlock.ToString(sb, newVars);
            }

            sb.Append("@endif");
        }


        public override void Synchronize(TemplateSynchronizationContext sc)
        {
            using (sc.NewScope())
            {
                Condition.Synchronize(sc, "if[]");

                using (sc.NewScope())
                {
                    Condition.Declare(sc.Variables);

                    IfBlock.Synchronize(sc);
                }

                if (ElseBlock != null)
                {
                    using (sc.NewScope())
                    {
                        Condition.Declare(sc.Variables);

                        ElseBlock.Synchronize(sc);
                    }
                }
            }
        }
    }
}

public class TextTemplateParameters : TemplateParameters
{
    public TextTemplateParameters(IEntity? entity, CultureInfo culture, QueryContext? qc) :
          base(entity, culture, qc)
    { }

    public StringBuilder StringBuilder = new StringBuilder();
    public bool IsHtml;
    public object? Model;

    public override object GetModel()
    {
        if (Model == null)
            throw new ArgumentException("There is no Model set");

        return Model;
    }
}
