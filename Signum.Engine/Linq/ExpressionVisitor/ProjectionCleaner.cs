using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Signum.Entities;
using Signum.Utilities;
using System.Diagnostics;
using Signum.Entities.Reflection;
using Signum.Engine.Maps;

namespace Signum.Engine.Linq
{
    internal class ProjectionCleaner : DbExpressionVisitor
    {
        internal class TableCondition
        {
            public TableExpression Table;
            public ColumnExpression Id;
            public ColumnExpression NewId;
        }

        HashSet<TableCondition> requests = new HashSet<TableCondition>();

        public static Expression Clean(Expression source)
        {
            ProjectionCleaner pc = new ProjectionCleaner();
            return pc.Visit(source);
        }

        protected override Expression VisitLazyReference(LazyReferenceExpression lazy)
        {
            FieldInitExpression fie = lazy.Reference as FieldInitExpression;
            if (fie != null)
            {
                if (fie.Bindings == null)
                {
                    Table table = ConnectionScope.Current.Schema.Table(fie.Type);

                    string tableAlias = this.GetNextAlias();
                    var bindings = table.CreateBindings(tableAlias);

                    ColumnExpression newId = bindings.IDColumn();
                    ColumnExpression toStr = bindings.ToStrColumn();

                    TableExpression tableExpression = new TableExpression(fie.Type, tableAlias, table.Name);
                    requests.Add(new TableCondition
                    {
                        Id = (ColumnExpression)fie.ExternalId,
                        NewId = newId,
                        Table = tableExpression
                    });

                    return new LazyLiteralExpression(lazy.Type, fie.Type, (ColumnExpression)fie.ExternalId, toStr);
                }
                else
                    return new LazyLiteralExpression(lazy.Type, fie.Type, (ColumnExpression)fie.ExternalId, fie.Bindings.ToStrColumn());
            }
            
            return base.VisitLazyReference(lazy);
        }

        protected override Expression VisitFieldInit(FieldInitExpression fieldInit)
        {
            if (typeof(IdentifiableEntity).IsAssignableFrom(fieldInit.Type))
            {
                Expression newID = Visit(fieldInit.ExternalId);
                if (newID != fieldInit.ExternalId)
                {
                    Debug.Assert(false, "FieldInit has identity"); 
                    return new FieldInitExpression(fieldInit.Type, fieldInit.Alias, newID, null); // eliminamos los bindings
                }
                else
                {
                    fieldInit.Bindings = null;
                    return fieldInit;
                }
            }
            else
                return base.VisitFieldInit(fieldInit); 
        }

        protected override Expression VisitImplementedByAll(ImplementedByAllExpression reference)
        {
            var id = (ColumnExpression)Visit(reference.ID);
            var typeId = (ColumnExpression)Visit(reference.TypeID);

            if (id != reference.ID || typeId != reference.TypeID)
            {
                Debug.Assert(false, "No se deberian estar tocando estas cosas");
                return new ImplementedByAllExpression(reference.Type, id, typeId);
            }
            else
            {
                reference.Implementations = null;
                return reference;
            }
        }

        protected override Expression VisitProjection(ProjectionExpression proj)
        {
            ProjectionExpression projection = (ProjectionExpression)base.VisitProjection(proj);

            if (requests.Count == 0)
                return projection;

            Type type = projection.Type;
            string newAlias = GetNextAlias();

            string[] oldAliases = requests.Select(p => p.Table.Alias).And(projection.Source.Alias).ToArray();

            ProjectedColumns pc = ColumnProjector.ProjectColumns(projection.Projector, newAlias, oldAliases);

            JoinExpression source = (JoinExpression)requests.Aggregate((Expression)projection.Source, (e, p) =>
                new JoinExpression(type, JoinType.LeftOuterJoin, e, p.Table,
                  SmartEqualizer.EqualNullable(p.Id, p.NewId),
                true));

            ProjectionExpression newProjection = new ProjectionExpression(
                 new SelectExpression(projection.Source.Type, newAlias, false, null, pc.Columns, source, null, null, null, null),
                 pc.Projector, null);

            return newProjection; 
        }

        int aliasCount = 0; 
        private string GetNextAlias()
        {
            return "r" + (aliasCount++);
        }
    }
}
