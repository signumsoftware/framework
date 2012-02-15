using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using System.ServiceModel;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Engine.Linq;

namespace Signum.Services
{
    public abstract class ServerBasic : IBaseServer, IDynamicQueryServer
    {
        protected Dictionary<string, object> session = new Dictionary<string, object>();

        protected T Return<T>(MethodBase mi, Func<T> function)
        {
            return Return(mi, mi.Name, function);
        }

        protected virtual T Return<T>(MethodBase mi, string description, Func<T> function)
        {
            try
            {
                using (ScopeSessionFactory.OverrideSession(session))
                using (ExecutionContext.Scope(GetDefaultExecutionContext(mi, description)))
                {
                    return function();
                }
            }
            catch (Exception e)
            {
                throw new FaultException(e.Message);
            }
            finally
            {
                Statics.CleanThreadContextAndAssert();
            }
        }

        protected void Execute(MethodBase mi, Action action)
        {
            Return(mi, mi.Name, () => { action(); return true; });
        }

        protected void Execute(MethodBase mi, string description, Action action)
        {
            Return(mi, description, () => { action(); return true; });
        }

        public static ExecutionContext GetDefaultExecutionContext(MethodBase mi, string desc)
        {
            return SuggestUserInterfaceAttribute.Suggests(mi) == true ? ExecutionContext.UserInterface : null;
        }

        #region IBaseServer
        [SuggestUserInterface]
        public virtual IdentifiableEntity Retrieve(Type type, int id)
        {
            return Return(MethodInfo.GetCurrentMethod(), type.Name,
                () =>Database.Retrieve(type, id));
        }

        [SuggestUserInterface]
        public virtual IdentifiableEntity Save(IdentifiableEntity entidad)
        {
            return Return(MethodInfo.GetCurrentMethod(), entidad.GetType().Name,
                () =>Database.Save(entidad));
        }

        [SuggestUserInterface]
        public virtual List<Lite> RetrieveAllLite(Type liteType, Implementations implementations)
        {
            return Return(MethodInfo.GetCurrentMethod(), liteType.Name,
                () =>AutoCompleteUtils.RetrieveAllLite(liteType, implementations));
        }

        [SuggestUserInterface]
        public virtual List<Lite> FindLiteLike(Type liteType, Implementations implementations, string subString, int count)
        {
            return Return(MethodInfo.GetCurrentMethod(), liteType.Name,
                () =>AutoCompleteUtils.FindLiteLike(liteType, implementations, subString, count));
        }

        [SuggestUserInterface]
        public virtual Implementations FindImplementations(PropertyRoute entityPath)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => Schema.Current.FindImplementations(entityPath));
        }

        [SuggestUserInterface]
        public virtual List<IdentifiableEntity> RetrieveAll(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(), type.Name,
                () => Database.RetrieveAll(type));
        }

        [SuggestUserInterface]
        public virtual List<IdentifiableEntity> SaveList(List<IdentifiableEntity> list)
        {
            Execute(MethodInfo.GetCurrentMethod(),
                () =>Database.SaveList(list));
            return list;
        }

        [SuggestUserInterface]
        public virtual Dictionary<Type, TypeDN> ServerTypes()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => Schema.Current.TypeToDN);
        }

        [SuggestUserInterface]
        public virtual DateTime ServerNow()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => TimeZoneManager.Now);
        }

        [SuggestUserInterface]
        public virtual List<Lite<TypeDN>> TypesAssignableFrom(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => TypeLogic.TypesAssignableFrom(type));
        }

        [SuggestUserInterface]
        public virtual string GetToStr(Type type, int id)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => Database.GetToStr(type, id));
        }
        #endregion

        #region IDynamicQueryServer
        [SuggestUserInterface]
        public virtual QueryDescription GetQueryDescription(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => DynamicQueryManager.Current.QueryDescription(queryName));
        }

        [SuggestUserInterface]
        public virtual ResultTable ExecuteQuery(QueryRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(), request.QueryName.ToString(),
                () => DynamicQueryManager.Current.ExecuteQuery(request));
        }

        [SuggestUserInterface]
        public virtual int ExecuteQueryCount(QueryCountRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(), request.QueryName.ToString(),
                () => DynamicQueryManager.Current.ExecuteQueryCount(request));
        }

        [SuggestUserInterface]
        public virtual Lite ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(), request.QueryName.ToString(),
                () => DynamicQueryManager.Current.ExecuteUniqueEntity(request));
        }

        [SuggestUserInterface]
        public virtual List<object> GetQueryNames()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => DynamicQueryManager.Current.GetQueryNames());
        }

        [SuggestUserInterface]
        public virtual List<QueryToken> ExternalQueryToken(Type type, QueryToken parent)
        {
            return Return(MethodInfo.GetCurrentMethod(),
               () => DynamicQueryManager.Current.GetExtensions(type, parent).ToList());
        }
        #endregion


       
    }
}
