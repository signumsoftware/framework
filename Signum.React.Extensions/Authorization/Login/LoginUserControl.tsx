import * as React from 'react'
import { UncontrolledNavDropdown, DropdownItem, NavItem, NavLink } from 'reactstrap'
import { AuthMessage, UserEntity } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'
import { LinkContainer } from '../../../../Framework/Signum.React/Scripts/LinkContainer'


export default class LoginUserControl extends React.Component<{}, { user: UserEntity }> {

    render() {
        const user = AuthClient.currentUser();

        if (!user)
            return <NavItem><LinkContainer to="~/auth/login" className="sf-login"><NavLink>{AuthMessage.Login.niceToString()}</NavLink></LinkContainer></NavItem>;

        return (
            <UncontrolledNavDropdown className="sf-user" title={user.userName!} id="sfUserDropDown">
                <LinkContainer to="~/auth/changePassword">
                    <DropdownItem><i className="fa fa-key fa-fw"></i> {AuthMessage.ChangePassword.niceToString()}</DropdownItem>
                </LinkContainer>
                <DropdownItem divider />
                <DropdownItem id="sf-auth-logout" onSelect={() => AuthClient.logout()}><i className="fa fa-sign-out fa-fw"></i> {AuthMessage.Logout.niceToString()}</DropdownItem>
            </UncontrolledNavDropdown>
        );
    }
}
