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
using Signum.Entities.UserQueries;
using System.Xml.Linq;

namespace Signum.Entities.ControlPanel
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class ControlPanelDN : Entity, IUserAssetEntity
    {
        public ControlPanelDN()
        {
            RebindEvents();
        }

        Lite<TypeDN> entityType;
        public Lite<TypeDN> EntityType
        {
            get { return entityType; }
            set { Set(ref entityType, value, () => EntityType); }
        }

        Lite<IdentifiableEntity> owner;
        public Lite<IdentifiableEntity> Owner
        {
            get { return owner; }
            set { Set(ref owner, value, () => Owner); }
        }

        int? homePagePriority;
        public int? HomePagePriority
        {
            get { return homePagePriority; }
            set { Set(ref homePagePriority, value, () => HomePagePriority); }
        }

        int? autoRefreshPeriod;
        [Unit("s"), NumberIsValidator(Entities.ComparisonType.GreaterThan, 1)]
        public int? AutoRefreshPeriod
        {
            get { return autoRefreshPeriod; }
            set { Set(ref autoRefreshPeriod, value, () => AutoRefreshPeriod); }
        }

        string displayName;
        [StringLengthValidator(AllowNulls = false, Min = 2)]
        public string DisplayName
        {
            get { return displayName; }
            set { Set(ref displayName, value, () => DisplayName); }
        }

        [ValidateChildProperty, NotifyCollectionChanged, NotifyChildProperty, NotNullable]
        MList<PanelPartDN> parts = new MList<PanelPartDN>();
        [NoRepeatValidator]
        public MList<PanelPartDN> Parts
        {
            get { return parts; }
            set { Set(ref parts, value, () => Parts); }
        }

        [UniqueIndex]
        Guid guid = Guid.NewGuid();
        public Guid Guid
        {
            get { return guid; }
            set { Set(ref guid, value, () => Guid); }
        }

        static Expression<Func<ControlPanelDN, IPartDN, bool>> ContainsContentExpression =
            (cp, content) => cp.Parts.Any(p => p.Content.Is(content));
        public bool ContainsContent(IPartDN content)
        {
            return ContainsContentExpression.Evaluate(this, content);
        }

        protected override string ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
        {
            if (sender is PanelPartDN)
            {
                PanelPartDN part = (PanelPartDN)sender;

                if (pi.Is(() => part.StartColumn))
                {
                    if (part.StartColumn + part.Columns > 12)
                        return ControlPanelMessage.Part0IsTooLarge.NiceToString(part);

                    var other = parts.TakeWhile(p => p != part)
                        .FirstOrDefault(a => a.Row == part.Row && a.ColumnInterval().Overlap(part.ColumnInterval()));

                    if (other != null)
                        return ControlPanelMessage.Part0OverlapsWith1.NiceToString(part, other);
                }

                if (entityType != null && pi.Is(() => part.Content) && part.Content != null)
                {
                    var idents = GraphExplorer.FromRoot((IdentifiableEntity)part.Content).OfType<IdentifiableEntity>();

                    string errorsUserQuery = idents.OfType<IHasEntitytype>()
                        .Where(uc => uc.EntityType != null && !uc.EntityType.Is(EntityType))
                        .ToString(uc => ControlPanelMessage._0Is1InstedOf2In3.NiceToString(NicePropertyName(() => EntityType), uc.EntityType, entityType, uc),
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
        //            return ControlPanelMessage.ControlPanelDN_Rows0DontHaveAnyParts.NiceToString().Formato(numbers.Where(n => !rows.Contains(n)).ToString(n => n.ToString(), ", "));
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
            if (!invalidating && sender is PanelPartDN && (e.PropertyName == "Row" || e.PropertyName == "Column"))
            {
                invalidating = true;
                foreach (var pp in Parts)
                    pp.NotifyRowColumn();
                invalidating = false;
            }

            base.ChildPropertyChanged(sender, e);
        }

        static readonly Expression<Func<ControlPanelDN, string>> ToStringExpression = e => e.displayName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public ControlPanelDN Clone()
        {
            return new ControlPanelDN
            {
                DisplayName = "Clone {0}".Formato(this.DisplayName),
                HomePagePriority = HomePagePriority,
                Parts = Parts.Select(p => p.Clone()).ToMList(),
                Owner = Owner,
            };
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("ControlPanel",
                new XAttribute("Guid", Guid),
                new XAttribute("DisplayName", DisplayName),
                EntityType == null ? null : new XAttribute("EntityType", ctx.TypeToName(EntityType)),
                Owner == null ? null : new XAttribute("Owner", Owner.Key()),
                HomePagePriority == null ? null : new XAttribute("HomePagePriority", HomePagePriority.Value.ToString()),
                new XElement("Parts", Parts.Select(p => p.ToXml(ctx)))); 
        }


        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            DisplayName = element.Attribute("DisplayName").Value;
            EntityType = element.Attribute("EntityType").TryCC(a => ctx.GetType(a.Value));
            Owner = element.Attribute("Owner").TryCC(a => Lite.Parse<IdentifiableEntity>(a.Value));
            HomePagePriority = element.Attribute("HomePagePriority").TryCS(a => int.Parse(a.Value));
            Parts.Syncronize(element.Element("Parts").Elements().ToList(), (pp, x) => pp.FromXml(x, ctx));

        }
    }

    public enum ControlPanelPermission
    {
        ViewControlPanel,
    }

    public enum ControlPanelOperation
    {
        Create,
        Save,
        Clone,
        Delete,
    }

    public enum ControlPanelMessage
    {
        CreateNewPart,


        [Description("Title must be specified for {0}")]
        ControlPanelDN_TitleMustBeSpecifiedFor0,
        [Description("Counter list")]
        CountSearchControlPartDN,
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
}
