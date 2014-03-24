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
        public Func<UrlHelper, Type, int?, string> ViewRoute { get; set; }

        public abstract Type StaticType { get; }
     
        public abstract Delegate UntypedMappingLine { get; }
        public abstract Delegate UntypedMappingMain { get; }

        internal abstract bool OnIsCreable(bool isSearchEntity);
        internal abstract bool OnIsViewable(string partialViewName);
        internal abstract bool OnIsNavigable(string partialViewName, bool isSearchEntity);
        internal abstract bool OnIsReadonly();

        public ViewOverrides ViewOverrides { get; set; }

        public abstract string OnPartialViewName(ModifiableEntity entity);

        public bool AvoidValidateRequest { get; set; }
    }

    public class EntitySettings<T> : EntitySettings where T : IdentifiableEntity
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

        public override Delegate UntypedMappingLine { get { return MappingLine; } }
        public override Delegate UntypedMappingMain { get { return MappingMain; } }

        public Func<T, string> PartialViewName { get; set; }

        public override string OnPartialViewName(ModifiableEntity entity)
        {
            if (PartialViewName == null)
                throw new InvalidOperationException("PartialViewName not set for {0}".Formato(GetType().TypeName()));

            return PartialViewName((T)entity);
        }
        
        public EntitySettings()
        {
            switch (EntityKindCache.GetEntityKind(typeof(T)))
            {
                case EntityKind.SystemString:
                    IsCreable = EntityWhen.Never;
                    IsViewable = false;
                    IsNavigable = EntityWhen.Never;
                    IsReadonly = true;
                    MappingMain = MappingLine = new EntityMapping<T>(false).GetValue;
                    break;

                case EntityKind.System:
                    IsCreable = EntityWhen.Never;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    IsReadonly = true;
                    MappingMain = MappingLine = new EntityMapping<T>(false).GetValue;
                    break;

                case EntityKind.Relational:
                    IsCreable = EntityWhen.Never;
                    IsViewable = false;
                    IsNavigable = EntityWhen.Never;
                    IsReadonly = true;
                    MappingMain = MappingLine = new EntityMapping<T>(false).GetValue;
                    break;

                case EntityKind.String:
                    IsCreable = EntityWhen.IsSearchEntity;
                    IsViewable = false;
                    IsNavigable = EntityWhen.IsSearchEntity;
                    MappingMain = new EntityMapping<T>(true).GetValue;
                    MappingLine = new EntityMapping<T>(false).GetValue;
                    break;

                case EntityKind.Shared:
                    IsCreable = EntityWhen.Always;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    MappingMain = MappingLine = new EntityMapping<T>(true).GetValue;
                    break;

                case EntityKind.Main:
                    IsCreable = EntityWhen.IsSearchEntity;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    MappingMain = MappingLine = new EntityMapping<T>(true).GetValue;
                    break;

                case EntityKind.Part:
                    IsCreable = EntityWhen.IsLine;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    MappingMain = MappingLine = new EntityMapping<T>(true).GetValue;
                    break;

                case EntityKind.SharedPart:
                    IsCreable = EntityWhen.IsLine;
                    IsViewable = true;
                    IsNavigable = EntityWhen.Always;
                    MappingMain = MappingLine = new EntityMapping<T>(true).GetValue;
                    break;
             
                default:
                    break;
            }
        }

        internal override bool OnIsCreable(bool isSearchEntity)
        {
            return IsCreable.HasFlag(isSearchEntity ? EntityWhen.IsSearchEntity : EntityWhen.IsLine);
        }

        internal override bool OnIsViewable(string partialViewName)
        {
            if (partialViewName == null && PartialViewName == null)
                return false;

            return IsViewable;
        }

        internal override bool OnIsNavigable(string partialViewName, bool isSearchEntity)
        {
            if (partialViewName == null && PartialViewName == null)
                return false;

            return IsNavigable.HasFlag(isSearchEntity ? EntityWhen.IsSearchEntity : EntityWhen.IsLine);
        }

        internal override bool OnIsReadonly()
        {
            return IsReadonly;
        }
    }

    public class EmbeddedEntitySettings<T> : EntitySettings, IImplementationsFinder where T : EmbeddedEntity
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
                throw new InvalidOperationException("PartialViewName not set for {0}".Formato(GetType().TypeName()));

            return PartialViewName((T)entity);
        }
        
        public EmbeddedEntitySettings()
        {
            MappingDefault = new EntityMapping<T>(true).GetValue;
            WebTypeName = typeof(T).Name;
            IsViewable = true;
            IsCreable = true;
        }

        internal override bool OnIsCreable(bool isSearchEntity)
        {
            if (isSearchEntity)
                throw new InvalidOperationException("EmbeddedEntitySettigs are not compatible with isSearchEntity");

            return IsCreable;
        }

        internal override bool OnIsViewable(string partialViewName)
        {
            if (partialViewName == null && PartialViewName == null)
                return false;

            return IsViewable;
        }

        internal override bool OnIsNavigable(string partialViewName, bool isSearchEntity)
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
    }

    public enum EntityWhen
    {
        Always = 3,
        IsSearchEntity = 2,
        IsLine = 1,
        Never = 0,
    }
}
