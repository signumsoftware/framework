using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using System.Windows;
using Signum.Utilities;

namespace Signum.Windows.Authorization
{
    public static class PropertyAuthClient
    {
        static DefaultDictionary<PropertyRoute, PropertyAllowed> propertyRules;

        public static bool Started { get; private set; }

        internal static void Start()
        {
            Started = true;

            Common.RouteTask += Common_RouteTask;
            Common.LabelOnlyRouteTask += Common_RouteTask;
            PropertyRoute.SetIsAllowedCallback(pr => GetPropertyAllowed(pr) >= PropertyAllowed.Read);

            AuthClient.UpdateCacheEvent += new Action(AuthClient_UpdateCacheEvent);
        }

        static void AuthClient_UpdateCacheEvent()
        {
            propertyRules = Server.Return((IPropertyAuthServer s) => s.AuthorizedProperties());
        }


        static PropertyAllowed GetPropertyAllowed(PropertyRoute route)
        {
            if (route.PropertyRouteType == PropertyRouteType.MListItems || route.PropertyRouteType == PropertyRouteType.LiteEntity)
                return GetPropertyAllowed(route.Parent);

            return propertyRules.GetAllowed(route);
        }


        static void Common_RouteTask(FrameworkElement fe, string route, PropertyRoute context)
        {
            if (context.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {
                switch (GetPropertyAllowed(context))
                {
                    case PropertyAllowed.None: fe.Visibility = Visibility.Collapsed; break;
                    case PropertyAllowed.Read: Common.SetIsReadOnly(fe, true); break;
                    case PropertyAllowed.Modify: break;
                }
            }
        }
    }
}

