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
            where T : IdentifiableEntity
    {
        public event PreSavingEventHandler<T> PreSaving;
        public event SavingEventHandler<T> Saving;
        public event SavedEventHandler<T> Saved;

        public event RetrievedEventHandler<T> Retrieved;

        public CacheControllerBase<T> CacheController { get; set; }

        public event FilterQueryEventHandler<T> FilterQuery;

        public event DeleteHandler<T> PreUnsafeDelete;
        public event DeleteMlistHandler<T> PreUnsafeMListDelete;

        public event UpdateHandler<T> PreUnsafeUpdate;

        public event InsertHandler<T> PreUnsafeInsert;
        public event BulkInsetHandler<T> PreBulkInsert;

        internal Expression<Func<T, bool>> OnFilterQuery()
        {
            Expression<Func<T, bool>> result = null;

            if (FilterQuery != null)
                foreach (FilterQueryEventHandler<T> filter in FilterQuery.GetInvocationList())
                    result = Combine(result, filter());

            return result;
        }

        private Expression<Func<T, bool>> Combine(Expression<Func<T, bool>> result, Expression<Func<T, bool>> expression)
        {
            if (result == null)
                return expression;

            if (expression == null)
                return result;

            return a => result.Evaluate(a) && expression.Evaluate(a);
        }

        public bool HasQueryFilter
        {
            get { return FilterQuery != null; }
        }

        internal void OnPreUnsafeDelete(IQueryable<T> entityQuery)
        {
            if (PreUnsafeDelete != null)
                foreach (DeleteHandler<T> action in PreUnsafeDelete.GetInvocationList().Reverse())
                    action(entityQuery);
        }

        internal void OnPreUnsafeMListDelete(IQueryable mlistQuery, IQueryable<T> entityQuery)
        {
            if (PreUnsafeMListDelete != null)
                foreach (DeleteMlistHandler<T> action in PreUnsafeMListDelete.GetInvocationList().Reverse())
                    action(mlistQuery, entityQuery);
        }

        void IEntityEvents.OnPreUnsafeUpdate(IUpdateable update)
        {
            if (PreUnsafeUpdate != null)
            {
                var query = update.EntityQuery<T>();
                foreach (UpdateHandler<T> action in PreUnsafeUpdate.GetInvocationList().Reverse())
                    action(update, query);
            }
        }

        void IEntityEvents.OnPreUnsafeInsert(IQueryable query, LambdaExpression constructor, IQueryable entityQuery)
        {
            if (PreUnsafeInsert != null)
                foreach (InsertHandler<T> action in PreUnsafeInsert.GetInvocationList().Reverse())
                    action(query, constructor, (IQueryable<T>)entityQuery);

        }

        void IEntityEvents.OnPreBulkInsert()
        {
            if (PreBulkInsert != null)
                foreach (BulkInsetHandler<T> action in PreBulkInsert.GetInvocationList().Reverse())
                    action();

        }

        void IEntityEvents.OnPreSaving(IdentifiableEntity entity, ref bool graphModified)
        {
            if (PreSaving != null)
                PreSaving((T)entity, ref graphModified);
        }

        void IEntityEvents.OnSaving(IdentifiableEntity entity)
        {
            if (Saving != null)
                Saving((T)entity);

        }

        void IEntityEvents.OnSaved(IdentifiableEntity entity, SavedEventArgs args)
        {
            if (Saved != null)
                Saved((T)entity, args);

        }

        void IEntityEvents.OnRetrieved(IdentifiableEntity entity)
        {
            if (Retrieved != null)
                Retrieved((T)entity);
        }

        ICacheController IEntityEvents.CacheController
        {
            get { return CacheController; }
        }
    }

    public delegate void PreSavingEventHandler<T>(T ident, ref bool graphModified) where T : IdentifiableEntity;
    public delegate void RetrievedEventHandler<T>(T ident) where T : IdentifiableEntity;
    public delegate void SavingEventHandler<T>(T ident) where T : IdentifiableEntity;
    public delegate void SavedEventHandler<T>(T ident, SavedEventArgs args) where T : IdentifiableEntity;
    public delegate Expression<Func<T, bool>> FilterQueryEventHandler<T>() where T : IdentifiableEntity;

    public delegate void DeleteHandler<T>(IQueryable<T> entityQuery);
    public delegate void DeleteMlistHandler<T>(IQueryable mlistQuery, IQueryable<T> entityQuery);
    public delegate void UpdateHandler<T>(IUpdateable update, IQueryable<T> entityQuery);
    public delegate void InsertHandler<T>(IQueryable query, LambdaExpression constructor, IQueryable<T> entityQuery);
    public delegate void BulkInsetHandler<T>();

    public class SavedEventArgs
    {
        public bool IsRoot { get; set; }
        public bool WasNew { get; set; }
        public bool WasSelfModified { get; set; }
    }

    internal interface IEntityEvents
    {
        void OnPreSaving(IdentifiableEntity entity, ref bool graphModified);
        void OnSaving(IdentifiableEntity entity);
        void OnSaved(IdentifiableEntity entity, SavedEventArgs args);

        void OnRetrieved(IdentifiableEntity entity);

        void OnPreUnsafeUpdate(IUpdateable update);
        void OnPreUnsafeInsert(IQueryable query, LambdaExpression constructor, IQueryable entityQuery);
        void OnPreBulkInsert();

        ICacheController CacheController { get; }

        bool HasQueryFilter { get; }
    }
}
