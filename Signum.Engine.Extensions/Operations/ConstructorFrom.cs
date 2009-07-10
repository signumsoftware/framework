using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Operations;
using Signum.Entities.Authorization;
using Signum.Utilities;

namespace Signum.Engine.Operations
{
    public interface IConstructorFromOperation : IEntityOperation
    {
        IIdentifiable Construct(IIdentifiable entity, params object[] parameters);
        IIdentifiable Construct(Lazy lazy, params object[] parameters);
    }

    public class BasicConstructorFrom<F, T> : IConstructorFromOperation
        where T : class, IIdentifiable
        where F: class, IIdentifiable
    {
        public Enum Key { get; private set; } 
        public Type Type { get { return typeof(F); } }
        public OperationType OperationType { get { return OperationType.ConstructorFrom; } }

        public bool Lazy { get; set; }
        public bool Returns { get; set; }

        public bool AllowsNew { get; set; }

        public Func<F, object[], T> FromEntity { get; set; }
        public Func<Lazy<F>, object[], T> FromLazy { get; set; }
        public Func<F, string> CanConstruct { get; set; }
        
        public BasicConstructorFrom(Enum key)
        {
            this.Key = key;
            this.Returns = true;
        }

        bool IEntityOperation.CanExecute(IIdentifiable entity)
        {
            return OnCanConstruct(entity) == null;
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

            using (Transaction tr = new Transaction())
            {
                LogOperationDN log = new LogOperationDN
                {
                    Operation = OperationLogic.ToOperation[Key],
                    Start = DateTime.Now,
                    User = UserDN.Current
                };

                IdentifiableEntity result = (IdentifiableEntity)(IIdentifiable)OnFromEntity((F)entity, args);

                result.Save(); //Nothing happens if allready saved

                log.Entity = result.ToLazy();
                log.End = DateTime.Now;
                log.Save();

                return tr.Commit(result);
            }
        }

        protected virtual T OnFromEntity(F entity, object[] args)
        {
            return FromEntity(entity, args);
        }

        IIdentifiable IConstructorFromOperation.Construct(Lazy lazy, params object[] args)
        {
            using (Transaction tr = new Transaction())
            {
                LogOperationDN log = new LogOperationDN
                {
                    Operation = OperationLogic.ToOperation[Key],
                    Start = DateTime.Now,
                    User = UserDN.Current
                };

                IdentifiableEntity result = (IdentifiableEntity)(IIdentifiable)OnFromLazy((Lazy<F>)lazy, args);

                result.Save(); //Nothing happens if allready saved

                log.Entity = result.ToLazy();
                log.End = DateTime.Now;
                log.Save();

                return tr.Commit(result);
            }
        }

        protected virtual T OnFromLazy(Lazy<F> lazy, object[] args)
        {
            return FromLazy(lazy, args);
        }

        public void AssertIsValid()
        {
            if (Lazy)
            {
                if (FromLazy == null)
                    throw new ApplicationException("Operation {0} does not have FromLazy initialized".Formato(Key));
            }
            else
            {
                if (FromEntity == null)
                    throw new ApplicationException("Operation {0} does not have FromEntity initialized".Formato(Key));
            }
        }
    }
}
