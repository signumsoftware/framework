using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Services;
using System.Windows.Controls;
using System.Windows;
using Signum.Entities.Authorization;

namespace Signum.Windows.Authorization
{
    public static class QueryAuthClient
    {
        static DefaultDictionary<object, bool> authorizedQueries; 

        public static bool Started { get; private set; }

        internal static void Start()
        {
            Started = true;

            Navigator.Manager.IsFindable += qn => GetAllowed(qn);

            MenuManager.Tasks += new Action<MenuItem>(MenuManager_TasksQueries);

            AuthClient.UpdateCacheEvent += new Action(AuthClient_UpdateCacheEvent);
        }

        static void AuthClient_UpdateCacheEvent()
        {
            authorizedQueries = Server.Return((IQueryAuthServer s) => s.QueriesRules()); 
        }

        public static bool GetAllowed(object queryName)
        {
            return authorizedQueries.GetAllowed(queryName);
        }

        static void MenuManager_TasksQueries(MenuItem menuItem)
        {
            if (menuItem.NotSet(MenuItem.VisibilityProperty))
            {
                object tag = menuItem.Tag;

                if (tag == null)
                    return;

                object queryName =
                    tag is Type ? null : //maybe a type but only if in FindOptions
                    tag is FindOptionsBase ? ((FindOptionsBase)tag).QueryName :
                    tag;

                if (queryName != null && Navigator.Manager.QuerySettings.ContainsKey(queryName))
                {
                    if (!GetAllowed(queryName))
                        menuItem.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}

