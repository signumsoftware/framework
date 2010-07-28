using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Reports;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Basics;
using Signum.Engine;

namespace Signum.Web.Queries.Models
{
    [Serializable(), Reactive]
    public class UserQueryModel : Entity
    {
        public UserQueryModel() { }

        public UserQueryModel(UserQueryDN userQuery)
        {
            IdUserQuery = userQuery.IdOrNull;
            Query = userQuery.Query;
            DisplayName = userQuery.DisplayName;

            Filters = userQuery.Filters.Select(qf => new QueryFilterModel(qf, userQuery.Query.Key)).ToMList();
            Columns = userQuery.Columns.Select(qc => new QueryColumnModel(qc, userQuery.Query.Key)).ToMList();
            Orders = userQuery.Orders.Select(qo => new QueryOrderModel(qo, userQuery.Query.Key)).ToMList();
            Top = userQuery.MaxItems;
        }

        public const string IdUserQueryKey = "IdUserQuery";
        int? idUserQuery;
        public int? IdUserQuery
        {
            get { return idUserQuery; }
            set { Set(ref idUserQuery, value, () => IdUserQuery); }
        }

        QueryDN query;
        [NotNullValidator]
        public QueryDN Query
        {
            get { return query; }
            set { Set(ref query, value, () => Query); }
        }

        [NotNullable]
        string displayName;
        [StringLengthValidator(Min = 1)]
        public string DisplayName
        {
            get { return displayName; }
            set { SetToStr(ref displayName, value, () => DisplayName); }
        }

        [NotNullable]
        MList<QueryFilterModel> filters;
        public MList<QueryFilterModel> Filters
        {
            get { return filters; }
            set { Set(ref filters, value, () => Filters); }
        }

        [NotNullable]
        MList<QueryColumnModel> columns;
        public MList<QueryColumnModel> Columns
        {
            get { return columns; }
            set { Set(ref columns, value, () => Columns); }
        }

        [NotNullable]
        MList<QueryOrderModel> orders;
        public MList<QueryOrderModel> Orders
        {
            get { return orders; }
            set { Set(ref orders, value, () => Orders); }
        }

        int? top;
        public int? Top
        {
            get { return top; }
            set { Set(ref top, value, () => Top); }
        }

        public UserQueryDN ToUserQueryDN()
        {
            UserQueryDN uq = IdUserQuery != null ? Database.Retrieve<UserQueryDN>(IdUserQuery.Value) : new UserQueryDN();

            uq.Query = this.Query;
            uq.DisplayName = this.DisplayName;
            uq.Filters = this.Filters.Select(qfm => qfm.ToQueryFilterDN()).ToMList();
            uq.Columns = this.Columns.Select(qcm => qcm.ToQueryColumnDN()).ToMList();
            uq.Orders = this.Orders.Select(qom => qom.ToQueryOrderDN()).ToMList();
            uq.MaxItems = this.top;

            return uq;
        }
    }
}
