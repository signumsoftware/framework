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
using Signum.Engine.Basics;
using Signum.Engine.Operations;
using Signum.Utilities.ExpressionTrees;
using Signum.Web.Operations;

namespace Signum.Web
{
    public abstract class EntitySettings
    {
        public virtual string WebTypeName { get; set; }
        public Func<UrlHelper, Type, PrimaryKey?, string> ViewRoute { get; set; }

        public bool AvoidPopup { get; set; }

        public abstract Type StaticType { get; }
     
        public abstract Delegate UntypedMappingLine { get; }
        public abstract Delegate UntypedMappingMain { get; }


        internal abstract bool OnIsCreable(bool isSearch);
        internal abstract bool OnIsFindable();
        internal abstract bool OnIsViewable(string partialViewName);
        internal abstract bool OnIsNavigable(string partialViewName, bool isSearch);
        internal abstract bool OnIsReadonly();

        public abstract IViewOverrides GetViewOverrides();

        public abstract string OnPartialViewName(ModifiableEntity entity);

        public bool AvoidValidateRequest { get; set; }
    }

    public class EntitySettings<T> : EntitySettings where T : Entity
    {
        public override string WebTypeName
        {
            get { return TypeLogic.GetCleanName(typeof(T)); }
            set { throw new InvalidOperationException(); }
        }

        public override Type StaticType
        {
            get { return typeof(T); }
        }

        public Mapping<T> MappingLine { get; set; }
        public Mapping<T> MappingMain { get; set; }

        public EntityWhen IsCreable { get; set; }
        public bool IsViewable { get; set; }
        public EntityWhen IsNavigable { get; set; }
        public bool IsReadonly { get; set; }
        public bool IsFindable { get; set; }

        public override Delegate UntypedMappingLine { get { return MappingLine; } }
        public override Delegate UntypedMappingMain { get { return MappingMain; } }

        public Func<T, string> PartialViewName { get; set; }

        public override string OnPartialViewName(ModifiableEntity entity)
        {
            if (PartialViewName == null)
                throw new InvalidOperationException("PartialViewName not set for {0}".FormatWith(GetType().TypeName()));

            return PartialViewName((T)entity);
        }
        
        public EntitySettings()
        {
            switch (EntityKindCache.GetEntityKind(typeof(T)))
            {
                case EntityKind.SystemString:
                    IsCreable = EntityWhen.Never;
                    IsFindable = true;
                    IsViewable = false;
                    IsNavigable = EntityWhen.Never;
                    IsReadonly = true;
                    MappingMain = MappingLine = new EntityMapping<T>(false).GetValue;
                    break;

                case EntityKind.System:
                    IsCreable = EntityWhen.Never;
                    IsFindable = true;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    IsReadonly = true;
                    MappingMain = MappingLine = new EntityMapping<T>(false).GetValue;
                    break;

                case EntityKind.Relational:
                    IsCreable = EntityWhen.Never;
                    IsFindable = false;
                    IsViewable = false;
                    IsNavigable = EntityWhen.Never;
                    IsReadonly = true;
                    MappingMain = MappingLine = new EntityMapping<T>(false).GetValue;
                    break;

                case EntityKind.String:
                    IsCreable = EntityWhen.IsSearch;
                    IsFindable = true;
                    IsViewable = false;
                    IsNavigable = EntityWhen.IsSearch;
                    MappingMain = new EntityMapping<T>(true).GetValue;
                    MappingLine = new EntityMapping<T>(false).GetValue;
                    break;

                case EntityKind.Shared:
                    IsCreable = EntityWhen.Always;
                    IsFindable = true;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    MappingMain = MappingLine = new EntityMapping<T>(true).GetValue;
                    break;

                case EntityKind.Main:
                    IsCreable = EntityWhen.IsSearch;
                    IsFindable = true;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    MappingMain = new EntityMapping<T>(true).GetValue;
                    MappingLine = new EntityMapping<T>(false).GetValue;
                    break;

                case EntityKind.Part:
                    IsCreable = EntityWhen.IsLine;
                    IsFindable = false;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    MappingMain = MappingLine = new EntityMapping<T>(true).GetValue;
                    break;

                case EntityKind.SharedPart:
                    IsCreable = EntityWhen.IsLine;
                    IsFindable = true;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    MappingMain = MappingLine = new EntityMapping<T>(true).GetValue;
                    break;
             
                default:
                    break;
            }
        }

        internal override bool OnIsCreable(bool isSearch)
        {
            return IsCreable.HasFlag(isSearch ? EntityWhen.IsSearch : EntityWhen.IsLine);
        }

        internal override bool OnIsFindable()
        {
            return IsFindable;
        }

        internal override bool OnIsViewable(string partialViewName)
        {
            if (partialViewName == null && PartialViewName == null)
                return false;

            return IsViewable;
        }

        internal override bool OnIsNavigable(string partialViewName, bool isSearch)
        {
            if (partialViewName == null && PartialViewName == null)
                return false;

            return IsNavigable.HasFlag(isSearch ? EntityWhen.IsSearch : EntityWhen.IsLine);
        }

        internal override bool OnIsReadonly()
        {
            return IsReadonly;
        }


        ViewOverrides<T> viewOverride;

        public ViewOverrides<T> CreateViewOverrides()
        {
            return viewOverride ?? (viewOverride = new ViewOverrides<T>());
        }

        public override IViewOverrides GetViewOverrides()
        {
            return viewOverride;
        }
    }

    public class EmbeddedEntitySettings<T> : ModifiableEntitySettings<T> where T : EmbeddedEntity { }
    public class ModelEntitySettings<T> : ModifiableEntitySettings<T> where T : ModelEntity { }
    
    public abstract class ModifiableEntitySettings<T> : EntitySettings, IImplementationsFinder where T : ModifiableEntity
    {
        public override string WebTypeName { get; set; }
        
        public override Type StaticType
        {
            get { return typeof(T); }
        }

        public Mapping<T> MappingDefault { get; set; }
        public override Delegate UntypedMappingLine { get { return MappingDefault; } }
        public override Delegate UntypedMappingMain { get { return MappingDefault; } }


        public bool IsCreable { get; set; }
        public bool IsViewable { get; set; }
        public bool IsReadonly { get; set; }  

        public Func<T, string> PartialViewName { get; set; }

        public override string OnPartialViewName(ModifiableEntity entity)
        {
            if (PartialViewName == null)
                throw new InvalidOperationException("PartialViewName not set for {0}".FormatWith(GetType().TypeName()));

            return PartialViewName((T)entity);
        }
        
        public ModifiableEntitySettings()
        {
            MappingDefault = new EntityMapping<T>(true).GetValue;
            WebTypeName = typeof(T).Name;
            IsViewable = true;
            IsCreable = true;
        }

        internal override bool OnIsCreable(bool isSearch)
        {
            if (isSearch)
                throw new InvalidOperationException("EmbeddedEntitySettigs are not compatible with isSearch");

            return IsCreable;
        }

        internal override bool OnIsFindable()
        {
            return false;
        }

        internal override bool OnIsViewable(string partialViewName)
        {
            if (partialViewName == null && PartialViewName == null)
                return false;

            return IsViewable;
        }

        internal override bool OnIsNavigable(string partialViewName, bool isSearch)
        {
            return false;
        }

        internal override bool OnIsReadonly()
        {
            return IsReadonly;
        }

        public Dictionary<PropertyRoute, Implementations> OverrideImplementations { get; set; }

        public Implementations FindImplementations(PropertyRoute route)
        {
            if (OverrideImplementations != null && OverrideImplementations.ContainsKey(route))
                return OverrideImplementations[route];

            return ModelEntity.GetImplementations(route); 
        }

        public override IViewOverrides GetViewOverrides()
        {
 	        return null; //not implemented
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
