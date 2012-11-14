using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Utilities;

namespace Signum.Engine.Authorization
{
    public class UserGraph : Graph<UserDN, UserState>
    {
        public static void Register()
        {
            GetState = u => u.State;

            new Construct(UserOperation.Create)
            {
                ToState = UserState.New,
                Construct = args => new UserDN { State = UserState.New }
            }.Register();

            new Execute(UserOperation.SaveNew)
            {
                FromStates = new[] { UserState.New },
                ToState = UserState.Created,
                Execute = (u, _) => { u.State = UserState.Created; },
                AllowsNew = true,
                Lite = false
            }.Register();

            new Execute(UserOperation.Save)
            {
                FromStates = new[] { UserState.Created },
                ToState = UserState.Created,
                Execute = (u, _) => { },
                Lite = false
            }.Register();

            new Execute(UserOperation.Disable)
            {
                FromStates = new[] { UserState.Created },
                ToState = UserState.Disabled,
                Execute = (u, _) =>
                {
                    u.AnulationDate = TimeZoneManager.Now;
                    u.State = UserState.Disabled;
                },
                AllowsNew = false,
                Lite = true
            }.Register();

            new Execute(UserOperation.Enable)
            {
                FromStates = new[] { UserState.Disabled },
                ToState = UserState.Created,
                Execute = (u, _) =>
                {
                    u.AnulationDate = null;
                    u.State = UserState.Created;
                },
                AllowsNew = false,
                Lite = true
            }.Register();

            new BasicExecute<UserDN>(UserOperation.SetPassword)
            {
                Lite = true,
                Execute = (u, args) =>
                {
                    string passwordHash = args.TryGetArgC<string>();
                    u.PasswordHash = passwordHash;
                }
            }.Register();
        }
    }
}