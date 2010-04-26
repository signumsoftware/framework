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
using Signum.Web.Properties;

namespace Signum.Web
{
    public static class Constructor
    {
        public static ConstructorManager ConstructorManager;

        public static void Start(ConstructorManager constructorManager)
        {
            ConstructorManager = constructorManager;
            constructorManager.Initialize();
        }

        public static ModifiableEntity Construct(Type type)
        {
            return ConstructorManager.Construct(type);
        }

        public static T Construct<T>() where T : ModifiableEntity
        {
            return (T)ConstructorManager.Construct(typeof(T));
        }

        public static ActionResult VisualConstruct(ControllerBase controller, Type type, string prefix, VisualConstructStyle preferredStyle)
        {
            return ConstructorManager.VisualConstruct(controller, type, prefix, preferredStyle);
        }
    }

    public class ConstructContext
    {
        public ControllerBase Controller { get; set; }
        public Type Type { get; set; }
        public string Prefix { get; set; }
        public VisualConstructStyle PreferredViewStyle { get; set; }
    }
    
    public class ConstructorManager
    {
        public event Func<Type, ModifiableEntity> GeneralConstructor;
        public Dictionary<Type, Func<ModifiableEntity>> Constructors;

        public event Func<ConstructContext, ActionResult> VisualGeneralConstructor;
        public Dictionary<Type, Func<ConstructContext, ActionResult>> VisualConstructors;

        internal void Initialize()
        {
            if (Constructors == null)
                Constructors = new Dictionary<Type, Func<ModifiableEntity>>();
            if (VisualConstructors == null)
                VisualConstructors = new Dictionary<Type, Func<ConstructContext, ActionResult>>();
        }

        public virtual ModifiableEntity Construct(Type type)
        {
            Func<ModifiableEntity> c = Constructors.TryGetC(type);
            if (c != null)
            {
                ModifiableEntity result = c();
                if (result != null)
                    return result;
            }

            if (GeneralConstructor != null)
            {
                ModifiableEntity result = GeneralConstructor(type);
                if (result != null)
                    return result;
            }

            return DefaultContructor(type);
        }

        public static ModifiableEntity DefaultContructor(Type type)
        {
            return (ModifiableEntity)Activator.CreateInstance(type);
        }

        public virtual ActionResult VisualConstruct(ControllerBase controller, Type type, string prefix, VisualConstructStyle preferredStyle)
        {
            ConstructContext ctx = new ConstructContext { Controller = controller, Type = type, Prefix = prefix, PreferredViewStyle = preferredStyle };
            Func<ConstructContext, ActionResult> c = VisualConstructors.TryGetC(type);
            if (c != null)
            {
                ActionResult result = c(ctx);
                if (result != null)
                    return result;
            }

            if (VisualGeneralConstructor != null)
            {
                ActionResult result = VisualGeneralConstructor(ctx);
                if (result != null)
                    return result;
            }

            if (preferredStyle == VisualConstructStyle.Navigate)
                return new ContentResult { Content = Navigator.ViewRoute(type, null) };

            ModifiableEntity entity = Constructor.Construct(type);
            return EncapsulateView(controller, entity, prefix, preferredStyle); 
        }

        private ViewResultBase EncapsulateView(ControllerBase controller, ModifiableEntity entity, string prefix, VisualConstructStyle preferredStyle)
        {
            IdentifiableEntity ident = entity as IdentifiableEntity;

            if (ident == null)
                throw new InvalidOperationException(Resources.VisualConstructorDoesnTWorkWithEmbeddedEntities); 

            AddFilterProperties(entity, controller);

            switch (preferredStyle)
            {
                case VisualConstructStyle.PopupView:
                    return Navigator.PopupView(controller, ident, prefix);
                case VisualConstructStyle.PartialView:
                    return Navigator.PartialView(controller, ident, prefix);
                case VisualConstructStyle.View:
                    return Navigator.View(controller, ident); 
                default:
                    throw new InvalidOperationException();
            }
        }

        public static void AddFilterProperties(ModifiableEntity obj, ControllerBase controller)
        {
            if (obj == null)
                throw new ArgumentNullException("result");

            HttpContextBase httpContext = controller.ControllerContext.HttpContext;

            if (!httpContext.Request.Params.AllKeys.Contains("sfQueryUrlName"))
                return;

            Type type = obj.GetType();

            object queryName = Navigator.ResolveQueryFromUrlName(httpContext.Request.Params["sfQueryUrlName"]);

            var filters = FindOptionsModelBinder.ExtractFilterOptions(httpContext, queryName)
                .Where(fo => fo.Operation == FilterOperation.EqualTo);

            var pairs = from pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        join fo in filters on pi.Name equals fo.Token.Key
                        //where CanConvert(fo.Value, pi.PropertyType) && fo.Value != null
                        where fo.Value != null
                        select new { pi, fo };

            foreach (var p in pairs)
                p.pi.SetValue(obj, Convert(p.fo.Value, p.pi.PropertyType), null);
        }

        public static object Convert(object obj, Type type)
        {
            if (obj == null) return null;

            Type objType = obj.GetType();

            if (type.IsAssignableFrom(objType))
                return obj;

            if (typeof(Lite).IsAssignableFrom(objType) && type.IsAssignableFrom(((Lite)obj).RuntimeType))
            {
                Lite lite = (Lite)obj;
                return lite.UntypedEntityOrNull ?? Database.RetrieveAndForget(lite);
            }

            if (typeof(Lite).IsAssignableFrom(type))
            {
                Type liteType = Reflector.ExtractLite(type);

                if (typeof(Lite).IsAssignableFrom(objType))
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

                else if (liteType.IsAssignableFrom(objType))
                {
                    return Lite.Create(liteType, (IdentifiableEntity)obj);
                }
            }

            throw new InvalidCastException(Properties.Resources.ImposibleConvertObject0From1To2.Formato(obj, objType, type));
        }
    }

    public enum VisualConstructStyle
    {
        PopupView, 
        PartialView,
        View,
        Navigate
    }
}
