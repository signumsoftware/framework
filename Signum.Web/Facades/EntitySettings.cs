using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Engine;
using System.Web.Mvc;
using Signum.Engine.Maps;
using Signum.Entities.Reflection;
using Signum.Utilities;

namespace Signum.Web
{
    public abstract class EntitySettings
    {
        public virtual string WebTypeName { get; set; }
        public Func<UrlHelper, Type, int?, string> ViewRoute { get; set; }

        public abstract Type StaticType { get; }
     
        public abstract Delegate UntypedMappingDefault { get; }
        public abstract Delegate UntypedMappingAdmin { get; }

        public abstract bool OnIsReadOnly(ModifiableEntity entity, EntitySettingsContext ctx);
        public abstract bool OnIsViewable(ModifiableEntity entity, EntitySettingsContext ctx);
        public abstract bool OnIsCreable(EntitySettingsContext ctx);
        public abstract bool OnShowSave();

        public abstract string OnPartialViewName(ModifiableEntity entity);
    }

    public class EntitySettings<T> : EntitySettings where T : IdentifiableEntity
    {
        public override string WebTypeName
        {
            get
            {
                return TypeLogic.GetCleanName(typeof(T));
            }
            set
            {
                throw new InvalidOperationException();
            }
        }

        public override Type StaticType
        {
            get { return typeof(T); }
        }

        public Mapping<T> MappingDefault { get; set; }
        public Mapping<T> MappingAdmin { get; set; }

        public override Delegate UntypedMappingDefault { get { return MappingDefault; } }
        public override Delegate UntypedMappingAdmin { get { return MappingAdmin; } }

        public override bool OnIsReadOnly(ModifiableEntity entity, EntitySettingsContext ctx)
        {
            if (IsReadOnly != null)
                foreach (Func<T, EntitySettingsContext, bool> item in IsReadOnly.GetInvocationList())
                {
                    if (item((T)entity, ctx))
                        return true;
                }

            return false;
        }

        public override bool OnIsViewable(ModifiableEntity entity, EntitySettingsContext ctx)
        {
            if (PartialViewName == null)
                return false;

            if (IsViewable != null)
                foreach (Func<T, EntitySettingsContext, bool> item in IsViewable.GetInvocationList())
                {
                    if (!item((T)entity, ctx))
                        return false;
                }

            return true;
        }

        public override bool OnIsCreable(EntitySettingsContext ctx)
        {
            if (IsCreable != null)
                foreach (Func<EntitySettingsContext, bool> item in IsCreable.GetInvocationList())
                {
                    if (!item(ctx))
                        return false;
                }

            return true;
        }

        public override bool OnShowSave()
        {
            return ShowSave;
        }

        public Func<EntitySettingsContext, bool> IsCreable { get; set; }
        public Func<T, EntitySettingsContext, bool> IsReadOnly { get; set; }
        public Func<T, EntitySettingsContext, bool> IsViewable { get; set; }      

        public bool ShowSave { get; set; }
   
        public Func<T, string> PartialViewName { get; set; }

        public override string OnPartialViewName(ModifiableEntity entity)
        {
            return PartialViewName((T)entity);
        }
        
        public EntitySettings(EntityType entityType)
        {
            switch (entityType)
            {
                case EntityType.Default:
                case EntityType.DefaultNotSaving:
                    ShowSave = entityType == EntityType.Default;
                    MappingAdmin = MappingDefault = new EntityMapping<T>(true).GetValue;
                    break;
                case EntityType.Admin:
                case EntityType.AdminNotSaving:
                    ShowSave =  entityType == EntityType.Admin;
                    IsCreable = ctx => ctx == EntitySettingsContext.Admin;
                    IsViewable = (_, ctx) => ctx == EntitySettingsContext.Admin;
                    MappingAdmin = new EntityMapping<T>(true).GetValue;
                    MappingDefault = new EntityMapping<T>(false).GetValue;
                    break;
                case EntityType.ServerOnly:
                    IsReadOnly = (_, ctx) => true;
                    IsCreable = ctx => false;
                    MappingAdmin = MappingDefault = new EntityMapping<T>(false).GetValue;
                    break;
                case EntityType.Content:
                    ShowSave = false;
                    IsCreable = ctx =>  ctx == EntitySettingsContext.Content;
                    IsViewable = (_, ctx) => ctx == EntitySettingsContext.Content;
                    MappingAdmin = null;
                    MappingDefault = new EntityMapping<T>(true).GetValue;
                    break;
                default:
                    break;
            }
        }
    }

    public enum EntitySettingsContext
    {
        Admin,
        Default,
        Content,
    }

    public class EmbeddedEntitySettings<T> : EntitySettings, IImplementationsFinder where T : EmbeddedEntity
    {
        public override string WebTypeName { get; set; }
        
        public override Type StaticType
        {
            get { return typeof(T); }
        }

        public Mapping<T> MappingDefault { get; set; }
        public override Delegate UntypedMappingDefault { get { return MappingDefault; } }
        public override Delegate UntypedMappingAdmin { get { return MappingDefault; } }

        public override bool OnIsReadOnly(ModifiableEntity entity, EntitySettingsContext ctx)
        {
            if (IsReadOnly != null)
                foreach (Func<T, bool> item in IsReadOnly.GetInvocationList())
                {
                    if (item((T)entity))
                        return true;
                }

            return false;
        }

        public override bool OnIsViewable(ModifiableEntity entity, EntitySettingsContext ctx)
        {
            if (PartialViewName == null)
                return false;

            if (IsViewable != null)
                foreach (Func<T, bool> item in IsViewable.GetInvocationList())
                {
                    if (!item((T)entity))
                        return false;
                }

            return true;
        }

        public override bool OnIsCreable(EntitySettingsContext ctx)
        {
            if (IsCreable != null)
                foreach (Func<bool> item in IsCreable.GetInvocationList())
                {
                    if (!item())
                        return false;
                }

            return true;
        }

        public override bool OnShowSave()
        {
            return ShowSave;
        }

        public Func<bool> IsCreable { get; set; }
        public Func<T, bool> IsReadOnly { get; set; }
        public Func<T, bool> IsViewable { get; set; }
        public Func<T, bool> IsNavigable{ get; set; }      

        public bool ShowSave { get; set; }
   
        public Func<T, string> PartialViewName { get; set; }

        public override string OnPartialViewName(ModifiableEntity entity)
        {
            return PartialViewName((T)entity);
        }
        
        public EmbeddedEntitySettings()
        {
            ShowSave = true;
            MappingDefault = new EntityMapping<T>(true).GetValue;
            WebTypeName = typeof(T).Name;
        }

        public Dictionary<PropertyRoute, Implementations> OverrideImplementations { get; set; }

        public Implementations FindImplementations(PropertyRoute route)
        {
            if (!typeof(ModelEntity).IsAssignableFrom(route.RootType))
                throw new InvalidOperationException("Route out");

            if (OverrideImplementations != null && OverrideImplementations.ContainsKey(route))
                return OverrideImplementations[route];

            if (route.PropertyRouteType == PropertyRouteType.MListItems || route.PropertyRouteType == PropertyRouteType.LiteEntity)
                return SchemaSettings.ToImplementations(route.Parent, route.Type.CleanType(), route.Parent.FieldInfo.GetCustomAttributes(true).Cast<Attribute>().ToArray());


            return SchemaSettings.ToImplementations(route, route.Type.CleanType(), route.FieldInfo.GetCustomAttributes(true).Cast<Attribute>().ToArray());
        }
    }

    public enum WindowType
    {
        View,
        Find,
        Admin
    }

    public enum EntityType
    {
        Admin,
        Default,
        AdminNotSaving,
        DefaultNotSaving,
        ServerOnly,
        Content,
    }
}
