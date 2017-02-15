using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities
{
    /// <summary>
    /// When used on a static class, auto-initializes its static fields of symbols or operations 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class AutoInitAttribute : Attribute
    {
        public static Exception ArgumentNullException(Type argumentType, string argumentName)
        {
            return new ArgumentNullException(argumentName, $"The argument '{argumentName}' of type '{argumentType.TypeName()}' is null. Are you missing an [AutoInit] attribute?");
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class InTypeScriptAttribute : Attribute
    {
        bool? inTypeScript = null;
        public bool? GetInTypeScript() => inTypeScript;
        
        
        bool? undefined = null;
        public bool? GetUndefined() => undefined;
        public bool Undefined
        {
            get { return undefined ?? NotSet(); }
            set { undefined = value; }
        }

        bool NotSet()
        {
            throw new InvalidOperationException("Not Set");
        }

        bool? @null = null;
        public bool? GetNull() => @null;
        public bool Null
        {
            get { return @null ?? NotSet(); }
            set { @null = value; }
        }

        public InTypeScriptAttribute() { }
        public InTypeScriptAttribute(bool inTypeScript)
        {
            this.inTypeScript = inTypeScript;
        }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ImportInTypeScriptAttribute : Attribute
    {
        public Type Type { get; set; }
        public string ForNamesace { get; set; }
        public ImportInTypeScriptAttribute(Type type, string forNamespace)
        {
            this.Type = type;
            this.ForNamesace = forNamespace;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CleanTypeNameAttribute: Attribute
    {
        public string Name { get; private set; }
        public CleanTypeNameAttribute(string name)
        {
            this.Name = name; 
        }
    }

    public static class EntityKindCache
    {
        static ConcurrentDictionary<Type, EntityKindAttribute> dictionary = new ConcurrentDictionary<Type, EntityKindAttribute>();

        public static EntityKind GetEntityKind(Type type)
        {
            return GetAttribute(type).EntityKind;
        }

        public static EntityData GetEntityData(Type type)
        {
            return GetAttribute(type).EntityData;
        }

        public static bool RequiresSaveOperation(Type type)
        {
            return GetAttribute(type).RequiresSaveOperation;
        }

        public static bool IsLowPopulation(Type type)
        {
            return TryGetAttribute(type)?.IsLowPopulation ?? false;
        }

        public static EntityKindAttribute GetAttribute(Type type)
        {
            var attr = TryGetAttribute(type);

            if (attr == null)
                throw new InvalidOperationException("{0} does not define an EntityKindAttribute".FormatWith(type.TypeName()));

            return attr;
        }

        public static EntityKindAttribute TryGetAttribute(Type type)
        {
            return dictionary.GetOrAdd(type, t =>
            {
                if (!t.IsIEntity())
                    throw new InvalidOperationException("{0} should be a non-abstrat Entity".FormatWith(type.TypeName()));
                
                return t.GetCustomAttribute<EntityKindAttribute>(true);
            });
        }

        public static void Override(Type type, EntityKindAttribute attr)
        {
            if (type == null)
                throw new ArgumentNullException("attr");

            if (attr == null)
                throw new ArgumentNullException("attr");

            dictionary.AddOrUpdate(type, attr, (t, _) => attr);
        }

    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class EntityKindAttribute : Attribute
    {
        public EntityKind EntityKind { get; private set; }
        public EntityData EntityData { get; private set; }

        public bool IsLowPopulation { get; set; }
        
        bool? overridenRequiresSaveOperation; 
        public bool RequiresSaveOperation
        {
            get { return overridenRequiresSaveOperation ?? CalculateRequiresSaveOperation(this.EntityKind) ; }
            set
            {
                if (overridenRequiresSaveOperation != CalculateRequiresSaveOperation(this.EntityKind))
                    overridenRequiresSaveOperation = value;
            }
        }

        public bool IsRequiresSaveOperationOverriden => overridenRequiresSaveOperation.HasValue;

        public static bool CalculateRequiresSaveOperation(EntityKind entityKind)
        {
            switch (entityKind)
            {
                case EntityKind.SystemString: return false;
                case EntityKind.System: return false;
                case EntityKind.Relational: return false;
                case EntityKind.String: return true;
                case EntityKind.Shared: return true;
                case EntityKind.Main: return true;
                case EntityKind.Part: return false;
                case EntityKind.SharedPart: return false;
                default: throw new InvalidOperationException("Unexpeced entityKind");
            }
        }

        public EntityKindAttribute(EntityKind entityKind, EntityData entityData)
        {
            this.EntityKind = entityKind;
            this.EntityData = entityData;
        }
    }

    
    public enum EntityKind
    {
        /// <summary>
        /// Doesn't make sense to view it from other entity, since there's not to much to see. Not editable. 
        /// Not RequiresSaveOperation
        /// ie: PermissionSymbol
        /// </summary>
        SystemString,

        /// <summary>
        /// Not editable.
        /// Not RequiresSaveOperation
        /// ie: ExceptionEntity
        /// </summary>
        System,

        /// <summary>
        /// An entity that connects two entitities to implement a N to N relationship in a symetric way (no MLists)
        /// Not RequiresSaveOperation, not vieable, not creable (override on SearchControl) 
        /// ie: DiscountProductEntity
        /// </summary>
        Relational,


        /// <summary>
        /// Doesn't make sense to view it from other entity, since there's not to much to see. 
        /// RequiresSaveOperation
        /// ie: CountryEntity
        /// </summary>
        String,

        /// <summary>
        /// Used and shared by other entities, can be created from other entity. 
        /// RequiresSaveOperation
        /// ie: CustomerEntity (can create new while creating the order)
        /// </summary>
        Shared,

        /// <summary>
        /// Used and shared by other entities, but too big to create it from other entity.
        /// RequiresSaveOperation
        /// ie: OrderEntity
        /// </summary>
        Main,

        /// <summary>
        /// Entity that belongs to just one entity and should be saved together, but that can not be implemented as EmbeddedEntity (usually to enable polymorphisim)
        /// Not RequiresSaveOperation
        /// ie :ProductExtensionEntity
        /// </summary>
        Part,

        /// <summary>
        /// Entity that can be created on the fly and saved with the parent entity, but could also be shared with other entities to save space. 
        /// Not RequiresSaveOperation
        /// ie: AddressEntity
        /// </summary>
        SharedPart,
    }

    public enum EntityData
    {
        /// <summary>
        /// Entity created for business definition
        /// By default ordered by id Ascending
        /// ie: ProductEntity, OperationEntity, PermissionEntity, CountryEntity...  
        /// </summary>
        Master,

        /// <summary>
        /// Entity created while the business is running
        /// By default is ordered by id Descending
        /// ie: OrderEntity, ExceptionEntity, OperationLogEntity...
        /// </summary>
        Transactional
    }
}