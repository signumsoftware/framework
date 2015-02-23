using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Engine.Maps
{
    public class EntityEvents<T> : IEntityEvents
            where T : Entity
    {
        public event PreSavingEventHandler<T> PreSaving;
        public event SavingEventHandler<T> Saving;
        public event SavedEventHandler<T> Saved;

        public event RetrievedEventHandler<T> Retrieved;

        public CacheControllerBase<T> CacheController { get; set; }

        public event FilterQueryEventHandler<T> FilterQuery;

        public event PreUnsafeDeleteHandler<T> PreUnsafeDelete;
        public event PreUnsafeMListDeleteHandler<T> PreUnsafeMListDelete;

        public event PreUnsafeUpdateHandler<T> PreUnsafeUpdate;

        public event PreUnsafeInsertHandler<T> PreUnsafeInsert;
        public event BulkInsetHandler<T> PreBulkInsert;

        internal IEnumerable<FilterQueryResult<T>> OnFilterQuery()
        {
            if (FilterQuery == null)
                return Enumerable.Empty<FilterQueryResult<T>>();

            return FilterQuery.GetInvocationListTyped().Select(f => f()).ToList();
        }

        internal void OnPreUnsafeDelete(IQueryable<T> entityQuery)
        {
            if (PreUnsafeDelete != null)
                foreach (var action in PreUnsafeDelete.GetInvocationListTyped().Reverse())
                    action(entityQuery);
        }

        internal void OnPreUnsafeMListDelete(IQueryable mlistQuery, IQueryable<T> entityQuery)
        {
            if (PreUnsafeMListDelete != null)
                foreach (var action in PreUnsafeMListDelete.GetInvocationListTyped().Reverse())
                    action(mlistQuery, entityQuery);
        }

        void IEntityEvents.OnPreUnsafeUpdate(IUpdateable update)
        {
            if (PreUnsafeUpdate != null)
            {
                var query = update.EntityQuery<T>();
                foreach (var action in PreUnsafeUpdate.GetInvocationListTyped().Reverse())
                    action(update, query);
            }
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

        void IEntityEvents.OnPreSaving(Entity entity, ref bool graphModified)
        {
            if (PreSaving != null)
                PreSaving((T)entity, ref graphModified);
        }

        void IEntityEvents.OnSaving(Entity entity)
        {
            if (Saving != null)
                Saving((T)entity);

        }

        void IEntityEvents.OnSaved(Entity entity, SavedEventArgs args)
        {
            if (Saved != null)
                Saved((T)entity, args);

        }

        void IEntityEvents.OnRetrieved(Entity entity)
        {
            if (Retrieved != null)
                Retrieved((T)entity);
        }

        ICacheController IEntityEvents.CacheController
        {
            get { return CacheController; }
        }
    }

    public delegate void PreSavingEventHandler<T>(T ident, ref bool graphModified) where T : Entity;
    public delegate void RetrievedEventHandler<T>(T ident) where T : Entity;
    public delegate void SavingEventHandler<T>(T ident) where T : Entity;
    public delegate void SavedEventHandler<T>(T ident, SavedEventArgs args) where T : Entity;
    public delegate FilterQueryResult<T> FilterQueryEventHandler<T>() where T : Entity;

    public delegate void PreUnsafeDeleteHandler<T>(IQueryable<T> entityQuery);
    public delegate void PreUnsafeMListDeleteHandler<T>(IQueryable mlistQuery, IQueryable<T> entityQuery);
    public delegate void PreUnsafeUpdateHandler<T>(IUpdateable update, IQueryable<T> entityQuery);
    public delegate LambdaExpression PreUnsafeInsertHandler<T>(IQueryable query, LambdaExpression constructor, IQueryable<T> entityQuery);
    public delegate void BulkInsetHandler<T>(bool inMListTable);

    public class SavedEventArgs
    {
        public bool IsRoot { get; set; }
        public bool WasNew { get; set; }
        public bool WasSelfModified { get; set; }
    }

    public interface IFilterQueryResult
    {
        LambdaExpression InDatabaseExpression{get;}
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
        void OnPreSaving(Entity entity, ref bool graphModified);
        void OnSaving(Entity entity);
        void OnSaved(Entity entity, SavedEventArgs args);

        void OnRetrieved(Entity entity);

        void OnPreUnsafeUpdate(IUpdateable update);
        LambdaExpression OnPreUnsafeInsert(IQueryable query, LambdaExpression constructor, IQueryable entityQuery);
        void OnPreBulkInsert(bool inMListTable);

        ICacheController CacheController { get; }
    }
}
