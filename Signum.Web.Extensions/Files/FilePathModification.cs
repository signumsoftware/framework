using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Web;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using Signum.Engine;
using System.Web.Mvc;
using Signum.Web.Extensions.Properties;

namespace Signum.Web.Files
{
    public class FilePathModification : EntityModification
    {
        public FilePathModification(Type staticType, SortedList<string, object> formValues, MinMax<int> interval, string controlID)
            : base(staticType, formValues, interval, controlID)
        {

        }

        protected override int GeneratePropertyModification(SortedList<string, object> formValues, MinMax<int> interval, string subControlID, string commonSubControlID, string propertyName, int index, Dictionary<string, PropertyPack> propertyValidators)
        {
            MinMax<int> subInterval = FindSubInterval(formValues, new MinMax<int>(index, interval.Max), ControlID.Length, TypeContext.Separator + propertyName);

            long? propertyIsLastChange = null;
            if (formValues.ContainsKey(TypeContext.Compose(commonSubControlID, TypeContext.Ticks)))
            {
                string changed = (string)formValues.TryGetC(TypeContext.Compose(commonSubControlID, TypeContext.Ticks));
                if (changed.HasText()) //It'll be null for EmbeddedControls 
                {
                    if (changed == "0")
                        return subInterval.Max - 1; //Don't apply changes, it will affect other properties and it has not been changed in the IU
                    else
                        propertyIsLastChange = long.Parse(changed);
                }
            }

            if (propertyName == FileLineKeys.FileType)
                return subInterval.Max - 1;

            return base.GeneratePropertyModification(formValues, interval, subControlID, commonSubControlID, propertyName, index, propertyValidators);
        }
    }
}
