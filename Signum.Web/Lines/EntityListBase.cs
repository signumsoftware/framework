#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web.Properties;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
#endregion

namespace Signum.Web
{
    public static class EntityListBaseKeys
    {
        public const string Index = "sfIndex";
    }

    public abstract class EntityListBase : EntityBase
    {
        public EntityListBase(Type type, object untypedValue, Context parent, string controlID, PropertyRoute propertyRoute)
            : base(type, untypedValue, parent, controlID, propertyRoute)
        {
        }

        protected override void SetReadOnly()
        {
            Parent.ReadOnly = true;
            Find = false;
            Create = false;
            Remove = false;
            //Implementations = null;
        }

        public Type ElementType
        {
            get { return Type.ElementType(); }
        }

        public WriteIndex WriteIndex = WriteIndex.ForSavedEntities;

        public bool ShouldWriteOldIndex(TypeContext tc)
        {
            if(WriteIndex == WriteIndex.Allways)
            return  true;

            if (WriteIndex == Web.WriteIndex.ForSavedEntities)
            {
                IdentifiableEntity ie = tc.Parent.FollowC(a => a.Parent).OfType<TypeContext>().Select(a=>a.UntypedValue).OfType<IdentifiableEntity>().First("Parent entity not found");
                return !ie.IsNew; 
            }

            return false; 
        }
    }

    public enum WriteIndex
    {
        ForSavedEntities,
        Allways,
        Never
    }
}
