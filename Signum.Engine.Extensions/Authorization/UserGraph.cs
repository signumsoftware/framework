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

            new Construct(UserOperation.Create, UserState.New)
            {
                Construct = args => new UserDN { State = UserState.New }
            }.Register();

            new Goto(UserOperation.SaveNew, UserState.Created)
            {
                FromStates = new[] { UserState.New },
                Execute = (u, _) => { u.State = UserState.Created; },
                AllowsNew = true,
                Lite = false
            }.Register();

            new Goto(UserOperation.Save, UserState.Created)
            {
                FromStates = new[] { UserState.Created },
                Execute = (u, _) => { },
                Lite = false
            }.Register();

            new Goto(UserOperation.Disable, UserState.Disabled)
            {
                FromStates = new[] { UserState.Created },
                Execute = (u, _) =>
                {
                    u.AnulationDate = TimeZoneManager.Now;
                    u.State = UserState.Disabled;
                },
                AllowsNew = false,
                Lite = true
            }.Register();

            new Goto(UserOperation.Enable, UserState.Created)
            {
                FromStates = new[] { UserState.Disabled },
                Execute = (u, _) =>
                {
                    u.AnulationDate = null;
                    u.State = UserState.Created;
                },
                AllowsNew = false,
                Lite = true
            }.Register();
        }
    }
}