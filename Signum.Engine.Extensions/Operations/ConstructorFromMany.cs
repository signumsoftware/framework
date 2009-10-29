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
    public interface IConstructorFromManyOperation : IOperation
    {
        IIdentifiable Construct(List<Lite> lites, params object[] parameters);
    }

    public class BasicConstructorFromMany<F, T> : IConstructorFromManyOperation
        where T : class, IIdentifiable
        where F: class, IIdentifiable
    {
        public Enum Key { get; private set; }
        public Type Type { get { return typeof(F); } }
        public OperationType OperationType { get { return OperationType.ConstructorFromMany; } }
        
        public bool Lite { get { return true; } }
        public bool Returns { get { return true; } }

        public Func<List<Lite<F>>, object[], T> Constructor { get; set; }

        public BasicConstructorFromMany(Enum key)
        {
            this.Key = key; 
        }

        IIdentifiable IConstructorFromManyOperation.Construct(List<Lite> lites, params object[] args)
        {
            if (Constructor == null)
                throw new ArgumentException("FromLite");

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

                    OperationLogic.OnBeginOperation(this, null);

                    IdentifiableEntity result = (IdentifiableEntity)(IIdentifiable)OnConstructor(lites.Select(l => l.ToLite<F>()).ToList(), args);

                    OperationLogic.OnEndOperation(this, result);

                    if (!result.IsNew)
                    {
                        log.Target = result.ToLite();
                        log.End = DateTime.Now;
                        log.Save();
                    }

                    return tr.Commit(result);
                }
            }
            catch (Exception e)
            {
                OperationLogic.OnErrorOperation(this, null, e);
                throw e;
            }
        }

        protected virtual T OnConstructor(List<Lite<F>> lites, object[] args)
        {
            return Constructor(lites, args);
        }

        public void AssertIsValid()
        {
            if (Constructor == null)
                throw new ApplicationException("Operation {0} does not have FromLazies initialized".Formato(Key));       
        }

    }
}
