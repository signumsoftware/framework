using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Operations;
using Signum.Entities;

namespace Signum.Engine.Operations.Internal
{
    public interface IConstructOperation : IOperation
    {
        IIdentifiable Construct(params object[] parameters);
    }

    public interface IConstructorFromOperation : IEntityOperation
    {
        IIdentifiable Construct(IIdentifiable entity, params object[] parameters);
    }

    public interface IConstructorFromManyOperation : IOperation
    {
        IIdentifiable Construct(IEnumerable<Lite<IIdentifiable>> lites, params object[] parameters);
    }

    public interface IExecuteOperation : IEntityOperation
    {
        void Execute(IIdentifiable entity, params object[] parameters);
    }

    public interface IDeleteOperation : IEntityOperation
    {
        void Delete(IIdentifiable entity, params object[] parameters);
    }

    //The only point of this clases is to avoid 'member names cannot be the same as their enclosing type' compilation message
    public class _Construct<T>
        where T : class, IIdentifiable
    {
        public Func<object[], T> Construct { get; set; }
    }

    public class _Execute<T>
        where T : class, IIdentifiable
    {
        public Action<T, object[]> Execute { get; set; }
    }


    public class _Delete<T>
      where T : class, IIdentifiable
    {
        public Action<T, object[]> Delete { get; set; }
    }
}