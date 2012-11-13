using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reports;
using Signum.Entities.UserQueries;
using Signum.Entities.Chart;
using System.Reflection;
using Signum.Entities.Extensions.Properties;
using System.Linq.Expressions;

namespace Signum.Entities.ControlPanel
{
    [Serializable]
    public class PanelPart : EmbeddedEntity
    {
        string title;
        public string Title
        {
            get { return title; }
            set { Set(ref title, value, () => Title); }
        }

        int row = 1;
        [NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int Row
        {
            get { return row; }
            set { Set(ref row, value, () => Row); }
        }

        int column = 1;
        [NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int Column
        {
            get { return column; }
            set { Set(ref column, value, () => Column); }
        }

        [ImplementedBy(typeof(UserChartPartDN), typeof(UserQueryPartDN), typeof(CountSearchControlPartDN), typeof(LinkListPartDN))]
        IIdentifiable content;
        public IIdentifiable Content
        {
            get { return content; }
            set { Set(ref content, value, () => Content); }
        }

        public override string ToString()
        {
            return title.HasText() ? title : content.ToString();
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => Title) && string.IsNullOrEmpty(title))
            {
                if (content != null && (content.GetType() == typeof(CountSearchControlPartDN) || content.GetType() == typeof(LinkListPartDN)))
                    return Resources.ControlPanelDN_PartTitleMustBeSpecifiedForListParts;
            }

            return base.PropertyValidation(pi);
        }
    }

    [Serializable]
    public class UserQueryPartDN : Entity
    {
        UserQueryDN userQuery;
        [NotNullValidator]
        public UserQueryDN UserQuery
        {
            get { return userQuery; }
            set { Set(ref userQuery, value, () => UserQuery); }
        }

        static readonly Expression<Func<UserQueryPartDN, string>> ToStringExpression = e => e.userQuery.ToString();
        public override string ToString()
        {
            return userQuery == null ? null : ToStringExpression.Evaluate(this);
        }
    }

    [Serializable]
    public class UserChartPartDN : Entity
    {
        UserChartDN userChart;
        [NotNullValidator]
        public UserChartDN UserChart
        {
            get { return userChart; }
            set { Set(ref userChart, value, () => UserChart); }
        }

        bool onlyData = false;
        public bool OnlyData
        {
            get { return onlyData; }
            set { Set(ref onlyData, value, () => OnlyData); }
        }

        static readonly Expression<Func<UserChartPartDN, string>> ToStringExpression = e => e.userChart.ToString();
        public override string ToString()
        {
            return userChart == null ? null : ToStringExpression.Evaluate(this);
        }
    }

    [Serializable]
    public class CountSearchControlPartDN : Entity
    {
        MList<CountUserQueryElement> userQueries = new MList<CountUserQueryElement>();
        public MList<CountUserQueryElement> UserQueries
        {
            get { return userQueries; }
            set { Set(ref userQueries, value, () => UserQueries); }
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(userQueries.Count, typeof(UserQueryDN).NicePluralName());
        }
    }

    [Serializable]
    public class CountUserQueryElement : EmbeddedEntity
    {
        string label;
        public string Label
        {
            get { return label ?? UserQuery.TryCC(uq => uq.DisplayName); }
            set { Set(ref label, value, () => Label); }
        }

        UserQueryDN userQuery;
        [NotNullValidator]
        public UserQueryDN UserQuery
        {
            get { return userQuery; }
            set { Set(ref userQuery, value, () => UserQuery); }
        }

        string href;
        public string Href
        {
            get { return href; }
            set { Set(ref href, value, () => Href); }
        }
    }

    [Serializable]
    public class LinkListPartDN : Entity
    {
        MList<LinkElement> links = new MList<LinkElement>();
        public MList<LinkElement> Links
        {
            get { return links; }
            set { Set(ref links, value, () => Links); }
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(links.Count, typeof(LinkElement).NicePluralName());
        }
    }

    [Serializable]
    public class LinkElement : EmbeddedEntity
    {
        string label;
        [NotNullValidator]
        public string Label
        {
            get { return label; }
            set { Set(ref label, value, () => Label); }
        }

        string link;
        [URLValidator, NotNullValidator]
        public string Link
        {
            get { return link; }
            set { Set(ref link, value, () => Link); }
        }
    }
}
