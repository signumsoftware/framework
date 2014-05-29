using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web;
using Signum.Entities;
using Signum.Entities.Basics;
using System.Reflection;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using System.Web.Mvc;
using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Web.Operations;
using Signum.Engine.Operations;

namespace Signum.Web
{
    public static class Constructor
    {
        public static ConstructorManager Manager;
        public static ConstructorClientManager ClientManager;

        public static void Start(ConstructorManager manager, ConstructorClientManager clientManager)
        {
            Manager = manager;
            ClientManager = clientManager;
        }

        public static T Construct<T>(this ControllerBase controller, List<object> args = null)
            where T : ModifiableEntity
        {
            return (T)controller.Construct(typeof(T), args);
        }

        public static ModifiableEntity Construct(this ControllerBase controller, Type entityType, List<object> args = null)
        {
            args = args ?? new List<object>();

            return Manager.SurroundConstruct(entityType, controller, args, Manager.ConstructCore);
        }

        public static T SurroundConstruct<T>(this ControllerBase controller, List<object> args, Func<ControllerBase, List<object>, T> constructor)
            where T : ModifiableEntity
        {
            return (T)SurroundConstruct(typeof(T), controller, args, (_type, _controller, _args) => constructor(_controller, _args));
        }

        public static T SurroundConstruct<T>(this ControllerBase controller, Func<T> constructor)
            where T : ModifiableEntity
        {
            return (T)SurroundConstruct(typeof(T), controller, null, (_type, _controller, _args) => constructor());
        }

        public static ModifiableEntity SurroundConstruct(Type entityType, ControllerBase controller, List<object> args, Func<Type, ControllerBase, List<object>, ModifiableEntity> constructor)
        {
            return Manager.SurroundConstruct(entityType, controller, args, constructor);
        }

        public static void Register<T>(Func<T> constructor) where T:ModifiableEntity
        {
            Manager.Constructors.Add(typeof(T), constructor);
        }
    }

    public class ConstructorClientManager
    {
        public event Func<Type, ControllerBase, JsFunction> PreConstructors;

        public Dictionary<Type, Func<ControllerBase, JsFunction>> Constructors = new Dictionary<Type, Func<ControllerBase, JsFunction>>();


    }
    
    public class ConstructorManager
    {
        public ConstructorManager()
        {
            PostConstructors += PostConstructors_AddFilterProperties;
        }

        public event Func<Type, ControllerBase, List<object>, bool> PreConstructors;

        public Dictionary<Type, Func<ModifiableEntity>> Constructors = new Dictionary<Type, Func<ModifiableEntity>>();

        public event Func<Type, ControllerBase, List<object>, ModifiableEntity, bool> PostConstructors;

        public virtual ModifiableEntity ConstructCore(Type entityType, ControllerBase element = null, List<object> args = null)
        {
            Func<ModifiableEntity> c = Constructors.TryGetC(entityType);
            if (c != null)
            {
                ModifiableEntity result = c();
                if (result != null)
                    return result;
            }

            if (entityType.IsIdentifiableEntity() && OperationLogic.HasConstructOperations(entityType))
                return OperationClient.Manager.ConstructSingle(entityType);

            return (ModifiableEntity)Activator.CreateInstance(entityType, true);
        }

        public static bool PostConstructors_AddFilterProperties(Type type, ControllerBase controller, List<object> args, ModifiableEntity entity)
        {   
            HttpContextBase httpContext = controller.ControllerContext.HttpContext;

            if (!httpContext.Request.Params.AllKeys.Contains("webQueryName"))
                return false;

            object queryName = Navigator.ResolveQueryName(httpContext.Request.Params["webQueryName"]);

            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(queryName);

            var filters = FindOptionsModelBinder.ExtractFilterOptions(httpContext, queryDescription)
                .Where(fo => fo.Operation == FilterOperation.EqualTo);

            var pairs = from pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        join fo in filters on pi.Name equals fo.Token.Key
                        //where CanConvert(fo.Value, pi.PropertyType) && fo.Value != null
                        where fo.Value != null
                        select new { pi, fo };

            foreach (var p in pairs)
                p.pi.SetValue(entity, Common.Convert(p.fo.Value, p.pi.PropertyType), null);

            return true;
        }

        public virtual ModifiableEntity SurroundConstruct(Type type, ControllerBase controller, List<object> args, 
            Func<Type, ControllerBase, List<object>, ModifiableEntity> constructor)
        {
            args = args ?? new List<object>();

            if (PreConstructors != null)
                foreach (Func<Type, ControllerBase, List<object>, bool> pre in PreConstructors.GetInvocationList())
                    if (!pre(type, controller, args))
                        return null;

            var entity = constructor(type, controller, args);

            if (entity == null)
                return null;

            if (PostConstructors != null)
                foreach (Func<Type, ControllerBase, List<object>, ModifiableEntity, bool> post in PostConstructors.GetInvocationList())
                    if (!post(type, controller, args, entity))
                        return null;

            return entity;
        }
    }

    public enum VisualConstructStyle
    {
        PopupView, 
        PopupNavigate,
        PartialView,  
        View,
        Navigate
    }
}
