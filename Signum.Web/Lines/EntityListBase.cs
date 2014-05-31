#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using Newtonsoft.Json.Linq;
#endregion

namespace Signum.Web
{
    public static class EntityListBaseKeys
    {
        public const string Indexes = "sfIndexes";
        public const string List = "sfList";
        public const string ListPresent = "sfListPresent";
    }

    public abstract class EntityListBase : EntityBase
    {
        public bool Reorder { get; set; }

        public int? MaxElements { get; set; }

        public EntityListBase(Type type, object untypedValue, Context parent, string prefix, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, prefix, propertyRoute)
        {
        }

        protected override Dictionary<string, object> OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            if(Create)
                result.Add("create", true);
            if (Remove)
                result.Add("remove", true);
            if (Find)
                result.Add("find", true);
            if (View)
                result.Add("view", true);
            if (Navigate)
                result.Add("navigate", true);
            if (Reorder)
                result.Add("reorder", true);
            if (MaxElements != null)
                result.Add("maxElements", MaxElements.Value);
            return result;
        }

        protected override PropertyRoute GetElementRoute()
        {
            return this.PropertyRoute.Add("Item");
        }

        protected override Type GetElementType()
        {
            return this.ElementType;
        }

        protected override void SetReadOnly()
        {
            Parent.ReadOnly = true;
            Find = false;
            Create = false;
            Remove = false;
            Reorder = false;
        }

        public Type ElementType
        {
            get { return Type.ElementType(); }
        }

        public WriteIndex WriteIndex = WriteIndex.ForSavedEntities;

        public bool ShouldWriteOldIndex(TypeContext tc)
        {
            if(WriteIndex == WriteIndex.Always)
            return  true;

            if (WriteIndex == Web.WriteIndex.ForSavedEntities)
            {
                IdentifiableEntity ie = tc.Parent.Follow(a => a.Parent).OfType<TypeContext>().Select(a => a.UntypedValue).OfType<IdentifiableEntity>().FirstEx(() => "Parent entity not found");
                return !ie.IsNew; 
            }

            return false; 
        }
    }

    public enum WriteIndex
    {
        ForSavedEntities,
        Always,
        Never
    }
}
