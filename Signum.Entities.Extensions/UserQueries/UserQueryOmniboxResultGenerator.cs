using System;
using System.Collections.Generic;
using Signum.Utilities;
using Signum.Entities.Omnibox;

namespace Signum.Entities.UserQueries
{
    public class UserQueryOmniboxResultGenerator : OmniboxResultGenerator<UserQueryOmniboxResult>
    {
        Func<string, int, IEnumerable<Lite<UserQueryEntity>>> autoComplete;

        public UserQueryOmniboxResultGenerator(Func<string, int, IEnumerable<Lite<UserQueryEntity>>> autoComplete)
        {
            this.autoComplete = autoComplete;
        }

        public int AutoCompleteLimit = 5;

        public override IEnumerable<UserQueryOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
        {
            if (tokenPattern != "S" || !OmniboxParser.Manager.AllowedPermission(UserQueryPermission.ViewUserQuery))
                yield break;

            string ident = OmniboxUtils.CleanCommas(tokens[0].Value);

            var userQueries = autoComplete(ident, AutoCompleteLimit);

            foreach (Lite<UserQueryEntity> uq in userQueries)
            {
                var match = OmniboxUtils.Contains(uq, uq.ToString()!, ident);

                yield return new UserQueryOmniboxResult
                {
                    ToStr = ident,
                    ToStrMatch = match,
                    Distance = match!.Distance,
                    UserQuery = (Lite<UserQueryEntity>)uq,
                };
            }
        }

        public override List<HelpOmniboxResult> GetHelp()
        {
            var resultType = typeof(UserQueryOmniboxResult);
            var userQuery = OmniboxMessage.Omnibox_UserQuery.NiceToString();
            return new List<HelpOmniboxResult>
            {
                new HelpOmniboxResult { Text = "'{0}'".FormatWith(userQuery), ReferencedType = resultType }
            };
        }
    }

    public class UserQueryOmniboxResult : OmniboxResult
    {
        public string ToStr { get; set; }
        public OmniboxMatch? ToStrMatch { get; set; }

        public Lite<UserQueryEntity>? UserQuery { get; set; }

        public override string ToString()
        {
            return "\"{0}\"".FormatWith(ToStr);
        }
    }
}
