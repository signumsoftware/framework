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
using Newtonsoft.Json;

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

            return Manager.SurroundConstruct(new ConstructorContext(entityType, controller, args), Manager.ConstructCore);
        }

        public static T SurroundConstruct<T>(this ControllerBase controller, Func<ConstructorContext, T> constructor)
            where T : ModifiableEntity
        {
            return (T)Manager.SurroundConstruct(new ConstructorContext(typeof(T), controller, null), constructor);
        }

        public static T SurroundConstruct<T>(this ControllerBase controller, List<object> args, Func<ConstructorContext, T> constructor)
            where T : ModifiableEntity
        {
            return (T)Manager.SurroundConstruct(new ConstructorContext(typeof(T), controller, args), constructor);
        }

        public static ModifiableEntity SurroundConstruct(this ControllerBase controller, Type entityType, List<object> args, Func<ConstructorContext, ModifiableEntity> constructor)
        {
            return Manager.SurroundConstruct(new ConstructorContext(entityType, controller, null), constructor);
        }

        public static void Register<T>(Func<ConstructorContext, T> constructor) where T:ModifiableEntity
        {
            Manager.Constructors.Add(typeof(T), constructor);
        }
    }

    public class ConstructorClientManager
    {
        public object ExtraJsonParams = new object();

        //JsFunction should take a ExtraJsonParams in their arguments, and return a Promise<any> with the new ExtraJsonParams
        public event Func<Type, JsFunction> GlobalPreConstructors;

        public Dictionary<Type, JsFunction> PreConstructors = new Dictionary<Type, JsFunction>();

        static readonly JsFunction[] Null = new JsFunction[0];

        public string GetPreConstructorScript(Type type)
        {
            if (!type.IsIdentifiableEntity())
                return null;

            var pre = GlobalPreConstructors == null ? Enumerable.Empty<JsFunction>() : 
                GlobalPreConstructors.GetInvocationList().Cast<Func<Type, JsFunction>>().Select(f=>f(type)).ToArray();

            var result = PreConstructors.TryGetC(type);

            if(result != null)
                pre = pre.And(result);

            if(pre.IsEmpty())
                return null;

            pre.Select(p=>JsFunction.VarName(p.Module));

            return
@"function(extraArgs){ 
    return new Promise(function(resolve){
        require([{moduleNames}], function({moduleVars}){
            return {code}
        });
    });
}".Replace("moduleNames", pre.ToString(js => "'" + js.Module.Name + "'", ", "))
  .Replace("moduleVars", pre.ToString(js => JsFunction.VarName(js.Module), ", "))
  .Replace("code", pre.Reverse().Aggregate("resolve(extraArgs)", (acum, js) => InvokeFunction(js) + "\r\n.then(function(extraArgs){ return " + acum + ";)"));

        }

        private string InvokeFunction(JsFunction func)
        {
            return JsFunction.VarName(func.Module) + "." + func.FunctionName + "(" +
                func.Arguments.ToString(a => a == ExtraJsonParams ? "extraArgs" : JsonConvert.SerializeObject(a, func.JsonSerializerSettings), ", ") +
                ")";
        }
    }

    public class ConstructorContext
    {
        public ConstructorContext(Type type, ControllerBase controller, List<object> args)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            this.Type = type;
            this.Controller = controller;
            this.Args = args ?? new List<object>();
        }

        public Type Type { get; private set; }
        public ControllerBase Controller { get; private set; }
        public List<object> Args { get; private set; }
    }

    public class ConstructorManager
    {
        public ConstructorManager()
        {
            PostConstructors += PostConstructors_AddFilterProperties;
        }

        public event Action<ConstructorContext> PreConstructors;

        public Dictionary<Type, Func<ConstructorContext, ModifiableEntity>> Constructors = new Dictionary<Type, Func<ConstructorContext, ModifiableEntity>>();

        public event Action<ConstructorContext, ModifiableEntity> PostConstructors;

        public virtual ModifiableEntity ConstructCore(ConstructorContext ctx)
        {
            Func<ConstructorContext, ModifiableEntity> c = Constructors.TryGetC(ctx.Type);
            if (c != null)
            {
                ModifiableEntity result = c(ctx);
                if (result != null)
                    return result;
            }

            if (ctx.Type.IsIdentifiableEntity() && OperationLogic.HasConstructOperations(ctx.Type))
                return OperationClient.Manager.ConstructSingle(ctx.Type);

            return (ModifiableEntity)Activator.CreateInstance(ctx.Type, true);
        }

        public static void PostConstructors_AddFilterProperties(ConstructorContext ctx, ModifiableEntity entity)
        {
            HttpContextBase httpContext = ctx.Controller.ControllerContext.HttpContext;

            if (!httpContext.Request.Params.AllKeys.Contains("webQueryName"))
                return;

            object queryName = Navigator.ResolveQueryName(httpContext.Request.Params["webQueryName"]);

            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(queryName);

            var filters = FindOptionsModelBinder.ExtractFilterOptions(httpContext, queryDescription)
                .Where(fo => fo.Operation == FilterOperation.EqualTo);

            var pairs = from pi in ctx.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        join fo in filters on pi.Name equals fo.Token.Key
                        //where CanConvert(fo.Value, pi.PropertyType) && fo.Value != null
                        where fo.Value != null
                        select new { pi, fo };

            foreach (var p in pairs)
                p.pi.SetValue(entity, Common.Convert(p.fo.Value, p.pi.PropertyType), null);

            return;
        }

        public virtual ModifiableEntity SurroundConstruct(ConstructorContext ctx, Func<ConstructorContext, ModifiableEntity> constructor)
        {
            IDisposable disposable = null;
            try
            {

                if (PreConstructors != null)
                    foreach (Func<ConstructorContext, IDisposable> pre in PreConstructors.GetInvocationList())
                    {
                        disposable = Disposable.Combine(disposable, pre(ctx));
                    }

                var entity = constructor(ctx);

                if (entity == null)
                    return null;

                if (PostConstructors != null)
                    foreach (Action<ConstructorContext, ModifiableEntity> post in PostConstructors.GetInvocationList())
                    {
                        post(ctx, entity);
                    }

                return entity;
            }
            finally
            {
                if (disposable != null)
                    disposable.Dispose();
            }
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
