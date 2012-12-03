using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities.Extensions.Properties;
using System.Linq.Expressions;

namespace Signum.Entities.ControlPanel
{
    [Serializable]
    public class ControlPanelDN : Entity
    {
        Lite<IdentifiableEntity> related;
        [NotNullValidator]
        public Lite<IdentifiableEntity> Related
        {
            get { return related; }
            set { Set(ref related, value, () => Related); }
        }

        bool homePage;
        public bool HomePage
        {
            get { return homePage; }
            set { Set(ref homePage, value, () => HomePage); }
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

        [ValidateChildProperty, NotNullable]
        MList<PanelPart> parts = new MList<PanelPart>();
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

                int index = Parts.IndexOf(part);

                if (pi.Is(() => part.Column))
                {
                    if (part.Column > NumberOfColumns)
                        return Resources.ControlPanelDN_Part0IsInColumn1ButPanelHasOnly2Columns.Formato(part.Title, part.Column, NumberOfColumns);

                    if (parts.Any(p => p != part && p.Row == part.Row && p.Column == part.Column))
                        return Resources.ControlPanelDN_Part0IsInColumn1WhichAlreadyHasOtherParts.Formato(part.Title, part.Column, part.Row);
                }
            }

            return base.ChildPropertyValidation(sender, pi);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => Parts) && Parts.Any())
            {
                var rows = Parts.Select(p => p.Row).Distinct().ToList();
                int maxRow = rows.Max();
                var numbers = 1.To(maxRow + 1);
                if (maxRow != rows.Count)
                    return Resources.ControlPanelDN_Rows0DontHaveAnyParts.Formato(numbers.Where(n => !rows.Contains(n)).ToString(n => n.ToString(), ", "));
            }

            return base.PropertyValidation(pi);
        }

        static readonly Expression<Func<ControlPanelDN, string>> ToStringExpression = e => e.displayName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public ControlPanelDN Clone()
        {

            return new ControlPanelDN {
            
            DisplayName="Clon {0}".Formato(this.DisplayName),
             HomePage=this.HomePage,
              NumberOfColumns=this.NumberOfColumns,
               Parts =this.Parts.Select(p=>p.Clone()).ToMList(),
                Related=this.Related,
            
            };
        
        }
    }
}
