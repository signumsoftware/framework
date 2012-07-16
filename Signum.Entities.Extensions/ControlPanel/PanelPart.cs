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

        int row;
        public int Row
        {
            get { return row; }
            set { Set(ref row, value, () => Row); }
        }

        int column;
        public int Column
        {
            get { return column; }
            set { Set(ref column, value, () => Column); }
        }

        [ImplementedBy(typeof(UserChartPartDN), typeof(UserQueryPartDN), typeof(CountSearchControlPartDN), typeof(LinkListPartDN))]
        IPanelPartContent content;
        public IPanelPartContent Content
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
                if (content != null && content.RequiresTitle)
                    return  Resources.ControlPanelDN_TitleMustBeSpecifiedFor0.Formato(content.GetType().NicePluralName());
            }

            return base.PropertyValidation(pi);
        }
    }

    public interface IPanelPartContent : IIdentifiable
    {
        bool RequiresTitle { get; }
    }

    [Serializable]
    public class UserQueryPartDN : Entity, IPanelPartContent
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

        public bool RequiresTitle
        {
            get { return false; }
        }
    }

    [Serializable]
    public class UserChartPartDN : Entity, IPanelPartContent
    {
        UserChartDN userChart;
        [NotNullValidator]
        public UserChartDN UserChart
        {
            get { return userChart; }
            set { Set(ref userChart, value, () => UserChart); }
        }

        static readonly Expression<Func<UserChartPartDN, string>> ToStringExpression = e => e.userChart.ToString();
        public override string ToString()
        {
            return userChart == null ? null : ToStringExpression.Evaluate(this);
        }

        public bool RequiresTitle
        {
            get { return false; }
        }
    }

    [Serializable]
    public class CountSearchControlPartDN : Entity, IPanelPartContent
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

        public bool RequiresTitle
        {
            get { return true; }
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
    }

    [Serializable]
    public class LinkListPartDN : Entity, IPanelPartContent
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

        public bool RequiresTitle
        {
            get { return true; }
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
