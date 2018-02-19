import * as React from 'react'
import { NavDropdown, MenuItem, NavItem } from 'react-bootstrap'
import { LinkContainer } from 'react-router-bootstrap'
import { AuthMessage, UserEntity } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'

export default class LoginUserControl extends React.Component<{}, { user: UserEntity }> {

    render() {
        const user = AuthClient.currentUser();

        if (!user)
            return <LinkContainer to="~/auth/login" className="sf-login"><NavItem>{AuthMessage.Login.niceToString() }</NavItem></LinkContainer>;

        return (
            <NavDropdown className="sf-user" title={user.userName!} id="sfUserDropDown">
                <LinkContainer to="~/auth/changePassword"><MenuItem><i className="fa fa-key fa-fw"></i> {AuthMessage.ChangePassword.niceToString()}</MenuItem></LinkContainer>
                <MenuItem divider />
                <LinkContainer to="~/auth/login"><MenuItem><i className="fa fa-user-plus"></i> {AuthMessage.SwitchUser.niceToString()}</MenuItem></LinkContainer>
                <MenuItem id="sf-auth-logout" onSelect={() => AuthClient.logout()}><i className="fa fa-sign-out fa-fw"></i> {AuthMessage.Logout.niceToString()}</MenuItem>
            </NavDropdown>
        );
    }
}
