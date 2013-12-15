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

        public EntityListBase(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
        }

        protected override JsOptionsBuilder OptionsJSInternal()
        {
            var result = base.OptionsJSInternal();
            if(Create)
                result.Add("create", "true");
            if (Remove)
                result.Add("remove", "true");
            if (Find)
                result.Add("find", "true");
            if (View)
                result.Add("view", "true");
            if (Navigate)
                result.Add("navigate", "true");
            if (Reorder)
                result.Add("reorder", "true");
            return result;
        }

        protected override void SetReadOnly()
        {
            Parent.ReadOnly = true;
            Find = false;
            Create = false;
            Remove = false;
            Reorder = false;
        }

        public string MovingUp { get; set; }
        protected abstract string DefaultMoveUp();
        internal string GetMovingUp()
        {
            if (!Reorder)
                return "";
            return MovingUp ?? DefaultMoveUp();
        }

        public string MovingDown { get; set; }
        protected abstract string DefaultMoveDown();
        internal string GetMovingDown()
        {
            if (!Reorder)
                return "";
            return MovingDown ?? DefaultMoveDown();
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
                IdentifiableEntity ie = tc.Parent.FollowC(a => a.Parent).OfType<TypeContext>().Select(a => a.UntypedValue).OfType<IdentifiableEntity>().FirstEx(() => "Parent entity not found");
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
