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
using System.ServiceModel;
using System.Windows;
using System.ServiceModel.Security;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Basics;
using System.Collections.Concurrent;

namespace Signum.Windows
{

    public static class Server
    {
        static Func<IBaseServer> getServer;
        
        static IBaseServer current;

        public static event Action Connecting;

        static Server()
        {
            Connecting += () =>
            {
                ServerTypes = current.ServerTypes();
                NameToType = ServerTypes.ToDictionary(a => a.Value.CleanName, a => a.Key);

                MixinDeclarations.Import(current.FindAllMixins()); 
            };
        }

        public static void SetNewServerCallback(Func<IBaseServer> server)
        {
            getServer = server;
        }

        static void AssertConnected()
        {
            if (Connected)
                return;

            if (!Connect())
                throw new NotConnectedToServerException(ConnectionMessage.AConnectionWithTheServerIsNecessaryToContinue.NiceToString());

        }

        public static bool Connect()
        {
            Disconnect();

            current = getServer();

            if (current == null)
                return false;

            if (Connecting != null)
                Connecting();

            return true;
        }

        public static bool Connected
        {
            get
            {
                if (current == null)
                    return false;

                if(!(current is ICommunicationObject))
                    return true;

                return ((ICommunicationObject)current).State != CommunicationState.Faulted;
            }
        }

        public static void Disconnect()
        {
            ICommunicationObject co = current as ICommunicationObject;

            if (co == null)
                return;

            if (co.State == CommunicationState.Faulted)
                co.Abort();
            else if(co.State != CommunicationState.Closed)
                co.Close();

            ((IDisposable)co).Dispose();
        }
       
        public static void Execute<S>(Action<S> action)
            where S : class
        {
        retry:
            AssertConnected();

            S server = current as S;
            if (server == null)
                throw new InvalidOperationException("Server {0} does not implement {1}".Formato(server.GetType(), typeof(S)));
            
            try
            {
                using (HeavyProfiler.Log("WCFClient", () => "{0}".Formato(typeof(S).TypeName())))
                {
                    action(server);
                }
            }
            catch (MessageSecurityException e)
            {
                HandleSessionException(e);
                current = null;
                goto retry;
            }
        }

        public static R Return<S, R>(Func<S, R> function)
          where S : class
        {
        retry:
            AssertConnected();

            S server = current as S;
            if (server == null)
                throw new InvalidOperationException("Server {0} does not implement {1}".Formato(current.GetType(), typeof(S)));

            try
            {
                using (HeavyProfiler.Log("WCFClient", () => "Return(({0} server)=>{1})".Formato(typeof(S).TypeName(), typeof(R).TypeName())))
                {
                    return function(server);
                }
            }
            catch (MessageSecurityException e)
            {
                HandleSessionException(e);
                current = null;
                goto retry;
            }
        }

        public static void ExecuteNoRetryOnSessionExpired<S>(Action<S> action)
            where S : class
        {
            if (current == null)
                return;

            S server = current as S;
            if (server == null)
                throw new InvalidOperationException("Server {0} does not implement {1}".Formato(server.GetType(), typeof(S)));

            using (HeavyProfiler.Log("WCFClient", () => typeof(S).TypeName()))
            {
                action(server);
            }
        }

        static void HandleSessionException(MessageSecurityException e)
        {
            MessageBox.Show(ConnectionMessage.SessionExpired.NiceToString(), ConnectionMessage.SessionExpired.NiceToString(), MessageBoxButton.OK, MessageBoxImage.Hand);
        }

        public static bool Implements<T>()
        {
            Connect();
            return current is T;
        }

        public static T Save<T>(this T entidad) where T : IdentifiableEntity
        {
            return (T)Return((IBaseServer s) => s.Save(entidad));
        }

        public static IdentifiableEntity Save(IdentifiableEntity entidad)
        {
            return Return((IBaseServer s) => s.Save(entidad)); 
        }

        public static T Retrieve<T>(int id) where T : IdentifiableEntity
        {
            return (T)Return((IBaseServer s) => s.Retrieve(typeof(T), id)); 
        }

        public static IdentifiableEntity Retrieve(Type type, int id)
        {
            return Return((IBaseServer s) => s.Retrieve(type, id)); 
        }

        public static T Retrieve<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            if (lite.EntityOrNull == null)
            {
                lite.SetEntity((IdentifiableEntity)(IIdentifiable)Return((IBaseServer s)=>s.Retrieve(lite.EntityType, lite.Id))); 
            }
            return lite.EntityOrNull;
        }

        public static T RetrieveAndForget<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            return (T)(IIdentifiable)Return((IBaseServer s) => s.Retrieve(lite.EntityType, lite.Id)); 
        }

        public static List<T> RetrieveAll<T>() where T : IdentifiableEntity
        {
            return Return((IBaseServer s) => s.RetrieveAll(typeof(T)).Cast<T>().ToList<T>()); 
        }

        public static List<IdentifiableEntity> RetrieveAll(Type type)
        {
            return Return((IBaseServer s) => s.RetrieveAll(type)); 
        }

        public static List<Lite<IdentifiableEntity>> RetrieveAllLite(Type type)
        {
            return Return((IBaseServer s) => s.RetrieveAllLite(type));
        }

        public static List<Lite<T>> RetrieveAllLite<T>() where T : class, IIdentifiable
        {
            return RetrieveAllLite(typeof(T)).Cast<Lite<T>>().ToList(); 
        }

        public static List<Lite<IdentifiableEntity>> FindAllLite(Implementations implementations)
        {
            return Return((IBaseServer s) => s.FindAllLite(implementations));
        }

        public static List<Lite<IdentifiableEntity>> FindLiteLike(Implementations implementations, string subString, int count)
        {
            return Return((IBaseServer s) => s.FindLiteLike(implementations, subString, count)); 
        }

        public static List<T> SaveList<T>(List<T> list)
            where T: IdentifiableEntity
        {
            return Return((IBaseServer s) => s.SaveList(list.Cast<IdentifiableEntity>().ToList()).Cast<T>().ToList()); 
        }

        static ConcurrentDictionary<Type, Dictionary<PropertyRoute, Implementations>> implementations = new ConcurrentDictionary<Type, Dictionary<PropertyRoute, Implementations>>();

        public static Implementations FindImplementations(PropertyRoute propertyRoute)
        {
            var dic = implementations.GetOrAdd(propertyRoute.RootType, type =>
            {
                if (!Server.ServerTypes.ContainsKey(type))
                    return null;

                return Server.Return((IBaseServer s) => s.FindAllImplementations(type));
            });

            return dic.GetOrThrow(propertyRoute, "{0} implementations not found");
        }

        public static object Convert(object obj, Type type)
        {
            if (obj == null) return null;

            Type objType = obj.GetType();

            if (type.IsAssignableFrom(objType))
                return obj;

            if (objType.IsLite() && type.IsAssignableFrom(((Lite<IIdentifiable>)obj).EntityType))
            {
                Lite<IdentifiableEntity> lite = (Lite<IdentifiableEntity>)obj;
                return lite.UntypedEntityOrNull ?? RetrieveAndForget(lite);
            }
            
            if (type.IsLite())
            {
                Type liteType = Lite.Extract(type); 
              
                if(liteType.IsAssignableFrom(objType))
                {
                    IdentifiableEntity ident = (IdentifiableEntity)obj;
                    return ident.ToLite(ident.IsNew);
                }
            }

            throw new InvalidCastException("Impossible to convert objet {0} from type {1} to type {2}".Formato(obj, objType, type));
        }

        public static bool CanConvert(object obj, Type type)
        {
            if (obj == null) 
                return true;

            Type objType = obj.GetType();

            if (objType == type)
                return true;

            if (objType.IsLite() && ((Lite<IdentifiableEntity>)obj).EntityType == type)
            {
                return true;
            }

            Type liteType;
            if (type.IsLite() && (liteType = Lite.Extract(type)).IsAssignableFrom(objType))
            {
                return true;
            }

            return false;
        }

        public static Dictionary<Type, TypeDN> ServerTypes { get; private set; }
        public static Dictionary<string, Type> NameToType { get; private set; }

        public static Type TryGetType(string cleanName)
        {
            return NameToType.TryGetC(cleanName);
        }

        public static Type GetType(string cleanName)
        {
            return NameToType.GetOrThrow(cleanName, "Type {0} not found in the Server");
        }

        public static Type ToType(this TypeDN typeDN)
        {
            return GetType(typeDN.CleanName);
        }

        public static string GetCleanName(Type type)
        {
            return ServerTypes.GetOrThrow(type).CleanName;
        }

        public static TypeDN ToTypeDN(this Type type)
        {
            return ServerTypes.GetOrThrow(type);
        }

        public static Lite<T> FillToStr<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            lite.SetToString(Return((IBaseServer s) => s.GetToStr(lite.EntityType, lite.Id)));

           return lite;
        }

      
    }

    [Serializable]
    public class NotConnectedToServerException : Exception
    {
        public NotConnectedToServerException() { }
        public NotConnectedToServerException(string message) : base(message) { }
        public NotConnectedToServerException(string message, Exception inner) : base(message, inner) { }
        protected NotConnectedToServerException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

}
