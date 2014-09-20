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
        public static ClientConstructorManager ClientManager;

        public static void Start(ConstructorManager manager, ClientConstructorManager clientManager)
        {
            Manager = manager;
            ClientManager = clientManager;
        }

        public static T Construct<T>(this ConstructorContext ctx)
            where T : ModifiableEntity
        {
            return (T)ctx.SurroundConstructUntyped(typeof(T), Manager.ConstructCore);
        }

        public static ModifiableEntity ConstructUntyped(this ConstructorContext ctx, Type type)
        {
            return ctx.SurroundConstructUntyped(type, Manager.ConstructCore);
        }

        public static T SurroundConstruct<T>(this ConstructorContext ctx, Func<ConstructorContext, T> constructor)
            where T : ModifiableEntity
        {
            ctx.Type = typeof(T);

            return (T)Manager.SurroundConstruct(ctx, constructor);
        }

        public static ModifiableEntity SurroundConstructUntyped(this ConstructorContext ctx, Type type, Func<ConstructorContext, ModifiableEntity> constructor)
        {
            ctx.Type = type;

            return Manager.SurroundConstruct(ctx, constructor);
        }

        public static void Register<T>(Func<ConstructorContext, T> constructor) where T:ModifiableEntity
        {
            Manager.Constructors.Add(typeof(T), constructor);
        }
    }


    public class ClientConstructorContext
    {
        public ClientConstructorContext(Type type, string prefix)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            this.Type = type;
            this.Prefix = prefix;
        }

        public Type Type { get; private set; }
        public HtmlHelper Helper { get; private set; }
        public string Prefix { get; private set; }
    }

    public class ClientConstructorManager
    {
        public static object ExtraJsonParams = new object();

        //JsFunction should take a ExtraJsonParams in their arguments, and return a Promise<any> with the new ExtraJsonParams
        public event Func<ClientConstructorContext, JsFunction> GlobalPreConstructors;

        public Dictionary<Type, Func<ClientConstructorContext, JsFunction>> PreConstructors = new Dictionary<Type, Func<ClientConstructorContext, JsFunction>>();

        static readonly JsFunction[] Null = new JsFunction[0];

        public string GetPreConstructorScript(ClientConstructorContext ctx)
        {
            if (!ctx.Type.IsIdentifiableEntity())
                return Default();

            var def = OperationClient.Manager.ClientConstruct(ctx);

            var concat = GlobalPreConstructors + PreConstructors.TryGetC(ctx.Type);

            if (concat == null && def == null)
                return Default();

            var pre = GlobalPreConstructors.GetInvocationListTyped()
                .Select(f => f(ctx)).And(def).NotNull().ToArray();

            if(pre.IsEmpty())
                return Default();

            var modules = pre.Select(p => p.Module).Distinct();

            var code = pre.Reverse().Aggregate("resolve(extraArgs);", (acum, js) =>
@"if(extraArgs == null) return Promise.resolve(null);
" + InvokeFunction(js) + @"
.then(function(extraArgs){ 
" + acum.Indent(4) + @"
});"); 

            var result =
@"function(extraArgs){ 
    return new Promise(function(resolve){
        require([{moduleNames}], function({moduleVars}){
            extraArgs = extraArgs || {};
{code}
        });
    });
}".Replace("{moduleNames}", modules.ToString(m => "'" + m.Name + "'", ", "))
  .Replace("{moduleVars}", modules.ToString(m => JsFunction.VarName(m), ", "))
  .Replace("{code}", code.Indent(12));


            return result;
        }

        private string Default()
        {
            return @"function(extraArgs){ return Promise.resolve(extraArgs || {}); }"; 
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
        public Type Type { get; internal set; }
        public ControllerBase Controller { get; private set; }
        public OperationInfo OperationInfo { get; private set; }
        public List<object> Args { get; private set; }

        public ConstructorContext(ControllerBase controller = null, OperationInfo info = null, List<object> args = null)
        {

            this.Controller = controller;
            this.OperationInfo = info;
            this.Args = args ?? new List<object>();
        }
    }

    public class ConstructorManager
    {
        public ConstructorManager()
        {
            PostConstructors += PostConstructors_AddFilterProperties;
        }

        public event Func<ConstructorContext, IDisposable> PreConstructors;

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
                return OperationClient.Manager.Construct(ctx);

            return (ModifiableEntity)Activator.CreateInstance(ctx.Type, true);
        }

        public static void PostConstructors_AddFilterProperties(ConstructorContext ctx, ModifiableEntity entity)
        {
            HttpContextBase httpContext = ctx.Controller.ControllerContext.HttpContext;

            if (!httpContext.Request.Params.AllKeys.Contains("webQueryName"))
                return;

            if (!(entity is IdentifiableEntity))
                return;

            object queryName = Finder.ResolveQueryName(httpContext.Request.Params["webQueryName"]);

            if (entity.GetType() != queryName as Type)
                return;

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
            using (Disposable.Combine(PreConstructors, f => f(ctx)))
            {
                var entity = constructor(ctx);

                if (entity == null)
                    return null;

                if (PostConstructors != null)
                    foreach (var post in PostConstructors.GetInvocationListTyped())
                    {
                        post(ctx, entity);
                    }

                return entity;
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
