using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.UserAssets;
using Signum.Entities.Chart;
using System.Reflection;
using System.Linq.Expressions;
using System.Xml.Linq;
using Signum.Utilities.DataStructures;
using Signum.Entities.UserQueries;

namespace Signum.Entities.Dashboard
{
    [Serializable]
    public class PanelPartDN : EmbeddedEntity, IGridEntity
    {
        string title;
        public string Title
        {
            get { return title; }
            set { Set(ref title, value); }
        }

        int row;
        [NumberIsValidator(ComparisonType.GreaterThanOrEqual, 0)]
        public int Row
        {
            get { return row; }
            set { Set(ref row, value); }
        }

        int startColumn;
        [NumberBetweenValidator(0, 11)]
        public int StartColumn
        {
            get { return startColumn; }
            set { Set(ref startColumn, value); }
        }

        int columns;
        [NumberBetweenValidator(1, 12)]
        public int Columns
        {
            get { return columns; }
            set { Set(ref columns, value); }
        }

        PanelStyle style;
        public PanelStyle Style
        {
            get { return style; }
            set { Set(ref style, value); }
        }

        [ImplementedBy(typeof(UserChartPartDN), typeof(UserQueryPartDN), typeof(CountSearchControlPartDN), typeof(LinkListPartDN))]
        IPartDN content;
        public IPartDN Content
        {
            get { return content; }
            set { Set(ref content, value); }
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
                    return DashboardMessage.DashboardDN_TitleMustBeSpecifiedFor0.NiceToString().Formato(content.GetType().NicePluralName());
            }

            return base.PropertyValidation(pi);
        }

        public PanelPartDN Clone()
        {
            return new PanelPartDN
            {
                Columns = Columns,
                StartColumn = StartColumn,
                Content = content.Clone(),
                Title = Title
            };
        }

        internal void NotifyRowColumn()
        {
            Notify(() => StartColumn);
            Notify(() => Columns);
        }

        internal XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Part",
                new XAttribute("Row", Row),
                new XAttribute("StartColumn", StartColumn),
                new XAttribute("Columns", Columns),
                Title == null ? null : new XAttribute("Title", Title),
                Content.ToXml(ctx));
        }

        internal void FromXml(XElement x, IFromXmlContext ctx)
        {
            Row = int.Parse(x.Attribute("Row").Value);
            StartColumn = int.Parse(x.Attribute("StartColumn").Value);
            Columns = int.Parse(x.Attribute("Columns").Value);
            Title = x.Attribute("Title").Try(a => a.Value);
            Content = ctx.GetPart(Content, x.Elements().Single());
        }

        internal Interval<int> ColumnInterval()
        {
            return new Interval<int>(this.StartColumn, this.StartColumn + this.Columns);
        }
    }

    public enum PanelStyle
    {
        Default,
        Primary,
        Success,
        Info,
        Warning,
        Danger
    }

    public interface IGridEntity
    {
        int Row { get; set; }
        int StartColumn { get; set; }
        int Columns { get; set; }
    }

    public interface IPartDN : IIdentifiable
    {
        bool RequiresTitle { get; }
        IPartDN Clone();

        XElement ToXml(IToXmlContext ctx);
        void FromXml(XElement element, IFromXmlContext ctx);
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class UserQueryPartDN : Entity, IPartDN
    {
        [NotNullable]
        UserQueryDN userQuery;
        [NotNullValidator]
        public UserQueryDN UserQuery
        {
            get { return userQuery; }
            set { Set(ref userQuery, value); }
        }

        public override string ToString()
        {
            return userQuery.TryToString();
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

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("UserQueryPart",
                new XAttribute("UserQuery", ctx.Include(UserQuery)));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            UserQuery = (UserQueryDN)ctx.GetEntity(Guid.Parse(element.Attribute("UserQuery").Value));
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class UserChartPartDN : Entity, IPartDN
    {
        [NotNullable]
        UserChartDN userChart;
        [NotNullValidator]
        public UserChartDN UserChart
        {
            get { return userChart; }
            set { Set(ref userChart, value); }
        }

        bool showData = false;
        public bool ShowData
        {
            get { return showData; }
            set { Set(ref showData, value); }
        }

        public override string ToString()
        {
            return userChart.TryToString();
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

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("UserChartPart",
                new XAttribute("ShowData", ShowData),
                new XAttribute("UserChart", ctx.Include(UserChart)));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            UserChart = (UserChartDN)ctx.GetEntity(Guid.Parse(element.Attribute("UserChart").Value));
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class CountSearchControlPartDN : Entity, IPartDN
    {
        [NotNullable]
        MList<CountUserQueryElementDN> userQueries = new MList<CountUserQueryElementDN>();
        public MList<CountUserQueryElementDN> UserQueries
        {
            get { return userQueries; }
            set { Set(ref userQueries, value); }
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
                UserQueries = this.UserQueries.Select(e => e.Clone()).ToMList(),
            };
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("CountSearchControlPart",
                UserQueries.Select(cuqe => cuqe.ToXml(ctx)));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            UserQueries.Syncronize(element.Elements().ToList(), (cuqe, x) => cuqe.FromXml(x, ctx));
        }
    }

    [Serializable]
    public class CountUserQueryElementDN : EmbeddedEntity
    {
        string label;
        public string Label
        {
            get { return label ?? UserQuery.Try(uq => uq.DisplayName); }
            set { Set(ref label, value); }
        }

        UserQueryDN userQuery;
        [NotNullValidator]
        public UserQueryDN UserQuery
        {
            get { return userQuery; }
            set { Set(ref userQuery, value); }
        }

        string href;
        public string Href
        {
            get { return href; }
            set { Set(ref href, value); }
        }
        public CountUserQueryElementDN Clone()
        {
            return new CountUserQueryElementDN
            {
                Href = this.Href,
                Label = this.Label,
                UserQuery = UserQuery,
            };
        }

        internal XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("CountUserQueryElement",
                Label == null ? null : new XAttribute("Label", Label),
                Href == null ? null : new XAttribute("Href", Href),
                new XAttribute("UserQuery", ctx.Include(UserQuery)));
        }

        internal void FromXml(XElement element, IFromXmlContext ctx)
        {
            Label = element.Attribute("Label").Try(a => a.Value);
            Href = element.Attribute("Href").Try(a => a.Value);
            UserQuery = (UserQueryDN)ctx.GetEntity(Guid.Parse(element.Attribute("UserQuery").Value));
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class LinkListPartDN : Entity, IPartDN
    {
        [NotNullable]
        MList<LinkElementDN> links = new MList<LinkElementDN>();
        public MList<LinkElementDN> Links
        {
            get { return links; }
            set { Set(ref links, value); }
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(links.Count, typeof(LinkElementDN).NicePluralName());
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

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("LinkListPart",
                Links.Select(lin => lin.ToXml(ctx)));
        }


        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Links.Syncronize(element.Elements().ToList(), (le, x) => le.FromXml(x));
        }
    }

    [Serializable]
    public class LinkElementDN : EmbeddedEntity
    {
        string label;
        [NotNullValidator]
        public string Label
        {
            get { return label; }
            set { Set(ref label, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string link;
        [URLValidator, NotNullValidator]
        public string Link
        {
            get { return link; }
            set { Set(ref link, value); }
        }

        public LinkElementDN Clone()
        {
            return new LinkElementDN
            {
                Label = this.Label,
                Link = this.Link
            };
        }

        internal XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("LinkElement",
                new XAttribute("Label", Label),
                new XAttribute("Link", Link));
        }

        internal void FromXml(XElement element)
        {
            Label = element.Attribute("Label").Value;
            Link = element.Attribute("Link").Value;
        }
    }
}
