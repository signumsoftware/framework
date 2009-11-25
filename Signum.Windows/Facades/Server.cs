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

        public static IdentifiableEntity Retrieve(Lite lite)
        {
            if (lite.UntypedEntityOrNull == null)
            {
                lite.SetEntity(Service<IBaseServer>().Retrieve(lite.RuntimeType, lite.Id));
            }
            return lite.UntypedEntityOrNull;
        }

        public static T Retrieve<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            if (lite.EntityOrNull == null)
            {
                lite.SetEntity((IdentifiableEntity)(IIdentifiable)Service<IBaseServer>().Retrieve(lite.RuntimeType, lite.Id));
            }
            return lite.EntityOrNull;
        }

        public static IdentifiableEntity RetrieveAndForget(Lite lite)
        {
            return Service<IBaseServer>().Retrieve(lite.RuntimeType, lite.Id);
        }

        public static T RetrieveAndForget<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            return (T)(IIdentifiable)Service<IBaseServer>().Retrieve(lite.RuntimeType, lite.Id);
        }

        public static List<T> RetrieveAll<T>() where T : IdentifiableEntity
        {
            return Service<IBaseServer>().RetrieveAll(typeof(T)).Cast<T>().ToList<T>();
        }

        public static List<IdentifiableEntity> RetrieveAll(Type type)
        {
            return Service<IBaseServer>().RetrieveAll(type);
        }

        public static List<Lite> RetrieveAllLite(Type liteType, Type[] types)
        {
            return Service<IBaseServer>().RetrieveAllLite(liteType, types);
        }

        public static List<Lite<T>> RetrieveAllLite<T>(Type[] types) where T : class, IIdentifiable
        {
            return Service<IBaseServer>().RetrieveAllLite(typeof(T), types).Cast<Lite<T>>().ToList();
        }


        public static List<Lite> FindLiteLike(Type liteType, Type[] types, string subString, int count)
        {
            return Service<IBaseServer>().FindLiteLike(liteType, types, subString, count);
        }

        public static List<T> SaveList<T>(List<T> list)
            where T: IdentifiableEntity
        {
            return Service<IBaseServer>().SaveList(list.Cast<IdentifiableEntity>().ToList()).Cast<T>().ToList();
        }

        public static Type[] FindImplementations(Type liteType, MemberInfo[] implementations)
        {
            return Service<IBaseServer>().FindImplementations(liteType, implementations); 
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
                Type liteType = Reflector.ExtractLite(type); 
                
                if(typeof(Lite).IsAssignableFrom(sourceType))
                {
                    Lite lite = (Lite)obj;
                    if (liteType.IsAssignableFrom(lite.RuntimeType))
                    {
                        if (lite.UntypedEntityOrNull != null)
                            return Lite.Create(liteType, lite.UntypedEntityOrNull);
                        else
                            return Lite.Create(liteType, lite.Id, lite.RuntimeType, lite.ToStr); 
                    }
                }

                else if(liteType.IsAssignableFrom(sourceType))
                {
                    return Lite.Create(liteType, (IdentifiableEntity)obj);
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

            Type liteType;
            if (typeof(Lite).IsAssignableFrom(type) && (liteType = Reflector.ExtractLite(type)).IsAssignableFrom(sourceType))
            {
                return true;
            }

            return false;
        }
    }
}
