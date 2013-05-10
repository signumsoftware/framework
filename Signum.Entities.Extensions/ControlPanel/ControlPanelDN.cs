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

namespace Signum.Entities.ControlPanel
{
    [Serializable, EntityKind(EntityKind.Main)]
    public class ControlPanelDN : Entity
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

        Lite<IdentifiableEntity> related;
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
        MList<PanelPartDN> parts = new MList<PanelPartDN>();
        [NoRepeatValidator]
        public MList<PanelPartDN> Parts
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
            if (sender is PanelPartDN)
            {
                PanelPartDN part = (PanelPartDN)sender;

                if (pi.Is(() => part.Column))
                {
                    if (part.Column >= NumberOfColumns)
                        return ControlPanelMessage.ControlPanelDN_Part0IsInColumn1ButPanelHasOnly2Columns.NiceToString().Formato(part.Title, part.Column, NumberOfColumns);
                }

                if (pi.Is(() => part.Row))
                {
                    if (part.Row > 0 && !parts.Any(p => p.Row == part.Row - 1 && p.Column == p.Column))
                        return "There's nothing in Column {0} Row {1}. Move this part up to Row {2}".Formato(part.Column, part.Row, part.Row - 1);
                }

                if (pi.Is(() => part.Row) || pi.Is(() => part.Column))
                {
                    if (parts.Any(p => p != part && p.Row == part.Row && p.Column == part.Column))
                        return ControlPanelMessage.ControlPanelDN_Part0IsInColumn1WhichAlreadyHasOtherParts.NiceToString().Formato(part.Title, part.Column, part.Row);
                }

                if (entityType != null && pi.Is(() => part.Content) && part.Content != null)
                {
                    var idents = GraphExplorer.FromRoot((IdentifiableEntity)part.Content).OfType<IdentifiableEntity>();

                    string errorsUserQuery = idents.OfType<IHasEntitytype>()
                        .Where(uc => uc.EntityType != null && !uc.EntityType.Is(EntityType))
                        .ToString(uc => ControlPanelMessage._0Is1InstedOf2In3.NiceToString().Formato(
                        NicePropertyName(() => EntityType), uc.EntityType, entityType, uc), "\r\n");

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
                NumberOfColumns = NumberOfColumns,
                Parts = Parts.Select(p => p.Clone()).ToMList(),
                Related = Related,
            };
        }
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
        [Description("Create new part")]
        ControlPanel_CreateNewPart,
        [Description("You must save the panel before adding parts")]
        ControlPanel_YouMustSaveThePanelBeforeAddingParts,
        [Description("Part {0} is in column {1} but panel has only {2} columns")]
        ControlPanelDN_Part0IsInColumn1ButPanelHasOnly2Columns,
        [Description("Part {0} is in column {1} of row {2} which already has other parts")]
        ControlPanelDN_Part0IsInColumn1WhichAlreadyHasOtherParts,
        [Description("There are not any parts in rows {0}")]
        ControlPanelDN_Rows0DontHaveAnyParts,
        [Description("Title must be specified for {0}")]
        ControlPanelDN_TitleMustBeSpecifiedFor0,
        [Description("Counter list")]
        CountSearchControlPartDN,
        [Description("Counter")]
        CountUserQueryElement,
        Preview,
        [Description("{0} is {1} (instead of {2}) in {3}")]
        _0Is1InstedOf2In3
    }
}
