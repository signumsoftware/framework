using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Data;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Linq;

namespace Signum.Engine.Maps
{
    public class Index
    {
        public ITable Table { get; private set; }
        public IColumn[] Columns { get; private set; }

        public static IColumn[] GetColumnsFromFields(params Field[] fields)
        {
            if (fields == null || fields.IsEmpty())
                throw new InvalidOperationException("No fields");

            if (fields.Any(f => f is FieldEmbedded || f is FieldMixin))
                throw new InvalidOperationException("Embedded fields not supported for indexes");

            return fields.SelectMany(f => f.Columns()).ToArray();
        }

        public Index(ITable table, params IColumn[] columns)
        {
            if (table == null)
                throw new ArgumentNullException("table");

            if (columns == null || columns.IsEmpty())
                throw new ArgumentNullException("columns");

            this.Table = table;
            this.Columns = columns;
        }

        public virtual string IndexName
        {
            get { return "IX_{0}".FormatWith(ColumnSignature()).TryStart(Connector.Current.MaxNameLength); }
        }

        protected virtual string ColumnSignature()
        {
            return Columns.ToString(c => c.Name, "_");
        }
    }

    public class UniqueIndex : Index
    {
        public UniqueIndex(ITable table, IColumn[] columns) : base(table, columns) { }

        public string Where { get; set; }


        public override string IndexName
        {
            get { return "UIX_{0}".FormatWith(ColumnSignature()).TryStart(Connector.Current.MaxNameLength); }
        }

        public string ViewName
        {
            get
            {
                if (string.IsNullOrEmpty(Where))
                    return null;

                if (Connector.Current.AllowsIndexWithWhere(Where))
                    return null;

                return "VIX_{0}_{1}".FormatWith(Table.Name.Name, ColumnSignature()).TryStart(Connector.Current.MaxNameLength);
            }
        }

       

        protected override string ColumnSignature()
        {
            string columns = base.ColumnSignature();
            if (string.IsNullOrEmpty(Where))
                return columns;

            return columns + "__" + StringHashEncoder.Codify(Where);
        }

        static bool IsComplexIB(Field field)
        {
            return field is FieldImplementedBy && ((FieldImplementedBy)field).ImplementationColumns.Count > 1;
        }

        public override string ToString()
        {
            return IndexName;
        }

        public bool AvoidAttachToUniqueIndexes { get; set; }
    }

    class IndexWhereExpressionVisitor : ExpressionVisitor
    {
        StringBuilder sb = new StringBuilder();

        IFieldFinder RootFinder;

        public static string GetIndexWhere(LambdaExpression lambda, IFieldFinder rootFiender)
        {
            IndexWhereExpressionVisitor visitor = new IndexWhereExpressionVisitor
            {
                RootFinder = rootFiender
            };

            var newLambda = (LambdaExpression)ExpressionEvaluator.PartialEval(lambda);

            visitor.Visit(newLambda.Body);

            return visitor.sb.ToString();
        }

        public Field GetField(Expression exp)
        {
            if (exp.NodeType == ExpressionType.Convert)
                exp = ((UnaryExpression)exp).Operand;

            return Schema.FindField(RootFinder, Reflector.GetMemberListBase(exp));
        }


        public override Expression Visit(Expression exp)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.Conditional:
                case ExpressionType.Constant:
                case ExpressionType.Parameter:
                case ExpressionType.Call:
                case ExpressionType.Lambda:
                case ExpressionType.New:
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                case ExpressionType.Invoke:
                case ExpressionType.MemberInit:
                case ExpressionType.ListInit:
                    throw new NotSupportedException("Expression of type {0} not supported: {1}".FormatWith(exp.NodeType, exp.ToString()));
                default:
                    return base.Visit(exp);
            }
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression b)
        {
            var f = GetField(b.Expression);

            FieldReference fr = f as FieldReference;
            if (fr != null)
            {
                if (b.TypeOperand.IsAssignableFrom(fr.FieldType))
                {
                    sb.Append(fr.Name.SqlEscape() + " IS NOT NULL");
                }
                else
                    throw new InvalidOperationException("A {0} will never be {1}".FormatWith(fr.FieldType.TypeName(), b.TypeOperand.TypeName()));

                return b;
            }

            FieldImplementedBy fib = f as FieldImplementedBy;
            if (fib != null)
            {
                var imp = fib.ImplementationColumns.Where(kvp => b.TypeOperand.IsAssignableFrom(kvp.Key));

                if (imp.Any())
                    sb.Append(imp.ToString(kvp => kvp.Value.Name.SqlEscape() + " IS NOT NULL", " OR "));
                else
                    throw new InvalidOperationException("No implementation ({0}) will never be {1}".FormatWith(fib.ImplementationColumns.Keys.ToString(t=>t.TypeName(), ", "), b.TypeOperand.TypeName()));

                return b;
            }

            throw new NotSupportedException("'is' only works with ImplementedBy or Reference fields");
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            var field = GetField(m);

            sb.Append(Equals(field, true, true));

            return m;
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    sb.Append(" NOT ");
                    this.Visit(u.Operand);
                    break;
                case ExpressionType.Negate:
                    sb.Append(" - ");
                    this.Visit(u.Operand);
                    break;
                case ExpressionType.UnaryPlus:
                    sb.Append(" + ");
                    this.Visit(u.Operand);
                    break;
                case ExpressionType.Convert:
                    //Las unicas conversiones explicitas son a Binary y desde datetime a numeros
                    this.Visit(u.Operand);
                    break;
                default:
                    throw new NotSupportedException(string.Format("The unary perator {0} is not supported", u.NodeType));
            }
            return u;
        }

        static bool IsString(SqlDbType sqlDbType)
        {
            switch (sqlDbType)
            {
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.Text:
                case SqlDbType.VarChar:
                    return true;
            }

            return false;
        }

        public static string IsNull(Field field, bool equals)
        {
            string isNull = equals ? "{0} IS NULL" : "{0} IS NOT NULL";

            if (field is IColumn)
            {
                var col = ((IColumn)field);

                string result = isNull.FormatWith(col.Name.SqlEscape());

                if (!IsString(col.SqlDbType))
                    return result;

                return result + (equals ? " OR " : " AND ") + (col.Name.SqlEscape() + (equals ? " == " : " <> ") + "''");

            }
            else if (field is FieldImplementedBy)
            {
                var ib = (FieldImplementedBy)field;

                return ib.ImplementationColumns.Values.Select(ic => isNull.FormatWith(ic.Name.SqlEscape())).ToString(equals ? " AND " : " OR ");
            }
            else if (field is FieldImplementedByAll)
            {
                var iba = (FieldImplementedByAll)field;

                return isNull.FormatWith(iba.Column.Name.SqlEscape()) +
                    (equals ? " AND " : " OR ") +
                    isNull.FormatWith(iba.ColumnType.Name.SqlEscape());
            }
            else if (field is FieldEmbedded)
            {
                var fe = (FieldEmbedded)field;

                if (fe.HasValue == null)
                    throw new NotSupportedException("{0} is not nullable".FormatWith(field));

                return fe.HasValue.Name.SqlEscape() + " = TRUE";
            }

            throw new NotSupportedException(isNull.FormatWith(field.GetType())); 
        }

        static string Equals(Field field, object value, bool equals)
        {
            if (value == null)
            {
                return IsNull(field, equals);
            }
            else
            {
                if (field is IColumn)
                {
                    return ((IColumn)field).Name.SqlEscape() +
                        (equals ? " = " : " <> ") + SqlPreCommandSimple.Encode(value);
                }

                throw new NotSupportedException("Impossible to compare {0} to {1}".FormatWith(field, value));
            }
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            if (b.NodeType == ExpressionType.Coalesce)
            {
                sb.Append("IsNull(");
                Visit(b.Left);
                sb.Append(",");
                Visit(b.Right);
                sb.Append(")");
            }
            else if (b.NodeType == ExpressionType.Equal || b.NodeType == ExpressionType.NotEqual)
            {
                if (b.Left is ConstantExpression)
                {
                    if (b.Right is ConstantExpression)
                        throw new NotSupportedException("NULL == NULL not supported");

                    Field field = GetField(b.Right);

                    sb.Append(Equals(field, ((ConstantExpression)b.Left).Value, b.NodeType == ExpressionType.Equal));
                }
                else if (b.Right is ConstantExpression)
                {
                    Field field = GetField(b.Left);

                    sb.Append(Equals(field, ((ConstantExpression)b.Right).Value, b.NodeType == ExpressionType.Equal));
                }
                else
                    throw new NotSupportedException("Impossible to translate {0}".FormatWith(b.ToString()));
            }
            else
            {
                sb.Append("(");
                this.Visit(b.Left);
                switch (b.NodeType)
                {
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                        sb.Append(b.Type.UnNullify() == typeof(bool) ? " AND " : " & ");
                        break;
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                        sb.Append(b.Type.UnNullify() == typeof(bool) ? " OR " : " | ");
                        break;
                    case ExpressionType.ExclusiveOr:
                        sb.Append(" ^ ");
                        break;
                    case ExpressionType.LessThan:
                        sb.Append(" < ");
                        break;
                    case ExpressionType.LessThanOrEqual:
                        sb.Append(" <= ");
                        break;
                    case ExpressionType.GreaterThan:
                        sb.Append(" > ");
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        sb.Append(" >= ");
                        break;

                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                        sb.Append(" + ");
                        break;
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                        sb.Append(" - ");
                        break;
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                        sb.Append(" * ");
                        break;
                    case ExpressionType.Divide:
                        sb.Append(" / ");
                        break;
                    case ExpressionType.Modulo:
                        sb.Append(" % ");
                        break;
                    default:
                        throw new NotSupportedException(string.Format("The binary operator {0} is not supported", b.NodeType));
                }
                this.Visit(b.Right);
                sb.Append(")");
            }
            return b;
        }
    }
}
