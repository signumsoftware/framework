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
using Signum.Entities.Internal;

namespace Signum.Entities
{
#pragma warning disable IDE1006
    public interface Lite<out T> : IComparable, IComparable<Lite<Entity>>
#pragma warning restore IDE1006
        where T : class, IEntity
    {
        T Entity { get; }
        T EntityOrNull { get; }

        PrimaryKey Id { get; }
        bool IsNew { get;  }
        PrimaryKey? IdOrNull { get; }
        Type EntityType { get; }
        
        void ClearEntity();      
        void SetEntity(Entity ei);
        void SetToString(string toStr);
        void RefreshId();

        string Key();
        string KeyLong();

        Lite<T> Clone(); 
    }

    namespace Internal
    {
        [Serializable]
        public abstract class LiteImp : Modifiable
        {

        }

        [Serializable]
        public sealed class LiteImp<T> : LiteImp, Lite<T>, ISerializable
            where T : Entity
        {
            T entityOrNull;
            PrimaryKey? id;
            string toStr;

            // Methods
            private LiteImp()
            {
            }

            public LiteImp(PrimaryKey id, string toStr)
            {
                if (typeof(T).IsAbstract)
                    throw new InvalidOperationException(typeof(T).Name + " is abstract");

                if (PrimaryKey.Type(typeof(T)) != id.Object.GetType())
                    throw new InvalidOperationException(typeof(T).TypeName() + " requires ids of type "
                        + PrimaryKey.Type(typeof(T)).TypeName() + ", not " + id.Object.GetType().TypeName());

                this.id = id;
                this.toStr = toStr;
                this.Modified = ModifiedState.Clean;
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

            public Entity UntypedEntityOrNull
            {
                get { return (Entity)(object)entityOrNull; }
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
                        throw new InvalidOperationException("The lite {0} is not loaded, use DataBase.Retrieve or consider rewriting your query".FormatWith(this));
                    return entityOrNull;
                }
            }

            public Type EntityType
            {
                get { return typeof(T); }
            }

            public PrimaryKey Id
            {
                get
                {
                    if (id == null)
                        throw new InvalidOperationException("The Lite is pointing to a new entity and has no Id yet");
                    return id.Value;
                }
            }

            public PrimaryKey? IdOrNull
            {
                get { return id; }
            }

            public void SetEntity(Entity ei)
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

                this.toStr = this.entityOrNull?.ToString();
                this.entityOrNull = null;
            }

            public void RefreshId()
            {
                id = entityOrNull.Id;
            }
            
            protected internal override void PreSaving(PreSavingContext ctx)
            {
                if (entityOrNull != null)
                {
                    entityOrNull.PreSaving(ctx);
                }
            }

            public override string ToString()
            {
                if (this.entityOrNull != null)
                    return this.entityOrNull.ToString();

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
                return "{0};{1}".FormatWith(TypeEntity.GetCleanName(this.EntityType), this.Id);
            }

            public string KeyLong()
            {
                return "{0};{1};{2}".FormatWith(TypeEntity.GetCleanName(this.EntityType), this.Id, this.ToString());
            }

            public int CompareTo(Lite<Entity> other)
            {
                return ToString().CompareTo(other.ToString());
            }

            public int CompareTo(object obj)
            {
                if (obj is Lite<Entity>)
                    return CompareTo((Lite<Entity>)obj);

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
                        case "id": this.id = (PrimaryKey)item.Value; break;
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
                    info.AddValue("id", this.id.Value, typeof(PrimaryKey));

                if (this.toStr != null)
                    info.AddValue("toStr", this.toStr, typeof(string));
            }
        }
    }

    public static class Lite
    {
        public static Type BaseImplementationType = typeof(LiteImp);

        static GenericInvoker<Func<PrimaryKey, string, Lite<Entity>>> giNewLite =
            new GenericInvoker<Func<PrimaryKey, string, Lite<Entity>>>((id, str) => new LiteImp<Entity>(id, str));

        static GenericInvoker<Func<Entity, string, Lite<Entity>>> giNewLiteFat =
            new GenericInvoker<Func<Entity, string, Lite<Entity>>>((entity, str) => new LiteImp<Entity>(entity, str));

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

        public static readonly Regex ParseRegex = new Regex(@"(?<type>[^;]+);(?<id>[\d\w-]+)(;(?<toStr>.+))?");

        public static Lite<Entity> Parse(string liteKey)
        {
            string error = TryParseLite(liteKey, out Lite<Entity> result);
            if (error == null)
                return result;
            else
                throw new FormatException(error);
        }

        public static Lite<T> Parse<T>(string liteKey) where T : class, IEntity
        {
            return (Lite<T>)Lite.Parse(liteKey);
        }

        public static string TryParseLite(string liteKey, out Lite<Entity> result)
        {
            result = null;
            if (string.IsNullOrEmpty(liteKey))
                return null;

            Match match = ParseRegex.Match(liteKey);
            if (!match.Success)
                return ValidationMessage.InvalidFormat.NiceToString();

            Type type = TypeEntity.TryGetType(match.Groups["type"].Value);
            if (type == null)
                return LiteMessage.Type0NotFound.NiceToString().FormatWith(match.Groups["type"].Value);

            if (!PrimaryKey.TryParse(match.Groups["id"].Value, type, out PrimaryKey id))
                return LiteMessage.IdNotValid.NiceToString();

            string toStr = match.Groups["toStr"].Value.DefaultText(null); //maybe null

            result = giNewLite.GetInvoker(type)(id, toStr);
            return null;
        }

        public static string TryParse<T>(string liteKey, out Lite<T> lite) where T : class, IEntity
        {
            var result = Lite.TryParseLite(liteKey, out Lite<Entity> untypedLite);
            lite = (Lite<T>)untypedLite;
            return result;
        }

        public static Lite<Entity> Create(Type type, PrimaryKey id)
        {
            return giNewLite.GetInvoker(type)(id, null);
        }

        public static Lite<Entity> Create(Type type, PrimaryKey id, string toStr)
        {
            return giNewLite.GetInvoker(type)(id, toStr);
        }

        public static Lite<T> ToLite<T>(this T entity)
          where T : class, IEntity
        {
            if (entity.IdOrNull == null)
                throw new InvalidOperationException("ToLite is not allowed for new entities, use ToLiteFat instead");

            return (Lite<T>)giNewLite.GetInvoker(entity.GetType())(entity.Id, entity.ToString());
        }

        public static Lite<T> ToLite<T>(this T entity, string toStr)
            where T : class, IEntity
        {
            if (entity.IsNew)
                throw new InvalidOperationException("ToLite is not allowed for new entities, use ToLiteFat instead");

            return (Lite<T>)giNewLite.GetInvoker(entity.GetType())(entity.Id, toStr ?? entity.ToString());
        }

        public static Lite<T> ToLiteFat<T>(this T entity)
         where T : class, IEntity
        {
            return (Lite<T>)giNewLiteFat.GetInvoker(entity.GetType())((Entity)(IEntity)entity, entity.ToString());
        }

        public static Lite<T> ToLiteFat<T>(this T entity, string toStr)
          where T : class, IEntity
        {
            return (Lite<T>)giNewLiteFat.GetInvoker(entity.GetType())((Entity)(IEntity)entity, toStr ?? entity.ToString());
        }

        public static Lite<T> ToLite<T>(this T entity, bool fat) where T : class, IEntity
        {
            if (fat)
                return entity.ToLiteFat();
            else
                return entity.ToLite();
        }

        public static Lite<T> ToLite<T>(this T entity, bool fat, string toStr) where T : class, IEntity
        {
            if (fat)
                return entity.ToLiteFat(toStr);
            else
                return entity.ToLite(toStr);
        }
       
        [MethodExpander(typeof(RefersToExpander))]
        public static bool RefersTo<T>(this Lite<T> lite, T entity)
            where T : class, IEntity
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
            static MethodInfo miToLazy = ReflectionTools.GetMethodInfo((TypeEntity type) => type.ToLite()).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                Expression lite = arguments[0];
                Expression entity = arguments[1];

                var evalEntity = ExpressionEvaluator.PartialEval(entity);

                var type = mi.GetGenericArguments()[0];

                var toLite = evalEntity is ConstantExpression c && c.Value == null ?
                    (Expression)Expression.Constant(null, typeof(Lite<>).MakeGenericType(type)) :
                    (Expression)Expression.Call(null, miToLazy.MakeGenericMethod(type), entity);

                return Expression.Equal(lite, toLite);
            }
        }

        [MethodExpander(typeof(IsExpander))]
        public static bool Is<T>(this T entity1, T entity2)
             where T : class, IEntity
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
            where T : class, IEntity
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

        public static XDocument EntityDGML(this Entity entity)
        {
            return GraphExplorer.FromRoot(entity).EntityDGML(); 
        }


        public static bool IsLite(this Type t)
        {
            return typeof(Lite<IEntity>).IsAssignableFrom(t);
        }

        public static Type CleanType(this Type t)
        {
            return Lite.Extract(t) ?? t;
        }


        public static Lite<T> Create<T>(PrimaryKey id) where T : Entity
        {
            return new LiteImp<T>(id, null);          
        }

        public static Lite<T> Create<T>(PrimaryKey id, string toStr) where T : Entity
        {
            return new LiteImp<T>(id, toStr);
        }

        static ConcurrentDictionary<Type, ConstructorInfo> ciLiteConstructorId = new ConcurrentDictionary<Type, ConstructorInfo>();
        public static ConstructorInfo LiteConstructorId(Type type)
        {
            return ciLiteConstructorId.GetOrAdd(type, t => typeof(LiteImp<>).MakeGenericType(t).GetConstructor(new[] { typeof(PrimaryKey), typeof(string) }));
        }

        public static NewExpression NewExpression(Type type, Expression id, Expression toString)
        {
            return Expression.New(Lite.LiteConstructorId(type), id.UnNullify(), toString);
        }


        static Lite<T> ToLiteFatInternal<T>(this T entity, string toStr)
            where T : class, IEntity
        {
            if (entity == null)
                return null;

            return entity.ToLiteFat(toStr);
        }

        static MethodInfo miToLiteFatInternal = ReflectionTools.GetMethodInfo(() => ToLiteFatInternal<Entity>(null, null)).GetGenericMethodDefinition();
        public static Expression ToLiteFatInternalExpression(Expression reference, Expression toString )
        {
            return Expression.Call(miToLiteFatInternal.MakeGenericMethod(reference.Type), reference, toString);
        }

        public static Lite<T> ParsePrimaryKey<T>(string id)
            where T : Entity
        {
            return Lite.Create<T>(PrimaryKey.Parse(id, typeof(T)));
        }

        public static Lite<Entity> ParsePrimaryKey(Type type, string id)
        {
            return Lite.Create(type, PrimaryKey.Parse(id, type));
        }
    }

    public enum LiteMessage
    {
        IdNotValid,
        [Description("Invalid Format")]
        InvalidFormat,
        [Description("New")]
        New_G,
        [Description("Type {0} not found")]
        Type0NotFound,
        [Description("Text")]
        ToStr
    }
}
