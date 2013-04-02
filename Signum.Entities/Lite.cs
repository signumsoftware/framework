using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Diagnostics;
using Signum.Entities.Basics;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace Signum.Entities
{
    public interface Lite<out T> : IComparable, IComparable<Lite<IdentifiableEntity>>
        where T : class, IIdentifiable
    {
        T Entity { get; }
        T EntityOrNull { get; }
    
        int Id { get; }
        bool IsNew { get;  }
        int? IdOrNull { get; }
        Type EntityType { get; }
        IdentifiableEntity UntypedEntityOrNull { get; }

        void ClearEntity();      
        void SetEntity(IdentifiableEntity ei);
        void SetToString(string toStr);
        void RefreshId();

        string Key();
        string KeyLong();

        Lite<T> Clone(); 
    }

    [Serializable]
    public abstract class LiteImp  : Modifiable
    {
    
    }

    [Serializable, DebuggerTypeProxy(typeof(FlattenHierarchyProxy))]
    public sealed class LiteImp<T> : LiteImp, Lite<T>, ISerializable
        where T : IdentifiableEntity
    {
        T entityOrNull;
        int? id;
        string toStr;

        // Methods
        private LiteImp()
        {
        }

        public LiteImp(int id, string toStr)
        {
            if (typeof(T).IsAbstract)
                throw new InvalidOperationException(typeof(T).Name + " is abstract"); 

            this.id = id;
            this.toStr = toStr;
            this.Modified = ModifiedState.Clean;
        }

        public LiteImp(int id, string toStr, ModifiedState modified)
        {
            if (typeof(T).IsAbstract)
                throw new InvalidOperationException(typeof(T).Name + " is abstract");

            this.id = id;
            this.toStr = toStr;
            this.Modified = modified; 
        }

        public LiteImp(T entity, string toStr)
        {
            if (typeof(T).IsAbstract)
                throw new InvalidOperationException(typeof(T).Name + " is abstract"); 

            if (entity.GetType() != typeof(T))
                throw new ArgumentNullException("entity");

            this.entityOrNull = entity;
            this.id = entity.IdOrNull;
            this.toStr = toStr;
        }

        public IdentifiableEntity UntypedEntityOrNull
        {
            get { return (IdentifiableEntity)(object)entityOrNull; }
        }

        public T EntityOrNull
        {
            get { return entityOrNull; }
        }

        public bool IsNew
        {
            get { return entityOrNull != null && entityOrNull.IsNew; }
        }

        public T Entity 
        {
            get
            {
                if (entityOrNull == null)
                    throw new InvalidOperationException("The lite {0} is not loaded, use DataBase.Retrieve or consider rewriting your query".Formato(this));
                return entityOrNull;
            }
        }

        public Type EntityType
        {
            get { return typeof(T); }
        }

        public int Id
        {
            get
            {
                if (id == null)
                    throw new InvalidOperationException("The Lite is pointing to a new entity and has no Id yet");
                return id.Value;
            }
        }

        public int? IdOrNull
        {
            get { return id; }
        }

        public void SetEntity(IdentifiableEntity ei)
        {
            if (id == null)
                throw new InvalidOperationException("New entities are not allowed");

            if (id != ei.id || EntityType != ei.GetType())
                throw new InvalidOperationException("Entities do not match");

            this.entityOrNull = (T)ei;
            if (ei != null && this.toStr == null)
                this.toStr = ei.ToString();
        }

        public void ClearEntity()
        {
            if (id == null)
                throw new InvalidOperationException("Removing entity not allowed in new Lite");

            this.toStr = this.UntypedEntityOrNull.TryToString();
            this.entityOrNull = null;
        }

        public void RefreshId()
        {
            id = UntypedEntityOrNull.Id;
        }


        protected internal override void PreSaving(ref bool graphModified)
        {
            if (UntypedEntityOrNull != null)
            {
                UntypedEntityOrNull.PreSaving(ref graphModified);
            }
        }

        public override string ToString()
        {
            if (this.UntypedEntityOrNull != null)
                return this.UntypedEntityOrNull.ToString();

            return this.toStr;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (this == obj)
                return true;

            if (GetType() != obj.GetType())
                return false;

            Lite<T> lite = (LiteImp<T>)obj;
            if (IdOrNull != null && lite.IdOrNull != null)
                return Id == lite.Id;
            else
                return object.ReferenceEquals(this.entityOrNull, lite.EntityOrNull);
        }

        const int MagicMask = 123456853;
        public override int GetHashCode()
        {
            return this.id == null ?
                entityOrNull.GetHashCode() ^ MagicMask :
                this.EntityType.FullName.GetHashCode() ^ this.Id.GetHashCode() ^ MagicMask;
        }

        public string Key()
        {
            return "{0};{1}".Formato(Lite.UniqueTypeName(this.EntityType), this.Id);
        }

        public string KeyLong()
        {
            return "{0};{1};{2}".Formato(Lite.UniqueTypeName(this.EntityType), this.Id, this.ToString());
        }

        public int CompareTo(Lite<IdentifiableEntity> other)
        {
            return ToString().CompareTo(other.ToString());
        }

        public int CompareTo(object obj)
        {
            if (obj is Lite<IdentifiableEntity>)
                return CompareTo((Lite<IdentifiableEntity>)obj);

            throw new InvalidOperationException("obj is not a Lite");
        }

        public void SetToString(string toStr)
        {
            this.toStr = toStr;
        }

        public Lite<T> Clone()
        {
            return new LiteImp<T>(Id, toStr); 
        }

        private LiteImp(SerializationInfo info, StreamingContext ctxt)
        {
            bool modifiedSet = false;

            foreach (SerializationEntry item in info)
            {
                switch (item.Name)
                {
                    case "modified": this.Modified = (ModifiedState)Enum.Parse(typeof(ModifiedState), (string)item.Value); modifiedSet = true; break;
                    case "entityOrNull": this.entityOrNull = (T)item.Value; break;
                    case "id": this.id = (int)item.Value; break;
                    case "toStr": this.toStr = (string)item.Value; break;
                    default: throw new InvalidOperationException("Unexpected SerializationEntry");
                }
            }

            if (!modifiedSet)
                this.Modified = ModifiedState.Clean;
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (this.Modified != ModifiedState.Clean)
                info.AddValue("modified", this.Modified.ToString(), typeof(string));

            if (this.entityOrNull != null)
                info.AddValue("entityOrNull", this.entityOrNull, typeof(T));

            if (this.id != null)
                info.AddValue("id", this.id.Value, typeof(int));

            if (this.toStr != null)
                info.AddValue("toStr", this.toStr, typeof(string));
        }
    }

    public static class Lite
    {
        public static Type BaseImplementationType = typeof(LiteImp);

        static GenericInvoker<Func<int, string, Lite<IdentifiableEntity>>> giNewLite =
            new GenericInvoker<Func<int, string, Lite<IdentifiableEntity>>>((id, str) => new LiteImp<IdentifiableEntity>(id, str));

        static GenericInvoker<Func<int, string, ModifiedState, Lite<IdentifiableEntity>>> giNewLiteModified =
            new GenericInvoker<Func<int, string, ModifiedState, Lite<IdentifiableEntity>>>((id, str, state) => new LiteImp<IdentifiableEntity>(id, str, state));

        static GenericInvoker<Func<IdentifiableEntity, string, Lite<IdentifiableEntity>>> giNewLiteFat =
            new GenericInvoker<Func<IdentifiableEntity, string, Lite<IdentifiableEntity>>>((entity, str) => new LiteImp<IdentifiableEntity>(entity, str));

        public static Type Generate(Type identificableType)
        {
            return typeof(Lite<>).MakeGenericType(identificableType);
        }

        public static Type Extract(Type liteType)
        {
            if (liteType.IsInstantiationOf(typeof(Lite<>)) || typeof(LiteImp).IsAssignableFrom(liteType))
                return liteType.GetGenericArguments()[0];
            return null;
        }

        static Regex regex = new Regex(@"(?<type>.+);(?<id>.+)(;(?<toStr>.+))?");

        public static Lite<IdentifiableEntity> Parse(string liteKey)
        {
            Lite<IdentifiableEntity> result;
            string error = TryParseLite(liteKey, out result);
            if (error == null)
                return result;
            else
                throw new FormatException(error);
        }

        public static Lite<T> Parse<T>(string liteKey) where T : class, IIdentifiable
        {
            return (Lite<T>)Lite.Parse(liteKey);
        }

        public static string TryParseLite(string liteKey, out Lite<IdentifiableEntity> result)
        {
            result = null;
            if (string.IsNullOrEmpty(liteKey))
                return null;

            Match match = regex.Match(liteKey);
            if (!match.Success)
                return ValidationMessage.InvalidFormat.NiceToString();

            Type type = ResolveType(match.Groups["type"].Value);
            if (type == null)
                return LiteMessage.TypeNotFound.NiceToString();

            int id;
            if (!int.TryParse(match.Groups["id"].Value, out id))
                return LiteMessage.IdNotValid.NiceToString();

            string toStr = match.Groups["toStr"].Value; //maybe null

            result = giNewLite.GetInvoker(type)(id, toStr);
            return null;
        }

        public static string TryParse<T>(string liteKey, out Lite<T> lite) where T : class, IIdentifiable
        {
            Lite<IdentifiableEntity> untypedLite;
            var result = Lite.TryParseLite(liteKey, out untypedLite);
            lite = (Lite<T>)untypedLite;
            return result;
        }

        public static Func<Type, string> UniqueTypeName { get; private set; }
        public static Func<string, Type> ResolveType { get; private set; }

        public static void SetTypeNameAndResolveType(Func<Type, string> uniqueTypeName, Func<string, Type> resolveType)
        {
            Lite.UniqueTypeName = uniqueTypeName;
            Lite.ResolveType = resolveType;
        }

        public static Lite<IdentifiableEntity> Create(Type type, int id)
        {
            return giNewLite.GetInvoker(type)(id, null);
        }

        public static Lite<IdentifiableEntity> Create(Type type, int id, string toStr)
        {
            return giNewLite.GetInvoker(type)(id, toStr);
        }

        public static Lite<IdentifiableEntity> Create(Type type, int id, string toStr, ModifiedState state)
        {
            return giNewLiteModified.GetInvoker(type)(id, toStr, state);
        }

        public static Lite<T> ToLite<T>(this T entity)
          where T : class, IIdentifiable
        {
            if (entity == null)
                return null;

            if (entity.IsNew)
                throw new InvalidOperationException("ToLite is not allowed for new entities, use ToLiteFat instead");

            return (Lite<T>)giNewLite.GetInvoker(entity.GetType())(entity.Id, entity.ToString());
        }

        public static Lite<T> ToLite<T>(this T entity, string toStr)
            where T : class, IIdentifiable
        {
            if (entity == null)
                return null;

            if (entity.IsNew)
                throw new InvalidOperationException("ToLite is not allowed for new entities, use ToLiteFat instead");

            return (Lite<T>)giNewLite.GetInvoker(entity.GetType())(entity.Id, toStr ?? entity.ToString());
        }

        public static Lite<T> ToLiteFat<T>(this T entity)
         where T : class, IIdentifiable
        {
            if (entity == null)
                return null;

            return (Lite<T>)giNewLiteFat.GetInvoker(entity.GetType())((IdentifiableEntity)(IIdentifiable)entity, entity.ToString());
        }

        public static Lite<T> ToLiteFat<T>(this T entity, string toStr)
          where T : class, IIdentifiable
        {
            if (entity == null)
                return null;

            return (Lite<T>)giNewLiteFat.GetInvoker(entity.GetType())((IdentifiableEntity)(IIdentifiable)entity, toStr ?? entity.ToString());
        }

        public static Lite<T> ToLite<T>(this T entity, bool fat) where T : class, IIdentifiable
        {
            if (fat)
                return entity.ToLiteFat();
            else
                return entity.ToLite();
        }

        public static Lite<T> ToLite<T>(this T entity, bool fat, string toStr) where T : class, IIdentifiable
        {
            if (fat)
                return entity.ToLiteFat(toStr);
            else
                return entity.ToLite(toStr);
        }
       
        [MethodExpander(typeof(RefersToExpander))]
        public static bool RefersTo<T>(this Lite<T> lite, T entity)
            where T : class, IIdentifiable
        {
            if (lite == null && entity == null)
                return true;

            if (lite == null || entity == null)
                return false;

            if (lite.EntityType != entity.GetType())
                return false;

            if (lite.IdOrNull != null)
                return lite.Id == entity.IdOrNull;
            else
                return object.ReferenceEquals(lite.Entity, entity);
        }

        class RefersToExpander : IMethodExpander
        {
            static MethodInfo miToLazy = ReflectionTools.GetMethodInfo((TypeDN type) => type.ToLite()).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                Expression lite = arguments[0];
                Expression entity = arguments[1];

                return Expression.Equal(lite, Expression.Call(null, miToLazy.MakeGenericMethod(mi.GetGenericArguments()[0]), entity));
            }
        }

        [MethodExpander(typeof(IsExpander))]
        public static bool Is<T>(this T entity1, T entity2)
             where T : class, IIdentifiable
        {
            if (entity1 == null && entity2 == null)
                return true;

            if (entity1 == null || entity2 == null)
                return false;

            if (entity1.GetType() != entity2.GetType())
                return false;

            if (entity1.IdOrNull != null)
                return entity1.Id == entity2.IdOrNull;
            else
                return object.ReferenceEquals(entity1, entity2);
        }

        class IsExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                return Expression.Equal(arguments[0], arguments[1]);
            }
        }

        [MethodExpander(typeof(IsExpander))]
        public static bool Is<T>(this Lite<T> lite1, Lite<T> lite2)
            where T : class, IIdentifiable
        {
            if (lite1 == null && lite2 == null)
                return true;

            if (lite1 == null || lite2 == null)
                return false;

            if (lite1.EntityType != lite2.EntityType)
                return false;

            if (lite1.IdOrNull != null && lite2.IdOrNull != null)
                return lite1.Id == lite2.Id;
            else
                return object.ReferenceEquals(lite1.EntityOrNull, lite2.EntityOrNull);
        }

        public static XDocument EntityDGML(this IdentifiableEntity entity)
        {
            return GraphExplorer.FromRoot(entity).EntityDGML(); 
        }


        public static bool IsLite(this Type t)
        {
            return typeof(Lite<IIdentifiable>).IsAssignableFrom(t);
        }

        public static Type CleanType(this Type t)
        {
            return Lite.Extract(t) ?? t;
        }


        public static Lite<T> Create<T>(int id) where T : IdentifiableEntity
        {
            return new LiteImp<T>(id, null);          
        }

        public static Lite<T> Create<T>(int id, string toStr) where T : IdentifiableEntity
        {
            return new LiteImp<T>(id, toStr);
        }

        public static Lite<T> Create<T>(int id, string toStr, ModifiedState modified) where T : IdentifiableEntity
        {
            return new LiteImp<T>(id, toStr, modified);
        }

        static ConcurrentDictionary<Type, ConstructorInfo> ciLiteConstructor = new ConcurrentDictionary<Type, ConstructorInfo>();

        public static ConstructorInfo LiteConstructor(Type type)
        {
            return ciLiteConstructor.GetOrAdd(type, t => typeof(LiteImp<>).MakeGenericType(t).GetConstructor(new[] { typeof(int), typeof(string), typeof(ModifiedState) }));
        }

        public static NewExpression NewExpression(Type type, Expression id, Expression toString, Expression modified)
        {
            return Expression.New(Lite.LiteConstructor(type), id.UnNullify(), toString, modified);
        }
    }

    public enum LiteMessage
    {
        IdNotValid,
        [Description("Invalid Format")]
        InvalidFormat,
        New,
        TypeNotFound,
        [Description("Text")]
        ToStr
    }
}
