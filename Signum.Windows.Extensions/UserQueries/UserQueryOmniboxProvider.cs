using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.Omnibox;
using Signum.Entities.UserQueries;
using Signum.Entities.Omnibox;
using System.Windows.Documents;
using System.Windows.Media;
using Signum.Utilities;
using Signum.Windows.Authorization;
using System.Windows;
using Signum.Services;

namespace Signum.Windows.UserQueries
{
    public class UserQueryOmniboxProvider: OmniboxProvider<UserQueryOmniboxResult>
    {
        public override OmniboxResultGenerator<UserQueryOmniboxResult> CreateGenerator()
        {
            return new UserQueryOmniboxResultGenerator((subString, limit) => Server.Return((IUserQueryServer s) => s.AutocompleteUserQueries(subString, limit)));
        }

        public override void RenderLines(UserQueryOmniboxResult result, InlineCollection lines)
        {
            lines.AddMatch(result.ToStrMatch);
        }

        public override Run GetIcon()
        {
            return new Run("({0})".FormatWith(typeof(UserQueryEntity).NiceName())) { Foreground = Brushes.DodgerBlue };
        }

        public override void OnSelected(UserQueryOmniboxResult result, Window window)
        {
            UserQueryEntity uq = result.UserQuery.RetrieveAndForget();

            var query = QueryClient.GetQueryName(uq.Query.Key);

            Finder.Explore(new ExploreOptions(query)
            {
                InitializeSearchControl = sc => UserQueryClient.SetUserQuery(sc, uq)
            });
        }

        public override string GetName(UserQueryOmniboxResult result)
        {
            return "UQ:" + result.UserQuery.Key();
        }
    }
}
