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
        public string Title { get; set; }

        [NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, 0)]
        public int Row { get; set; }

        [NumberBetweenValidator(0, 11)]
        public int StartColumn { get; set; }

        [NumberBetweenValidator(1, 12)]
        public int Columns { get; set; }

        public PanelStyle Style { get; set; }

        [ImplementedBy(typeof(UserChartPartEntity), typeof(UserQueryPartEntity), typeof(CountSearchControlPartEntity), typeof(LinkListPartEntity))]
        public IPartEntity Content { get; set; }

        public override string ToString()
        {
            return Title.HasText() ? Title : Content.ToString();
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Title) && string.IsNullOrEmpty(Title))
            {
                if (Content != null && Content.RequiresTitle)
                    return DashboardMessage.DashboardDN_TitleMustBeSpecifiedFor0.NiceToString().FormatWith(Content.GetType().NicePluralName());
            }

            return base.PropertyValidation(pi);
        }

        public PanelPartEntity Clone()
        {
            return new PanelPartEntity
            {
                Columns = Columns,
                StartColumn = StartColumn,
                Content = Content.Clone(),
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
            Title = x.Attribute("Title")?.Value;
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
        [NotNullValidator]
        public UserQueryEntity UserQuery { get; set; }
        
        public bool AllowSelection { get; set; }
        
        public override string ToString()
        {
            return UserQuery?.ToString();
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
        [NotNullValidator]
        public UserChartEntity UserChart { get; set; }

        public bool ShowData { get; set; } = false;

        public override string ToString()
        {
            return UserChart?.ToString();
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
        public MList<CountUserQueryElementEntity> UserQueries { get; set; } = new MList<CountUserQueryElementEntity>();

        public override string ToString()
        {
            return "{0} {1}".FormatWith(UserQueries.Count, typeof(UserQueryEntity).NicePluralName());
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
            get { return label ?? UserQuery?.DisplayName; }
            set { Set(ref label, value); }
        }

        [NotNullValidator]
        public UserQueryEntity UserQuery { get; set; }

        public string Href { get; set; }

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
            Label = element.Attribute("Label")?.Value;
            Href = element.Attribute("Href")?.Value;
            UserQuery = (UserQueryEntity)ctx.GetEntity(Guid.Parse(element.Attribute("UserQuery").Value));
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class LinkListPartEntity : Entity, IPartEntity
    {
        [NotNullable]
        public MList<LinkElementEntity> Links { get; set; } = new MList<LinkElementEntity>();

        public override string ToString()
        {
            return "{0} {1}".FormatWith(Links.Count, typeof(LinkElementEntity).NicePluralName());
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
        [NotNullValidator]
        public string Label { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        [URLValidator(absolute: true, aspNetSiteRelative: true), StringLengthValidator(AllowNulls = false)]
        public string Link { get; set; }

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
