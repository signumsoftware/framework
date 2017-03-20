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
using Signum.Windows.Operations;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Basics;

namespace Signum.Windows
{
    public abstract class EntitySettings
    {
        public abstract Type StaticType { get; }

        public abstract Control CreateView(ModifiableEntity entity, PropertyRoute typeContext);
        public abstract Control OnOverrideView(ModifiableEntity entity, Control control);

        public DataTemplate DataTemplate { get; set; }
        public ImageSource Icon { get; set; }

        public abstract Implementations FindImplementations(PropertyRoute route);

        internal abstract bool OnIsCreable(bool isSearchEntity);
        internal abstract bool OnIsViewable();
        public abstract bool OnIsNavigable(bool isSearchEntity);
        internal abstract bool OnIsReadonly();

        internal abstract bool HasView();
    }

    public class EntitySettings<T> : EntitySettings where T:Entity
    {
        public override Type StaticType
        {
            get { return typeof(T); }
        }

        public Func<T, Control> View { get; set; }
        public event Func<T, Control, Control> OverrideView;

        public EntityWhen IsCreable { get; set; }
        public bool IsViewable { get; set; }
        public EntityWhen IsNavigable { get; set; }
        public bool IsReadOnly { get; set; }

        public EntitySettings()
        {
            EntityKind entityKind = EntityKindCache.GetEntityKind(typeof(T));

            switch (entityKind)
            {
                case EntityKind.SystemString:
                    IsCreable = EntityWhen.Never;
                    IsViewable = false;
                    IsNavigable = EntityWhen.Never;
                    IsReadOnly = true;
                    break;
                case EntityKind.System:
                    IsCreable = EntityWhen.Never;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    IsReadOnly = true;
                    break;
                case EntityKind.Relational:
                    IsCreable = EntityWhen.Never;
                    IsViewable = false;
                    IsNavigable = EntityWhen.Never;
                    IsReadOnly = true;
                    break;

                case EntityKind.String:
                    IsCreable = EntityWhen.IsSearch;
                    IsViewable = false;
                    IsNavigable = EntityWhen.IsSearch;
                    break;
                case EntityKind.Shared:
                    IsCreable = EntityWhen.Always;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    break;
                case EntityKind.Main:
                    IsCreable = EntityWhen.IsSearch;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    break;

                case EntityKind.Part:
                    IsCreable = EntityWhen.IsLine;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    break;
                case EntityKind.SharedPart:
                    IsCreable = EntityWhen.IsLine;
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

        public override Control OnOverrideView(ModifiableEntity e, Control control)
        {
            foreach (var f in OverrideView.GetInvocationListTyped())
            {
                control = f((T)e, control);
            }
            return control;
        }

        public override Implementations FindImplementations(PropertyRoute route)
        {
            throw new InvalidOperationException("Call Server.FindImplementations for IdentifiableEntities");
        }

        internal override bool OnIsCreable(bool isSearch)
        {
            return IsCreable.HasFlag(isSearch ? EntityWhen.IsSearch : EntityWhen.IsLine);
        }

        internal override bool OnIsViewable()
        {
            return IsViewable;
        }

        public override bool OnIsNavigable(bool isSearch)
        {
            return IsNavigable.HasFlag(isSearch ? EntityWhen.IsSearch : EntityWhen.IsLine);
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

    public class EmbeddedEntitySettings<T> : ModifiableEntitySettings<T> where T: EmbeddedEntity { }
    public class ModelEntitySettings<T> : ModifiableEntitySettings<T> where T: ModelEntity { }

    public abstract class ModifiableEntitySettings<T> : EntitySettings where T : ModifiableEntity
    {
        public override Type StaticType
        {
            get { return typeof(T); }
        }

        public Func<T, Control> View { get; set; }
        public event Func<T, Control, Control> OverrideView;

        public bool IsCreable { get; set; }
        public bool IsViewable { get; set; }
        public bool IsReadonly { get; set; }

        public ModifiableEntitySettings()
        {
            IsCreable = true;
            IsViewable = true;
        }

        public override Control CreateView(ModifiableEntity entity, PropertyRoute typeContext)
        {
            if (View == null)
                throw new InvalidOperationException("View not defined in EntitySettings");

            if (typeContext == null && !(entity is IRootEntity))
                throw new ArgumentException("An EmbeddedEntity neeed TypeContext");

            Control control = View((T)entity);

            Common.SetPropertyRoute(control, typeContext ?? PropertyRoute.Root(entity.GetType()));

            return control;
        }

        public override Control OnOverrideView(ModifiableEntity entity, Control control)
        {
            foreach (var f in OverrideView.GetInvocationListTyped())
            {
                control = f((T)entity, control);
            }
            return control;
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

        public override bool OnIsNavigable(bool isSearchEntity)
        {
            return false;
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
        IsSearch = 2,
        IsLine = 1,
        Never = 0,
    }
}
