using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
                FromStates = { UserState.New },
                ToState = UserState.Saved,
                Execute = (u, _) => { u.State = UserState.Saved; },
                AllowsNew = true,
                Lite = false
            }.Register();

            new Execute(UserOperation.Save)
            {
                FromStates = { UserState.Saved },
                ToState = UserState.Saved,
                Execute = (u, _) => { },
                Lite = false
            }.Register();

            new Execute(UserOperation.Disable)
            {
                FromStates = { UserState.Saved },
                ToState = UserState.Disabled,
                Execute = (u, _) =>
                {
                    Thread.Sleep(500);
                    u.AnulationDate = TimeZoneManager.Now;
                    u.State = UserState.Disabled;
                },
                AllowsNew = false,
                Lite = true
            }.Register();

            new Execute(UserOperation.Enable)
            {
                FromStates = { UserState.Disabled },
                ToState = UserState.Saved,
                Execute = (u, _) =>
                {
                    Thread.Sleep(500);
                    u.AnulationDate = null;
                    u.State = UserState.Saved;
                },
                AllowsNew = false,
                Lite = true
            }.Register();

            new Graph<UserDN>.Execute(UserOperation.SetPassword)
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