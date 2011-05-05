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
        ColumnGenerator generator = new ColumnGenerator();

        Dictionary<ColumnExpression, ColumnExpression> map = new Dictionary<ColumnExpression, ColumnExpression>();
        HashSet<Expression> candidates;
        ProjectionToken[] tokens;
        ProjectionToken newToken;
        string[] knownAliases;
        string newAlias;


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

            return new ProjectedColumns(e, cp.generator.columns.AsReadOnly(), newToken);
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

            return new ProjectedColumns(e, cp.generator.columns.AsReadOnly(), newToken);
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
                        mapped = generator.MapColumn(column).GetReference(newAlias);
                        this.map[column] = mapped;
                        return mapped;
                    }
                    // must be referring to outer scope
                    return column;
                }
                else
                {
                    return generator.NewColumn(expression).GetReference(newAlias); ;
                }
            }
            else
            {
                return base.Visit(expression);
            }
        }


        protected override ProjectionToken VisitProjectionToken(ProjectionToken token)
        {
            if (token != null && tokens.Contains(token))
                return newToken;

            return token;
        }

        Dictionary<ImplementedByAllExpression, Expression> ibas = new Dictionary<ImplementedByAllExpression, Expression>();
        protected override Expression VisitImplementedByAll(ImplementedByAllExpression iba)
        {
            return ibas.GetOrCreate(iba, () => base.VisitImplementedByAll(iba));
        }

        Dictionary<ImplementedByExpression, Expression> ibs = new Dictionary<ImplementedByExpression, Expression>();
        protected override Expression VisitImplementedBy(ImplementedByExpression ib)
        {
            return ibs.GetOrCreate(ib, () => base.VisitImplementedBy(ib));
        }


        Dictionary<FieldInitExpression, Expression> fies = new Dictionary<FieldInitExpression, Expression>();
        protected override Expression VisitFieldInit(FieldInitExpression fie)
        {
            return fies.GetOrCreate(fie, () => base.VisitFieldInit(fie));
        }
    }

    internal class ColumnGenerator
    {
        public List<ColumnDeclaration> columns = new List<ColumnDeclaration>();
        int iColumn;
        
        public string GetUniqueColumnName(string name)
        {
            string baseName = name;
            int suffix = 1;
            while (this.columns.Select(c => c.Name).Contains(name))
                name = baseName + (suffix++);
            return name;
        }

        public string GetNextColumnName()
        {
            return this.GetUniqueColumnName("c" + (iColumn++));
        }

        public ColumnDeclaration MapColumn(ColumnExpression ce)
        {
            string columnName = GetUniqueColumnName(ce.Name);
            var result = new ColumnDeclaration(columnName, ce);
            columns.Add(result);
            return result; 
        }

        public ColumnDeclaration NewColumn(Expression exp)
        {
            string columnName = GetNextColumnName();
            var result = new ColumnDeclaration(columnName, exp);
            columns.Add(result);
            return result; 
        }
    }
}

