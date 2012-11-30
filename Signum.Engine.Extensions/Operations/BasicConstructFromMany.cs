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
    public interface IConstructorFromManyOperation : IOperation
    {
        IIdentifiable Construct(List<Lite> lites, params object[] parameters);
    }

    public class BasicConstructFromMany<F, T> : IConstructorFromManyOperation
        where T : class, IIdentifiable
        where F: class, IIdentifiable
    {
        protected readonly Enum key;
        Enum IOperation.Key { get { return key; } }
        Type IOperation.Type { get { return typeof(F); } }
        OperationType IOperation.OperationType { get { return OperationType.ConstructorFromMany; } }

        bool IOperation.Returns { get { return true; } }
        Type IOperation.ReturnType { get { return typeof(T); } }

        public Func<List<Lite<F>>, object[], T> Construct { get; set; }

        public BasicConstructFromMany(Enum key)
        {
            this.key = key; 
        }

        IIdentifiable IConstructorFromManyOperation.Construct(List<Lite> lites, params object[] args)
        {
            OperationLogic.AssertOperationAllowed(key, inUserInterface: false);

            using (OperationLogic.AllowSave<F>())
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

                        OnBeginOperation();

                        T result = OnConstruct(lites.Select(l => l.ToLite<F>()).ToList(), args);

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
                    OperationLogic.OnErrorOperation(this, null, e);
                    throw;
                }
            }
        }

        protected virtual void OnBeginOperation()
        {
            OperationLogic.OnBeginOperation(this, null);
        }

        protected virtual void OnEndOperation(T result)
        {
            OperationLogic.OnEndOperation(this, result);
        }

        protected virtual T OnConstruct(List<Lite<F>> lites, object[] args)
        {
            return Construct(lites, args);
        }

        public virtual void AssertIsValid()
        {
            if (Construct == null)
                throw new InvalidOperationException("Operation {0} Constructor initialized".Formato(key));       
        }

        public override string ToString()
        {
            return "{0} ConstructFromMany {1} -> {2}".Formato(key, typeof(F), typeof(T));
        }

    }
}
