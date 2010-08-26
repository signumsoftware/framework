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
        public readonly Expression Projector;
        public readonly ReadOnlyCollection<ColumnDeclaration> Columns;
        public readonly ProjectionToken Token;

        internal ProjectedColumns(Expression projector, ReadOnlyCollection<ColumnDeclaration> columns, ProjectionToken token)
        {
            this.Projector = projector;
            this.Columns = columns;
            this.Token = token;
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
        ProjectionToken[] tokens;
        ProjectionToken newToken;
        string[] knownAliases;
        string newAlias;
        int iColumn;

        private ColumnProjector() { }

        static internal ColumnExpression SingleProjection(ColumnDeclaration declaration, string newAlias, Type columnType)
        {
            return new ColumnExpression(columnType, newAlias, declaration.Name);
        }

        static internal ProjectedColumns ProjectColumns(ProjectionExpression projector, string newAlias)
        {
            return ProjectColumns(projector.Projector, newAlias, projector.Source.KnownAliases, new[] { projector.Token });
        }

        static internal ProjectedColumns ProjectColumnsGroupBy(Expression projector, string newAlias, string[] knownAliases, ProjectionToken[] tokens)
        {
            ProjectionToken newToken = new ProjectionToken();

            Expression newProj;
            var candidates = DbExpressionNominator.NominateGroupBy(projector, knownAliases, out newProj);

            ColumnProjector cp = new ColumnProjector
            {
                tokens = tokens,
                newToken = newToken,
                newAlias = newAlias,
                knownAliases = knownAliases,
                candidates = candidates
            };

            Expression e = cp.Visit(newProj);

            return new ProjectedColumns(e, cp.columns.AsReadOnly(), newToken);
        }

        static internal ProjectedColumns ProjectColumns(Expression projector, string newAlias, string[] knownAliases, ProjectionToken[] tokens)
        {
            ProjectionToken newToken = new ProjectionToken(); 

            Expression newProj;
            var candidates = DbExpressionNominator.Nominate(projector, knownAliases, out newProj);

            ColumnProjector cp = new ColumnProjector
            {
                tokens = tokens,
                newToken = newToken,
                newAlias = newAlias,
                knownAliases = knownAliases,
                candidates = candidates
            };

            Expression e = cp.Visit(newProj);

            return new ProjectedColumns(e, cp.columns.AsReadOnly(), newToken);
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

        protected override ProjectionToken VisitProjectionToken(ProjectionToken token)
        {
            if (token != null && tokens.Contains(token))
                return newToken;

            return token;
        }
    }
}

