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
        IIdentifiable Construct(List<Lazy> lazies, params object[] parameters);
    }

    public class BasicConstructorFromMany<F, T> : IConstructorFromManyOperation
        where T : class, IIdentifiable
        where F: class, IIdentifiable
    {
        public Enum Key { get; private set; }
        public Type Type { get { return typeof(F); } }
        public OperationType OperationType { get { return OperationType.ConstructorFromMany; } }
        
        public bool Lazy { get { return true; } }
        public bool Returns { get { return true; } }

        public Func<List<Lazy<F>>, object[], T> Constructor { get; set; }

        public BasicConstructorFromMany(Enum key)
        {
            this.Key = key; 
        }

        IIdentifiable IConstructorFromManyOperation.Construct(List<Lazy> lazies, params object[] args)
        {
            if (Constructor == null)
                throw new ArgumentException("FromLazy");

            using (Transaction tr = new Transaction())
            {
                LogOperationDN log = new LogOperationDN
                {
                    Operation = EnumLogic<OperationDN>.ToEntity(Key),
                    Start = DateTime.Now,
                    User = UserDN.Current
                };

                IdentifiableEntity result = (IdentifiableEntity)(IIdentifiable)OnConstructor(lazies.Select(l=>l.ToLazy<F>()).ToList(), args);

                result.Save(); //Nothing happens if already saved

                log.Target = result.ToLazy();
                log.End = DateTime.Now;
                log.Save();

                return tr.Commit(result);
            }
        }

        protected virtual T OnConstructor(List<Lazy<F>> lazies, object[] args)
        {
            return Constructor(lazies, args);
        }

        public void AssertIsValid()
        {
            if (Constructor == null)
                throw new ApplicationException("Operation {0} does not have FromLazies initialized".Formato(Key));       
        }

    }
}
