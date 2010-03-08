using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using System.Threading;
using Signum.Utilities;
using Signum.Engine.Basics;
using Signum.Engine.Extensions.Properties;

namespace Signum.Engine.Operations
{
    public interface IConstructorOperation : IOperation
    {
        IIdentifiable Construct(params object[] parameters);
    }

    public class BasicConstructor<T> : IConstructorOperation
        where T: IIdentifiable
    {
        public Enum Key { get; private set; }        
        public Type Type { get { return typeof(T); } }
        public OperationType OperationType { get { return OperationType.Constructor; } }
        public bool Returns { get { return true; } }
        public Type ReturnType { get { return typeof(T); } }

        public bool Lite { get { return false; } }
        public Func<object[], T> Constructor { get; set; }

        public BasicConstructor(Enum key)
        {
            this.Key = key; 
        }

        IIdentifiable IConstructorOperation.Construct(params object[] args)
        {
            OperationLogic.AssertOperationAllowed(Key);
             
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

                     T entity = OnConstruct(args);

                     OperationLogic.OnEndOperation(this, entity);

                     if (!entity.IsNew)
                     {
                         log.Target = entity.ToLite<IIdentifiable>();
                         log.End = DateTime.Now;
                         log.Save();
                     }

                     return tr.Commit(entity);
                 }
             }
             catch (Exception ex)
             {
                 OperationLogic.OnErrorOperation(this, null, ex);
                 throw;
             }
        }

        protected virtual T OnConstruct(object[] args)
        {
            return Constructor(args);
        }

        public void AssertIsValid()
        {
            if (Constructor == null)
                throw new InvalidOperationException(Resources.Operation0DoesNotHaveConstructorInitialized.Formato(Key));
        }
    }
}
