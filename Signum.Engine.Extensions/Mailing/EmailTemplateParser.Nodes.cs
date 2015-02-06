using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Mailing;
using Signum.Engine.Translation;
using Signum.Engine.UserQueries;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Mailing;
using Signum.Entities.Reflection;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Engine.Templating;
using System.Collections;

namespace Signum.Engine.Mailing
{
    public static partial class EmailTemplateParser
    {
        public abstract class TextNode
        {
            public abstract void PrintList(EmailTemplateParameters p);
            public abstract void FillQueryTokens(List<QueryToken> list);

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                ScopedDictionary<string, ValueProviderBase> variables = new ScopedDictionary<string, ValueProviderBase>(null);
                ToString(sb, variables);
                return sb.ToString();
            }

            public abstract void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables);

            public abstract void Synchronize(SyncronizationContext sc);
        }

        public class LiteralNode : TextNode
        {
            public string Text;

            public override void PrintList(EmailTemplateParameters p)
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

            public override void Synchronize(SyncronizationContext sc)
            {
                return;
            }
        }

        public class DeclareNode : TextNode
        {
            public readonly ValueProviderBase ValueProvider;

            internal DeclareNode(ValueProviderBase valueProvider, Action<bool, string> addError)
            {
                if (!valueProvider.Variable.HasText())
                    addError(true, "declare[{0}] should end with 'as $someVariable'".FormatWith(valueProvider.ToString()));

                this.ValueProvider = valueProvider;
            }

            public override void PrintList(EmailTemplateParameters p)
            {
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
            }

            public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
            {
                sb.Append("@declare");

                ValueProvider.ToString(sb, variables, null);

                ValueProvider.Declare(variables);
            }

            public override void Synchronize(SyncronizationContext sc)
            {
                ValueProvider.Synchronize(sc, "@declare");
            }
        }

        public class ValueNode : TextNode
        {
            public readonly ValueProviderBase ValueProvider;
            public readonly bool IsRaw;
            public readonly string Format;

            internal ValueNode(ValueProviderBase valueProvider, string format, bool isRaw)
            {
                this.ValueProvider = valueProvider;
                this.Format = format;
                this.IsRaw = isRaw;
            }

            public override void PrintList(EmailTemplateParameters p)
            {
                var obj = ValueProvider.GetValue(p);

                var text = obj is Enum ? ((Enum)obj).NiceToString() :
                       obj is IFormattable ? ((IFormattable)obj).ToString(Format ?? ValueProvider.Format, p.Culture) :
                       obj.TryToString();

                p.StringBuilder.Append(p.IsHtml && !IsRaw && !(obj is HtmlString )? HttpUtility.HtmlEncode(text) : text);
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                this.ValueProvider.FillQueryTokens(list);
            }

            public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
            {
                sb.Append("@");
                if (IsRaw)
                    sb.Append("raw");

                ValueProvider.ToString(sb, variables, Format.HasText() ? (":" + Format) : null);
            }

            public override void Synchronize(SyncronizationContext sc)
            {
                ValueProvider.Synchronize(sc, IsRaw ? "@raw[]" : "@[]");
            }
        }

        public sealed class BlockNode : TextNode
        {
            public readonly List<TextNode> Nodes = new List<TextNode>();

            public readonly TextNode owner;

            public BlockNode(TextNode owner)
            {
                this.owner = owner;
            }

            public string Print(EmailTemplateParameters p)
            {
                this.PrintList(p);
                return p.StringBuilder.ToString();
            }

            public override void PrintList(EmailTemplateParameters p)
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

            public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
            {
                foreach (var n in Nodes)
                {
                    n.ToString(sb, variables);
                }
            }

            public override void Synchronize(SyncronizationContext sc)
            {
                foreach (var item in Nodes)
                    item.Synchronize(sc);
            }
            
        }

        public class ForeachNode : TextNode
        {
            public readonly ValueProviderBase ValueProvider;

            public readonly BlockNode Block;

            public ForeachNode(ValueProviderBase valueProvider)
            {
                this.ValueProvider = valueProvider;
                this.Block = new BlockNode(this);
            }

            public override void PrintList(EmailTemplateParameters p)
            {
                ValueProvider.Foreach(p, () => Block.PrintList(p));
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                ValueProvider.FillQueryTokens(list);
                Block.FillQueryTokens(list);
            }

            public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
            {
                sb.Append("@foreach");
                ValueProvider.ToString(sb, variables, null);
                {
                    var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                    ValueProvider.Declare(newVars);
                    Block.ToString(sb, newVars);
                }

                sb.Append("@endforeach");
            }

            public override void Synchronize(SyncronizationContext sc)
            {
                ValueProvider.Synchronize(sc, "@foreach[]");

                using (sc.NewScope())
                {
                    ValueProvider.Declare(sc.Variables);

                    Block.Synchronize(sc);
                }
            }
        }

        public class AnyNode : TextNode
        {
            public readonly ValueProviderBase ValueProvider;
            public readonly FilterOperation? Operation;
            public string Value;

            public readonly BlockNode AnyBlock;
            public BlockNode NotAnyBlock;

            internal AnyNode(ValueProviderBase valueProvider)
            {
                this.ValueProvider = valueProvider;
                AnyBlock = new BlockNode(this);
            }

            internal AnyNode(ValueProviderBase valueProvider, string operation, string value, Action<bool, string> addError)
            {
                this.ValueProvider = valueProvider;
                this.Operation = FilterValueConverter.ParseOperation(operation);
                this.Value = value;

                ValueProvider.ValidateConditionValue(value, Operation, addError);

                AnyBlock = new BlockNode(this);
            }

            public BlockNode CreateNotAny()
            {
                NotAnyBlock = new BlockNode(this);
                return NotAnyBlock;
            }

            public override void PrintList(EmailTemplateParameters p)
            {
                var filtered = this.ValueProvider.GetFilteredRows(p, this.Operation, this.Value);

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
                ValueProvider.FillQueryTokens(list);
                AnyBlock.FillQueryTokens(list);
                if (NotAnyBlock != null)
                    NotAnyBlock.FillQueryTokens(list);
            }


            public override void ToString(StringBuilder sb, ScopedDictionary<string, ValueProviderBase> variables)
            {
                sb.Append("@any");
                ValueProvider.ToString(sb, variables, Operation == null ? null : FilterValueConverter.ToStringOperation(Operation.Value) + Value);
                {
                    var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                    ValueProvider.Declare(newVars);
                    AnyBlock.ToString(sb, newVars);
                }
                
                if (NotAnyBlock != null)
                {
                    sb.Append("@notany");
                    var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                    ValueProvider.Declare(newVars);
                    NotAnyBlock.ToString(sb, newVars);
                }

                sb.Append("@endany");
            }

            public override void Synchronize(SyncronizationContext sc)
            {
                ValueProvider.Synchronize(sc, "@any[]");

                if (Operation != null)
                    sc.SynchronizeValue(ValueProvider.Type, ref Value, Operation == FilterOperation.IsIn);

                using (sc.NewScope())
                {
                    ValueProvider.Declare(sc.Variables);

                    AnyBlock.Synchronize(sc);
                }

                if (NotAnyBlock != null)
                {
                    using (sc.NewScope())
                    {
                        ValueProvider.Declare(sc.Variables);

                        NotAnyBlock.Synchronize(sc);
                    }
                }
            }
        }

        public class IfNode : TextNode
        {
            public readonly ValueProviderBase ValueProvider;
            public readonly BlockNode IfBlock;
            public BlockNode ElseBlock;
            private FilterOperation? Operation;
            private string Value;

            internal IfNode(ValueProviderBase valueProvider, TemplateWalker walker)
            {
                this.ValueProvider = valueProvider;
                this.IfBlock = new BlockNode(this);
            }

            internal IfNode(ValueProviderBase valueProvider, string operation, string value, Action<bool, string> addError)
            {
                this.ValueProvider = valueProvider;
                this.Operation = FilterValueConverter.ParseOperation(operation);
                this.Value = value;

                ValueProvider.ValidateConditionValue(value, Operation, addError);

                this.IfBlock = new BlockNode(this);
            }

            public BlockNode CreateElse()
            {
                ElseBlock = new BlockNode(this);
                return ElseBlock;
            }

            public override void FillQueryTokens(List<QueryToken> list)
            {
                this.ValueProvider.FillQueryTokens(list);
                IfBlock.FillQueryTokens(list);
                if (ElseBlock != null)
                    ElseBlock.FillQueryTokens(list);
            }

            public override void PrintList(EmailTemplateParameters p)
            {
                if (ValueProvider.GetCondition(p, this.Operation, this.Value))
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
                sb.Append("@if");
                ValueProvider.ToString(sb, variables, Operation == null ? null : FilterValueConverter.ToStringOperation(Operation.Value) + Value);
                {
                    var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                    ValueProvider.Declare(newVars);
                    IfBlock.ToString(sb, newVars);
                }

                if (ElseBlock != null)
                {
                    sb.Append("@else");
                    var newVars = new ScopedDictionary<string, ValueProviderBase>(variables);
                    ValueProvider.Declare(newVars);
                    ElseBlock.ToString(sb, newVars);
                }

                sb.Append("@endif");
            }


            public override void Synchronize(SyncronizationContext sc)
            {
                ValueProvider.Synchronize(sc, "if[]");

                if (Operation != null)
                    sc.SynchronizeValue(ValueProvider.Type, ref Value, Operation == FilterOperation.IsIn);

                using (sc.NewScope())
                {
                    ValueProvider.Declare(sc.Variables);

                    IfBlock.Synchronize(sc);
                }

                if (ElseBlock != null)
                {
                    using (sc.NewScope())
                    {
                        ValueProvider.Declare(sc.Variables);

                        ElseBlock.Synchronize(sc);
                    }
                }
            }
        }
    }
}
