import * as React from 'react'
import { UncontrolledDropdown, DropdownItem, NavItem, NavLink, DropdownToggle, DropdownMenu } from 'reactstrap'
import { AuthMessage, UserEntity } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'
import { LinkContainer } from '../../../../Framework/Signum.React/Scripts/LinkContainer'


export default class LoginDropdown extends React.Component<{}, { user: UserEntity }> {

    render() {
        const user = AuthClient.currentUser();

        if (!user)
            return <NavItem><LinkContainer to="~/auth/login" className="sf-login"><NavLink>{AuthMessage.Login.niceToString()}</NavLink></LinkContainer></NavItem>;

        return (
            <UncontrolledDropdown className="sf-user" id="sfUserDropDown">
                <DropdownToggle nav caret>
                    {user.userName!}
                </DropdownToggle>
                <DropdownMenu right style={{ minWidth: "200px" }}>
                    <LinkContainer to="~/auth/changePassword">
                        <DropdownItem><i className="fa fa-key fa-fw"></i> {AuthMessage.ChangePassword.niceToString()}</DropdownItem>
                    </LinkContainer>
                    <DropdownItem divider />
                    <DropdownItem id="sf-auth-logout" onClick={() => AuthClient.logout()}><i className="fa fa-sign-out fa-fw"></i> {AuthMessage.Logout.niceToString()}</DropdownItem>
                </DropdownMenu>
            </UncontrolledDropdown>
        );
    }
}
