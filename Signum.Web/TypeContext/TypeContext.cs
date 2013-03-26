using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using Signum.Entities.Reflection;
using Signum.Web.Properties;
using Signum.Utilities.Reflection;
using Signum.Engine;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Web
{
    #region Context
    public class Context : IDisposable
    {
        public const string Separator = "_";

        public string ControlID { get; set; }

        public Context Parent { get; private set; } 

        public Context(Context parent, string controlID)
        {
            if(parent == null)
            {
                this.Parent = Default;
                this.ControlID = controlID; 
            }
            else
            {
                this.Parent = parent;
                this.ControlID = parent.Compose(controlID); 
            }
        }

        public string Compose(string nameToAppend)
        {
            return ControlID.Add(Separator, nameToAppend);
        }

        public string Compose(params string[] namesToAppend)
        {
            return Compose((IEnumerable<string>)namesToAppend);
        }

        public string Compose(IEnumerable<string> namesToAppend)
        {
            return this.Compose(namesToAppend.ToString(Separator));
        }

        #region Styles
        [Flags]
        enum BoolStyles : short
        {
            LabelVisible = 2,
            ShowValidationMessage = 4,
            ReadOnly =8,
            ValueFirst = 16,
            ShowFieldDiv =32,
            OnlyValue = 64
        }

        BoolStyles styleValues;
        BoolStyles styleHasValue;

        private bool? this[BoolStyles pos]
        {
            get
            {
                if ((styleHasValue & pos) == pos)
                    return (styleValues & pos) == pos;
                return null;
            }

            set
            {
                if (!value.HasValue)
                    styleHasValue &= ~pos;
                else
                {
                    styleHasValue |= pos;

                    if (value.Value)
                        styleValues |= pos;
                    else
                        styleValues &= ~pos;
                }
            }
        }

        public static readonly Context Default = new Context(null, null)
        {
            LabelVisible = true,
            ShowValidationMessage = true,
            ReadOnly = false,
            ValueFirst = false,
            OnlyValue = false
        };

        /* It prints only the value. Useful when used together with Html.Field helper
         * to join different valueLines in the same line */
        public bool OnlyValue
        {
            get { return this[BoolStyles.OnlyValue] ?? Parent.OnlyValue; }
            set { this[BoolStyles.OnlyValue] = value; }        
        }

        public bool LabelVisible
        {
            get { return this[BoolStyles.LabelVisible] ?? Parent.LabelVisible; }
            set { this[BoolStyles.LabelVisible] = value; }
        }

        public bool ReadOnly
        {
            get { return this[BoolStyles.ReadOnly] ?? Parent.ReadOnly; }
            set
            {
                this[BoolStyles.ReadOnly] = value;
                if (value) SetReadOnly();
            }
        }

        protected virtual void SetReadOnly() { }

        public bool ShowValidationMessage
        {
            get { return this[BoolStyles.ShowValidationMessage] ?? Parent.ShowValidationMessage; }
            set { this[BoolStyles.ShowValidationMessage] = value; }
        }

        public bool ValueFirst
        {
            get { return this[BoolStyles.ValueFirst] ?? Parent.ValueFirst; }
            set { this[BoolStyles.ValueFirst] = value; }
        }

        public override string ToString()
        {
            return ControlID; 
        }
        #endregion

        public void Dispose()
        {
        }
    }
    #endregion

    #region TypeContext
    public abstract class TypeContext : Context
    {
        public abstract object UntypedValue { get; }

        public abstract Type Type { get; }

        public PropertyRoute PropertyRoute { get; private set; }

        protected TypeContext(Context parent, string controlID, PropertyRoute route)
            :base(parent, controlID)
        {
            this.PropertyRoute = route;
        }

        public RuntimeInfo RuntimeInfo()
        {
            if (this.UntypedValue == null)
                return new RuntimeInfo() { EntityType = null };

            Type type = this.UntypedValue.GetType();
            if (type.IsLite())
                return new RuntimeInfo((Lite<IIdentifiable>)this.UntypedValue);

            if (type.IsEmbeddedEntity())
                return new RuntimeInfo((EmbeddedEntity)this.UntypedValue);

            if (type.IsIdentifiableEntity())
                return new RuntimeInfo((IdentifiableEntity)this.UntypedValue);

            throw new ArgumentException("Invalid type {0} for RuntimeInfo. It must be Lite, IdentifiableEntity or EmbeddedEntity".Formato(type));
        }

        internal abstract TypeContext Clone(object newValue);
    }
    #endregion

    #region TypeContext<T>
    public class TypeContext<T> : TypeContext
    {
        public T Value { get; set; }

        public override object UntypedValue
        {
            get { return Value; }
        }

        public TypeContext(T value, string controlID)
            : base(null, controlID, PropertyRoute.Root(value.GetType()))
        {
            Value = value;
        }

        public TypeContext(T value, TypeContext parent, string controlID, PropertyRoute route)
            : base(parent, controlID, route)
        {
            Value = value;
        }

        public override Type Type
        {
            get { return typeof(T); }
        }


        public TypeContext<T> SubContext()
        {
            return new TypeContext<T>(this.Value, this, null, PropertyRoute);
        }

        public TypeContext<S> SubContext<S>(Expression<Func<T, S>> property)
        {
            return Common.WalkExpression(this, property);
        }

        public IEnumerable<TypeElementContext<S>> TypeElementContext<S>(Expression<Func<T, MList<S>>> property)
        {
            return TypeContextUtilities.TypeElementContext(Common.WalkExpression(this, property));
        }

        internal override TypeContext Clone(object newValue)
        {
            return new TypeContext<T>((T)newValue, (TypeContext)Parent, ControlID, PropertyRoute);
        }
    }
    #endregion

    #region TypeSubContext<T>
    public class TypeSubContext<T> : TypeContext<T>, IDisposable
    {
        PropertyInfo[] properties;

        public TypeSubContext(T value, TypeContext parent, PropertyInfo[] properties, PropertyRoute route)
            : base(value, parent.ThrowIfNullC(""), properties.ToString(a => a.Name, Separator), route)
        {
            this.properties = properties;
        }

        public PropertyInfo[] Properties
        {
            get { return properties; }
        }

        internal override TypeContext Clone(object newValue)
        {
            return new TypeSubContext<T>((T)newValue, (TypeContext)Parent, Properties, PropertyRoute);
        }
    }
    #endregion

    #region TypeElementContext<T>
    public class TypeElementContext<T> : TypeContext<T>
    {
        public int Index { get; private set; }

        public TypeElementContext(T value, TypeContext parent, int index)
            : base(value, parent, index.ToString(), parent.PropertyRoute.Add("Item"))
        {
            this.Index = index;
        }

        internal override TypeContext Clone(object newValue)
        {
            return new TypeElementContext<T>((T)newValue, (TypeContext)Parent, Index);
        }
    }
    #endregion
}
