using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities.Isolation
{
    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class IsolationDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        static Expression<Func<IsolationDN, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public static readonly SessionVariable<Lite<IsolationDN>> DefaultVariable = Statics.SessionVariable<Lite<IsolationDN>>("CurrentIsolation");
        public static Lite<IsolationDN> Default
        {
            get { return DefaultVariable.Value; }
            set { DefaultVariable.Value = value; }
        }

        static readonly ThreadVariable<Lite<IsolationDN>> CurrentThreadVariable = Statics.ThreadVariable<Lite<IsolationDN>>("CurrentIsolation");

        public IDisposable OverrideCurrentIsolation(Lite<IsolationDN> isolation)
        {
            var old = CurrentThreadVariable.Value; 

            CurrentThreadVariable.Value = isolation;

            return new Disposable(() => CurrentThreadVariable.Value = old); 
        }

        public static Lite<IsolationDN> Current
        {
            get { return CurrentThreadVariable.Value ?? Default; }
        }
    }

    public static class IsolationOperation
    {
        public static readonly ExecuteSymbol<IsolationDN> Save = OperationSymbol.Execute<IsolationDN>(); 
    }

    public enum IsolationMessage
    {
        [Description("Entity {0} has isolation {1} but current isolation is {2}")]
        Entity0HasIsolation1ButCurrentIsolationIs2
    }

    [Serializable]
    public class IsolationMixin : MixinEntity
    {
        IsolationMixin(IdentifiableEntity mainEntity, MixinEntity next) : base(mainEntity, next) { }

        [NotNullable]
        Lite<IsolationDN> isolation;
        [NotNullValidator]
        public Lite<IsolationDN> Isolation
        {
            get { return isolation; }
            set { Set(ref isolation, value); }
        }

        protected override void CopyFrom(MixinEntity mixin, object[] args)
        {
            Isolation = ((IsolationMixin)mixin).Isolation;
        }
    }

    public static class IsolationExtensions
    {
        static Expression<Func<IIdentifiable, Lite<IsolationDN>>> IsolationExpression =
             entity => ((IdentifiableEntity)entity).Mixin<IsolationMixin>().Isolation;
        public static Lite<IsolationDN> Isolation(this IIdentifiable entity)
        {
            return IsolationExpression.Evaluate(entity);
        }

        public static Lite<IsolationDN> TryIsolation(this IIdentifiable entity)
        {
            var mixin = ((IdentifiableEntity)entity).Mixins.OfType<IsolationMixin>().SingleOrDefaultEx();

            if (mixin == null)
                return null;

            return mixin.Isolation;
        }

        public static T SetIsolation<T>(this T entity, Lite<IsolationDN> isolation)
            where T:IdentifiableEntity
        {
            return entity.SetMixin((IsolationMixin m) => m.Isolation, isolation); 
        }
    }
}
