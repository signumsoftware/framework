using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Maps
{
    public class EntityEvents<T> : IEntityEvents
            where T : Entity
    {
        public event PreSavingEventHandler<T> PreSaving;
        public event SavingEventHandler<T> Saving;
        public event SavedEventHandler<T> Saved;

        public event AlternativeRetriveEventHandler<T> AlternativeRetrive;
        public event RetrievedEventHandler<T> Retrieved;

        public CacheControllerBase<T> CacheController { get; set; }

        public event FilterQueryEventHandler<T> FilterQuery;

        public event PreUnsafeDeleteHandler<T> PreUnsafeDelete;
        public event PreUnsafeMListDeleteHandler<T> PreUnsafeMListDelete;

        public event PreUnsafeUpdateHandler<T> PreUnsafeUpdate;

        public event PreUnsafeInsertHandler<T> PreUnsafeInsert;
        public event BulkInsetHandler<T> PreBulkInsert;

        public Dictionary<PropertyRoute, IAdditionalBinding> AdditionalBindings { get; private set; }
        
        /// <param name="valueFunction">For Caching scenarios</param>
        public void RegisterBinding<M>(Expression<Func<T, M>> field, Func<bool> shouldSet, Expression<Func<T, M>> valueExpression, Func<T, IRetriever, M> valueFunction = null)
        {
            if (AdditionalBindings == null)
                AdditionalBindings = new Dictionary<PropertyRoute, IAdditionalBinding>();

            var ma = (MemberExpression)field.Body;

            var pr = PropertyRoute.Construct(field);

            AdditionalBindings.Add(pr, new AdditionalBinding<T, M>
            {
                PropertyRoute = pr,
                ShouldSet = shouldSet,
                ValueExpression = valueExpression,
                ValueFunction = valueFunction,
            });
        }

       

        internal IEnumerable<FilterQueryResult<T>> OnFilterQuery()
        {
            if (FilterQuery == null)
                return Enumerable.Empty<FilterQueryResult<T>>();

            return FilterQuery.GetInvocationListTyped().Select(f => f()).ToList();
        }

        internal IDisposable OnPreUnsafeDelete(IQueryable<T> entityQuery)
        {
            IDisposable result = null;
            if (PreUnsafeDelete != null)
                foreach (var action in PreUnsafeDelete.GetInvocationListTyped().Reverse())
                    result = Disposable.Combine(result, action(entityQuery));

            return result;
        }

        internal IDisposable OnPreUnsafeMListDelete(IQueryable mlistQuery, IQueryable<T> entityQuery)
        {
            IDisposable result = null;
            if (PreUnsafeMListDelete != null)
                foreach (var action in PreUnsafeMListDelete.GetInvocationListTyped().Reverse())
                    result = Disposable.Combine(result, action(mlistQuery, entityQuery));

            return result;
        }

        IDisposable IEntityEvents.OnPreUnsafeUpdate(IUpdateable update)
        {
            IDisposable result = null;
            if (PreUnsafeUpdate != null)
            {
                var query = update.EntityQuery<T>();
                foreach (var action in PreUnsafeUpdate.GetInvocationListTyped().Reverse())
                    result = Disposable.Combine(result, action(update, query));
            }

            return result;
        }

        LambdaExpression IEntityEvents.OnPreUnsafeInsert(IQueryable query, LambdaExpression constructor, IQueryable entityQuery)
        {
            if (PreUnsafeInsert != null)
                foreach (var action in PreUnsafeInsert.GetInvocationListTyped().Reverse())
                    constructor = action(query, constructor, (IQueryable<T>)entityQuery);

            return constructor;
        }

        void IEntityEvents.OnPreBulkInsert(bool inMListTable)
        {
            if (PreBulkInsert != null)
                foreach (var action in PreBulkInsert.GetInvocationListTyped().Reverse())
                    action(inMListTable);
        }

        void IEntityEvents.OnPreSaving(Entity entity, PreSavingContext ctx)
        {
            PreSaving?.Invoke((T)entity, ctx);
        }

        void IEntityEvents.OnSaving(Entity entity)
        {
            Saving?.Invoke((T)entity);

        }

        void IEntityEvents.OnSaved(Entity entity, SavedEventArgs args)
        {
            Saved?.Invoke((T)entity, args);

        }

        void IEntityEvents.OnRetrieved(Entity entity)
        {
            Retrieved?.Invoke((T)entity);
        }

        public Entity OnAlternativeRetriving(PrimaryKey id)
        {
            if (AlternativeRetrive == null)
                return null;

            var args = new AlternativeRetrieveArgs<T>();

            AlternativeRetrive(id, args);


            if (args.Entity == null)
                throw new EntityNotFoundException(typeof(T), id);

            if (!args.AvoidAccesVerify)
            {
                var verifyAcces = Database.Query<T>().Where(a => a.Id == id).Any();
                if (!verifyAcces)
                    throw new EntityNotFoundException(typeof(T), id);
            }

            return (Entity)args.Entity;


        }

        ICacheController IEntityEvents.CacheController
        {
            get { return CacheController; }
        }
    }

    public interface IAdditionalBinding
    {
        PropertyRoute PropertyRoute { get; }
        Func<bool> ShouldSet { get; }
        LambdaExpression ValueExpression { get; }
        void SetInMemory(Entity entity, IRetriever retriever);
    }

    public class AdditionalBinding<T, M> : IAdditionalBinding
        where T : Entity
    {
        public PropertyRoute PropertyRoute { get; set; }
        public Func<bool> ShouldSet { get; set; }
        public Expression<Func<T, M>> ValueExpression { get; set; }
        public Func<T, IRetriever, M> ValueFunction { get; set; }
        LambdaExpression IAdditionalBinding.ValueExpression => ValueExpression;

        Action<T, M> _setter;

        public void SetInMemory(Entity entity, IRetriever retriever) => SetInMemory((T)entity, retriever);
        void SetInMemory(T entity, IRetriever retriever)
        {
            if (!ShouldSet())
                return;

            if (ValueFunction == null)
                throw new InvalidOperationException($"ValueFunction should be set in AdditionalBinding {PropertyRoute} because {PropertyRoute.Type} is Cached");

            var setter = _setter ?? (_setter = CreateSetter());

            var value = ValueFunction(entity, retriever);

            setter(entity, value);
        }

        Action<T, M> CreateSetter()
        {
            if (PropertyRoute.Type.IsMList())
            {
                var partGetter = PropertyRoute.GetLambdaExpression<T, M>(true).Compile();

                return (e, value) =>
                {
                    var mlist = partGetter(e);

                    if (mlist == null)
                        return;

                    ((IMListPrivate)mlist).AssignAndPostRetrieving((IMListPrivate)value);
                };
            }
            else if (PropertyRoute.Parent.PropertyRouteType == PropertyRouteType.Root)
                return ReflectionTools.CreateSetter<T, M>(PropertyRoute.PropertyInfo);
            else
            {
                var partGetter = PropertyRoute.Parent.GetLambdaExpression<T, ModifiableEntity>(true).Compile();

                var setter = ReflectionTools.CreateSetter<ModifiableEntity, M>(PropertyRoute.PropertyInfo);

                return (e, value) =>
                {
                    var part = partGetter(e);
                    if (part == null)
                        return;

                    setter(part, value);
                };
            }
        }
    }

    public delegate void PreSavingEventHandler<T>(T ident, PreSavingContext ctx) where T : Entity;
    public delegate void RetrievedEventHandler<T>(T ident) where T : Entity;
    public delegate void SavingEventHandler<T>(T ident) where T : Entity;
    public delegate void SavedEventHandler<T>(T ident, SavedEventArgs args) where T : Entity;
    public delegate FilterQueryResult<T> FilterQueryEventHandler<T>() where T : Entity;
    public delegate void AlternativeRetriveEventHandler<T>(PrimaryKey id, AlternativeRetrieveArgs<T> args) where T : Entity;

    public delegate IDisposable PreUnsafeDeleteHandler<T>(IQueryable<T> entityQuery);
    public delegate IDisposable PreUnsafeMListDeleteHandler<T>(IQueryable mlistQuery, IQueryable<T> entityQuery);
    public delegate IDisposable PreUnsafeUpdateHandler<T>(IUpdateable update, IQueryable<T> entityQuery);
    public delegate LambdaExpression PreUnsafeInsertHandler<T>(IQueryable query, LambdaExpression constructor, IQueryable<T> entityQuery);
    public delegate void BulkInsetHandler<T>(bool inMListTable);


    public class AlternativeRetrieveArgs<T> where T : Entity
    {
        public bool AvoidAccesVerify { get; set; }
        public T Entity { get; set; }
    }

    public class SavedEventArgs
    {
        public bool IsRoot { get; set; }
        public bool WasNew { get; set; }
        public bool WasSelfModified { get; set; }
    }

    public interface IFilterQueryResult
    {
        LambdaExpression InDatabaseExpression { get; }
    }

    public class FilterQueryResult<T> : IFilterQueryResult where T : Entity
    {
        public FilterQueryResult(Expression<Func<T, bool>> inDatabaseExpression, Func<T, bool> inMemoryFunction)
        {
            this.InDatabaseExpresson = inDatabaseExpression;
            this.InMemoryFunction = inMemoryFunction;
        }

        public readonly Expression<Func<T, bool>> InDatabaseExpresson;
        public readonly Func<T, bool> InMemoryFunction;

        LambdaExpression IFilterQueryResult.InDatabaseExpression { get { return this.InDatabaseExpresson; } }
    }

    internal interface IEntityEvents
    {
        Entity OnAlternativeRetriving(PrimaryKey id);
        void OnPreSaving(Entity entity, PreSavingContext ctx);
        void OnSaving(Entity entity);
        void OnSaved(Entity entity, SavedEventArgs args);

        void OnRetrieved(Entity entity);

        IDisposable OnPreUnsafeUpdate(IUpdateable update);
        LambdaExpression OnPreUnsafeInsert(IQueryable query, LambdaExpression constructor, IQueryable entityQuery);
        void OnPreBulkInsert(bool inMListTable);

        ICacheController CacheController { get; }

        Dictionary<PropertyRoute, IAdditionalBinding> AdditionalBindings { get; }
    }
}
