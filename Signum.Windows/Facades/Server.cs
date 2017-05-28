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
using System.Threading.Tasks;
using System.Globalization;

namespace Signum.Windows
{

    public static class Server
    {
        public static bool OfflineMode { get; set; }

        static Func<IBaseServer> getServer;
        
        static IBaseServer current;

        public static event Action Connecting;

        public static event Action<OperationContext> OnOperation; 

        static Server()
        {
            Connecting += () =>
            {
                ServerTypes = current.ServerTypes();
                NameToType = ServerTypes.ToDictionary(a => a.Value.CleanName, a => a.Key);

                MixinDeclarations.Import(current.FindAllMixins());
                PrimaryKey.Import(current.ImportPrimaryKeyDefinitions());
            };
        }

        public static void SetSymbolIds<S>() where S :Symbol 
        {
            if (OfflineMode)
                return;

            Symbol.SetSymbolIds<S>(Server.Return((IBaseServer s) => s.GetSymbolIds(typeof(S))));
        }

        public static void SetSemiSymbolIds<S>() where S : SemiSymbol
        {
            if (OfflineMode)
                return;

            SemiSymbol.SetSemiSymbolIdsAndNames<S>(Server.Return((IBaseServer s) => s.GetSemiSymbolIdsAndNames(typeof(S))));
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

            Connecting?.Invoke();

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

        public static Task ExecuteAsync<S>(Action<S> action)
            where S : class
        {
            return Task.Factory.StartNew(() => Execute(action));
        }
       
        public static void Execute<S>(Action<S> action)
            where S : class
        {
        retry:
            AssertConnected();

            S server = current as S;
            if (server == null)
                throw new InvalidOperationException("Server does not implement {0}".FormatWith(typeof(S)));
            
            try
            {
                using (HeavyProfiler.Log("WCFClient", () => "{0}".FormatWith(typeof(S).TypeName())))
                using (CreateOperationContext((IContextChannel)current))
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

        public static Task<R> ReturnAsync<S, R>(Func<S, R> function)
            where S : class
        {
            return Task.Factory.StartNew(() => Return(function));
        }

        public static R Return<S, R>(Func<S, R> function)
          where S : class
        {
        retry:
            AssertConnected();

            S server = current as S;
            if (server == null)
                throw new InvalidOperationException("Server {0} does not implement {1}".FormatWith(current.GetType(), typeof(S)));

            try
            {
                using (HeavyProfiler.Log("WCFClient", () => "Return(({0} server)=>{1})".FormatWith(typeof(S).TypeName(), typeof(R).TypeName())))
                using (CreateOperationContext((IContextChannel)current))
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

        public static IDisposable CreateOperationContext(IContextChannel contex)
        {
            if (Server.OnOperation == null)
                return null;

            var result = new OperationContextScope(contex);

            Server.OnOperation(OperationContext.Current);

            return result;
        }

        public static void ExecuteNoRetryOnSessionExpired<S>(Action<S> action)
            where S : class
        {
            if (current == null)
                return;

            S server = current as S;
            if (server == null)
                throw new InvalidOperationException("Server {0} does not implement {1}".FormatWith(server.GetType(), typeof(S)));

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

        public static T Save<T>(this T entidad) where T : Entity
        {
            return (T)Return((IBaseServer s) => s.Save(entidad));
        }

        public static Entity Save(Entity entidad)
        {
            return Return((IBaseServer s) => s.Save(entidad)); 
        }

        public static bool Exists<T>(PrimaryKey id) where T : Entity
        {
            return Return((IBaseServer s) => s.Exists(typeof(T), id));
        }

        public static bool Exists<T>(Lite<T> lite) where T : class, IEntity
        {
            return Return((IBaseServer s) => s.Exists(lite.EntityType, lite.Id));
        }

        public static bool Exists<T>(T entity) where T : class, IEntity
        {
            return Return((IBaseServer s) => s.Exists(entity.GetType(), entity.Id));
        }

        public static T Retrieve<T>(PrimaryKey id) where T : Entity
        {
            return (T)Return((IBaseServer s) => s.Retrieve(typeof(T), id)); 
        }

        public static Entity Retrieve(Type type, PrimaryKey id)
        {
            return Return((IBaseServer s) => s.Retrieve(type, id)); 
        }

        public static T Retrieve<T>(this Lite<T> lite) where T : class, IEntity
        {
            if (lite.EntityOrNull == null)
            {
                lite.SetEntity((Entity)(IEntity)Return((IBaseServer s)=>s.Retrieve(lite.EntityType, lite.Id))); 
            }
            return lite.EntityOrNull;
        }

        public static T RetrieveAndForget<T>(this Lite<T> lite) where T : class, IEntity
        {
            return (T)(IEntity)Return((IBaseServer s) => s.Retrieve(lite.EntityType, lite.Id)); 
        }

        public static List<T> RetrieveAll<T>() where T : Entity
        {
            return Return((IBaseServer s) => s.RetrieveAll(typeof(T)).Cast<T>().ToList<T>()); 
        }

        public static List<Entity> RetrieveAll(Type type)
        {
            return Return((IBaseServer s) => s.RetrieveAll(type)); 
        }

        public static List<Lite<Entity>> RetrieveAllLite(Type type)
        {
            return Return((IBaseServer s) => s.RetrieveAllLite(type));
        }

        public static List<Lite<T>> RetrieveAllLite<T>() where T : class, IEntity
        {
            return RetrieveAllLite(typeof(T)).Cast<Lite<T>>().ToList(); 
        }

        public static List<Lite<Entity>> FindAllLite(Implementations implementations)
        {
            return Return((IBaseServer s) => s.FindAllLite(implementations));
        }

        public static List<Lite<Entity>> FindLiteLike(Implementations implementations, string subString, int count)
        {
            return Return((IBaseServer s) => s.FindLiteLike(implementations, subString, count)); 
        }

        public static List<T> SaveList<T>(List<T> list)
            where T: Entity
        {
            return Return((IBaseServer s) => s.SaveList(list.Cast<Entity>().ToList()).Cast<T>().ToList()); 
        }

        public static Lite<T> FillToStr<T>(this Lite<T> lite) where T : class, IEntity
        {
            lite.SetToString(Return((IBaseServer s) => s.GetToStr(lite.EntityType, lite.Id)));

            return lite;
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

            if (objType.IsLite() && type.IsAssignableFrom(((Lite<IEntity>)obj).EntityType))
            {
                Lite<Entity> lite = (Lite<Entity>)obj;
                return lite.EntityOrNull ?? RetrieveAndForget(lite);
            }
            
            if (type.IsLite())
            {
                Type liteType = Lite.Extract(type); 
              
                if(liteType.IsAssignableFrom(objType))
                {
                    Entity ident = (Entity)obj;
                    return ident.ToLite(ident.IsNew);
                }
            }

            throw new InvalidCastException("Impossible to convert objet {0} from type {1} to type {2}".FormatWith(obj, objType, type));
        }

        public static bool CanConvert(object obj, Type type)
        {
            if (obj == null) 
                return true;

            Type objType = obj.GetType();

            if (objType == type)
                return true;

            if (objType.IsLite() && ((Lite<Entity>)obj).EntityType == type)
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

        public static Dictionary<Type, TypeEntity> ServerTypes { get; private set; }
        public static Dictionary<string, Type> NameToType { get; private set; }

        public static Type TryGetType(string cleanName)
        {
            return NameToType.TryGetC(cleanName);
        }

        public static Type GetType(string cleanName)
        {
            return NameToType.GetOrThrow(cleanName, "Type {0} not found in the Server");
        }

        public static void OnOperation_SaveCurrentCulture(OperationContext ctx)
        {
            ctx.OutgoingMessageHeaders.Add(
                new MessageHeader<string>(CultureInfo.CurrentCulture.Name)
                .GetUntypedHeader("CurrentCulture", "http://www.signumsoftware.com/Culture"));

            ctx.OutgoingMessageHeaders.Add(
                new MessageHeader<string>(CultureInfo.CurrentUICulture.Name)
                .GetUntypedHeader("CurrentUICulture", "http://www.signumsoftware.com/Culture"));
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
