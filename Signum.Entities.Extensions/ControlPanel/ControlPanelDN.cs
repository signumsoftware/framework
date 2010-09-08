using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities.Extensions.Properties;

namespace Signum.Entities.ControlPanel
{
    [Serializable]
    public class ControlPanelDN : Entity
    {
        Lite<IIdentifiable> related;
        [NotNullValidator]
        public Lite<IIdentifiable> Related
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
        [StringLengthValidator(AllowNulls=false, Min=2)]
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

        [ValidateChildProperty]
        MList<PanelPart> parts = new MList<PanelPart>();
        public MList<PanelPart> Parts
        {
            get { return parts.OrderBy(p => p.Row).ThenBy(p => p.Column).ToMList(); }
            set { Set(ref parts, value, () => Parts); }
        }

        protected override string ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi, object propertyValue)
        {
            if(sender is PanelPart)
            {
                PanelPart part =(PanelPart)sender;

                int index = Parts.IndexOf(part);

                if (pi.Is(() => part.Column))
                {
                    int colNumber = int.Parse(propertyValue.ToString());

                    if (colNumber > NumberOfColumns)
                        return Resources.ControlPanelDN_Part0IsInColumn1ButPanelHasOnly2Columns.Formato(index + 1, part.Column, NumberOfColumns);

                    if (parts.Any(p => p != part && p.Row == part.Row && (p.Fill || p.Column == colNumber)))
                        return Resources.ControlPanelDN_Part0IsInColumn1WhichAlreadyHasOtherParts.Formato(index + 1, part.Column, part.Row);
                }
            }

            return base.ChildPropertyValidation(sender, pi, propertyValue);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => Parts))
            {
                var rows = Parts.Select(p => p.Row).Distinct().ToList();
                int maxRow = rows.Max();
                var numbers = 1.To(maxRow+1);
                if (maxRow != rows.Count)
                    return Resources.ControlPanelDN_Rows0DontHaveAnyParts.Formato(numbers.Where(n => !rows.Contains(n)).ToString(n => n.ToString(), ", "));
            }

            return base.PropertyValidation(pi);
        }
    }
}
