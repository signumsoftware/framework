import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { AuthMessage, UserEntity } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'
import { DropdownToggle, NavItem, UncontrolledDropdown, DropdownMenu, DropdownItem, NavLink, LinkContainer } from '@framework/Components';


export default class LoginDropdown extends React.Component<{}, { user: UserEntity }> {

    render() {
        const user = AuthClient.currentUser();

        if (!user)
            return (
                <NavItem>
                    <LinkContainer to="~/auth/login" className="sf-login"><NavLink>{AuthMessage.Login.niceToString()}</NavLink>
                    </LinkContainer>
                </NavItem>
            );

        return (
            <UncontrolledDropdown className="sf-user" id="sfUserDropDown" nav inNavbar>
                <DropdownToggle nav caret>
                    {user.userName!}
                </DropdownToggle>
                <DropdownMenu right style={{ minWidth: "200px" }}>
                    <LinkContainer to="~/auth/changePassword">
                        <DropdownItem><FontAwesomeIcon icon="key" fixedWidth /> {AuthMessage.ChangePassword.niceToString()}</DropdownItem>
                    </LinkContainer>
                    <DropdownItem divider />
                    <LinkContainer to="~/auth/login"><DropdownItem><FontAwesomeIcon icon="user-plus" /> {AuthMessage.SwitchUser.niceToString()}</DropdownItem></LinkContainer>
                    <DropdownItem id="sf-auth-logout" onClick={() => AuthClient.logout()}><FontAwesomeIcon icon="sign-out-alt" fixedWidth /> {AuthMessage.Logout.niceToString()}</DropdownItem>
                </DropdownMenu>
            </UncontrolledDropdown>
        );
    }
}
