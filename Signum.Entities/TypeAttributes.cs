using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities
{
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

        public static bool IsLowPopulation(Type type)
        {
            return TryGetAttribute(type).Try(a => a.IsLowPopulation) ?? false;
        }

        public static EntityKindAttribute GetAttribute(Type type)
        {
            var attr = TryGetAttribute(type);

            if (attr == null)
                throw new InvalidOperationException("{0} does not define an EntityKindAttribute".Formato(type.TypeName()));

            return attr;
        }

        public static EntityKindAttribute TryGetAttribute(Type type)
        {
            return dictionary.GetOrAdd(type, t =>
            {
                if (!t.IsIIdentifiable())
                    throw new InvalidOperationException("{0} should be a non-abstrat IdentifiableEntity");

                return t.SingleAttributeInherit<EntityKindAttribute>();
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
        /// Not SaveProtected
        /// ie: MultiEnumDN
        /// </summary>
        SystemString,

        /// <summary>
        /// Not editable.
        /// Not SaveProtected
        /// ie: ExceptionDN
        /// </summary>
        System,

        /// <summary>
        /// An entity that connects to entity to implement a N to N relationship in a symetric way (no MLists)
        /// Not SaveProtected, not vieable, not creable (override on SearchControl) 
        /// ie: DiscountProductDN
        /// </summary>
        Relational,


        /// <summary>
        /// Doesn't make sense to view it from other entity, since there's not to much to see. 
        /// SaveProtected
        /// ie: CountryDN
        /// </summary>
        String,

        /// <summary>
        /// Used and shared by other entities, can be created from other entity. 
        /// SaveProtected
        /// ie: CustomerDN (can create new while creating the order)
        /// </summary>
        Shared,

        /// <summary>
        /// Used and shared by other entities, but too big to create it from other entity.
        /// SaveProtected
        /// ie: OrderDN
        /// </summary>
        Main,

        /// <summary>
        /// Entity that belongs to just one entity and should be saved together, but that can not be implemented as EmbeddedEntity (usually to enable polymorphisim)
        /// Not SaveProtected
        /// ie :ProductExtensionDN
        /// </summary>
        Part,

        /// <summary>
        /// Entity that can be created on the fly and saved with the parent entity, but could also be shared with other entities to save space. 
        /// Not SaveProtected
        /// ie: AddressDN
        /// </summary>
        SharedPart,
    }

    public enum EntityData
    {
        /// <summary>
        /// Entity created for business definition
        /// By defautlt ordered by id Ascending
        /// ie: ProductDN, OperationDN, PermissionDN, CountryDN...  
        /// </summary>
        Master,

        /// <summary>
        /// Entity created while the business is running
        /// By default is ordered by id Descending
        /// ie: OrderDN, ExceptionDN, OperationLogDN...
        /// </summary>
        Transactional
    }
}