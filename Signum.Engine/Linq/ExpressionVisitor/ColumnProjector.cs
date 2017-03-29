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

        internal ProjectedColumns(Expression projector, ReadOnlyCollection<ColumnDeclaration> columns)
        {
            this.Projector = projector;
            this.Columns = columns;
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
        Alias newAlias;
        bool projectTrivialColumns;


        private ColumnProjector() { }

        static internal ColumnExpression SingleProjection(ColumnDeclaration declaration, Alias newAlias, Type columnType)
        {
            return new ColumnExpression(columnType, newAlias, declaration.Name);
        }

        static internal ProjectedColumns ProjectColumns(Expression projector, Alias newAlias, bool isGroupKey = false, bool selectTrivialColumns = false)
        {
            var candidates = DbExpressionNominator.Nominate(projector, out Expression newProj, isGroupKey: isGroupKey);

            ColumnProjector cp = new ColumnProjector
            {
                newAlias = newAlias,
                candidates = candidates,
                projectTrivialColumns = selectTrivialColumns
            };

            Expression e = cp.Visit(newProj);

            return new ProjectedColumns(e, cp.generator.Columns.ToReadOnly());
        }


        public override Expression Visit(Expression expression)
        {
            if (this.candidates.Contains(expression))
            {
                if (expression is ColumnExpression)
                {
                    if (!projectTrivialColumns)
                        return expression;

                    ColumnExpression column = (ColumnExpression)expression;
                    if (this.map.TryGetValue(column, out ColumnExpression mapped))
                    {
                        return mapped;
                    }

                    mapped = generator.MapColumn(column).GetReference(newAlias);
                    this.map[column] = mapped;
                    return mapped;
                }
                else
                {
                    if (expression.Type.UnNullify().IsEnum)
                    {
                        var convert = expression.TryConvert(expression.Type.IsNullable() ? typeof(int?) : typeof(int));

                        return generator.NewColumn(convert).GetReference(newAlias).TryConvert(expression.Type);
                    }
                    else
                    {
                        return generator.NewColumn(expression).GetReference(newAlias);
                    }
                }
            }
            else
            {
                return base.Visit(expression);
            }
        }
    }

    internal class ColumnUnionProjector : DbExpressionVisitor
    {
        Dictionary<ColumnExpression, ColumnExpression> map = new Dictionary<ColumnExpression, ColumnExpression>();
        HashSet<Expression> candidates;
        UnionAllRequest request;
        Type implementation; 

        private ColumnUnionProjector() { }

        static internal Expression Project(Expression projector, HashSet<Expression> candidates, UnionAllRequest request, Type implementation)
        {
            ColumnUnionProjector cp = new ColumnUnionProjector
            {
                request = request,
                implementation = implementation,
                candidates = candidates,
            };

            return cp.Visit(projector);;
        }


        public override Expression Visit(Expression expression)
        {
            if (this.candidates.Contains(expression))
            {
                if (expression is ColumnExpression column)
                {
                    if (this.map.TryGetValue(column, out ColumnExpression mapped))
                    {
                        return mapped;
                    }

                    mapped = request.AddIndependentColumn(column.Type, column.Name, implementation, column);
                    this.map[column] = mapped;
                    return mapped;
                }
                else
                {
                    if (expression.Type.UnNullify().IsEnum)
                    {
                        var convert = expression.TryConvert(expression.Type.IsNullable() ? typeof(int?) : typeof(int));

                        return request.AddIndependentColumn(convert.Type, "v", implementation, convert).TryConvert(expression.Type);
                    }
                    else
                    {
                        return request.AddIndependentColumn(expression.Type, "v", implementation, expression);
                    }
                }
            }
            else
            {
                return base.Visit(expression);
            }
        }
    }


    internal class ColumnGenerator
    {
        public ColumnGenerator()
        {
        }

        public ColumnGenerator(IEnumerable<ColumnDeclaration> columns)
        {
            foreach (var item in columns)
                this.columns.Add(item.Name ?? "-", item);
        }

        public IEnumerable<ColumnDeclaration> Columns { get { return columns.Values; } }

        Dictionary<string, ColumnDeclaration> columns = new Dictionary<string, ColumnDeclaration>(StringComparer.InvariantCultureIgnoreCase);
        int iColumn;
        
        public string GetUniqueColumnName(string name)
        {
            string baseName = name;
            int suffix = 1;
            while (this.columns.ContainsKey(name))
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
            columns.Add(result.Name, result);
            return result; 
        }

        public ColumnDeclaration NewColumn(Expression exp)
        {
            string columnName = GetNextColumnName();
            var result = new ColumnDeclaration(columnName, exp);
            columns.Add(result.Name, result);
            return result; 
        }

        public void AddUsedName(string name)
        {
            columns.Add(name, null);
        }
    }
}

