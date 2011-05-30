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
        public Enum Key { get; private set; }
        public Type Type { get { return typeof(F); } }
        public OperationType OperationType { get { return OperationType.ConstructorFrom; } }

        public bool Lite { get; set; }
        public bool Returns { get; set; }
        public Type ReturnType { get { return typeof(T); } }

        public bool AllowsNew { get; set; }

        public Func<F, object[], T> Construct { get; set; }
        public Func<F, string> CanConstruct { get; set; }

        public BasicConstructFrom(Enum key)
        {
            this.Key = key;
            this.Returns = true;
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
            OperationLogic.AssertOperationAllowed(Key);

            string error = OnCanConstruct(entity);
            if (error != null)
                throw new ApplicationException(error);

            try
            {
                using (Transaction tr = new Transaction())
                {
                    LogOperationDN log = new LogOperationDN
                    {
                        Operation = EnumLogic<OperationDN>.ToEntity(Key),
                        Start = TimeZoneManager.Now,
                        User = UserDN.Current.ToLite()
                    };

                    OperationLogic.OnBeginOperation(this, (IdentifiableEntity)entity);

                    T result = OnConstruct((F)entity, args);

                    OperationLogic.OnEndOperation(this, result);

                    if (!result.IsNew)
                    {
                        log.Target = result.ToLite<IIdentifiable>();
                        log.End = TimeZoneManager.Now;
                        using (AuthLogic.User(AuthLogic.SystemUser))
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

        protected virtual T OnConstruct(F entity, object[] args)
        {
            return Construct(entity, args);
        }


        public virtual void AssertIsValid()
        {
            if (Construct == null)
                throw new InvalidOperationException("Operation {0} does not hace Construct initialized".Formato(Key));
        }

        public override string ToString()
        {
            return "{0} ConstructFrom {1} -> {2}".Formato(Key, typeof(F), typeof(T));
        }
    }
}
