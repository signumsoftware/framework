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
    public class PanelPartEntity : EmbeddedEntity, IGridEntity
    {
        string title;
        public string Title
        {
            get { return title; }
            set { Set(ref title, value); }
        }

        int row;
        [NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, 0)]
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

        [ImplementedBy(typeof(UserChartPartEntity), typeof(UserQueryPartEntity), typeof(CountSearchControlPartEntity), typeof(LinkListPartEntity))]
        IPartEntity content;
        public IPartEntity Content
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
                    return DashboardMessage.DashboardDN_TitleMustBeSpecifiedFor0.NiceToString().FormatWith(content.GetType().NicePluralName());
            }

            return base.PropertyValidation(pi);
        }

        public PanelPartEntity Clone()
        {
            return new PanelPartEntity
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

    public interface IPartEntity : IEntity
    {
        bool RequiresTitle { get; }
        IPartEntity Clone();

        XElement ToXml(IToXmlContext ctx);
        void FromXml(XElement element, IFromXmlContext ctx);
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class UserQueryPartEntity : Entity, IPartEntity
    {
        [NotNullable]
        UserQueryEntity userQuery;
        [NotNullValidator]
        public UserQueryEntity UserQuery
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

        public IPartEntity Clone()
        {
            return new UserQueryPartEntity
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
            UserQuery = (UserQueryEntity)ctx.GetEntity(Guid.Parse(element.Attribute("UserQuery").Value));
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class UserChartPartEntity : Entity, IPartEntity
    {
        [NotNullable]
        UserChartEntity userChart;
        [NotNullValidator]
        public UserChartEntity UserChart
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

        public IPartEntity Clone()
        {
            return new UserChartPartEntity
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
            UserChart = (UserChartEntity)ctx.GetEntity(Guid.Parse(element.Attribute("UserChart").Value));
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class CountSearchControlPartEntity : Entity, IPartEntity
    {
        [NotNullable]
        MList<CountUserQueryElementEntity> userQueries = new MList<CountUserQueryElementEntity>();
        public MList<CountUserQueryElementEntity> UserQueries
        {
            get { return userQueries; }
            set { Set(ref userQueries, value); }
        }

        public override string ToString()
        {
            return "{0} {1}".FormatWith(userQueries.Count, typeof(UserQueryEntity).NicePluralName());
        }

        public bool RequiresTitle
        {
            get { return true; }
        }

        public IPartEntity Clone()
        {
            return new CountSearchControlPartEntity
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
    public class CountUserQueryElementEntity : EmbeddedEntity
    {
        string label;
        public string Label
        {
            get { return label ?? UserQuery.Try(uq => uq.DisplayName); }
            set { Set(ref label, value); }
        }

        UserQueryEntity userQuery;
        [NotNullValidator]
        public UserQueryEntity UserQuery
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
        public CountUserQueryElementEntity Clone()
        {
            return new CountUserQueryElementEntity
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
            UserQuery = (UserQueryEntity)ctx.GetEntity(Guid.Parse(element.Attribute("UserQuery").Value));
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class LinkListPartEntity : Entity, IPartEntity
    {
        [NotNullable]
        MList<LinkElementEntity> links = new MList<LinkElementEntity>();
        public MList<LinkElementEntity> Links
        {
            get { return links; }
            set { Set(ref links, value); }
        }

        public override string ToString()
        {
            return "{0} {1}".FormatWith(links.Count, typeof(LinkElementEntity).NicePluralName());
        }

        public bool RequiresTitle
        {
            get { return true; }
        }

        public IPartEntity Clone()
        {
            return new LinkListPartEntity
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
    public class LinkElementEntity : EmbeddedEntity
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
        [URLValidator, StringLengthValidator(AllowNulls = false)]
        public string Link
        {
            get { return link; }
            set { Set(ref link, value); }
        }

        public LinkElementEntity Clone()
        {
            return new LinkElementEntity
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
