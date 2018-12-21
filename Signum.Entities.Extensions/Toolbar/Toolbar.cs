using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.Dashboard;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using System.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Entities.Toolbar
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class ToolbarEntity : Entity, IUserAssetEntity
    {
        [ImplementedBy(typeof(UserEntity), typeof(RoleEntity))]
        public Lite<IEntity> Owner { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        public ToolbarLocation Location { get; set; }

        public int? Priority { get; set; }

        [PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<ToolbarElementEmbedded> Elements { get; set; } = new MList<ToolbarElementEmbedded>();

        [UniqueIndex]
        public Guid Guid { get; set; } = Guid.NewGuid();

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Toolbar",
                new XAttribute("Guid", Guid),
                new XAttribute("Name", Name),
                new XAttribute("Location", Location),
                Owner == null ? null : new XAttribute("Owner", Owner.Key()),
                Priority == null ? null : new XAttribute("Priority", Priority.Value.ToString()),
                new XElement("Elements", Elements.Select(p => p.ToXml(ctx))));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Name = element.Attribute("Name").Value;
            Location = element.Attribute("Location").Value.ToEnum<ToolbarLocation>();
            Owner = element.Attribute("Owner")?.Let(a => Lite.Parse<Entity>(a.Value));
            Priority = element.Attribute("Priority")?.Let(a => int.Parse(a.Value));
            Elements.Synchronize(element.Element("Elements").Elements().ToList(), (pp, x) => pp.FromXml(x, ctx));
        }


        static Expression<Func<ToolbarEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public enum ToolbarLocation
    {
        Top,
        Side,
    }

    [AutoInit]
    public static class ToolbarOperation
    {
        public static readonly ExecuteSymbol<ToolbarEntity> Save;
        public static readonly DeleteSymbol<ToolbarEntity> Delete;
    }

    [Serializable]
    public class ToolbarElementEmbedded : EmbeddedEntity
    {
        public ToolbarElementType Type { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 1, Max = 100)]
        public string Label { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string IconName { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string IconColor { get; set; }

        [ImplementedBy(typeof(ToolbarMenuEntity), typeof(UserQueryEntity), typeof(UserChartEntity), typeof(QueryEntity), typeof(DashboardEntity), typeof(PermissionSymbol))]
        public Lite<Entity> Content { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 1, Max = int.MaxValue), URLValidator(absolute: true, aspNetSiteRelative: true)]
        public string Url { get; set; }

        public bool OpenInPopup { get; set; }


        [Unit("s"), NumberIsValidator(Entities.ComparisonType.GreaterThanOrEqualTo, 10)]
        public int? AutoRefreshPeriod { get; set; }

        internal XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("ToolbarElement",
                new XAttribute("Type", Type),
                string.IsNullOrEmpty(Label) ? null : new XAttribute("Label", Label),
                string.IsNullOrEmpty(IconName) ? null : new XAttribute("IconName", IconName),
                string.IsNullOrEmpty(IconColor) ? null :  new XAttribute("IconColor", IconColor),
                OpenInPopup ? new XAttribute("OpenInPopup", OpenInPopup) : null,
                AutoRefreshPeriod == null ? null : new XAttribute("AutoRefreshPeriod", AutoRefreshPeriod),
                this.Content == null ? null : new XAttribute("Content",
                this.Content is Lite<QueryEntity> ?  ctx.QueryToName((Lite<QueryEntity>)this.Content) :
                this.Content is Lite<PermissionSymbol> ?  ctx.PermissionToName((Lite<PermissionSymbol>)this.Content) :
                (object)ctx.Include((Lite<IUserAssetEntity>)this.Content)),
                string.IsNullOrEmpty(this.Url) ? null : new XAttribute("Url", this.Url));
        }

        internal void FromXml(XElement x, IFromXmlContext ctx)
        {
            Type = x.Attribute("Type").Value.ToEnum<ToolbarElementType>();
            Label = x.Attribute("Label")?.Value;
            IconName = x.Attribute("IconName")?.Value;
            IconColor = x.Attribute("IconColor")?.Value;
            OpenInPopup = x.Attribute("OpenInPopup")?.Value.ToBool() ?? false;
            AutoRefreshPeriod = x.Attribute("AutoRefreshPeriod")?.Value.ToInt() ?? null;

            var content = x.Attribute("Content")?.Value;

            Content = string.IsNullOrEmpty(content) ? null :
                Guid.TryParse(content, out Guid guid) ? (Lite<Entity>)ctx.GetEntity(guid).ToLiteFat() :
                (Lite<Entity>)ctx.TryGetQuery(content)?.ToLite() ??
                (Lite<Entity>)ctx.TryPermission(content)?.ToLite() ??
                throw new InvalidOperationException($"Content '{content}' not found");

            Url = x.Attribute("Url")?.Value;
        }

        static StateValidator<ToolbarElementEmbedded, ToolbarElementType> stateValidator = new StateValidator<ToolbarElementEmbedded, ToolbarElementType>
                (n => n.Type,                   n => n.Content, n=> n.Url, n => n.IconName, n => n.Label)
            {
                { ToolbarElementType.Divider,   false,          false,     false,          false  },
                { ToolbarElementType.Header,    null,           null,       null,           null  },
                { ToolbarElementType.Item,      null,           null,      null,           null },
            };

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if(this.Type == ToolbarElementType.Item || this.Type == ToolbarElementType.Header)
            {
                if (pi.Name == nameof(this.Label))
                {
                    if (string.IsNullOrEmpty(this.Label) && this.Content == null)
                        return ValidationMessage._0IsMandatoryWhen1IsNotSet.NiceToString(pi.NiceName(), ReflectionTools.GetPropertyInfo(() => Content).NiceName());
                }

                if(pi.Name == nameof(this.Url))
                { 
                    if (string.IsNullOrEmpty(this.Url) && this.Content == null && this.Type == ToolbarElementType.Item)
                        return ValidationMessage._0IsMandatoryWhen1IsNotSet.NiceToString(pi.NiceName(), ReflectionTools.GetPropertyInfo(() => Content).NiceName());
                }
            }

            return stateValidator.Validate(this, pi) ?? base.PropertyValidation(pi);
        }
    }

    public enum ToolbarElementType
    {
        Header = 2,
        Divider,
        Item,
    }

    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class ToolbarMenuEntity : Entity, IUserAssetEntity
    {
        [ImplementedBy(typeof(UserEntity), typeof(RoleEntity))]
        public Lite<IEntity> Owner { get; set; }

        [UniqueIndex]
        public Guid Guid { get; set; } = Guid.NewGuid();

        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<ToolbarElementEmbedded> Elements { get; set; } = new MList<ToolbarElementEmbedded>();

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("ToolbarMenu",
                new XAttribute("Guid", Guid),
                new XAttribute("Name", Name),
                Owner == null ? null : new XAttribute("Owner", Owner.Key()),
                new XElement("Elements", Elements.Select(p => p.ToXml(ctx))));
        }


        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Name = element.Attribute("Name").Value;
            Elements.Synchronize(element.Element("Elements").Elements().ToList(), (pp, x) => pp.FromXml(x, ctx));
            Owner = element.Attribute("Owner")?.Let(a => Lite.Parse<Entity>(a.Value));
        }


        static Expression<Func<ToolbarMenuEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class ToolbarMenuOperation
    {
        public static readonly ExecuteSymbol<ToolbarMenuEntity> Save;
        public static readonly DeleteSymbol<ToolbarMenuEntity> Delete;
    }

}
