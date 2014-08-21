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
using Signum.Engine.Exceptions;
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

namespace Signum.Services
{
    public abstract class ServerExtensions : ServerBasic, ILoginServer, IQueryServer, IProcessServer, IDashboardServer,
        IChartServer, IExcelReportServer, IUserQueryServer, IQueryAuthServer, IPropertyAuthServer, IUserAssetsServer,
        ITypeAuthServer, IPermissionAuthServer, IOperationAuthServer, ISmsServer,
        IProfilerServer, IDiffLogServer, IIsolationServer
    {
        public override IdentifiableEntity Retrieve(Type type, int id)
        {
            using (ViewLogLogic.LogView(Lite.Create(type, id), "WCFRetrieve"))
                return base.Retrieve(type, id);
        }


        #region ILoginServer Members
        public virtual void Login(string username, string passwordHash)
        {
            Execute(MethodInfo.GetCurrentMethod(), null, () =>
            {
                UserDN.Current = AuthLogic.Login(username, passwordHash);
            });
        }

        public virtual void ChagePassword(Lite<UserDN> user, string passwordHash, string newPasswordHash)
        {
            Execute(MethodInfo.GetCurrentMethod(), () =>
            {
                AuthLogic.ChangePassword(user, passwordHash, newPasswordHash);
            });
        }


        public virtual void LoginChagePassword(string username, string passwordHash, string newPasswordHash)
        {
            Execute(MethodInfo.GetCurrentMethod(), null, () =>
            {
                UserDN.Current = AuthLogic.ChangePasswordLogin(username, passwordHash, newPasswordHash);
            });
        }

        public UserDN GetCurrentUser()
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => UserDN.Current);
        }

        public string PasswordNearExpired()
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => AuthLogic.OnLoginMessage());
        }

        #endregion

        #region IProcessServer
        public ProcessDN CreatePackageOperation(IEnumerable<Lite<IIdentifiable>> lites, OperationSymbol operationSymbol, params object[] operationArgs)
        {
            return Return(MethodInfo.GetCurrentMethod(), null,
                () => PackageLogic.CreatePackageOperation(lites, operationSymbol, operationArgs));
        }
        #endregion

        #region IQueryServer Members
        public QueryDN GetQuery(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => QueryLogic.GetQuery(queryName));
        }

        #endregion

        #region IPropertyAuthServer Members
        public PropertyRulePack GetPropertyRules(Lite<RoleDN> role, TypeDN typeDN)
        {
            return Return(MethodInfo.GetCurrentMethod(),
             () => PropertyAuthLogic.GetPropertyRules(role, typeDN));
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

        public TypeRulePack GetTypesRules(Lite<RoleDN> role)
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

        public bool IsAllowedForInUserInterface(Lite<IIdentifiable> lite, TypeAllowedBasic allowed)
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

        public QueryRulePack GetQueryRules(Lite<RoleDN> role, TypeDN typeDN)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => QueryAuthLogic.GetQueryRules(role, typeDN));
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

        public PermissionRulePack GetPermissionRules(Lite<RoleDN> role)
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
        public OperationRulePack GetOperationRules(Lite<RoleDN> role, TypeDN typeDN)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => OperationAuthLogic.GetOperationRules(role, typeDN));
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

        public List<Lite<UserChartDN>> GetUserCharts(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => UserChartLogic.GetUserCharts(queryName));
        }

        public List<Lite<UserChartDN>> GetUserChartsEntity(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => UserChartLogic.GetUserChartsEntity(entityType));
        }

        public List<Lite<UserChartDN>> AutocompleteUserChart(string subString, int limit)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => UserChartLogic.Autocomplete(subString, limit));
        }

        public UserChartDN RetrieveUserChart(Lite<UserChartDN> userChart)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                   () => UserChartLogic.RetrieveUserChart(userChart));
        }

        public List<ChartScriptDN> GetChartScripts()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                    () => ChartScriptLogic.Scripts.Value.Values.ToList());
        }
        #endregion

        #region IExcelReportServer Members

        public List<Lite<ExcelReportDN>> GetExcelReports(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => ExcelLogic.GetExcelReports(queryName));
        }

        public byte[] ExecuteExcelReport(Lite<ExcelReportDN> excelReport, QueryRequest request)
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
        public List<Lite<UserQueryDN>> GetUserQueries(object queryName)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => UserQueryLogic.GetUserQueries(queryName));
        }

        public List<Lite<UserQueryDN>> GetUserQueriesEntity(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => UserQueryLogic.GetUserQueriesEntity(entityType));
        }

        public List<Lite<UserQueryDN>> AutocompleteUserQueries(string subString, int limit)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                  () => UserQueryLogic.Autocomplete(subString, limit));
        }

        public UserQueryDN RetrieveUserQuery(Lite<UserQueryDN> userQuery)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                 () => UserQueryLogic.RetrieveUserQuery(userQuery));
        }
        #endregion

        #region SMS Members
        public string GetPhoneNumber(IdentifiableEntity ie)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => SMSLogic.GetPhoneNumber(ie));
        }

        public List<string> GetLiteralsFromDataObjectProvider(TypeDN type)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => SMSLogic.GetLiteralsFromDataObjectProvider(type.ToType()));
        }

        public List<Lite<TypeDN>> GetAssociatedTypesForTemplates()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => SMSLogic.RegisteredDataObjectProviders());
        }

        public CultureInfoDN GetDefaultCulture()
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
        public DashboardDN GetHomePageDashboard()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => DashboardLogic.GetHomePageDashboard());
        }

        public DashboardDN GetEmbeddedDashboard(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => DashboardLogic.GetEmbeddedDashboard(entityType));
        }

        public List<Lite<DashboardDN>> GetDashboardsEntity(Type entityType)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                () => DashboardLogic.GetDashboardsEntity(entityType));
        }

        public List<Lite<DashboardDN>> AutocompleteDashboard(string subString, int limit)
        {
            return Return(MethodInfo.GetCurrentMethod(),
                  () => DashboardLogic.Autocomplete(subString, limit));
        }

        public List<Lite<DashboardDN>> GetDashboards()
        {
            return Return(MethodInfo.GetCurrentMethod(),
                  () => DashboardLogic.GetDashboards());
        }

        public DashboardDN RetrieveDashboard(Lite<DashboardDN> dashboard)
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


       public MinMax<OperationLogDN> OperationLogNextPrev(OperationLogDN log)
        {
            return Return(MethodInfo.GetCurrentMethod(),
              () => DiffLogLogic.OperationLogNextPrev(log));
        }

         #region IIsolationServer
        public Lite<IsolationDN> GetOnlyIsolation(List<Lite<IdentifiableEntity>> selectedEntities)
        {
            return Return(MethodInfo.GetCurrentMethod(),
            () => IsolationLogic.GetOnlyIsolation(selectedEntities));
        }
        #endregion
    }
}
