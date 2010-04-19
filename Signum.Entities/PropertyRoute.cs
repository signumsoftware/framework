using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities.Properties;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities
{
    [Serializable]
    public class PropertyRoute : IEquatable<PropertyRoute>
    {
        Type type;
        public PropertyRouteType PropertyRouteType { get; private set; } 
        public PropertyInfo PropertyInfo{get; private set;}
        public PropertyRoute Parent {get; private set;}

        public static PropertyRoute Construct<T>(Expression<Func<T, object>> expression)
            where T : IdentifiableEntity
        {
            PropertyRoute result = Root(typeof(T));

            foreach (var mi in Reflector.GetMemberList(expression))
            {
                result = result.Add((PropertyInfo)mi);
            }
            return result;
        }

        public PropertyRoute Add(string propertyName)
        {
            return Add(Type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance));
        }

        public PropertyRoute Add(PropertyInfo propertyInfo)
        {
            if (typeof(IIdentifiable).IsAssignableFrom(this.Type))
            {
                Implementations imp = GetImplementations();

                ImplementedByAttribute ib = imp as ImplementedByAttribute;
                if (ib != null && ib.ImplementedTypes.Length == 1)
                {
                    return new PropertyRoute(Root(ib.ImplementedTypes.Single()), propertyInfo); 
                }

                if (imp != null)
                    throw new InvalidOperationException("Attempt to make a property route on a {0}. Cast first.".Formato(imp.GetType()));

                return new PropertyRoute(Root(this.Type), propertyInfo);
            }
            return new PropertyRoute(this, propertyInfo);
        }

        PropertyRoute(PropertyRoute parent, PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");

            if (parent == null)
                throw new ArgumentNullException("parent");

            if (!parent.Type.FollowC(a => a.BaseType).Contains(propertyInfo.DeclaringType))
                throw new ArgumentException("propertyInfo '{0}' not found on {1}".Formato(propertyInfo.PropertyName(), parent.Type));

            if (typeof(IIdentifiable).IsAssignableFrom(parent.Type) && parent.PropertyRouteType != PropertyRouteType.Root)
                throw new ArgumentException("parent can not be an non-Root Identifiable");

            if (Reflector.IsMList(parent.Type))
            {
                if (propertyInfo.Name != "Item")
                    throw new NotSupportedException("{0} PropertyInfo is not supported".Formato(propertyInfo.Name)); 

                PropertyRouteType = PropertyRouteType.MListItems;
            }
            else if (Reflector.IsLite(parent.Type))
            {
                if (propertyInfo.Name != "Entity" && propertyInfo.Name != "EntityOrNull")
                    throw new NotSupportedException("{0} PropertyInfo is not supported".Formato(propertyInfo.Name));

                PropertyRouteType = PropertyRouteType.LiteEntity;
            }
            else if (typeof(ModifiableEntity).IsAssignableFrom(parent.Type))
            {
                PropertyRouteType = PropertyRouteType.Property;
            }
            else
                throw new NotSupportedException("Properties of type {0} not supported".Formato(parent.Type));

            this.PropertyInfo = propertyInfo;
            this.Parent = parent;
        }

        public static PropertyRoute Root(Type identifiable)
        {
            return new PropertyRoute(identifiable);
        }

        PropertyRoute(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (!typeof(IdentifiableEntity).IsAssignableFrom(type))
                throw new ArgumentException("type must be a Type inheriting IdentitiableEntity");

            this.type = type;
            this.PropertyRouteType = PropertyRouteType.Root;
        }

        public Type Type { get { return type ?? PropertyInfo.PropertyType; } }
        public Type IdentifiableType { get { return type ?? Parent.IdentifiableType; } }

        public PropertyInfo[] Properties
        {
            get
            {
                return this.FollowC(a => a.Parent).Select(a => a.PropertyInfo).Reverse().Skip(1).ToArray();
            }
        }

        public override string ToString()
        {
            switch (PropertyRouteType)
            {
                case PropertyRouteType.Root:
                    return "({0})".Formato(type.Name);
                case PropertyRouteType.Property:
                    return Parent.ToString() + (Parent.PropertyRouteType == PropertyRouteType.MListItems ? "" : ".") + PropertyInfo.Name;
                case PropertyRouteType.MListItems:
                    return Parent.ToString() + "/";
                case PropertyRouteType.LiteEntity:
                    return Parent.ToString() + ".Entity";
            }
            throw new InvalidOperationException();
        }

        public string PropertyString()
        {
            switch (PropertyRouteType)
            {
                case PropertyRouteType.Root:
                    throw new InvalidOperationException("Root has not PropertyString");
                case PropertyRouteType.Property:
                    switch (Parent.PropertyRouteType)
                    {
                        case PropertyRouteType.Root: return PropertyInfo.Name;
                        case PropertyRouteType.Property: return Parent.PropertyString() + "." + PropertyInfo.Name;
                        case PropertyRouteType.MListItems: return Parent.PropertyString() + PropertyInfo.Name;
                        default: throw new InvalidOperationException();
                    }
                case PropertyRouteType.MListItems:
                    return Parent.PropertyString() + "/";
            }
            throw new InvalidOperationException();
        }


        public static PropertyRoute Parse(Type type, string route)
        {
            PropertyRoute result = PropertyRoute.Root(type);

            foreach (var part in route.Replace("/", ".Item.").Split('.'))
            {
                result = result.Add(part);
            }

            return result;
        }

        public static void SetFindImplementationsCallback(Func<PropertyRoute, Implementations> findImplementations)
        {
            FindImplementations = findImplementations;
        }

        static Func<PropertyRoute, Implementations> FindImplementations;

        public Implementations GetImplementations()
        {
            if (FindImplementations == null)
                throw new InvalidOperationException("PropertyPath.FindImplementations not set");

            return FindImplementations(this);
        }

        public static void SetIsAllowedCallback(Func<PropertyRoute, bool> isAllowed)
        {
            IsAllowedCallback = isAllowed;
        }

        static Func<PropertyRoute, bool> IsAllowedCallback;
        
        public bool IsAllowed()
        {
            if (IsAllowedCallback != null)
                return IsAllowedCallback(this);

            return true;
        }


        public static List<PropertyRoute> GenerateRoutes(Type type)
        {
            PropertyRoute root = PropertyRoute.Root(type);
            List<PropertyRoute> result = new List<PropertyRoute>();

            foreach (var pi in Reflector.PublicInstancePropertiesInOrder(type))
            {
                PropertyRoute property = root.Add(pi);
                result.Add(property);

                if (Reflector.IsEmbeddedEntity(pi.PropertyType))
                    result.AddRange(GenerateEmbeddedProperties(property));

                if (Reflector.IsMList(pi.PropertyType))
                {
                    Type colType = pi.PropertyType.ElementType();
                    if (Reflector.IsEmbeddedEntity(colType))
                        result.AddRange(GenerateEmbeddedProperties(property.Add("Item")));
                }
            }

            return result;
        }

        static List<PropertyRoute> GenerateEmbeddedProperties(PropertyRoute embeddedProperty)
        {
            List<PropertyRoute> result = new List<PropertyRoute>();
            foreach (var pi in Reflector.PublicInstancePropertiesInOrder(embeddedProperty.Type))
            {
                PropertyRoute property = embeddedProperty.Add(pi);
                result.AddRange(property);

                if (Reflector.IsEmbeddedEntity(pi.PropertyType))
                    result.AddRange(GenerateEmbeddedProperties(property));
            }

            return result;
        }

        public bool Equals(PropertyRoute other)
        {
            if (other.PropertyRouteType != this.PropertyRouteType)
                return false;

            if (Type != other.Type)
                return false;

            if (!ReflectionTools.PropertyEquals(PropertyInfo, other.PropertyInfo))
                return false;

            return object.Equals(Parent, other.Parent); 
        }

        public override int GetHashCode()
        {
            if (PropertyInfo != null)
                return PropertyInfo.Name.GetHashCode();

            return type.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            PropertyRoute other = obj as PropertyRoute;

            if (obj == null)
                return false;

            return Equals(other);
        }
    }

    public enum PropertyRouteType
    {
        Root,
        Property,
        LiteEntity, 
        MListItems,
    }

   
}
