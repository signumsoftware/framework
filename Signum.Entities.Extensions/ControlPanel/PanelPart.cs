using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reports;
using Signum.Entities.UserQueries;
using Signum.Entities.Chart;
using System.Reflection;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace Signum.Entities.ControlPanel
{
    [Serializable]
    public class PanelPartDN : EmbeddedEntity
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
                    return  ControlPanelMessage.ControlPanelDN_TitleMustBeSpecifiedFor0.NiceToString().Formato(content.GetType().NicePluralName());
            }

            return base.PropertyValidation(pi);
        }

        public PanelPartDN Clone()
        {
            return new PanelPartDN
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

        internal XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Part",
                new XAttribute("Row", Row),
                new XAttribute("Column", Column),
                Title == null ? null : new XAttribute("Title", Title),
                Content.ToXml(ctx));
        }

        internal void FromXml(XElement x, IFromXmlContext ctx)
        {
            Row = int.Parse(x.Attribute("Row").Value);
            Column = int.Parse(x.Attribute("Column").Value);
            Title = x.Attribute("Title").TryCC(a => a.Value);
            Content = ctx.GetPart(Content, x.Elements().Single());
        }
    }

    public interface IPartDN : IIdentifiable
    {
        bool RequiresTitle { get; }
        IPartDN Clone();

        XElement ToXml(IToXmlContext ctx);
        void FromXml(XElement element, IFromXmlContext ctx);
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

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("CountSearchControlPart",
                UserQueries.Select(cuqe => cuqe.ToXml(ctx)));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            UserQueries.Syncronize(element.Elements().ToList(), (cuqe, x) => cuqe.FromXml(element, ctx));
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

        internal XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("CountUserQueryElement",
                Label == null ? null : new XAttribute("Label", Label),
                Href == null ? null : new XAttribute("Href", Href),
                new XAttribute("UserQuery", ctx.Include(UserQuery)));
        }

        internal void FromXml(XElement element, IFromXmlContext ctx)
        {
            Label = element.Attribute("Label").TryCC(a => a.Value);
            Href = element.Attribute("Href").TryCC(a => a.Value);
            UserQuery = (UserQueryDN)ctx.GetEntity(Guid.Parse(element.Attribute("UserQuery").Value));
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
