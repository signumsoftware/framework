using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities;
using System.Reflection;
using System.Collections;
using Signum.Engine;
using System.Web.Mvc;
using Signum.Utilities.DataStructures;

namespace Signum.Web
{
    public class SessionModification
    {
        public static Modification Create(Controller controller, Modifiable parentEntity, string sfStaticType, int? sfId, string prefix, SortedList<string, object> formValues, MinMax<int> interval)
        {
            //Look for the parentEntity's right property using prefix and: 
            //  1. create it if it doesn't exist (new list element or new subentity)
            //  2. retrieve it if it comes with an id
            //  3. modify it if it doesn't have id and it's also new in the parentEntity
            Type type = Navigator.ResolveType(sfStaticType);
            Modifiable entity = GetPropertyValue(parentEntity, prefix);
            if (sfId.HasValue)
            {
                if (((IIdentifiable)entity).IdOrNull != sfId)
                    entity = Database.Retrieve(type, sfId.Value);
            }
            else
            {
                if (entity == null)
                    entity = (ModifiableEntity)Constructor.Construct(type, controller);
            }

            return Modification.Create(type, formValues, interval, prefix);
        }

        protected internal static Modifiable GetPropertyValue(Modifiable entity, string prefix)
        {
            if (!prefix.HasText())
                return entity;

            string[] properties = prefix.Split(new string[] { TypeContext.Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (properties == null || properties.Length == 0)
                throw new ArgumentException("Invalid property prefix");

            List<PropertyInfo> pis = new List<PropertyInfo>();
            object currentEntity = entity;
            foreach (string property in properties)
            {
                int index;
                if (int.TryParse(property, out index))
                {
                    IList ilist = (IList)currentEntity;
                    if (ilist.Count <= index)
                        return null;
                    currentEntity = ilist[index];
                }
                else
                {
                    PropertyInfo pi = currentEntity.GetType().GetProperty(property);
                    pis.Add(pi);
                    currentEntity = pi.GetValue(currentEntity, null);
                }

                if (currentEntity == null)
                    return null;
            }

            return (Modifiable)currentEntity;
        }

        protected static void SetPropertyValue(Modifiable entity, string prefix, Modifiable value)
        {
            if (!prefix.HasText())
                throw new ArgumentException("Invalid property prefix");

            string[] properties = prefix.Split(new string[] { TypeContext.Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (properties == null || properties.Length == 0)
                throw new ArgumentException("Invalid property prefix");

            List<PropertyInfo> pis = new List<PropertyInfo>();
            object currentEntity = entity;
            string property;
            for (int i = 0; i < properties.Length - 1; i++)
            {
                property = properties[i];
                int index;
                if (int.TryParse(property, out index))
                {
                    IList ilist = (IList)currentEntity;
                    if (ilist.Count <= index)
                        throw new ApplicationException("Invalid property prefix");
                    currentEntity = ilist[index];
                }
                else
                {
                    PropertyInfo pi = currentEntity.GetType().GetProperty(property);
                    pis.Add(pi);
                    currentEntity = pi.GetValue(currentEntity, null);
                }
            }

            int ind;
            property = properties[properties.Length - 1];
            if (int.TryParse(property, out ind))
            {
                IList ilist = (IList)currentEntity;
                if (ilist.Count <= ind)
                    ilist.Add(value);
                else
                {
                    ilist[ind] = value;
                }
            }
            else
                currentEntity.GetType().GetProperty(property).SetValue(currentEntity, value, null);
        }
    }
}
