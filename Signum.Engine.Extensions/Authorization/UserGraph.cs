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
    public class UserGraph : Graph<UserEntity, UserState>
    {
        public static void Register()
        {
            GetState = u => u.State;

            new Construct(UserOperation.Create)
            {
                ToStates = { UserState.New },
                Construct = args => new UserEntity { State = UserState.New }
            }.Register();

            new Execute(UserOperation.SaveNew)
            {
                FromStates = { UserState.New },
                ToStates = { UserState.Saved },
                Execute = (u, _) => { u.State = UserState.Saved; },
                AllowsNew = true,
                Lite = false
            }.Register();

            new Execute(UserOperation.Save)
            {
                FromStates = { UserState.Saved },
                ToStates = { UserState.Saved },
                Execute = (u, _) => { },
                Lite = false
            }.Register();

            new Execute(UserOperation.Disable)
            {
                FromStates = { UserState.Saved },
                ToStates = { UserState.Disabled },
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
                FromStates = { UserState.Disabled },
                ToStates = { UserState.Saved },
                Execute = (u, _) =>
                {
                    u.AnulationDate = null;
                    u.State = UserState.Saved;
                },
                AllowsNew = false,
                Lite = true
            }.Register();

            new Graph<UserEntity>.Execute(UserOperation.SetPassword)
            {
                Lite = true,
                Execute = (u, args) =>
                {
                    byte[] passwordHash = args.GetArg<byte[]>();
                    u.PasswordHash = passwordHash;
                }
            }.Register();
        }
    }
}