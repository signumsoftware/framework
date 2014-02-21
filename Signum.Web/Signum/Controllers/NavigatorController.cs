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
#endregion

namespace Signum.Web.Controllers
{
    public class NavigatorController : Controller
    {
        [ValidateInput(false)]  //this is needed since a return content(View...) from an action that doesn't validate will throw here an exception. We suppose that validation has already been performed before getting here
        public ViewResult View(string webTypeName, int id)
        {
            Type t = Navigator.ResolveType(webTypeName);

            return Navigator.NormalPage(this, Database.Retrieve(t, id));
        }

        public ActionResult Create(string entityType, string prefix)
        {
            Type type = Navigator.ResolveType(entityType);

            return Constructor.VisualConstruct(this, type, prefix, VisualConstructStyle.Navigate, null);
        }

        public PartialViewResult PopupNavigate(string entityType, int? id, string prefix, string partialViewName)
        {
            Type type = Navigator.ResolveType(entityType);

            IdentifiableEntity entity = null;
            if (id.HasValue)
                entity = Database.Retrieve(type, id.Value);
            else
            {
                object result = Constructor.VisualConstruct(this, type, prefix, VisualConstructStyle.PopupNavigate, partialViewName);
                if (result.GetType() == typeof(PartialViewResult))
                    return (PartialViewResult)result;

                if (result.GetType().IsEmbeddedEntity())
                    throw new InvalidOperationException("PopupNavigate cannot be called for EmbeddedEntity {0}".Formato(result.GetType()));

                if (!typeof(IdentifiableEntity).IsAssignableFrom(result.GetType()))
                    throw new InvalidOperationException("Invalid result type for a Constructor");

                entity = (IdentifiableEntity)result;
            }

            TypeContext tc = TypeContextUtilities.UntypedNew(entity, prefix);
            return this.PopupOpen(new PopupNavigateOptions(tc) { PartialViewName = partialViewName });
        }

        public PartialViewResult PopupView(string entityType, int? id, string prefix, bool? readOnly, string partialViewName)
        {
            Type type = Navigator.ResolveType(entityType);

            IdentifiableEntity entity = null;
            if (id.HasValue)
                entity = Database.Retrieve(type, id.Value);
            else
            {
                ActionResult result = Constructor.VisualConstruct(this, type, prefix, VisualConstructStyle.PopupView, partialViewName);
                if (result is PartialViewResult)
                    return (PartialViewResult)result;
                else
                    throw new InvalidOperationException("Invalid result type for a Constructor");
            }

            TypeContext tc = TypeContextUtilities.UntypedNew((IdentifiableEntity)entity, prefix);
            return this.PopupOpen(new PopupViewOptions(tc) { PartialViewName = partialViewName, ReadOnly = readOnly.HasValue });
        }

        [HttpPost]
        public PartialViewResult PartialView(string entityType, int? id, string prefix, bool? readOnly, string partialViewName)
        {
            Type type = Navigator.ResolveType(entityType);

            IdentifiableEntity entity = null;
            if (id.HasValue)
                entity = Database.Retrieve(type, id.Value);
            else
            {
                object result = Constructor.VisualConstruct(this, type, prefix, VisualConstructStyle.PartialView, partialViewName);
                if (result is PartialViewResult)
                    return (PartialViewResult)result;
                else
                    throw new InvalidOperationException("Invalid result type for a Constructor");
            }

            TypeContext tc = TypeContextUtilities.UntypedNew((IdentifiableEntity)entity, prefix);

            if (readOnly == true)
                tc.ReadOnly = true;

            return Navigator.PartialView(this, tc, partialViewName);
        }

        [HttpPost]
        public PartialViewResult NormalControl(string entityType, int id, bool? readOnly, string partialViewName)
        {
            Type type = Navigator.ResolveType(entityType);

            IdentifiableEntity entity = Database.Retrieve(type, id);

            return Navigator.NormalControl(this, new NavigateOptions(entity) { ReadOnly = readOnly, PartialViewName = partialViewName });
        }

        [HttpPost]
        public PartialViewResult ValueLineBox(string prefix, ValueLineType type, string title, string labelText, string message)
        {
            ValueLineBoxOptions options = new ValueLineBoxOptions(type, prefix, null) { labelText = labelText, title = title };

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
                    options.value = this.ParseValue<decimal?>("value");
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
    }
}
