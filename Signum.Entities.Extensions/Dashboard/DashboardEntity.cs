using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Reflection;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Collections.Specialized;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Entities.Chart;
using Signum.Utilities.Reflection;
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

        DashboardEmbedededInEntity? embeddedInEntity;
        public DashboardEmbedededInEntity? EmbeddedInEntity
        {
            get { return embeddedInEntity; }
            set { Set(ref embeddedInEntity, value); }
        }

        Lite<Entity> owner;
        public Lite<Entity> Owner
        {
            get { return owner; }
            set { Set(ref owner, value); }
        }

        int? dashboardPriority;
        public int? DashboardPriority
        {
            get { return dashboardPriority; }
            set { Set(ref dashboardPriority, value); }
        }

        int? autoRefreshPeriod;
        [Unit("s"), NumberIsValidator(Entities.ComparisonType.GreaterThan, 1)]
        public int? AutoRefreshPeriod
        {
            get { return autoRefreshPeriod; }
            set { Set(ref autoRefreshPeriod, value); }
        }

        string displayName;
        [StringLengthValidator(AllowNulls = false, Min = 2)]
        public string DisplayName
        {
            get { return displayName; }
            set { Set(ref displayName, value); }
        }

        [ValidateChildProperty, NotifyCollectionChanged, NotifyChildProperty, NotNullable]
        MList<PanelPartEntity> parts = new MList<PanelPartEntity>();
        [NoRepeatValidator]
        public MList<PanelPartEntity> Parts
        {
            get { return parts; }
            set { Set(ref parts, value); }
        }

        [UniqueIndex]
        Guid guid = Guid.NewGuid();
        public Guid Guid
        {
            get { return guid; }
            set { Set(ref guid, value); }
        }

        static Expression<Func<DashboardEntity, IPartEntity, bool>> ContainsContentExpression =
            (cp, content) => cp.Parts.Any(p => p.Content.Is(content));
        public bool ContainsContent(IPartEntity content)
        {
            return ContainsContentExpression.Evaluate(this, content);
        }

        protected override string ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
        {
            if (sender is PanelPartEntity)
            {
                PanelPartEntity part = (PanelPartEntity)sender;

                if (pi.Is(() => part.StartColumn))
                {
                    if (part.StartColumn + part.Columns > 12)
                        return DashboardMessage.Part0IsTooLarge.NiceToString(part);

                    var other = parts.TakeWhile(p => p != part)
                        .FirstOrDefault(a => a.Row == part.Row && a.ColumnInterval().Overlap(part.ColumnInterval()));

                    if (other != null)
                        return DashboardMessage.Part0OverlapsWith1.NiceToString(part, other);
                }

                if (entityType != null && pi.Is(() => part.Content) && part.Content != null)
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

        //protected override string PropertyValidation(PropertyInfo pi)
        //{
        //    if (pi.Is(() => Parts) && Parts.Any())
        //    {
        //        var rows = Parts.Select(p => p.Row).Distinct().ToList();
        //        int maxRow = rows.Max();
        //        var numbers = 0.To(maxRow);
        //        if (maxRow != rows.Count)
        //            return DashboardMessage.DashboardDN_Rows0DontHaveAnyParts.NiceToString().FormatWith(numbers.Where(n => !rows.Contains(n)).ToString(n => n.ToString(), ", "));
        //    }

        //    return base.PropertyValidation(pi);
        //}

        protected override void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if(sender == Parts)
                foreach (var pp in Parts)
                    pp.NotifyRowColumn();

            base.ChildCollectionChanged(sender, args);
        }


        [Ignore]
        bool invalidating = false;
        protected override void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!invalidating && sender is PanelPartEntity && (e.PropertyName == "Row" || e.PropertyName == "Column"))
            {
                invalidating = true;
                foreach (var pp in Parts)
                    pp.NotifyRowColumn();
                invalidating = false;
            }

            base.ChildPropertyChanged(sender, e);
        }

        static readonly Expression<Func<DashboardEntity, string>> ToStringExpression = e => e.displayName;
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
                new XElement("Parts", Parts.Select(p => p.ToXml(ctx)))); 
        }


        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            DisplayName = element.Attribute("DisplayName").Value;
            EntityType = element.Attribute("EntityType").Try(a => ctx.GetType(a.Value));
            Owner = element.Attribute("Owner").Try(a => Lite.Parse<Entity>(a.Value));
            DashboardPriority = element.Attribute("DashboardPriority").Try(a => int.Parse(a.Value));
            EmbeddedInEntity = element.Attribute("EmbeddedInEntity").Try(a => a.Value.ToEnum<DashboardEmbedededInEntity>());
            Parts.Syncronize(element.Element("Parts").Elements().ToList(), (pp, x) => pp.FromXml(x, ctx));
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => EmbeddedInEntity))
            {
                if (EmbeddedInEntity == null && EntityType != null)
                    return ValidationMessage._0IsNecessary.NiceToString(pi.NiceName());

                if (EmbeddedInEntity != null && EntityType == null)
                    return ValidationMessage._0IsNotAllowed.NiceToString(pi.NiceName());
            }

            return base.PropertyValidation(pi);
        }
    }

    public static class DashboardPermission
    {
        public static readonly PermissionSymbol ViewDashboard = new PermissionSymbol();
    }

    public static class DashboardOperation
    {
        public static readonly ConstructSymbol<DashboardEntity>.Simple Create = OperationSymbol.Construct<DashboardEntity>.Simple();
        public static readonly ExecuteSymbol<DashboardEntity> Save = OperationSymbol.Execute<DashboardEntity>();
        public static readonly ConstructSymbol<DashboardEntity>.From<DashboardEntity> Clone = OperationSymbol.Construct<DashboardEntity>.From<DashboardEntity>();
        public static readonly DeleteSymbol<DashboardEntity> Delete = OperationSymbol.Delete<DashboardEntity>();
    }

    public enum DashboardMessage
    {
        CreateNewPart,


        [Description("Title must be specified for {0}")]
        DashboardDN_TitleMustBeSpecifiedFor0,
        [Description("Counter list")]
        CountSearchControlPartEntity,
        [Description("Counter")]
        CountUserQueryElement,
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
