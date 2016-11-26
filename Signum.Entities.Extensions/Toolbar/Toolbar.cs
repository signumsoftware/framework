using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.Dashboard;
using Signum.Entities.Files;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Reflection;

namespace Signum.Entities.Toolbar
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class ToolbarEntity : Entity, IUserAssetEntity
    {
        [ImplementedBy(typeof(UserEntity), typeof(RoleEntity))]
        public Lite<IEntity> Owner { get; set; }

        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        public int? Priority { get; set; }

        public MList<ToolbarElementEntity> Elements { get; set; } = new MList<ToolbarElementEntity>();

        [UniqueIndex]
        public Guid Guid { get; set; } = Guid.NewGuid();

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Toolbar",
                new XAttribute("Guid", Guid),
                new XAttribute("Name", Name),
                Owner == null ? null : new XAttribute("Owner", Owner.Key()),
                Priority == null ? null : new XAttribute("Priority", Priority.Value.ToString()),
                new XElement("Elements", Elements.Select(p => p.ToXml(ctx))));
        }
        
        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Name = element.Attribute("Name").Value;
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

    [AutoInit]
    public static class ToolbarOperation
    {
        public static readonly ExecuteSymbol<ToolbarEntity> Save;
        public static readonly DeleteSymbol<ToolbarEntity> Delete;
    }

    [Serializable]
    public class ToolbarElementEntity : EmbeddedEntity
    {
        public ToolbarElementType Type { get; set; }

        [SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string Label { get; set; }

        [SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string IconName { get; set; }

        [NotNullable]
        [NotNullValidator]
        [ImplementedBy(typeof(ToolbarMenuEntity), typeof(UserQueryEntity), typeof(UserChartEntity), typeof(QueryEntity), typeof(DashboardEntity))]
        public Lite<Entity> Content { get; set; }


        internal XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("ToolbarElement",
                Label == null ? null : new XAttribute("Label", Label),
                new XAttribute("Type", Type),
                new XAttribute("IconName", IconName),
                new XElement("Content", this.Content is Lite<QueryEntity> ?
                ctx.QueryToName((Lite<QueryEntity>)this.Content) :
                (object)ctx.Include((Lite<IUserAssetEntity>)this.Content)));
        }



        internal void FromXml(XElement x, IFromXmlContext ctx)
        {
            Label = x.Attribute("Label")?.Value;
            Type = x.Attribute("Type").Value.ToEnum<ToolbarElementType>();
            IconName = x.Attribute("IconName")?.Value;

            var content = x.Attribute("Content").Value;

            Guid guid;
            Content = Guid.TryParse(content, out guid) ?
                (Lite<Entity>)ctx.GetEntity(guid).ToLite() :
                (Lite<Entity>)ctx.GetQuery(content).ToLite();
        }

        static StateValidator<ToolbarElementEntity, ToolbarElementType> stateValidator = new StateValidator<ToolbarElementEntity, ToolbarElementType>
                (n => n.Type,           n => n.Content, n => n.IconName, n => n.Label)
            {
                { ToolbarElementType.Divider, false,      false,        false  },
                { ToolbarElementType.Header, false,      null,        true  },
                { ToolbarElementType.Link, true,      null,        null },
                { ToolbarElementType.Menu, true,      null,        null },
            };

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(this.Content) && this.Content != null)
            {
                if (this.Content.EntityType == typeof(ToolbarMenuEntity) && (this.Type == ToolbarElementType.Link))
                    return ValidationMessage._0ShouldNotBeOfType1.NiceToString(pi.NiceName(), typeof(ToolbarMenuEntity));


                if (this.Content.EntityType != typeof(ToolbarMenuEntity) && (this.Type == ToolbarElementType.Menu))
                    return ValidationMessage._0ShouldBeOfType1.NiceToString(pi.NiceName(), typeof(ToolbarMenuEntity));
            }

            return stateValidator.Validate(this, pi) ?? base.PropertyValidation(pi);
        }
    }

    public enum ToolbarElementType
    {
        Link,
        Menu,
        Header,
        Divider
    }

    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class ToolbarMenuEntity : Entity, IUserAssetEntity
    {
        [UniqueIndex]
        public Guid Guid { get; set; } = Guid.NewGuid();

        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        public MList<ToolbarElementEntity> Elements { get; set; } = new MList<ToolbarElementEntity>();

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("ToolbarMenu",
                new XAttribute("Guid", Guid),
                new XAttribute("Name", Name),
                new XElement("Elements", Elements.Select(p => p.ToXml(ctx))));
        }


        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Name = element.Attribute("Name").Value;
            Elements.Synchronize(element.Element("Elements").Elements().ToList(), (pp, x) => pp.FromXml(x, ctx));
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
