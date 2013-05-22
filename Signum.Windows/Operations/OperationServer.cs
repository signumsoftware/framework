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
            return (T)(IIdentifiable)Server.Return((IOperationServer s)=>s.ExecuteOperation(entity, operationKey, args)); 
        }

        public static T ExecuteLite<T>(this Lite<T> lite, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            return (T)(IIdentifiable)Server.Return((IOperationServer s) => s.ExecuteOperationLite(lite, operationKey, args)); 
        }

        public static void Delete<T>(this Lite<T> lite, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            Server.Execute((IOperationServer s)=>s.Delete(lite, operationKey, args)); 
        }

        public static T Construct<T>(Enum operationKey, params object[] args)
            where T : class, IIdentifiable
        {
            return (T)(IIdentifiable)Server.Return((IOperationServer s) => s.Construct(typeof(T), operationKey, args)); 
        }

        public static T ConstructFrom<T>(this IIdentifiable entity, Enum operationKey, params object[] args)
              where T : class, IIdentifiable
        {
            return (T)(IIdentifiable)Server.Return((IOperationServer s) => s.ConstructFrom(entity, operationKey, args)); 
        }

        public static T ConstructFromLite<T>(this Lite<IIdentifiable> lite, Enum operationKey, params object[] args)
           where T : class, IIdentifiable
        {
            return (T)(IIdentifiable)Server.Return((IOperationServer s) => s.ConstructFromLite(lite, operationKey, args)); 
        }

        public static T ConstructFromMany<F, T>(List<Lite<F>> lites, Enum operationKey, params object[] args)
            where T : class, IIdentifiable
            where F : class, IIdentifiable
        {
            return (T)(IIdentifiable)Server.Return((IOperationServer s) => s.ConstructFromMany(lites, typeof(F), operationKey, args)); 
        }
    }
}
