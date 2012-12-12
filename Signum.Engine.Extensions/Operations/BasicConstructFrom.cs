using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Operations;
using Signum.Entities.Authorization;
using Signum.Utilities;
using Signum.Engine.Basics;
using Signum.Engine.Extensions.Properties;
using Signum.Engine.Authorization;

namespace Signum.Engine.Operations
{
    public interface IConstructorFromOperation : IEntityOperation
    {
        IIdentifiable Construct(IIdentifiable entity, params object[] parameters);
    }

    public class BasicConstructFrom<F, T> : IConstructorFromOperation
        where T : class, IIdentifiable
        where F : class, IIdentifiable
    {
        protected readonly Enum key;
        Enum IOperation.Key { get { return key; } }
        Type IOperation.Type { get { return typeof(F); } }
        OperationType IOperation.OperationType { get { return OperationType.ConstructorFrom; } }

        public bool Lite { get; set; }
        bool IOperation.Returns { get { return true; } }
        Type IOperation.ReturnType { get { return typeof(T); } }

        bool IEntityOperation.HasCanExecute { get { return CanConstruct != null; } }

        public bool AllowsNew { get; set; }

        public Func<F, object[], T> Construct { get; set; }
        public Func<F, string> CanConstruct { get; set; }

        public BasicConstructFrom(Enum key)
        {
            this.key = key;
            this.Lite = true;
        }

        string IEntityOperation.CanExecute(IIdentifiable entity)
        {
            return OnCanConstruct(entity);
        }

        string OnCanConstruct(IIdentifiable entity)
        {
            if (entity.IsNew && !AllowsNew)
                return Resources.TheEntity0IsNew.Formato(entity);

            if (CanConstruct != null)
                return CanConstruct((F)entity);

            return null;
        }

        IIdentifiable IConstructorFromOperation.Construct(IIdentifiable entity, params object[] args)
        {
            OperationLogic.AssertOperationAllowed(key, inUserInterface: false);

            string error = OnCanConstruct(entity);
            if (error != null)
                throw new ApplicationException(error);

            using (OperationLogic.AllowSave(entity.GetType()))
            using (OperationLogic.AllowSave<T>())
            {
                try
                {
                    using (Transaction tr = new Transaction())
                    {
                        OperationLogDN log = new OperationLogDN
                        {
                            Operation = MultiEnumLogic<OperationDN>.ToEntity(key),
                            Start = TimeZoneManager.Now,
                            User = UserDN.Current.ToLite()
                        };

                        OnBeginOperation((IdentifiableEntity)entity);

                        T result = Construct((F)entity, args);

                        OnEndOperation(result);

                        if (!result.IsNew)
                        {
                            log.Target = result.ToLite<IIdentifiable>();
                            log.End = TimeZoneManager.Now;
                            using (AuthLogic.Disable())
                                log.Save();
                        }

                        return tr.Commit(result);
                    }
                }
                catch (Exception e)
                {
                    OperationLogic.OnErrorOperation(this, (IdentifiableEntity)entity, e);
                    throw;
                }
            }
        }

        protected virtual void OnBeginOperation(IdentifiableEntity entity)
        {
            OperationLogic.OnBeginOperation(this, entity);
        }

        protected virtual void OnEndOperation(T result)
        {
            OperationLogic.OnEndOperation(this, result);
        }


        public virtual void AssertIsValid()
        {
            if (Construct == null)
                throw new InvalidOperationException("Operation {0} does not hace Construct initialized".Formato(key));
        }

        public override string ToString()
        {
            return "{0} ConstructFrom {1} -> {2}".Formato(key, typeof(F), typeof(T));
        }
      
    }
}
