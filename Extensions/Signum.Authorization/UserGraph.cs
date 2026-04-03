using Signum.Authorization.UserTicket;

namespace Signum.Authorization;

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
            FromStates = { UserState.Active, UserState.New },
            ToStates = { UserState.Active },
            CanBeNew = true,
            CanBeModified = true,
            Execute = (u, _) => { u.State = UserState.Active; }
        }.Register();

        new Execute(UserOperation.Deactivate)
        {
            FromStates = { UserState.Active },
            ToStates = { UserState.Deactivated },
            Execute = (u, _) =>
            {
                u.DisabledOn = Clock.Now;
                u.State = UserState.Deactivated;

                AuthLogic.RecentlyUsersDisabled.Reset();
                var rt = UserTicketLogic.RemoveTickets(u);
            },
        }.Register();

        new Execute(UserOperation.AutoDeactivate)
        {
            FromStates = { UserState.Active },
            ToStates = { UserState.AutoDeactivate },
            Execute = (u, _) =>
            {
                u.DisabledOn = Clock.Now;
                u.State = UserState.AutoDeactivate;

                AuthLogic.RecentlyUsersDisabled.Reset();
                var rt = UserTicketLogic.RemoveTickets(u);
            },
        }.Register();

        new Execute(UserOperation.Reactivate)
        {
            FromStates = { UserState.Deactivated, UserState.AutoDeactivate },
            ToStates = { UserState.Active },
            Execute = (u, _) =>
            {
                u.DisabledOn = null;
                u.State = UserState.Active;

                AuthLogic.RecentlyUsersDisabled.Reset();
            },
        }.Register();

        new Delete(UserOperation.Delete)
        {
            FromStates = { UserState.Deactivated, UserState.AutoDeactivate, UserState.Active },
            Delete = (u, _) =>
            {
                u.Delete();
            },
        }.Register();
    }
}
