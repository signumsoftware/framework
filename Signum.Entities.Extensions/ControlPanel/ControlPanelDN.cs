using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities.Extensions.Properties;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Collections.Specialized;

namespace Signum.Entities.ControlPanel
{
    [Serializable, EntityType(EntityType.Main)]
    public class ControlPanelDN : Entity
    {
        public ControlPanelDN()
        {
            RebindEvents();
        }

        Lite<IdentifiableEntity> related;
        [NotNullValidator]
        public Lite<IdentifiableEntity> Related
        {
            get { return related; }
            set { Set(ref related, value, () => Related); }
        }

        int? homePagePriority;
        public int? HomePagePriority
        {
            get { return homePagePriority; }
            set { Set(ref homePagePriority, value, () => HomePagePriority); }
        }

        string displayName;
        [StringLengthValidator(AllowNulls = false, Min = 2)]
        public string DisplayName
        {
            get { return displayName; }
            set { Set(ref displayName, value, () => DisplayName); }
        }

        int numberOfColumns = 1;
        [NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int NumberOfColumns
        {
            get { return numberOfColumns; }
            set { Set(ref numberOfColumns, value, () => NumberOfColumns); }
        }

        [ValidateChildProperty, NotifyCollectionChanged, NotifyChildProperty, NotNullable]
        MList<PanelPart> parts = new MList<PanelPart>();
        [NoRepeatValidator]
        public MList<PanelPart> Parts
        {
            get { return parts; }
            set { Set(ref parts, value, () => Parts); }
        }

        static Expression<Func<ControlPanelDN, IPartDN, bool>> ContainsContentExpression =
            (cp, content) => cp.Parts.Any(p => p.Content.Is(content));
        public bool ContainsContent(IPartDN content)
        {
            return ContainsContentExpression.Evaluate(this, content);
        }

        protected override string ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
        {
            if (sender is PanelPart)
            {
                PanelPart part = (PanelPart)sender;

                if (pi.Is(() => part.Column))
                {
                    if (part.Column >= NumberOfColumns)
                        return Resources.ControlPanelDN_Part0IsInColumn1ButPanelHasOnly2Columns.Formato(part.Title, part.Column, NumberOfColumns);
                }

                if (pi.Is(() => part.Row))
                {
                    if (part.Row > 0 && !parts.Any(p => p.Row == part.Row - 1 && p.Column == p.Column))
                        return "There's nothing in Column {0} Row {1}. Move this part up to Row {2}".Formato(part.Column, part.Row, part.Row - 1);
                }

                if (pi.Is(() => part.Row) || pi.Is(() => part.Column))
                {
                    if (parts.Any(p => p != part && p.Row == part.Row && p.Column == part.Column))
                        return Resources.ControlPanelDN_Part0IsInColumn1WhichAlreadyHasOtherParts.Formato(part.Title, part.Column, part.Row);
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
        //            return Resources.ControlPanelDN_Rows0DontHaveAnyParts.Formato(numbers.Where(n => !rows.Contains(n)).ToString(n => n.ToString(), ", "));
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
            if (!invalidating && sender is PanelPart && (e.PropertyName == "Row" || e.PropertyName == "Column"))
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
                NumberOfColumns = NumberOfColumns,
                Parts = Parts.Select(p => p.Clone()).ToMList(),
                Related = Related,
            };
        }
    }

    public enum ControlPanelOperation
    {
        Save,
        Clone,
        Delete,
    }
}
