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
        public UserIUModification(Type staticType, SortedList<string, object> formValues, MinMax<int> interval, string controlID)
            : base(staticType, formValues, interval, controlID)
        {

        }

        protected override int GeneratePropertyModification(SortedList<string, object> formValues, MinMax<int> interval, string subControlID, string commonSubControlID, string propertyName, int index, Dictionary<string, PropertyPack> propertyValidators)
        {
            if (propertyName == "OldPassword")
            {
                PropertyPack ppCP = propertyValidators.GetOrThrow("PasswordHash", Resources.Property0NotExistsInType1.Formato("PasswordHash", RuntimeType));
                ValueModification pCP = new ValueModification(typeof(string), subControlID);

                string formCP = (string)formValues[subControlID];
                if (!formCP.HasText())
                {
                    pCP.BindingError = Resources.PasswordMustHaveAValue;
                    Properties.Add(propertyName, new PropertyPackModification { Modification = pCP, PropertyPack = ppCP });
                    return index;
                }
                
                string passwordHash = Signum.Services.Security.EncodePassword(formCP);

                if (passwordHash != Database.Retrieve<UserDN>(EntityId.Value).PasswordHash)
                {
                    pCP.BindingError = Resources.PasswordDoesNotMatchCurrent;
                    Properties.Add(propertyName, new PropertyPackModification { Modification = pCP, PropertyPack = ppCP });
                    return index;
                }

                pCP.Value = passwordHash;

                Properties.Add(propertyName, new PropertyPackModification{Modification = pCP, PropertyPack = ppCP});
                return index;
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
                    return index;
                }

                string passwordHash = Signum.Services.Security.EncodePassword(formCP);

                pCP.Value = passwordHash;

                Properties.Add(propertyName, new PropertyPackModification { Modification = pCP, PropertyPack = ppCP });
                return index;
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
                    return index;
                }

                string formCPBis = (string)formValues[subControlID.RemoveRight(3)];
                if (formCP != formCPBis)
                {
                    pCP.BindingError = Resources.TheSpecifiedPasswordsDontMatch;
                    Properties.Add(propertyName, new PropertyPackModification { Modification = pCP, PropertyPack = ppCP });
                    return index;
                }

                return index;
            }

            return base.GeneratePropertyModification(formValues, interval, subControlID, commonSubControlID, propertyName, index, propertyValidators);
        }

        public override void Validate(object entity, Dictionary<string, List<string>> errors)
        {
            if (Properties != null)
            {
                foreach (var ppm in Properties.Values)
                {
                    ppm.Modification.Validate(ppm.PropertyPack.GetValue(entity), errors);

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
