using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Engine.Templating;
using Microsoft.AspNetCore.Html;
using Signum.Entities;
using System.Globalization;

namespace Signum.Engine.Templating
{
    public static partial class TextTemplateParser
    {
        public abstract class TextNode
        {
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

            public abstract void Synchronize(SynchronizationContext sc);
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

            public override void Synchronize(SynchronizationContext sc)
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

            public override void Synchronize(SynchronizationContext sc)
            {
                ValueProvider!.Synchronize(sc, "@declare");
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
                       obj is IFormattable ? ((IFormattable)obj).ToString(Format ?? ValueProvider.Format, p.Culture) :
                       obj?.ToString();

                p.StringBuilder.Append(p.IsHtml && !IsRaw && !(obj is HtmlString )? HttpUtility.HtmlEncode(text) : text);
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                this.ValueProvider!.FillQueryTokens(list);
            }

            public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
            {
                sb.Append("@");
                if (IsRaw)
                    sb.Append("raw");

                ValueProvider!.ToStringBrackets(sb, variables, Format.HasText() ? (":" + TemplateUtils.ScapeColon(Format)) : null);
            }

            public override void Synchronize(SynchronizationContext sc)
            {
                ValueProvider!.Synchronize(sc, IsRaw ? "@raw[]" : "@[]");
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
                return p.StringBuilder.ToString();
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

            public override void Synchronize(SynchronizationContext sc)
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
                ValueProvider!.Foreach(p, () => Block.PrintList(p));
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                ValueProvider!.FillQueryTokens(list);
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

            public override void Synchronize(SynchronizationContext sc)
            {
                ValueProvider!.Synchronize(sc, "@foreach[]");

                using (sc.NewScope())
                {
                    ValueProvider.Declare(sc.Variables);

                    Block.Synchronize(sc);
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

                using (filtered is IEnumerable<ResultRow> ? p.OverrideRows((IEnumerable<ResultRow>)filtered) : null)
                {
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

            public override void Synchronize(SynchronizationContext sc)
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
                if (Condition.Evaluate(p))
                {
                    IfBlock.PrintList(p);
                }
                else if (ElseBlock != null)
                {
                    ElseBlock.PrintList(p);
                }
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


            public override void Synchronize(SynchronizationContext sc)
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

    public class TextTemplateParameters : TemplateParameters
    {
        public TextTemplateParameters(IEntity? entity, CultureInfo culture, Dictionary<QueryToken, ResultColumn> columns, IEnumerable<ResultRow> rows) :
              base(entity, culture, columns, rows)
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
}
