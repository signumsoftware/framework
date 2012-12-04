using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Threading;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Engine.Maps;
using Signum.Engine.Basics;

namespace Signum.Engine.Operations
{
    public interface IConstructOperation : IOperation
    {
        IIdentifiable Construct(params object[] parameters);
    }

    public class BasicConstruct<T> : IConstructOperation
        where T: class, IIdentifiable
    {
        protected readonly Enum key;
        Enum IOperation.Key { get { return key; } }
        Type IOperation.Type { get { return typeof(T); } }
        OperationType IOperation.OperationType { get { return OperationType.Constructor; } }
        bool IOperation.Returns { get { return true; } }
        Type IOperation.ReturnType { get { return typeof(T); } }

        public bool Lite { get { return false; } }
        public Func<object[], T> Construct { get; set; }

        public BasicConstruct(Enum key)
        {
            this.key = key; 
        }

        IIdentifiable IConstructOperation.Construct(params object[] args)
        {
            OperationLogic.AssertOperationAllowed(key, inUserInterface: false);

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
                            User = UserHolder.Current.ToLite()
                        };

                        OnBeginOperation();

                        T entity = Construct(args);

                        OnEndOperation(entity);

                        if (!entity.IsNew)
                        {
                            log.Target = entity.ToLite<IIdentifiable>();
                            log.End = TimeZoneManager.Now;
                            using (ExecutionMode.Global())
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
        }

        protected virtual void OnBeginOperation()
        {
            OperationLogic.OnBeginOperation(this, null);
        }

        protected virtual void OnEndOperation(T entity)
        {
            OperationLogic.OnEndOperation(this, entity);
        }

        public virtual void AssertIsValid()
        {
            if (Construct == null)
                throw new InvalidOperationException("Operation {0} does not have Constructor initialized".Formato(key));
        }

        public override string ToString()
        {
            return "{0} Construct {1}".Formato(key, typeof(T));
        }
    }
}
