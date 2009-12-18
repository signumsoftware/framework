using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Operations;
using Signum.Entities.Authorization;
using Signum.Utilities;
using Signum.Engine.Basics;

namespace Signum.Engine.Operations
{
    public interface IConstructorFromOperation : IEntityOperation
    {
        IIdentifiable Construct(IIdentifiable entity, params object[] parameters);
    }

    public class BasicConstructorFrom<F, T> : IConstructorFromOperation
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

        public BasicConstructorFrom(Enum key)
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
                return "The Entity {0} is New".Formato(entity);

            if (CanConstruct != null)
                return CanConstruct((F)entity);

            return null;
        }

        IIdentifiable IConstructorFromOperation.Construct(IIdentifiable entity, params object[] args)
        {
            string error = OnCanConstruct(entity);
            if (error != null)
                throw new ApplicationException(error);

            if (!OperationLogic.OnAllowOperation(Key))
                throw new UnauthorizedAccessException("Operation {0} is not Authorized".Formato(Key)); 

            try
            {
                using (Transaction tr = new Transaction())
                {
                    LogOperationDN log = new LogOperationDN
                    {
                        Operation = EnumLogic<OperationDN>.ToEntity(Key),
                        Start = DateTime.Now,
                        User = UserDN.Current
                    };

                    OperationLogic.OnBeginOperation(this, (IdentifiableEntity)entity);

                    T result = OnConstruct((F)entity, args);

                    OperationLogic.OnEndOperation(this, result);

                    if (!result.IsNew)
                    {
                        log.Target = result.ToLite<IIdentifiable>();
                        log.End = DateTime.Now;
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


        public void AssertIsValid()
        {
            if (Construct == null)
                throw new ApplicationException("Operation {0} has no Construct".Formato(Key));
        }
    }
}
