using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Services;

namespace Signum.Windows
{
    public static class OperationServer
    {
        public static T Execute<T>(this T entity, Enum operationKey, params object[] args)
           where T : class, IIdentifiable
        {
            return (T)Server.Service<IOperationServer>().ExecuteOperation(entity, operationKey, args);
        }

        public static T ExecuteLite<T>(this Lite<T> lite, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            return (T)Server.Service<IOperationServer>().ExecuteOperationLite(lite, operationKey, args);
        }

        public static void Delete<T>(this Lite<T> lite, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            Server.Service<IOperationServer>().Delete(lite, operationKey, args);
        }

        public static T Construct<T>(Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            return (T)Server.Service<IOperationServer>().Construct(typeof(T), operationKey, args);
        }

        public static T ConstructFrom<T>(this IIdentifiable entity, Enum operationKey, params object[] args)
              where T : class, IIdentifiable
        {
            return (T)Server.Service<IOperationServer>().ConstructFrom(entity, operationKey, args);
        }

        public static T ConstructFromLite<T>(this Lite lite, Enum operationKey, params object[] args)
           where T : class, IIdentifiable
        {
            return (T)Server.Service<IOperationServer>().ConstructFromLite(lite, operationKey, args);
        }

        public static T ConstructFromMany<F, T>(List<Lite<F>> lites, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
            where F : class, IIdentifiable
        {
            return (T)Server.Service<IOperationServer>().ConstructFromMany(lites.Cast<Lite>().ToList(), typeof(F), operationKey, args);
        }
    }
}
