using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Engine;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.Web.Script.Serialization;
using System.Text;
using System.IO;
using Signum.Engine.Basics;
using System.Globalization;
using Signum.Utilities.Reflection;
using Signum.Web.Operations;
using Signum.Engine.Operations;

namespace Signum.Web.Controllers
{
    public class NavigatorController : Controller
    {
        [ValidateInput(false), ActionSplitter("webTypeName")]  //this is needed since a return content(View...) from an action that doesn't validate will throw here an exception. We suppose that validation has already been performed before getting here
        public new ViewResult View(string webTypeName, string id)
        {
            Type t = Navigator.ResolveType(webTypeName);

            Lite<Entity> lite = Lite.Create(t, PrimaryKey.Parse(id, t));

            using (Navigator.Manager.OnRetrievingForView(lite))
            {
                return Navigator.NormalPage(this, Database.RetrieveAndForget(lite));
            }
        }

        [ActionSplitter("webTypeName")]
        public ActionResult Create(string webTypeName)
        {
            Type type = Navigator.ResolveType(webTypeName);

            if (!type.IsEntity())
                throw new InvalidOperationException("Only classes that inherit from Entity can be created using this Action"); 

            var entity = (Entity)new ConstructorContext(this, this.TryGetOperationInfo(type)).ConstructUntyped(type);

            return this.NormalPage(entity);
        }

        [ActionSplitter("entityType")]
        public PartialViewResult PopupNavigate(string entityType, string id, string prefix, string partialViewName, bool? readOnly, bool? showOperations, bool? saveProtected)
        {
            Type type = Navigator.ResolveType(entityType);

            Entity entity = null;
            if (id.HasText())
            {
                Lite<Entity> lite = Lite.Create(type, PrimaryKey.Parse(id, type));
                using (Navigator.Manager.OnRetrievingForView(lite))
                {
                    entity = Database.Retrieve(lite);
                }
            }
            else
                entity = (Entity)new ConstructorContext(this, this.TryGetOperationInfo(type)).ConstructUntyped(type);

            return this.PopupNavigate(entity, new PopupNavigateOptions(prefix)
            {
                PartialViewName = partialViewName,
                ReadOnly = readOnly,
                ShowOperations = showOperations ?? true
            });
        }

        [ActionSplitter("entityType")]
        public PartialViewResult PopupView(string entityType, string id, string prefix, string partialViewName, bool? readOnly, bool? showOperations, bool? saveProtected)
        {
            Type type = Navigator.ResolveType(entityType);

            Entity entity = null;
            if (id.HasText())
            {
                Lite<Entity> lite = Lite.Create(type, PrimaryKey.Parse(id, type));
                 using (Navigator.Manager.OnRetrievingForView(lite))
                 {
                     entity = Database.Retrieve(lite);
                 }
            }
            else
                entity = (Entity)new ConstructorContext(this, this.TryGetOperationInfo(type)).ConstructUntyped(type);

            return this.PopupView(entity, new PopupViewOptions(prefix)
            {
                PartialViewName = partialViewName,
                ReadOnly = readOnly,
                ShowOperations = showOperations ?? true,
                RequiresSaveOperation = saveProtected
            });
        }

        [HttpPost, ActionSplitter("entityType")]
        public PartialViewResult PartialView(string entityType, string id, string prefix, string partialViewName, bool? readOnly)
        {
            Type type = Navigator.ResolveType(entityType);

            Entity entity = null;
            if (id.HasText())
            {    
                 Lite<Entity> lite = Lite.Create(type, PrimaryKey.Parse(id, type));
                 using (Navigator.Manager.OnRetrievingForView(lite))
                 {
                     entity = Database.Retrieve(lite);
                 }
            }
            else
                entity = (Entity)new ConstructorContext(this, this.TryGetOperationInfo(type)).ConstructUntyped(type);

            TypeContext tc = TypeContextUtilities.UntypedNew((Entity)entity, prefix);

            if (readOnly == true)
                tc.ReadOnly = true;

            return Navigator.PartialView(this, tc, partialViewName);
        }

        [HttpPost, ActionSplitter("entityType")]
        public PartialViewResult NormalControl(string entityType, string id, bool? readOnly, string partialViewName)
        {
            Type type = Navigator.ResolveType(entityType);
            Lite<Entity> lite = Lite.Create(type, PrimaryKey.Parse(id, type));
            Entity entity;
            using (Navigator.Manager.OnRetrievingForView(lite))
            {
                entity = Database.Retrieve(lite);
            }

            return this.NormalControl(entity, new NavigateOptions { ReadOnly = readOnly, PartialViewName = partialViewName });
        }

        [HttpPost]
        public PartialViewResult ValueLineBox(string prefix, ValueLineType type, string title, string labelText, string message, string unit, string format)
        {
            ValueLineBoxOptions options = new ValueLineBoxOptions(type, prefix, null) 
            { 
                message = message,
                labelText = labelText, 
                title = title, 
                unit = unit, 
                format = format 
            };

            Type vlType = null;
            switch (type)
            {
                case ValueLineType.TextBox:
                case ValueLineType.TextArea:
                    vlType = typeof(string);
                    options.value = this.ParseValue<string>("value");
                    break;
                case ValueLineType.Boolean:
                    vlType = typeof(bool);
                    options.value = this.ParseValue<bool?>("value");
                    break;
                case ValueLineType.Number:
                    vlType = typeof(decimal);

                    options.value = format != null && format.StartsWith("p", StringComparison.InvariantCultureIgnoreCase) ?
                        this.ParsePercentage<decimal?>("value"):
                        this.ParseValue<decimal?>("value");
                    break;
                case ValueLineType.DateTime:
                    vlType = typeof(DateTime);
                    options.value = this.ParseValue<DateTime?>("value");
                    break;
                default:
                    break;
            }

            ViewData["type"] = vlType;
            ViewData["options"] = options;

            return this.PartialView(Navigator.Manager.ValueLineBoxView, new Context(null, prefix));
        }


        private ViewResultBase EncapsulateView(ControllerBase controller, ModifiableEntity entity, string prefix, VisualConstructStyle preferredStyle, string partialViewName, bool? readOnly, bool showOperations, bool? saveProtected)
        {
            Entity ident = entity as Entity;

            if (ident == null)
                throw new InvalidOperationException("Visual Constructor doesn't work with EmbeddedEntities");


            switch (preferredStyle)
            {
                case VisualConstructStyle.PopupView:
                    return controller.PopupView(ident, new PopupViewOptions(prefix)
                    {
                        PartialViewName = partialViewName,
                        ReadOnly = readOnly,
                        RequiresSaveOperation = saveProtected,
                        ShowOperations = showOperations
                    });
                case VisualConstructStyle.PopupNavigate:
                    return controller.PopupNavigate(ident, new PopupNavigateOptions(prefix)
                    {
                        PartialViewName = partialViewName,
                        ReadOnly = readOnly,
                        ShowOperations = showOperations
                    });
                case VisualConstructStyle.PartialView:
                    return controller.PartialView(ident, prefix, partialViewName);
                case VisualConstructStyle.View:
                    return controller.NormalPage(ident, new NavigateOptions { PartialViewName = partialViewName });
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
