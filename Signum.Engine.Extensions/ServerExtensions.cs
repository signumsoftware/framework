using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Signum.Entities;
using Signum.Entities.Basics;
using System.Reflection;
using Signum.Entities.Authorization;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Engine.Authorization;
using Signum.Utilities;
using Signum.Engine.Basics;
using Signum.Entities.Chart;
using Signum.Utilities.DataStructures;
using Signum.Engine.SMS;
using Signum.Entities.UserQueries;
using Signum.Engine.UserQueries;
using Signum.Entities.SMS;
using Signum.Engine.Chart;
using System.IO;
using System.Xml;
using Signum.Engine.Profiler;
using Signum.Entities.Processes;
using Signum.Engine.Processes;
using Signum.Engine.Operations;
using Signum.Entities.Dashboard;
using Signum.Engine.Dashboard;
using Signum.Entities.Scheduler;
using Signum.Entities.Excel;
using Signum.Engine.Excel;
using Signum.Entities.UserAssets;
using Signum.Engine.UserAssets;
using Signum.Engine.ViewLog;
using Signum.Engine.DiffLog;
using Signum.Entities.Isolation;
using Signum.Engine.Isolation;
using Signum.Engine.Help;

namespace Signum.Services
{
    public abstract class ServerExtensions : ServerBasic, ILoginServer, IQueryServer, IProcessServer, IDashboardServer,
        IChartServer, IExcelReportServer, IUserQueryServer, IQueryAuthServer, IPropertyAuthServer, IUserAssetsServer,
        ITypeAuthServer, IPermissionAuthServer, IOperationAuthServer, ISmsServer,
        IProfilerServer, IDiffLogServer, IIsolationServer, IHelpServer
    {
        public override Entity Retrieve(Type type, PrimaryKey id)
        {
            using (ViewLogLogic.LogView(Lite.Create(type, id), "WCFRetrieve"))
                return base.Retrieve(type, id);
        }


        #region ILoginServer Members
        public virtual void Login(string username, byte[] passwordHash)
        {
            Execute(MethodInfo.GetCurrentMethod(), null, () =>
            {
                UserEntity.Current = AuthLogic.Login(username, passwordHash);
            });
        }

        public virtual void ChagePassword(Lite<UserEntity> user, byte[] passwordHash, byte[] newPasswordHash)
        {
            Execute(MethodInfo.GetCurrentMethod(), () =>
            {
                AuthLogic.ChangePassword(user, passwordHash, newPasswordHash);
            });
        }


        public virtual void LoginChagePassword(string username, byte[] passwordHash, byte[] newPasswordHash)
        {
            Execute(MethodInfo.GetCurrentMethod(), null, () =>
            {
                UserEntity.Current = AuthLogic.ChangePasswordLogin(username, passwordHash, newPasswordHash);
            });
        }

        public UserEntity GetCurrentUser()
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => UserEntity.Current);
        }

        public string PasswordNearExpired()
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => AuthLogic.OnLoginMessage());
        }

        #endregion

        #region IProcessServer
        public ProcessEntity CreatePackageOperation(IEnumerable<Lite<IEntity>> lites, OperationSymbol operationSymbol, params object[] operationArgs)
        {
            return Return(MethodInfo.GetCurrentMethod(), null,
                () => PackageLogic.CreatePackageOperation(lites, operationSymbol, operationArgs));
        }
        #endregion

        #region IQueryServer Members
        public QueryEntity GetQuery(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => QueryLogic.GetQueryEntity(queryName));
        }

        #endregion

        #region IPropertyAuthServer Members
        public PropertyRulePack GetPropertyRules(Lite<RoleEntity> role, TypeEntity typeEntity)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => PropertyAuthLogic.GetPropertyRules(role, typeEntity));
        }

        public void SetPropertyRules(PropertyRulePack rules)
        {
            Execute(MethodInfo.GetCurrentMethod(),
             () => PropertyAuthLogic.SetPropertyRules(rules));
        }

        public Dictionary<PropertyRoute, PropertyAllowed> OverridenProperties()
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => PropertyAuthLogic.OverridenProperties());
        }
        #endregion

        #region ITypeAuthServer Members

        public TypeRulePack GetTypesRules(Lite<RoleEntity> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => TypeAuthLogic.GetTypeRules(role));
        }

        public void SetTypesRules(TypeRulePack rules)
        {
            Execute(MethodInfo.GetCurrentMethod(),
              () => TypeAuthLogic.SetTypeRules(rules));
        }

        public DefaultDictionary<Type, TypeAllowedAndConditions> AuthorizedTypes()
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => TypeAuthLogic.AuthorizedTypes());
        }

        public bool IsAllowedForInUserInterface(Lite<IEntity> lite, TypeAllowedBasic allowed)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => TypeAuthLogic.IsAllowedFor(lite, allowed, inUserInterface: true));
        }

        public byte[] DownloadAuthRules()
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () =>
              {
                  using (MemoryStream ms = new MemoryStream())
                  {
                      using (XmlWriter wr = new XmlTextWriter(ms, Encoding.UTF8) { Formatting = Formatting.Indented })
                      {

                          AuthLogic.ExportRules().WriteTo(wr);
                      }
                      return ms.ToArray();
                  }
              });
        }

        #endregion

        #region IQueryAuthServer Members

        public QueryRulePack GetQueryRules(Lite<RoleEntity> role, TypeEntity typeEntity)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => QueryAuthLogic.GetQueryRules(role, typeEntity));
        }

        public void SetQueryRules(QueryRulePack rules)
        {
            Execute(MethodInfo.GetCurrentMethod(),
               () => QueryAuthLogic.SetQueryRules(rules));
        }

        public HashSet<object> AllowedQueries()
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => DynamicQueryManager.Current.GetAllowedQueryNames().ToHashSet());
        }

        #endregion

        #region IPermissionAuthServer Members

        public PermissionRulePack GetPermissionRules(Lite<RoleEntity> role)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => PermissionAuthLogic.GetPermissionRules(role));
        }

        public void SetPermissionRules(PermissionRulePack rules)
        {
            Execute(MethodInfo.GetCurrentMethod(),
            () => PermissionAuthLogic.SetPermissionRules(rules));
        }

        public DefaultDictionary<PermissionSymbol, bool> PermissionRules()
        {
            return Return(MethodInfo.GetCurrentMethod(),
           () => PermissionAuthLogic.ServicePermissionRules());
        }

        #endregion

        #region IOperationAuthServer Members
        public OperationRulePack GetOperationRules(Lite<RoleEntity> role, TypeEntity typeEntity)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => OperationAuthLogic.GetOperationRules(role, typeEntity));
        }

        public void SetOperationRules(OperationRulePack rules)
        {
            Execute(MethodInfo.GetCurrentMethod(),
               () => OperationAuthLogic.SetOperationRules(rules));
        }

        public Dictionary<OperationSymbol, OperationAllowed> AllowedOperations()
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => OperationAuthLogic.AllowedOperations());
        }
        #endregion

        #region IChartServer
        public ResultTable ExecuteChart(ChartRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(),
               () => ChartLogic.ExecuteChart(request));
        }

        public List<Lite<UserChartEntity>> GetUserCharts(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => UserChartLogic.GetUserCharts(queryName));
        }

        public List<Lite<UserChartEntity>> GetUserChartsEntity(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => UserChartLogic.GetUserChartsEntity(entityType));
        }

        public List<Lite<UserChartEntity>> AutocompleteUserChart(string subString, int limit)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => UserChartLogic.Autocomplete(subString, limit));
        }

        public UserChartEntity RetrieveUserChart(Lite<UserChartEntity> userChart)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                   () => UserChartLogic.RetrieveUserChart(userChart));
        }

        public List<ChartScriptEntity> GetChartScripts()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                    () => ChartScriptLogic.Scripts.Value.Values.ToList());
        }
        #endregion

        #region IExcelReportServer Members

        public List<Lite<ExcelReportEntity>> GetExcelReports(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => ExcelLogic.GetExcelReports(queryName));
        }

        public byte[] ExecuteExcelReport(Lite<ExcelReportEntity> excelReport, QueryRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => ExcelLogic.ExecuteExcelReport(excelReport, request));
        }

        public byte[] ExecutePlainExcel(QueryRequest request)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => ExcelLogic.ExecutePlainExcel(request));
        }

        #endregion

        #region IUserQueriesServer
        public List<Lite<UserQueryEntity>> GetUserQueries(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => UserQueryLogic.GetUserQueries(queryName));
        }

        public List<Lite<UserQueryEntity>> GetUserQueriesEntity(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => UserQueryLogic.GetUserQueriesEntity(entityType));
        }

        public List<Lite<UserQueryEntity>> AutocompleteUserQueries(string subString, int limit)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                  () => UserQueryLogic.Autocomplete(subString, limit));
        }

        public UserQueryEntity RetrieveUserQuery(Lite<UserQueryEntity> userQuery)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                 () => UserQueryLogic.RetrieveUserQuery(userQuery));
        }
        #endregion

        #region SMS Members
        public string GetPhoneNumber(Entity ie)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => SMSLogic.GetPhoneNumber(ie));
        }

        public List<string> GetLiteralsFromDataObjectProvider(TypeEntity type)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => SMSLogic.GetLiteralsFromDataObjectProvider(type.ToType()));
        }

        public List<Lite<TypeEntity>> GetAssociatedTypesForTemplates()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => SMSLogic.RegisteredDataObjectProviders());
        }

        public CultureInfoEntity GetDefaultCulture()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => SMSLogic.Configuration.DefaultCulture);
        }
        #endregion

        #region Profiler
        public void PushProfilerEntries(List<HeavyProfilerEntry> entries)
        {
            Execute(MethodInfo.GetCurrentMethod(), () =>
                ProfilerLogic.ProfilerEntries(entries));
        }
        #endregion

        #region IDashboardServer
        public DashboardEntity GetHomePageDashboard()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => DashboardLogic.GetHomePageDashboard());
        }

        public DashboardEntity GetEmbeddedDashboard(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => DashboardLogic.GetEmbeddedDashboard(entityType));
        }

        public List<Lite<DashboardEntity>> GetDashboardsEntity(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => DashboardLogic.GetDashboardsEntity(entityType));
        }

        public List<Lite<DashboardEntity>> AutocompleteDashboard(string subString, int limit)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                  () => DashboardLogic.Autocomplete(subString, limit));
        }

        public List<Lite<DashboardEntity>> GetDashboards()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                  () => DashboardLogic.GetDashboards());
        }

        public DashboardEntity RetrieveDashboard(Lite<DashboardEntity> dashboard)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                   () => DashboardLogic.RetrieveDashboard(dashboard));
        }

        #endregion

        #region IUserAssetsServer
        public byte[] ExportAsset(Lite<IUserAssetEntity> asset)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => UserAssetsExporter.ToXml(asset.Retrieve()));
        }

        public UserAssetPreviewModel PreviewAssetImport(byte[] document)
        {
            return Return(MethodInfo.GetCurrentMethod(),
               () => UserAssetsImporter.Preview(document));
        }

        public void AssetImport(byte[] document, UserAssetPreviewModel previews)
        {
            Execute(MethodInfo.GetCurrentMethod(),
              () => UserAssetsImporter.Import(document, previews));
        }
        #endregion


        public MinMax<OperationLogEntity> OperationLogNextPrev(OperationLogEntity log)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => DiffLogLogic.OperationLogNextPrev(log));
        }

        #region IIsolationServer
        public Lite<IsolationEntity> GetOnlyIsolation(List<Lite<Entity>> selectedEntities)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => IsolationLogic.GetOnlyIsolation(selectedEntities));
        }
        #endregion

        #region IHelpServer
        public EntityHelpService GetEntityHelpService(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(),
               () => HelpLogic.GetEntityHelpService(type));
        }

        public bool HasEntityHelpService(Type type)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => HelpLogic.GetEntityHelp(type).HasEntity);
        }

        public QueryHelpService GetQueryHelpService(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => HelpLogic.GetQueryHelpService(queryName));
        }

        public bool HasQueryHelpService(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => HelpLogic.GetQueryHelp(queryName).HasEntity);
        }

        public Dictionary<PropertyRoute, HelpToolTipInfo> GetPropertyRoutesService(List<PropertyRoute> routes)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => HelpLogic.GetPropertyRoutesService(routes));
        } 
        #endregion
    }
}
