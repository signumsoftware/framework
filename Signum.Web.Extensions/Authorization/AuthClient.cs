using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using System.Reflection;

namespace Signum.Web.Authorization
{
    public class AuthClient
    {
        public static void Start(NavigationManager manager, bool types, bool property)
        {
            manager.EntitySettings.Add(typeof(UserDN), new EntitySettings(true)); //{ View = () => new User() });
            manager.EntitySettings.Add(typeof(RoleDN), new EntitySettings(false)); //{ View = () => new Role() });

            if (property)
                Common.CommonTask += new CommonTask(TaskAuthorizeProperties);

            if (types)
            {
                Navigator.NavigationManager.GlobalIsCreable += type => TypeAuthLogic.GetTypeAccess(type) == TypeAccess.Create;
                Navigator.NavigationManager.GlobalIsReadOnly += type => TypeAuthLogic.GetTypeAccess(type) < TypeAccess.Modify;
                Navigator.NavigationManager.GlobalIsViewable += type => TypeAuthLogic.GetTypeAccess(type) >= TypeAccess.Read;
            }
                //TypeAuthLogic.AuthorizedTypes().JoinDictionaryForeach(manager.EntitySettings, Authorize);
        }

        static void TaskAuthorizeProperties(BaseLine bl, Type type, TypeContext context)
        {
            List<PropertyInfo> contextList = context.GetPath();

            if (contextList.Count > 0)
            {
                string path = ((IEnumerable<PropertyInfo>)contextList).Reverse()
                    .ToString(a => a.Map(p =>
                        p.Name == "Item" && p.GetIndexParameters().Length > 1 ? "/" : p.Name), ".");

                path = path.Replace("./.", "/");

                Type parentType = contextList.Last().DeclaringType;

                switch (PropertyAuthLogic.GetPropertyAccess(parentType, path))
                {
                    case Access.None: 
                        bl.View = false; 
                        break;
                    case Access.Read:
                        if (bl.StyleContext == null)
                            bl.StyleContext = new StyleContext();
                        bl.StyleContext.ReadOnly = true;
                        bl.SetReadOnly();
                        break;
                    case Access.Modify: 
                        break;
                }
            }
        }

       

        //static void Authorize(Type type, TypeAccess typeAccess, EntitySettings settings)
        //{
        //    if (typeAccess == TypeAccess.None)
        //        settings.IsViewable = admin => false;

        //    if (typeAccess <= TypeAccess.Read)
        //        settings.IsReadOnly = admin => true;

        //    if (typeAccess <= TypeAccess.Modify)
        //        settings.IsCreable = admin => false;
        //}

    }
}
