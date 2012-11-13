using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Data;
using System.Collections;
using Signum.Utilities.DataStructures;
using Signum.Entities;

namespace Signum.Windows
{
    public abstract class EntitySettings
    {
        public abstract Type StaticType { get; }

        public abstract Control CreateView(ModifiableEntity entity, PropertyRoute typeContext);

        public DataTemplate DataTemplate { get; set; }
        public ImageSource Icon { get; set; }

        public abstract Implementations FindImplementations(PropertyRoute route);

        internal abstract bool OnIsCreable(bool isSearchEntity);
        internal abstract bool OnIsViewable();
        internal abstract bool OnIsNavigable(bool isSearchEntity);
        internal abstract bool OnIsReadonly();

        internal abstract bool HasView();
    }

    public class EntitySettings<T> : EntitySettings where T:IdentifiableEntity
    {
        public override Type StaticType
        {
            get { return typeof(T); }
        }

        public Func<T, Control> View { get; set; }

        public EntityWhen IsCreable { get; set; }
        public bool IsViewable { get; set; }
        public EntityWhen IsNavigable { get; set; }
        public bool IsReadOnly { get; set; }

        public EntitySettings(EntityType entityType)
        {
            switch (entityType)
            {
                case EntityType.SystemString:
                    IsCreable = EntityWhen.Never;
                    IsViewable = false;
                    IsNavigable = EntityWhen.Never;
                    IsReadOnly = true;
                    break;
                case EntityType.System:
                    IsCreable = EntityWhen.Never;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    IsReadOnly = true;
                    break;
                case EntityType.String:
                    IsCreable = EntityWhen.IsSearchEntity;
                    IsViewable = false;
                    IsNavigable = EntityWhen.IsSearchEntity;
                    break;
                case EntityType.Part:
                    IsCreable = EntityWhen.IsLine;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    break;
                case EntityType.Shared:
                    IsCreable = EntityWhen.Always;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    break;
                case EntityType.Main:
                    IsCreable = EntityWhen.IsSearchEntity;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    break;
                default:
                    break;
            }
        }

        public override Control CreateView(ModifiableEntity entity, PropertyRoute typeContext)
        {
            if (View == null)
                throw new InvalidOperationException("View not defined in EntitySettings");

            return View((T)entity);
        }

        public void OverrideView(Func<IdentifiableEntity, Control, Control> overrideView)
        {
            var view = View;
            View = e => overrideView(e, view(e));
        }

        public override Implementations FindImplementations(PropertyRoute route)
        {
            throw new InvalidOperationException("Call Server.FindImplementations for IdentifiableEntities");
        }

        internal override bool OnIsCreable(bool isSearchEntity)
        {
            return IsCreable.HasFlag(isSearchEntity ? EntityWhen.IsSearchEntity : EntityWhen.IsLine);
        }

        internal override bool OnIsViewable()
        {
            return IsViewable;
        }

        internal override bool OnIsNavigable(bool isSearchEntity)
        {
            return IsNavigable.HasFlag(isSearchEntity ? EntityWhen.IsSearchEntity : EntityWhen.IsLine);
        }

        internal override bool OnIsReadonly()
        {
            return IsReadOnly;
        }

        internal override bool HasView()
        {
            return View != null;
        }
    }

    public enum EntityType
    {
        SystemString,
        System,
        String,
        Part,
        Shared,
        Main,
    }

 

    public class EmbeddedEntitySettings<T> : EntitySettings where T : EmbeddedEntity
    {
        public override Type StaticType
        {
            get { return typeof(T); }
        }

        public Func<T, PropertyRoute, Control> View { get; set; }

        public bool IsCreable { get; set; }
        public bool IsViewable { get; set; }
        public bool IsReadonly { get; set; }

        public override Control CreateView(ModifiableEntity entity, PropertyRoute typeContext)
        {
            if (View == null)
                throw new InvalidOperationException("View not defined in EntitySettings");

            if (typeContext == null && !(entity is IRootEntity))
                throw new ArgumentException("An EmbeddedEntity neeed TypeContext");

            return View((T)entity, typeContext ?? PropertyRoute.Root(entity.GetType()));
        }

        public void OverrideView(Func<EmbeddedEntity, PropertyRoute, Control, Control> overrideView)
        {
            var viewEmbedded = View;
            View = (e, tc) => overrideView(e, tc, viewEmbedded(e, tc));
        }

        internal override bool OnIsCreable(bool isSearchEntity)
        {
            if (isSearchEntity)
                throw new InvalidOperationException("EmbeddedEntitySettigs are not compatible wirh isSearchEntity");

            return IsCreable;
        }

        internal override bool OnIsViewable()
        {
            return View != null && IsViewable;
        }

        internal override bool OnIsNavigable(bool isSearchEntity)
        {
            throw new InvalidOperationException("EmbeddedEntitySettigs are not compatible wirh isSearchEntity");
        }

        internal override bool OnIsReadonly()
        {
            return IsReadonly; 
        }

        public Dictionary<PropertyRoute, Implementations> OverrideImplementations { get; set; }

        public override Implementations FindImplementations(PropertyRoute route)
        {
            if (OverrideImplementations != null && OverrideImplementations.ContainsKey(route))
                return OverrideImplementations[route];

            return ModelEntity.GetImplementations(route);
        }

        internal override bool HasView()
        {
            return View != null;
        }
    }

    public enum EntityWhen
    {
        Always = 3,
        IsSearchEntity = 2,
        IsLine = 1,
        Never = 0,
    }
}
