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
        public Enum Key { get; private set; }
        public Type Type { get { return typeof(F); } }
        public OperationType OperationType { get { return OperationType.ConstructorFromMany; } }
        
        public bool Lite { get { return true; } }
        public bool Returns { get { return true; } }
        public Type ReturnType { get { return typeof(T); } }

        public Func<List<Lite<F>>, object[], T> Construct { get; set; }

        public BasicConstructFromMany(Enum key)
        {
            this.Key = key; 
        }

        IIdentifiable IConstructorFromManyOperation.Construct(List<Lite> lites, params object[] args)
        {
            OperationLogic.AssertOperationAllowed(Key);

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

                    OperationLogic.OnBeginOperation(this, null);

                    T result = OnConstruct(lites.Select(l => l.ToLite<F>()).ToList(), args);

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
                OperationLogic.OnErrorOperation(this, null, e);
                throw;
            }
        }

        protected virtual T OnConstruct(List<Lite<F>> lites, object[] args)
        {
            return Construct(lites, args);
        }

        public virtual void AssertIsValid()
        {
            if (Construct == null)
                throw new InvalidOperationException("Operation {0} Constructor initialized".Formato(Key));       
        }

        public override string ToString()
        {
            return "{0} ConstructFromMany {1} -> {2}".Formato(Key, typeof(F), typeof(T));
        }

    }
}
