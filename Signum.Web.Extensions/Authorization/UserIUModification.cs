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
using Signum.Entities.Authorization;
using Signum.Web.Extensions.Properties;

namespace Signum.Web.Authorization
{
    public class UserIUModification : EntityModification
    {
        public UserIUModification(Type staticType, SortedList<string, object> formValues, Interval<int> interval, string controlID)
            : base(staticType, formValues, interval, controlID)
        {

        }

        protected override int GeneratePropertyModification(SortedList<string, object> formValues, Interval<int> interval, string subControlID, string commonSubControlID, string propertyName, int index, Dictionary<string, PropertyPack> propertyValidators)
        {
            Interval<int> subInterval = FindSubInterval(formValues, new Interval<int>(index, interval.Max), ControlID.Length, TypeContext.Separator + propertyName);

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

            if (propertyName == "OldPassword")
            {
                PropertyPack ppCP = propertyValidators.GetOrThrow("PasswordHash", Resources.Property0NotExistsInType1.Formato("PasswordHash", RuntimeType));
                ValueModification pCP = new ValueModification(typeof(string), subControlID);

                string formCP = (string)formValues[subControlID];
                if (!formCP.HasText())
                {
                    pCP.BindingError = Resources.PasswordMustHaveAValue;
                    Properties.Add(propertyName, new PropertyPackModification { Modification = pCP, PropertyPack = ppCP });
                    return subInterval.Max - 1;
                }
                
                string passwordHash = Signum.Services.Security.EncodePassword(formCP);

                if (passwordHash != Database.Retrieve<UserDN>(EntityId.Value).PasswordHash)
                {
                    pCP.BindingError = Resources.PasswordDoesNotMatchCurrent;
                    Properties.Add(propertyName, new PropertyPackModification { Modification = pCP, PropertyPack = ppCP });
                    return subInterval.Max - 1;
                }

                pCP.Value = passwordHash;

                Properties.Add(propertyName, new PropertyPackModification{Modification = pCP, PropertyPack = ppCP});
                return subInterval.Max - 1;
            }

            if (propertyName == "NewPassword")
            {
                PropertyPack ppCP = propertyValidators.GetOrThrow("PasswordHash", Resources.Property0NotExistsInType1.Formato("PasswordHash", RuntimeType));
                ValueModification pCP = new ValueModification(typeof(string), subControlID);

                string formCP = (string)formValues[subControlID];
                if (!formCP.HasText())
                {
                    pCP.BindingError = Resources.PasswordMustHaveAValue;
                    Properties.Add(propertyName, new PropertyPackModification { Modification = pCP, PropertyPack = ppCP });
                    return subInterval.Max - 1;
                }

                string passwordHash = Signum.Services.Security.EncodePassword(formCP);

                pCP.Value = passwordHash;

                Properties.Add(propertyName, new PropertyPackModification { Modification = pCP, PropertyPack = ppCP });
                return subInterval.Max - 1;
            }

            if (propertyName == "NewPasswordBis")
            {
                PropertyPack ppCP = propertyValidators.GetOrThrow("PasswordHash", Resources.Property0NotExistsInType1.Formato("PasswordHash", RuntimeType));
                ValueModification pCP = new ValueModification(typeof(string), subControlID);

                string formCP = (string)formValues[subControlID];
                if (!formCP.HasText())
                {
                    pCP.BindingError = Resources.YouMustRepeatTheNewPassword;
                    Properties.Add(propertyName, new PropertyPackModification { Modification = pCP, PropertyPack = ppCP });
                    return subInterval.Max - 1;
                }

                string formCPBis = (string)formValues[subControlID.RemoveRight(3)];
                if (formCP != formCPBis)
                {
                    pCP.BindingError = Resources.TheSpecifiedPasswordsDontMatch;
                    Properties.Add(propertyName, new PropertyPackModification { Modification = pCP, PropertyPack = ppCP });
                    return subInterval.Max - 1;
                }

                return subInterval.Max - 1;
            }

            return base.GeneratePropertyModification(formValues, interval, subControlID, commonSubControlID, propertyName, index, propertyValidators);
        }

        public override void Validate(Controller controller, object entity, Dictionary<string, List<string>> errors, string prefix)
        {
            if (Properties != null)
            {
                foreach (var ppm in Properties.Values)
                {
                    ppm.Modification.Validate(controller, ppm.PropertyPack.GetValue(entity), errors, prefix);

                    if (ppm.PropertyPack.PropertyInfo.Name == "PasswordHash")
                        continue;

                    string error = ((ModifiableEntity)entity)[ppm.PropertyPack.PropertyInfo.Name];
                    if (error != null)
                        errors.GetOrCreate(ppm.Modification.ControlID).AddRange(error.Lines());
                }
            }

            if (!string.IsNullOrEmpty(BindingError))
                errors.GetOrCreate(ControlID).Add(BindingError);
        }
    }
}
