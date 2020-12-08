using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Signum.Engine.Maps
{
    public class TableIndex
    {
        public ITable Table { get; private set; }
        public IColumn[] Columns { get; private set; }
        public IColumn[]? IncludeColumns { get; set; }

        public string? Where { get; set; }

        public static IColumn[] GetColumnsFromFields(params Field[] fields)
        {
            if (fields == null || fields.IsEmpty())
                throw new InvalidOperationException("No fields");

            if (fields.Any(f => f is FieldEmbedded || f is FieldMixin))
                throw new InvalidOperationException("Embedded fields not supported for indexes");

            return fields.SelectMany(f => f.Columns()).ToArray();
        }

        public TableIndex(ITable table, params IColumn[] columns)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            if (columns == null || columns.IsEmpty())
                throw new ArgumentNullException(nameof(columns));

            this.Table = table;
            this.Columns = columns;
        }

        public virtual string GetIndexName(ObjectName tableName)
        {
            int maxLength = MaxNameLength();

            return StringHashEncoder.ChopHash("IX_{0}_{1}".FormatWith(tableName.Name, ColumnSignature()), maxLength) + WhereSignature();
        }

        protected static int MaxNameLength()
        {
            return Connector.Current.MaxNameLength - StringHashEncoder.HashSize - 2;
        }

        public string IndexName => GetIndexName(Table.Name);

        protected string ColumnSignature()
        {
            return Columns.ToString(c => c.Name, "_");
        }

        public string? WhereSignature()
        {
            var includeColumns = IncludeColumns.HasItems() ? IncludeColumns.ToString(c => c.Name, "_") : null;

            if (string.IsNullOrEmpty(Where) && includeColumns == null)
                return null;

            return "__" + StringHashEncoder.Codify(Where + includeColumns);
        }

        public override string ToString()
        {
            return IndexName;
        }

        public string HintText()
        {
            return $"INDEX([{this.IndexName}])";
        }
    }

    public class PrimaryKeyIndex : TableIndex
    {
        public PrimaryKeyIndex(ITable table) : base(table, new[] { table.PrimaryKey })
        {

        }

        public override string GetIndexName(ObjectName tableName) => GetPrimaryKeyName(tableName);

        public static string GetPrimaryKeyName(ObjectName tableName)
        {
            return "PK_" + tableName.Schema.Name + "_" + tableName.Name;
        }
    }

    public class UniqueTableIndex : TableIndex
    {
        public UniqueTableIndex(ITable table, IColumn[] columns) 
            : base(table, columns) 
        { 
        }


        public override string GetIndexName(ObjectName tableName)
        {
            var maxSize = MaxNameLength();

            return StringHashEncoder.ChopHash("UIX_{0}_{1}".FormatWith(tableName.Name, ColumnSignature()), maxSize) + WhereSignature();
        }

        public string? ViewName
        {
            get
            {
                if (!Where.HasText())
                    return null;

                if (Connector.Current.AllowsIndexWithWhere(Where))
                    return null;

                var maxSize = MaxNameLength();

                return StringHashEncoder.ChopHash("VIX_{0}_{1}".FormatWith(Table.Name.Name, ColumnSignature()), maxSize) + WhereSignature();
            }
        }

        public bool AvoidAttachToUniqueIndexes { get; set; }
    }

    public class IndexKeyColumns
    {

        public static IColumn[] Split(IFieldFinder finder, LambdaExpression columns)
        {
            if (columns == null)
                throw new ArgumentNullException(nameof(columns));

            if (columns.Body.NodeType == ExpressionType.New)
            {
                var resultColumns = (from a in ((NewExpression)columns.Body).Arguments
                    from c in GetColumns(finder, Expression.Lambda(Expression.Convert(a, typeof(object)), columns.Parameters))
                    select c);
                
                return resultColumns.ToArray();
            }
            
            return GetColumns(finder, columns);
        }

        static string[] ignoreMembers = new string[] { "ToLite", "ToLiteFat" };

        static IColumn[] GetColumns(IFieldFinder finder, LambdaExpression field)
        {
            var body = field.Body;

            Type? type = RemoveCasting(ref body);

            body = IndexWhereExpressionVisitor.RemoveLiteEntity(body);

            var members = Reflector.GetMemberListBase(body);
            if (members.Any(a => ignoreMembers.Contains(a.Name)))
                members = members.Where(a => !ignoreMembers.Contains(a.Name)).ToArray();

            Field f = Schema.FindField(finder, members);

            if (type != null)
            {
                var ib = f as FieldImplementedBy;
                if (ib == null)
                    throw new InvalidOperationException("Casting only supported for {0}".FormatWith(typeof(FieldImplementedBy).Name));

                return (from ic in ib.ImplementationColumns
                        where type.IsAssignableFrom(ic.Key)
                        select (IColumn)ic.Value).ToArray();
            }

            return TableIndex.GetColumnsFromFields(f);
        }

        static Type? RemoveCasting(ref Expression body)
        {
            if (body.NodeType == ExpressionType.Convert && body.Type == typeof(object))
                body = ((UnaryExpression)body).Operand;

            Type? type = null;
            if ((body.NodeType == ExpressionType.Convert || body.NodeType == ExpressionType.TypeAs) &&
                body.Type != typeof(object))
            {
                type = body.Type;
                body = ((UnaryExpression)body).Operand;
            }
            
            return type;
        }
    }

    public class IndexWhereExpressionVisitor : ExpressionVisitor
    {
        StringBuilder sb = new StringBuilder();

        IFieldFinder RootFinder;
        bool isPostgres;

        public IndexWhereExpressionVisitor(IFieldFinder rootFinder)
        {
            RootFinder = rootFinder;
            this.isPostgres = Schema.Current.Settings.IsPostgres;
        }

        public static string GetIndexWhere(LambdaExpression lambda, IFieldFinder rootFiender)
        {
            IndexWhereExpressionVisitor visitor = new IndexWhereExpressionVisitor(rootFiender);

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


        [return: NotNullIfNotNull("exp")]
        public override Expression? Visit(Expression? exp)
        {
            switch (exp!.NodeType)
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
            var exp = RemoveLiteEntity(b.Expression);

            var f = GetField(exp);

            if (f is FieldReference fr)
            {
                if (b.TypeOperand.IsAssignableFrom(fr.FieldType))
                    sb.Append(fr.Name.SqlEscape(isPostgres) + " IS NOT NULL");
                else
                    throw new InvalidOperationException("A {0} will never be {1}".FormatWith(fr.FieldType.TypeName(), b.TypeOperand.TypeName()));

                return b;
            }

            if (f is FieldImplementedBy fib)
            {
                var typeOperant = b.TypeOperand.CleanType();

                var imp = fib.ImplementationColumns.Where(kvp => typeOperant.IsAssignableFrom(kvp.Key));

                if (imp.Any())
                    sb.Append(imp.ToString(kvp => kvp.Value.Name.SqlEscape(isPostgres) + " IS NOT NULL", " OR "));
                else
                    throw new InvalidOperationException("No implementation ({0}) will never be {1}".FormatWith(fib.ImplementationColumns.Keys.ToString(t => t.TypeName(), ", "), b.TypeOperand.TypeName()));

                return b;
            }

            throw new NotSupportedException("'is' only works with ImplementedBy or Reference fields");
        }

        public static Expression RemoveLiteEntity(Expression exp)
        {
            if (exp is MemberExpression m && m.Member is PropertyInfo pi && m.Expression!.Type.IsInstantiationOf(typeof(Lite<>)) &&
                (pi.Name == nameof(Lite<Entity>.Entity) || pi.Name == nameof(Lite<Entity>.EntityOrNull)))
                return m.Expression;
            return exp;
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            var field = GetField(m);

            sb.Append(Equals(field, value: true, equals: true, isPostgres));

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


        public static string IsNull(Field field, bool equals, bool isPostgres)
        {
            string isNull = equals ? "{0} IS NULL" : "{0} IS NOT NULL";

            if (field is IColumn col)
            {
                string result = isNull.FormatWith(col.Name.SqlEscape(isPostgres));

                if (!col.DbType.IsString())
                    return result;

                return result + (equals ? " OR " : " AND ") + (col.Name.SqlEscape(isPostgres) + (equals ? " = " : " <> ") + "''");

            }
            else if (field is FieldImplementedBy ib)
            {
                return ib.ImplementationColumns.Values.Select(ic => isNull.FormatWith(ic.Name.SqlEscape(isPostgres))).ToString(equals ? " AND " : " OR ");
            }
            else if (field is FieldImplementedByAll iba)
            {
                return isNull.FormatWith(iba.Column.Name.SqlEscape(isPostgres)) +
                    (equals ? " AND " : " OR ") +
                    isNull.FormatWith(iba.ColumnType.Name.SqlEscape(isPostgres));
            }
            else if (field is FieldEmbedded fe)
            {
                if (fe.HasValue == null)
                    throw new NotSupportedException("{0} is not nullable".FormatWith(field));

                return fe.HasValue.Name.SqlEscape(isPostgres) + " = 1";
            }

            throw new NotSupportedException(isNull.FormatWith(field.GetType()));
        }

        static string Equals(Field field, object value, bool equals, bool isPostgres)
        {
            if (value == null)
            {
                return IsNull(field, equals, isPostgres);
            }
            else
            {
                if (field is IColumn)
                {
                    return ((IColumn)field).Name.SqlEscape(isPostgres) +
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

                    sb.Append(Equals(field, ((ConstantExpression)b.Left).Value!, b.NodeType == ExpressionType.Equal, isPostgres));
                }
                else if (b.Right is ConstantExpression)
                {
                    Field field = GetField(b.Left);

                    sb.Append(Equals(field, ((ConstantExpression)b.Right).Value!, b.NodeType == ExpressionType.Equal, isPostgres));
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
