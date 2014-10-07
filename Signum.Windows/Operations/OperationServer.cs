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
        public static T Execute<T, B>(this T entity, ExecuteSymbol<B> symbol, params object[] args)
            where T : class, IIdentifiable, B
            where B : class, IIdentifiable
        {
            return (T)(IIdentifiable)Server.Return((IOperationServer s) => s.ExecuteOperation(entity, symbol.Symbol, args)); 
        }

        public static T ExecuteLite<T, B>(this Lite<T> lite, ExecuteSymbol<B> symbol, params object[] args)
            where T : class, IIdentifiable, B
            where B : class, IIdentifiable
        {
            return (T)(IIdentifiable)Server.Return((IOperationServer s) => s.ExecuteOperationLite(lite, symbol.Symbol, args)); 
        }

        public static void Delete<T, B>(this T entity, DeleteSymbol<B> symbol, params object[] args)
            where T : class, IIdentifiable, B
            where B : class, IIdentifiable
        {
            Server.Execute((IOperationServer s) => s.Delete(entity, symbol.Symbol, args));
        }

        public static void DeleteLite<T, B>(this Lite<T> lite, DeleteSymbol<B> symbol, params object[] args)
            where T : class, IIdentifiable, B
            where B : class, IIdentifiable
        {
            Server.Execute((IOperationServer s) => s.DeleteLite(lite, symbol.Symbol, args)); 
        }

        public static T Construct<T>(ConstructSymbol<T>.Simple symbol, params object[] args)
            where T : class, IIdentifiable
        {
            return (T)(IIdentifiable)Server.Return((IOperationServer s) => s.Construct(typeof(T), symbol.Symbol, args)); 
        }

        public static T ConstructFrom<F, FB, T>(this F entity, ConstructSymbol<T>.From<FB> symbol, params object[] args)
            where T : class, IIdentifiable
            where FB : class, IIdentifiable
            where F : class, IIdentifiable, FB
        {
            return (T)(IIdentifiable)Server.Return((IOperationServer s) => s.ConstructFrom(entity, symbol.Symbol, args)); 
        }

        public static T ConstructFromLite<F, FB, T>(this Lite<F> lite, ConstructSymbol<T>.From<FB> symbol, params object[] args)
            where T : class, IIdentifiable
            where FB : class, IIdentifiable
            where F : class, IIdentifiable, FB
        {
            return (T)(IIdentifiable)Server.Return((IOperationServer s) => s.ConstructFromLite(lite, symbol.Symbol, args)); 
        }

        public static T ConstructFromMany<F, FB, T>(List<Lite<F>> lites, ConstructSymbol<T>.FromMany<FB> symbol, params object[] args)
            where T : class, IIdentifiable
            where FB : class, IIdentifiable
            where F : class, IIdentifiable, FB
        {
            return (T)(IIdentifiable)Server.Return((IOperationServer s) => s.ConstructFromMany(lites, typeof(F), symbol.Symbol, args)); 
        }
    }
}
