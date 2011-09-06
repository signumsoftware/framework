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
    public class FieldRoute : IEquatable<FieldRoute>
    {
        Type type;
        public FieldRouteType FieldRouteType { get; private set; } 
        public FieldInfo FieldInfo { get; private set;}
        public PropertyInfo PropertyInfo { get { return Reflector.FindPropertyInfo(FieldInfo); } }
        public FieldRoute Parent { get; private set;}

        public static FieldRoute Construct<T>(Expression<Func<T, object>> expression)
            where T : IRootEntity
        {
            FieldRoute result = Root(typeof(T));

            foreach (var mi in Reflector.GetMemberList(expression))
            {
                result = result.Add(mi); 
            }
            return result;
        }

        public FieldRoute Add(string fieldOrProperty)
        {
            MemberInfo mi = (MemberInfo)Type.GetProperty(fieldOrProperty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
                            (MemberInfo)Type.GetField(fieldOrProperty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (mi == null)
                throw new InvalidOperationException("{0} of {1} does not exist".Formato(fieldOrProperty, this));

            return Add(mi);
        }

        public FieldRoute Add(MemberInfo fieldOrProperty)
        {
            if (this.Type.IsIIdentifiable())
            {
                Implementations imp = GetImplementations();

                ImplementedByAttribute ib = imp as ImplementedByAttribute;
                if (ib != null && ib.ImplementedTypes.Length == 1)
                {
                    return new FieldRoute(Root(ib.ImplementedTypes.Single()), fieldOrProperty); 
                }

                if (imp != null)
                    throw new InvalidOperationException("Attempt to make a PropertyRoute on a {0}. Cast first".Formato(imp.GetType()));

                return new FieldRoute(Root(this.Type), fieldOrProperty);
            }
            return new FieldRoute(this, fieldOrProperty);
        }

        FieldRoute(FieldRoute parent, MemberInfo fieldOrProperty)
        {
            if (fieldOrProperty == null)
                throw new ArgumentNullException("fieldOrProperty");

            if (parent == null)
                throw new ArgumentNullException("parent");

            if (fieldOrProperty is PropertyInfo && !parent.Type.FollowC(a => a.BaseType).Contains(fieldOrProperty.DeclaringType))
            {
                var pi = (PropertyInfo)fieldOrProperty;

                if (!parent.Type.GetInterfaces().Contains(fieldOrProperty.DeclaringType))
                    throw new ArgumentException("PropertyInfo {0} not found on {1}".Formato(pi.PropertyName(), parent.Type));

                var otherProperty = parent.Type.FollowC(a => a.BaseType)
                    .Select(a => a.GetProperty(fieldOrProperty.Name, BindingFlags.Public | BindingFlags.Instance)).NotNull().First();

                if (otherProperty == null)
                    throw new ArgumentException("PropertyInfo {0} not found on {1}".Formato(pi.PropertyName(), parent.Type));

                fieldOrProperty = otherProperty;
            }

            if (parent.Type.IsIIdentifiable() && parent.FieldRouteType != FieldRouteType.Root)
                throw new ArgumentException("Parent can not be a non-root Identifiable");

            if (fieldOrProperty is PropertyInfo && Reflector.IsMList(parent.Type))
            {
                if (fieldOrProperty.Name != "Item")
                    throw new NotSupportedException("PropertyInfo {0} is not supported".Formato(fieldOrProperty.Name)); 

                FieldRouteType = FieldRouteType.MListItems;
            }
            else if (fieldOrProperty is PropertyInfo && Reflector.IsLite(parent.Type))
            {
                if (fieldOrProperty.Name != "Entity" && fieldOrProperty.Name != "EntityOrNull")
                    throw new NotSupportedException("PropertyInfo {0} is not supported".Formato(fieldOrProperty.Name));

                FieldRouteType = FieldRouteType.LiteEntity;
            }
            else if (typeof(ModifiableEntity).IsAssignableFrom(parent.Type))
            {
                FieldRouteType = FieldRouteType.Field;
                this.FieldInfo = Reflector.FindFieldInfo(Parent.Type, fieldOrProperty, true);
            }
            else
                throw new NotSupportedException("Properties of {0} not supported".Formato(parent.Type));

         
            this.Parent = parent;
        }

        public static FieldRoute Root(Type rootEntity)
        {
            return new FieldRoute(rootEntity);
        }

        FieldRoute(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (!typeof(IRootEntity).IsAssignableFrom(type))
                throw new ArgumentException("Type must implement IPropertyRouteRoot");

            this.type = type;
            this.FieldRouteType = FieldRouteType.Root;
        }

        public Type Type { get { return type ?? FieldInfo.FieldType; } }
        public Type RootType { get { return type ?? Parent.RootType; } }

        public FieldInfo[] Fields
        {
            get
            {
                return this.FollowC(a => a.Parent).Select(a => a.FieldInfo).Reverse().Skip(1).ToArray();
            }
        }

        public override string ToString()
        {
            switch (FieldRouteType)
            {
                case FieldRouteType.Root:
                    return "({0})".Formato(type.Name);
                case FieldRouteType.Field:
                    return Parent.ToString() + (Parent.FieldRouteType == FieldRouteType.MListItems ? "" : ".") + FieldInfo.Name;
                case FieldRouteType.MListItems:
                    return Parent.ToString() + "/";
                case FieldRouteType.LiteEntity:
                    return Parent.ToString() + ".Entity";
            }
            throw new InvalidOperationException();
        }

        public string PropertyString()
        {
            switch (FieldRouteType)
            {
                case FieldRouteType.Root:
                    throw new InvalidOperationException("Root has no PropertyString");
                case FieldRouteType.Field:
                    switch (Parent.FieldRouteType)
                    {
                        case FieldRouteType.Root: return FieldInfo.Name;
                        case FieldRouteType.Field: return Parent.PropertyString() + "." + FieldInfo.Name;
                        case FieldRouteType.MListItems: return Parent.PropertyString() + FieldInfo.Name;
                        default: throw new InvalidOperationException();
                    }
                case FieldRouteType.MListItems:
                    return Parent.PropertyString() + "/";
            }
            throw new InvalidOperationException();
        }


        public static FieldRoute Parse(Type type, string route)
        {
            FieldRoute result = FieldRoute.Root(type);

            foreach (var part in route.Replace("/", ".Item.").Split('.'))
            {
                result = result.Add(part);
            }

            return result;
        }

        public static void SetFindImplementationsCallback(Func<FieldRoute, Implementations> findImplementations)
        {
            FindImplementations = findImplementations;
        }

        static Func<FieldRoute, Implementations> FindImplementations;

        public Implementations GetImplementations()
        {
            if (FindImplementations == null)
                throw new InvalidOperationException("PropertyRoute.FindImplementations not set");

            return FindImplementations(this);
        }

        public static void SetIsAllowedCallback(Func<FieldRoute, bool> isAllowed)
        {
            IsAllowedCallback = isAllowed;
        }

        static Func<FieldRoute, bool> IsAllowedCallback;
        
        public bool IsAllowed()
        {
            if (IsAllowedCallback != null)
                return IsAllowedCallback(this);

            return true;
        }


        public static List<FieldRoute> GenerateRoutes(Type type)
        {
            FieldRoute root = FieldRoute.Root(type);
            List<FieldRoute> result = new List<FieldRoute>();

            foreach (var fi in Reflector.InstanceFieldsInOrder(type))
            {
                FieldRoute route = root.Add(fi);
                result.Add(route);

                if (Reflector.IsEmbeddedEntity(fi.FieldType))
                    result.AddRange(GenerateEmbeddedProperties(route));

                if (Reflector.IsMList(fi.FieldType))
                {
                    Type colType = fi.FieldType.ElementType();
                    if (Reflector.IsEmbeddedEntity(colType))
                        result.AddRange(GenerateEmbeddedProperties(route.Add("Item")));
                }
            }

            return result;
        }

        static List<FieldRoute> GenerateEmbeddedProperties(FieldRoute embeddedProperty)
        {
            List<FieldRoute> result = new List<FieldRoute>();
            foreach (var pi in Reflector.InstanceFieldsInOrder(embeddedProperty.Type))
            {
                FieldRoute property = embeddedProperty.Add(pi);
                result.AddRange(property);

                if (Reflector.IsEmbeddedEntity(pi.FieldType))
                    result.AddRange(GenerateEmbeddedProperties(property));
            }

            return result;
        }

        public bool Equals(FieldRoute other)
        {
            if (other.FieldRouteType != this.FieldRouteType)
                return false;

            if (Type != other.Type)
                return false;

            if (!ReflectionTools.FieldEquals(FieldInfo, other.FieldInfo))
                return false;

            return object.Equals(Parent, other.Parent); 
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            FieldRoute other = obj as FieldRoute;

            if (obj == null)
                return false;

            return Equals(other);
        }
    }

    public interface IImplementationsFinder
    {
        Implementations FindImplementations(FieldRoute route);
    }

    public enum FieldRouteType
    {
        Root,
        Field,
        LiteEntity, 
        MListItems,
    }

   
}
