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

            new Execute(UserOperation.Save)
            {
                FromStates = { UserState.Saved, UserState.New },
                ToStates = { UserState.Saved },
                CanBeNew = true,
                CanBeModified = true,
                Execute = (u, _) => { u.State = UserState.Saved; }
            }.Register();

            new Execute(UserOperation.Disable)
            {
                FromStates = { UserState.Saved },
                ToStates = { UserState.Disabled },
                Execute = (u, _) =>
                {
                    u.DisabledOn = TimeZoneManager.Now;
                    u.State = UserState.Disabled;
                },
            }.Register();

            new Execute(UserOperation.Enable)
            {
                FromStates = { UserState.Disabled },
                ToStates = { UserState.Saved },
                Execute = (u, _) =>
                {
                    u.DisabledOn = null;
                    u.State = UserState.Saved;
                },
            }.Register();

            new Graph<UserEntity>.Execute(UserOperation.SetPassword)
            {
                Execute = (u, args) =>
                {
                    byte[] passwordHash = args.GetArg<byte[]>();
                    u.PasswordHash = passwordHash;
                }
            }.Register();
        }
    }
}
