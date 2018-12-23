using System;
using System.Linq;
using Signum.Utilities;
using System.Reflection;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Collections.Specialized;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Entities.Chart;
using Signum.Entities.UserAssets;
using System.Xml.Linq;
using Signum.Entities.Authorization;

namespace Signum.Entities.Dashboard
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class DashboardEntity : Entity, IUserAssetEntity
    {
        public DashboardEntity()
        {
            RebindEvents();
        }

        Lite<TypeEntity> entityType;
        public Lite<TypeEntity> EntityType
        {
            get { return entityType; }
            set
            {
                if (Set(ref entityType, value) && value == null)
                    EmbeddedInEntity = null;
            }
        }

        public DashboardEmbedededInEntity? EmbeddedInEntity { get; set; }

        public Lite<Entity> Owner { get; set; }

        public int? DashboardPriority { get; set; }

        [Unit("s"), NumberIsValidator(Entities.ComparisonType.GreaterThanOrEqualTo, 10)]
        public int? AutoRefreshPeriod { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 200)]
        public string DisplayName { get; set; }

        public bool CombineSimilarRows { get; set; } = true;

        [NotifyCollectionChanged, NotifyChildProperty, NotNullValidator]
        [NoRepeatValidator]
        public MList<PanelPartEmbedded> Parts { get; set; } = new MList<PanelPartEmbedded>();

        [UniqueIndex]
        public Guid Guid { get; set; } = Guid.NewGuid();

        public bool ForNavbar { get; set; }

        public string Key { get; set; }

        static Expression<Func<DashboardEntity, IPartEntity, bool>> ContainsContentExpression =
            (cp, content) => cp.Parts.Any(p => p.Content.Is(content));
        [ExpressionField]
        public bool ContainsContent(IPartEntity content)
        {
            return ContainsContentExpression.Evaluate(this, content);
        }

        protected override string ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
        {
            if (sender is PanelPartEmbedded part)
            {
                if (pi.Name == nameof(part.StartColumn))
                {
                    if (part.StartColumn + part.Columns > 12)
                        return DashboardMessage.Part0IsTooLarge.NiceToString(part);

                    var other = Parts.TakeWhile(p => p != part)
                        .FirstOrDefault(a => a.Row == part.Row && a.ColumnInterval().Overlaps(part.ColumnInterval()));

                    if (other != null)
                        return DashboardMessage.Part0OverlapsWith1.NiceToString(part, other);
                }

                if (entityType != null && pi.Name == nameof(part.Content) && part.Content != null)
                {
                    var idents = GraphExplorer.FromRoot((Entity)part.Content).OfType<Entity>();

                    string errorsUserQuery = idents.OfType<IHasEntitytype>()
                        .Where(uc => uc.EntityType != null && !uc.EntityType.Is(EntityType))
                        .ToString(uc => DashboardMessage._0Is1InstedOf2In3.NiceToString(NicePropertyName(() => EntityType), uc.EntityType, entityType, uc),
                        "\r\n");

                    return errorsUserQuery.DefaultText(null);
                }
            }

            return base.ChildPropertyValidation(sender, pi);
        }

        protected override void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (sender == Parts)
                foreach (var pp in Parts)
                    pp.NotifyRowColumn();

            base.ChildCollectionChanged(sender, args);
        }


        [Ignore]
        bool invalidating = false;
        protected override void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!invalidating && sender is PanelPartEmbedded && (e.PropertyName == "Row" || e.PropertyName == "Column"))
            {
                invalidating = true;
                foreach (var pp in Parts)
                    pp.NotifyRowColumn();
                invalidating = false;
            }

            base.ChildPropertyChanged(sender, e);
        }

        static readonly Expression<Func<DashboardEntity, string>> ToStringExpression = e => e.DisplayName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public DashboardEntity Clone()
        {
            return new DashboardEntity
            {
                DisplayName = "Clone {0}".FormatWith(this.DisplayName),
                DashboardPriority = DashboardPriority,
                Parts = Parts.Select(p => p.Clone()).ToMList(),
                Owner = Owner,
                EntityType = this.EntityType,
                EmbeddedInEntity = this.EmbeddedInEntity,
                AutoRefreshPeriod = this.AutoRefreshPeriod,
                ForNavbar = this.ForNavbar,
                Key = this.Key
            };
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Dashboard",
                new XAttribute("Guid", Guid),
                new XAttribute("DisplayName", DisplayName),
                EntityType == null ? null : new XAttribute("EntityType", ctx.TypeToName(EntityType)),
                Owner == null ? null : new XAttribute("Owner", Owner.Key()),
                DashboardPriority == null ? null : new XAttribute("DashboardPriority", DashboardPriority.Value.ToString()),
                EmbeddedInEntity == null ? null : new XAttribute("EmbeddedInEntity", EmbeddedInEntity.Value.ToString()),
                new XAttribute("CombineSimilarRows", CombineSimilarRows),
                new XElement("Parts", Parts.Select(p => p.ToXml(ctx))));
        }


        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            DisplayName = element.Attribute("DisplayName").Value;
            EntityType = element.Attribute("EntityType")?.Let(a => ctx.GetType(a.Value));
            Owner = element.Attribute("Owner")?.Let(a => Lite.Parse<Entity>(a.Value));
            DashboardPriority = element.Attribute("DashboardPriority")?.Let(a => int.Parse(a.Value));
            EmbeddedInEntity = element.Attribute("EmbeddedInEntity")?.Let(a => a.Value.ToEnum<DashboardEmbedededInEntity>());
            CombineSimilarRows = element.Attribute("CombineSimilarRows")?.Let(a => bool.Parse(a.Value)) ?? false;
            Parts.Synchronize(element.Element("Parts").Elements().ToList(), (pp, x) => pp.FromXml(x, ctx));
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(EmbeddedInEntity))
            {
                if (EmbeddedInEntity == null && EntityType != null)
                    return ValidationMessage._0IsNecessary.NiceToString(pi.NiceName());

                if (EmbeddedInEntity != null && EntityType == null)
                    return ValidationMessage._0IsNotAllowed.NiceToString(pi.NiceName());
            }

            return base.PropertyValidation(pi);
        }
    }

    [AutoInit]
    public static class DashboardPermission
    {
        public static PermissionSymbol ViewDashboard;
    }

    [AutoInit]
    public static class DashboardOperation
    {
        public static ExecuteSymbol<DashboardEntity> Save;
        public static ConstructSymbol<DashboardEntity>.From<DashboardEntity> Clone;
        public static DeleteSymbol<DashboardEntity> Delete;
    }

    public enum DashboardMessage
    {
        CreateNewPart,

        [Description("Title must be specified for {0}")]
        DashboardDN_TitleMustBeSpecifiedFor0,

        Preview,
        [Description("{0} is {1} (instead of {2}) in {3}")]
        _0Is1InstedOf2In3,

        [Description("Part {0} is too large")]
        Part0IsTooLarge,

        [Description("Part {0} overlaps with {1}")]
        Part0OverlapsWith1
    }

    public enum DashboardEmbedededInEntity
    {
        None,
        Top,
        Bottom
    }
}
