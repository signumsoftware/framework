#region usings
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
#endregion

namespace Signum.Web.Controllers
{
    public class NavigatorController : Controller
    {
        [ValidateInput(false), ActionSplitter("webTypeName")]  //this is needed since a return content(View...) from an action that doesn't validate will throw here an exception. We suppose that validation has already been performed before getting here
        public ViewResult View(string webTypeName, int id)
        {
            Type t = Navigator.ResolveType(webTypeName);

            Lite<IdentifiableEntity> lite = Lite.Create(t, id);

            using (Navigator.Manager.OnRetrievingForView(lite))
            {
                return Navigator.NormalPage(this, Database.Retrieve(lite));
            }
        }

        [ActionSplitter("webTypeName")]
        public ActionResult Create(string webTypeName)
        {
            Type type = Navigator.ResolveType(webTypeName);

            if (!type.IsIdentifiableEntity())
                throw new InvalidOperationException("Only classes that inherit from IdentifiableEntity can be created using this Action"); 

            var entity = (IdentifiableEntity)new ConstructorContext(this).ConstructUntyped(type);

            return this.NormalPage(new NavigateOptions(entity));
        }

        [ActionSplitter("entityType")]
        public PartialViewResult PopupNavigate(string entityType, int? id, string prefix, string partialViewName, bool? readOnly, bool? showOperations, bool? saveProtected)
        {
            Type type = Navigator.ResolveType(entityType);

            IdentifiableEntity entity = null;
            if (id.HasValue)
            {
                Lite<IdentifiableEntity> lite = Lite.Create(type, id.Value);
                using (Navigator.Manager.OnRetrievingForView(lite))
                {
                    entity = Database.Retrieve(lite);
                }
            }
            else
                entity = (IdentifiableEntity)new ConstructorContext(this).ConstructUntyped(type);

            TypeContext tc = TypeContextUtilities.UntypedNew(entity, prefix);

            return this.PopupOpen(new PopupNavigateOptions(tc)
            {
                PartialViewName = partialViewName,
                ReadOnly = readOnly,
                ShowOperations = showOperations ?? true
            });
        }

        [ActionSplitter("entityType")]
        public PartialViewResult PopupView(string entityType, int? id, string prefix, string partialViewName, bool? readOnly, bool? showOperations, bool? saveProtected)
        {
            Type type = Navigator.ResolveType(entityType);

            IdentifiableEntity entity = null;
            if (id.HasValue)
            {
                 Lite<IdentifiableEntity> lite = Lite.Create(type, id.Value);
                 using (Navigator.Manager.OnRetrievingForView(lite))
                 {
                     entity = Database.Retrieve(lite);
                 }
            }
            else
                entity = (IdentifiableEntity)new ConstructorContext(this).ConstructUntyped(type);
        
            TypeContext tc = TypeContextUtilities.UntypedNew((IdentifiableEntity)entity, prefix);

            return this.PopupOpen(new PopupViewOptions(tc)
            {
                PartialViewName = partialViewName,
                ReadOnly = readOnly,
                ShowOperations = showOperations ?? true,
                SaveProtected = saveProtected
            });
        }

        [HttpPost, ActionSplitter("entityType")]
        public PartialViewResult PartialView(string entityType, int? id, string prefix, string partialViewName, bool? readOnly)
        {
            Type type = Navigator.ResolveType(entityType);

            IdentifiableEntity entity = null;
            if (id.HasValue)
            {    
                 Lite<IdentifiableEntity> lite = Lite.Create(type, id.Value);
                 using (Navigator.Manager.OnRetrievingForView(lite))
                 {
                     entity = Database.Retrieve(lite);
                 }
            }
            else
                entity = (IdentifiableEntity)new ConstructorContext(this).ConstructUntyped(type);

            TypeContext tc = TypeContextUtilities.UntypedNew((IdentifiableEntity)entity, prefix);

            if (readOnly == true)
                tc.ReadOnly = true;

            return Navigator.PartialView(this, tc, partialViewName);
        }

        [HttpPost, ActionSplitter("entityType")]
        public PartialViewResult NormalControl(string entityType, int id, bool? readOnly, string partialViewName)
        {
            Type type = Navigator.ResolveType(entityType);
            Lite<IdentifiableEntity> lite = Lite.Create(type, id);
            IdentifiableEntity entity;
            using (Navigator.Manager.OnRetrievingForView(lite))
            {
                entity = Database.Retrieve(lite);
            }

            return Navigator.NormalControl(this, new NavigateOptions(entity) { ReadOnly = readOnly, PartialViewName = partialViewName });
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
            IdentifiableEntity ident = entity as IdentifiableEntity;

            if (ident == null)
                throw new InvalidOperationException("Visual Constructor doesn't work with EmbeddedEntities");


            switch (preferredStyle)
            {
                case VisualConstructStyle.PopupView:
                    var viewOptions = new PopupViewOptions(TypeContextUtilities.UntypedNew(ident, prefix))
                    {
                        PartialViewName = partialViewName,
                        ReadOnly = readOnly,
                        SaveProtected = saveProtected,
                        ShowOperations = showOperations
                    };
                    return Navigator.PopupOpen(controller, viewOptions);
                case VisualConstructStyle.PopupNavigate:
                    var navigateOptions = new PopupNavigateOptions(TypeContextUtilities.UntypedNew(ident, prefix))
                    {
                        PartialViewName = partialViewName,
                        ReadOnly = readOnly,
                        ShowOperations = showOperations
                    };
                    return Navigator.PopupOpen(controller, navigateOptions);
                case VisualConstructStyle.PartialView:
                    return Navigator.PartialView(controller, ident, prefix, partialViewName);
                case VisualConstructStyle.View:
                    return Navigator.NormalPage(controller, new NavigateOptions(ident) { PartialViewName = partialViewName });
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
