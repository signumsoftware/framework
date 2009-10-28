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

namespace Signum.Windows
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

        public static IdentifiableEntity Retrieve(Lite lazy)
        {
            if (lazy.UntypedEntityOrNull == null)
            {
                lazy.SetEntity(Service<IBaseServer>().Retrieve(lazy.RuntimeType, lazy.Id));
            }
            return lazy.UntypedEntityOrNull;
        }

        public static T Retrieve<T>(this Lite<T> lazy) where T : class, IIdentifiable
        {
            if (lazy.EntityOrNull == null)
            {
                lazy.SetEntity((IdentifiableEntity)(IIdentifiable)Service<IBaseServer>().Retrieve(lazy.RuntimeType, lazy.Id));
            }
            return lazy.EntityOrNull;
        }

        public static IdentifiableEntity RetrieveAndForget(Lite lazy)
        {
            return Service<IBaseServer>().Retrieve(lazy.RuntimeType, lazy.Id);
        }

        public static T RetrieveAndForget<T>(this Lite<T> lazy) where T : class, IIdentifiable
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

        public static List<Lite> RetriveAllLite(Type lazyType, Type[] types)
        {
            return Service<IBaseServer>().RetrieveAllLite(lazyType, types);
        }

        public static List<Lite> FindLiteLike(Type lazyType, Type[] types, string subString, int count)
        {
            return Service<IBaseServer>().FindLiteLike(lazyType, types, subString, count);
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

            if (typeof(Lite).IsAssignableFrom(sourceType) && type.IsAssignableFrom(((Lite)obj).RuntimeType))
            {
                return RetrieveAndForget((Lite)obj);
            }

            
            if (typeof(Lite).IsAssignableFrom(type))
            {
                Type lazyType = Reflector.ExtractLite(type); 
                
                if(typeof(Lite).IsAssignableFrom(sourceType))
                {
                    Lite lazy = (Lite)obj;
                    if (lazyType.IsAssignableFrom(lazy.RuntimeType))
                    {
                        if (lazy.UntypedEntityOrNull != null)
                            return Lite.Create(lazyType, lazy.UntypedEntityOrNull);
                        else
                            return Lite.Create(lazyType, lazy.Id, lazy.RuntimeType, lazy.ToStr); 
                    }
                }

                else if(lazyType.IsAssignableFrom(sourceType))
                {
                    return Lite.Create(lazyType, (IdentifiableEntity)obj);
                }
            }

            throw new ApplicationException(Properties.Resources.ImposibleConvertObject0From1To2.Formato(obj, sourceType, type));
        }

        public static bool CanConvert(object obj, Type type)
        {
            if (obj == null) 
                return true;

            Type sourceType = obj.GetType();

            if (sourceType == type)
                return true;

            if (typeof(Lite).IsAssignableFrom(sourceType) && ((Lite)obj).RuntimeType == type)
            {
                return true;
            }

            Type lazyType;
            if (typeof(Lite).IsAssignableFrom(type) && (lazyType = Reflector.ExtractLite(type)).IsAssignableFrom(sourceType))
            {
                return true;
            }

            return false;
        }
    }
}
