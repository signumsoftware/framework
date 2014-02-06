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

namespace Signum.Web
{
    public static class Constructor
    {
        public static ConstructorManager ConstructorManager;

        public static void Start(ConstructorManager constructorManager)
        {
            ConstructorManager = constructorManager;
        }

        public static ModifiableEntity Construct(Type type)
        {
            return ConstructorManager.Construct(type);
        }

        public static T Construct<T>() where T : ModifiableEntity
        {
            return (T)ConstructorManager.Construct(typeof(T));
        }

        public static ActionResult VisualConstruct(ControllerBase controller, Type type, string prefix, VisualConstructStyle preferredStyle, string partialViewName)
        {
            return ConstructorManager.VisualConstruct(controller, type, prefix, preferredStyle, partialViewName);
        }

        public static void AddConstructor<T>(Func<T> constructor) where T:ModifiableEntity
        {
            ConstructorManager.Constructors.Add(typeof(T), constructor);
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
        public Dictionary<Type, Func<ModifiableEntity>> Constructors = new Dictionary<Type, Func<ModifiableEntity>>();

        public event Func<ConstructContext, ActionResult> VisualGeneralConstructor;
        public Dictionary<Type, Func<ConstructContext, ActionResult>> VisualConstructors = new Dictionary<Type, Func<ConstructContext, ActionResult>>();

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
            return (ModifiableEntity)Activator.CreateInstance(type, true);
        }

        public virtual ActionResult VisualConstruct(ControllerBase controller, Type type, string prefix, VisualConstructStyle preferredStyle, string partialViewName)
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
                return JsonAction.RedirectAjax(Navigator.NavigateRoute(type, null));

            ModifiableEntity entity = Constructor.Construct(type);
            return EncapsulateView(controller, entity, prefix, preferredStyle, partialViewName); 
        }

        private ViewResultBase EncapsulateView(ControllerBase controller, ModifiableEntity entity, string prefix, VisualConstructStyle preferredStyle, string partialViewName)
        {
            IdentifiableEntity ident = entity as IdentifiableEntity;

            if (ident == null)
                throw new InvalidOperationException("Visual Constructor doesn't work with EmbeddedEntities");

            AddFilterProperties(entity, controller);

            switch (preferredStyle)
            {
                case VisualConstructStyle.PopupView:
                    return Navigator.PopupOpen(controller, new PopupViewOptions(TypeContextUtilities.UntypedNew(ident, prefix)) { PartialViewName = partialViewName });
                case VisualConstructStyle.PopupNavigate:
                    return Navigator.PopupOpen(controller, new PopupNavigateOptions(TypeContextUtilities.UntypedNew(ident, prefix)) { PartialViewName = partialViewName });
                case VisualConstructStyle.PartialView:
                    return Navigator.PartialView(controller, ident, prefix, partialViewName);
                case VisualConstructStyle.View:
                    return Navigator.NormalPage(controller, new NavigateOptions(ident) { PartialViewName = partialViewName });
                default:
                    throw new InvalidOperationException();
            }
        }

        public static void AddFilterProperties(ModifiableEntity obj, ControllerBase controller)
        {
            if (obj == null)
                throw new ArgumentNullException("result");

            HttpContextBase httpContext = controller.ControllerContext.HttpContext;

            if (!httpContext.Request.Params.AllKeys.Contains("webQueryName"))
                return;

            Type type = obj.GetType();

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
                p.pi.SetValue(obj, Common.Convert(p.fo.Value, p.pi.PropertyType), null);
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
