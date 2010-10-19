using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Signum.Entities;
using Signum.Engine;
using Signum.Utilities;
using System.Data;
using Signum.Utilities.Reflection;
using Signum.Engine.Properties;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Maps
{
    public partial class Table
    {
        internal SqlPreCommand SelectAllIDs()
        {
            return SqlBuilder.SelectAll(Name, new[] { SqlBuilder.PrimaryKeyName });
        }

        internal SqlPreCommand BatchSelect(int[] ids)
        {
            return SqlBuilder.SelectByIds(Name, this.Columns.Values.Select(a => a.Name).ToArray(), SqlBuilder.PrimaryKeyName, ids);
        }

        internal SqlPreCommand BatchSelectLite(int[] ids)
        {
            return SqlBuilder.SelectByIds(Name, new[] { SqlBuilder.PrimaryKeyName, SqlBuilder.ToStrName }, SqlBuilder.PrimaryKeyName, ids); 
        }

        internal Action<IdentifiableEntity, FieldReader, Retriever> Fill;
        internal Expression<Action<IdentifiableEntity, FieldReader, Retriever>> FillExpression; 

        void CompleteRetrieve()
        {
            ParameterExpression ident = Expression.Parameter(typeof(IdentifiableEntity), "ident"); 
            ParameterExpression reader = Expression.Parameter(typeof(FieldReader), "reader"); 
            ParameterExpression retriever = Expression.Parameter(typeof(Retriever), "retriever"); 
            
            ParameterExpression entity =  Expression.Variable(this.Type, "entity");

            List<Expression> assigments = Fields.Values.Select(f =>
                (Expression)Expression.Assign(Expression.Field(entity, f.FieldInfo), f.Field.GenerateValue(reader, retriever))).ToList();

            assigments.Insert(0, Expression.Assign(entity, Expression.Convert(ident, Type)));

            FillExpression = Expression.Lambda<Action<IdentifiableEntity, FieldReader, Retriever>>(
                Expression.Block(new[] { entity }, assigments), ident, reader, retriever);
            Fill = FillExpression.Compile(); 
        }
    }

    public partial class RelationalTable
    {
        internal SqlPreCommand BatchSelect(int[] ids)
        {
            return SqlBuilder.SelectByIds(Name, this.Columns.Values.Select(a => a.Name).ToArray(), BackReference.Name, ids);
        }

        internal Action<IList, FieldReader, Retriever> AddInList;
        internal Expression<Action<IList, FieldReader, Retriever>> AddInListExpression;

        void CompleteRetrieve()
        {
            ParameterExpression reader = Expression.Parameter(typeof(FieldReader), "reader");
            ParameterExpression retriever = Expression.Parameter(typeof(Retriever), "retriever");
            ParameterExpression ilist = Expression.Parameter(typeof(IList), "ilist");

            MethodInfo miAdd = CollectionType.GetMethod("Add", new[]{ Field.FieldType });

            AddInListExpression = Expression.Lambda<Action<IList, FieldReader, Retriever>>(
                    Expression.Call(Expression.Convert(ilist, CollectionType), miAdd, Field.GenerateValue(reader, retriever))
                    , ilist, reader, retriever);

            AddInList = AddInListExpression.Compile();
        }
    }

    public abstract partial class Field
    {
        internal abstract Expression GenerateValue(ParameterExpression reader, ParameterExpression retriever);
    }

    public partial class FieldPrimaryKey
    {
        internal override Expression GenerateValue(ParameterExpression reader, ParameterExpression retriever)
        {
            return Expression.Convert(FieldReader.GetExpression(reader, this.Position, typeof(int)), typeof(int?));
        }
    }

    public partial class FieldValue
    {
        internal override Expression GenerateValue(ParameterExpression reader, ParameterExpression retriever)
        {
            return FieldReader.GetExpression(reader, this.Position, FieldType);
        }
    }

    public static partial class ReferenceFieldExtensions
    {
        static MethodInfo miGetLite = ReflectionTools.GetMethodInfo((Retriever r)=>r.GetLite(null, null, 0));
        static  MethodInfo miIdentifiable = ReflectionTools.GetMethodInfo((Retriever r) => r.GetIdentifiable(null, 0, false));

        public static Expression GetIdentifiableOrLite(this IFieldReference field,  ParameterExpression retriever, Expression referenceTable, Expression id)
        {
            Expression result = field.IsLite ?
                Expression.Call(retriever, miGetLite, referenceTable, Expression.Constant(Reflector.ExtractLite(field.FieldType)), id) :
                Expression.Call(retriever, miIdentifiable, referenceTable, id, Expression.Constant(false));

            return Expression.Convert(result, field.FieldType); 
        }
    }

    public partial class FieldReference
    {
        internal override Expression GenerateValue(ParameterExpression reader, ParameterExpression retriever)
        {
            Expression result = this.GetIdentifiableOrLite(retriever, Expression.Constant(ReferenceTable),
                FieldReader.GetExpression(reader, this.Position, typeof(int)));

            if (!Nullable)
                return result;

            return Expression.Condition(FieldReader.GetIsNull(reader, this.Position), 
                Expression.Constant(null, FieldType), 
                result); 
        }
    }

    public partial class FieldEnum
    {
        internal override Expression GenerateValue(ParameterExpression reader, ParameterExpression retriever)
        {
            return Expression.Convert(
                FieldReader.GetExpression(reader, this.Position, Nullable? typeof(int?): typeof(int)), 
                FieldType);
        }     
    }

    public partial class FieldMList
    {
        static MethodInfo miGetList = ReflectionTools.GetMethodInfo((Retriever r) => r.GetList(null,  0));

        internal override Expression GenerateValue(ParameterExpression reader, ParameterExpression retriever)
        {
            return Expression.Convert(
                Expression.Call(retriever, miGetList, 
                    Expression.Constant(RelationalTable),
                    FieldReader.GetExpression(reader, 0, typeof(int))), FieldType);
        }
    }

    public partial class FieldEmbedded
    {
        internal override Expression GenerateValue(ParameterExpression reader, ParameterExpression retriever)
        {
            ParameterExpression entity = Expression.Variable(this.FieldType, "entity");

            List<Expression> assigments = EmbeddedFields.Values.Select(f =>
                (Expression)Expression.Assign(Expression.Field(entity, f.FieldInfo), f.Field.GenerateValue(reader, retriever))).ToList();

            assigments.Insert(0, Expression.Assign(entity, Expression.New(this.FieldType)));
            assigments.Add(entity);

            Expression result = Expression.Block(new[] { entity }, assigments);

            if (HasValue == null)
                return result;

            return Expression.Condition(FieldReader.GetExpression(reader, HasValue.Position, typeof(bool)), 
                result,
                Expression.Constant(null, FieldType)); 
        }
    }

    public partial class FieldImplementedBy
    {
        internal override Expression GenerateValue(ParameterExpression reader, ParameterExpression retriever)
        {
            var result = ImplementationColumns.Values.Reverse().Aggregate(
                (Expression)Expression.Constant(null, FieldType), (acum, imp) =>
                Expression.Condition(
                    FieldReader.GetIsNull(reader, imp.Position), 
                    acum,
                    this.GetIdentifiableOrLite(retriever, Expression.Constant(imp.ReferenceTable),
                        FieldReader.GetExpression(reader, imp.Position, typeof(int)))));


            return result;
        }
    }

    public partial class FieldImplementedByAll
    {
        static Expression<Func<int, Table>> getTable = i=> Schema.Current.TablesForID[i];

        internal override Expression GenerateValue(ParameterExpression reader, ParameterExpression retriever)
        {
            var result = this.GetIdentifiableOrLite(retriever,
                ExpressionReplacer.Replace(Expression.Invoke(getTable, FieldReader.GetExpression(reader, ColumnTypes.Position, typeof(int)))),
                FieldReader.GetExpression(reader, Column.Position, typeof(int)));

            if (!Column.Nullable)
                return result;

            return Expression.Condition(FieldReader.GetIsNull(reader, Column.Position), 
                Expression.Constant(null, FieldType), 
                result); 
        }
    }

    public static class CleanUtil
    {
        public static object Cell(this DataRow dt, string campo)
        {
            if(dt.IsNull(campo))
                return null;
            return dt[campo]; 
        }
    }
}
