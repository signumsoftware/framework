using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Linq
{
    internal sealed class ProjectedColumns
    {
        Expression projector;
        ReadOnlyCollection<ColumnDeclaration> columns;
        internal ProjectedColumns(Expression projector, ReadOnlyCollection<ColumnDeclaration> columns)
        {
            this.projector = projector;
            this.columns = columns;
        }
        internal Expression Projector
        {
            get { return this.projector; }
        }
        internal ReadOnlyCollection<ColumnDeclaration> Columns
        {
            get { return this.columns; }
        }
    }

    /// <summary>
    /// ColumnProjection is a visitor that splits an expression representing the result of a query into 
    /// two parts, a list of column declarations of expressions that must be evaluated on the server
    /// and a projector expression that describes how to combine the columns back into the result object
    /// </summary>
    internal class ColumnProjector : DbExpressionVisitor
    {
        Dictionary<ColumnExpression, ColumnExpression> map = new Dictionary<ColumnExpression, ColumnExpression>();
        List<ColumnDeclaration> columns = new List<ColumnDeclaration>();
        HashSet<Expression> candidates;
        string[] knownAliases;
        string newAlias;
        int iColumn;

        private ColumnProjector() { }

        static internal ColumnExpression SingleProjection(ColumnDeclaration declaration, string newAlias, Type columnType)
        {
            return new ColumnExpression(columnType, newAlias, declaration.Name);
        }

        static internal ProjectedColumns ProjectColumns(Expression projector, string newAlias, params string[] knownAliases)
        {
            Expression newProj;
            ColumnProjector cp = new ColumnProjector
            {
                newAlias = newAlias,
                knownAliases = knownAliases,
                candidates = DbExpressionNominator.Nominate(projector, knownAliases, out newProj)
            };

            Expression e = cp.Visit(newProj);

            return new ProjectedColumns(e, cp.columns.AsReadOnly());
        }

        protected override Expression Visit(Expression expression)
        {
            if (this.candidates.Contains(expression))
            {
                if (expression.NodeType == (ExpressionType)DbExpressionType.Column)
                {
                    ColumnExpression column = (ColumnExpression)expression;
                    ColumnExpression mapped;
                    if (this.map.TryGetValue(column, out mapped))
                    {
                        return mapped;
                    }
                    if (this.knownAliases.Contains(column.Alias))
                    {
                        int ordinal = this.columns.Count;
                        string columnName = this.GetUniqueColumnName(column.Name);
                        this.columns.Add(new ColumnDeclaration(columnName, column));
                        mapped = new ColumnExpression(column.Type, this.newAlias, columnName);
                        this.map[column] = mapped;
                        return mapped;
                    }
                    // must be referring to outer scope
                    return column;
                }
                else
                {
                    string columnName = this.GetNextColumnName();
                    int ordinal = this.columns.Count;
                    if (ConditionsRewriter.IsBooleanExpression(expression))
                        expression = ConditionsRewriter.MakeSqlValue(expression);
                    this.columns.Add(new ColumnDeclaration(columnName, expression));
                    return new ColumnExpression(expression.Type, this.newAlias, columnName);
                }
            }
            else
            {
                return base.Visit(expression);
            }
        }

        private string GetUniqueColumnName(string name)
        {
            string baseName = name;
            int suffix = 1;
            while (this.columns.Select(c => c.Name).Contains(name))
                name = baseName + (suffix++);
            return name;
        }

        private string GetNextColumnName()
        {
            return this.GetUniqueColumnName("c" + (iColumn++));
        }
    }
}

