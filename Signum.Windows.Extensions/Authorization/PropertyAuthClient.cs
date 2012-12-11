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
            PropertyRoute.SetIsAllowedCallback(pr => pr.GetAllowedFor(PropertyAllowed.Read));

            AuthClient.UpdateCacheEvent += new Action(AuthClient_UpdateCacheEvent);
        }

        static void AuthClient_UpdateCacheEvent()
        {
            propertyRules = Server.Return((IPropertyAuthServer s) => s.AuthorizedProperties());
        }


        static PropertyAllowed GetPropertyAllowed(this PropertyRoute route)
        {
            if (route.PropertyRouteType == PropertyRouteType.MListItems || route.PropertyRouteType == PropertyRouteType.LiteEntity)
                return GetPropertyAllowed(route.Parent);

            if (TypeAuthClient.GetAllowed(route.RootType).Max().GetUI() == TypeAllowedBasic.None)
                return PropertyAllowed.None;

            return propertyRules.GetAllowed(route);
        }

        static string GetAllowedFor(this PropertyRoute route, PropertyAllowed requested)
        {
            if (route.PropertyRouteType == PropertyRouteType.MListItems || route.PropertyRouteType == PropertyRouteType.LiteEntity)
                return GetAllowedFor(route.Parent, requested);

            if (TypeAuthClient.GetAllowed(route.RootType).Max().GetUI() == TypeAllowedBasic.None)
                return "Type {0} is set to None for {1}".Formato(route.RootType.NiceName(), RoleDN.Current);

            var current = propertyRules.GetAllowed(route);
            if (requested > current)
                return "Property {0} is set to {1} for {2}".Formato(route, current, RoleDN.Current);
            
            return null;
        }


        static void Common_RouteTask(FrameworkElement fe, string route, PropertyRoute context)
        {
            if (context.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {
                switch (GetPropertyAllowed(context))
                {
                    case PropertyAllowed.Read: Common.SetIsReadOnly(fe, true); break;
                }
            }
        }
    }
}

