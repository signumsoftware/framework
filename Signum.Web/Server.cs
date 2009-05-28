using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Services;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using System.Reflection;

namespace Signum.Web
{
    public static class Server
    {
        private static Func<IBaseServer> getServer;

        public static T Service<T>()
        {
            IBaseServer server = getServer();
            if (!(server is T))
                throw new ApplicationException("Server {0} does not implement {1}".Formato(server.GetType(), typeof(T)));
            return (T)server;
        }

        public static bool Implements<T>()
        {
            return getServer() is T;
        }

        public static void SetServerFunction(Func<IBaseServer> server)
        {
            getServer = server;
        }


        public static T Save<T>(this T entidad) where T : IdentifiableEntity
        {
            return (T)Service<IBaseServer>().Save(entidad);
        }

        public static IdentifiableEntity Save(IdentifiableEntity entidad)
        {
            return Service<IBaseServer>().Save(entidad);
        }

        public static T Retrieve<T>(int id) where T : IdentifiableEntity
        {
            return (T)Service<IBaseServer>().Retrieve(typeof(T), id);
        }

        public static IdentifiableEntity Retrieve(Type type, int id)
        {
            return Service<IBaseServer>().Retrieve(type, id);
        }

        public static IdentifiableEntity RetrieveFromLazyAndRemember(Lazy lazy)
        {
            if (lazy.UntypedEntityOrNull == null)
            {
                lazy.SetEntity(Service<IBaseServer>().Retrieve(lazy.RuntimeType, lazy.Id));
            }
            return lazy.UntypedEntityOrNull;
        }

        public static T RetrieveFromLazyAndRemember<T>(this Lazy<T> lazy) where T : class, IIdentifiable
        {
            if (lazy.EntityOrNull == null)
            {
                lazy.SetEntity((IdentifiableEntity)(IIdentifiable)Service<IBaseServer>().Retrieve(lazy.RuntimeType, lazy.Id));
            }
            return lazy.EntityOrNull;
        }

        public static IdentifiableEntity RetrieveFromLazyAndForget(Lazy lazy)
        {
            return Service<IBaseServer>().Retrieve(lazy.RuntimeType, lazy.Id);
        }

        public static T RetrieveFromLazyAndForget<T>(this Lazy<T> lazy) where T : class, IIdentifiable
        {
            return (T)(IIdentifiable)Service<IBaseServer>().Retrieve(lazy.RuntimeType, lazy.Id);
        }

        public static List<T> RetrieveAll<T>()
        {
            return Service<IBaseServer>().RetrieveAll(typeof(T)).Cast<T>().ToList<T>();
        }

        public static List<IdentifiableEntity> RetrieveAll(Type type)
        {
            return Service<IBaseServer>().RetrieveAll(type);
        }

        public static List<Lazy> RetriveAllLazy(Type lazyType, Type[] types)
        {
            return Service<IBaseServer>().RetrieveAllLazy(lazyType, types);
        }

        public static List<Lazy> FindLazyLike(Type lazyType, Type[] types, string subString, int count)
        {
            return Service<IBaseServer>().FindLazyLike(lazyType, types, subString, count);
        }

        public static List<T> SaveList<T>(List<T> list)
            where T: IdentifiableEntity
        {
            return Service<IBaseServer>().SaveList(list.Cast<IdentifiableEntity>().ToList()).Cast<T>().ToList();
        }

        public static Type[] FindImplementations(Type lazyType, MemberInfo[] implementations)
        {
            return Service<IBaseServer>().FindImplementations(lazyType, implementations); 
        }


        public static object Convert(object obj, Type type)
        {
            if (obj == null) return null;

            Type sourceType = obj.GetType();

            if (type.IsAssignableFrom(sourceType))
                return obj;

            if (typeof(Lazy).IsAssignableFrom(sourceType) && type.IsAssignableFrom(((Lazy)obj).RuntimeType))
            {
                return RetrieveFromLazyAndForget((Lazy)obj);
            }

            
            if (typeof(Lazy).IsAssignableFrom(type))
            {
                Type lazyType= Reflector.ExtractLazy(type); 
                
                if(typeof(Lazy).IsAssignableFrom(sourceType))
                {
                    Lazy lazy = (Lazy)obj;
                    if(lazyType.IsAssignableFrom( lazy.RuntimeType))
                    return Lazy.Create(lazyType, lazy.UntypedEntityOrNull);
                }

                else if(lazyType.IsAssignableFrom(sourceType))
                {
                    return Lazy.Create(lazyType, (IdentifiableEntity)obj);
                }
            }

            throw new ApplicationException("Imposible to convert object {0} from {1} to {2}".Formato(obj, sourceType, type));
        }

        public static bool CanConvert(object obj, Type type)
        {
            if (obj == null) 
                return true;

            Type sourceType = obj.GetType();

            if (sourceType == type)
                return true;

            if (typeof(Lazy).IsAssignableFrom(sourceType) && ((Lazy)obj).RuntimeType == type)
            {
                return true;
            }

            Type lazyType;
            if (typeof(Lazy).IsAssignableFrom(type) && (lazyType = Reflector.ExtractLazy(type)).IsAssignableFrom(sourceType))
            {
                return true;
            }

            return false;
        }

        internal static QueryDescription GetQueryDescription(object p)
        {
            throw new NotImplementedException();
        }
    }
}
