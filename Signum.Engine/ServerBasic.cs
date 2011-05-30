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
        public static ExecutionContext GetDefaultExecutionContext(MethodBase mi, string desc)
        {
            return SuggestUserInterfaceAttribute.Suggests(mi) == true ? ExecutionContext.UserInterface : null;
        }

        protected T Return<T>(MethodBase mi, Func<T> function)
        {
            return Return(mi, mi.Name, function);
        }

        protected virtual T Return<T>(MethodBase mi, string description, Func<T> function)
        {
            try
            {
                using (ExecutionContext.Scope(GetDefaultExecutionContext(mi, description)))
                {
                    return function();
                }
            }
            catch (Exception e)
            {
                throw new FaultException(e.Message);
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

        #region IBaseServer
        [SuggestUserInterface]
        public virtual IdentifiableEntity Retrieve(Type type, int id)
        {
            return Return(MethodInfo.GetCurrentMethod(), "Retrieve {0}".Formato(type.Name),
                () =>Database.Retrieve(type, id));
        }

        [SuggestUserInterface]
        public virtual IdentifiableEntity Save(IdentifiableEntity entidad)
        {
            return Return(MethodInfo.GetCurrentMethod(), "Save {0}".Formato(entidad.GetType()),
                () =>Database.Save(entidad));
        }

        [SuggestUserInterface]
        public virtual List<Lite> RetrieveAllLite(Type liteType, Implementations implementations)
        {
            return Return(MethodInfo.GetCurrentMethod(), "RetrieveAllLite {0}".Formato(liteType),
                () =>AutoCompleteUtils.RetriveAllLite(liteType, implementations));
        }

        [SuggestUserInterface]
        public virtual List<Lite> FindLiteLike(Type liteType, Implementations implementations, string subString, int count)
        {
            return Return(MethodInfo.GetCurrentMethod(), "FindLiteLike {0}".Formato(liteType),
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
            return Return(MethodInfo.GetCurrentMethod(), "RetrieveAll {0}".Formato(type),
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
            return Return(MethodInfo.GetCurrentMethod(), "GetQueryResult {0}".Formato(request.QueryName),
                () => DynamicQueryManager.Current.ExecuteQuery(request));
        }

        [SuggestUserInterface]
        public virtual int ExecuteQueryCount(QueryCountRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(), "GetQueryCount {0}".Formato(request.QueryName),
                () => DynamicQueryManager.Current.ExecuteQueryCount(request));
        }

        [SuggestUserInterface]
        public virtual Lite ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(), "GetQueryEntity {0}".Formato(request.QueryName),
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
