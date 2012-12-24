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
        [NumberIsValidator(ComparisonType.GreaterThanOrEqual, 0)]
        public int Row
        {
            get { return row; }
            set { Set(ref row, value, () => Row); }
        }

        int column;
        [NumberIsValidator(ComparisonType.GreaterThanOrEqual, 0)]
        public int Column
        {
            get { return column; }
            set { Set(ref column, value, () => Column); }
        }

        [ImplementedBy(typeof(UserChartPartDN), typeof(UserQueryPartDN), typeof(CountSearchControlPartDN), typeof(LinkListPartDN))]
        IPartDN content;
        public IPartDN Content
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

        public PanelPart Clone()
        {
            return new PanelPart
            {
                Column = Column,
                Row = Row,
                Content = content.Clone(),
                Title = Title
            };
        }

        internal void NotifyRowColumn()
        {
            Notify(() => Row);
            Notify(() => Column);
        }
    }

    public interface IPartDN : IIdentifiable
    {
        bool RequiresTitle { get; }
        IPartDN Clone();
    }

    [Serializable, EntityKind(EntityKind.Part)]
    public class UserQueryPartDN : Entity, IPartDN
    {
        [NotNullable]
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

        public IPartDN Clone()
        {
            return new UserQueryPartDN
            {
                UserQuery = this.UserQuery,
            };
        }
    }

    [Serializable, EntityKind(EntityKind.Part)]
    public class UserChartPartDN : Entity, IPartDN
    {
        [NotNullable]
        UserChartDN userChart;
        [NotNullValidator]
        public UserChartDN UserChart
        {
            get { return userChart; }
            set { Set(ref userChart, value, () => UserChart); }
        }

        bool showData = false;
        public bool ShowData
        {
            get { return showData; }
            set { Set(ref showData, value, () => ShowData); }
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

        public IPartDN Clone()
        {
            return new UserChartPartDN
            {
                UserChart = this.UserChart,
                ShowData = this.ShowData
            };
        }
    }

    [Serializable, EntityKind(EntityKind.Part)]
    public class CountSearchControlPartDN : Entity, IPartDN
    {
        [NotNullable]
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

        public IPartDN Clone()
        {
            return new CountSearchControlPartDN
            {
                UserQueries = this.UserQueries.Select(e=>e.Clone()).ToMList(),
            };
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
        public CountUserQueryElement Clone()
        {
            return new CountUserQueryElement
            {
                Href = this.Href,
                Label = this.Label,
                UserQuery = UserQuery,
            };
        }
    }

    [Serializable, EntityKind(EntityKind.Part)]
    public class LinkListPartDN : Entity, IPartDN
    {
        [NotNullable]
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

        public IPartDN Clone()
        {
            return new LinkListPartDN
            {
                Links = this.Links.Select(e => e.Clone()).ToMList(),
            };
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

        public LinkElement Clone()
        {
            return new LinkElement
            {
                Label = this.Label,
                Link = this.Link
            };
        }

    }
}
