using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data;
using System.IO;
using Microsoft.SqlServer.Server;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Signum.Engine.Linq;
using Signum.Entities.Reflection;

namespace Signum.Engine.Maps
{
    public partial class Table
    {
        internal ReadOnlyCollection<FieldBinding> CreateBindings(string alias)
        {
            var bindings = Fields.Values.Select(c => new FieldBinding(c.FieldInfo, c.Field.GetExpression(alias))).ToReadOnly();

            if (!IsView)
            {
                ColumnExpression ce = bindings.IDColumn();

                bindings.Select(fb => fb.Binding).OfType<MListExpression>().ForEach(ml => ml.BackID = ce);
            }

            return bindings; 
        }
    }

    public partial class RelationalTable
    {
        internal ColumnExpression BackColumnExpression(string alias)
        {
            return new ColumnExpression(BackReference.ReferenceType(), alias, BackReference.Name);
        }

        internal Expression CampoExpression(string alias)
        {
            return Field.GetExpression(alias); 
        }
    }

    public static partial class ColumnExtensions
    {
        public static Type ReferenceType(this IColumn columna)
        {
            Debug.Assert(columna.SqlDbType == SqlBuilder.PrimaryKeyType);

            return columna.Nullable ? typeof(int?) : typeof(int);
        }
    }

    public abstract partial class Field
    {
        internal abstract Expression GetExpression(string alias);
    }

    public partial class FieldPrimaryKey
    {
        internal override Expression GetExpression(string alias)
        {
            return new ColumnExpression(typeof(int), alias, this.Name);
        } 
    }

    public partial class FieldValue
    {
        internal override Expression GetExpression(string alias)
        {
            return new ColumnExpression(this.FieldType, alias, this.Name);
        } 
    }

    public static partial class ReferenceFieldExtensions
    {
        internal static Expression MaybeLazy(this IFieldReference campo, Expression reference)
        {
            if (!campo.IsLazy)
                return reference;
            else
                return new LazyReferenceExpression(Reflector.GenerateLazy(reference.Type), reference);
        }
    }


    public partial class FieldReference
    {
        internal override Expression GetExpression(string alias)
        {
            return this.MaybeLazy(new FieldInitExpression(Reflector.ExtractLazy(FieldType) ?? FieldType, alias, new ColumnExpression(this.ReferenceType(), alias, Name))); 
        } 
    }

    public partial class FieldEnum
    {
        internal override Expression GetExpression(string alias)
        {
            return Expression.Convert(new ColumnExpression(this.ReferenceType(), alias, Name), FieldType);
        }
    }

    public partial class FieldMList
    {
        internal override Expression GetExpression(string alias)
        {
            return new MListExpression(FieldType, null, RelationalTable); // keep back id empty for some seconds 
        }
    }

    public partial class FieldEmbedded
    {
        internal override Expression GetExpression(string alias)
        {
            List<FieldBinding> fb = new List<FieldBinding>();
            foreach (var kvp in EmbeddedFields)
	        {
                FieldInfo fi = FieldType.GetField(kvp.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); 
                fb.Add(new FieldBinding(fi, kvp.Value.Field.GetExpression(alias))); 
            }
            return new FieldInitExpression(this.FieldType, alias, null) { Bindings = fb.NotNull().ToReadOnly() }; 
        }
    }

    public partial class FieldImplementedBy
    {
        internal override Expression GetExpression(string alias)
        {
            List<ImplementationColumnExpression> ri = new List<ImplementationColumnExpression>();
            foreach (var kvp in ImplementationColumns)
	        {
                ri.Add(new ImplementationColumnExpression(kvp.Key,
                    new FieldInitExpression(kvp.Key, new ColumnExpression(kvp.Value.ReferenceType(), alias, kvp.Value.Name))));
            }

            return this.MaybeLazy(new ImplementedByExpression(Reflector.ExtractLazy(FieldType)??FieldType, ri.NotNull().ToReadOnly())); 
        }
    }

    public partial class FieldImplementedByAll
    {
        internal override Expression GetExpression(string alias)
        {
            return this.MaybeLazy(new ImplementedByAllExpression(Reflector.ExtractLazy(FieldType) ?? FieldType,
                new ColumnExpression( Column.ReferenceType(), alias, Column.Name),
                new ColumnExpression( Column.ReferenceType(), alias, ColumnTypes.Name)));
        }
    }
}
