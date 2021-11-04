using System;
using System.Linq;
using Signum.Utilities;
using Signum.Entities.UserAssets;
using Signum.Entities.Chart;
using System.Reflection;
using System.Xml.Linq;
using Signum.Utilities.DataStructures;
using Signum.Entities.UserQueries;
using Signum.Entities;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using System.ComponentModel;

namespace Signum.Entities.Dashboard
{
    [Serializable]
    public class PanelPartEmbedded : EmbeddedEntity, IGridEntity
    {
        [StringLengthValidator(Min = 3, Max = 100)]
        public string? Title { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string? IconName { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string? IconColor { get; set; }

        [NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, 0)]
        public int Row { get; set; }

        [NumberBetweenValidator(0, 11)]
        public int StartColumn { get; set; }

        [NumberBetweenValidator(1, 12)]
        public int Columns { get; set; }

        public InteractionGroup? InteractionGroup { get; set; }

        public BootstrapStyle Style { get; set; }

        [ImplementedBy(
            typeof(UserChartPartEntity),
            typeof(CombinedUserChartPartEntity),
            typeof(UserQueryPartEntity),
            typeof(ValueUserQueryListPartEntity),
            typeof(LinkListPartEntity))]
        public IPartEntity Content { get; set; }

        public override string ToString()
        {
            return Title.HasText() ? Title :
                Content==null?"":
                Content.ToString()!;
        }

        protected override string? PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Title) && string.IsNullOrEmpty(Title))
            {
                if (Content != null && Content.RequiresTitle)
                    return DashboardMessage.DashboardDN_TitleMustBeSpecifiedFor0.NiceToString().FormatWith(Content.GetType().NicePluralName());
            }

            return base.PropertyValidation(pi);
        }

        public PanelPartEmbedded Clone()
        {
            return new PanelPartEmbedded
            {
                Columns = Columns,
                StartColumn = StartColumn,
                Content = Content.Clone(),
                Title = Title,
                Row = Row,
                Style = Style,

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
                Title == null ? null! : new XAttribute("Title", Title),
                IconName == null ? null! : new XAttribute("IconName", IconName),
                IconColor == null ? null! : new XAttribute("IconColor", IconColor),
                InteractionGroup == null ? null! : new XAttribute("InteractionGroup", InteractionGroup),
                new XAttribute("Style", Style),
                Content.ToXml(ctx));
        }

        internal void FromXml(XElement x, IFromXmlContext ctx)
        {
            Row = int.Parse(x.Attribute("Row")!.Value);
            StartColumn = int.Parse(x.Attribute("StartColumn")!.Value);
            Columns = int.Parse(x.Attribute("Columns")!.Value);
            Title = x.Attribute("Title")?.Value;
            IconName = x.Attribute("IconName")?.Value;
            IconColor = x.Attribute("IconColor")?.Value;
            Style = x.Attribute("Style")?.Value.TryToEnum<BootstrapStyle>() ?? BootstrapStyle.Light;
            InteractionGroup = x.Attribute("InteractionGroup")?.Value.ToEnum<InteractionGroup>();
            Content = ctx.GetPart(Content, x.Elements().Single());
        }

        internal Interval<int> ColumnInterval()
        {
            return new Interval<int>(this.StartColumn, this.StartColumn + this.Columns);
        }
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
        public UserQueryEntity UserQuery { get; set; }

        public UserQueryPartRenderMode RenderMode { get; set; }

        public bool AllowSelection { get; set; }

        public bool ShowFooter { get; set; }

        public bool CreateNew { get; set; } = false;

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => UserQuery + "");

        public bool RequiresTitle
        {
            get { return false; }
        }

        public IPartEntity Clone()
        {
            return new UserQueryPartEntity
            {
                UserQuery = this.UserQuery,
                RenderMode = this.RenderMode,
                AllowSelection = this.AllowSelection,
                ShowFooter = this.ShowFooter,
                CreateNew = this.CreateNew,
            };
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("UserQueryPart",
                new XAttribute("UserQuery", ctx.Include(UserQuery)),
                new XAttribute("RenderMode", RenderMode.ToString()),
                new XAttribute("AllowSelection", AllowSelection.ToString()),
                new XAttribute("ShowFooter", ShowFooter.ToString()),
                new XAttribute("CreateNew", CreateNew.ToString())
                );
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            UserQuery = (UserQueryEntity)ctx.GetEntity(Guid.Parse(element.Attribute("UserQuery")!.Value));
            RenderMode = element.Attribute("RenderMode")?.Value.ToEnum<UserQueryPartRenderMode>() ?? UserQueryPartRenderMode.SearchControl;
            AllowSelection = element.Attribute("AllowSelection")?.Value.ToBool() ?? true;
            ShowFooter = element.Attribute("ShowFooter")?.Value.ToBool() ?? false;
            CreateNew = element.Attribute("CreateNew")?.Value.ToBool() ?? false;
        }
    }

    public enum InteractionGroup
    {
        [Description("Group 1")] Group1,
        [Description("Group 2")] Group2,
        [Description("Group 3")] Group3,
        [Description("Group 4")] Group4,
        [Description("Group 5")] Group5,
        [Description("Group 6")] Group6,
        [Description("Group 7")] Group7,
        [Description("Group 8")] Group8,
    }

    public enum UserQueryPartRenderMode
    {
        SearchControl,
        BigValue,
    }


    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class UserTreePartEntity : Entity, IPartEntity
    {
        public UserQueryEntity UserQuery { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => UserQuery + "");

        public bool RequiresTitle
        {
            get { return false; }
        }

        public IPartEntity Clone()
        {
            return new UserTreePartEntity
            {
                UserQuery = this.UserQuery,
            };
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("UserTreePart",
                new XAttribute("UserQuery", ctx.Include(UserQuery))
                );
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            UserQuery = (UserQueryEntity)ctx.GetEntity(Guid.Parse(element.Attribute("UserQuery")!.Value));
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class UserChartPartEntity : Entity, IPartEntity
    {   
        public UserChartEntity UserChart { get; set; }

        public bool ShowData { get; set; } = false;

        public bool AllowChangeShowData { get; set; } = false;

        public bool CreateNew { get; set; } = false;

        public bool AutoRefresh { get; set; } = false;

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => UserChart + "");

        public bool RequiresTitle
        {
            get { return false; }
        }

        public IPartEntity Clone()
        {
            return new UserChartPartEntity
            {
                UserChart = this.UserChart,
                ShowData = this.ShowData,
                AllowChangeShowData = this.AllowChangeShowData,
            };
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("UserChartPart",
                new XAttribute("ShowData", ShowData),
                new XAttribute("AllowChangeShowData", AllowChangeShowData),
                CreateNew ? new XAttribute("CreateNew", CreateNew) : null!,
                AutoRefresh ? new XAttribute("AutoRefresh", AutoRefresh) : null!,
                new XAttribute("UserChart", ctx.Include(UserChart)));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            ShowData = element.Attribute("ShowData")?.Value.ToBool() ?? false;
            AllowChangeShowData = element.Attribute("AllowChangeShowData")?.Value.ToBool() ?? false;
            CreateNew = element.Attribute("CreateNew")?.Value.ToBool() ?? false;
            AutoRefresh = element.Attribute("AutoRefresh")?.Value.ToBool() ?? false;
            UserChart = (UserChartEntity)ctx.GetEntity(Guid.Parse(element.Attribute("UserChart")!.Value));
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class CombinedUserChartPartEntity : Entity, IPartEntity
    {
        [PreserveOrder, NoRepeatValidator]
        public MList<UserChartEntity> UserCharts { get; set; } = new MList<UserChartEntity>();

        public bool ShowData { get; set; } = false;

        public bool AllowChangeShowData { get; set; } = false;

        public bool CombinePinnedFiltersWithSameLabel { get; set; } = true;

        public bool UseSameScale { get; set; }

        public override string ToString()
        {
            return UserCharts.ToString(", ");
        }

        public bool RequiresTitle
        {
            get { return true; }
        }

        public IPartEntity Clone()
        {
            return new CombinedUserChartPartEntity
            {
                UserCharts = this.UserCharts.ToMList(),
            };
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("CombinedUserChartPart",
                new XAttribute("ShowData", ShowData),
                new XAttribute("AllowChangeShowData", AllowChangeShowData),
                new XAttribute("CombinePinnedFiltersWithSameLabel", CombinePinnedFiltersWithSameLabel),
                new XAttribute("UseSameScale", UseSameScale),
                UserCharts.Select(uc => new XElement("UserChart", new XAttribute("Guid", ctx.Include(uc)))));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            var newUserCharts = element.Elements("UserChart").Select(uc => (UserChartEntity)ctx.GetEntity(Guid.Parse(uc.Attribute("Guid")!.Value))).ToList();
            ShowData = element.Attribute("ShowData")?.Value.ToBool() ?? false;
            AllowChangeShowData = element.Attribute("AllowChangeShowData")?.Value.ToBool() ?? false;
            CombinePinnedFiltersWithSameLabel = element.Attribute("CombinePinnedFiltersWithSameLabel")?.Value.ToBool() ?? false;
            UseSameScale = element.Attribute("UseSameScale")?.Value.ToBool() ?? false;
            UserCharts.Synchronize(newUserCharts);
        }
    }


    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class ValueUserQueryListPartEntity : Entity, IPartEntity
    {   
        public MList<ValueUserQueryElementEmbedded> UserQueries { get; set; } = new MList<ValueUserQueryElementEmbedded>();

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
            return new ValueUserQueryListPartEntity
            {
                UserQueries = this.UserQueries.Select(e => e.Clone()).ToMList(),
            };
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("ValueUserQueryListPart",
                UserQueries.Select(cuqe => cuqe.ToXml(ctx)));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            UserQueries.Synchronize(element.Elements().ToList(), (cuqe, x) => cuqe.FromXml(x, ctx));
        }
    }

    [Serializable]
    public class ValueUserQueryElementEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(Max = 200)]
        public string? Label { get; set; }
        
        public UserQueryEntity UserQuery { get; set; }

        [StringLengthValidator(Max = 200)]
        public string? Href { get; set; }

        public ValueUserQueryElementEmbedded Clone()
        {
            return new ValueUserQueryElementEmbedded
            {
                Href = this.Href,
                Label = this.Label,
                UserQuery = UserQuery,
            };
        }

        internal XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("ValueUserQueryElement",
                Label == null ? null! : new XAttribute("Label", Label),
                Href == null ? null! : new XAttribute("Href", Href),
                new XAttribute("UserQuery", ctx.Include(UserQuery)));
        }

        internal void FromXml(XElement element, IFromXmlContext ctx)
        {
            Label = element.Attribute("Label")?.Value;
            Href = element.Attribute("Href")?.Value;
            UserQuery = (UserQueryEntity)ctx.GetEntity(Guid.Parse(element.Attribute("UserQuery")!.Value));
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class LinkListPartEntity : Entity, IPartEntity
    {
        
        public MList<LinkElementEmbedded> Links { get; set; } = new MList<LinkElementEmbedded>();

        public override string ToString()
        {
            return "{0} {1}".FormatWith(Links.Count, typeof(LinkElementEmbedded).NicePluralName());
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
            Links.Synchronize(element.Elements().ToList(), (le, x) => le.FromXml(x));
        }
    }

    [Serializable]
    public class LinkElementEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(Max = 200)]
        public string Label { get; set; }

        [URLValidator(absolute: true, aspNetSiteRelative: true), StringLengthValidator(Max = int.MaxValue)]
        public string Link { get; set; }

        public LinkElementEmbedded Clone()
        {
            return new LinkElementEmbedded
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
            Label = element.Attribute("Label")!.Value;
            Link = element.Attribute("Link")!.Value;
        }
    }
}
