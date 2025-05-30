using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Signum.Utilities.Reflection;

namespace Signum.Utilities.ExpressionTrees;


internal sealed class ExpressionStringBuilder : ExpressionVisitor
{
    private readonly StringBuilder _out;

    // Associate every unique label or anonymous parameter in the tree with an integer.
    // Labels are displayed as UnnamedLabel_#; parameters are displayed as Param_#.
    private Dictionary<object, int>? _ids;

    private ExpressionStringBuilder()
    {
        _out = new StringBuilder();
    }

    public override string ToString()
    {
        return _out.ToString();
    }

    private int GetLabelId(LabelTarget label) => GetId(label);
    private int GetParamId(ParameterExpression p) => GetId(p);

    private int GetId(object o)
    {
        _ids ??= new Dictionary<object, int>();

        int id;
        if (!_ids.TryGetValue(o, out id))
        {
            id = _ids.Count;
            _ids.Add(o, id);
        }

        return id;
    }

    #region The printing code

    private void Out(string? s)
    {
        _out.Append(s);
    }

    private void Out(char c)
    {
        _out.Append(c);
    }


    int indent = 0;


    public IDisposable Indent()
    {
        indent++;

        return new Disposable(() => { _out.Remove(_out.Length - 4, 4); indent--; });
    }
    void NewLine()
    {
        _out.AppendLine();
        for (int i = 0; i < indent; i++)
        {
            _out.Append("    ");
        }
    }

    #endregion

    #region Output an expression tree to a string

    /// <summary>
    /// Output a given expression tree to a string.
    /// </summary>
    internal static string ExpressionToString(Expression node)
    {
        Debug.Assert(node != null);
        ExpressionStringBuilder esb = new ExpressionStringBuilder();
        esb.Visit(node);
        return esb.ToString();
    }

    internal static string CatchBlockToString(CatchBlock node)
    {
        Debug.Assert(node != null);
        ExpressionStringBuilder esb = new ExpressionStringBuilder();
        esb.VisitCatchBlock(node);
        return esb.ToString();
    }

    internal static string SwitchCaseToString(SwitchCase node)
    {
        Debug.Assert(node != null);
        ExpressionStringBuilder esb = new ExpressionStringBuilder();
        esb.VisitSwitchCase(node);
        return esb.ToString();
    }

    /// <summary>
    /// Output a given member binding to a string.
    /// </summary>
    internal static string MemberBindingToString(MemberBinding node)
    {
        Debug.Assert(node != null);
        ExpressionStringBuilder esb = new ExpressionStringBuilder();
        esb.VisitMemberBinding(node);
        return esb.ToString();
    }

    /// <summary>
    /// Output a given ElementInit to a string.
    /// </summary>
    internal static string ElementInitBindingToString(ElementInit node)
    {
        Debug.Assert(node != null);
        ExpressionStringBuilder esb = new ExpressionStringBuilder();
        esb.VisitElementInit(node);
        return esb.ToString();
    }

    private void VisitExpressions<T>(char open, ReadOnlyCollection<T> expressions, char close) where T : Expression
    {
        VisitExpressions(open, expressions, close, ", ");
    }

    private void VisitExpressions<T>(char open, ReadOnlyCollection<T> expressions, char close, string separator) where T : Expression
    {
        Out(open);
        if (expressions != null)
        {
            bool isFirst = true;
            foreach (T e in expressions)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    Out(separator);
                }
                Visit(e);
            }
        }
        Out(close);
    }


    bool skipFirstParens = false;

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var sp = skipFirstParens;
        skipFirstParens = false;
        if (node.NodeType == ExpressionType.ArrayIndex)
        {
            Visit(node.Left);
            Out('[');
            Visit(node.Right);
            Out(']');
        }
        else
        {
            string op;
            switch (node.NodeType)
            {
                // AndAlso and OrElse were unintentionally changed in
                // CLR 4. We changed them to "AndAlso" and "OrElse" to
                // be 3.5 compatible, but it turns out 3.5 shipped with
                // "&&" and "||". Oops.
                case ExpressionType.AndAlso:
                    op = "AndAlso";
                    break;
                case ExpressionType.OrElse:
                    op = "OrElse";
                    break;
                case ExpressionType.Assign:
                    op = "=";
                    break;
                case ExpressionType.Equal:
                    op = "==";
                    break;
                case ExpressionType.NotEqual:
                    op = "!=";
                    break;
                case ExpressionType.GreaterThan:
                    op = ">";
                    break;
                case ExpressionType.LessThan:
                    op = "<";
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    op = ">=";
                    break;
                case ExpressionType.LessThanOrEqual:
                    op = "<=";
                    break;
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    op = "+";
                    break;
                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked:
                    op = "+=";
                    break;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    op = "-";
                    break;
                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked:
                    op = "-=";
                    break;
                case ExpressionType.Divide:
                    op = "/";
                    break;
                case ExpressionType.DivideAssign:
                    op = "/=";
                    break;
                case ExpressionType.Modulo:
                    op = "%";
                    break;
                case ExpressionType.ModuloAssign:
                    op = "%=";
                    break;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    op = "*";
                    break;
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked:
                    op = "*=";
                    break;
                case ExpressionType.LeftShift:
                    op = "<<";
                    break;
                case ExpressionType.LeftShiftAssign:
                    op = "<<=";
                    break;
                case ExpressionType.RightShift:
                    op = ">>";
                    break;
                case ExpressionType.RightShiftAssign:
                    op = ">>=";
                    break;
                case ExpressionType.And:
                    op = IsBool(node) ? "And" : "&";
                    break;
                case ExpressionType.AndAssign:
                    op = IsBool(node) ? "&&=" : "&=";
                    break;
                case ExpressionType.Or:
                    op = IsBool(node) ? "Or" : "|";
                    break;
                case ExpressionType.OrAssign:
                    op = IsBool(node) ? "||=" : "|=";
                    break;
                case ExpressionType.ExclusiveOr:
                    op = "^";
                    break;
                case ExpressionType.ExclusiveOrAssign:
                    op = "^=";
                    break;
                case ExpressionType.Power:
                    op = "**";
                    break; // This was changed in .NET Core from ^ to **
                case ExpressionType.PowerAssign:
                    op = "**=";
                    break;
                case ExpressionType.Coalesce:
                    op = "??";
                    break;
                default:
                    throw new InvalidOperationException();
            }

            if (!sp)
                Out('(');
            Visit(node.Left);
            Out(' ');
            Out(op);
            Out(' ');
            Visit(node.Right);

            if (!sp)
                Out(')');
        }
        return node;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node.IsByRef)
        {
            Out("ref ");
        }
        string? name = node.Name;
        if (string.IsNullOrEmpty(name))
        {
            Out("Param_" + GetParamId(node));
        }
        else
        {
            Out(name);
        }
        return node;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        if (node.Parameters.Count == 1)
        {
            // p => body
            Visit(node.Parameters[0]);
        }
        else
        {
            // (p1, p2, ..., pn) => body
            Out('(');
            string sep = ", ";
            for (int i = 0, n = node.Parameters.Count; i < n; i++)
            {
                if (i > 0)
                {
                    Out(sep);
                }
                Visit(node.Parameters[i]);
            }
            Out(')');
        }
        Out(" => ");
        Visit(node.Body);
        return node;
    }

    protected override Expression VisitListInit(ListInitExpression node)
    {
        Visit(node.NewExpression);
        Out(" {");
        using (Indent())
        {
            NewLine();
            for (int i = 0, n = node.Initializers.Count; i < n; i++)
            {
                VisitElementInit(node.Initializers[i]);
                Out(",");
                NewLine();
            }
        }
        Out('}');
        return node;
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        Out("IIF(");
        Visit(node.Test);
        Out(", ");
        Visit(node.IfTrue);
        Out(", ");
        Visit(node.IfFalse);
        Out(')');
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value != null)
        {
            string? sValue = node.Value.ToString();
            if (node.Value is string)
            {
                Out('\"');
                Out(sValue);
                Out('\"');
            }
            else if (sValue == node.Value.GetType().ToString())
            {
                Out("value(");
                Out(sValue);
                Out(')');
            }
            else
            {
                Out(sValue);
            }
        }
        else
        {
            Out("null");
        }
        return node;
    }

    protected override Expression VisitDebugInfo(DebugInfoExpression node)
    {
        Out($"<DebugInfo({node.Document.FileName}: {node.StartLine}, {node.StartColumn}, {node.EndLine}, {node.EndColumn})>");
        return node;
    }

    protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
    {
        VisitExpressions('(', node.Variables, ')');
        return node;
    }

    // Prints ".instanceField" or "declaringType.staticField"
    private void OutMember(Expression? instance, MemberInfo member)
    {
        if (instance != null)
        {
            Visit(instance);
        }
        else
        {
            // For static members, include the type name
            Out(member.DeclaringType!.Name);
        }

        Out('.');
        Out(member.Name);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        OutMember(node.Expression, node.Member);
        return node;
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        if (node.NewExpression.Arguments.Count == 0 &&
            node.NewExpression.Type.Name.Contains('<'))
        {
            // anonymous type constructor
            Out("new");
        }
        else
        {
            Visit(node.NewExpression);
        }
        Out(" {");
        using (Indent())
        {
            for (int i = 0, n = node.Bindings.Count; i < n; i++)
            {
                MemberBinding b = node.Bindings[i];

                VisitMemberBinding(b);
                Out(", ");
                NewLine();

            }
        }
        Out('}');
        return node;
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
    {
        Out(assignment.Member.Name);
        Out(" = ");
        Visit(assignment.Expression);
        return assignment;
    }

    protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
    {
        Out(binding.Member.Name);
        Out(" = {");
        using (Indent())
        {
            NewLine();
            for (int i = 0, n = binding.Initializers.Count; i < n; i++)
            {
                VisitElementInit(binding.Initializers[i]);
                Out(", ");
                NewLine();
            }
        }
        Out('}');
        return binding;
    }

    protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
    {
        Out(binding.Member.Name);
        Out(" = {");
        for (int i = 0, n = binding.Bindings.Count; i < n; i++)
        {
            if (i > 0)
            {
                Out(", ");
            }
            VisitMemberBinding(binding.Bindings[i]);
        }
        Out('}');
        return binding;
    }

    protected override ElementInit VisitElementInit(ElementInit initializer)
    {
        //Out(initializer.AddMethod.ToString());
        string sep = ", ";
        Out('(');
        for (int i = 0, n = initializer.Arguments.Count; i < n; i++)
        {
            if (i > 0)
            {
                Out(sep);
            }
            Visit(initializer.Arguments[i]);
        }
        Out(')');
        return initializer;
    }

    protected override Expression VisitInvocation(InvocationExpression node)
    {
        Out("Invoke(");
        Visit(node.Expression);
        string sep = ", ";
        for (int i = 0, n = node.Arguments.Count; i < n; i++)
        {
            Out(sep);
            Visit(node.Arguments[i]);
        }
        Out(')');
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        int start = 0;
        Expression? ob = node.Object;

        if (node.Method.GetCustomAttribute(typeof(ExtensionAttribute)) != null)
        {
            start = 1;
            ob = node.Arguments[0];
        }

        if (ob != null)
        {
            Visit(ob);
            if (ob.Type.IsInstantiationOf(typeof(IEnumerable<>)) ||
                ob.Type.IsInstantiationOf(typeof(IQueryable<>)))
                NewLine();
            Out('.');
        }
        Out(node.Method.Name);
        Out('(');
        for (int i = start, n = node.Arguments.Count; i < n; i++)
        {
            if (i > start)
                Out(", ");
            Visit(node.Arguments[i]);
        }
        Out(')');
        return node;
    }

    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.NewArrayBounds:
                // new MyType[](expr1, expr2)
                Out("new ");
                Out(node.Type.ToString());
                VisitExpressions('(', node.Expressions, ')');
                break;
            case ExpressionType.NewArrayInit:
                // new [] {expr1, expr2}
                Out("new [] ");
                VisitExpressions('{', node.Expressions, '}');
                break;
        }
        return node;
    }

    protected override Expression VisitNew(NewExpression node)
    {
        Out("new ");
        Out(node.Type.Name);
        Out('(');

        bool indent = node.Members != null || TupleReflection.IsTuple(node.Type);

        ReadOnlyCollection<MemberInfo>? members = node.Members;
        using (indent ? this.Indent() : null)
        {
            if (indent)
                this.NewLine();
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                if (members != null)
                {
                    string name = members[i].Name;
                    Out(name);
                    Out(" = ");
                }
                Visit(node.Arguments[i]);

                if (i < node.Arguments.Count - 1)
                {
                    Out(", ");
                }

                if (indent)
                    this.NewLine();
            }
        }
        Out(')');
        return node;
    }

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        Out('(');
        Visit(node.Expression);
        switch (node.NodeType)
        {
            case ExpressionType.TypeIs:
                Out(" Is ");
                break;
            case ExpressionType.TypeEqual:
                Out(" TypeEqual ");
                break;
        }
        Out(node.TypeOperand.Name);
        Out(')');
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked: Out('-'); break;
            case ExpressionType.Not: Out("Not("); break;
            case ExpressionType.IsFalse: Out("IsFalse("); break;
            case ExpressionType.IsTrue: Out("IsTrue("); break;
            case ExpressionType.OnesComplement: Out("~("); break;
            case ExpressionType.ArrayLength: Out("ArrayLength("); break;
            case ExpressionType.Convert: Out("Convert("); break;
            case ExpressionType.ConvertChecked: Out("ConvertChecked("); break;
            case ExpressionType.Throw: Out("throw("); break;
            case ExpressionType.TypeAs: Out('('); break;
            case ExpressionType.UnaryPlus: Out('+'); break;
            case ExpressionType.Unbox: Out("Unbox("); break;
            case ExpressionType.Increment: Out("Increment("); break;
            case ExpressionType.Decrement: Out("Decrement("); break;
            case ExpressionType.PreIncrementAssign: Out("++"); break;
            case ExpressionType.PreDecrementAssign: Out("--"); break;
            case ExpressionType.Quote:
            case ExpressionType.PostIncrementAssign:
            case ExpressionType.PostDecrementAssign:
                break;
            default:
                throw new InvalidOperationException();
        }

        Visit(node.Operand);

        switch (node.NodeType)
        {
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
            case ExpressionType.UnaryPlus:
            case ExpressionType.PreDecrementAssign:
            case ExpressionType.PreIncrementAssign:
            case ExpressionType.Quote:
                break;
            case ExpressionType.TypeAs:
                Out(" As ");
                Out(node.Type.Name);
                Out(')'); break;
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                Out(", ");
                Out(node.Type.Name);
                Out(')'); break; // These were changed in .NET Core to add the type name
            case ExpressionType.PostIncrementAssign: Out("++"); break;
            case ExpressionType.PostDecrementAssign: Out("--"); break;
            default: Out(')'); break;
        }
        return node;
    }

    protected override Expression VisitBlock(BlockExpression node)
    {
        Out('{');
        using (Indent())
        {
            NewLine();
            foreach (ParameterExpression v in node.Variables)
            {
                Out("var ");
                Visit(v);
                Out(';');
                NewLine();
            }

            foreach (var e in node.Expressions)
            {
                skipFirstParens = true;
                Visit(e);
                Out(';');
                NewLine();
            }

            skipFirstParens = false;
        }
        Out("}");
        return node;
    }

    protected override Expression VisitDefault(DefaultExpression node)
    {
        Out("default(");
        Out(node.Type.Name);
        Out(')');
        return node;
    }

    protected override Expression VisitLabel(LabelExpression node)
    {
        Out("{ ... } ");
        DumpLabel(node.Target);
        Out(':');
        return node;
    }

    protected override Expression VisitGoto(GotoExpression node)
    {
        string op = node.Kind switch
        {
            GotoExpressionKind.Goto => "goto",
            GotoExpressionKind.Break => "break",
            GotoExpressionKind.Continue => "continue",
            GotoExpressionKind.Return => "return",
            _ => throw new InvalidOperationException(),
        };
        Out(op);
        Out(' ');
        DumpLabel(node.Target);
        if (node.Value != null)
        {
            Out(" (");
            Visit(node.Value);
            Out(")");
        }
        return node;
    }

    protected override Expression VisitLoop(LoopExpression node)
    {
        Out("loop { ... }");
        return node;
    }

    protected override SwitchCase VisitSwitchCase(SwitchCase node)
    {
        Out("case ");
        VisitExpressions('(', node.TestValues, ')');
        Out(": ...");
        return node;
    }

    protected override Expression VisitSwitch(SwitchExpression node)
    {
        Out("switch ");
        Out('(');
        Visit(node.SwitchValue);
        Out(") { ... }");
        return node;
    }

    protected override CatchBlock VisitCatchBlock(CatchBlock node)
    {
        Out("catch (");
        Out(node.Test.Name);
        if (!string.IsNullOrEmpty(node.Variable?.Name))
        {
            Out(' ');
            Out(node.Variable.Name);
        }
        Out(") { ... }");
        return node;
    }

    protected override Expression VisitTry(TryExpression node)
    {
        Out("try { ... }");
        return node;
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        if (node.Object != null)
        {
            Visit(node.Object);
        }
        else
        {
            Debug.Assert(node.Indexer != null);
            Out(node.Indexer.DeclaringType!.Name);
        }
        if (node.Indexer != null)
        {
            Out('.');
            Out(node.Indexer.Name);
        }

        Out('[');
        for (int i = 0, n = node.Arguments.Count; i < n; i++)
        {
            if (i > 0)
                Out(", ");
            Visit(node.Arguments[i]);
        }
        Out(']');

        return node;
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
        Justification = "The 'ToString' method cannot be trimmed on any Expression type because we are calling Expression.ToString() in this method.")]
    protected override Expression VisitExtension(Expression node)
    {
        // Prefer an overridden ToString, if available.
        MethodInfo toString = node.GetType().GetMethod("ToString", Type.EmptyTypes)!;
        if (toString.DeclaringType != typeof(Expression) && !toString.IsStatic)
        {
            Out(node.ToString());
            return node;
        }

        Out('[');
        // For 3.5 subclasses, print the NodeType.
        // For Extension nodes, print the class name.
        Out(node.NodeType == ExpressionType.Extension ? node.GetType().FullName : node.NodeType.ToString());
        Out(']');
        return node;
    }

    private void DumpLabel(LabelTarget target)
    {
        if (!string.IsNullOrEmpty(target.Name))
        {
            Out(target.Name);
        }
        else
        {
            int labelId = GetLabelId(target);
            Out("UnnamedLabel_" + labelId);
        }
    }

    private static bool IsBool(Expression node)
    {
        return node.Type == typeof(bool) || node.Type == typeof(bool?);
    }

    #endregion
}
