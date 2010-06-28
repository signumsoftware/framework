using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Runtime.Serialization;
using System.Reflection;
using System.Collections.Specialized;
using Signum.Entities.Properties;
using Signum.Utilities.DataStructures;
using Signum.Entities.Reflection;
using System.ComponentModel;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities
{
    [Serializable]
    public abstract class IdentifiableEntity : ModifiableEntity, IIdentifiable
    {
        internal int? id = null; //primary key
        internal string toStr; //no value for new entities

        [HiddenProperty, Description("Id")]
        public int Id
        {
            get
            {
                if (id == null)
                    throw new InvalidOperationException(Resources._0IsNewAndHasNoId.Formato(this.GetType().Name));
                return id.Value;
            }
            internal set { id = value; }
        }

        [HiddenProperty]
        [Description("Id")]
        public int? IdOrNull
        {
            get { return id; }
        }

        [HiddenProperty]
        public string ToStr
        {
            get { return toStr; }
            protected set { Set(ref toStr, value, () => ToStr); }
        }

        [Ignore]
        bool isNew = true;
        [HiddenProperty]
        public bool IsNew
        {
            get { return isNew; }
            internal set { isNew = value; }
        }

        protected bool SetIfNew<T>(ref T field, T value, Expression<Func<T>> property)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            if (!IsNew)
            {
                PropertyInfo pi = ReflectionTools.BasePropertyInfo(property);
                throw new InvalidOperationException(Resources.AttemptToModify0WhenTheEntityIsNotNew.Formato(pi.Name));
            }

            return base.Set<T>(ref field, value, property);
        }

        protected internal override void PreSaving(ref bool graphModified)
        {
            base.PreSaving(ref graphModified);

            ToStr = ToString();
        }

        public override string ToString()
        {
            return BaseToString();
        }

        internal string BaseToString()
        {
            return "{0} ({1})".Formato(GetType().Name, id.HasValue ? id.ToString() : Resources.New);
        }

        public override bool Equals(object obj)
        {
            if(obj == this)
                return true;

            if(obj == null)
                return false;

            IdentifiableEntity ident = obj as IdentifiableEntity;
            if (ident != null && ident.GetType() == this.GetType() && this.id != null && this.id == ident.id)
                return true;

            return false;
        }

        public virtual string IdentifiableIntegrityCheck()
        {
            return GraphExplorer.IdentifiableIntegrityCheck(GraphExplorer.FromRootIdentifiable(this));
        }

        public override int GetHashCode()
        {
            return id == null ?
                base.GetHashCode() :
                GetType().FullName.GetHashCode() ^ id.Value;
        }

        /// <summary>
        /// Sql-nullify an constant expression in order to work on polymorphic member accesses on Linq queries.
        /// </summary>
        [MethodExpander(typeof(ShyExpander))]
        public T Shy<T>(T value)
        {
            return value;
        }

        class ShyExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments, Type[] typeArguments)
            {
                Type t = typeArguments[0];

                Expression ce = (t.IsValueType && !t.IsNullable()) ?
                    Expression.Convert(Expression.Constant(null, t.Nullify()), t) :
                    (Expression)Expression.Constant(null, t);

                return ExpressionNominatorExtensions.InSqlExpression(
                            Expression.Condition(Expression.TypeIs(instance, instance.Type), arguments[0], ce));
            }
        }

    }

    public interface IIdentifiable: INotifyPropertyChanged, IDataErrorInfo, ICloneable
    {
        int Id { get; }
        int? IdOrNull { get; }
        string ToStr { get; }
        bool IsNew { get; }
    }
}
