import * as React from 'react'
import { UncontrolledNavDropdown, DropdownItem, NavItem } from 'reactstrap'
import { LinkContainer } from 'react-router-bootstrap'
import { AuthMessage, UserEntity } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'

export default class LoginUserControl extends React.Component<{}, { user: UserEntity }> {

    render() {
        const user = AuthClient.currentUser();

        if (!user)
            return <LinkContainer to="~/auth/login" className="sf-login"><NavItem>{AuthMessage.Login.niceToString() }</NavItem></LinkContainer>;

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
