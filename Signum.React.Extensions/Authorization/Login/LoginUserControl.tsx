/// <reference path="../../../../framework/signum.react/scripts/globals.ts" />

import * as React from 'react'
import { Link } from 'react-router'
import { NavDropdown, MenuItem, NavItem } from 'react-bootstrap'
import { LinkContainer } from 'react-router-bootstrap'
import { AuthMessage, UserEntity } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'

export default class LoginUserControl extends React.Component<{}, { user: UserEntity }> {

    constructor(props) {
        super(props);
        this.state = { user: AuthClient.currentUser() };
    }

    componentWillMount() {
        AuthClient.onCurrentUserChanged.push(newUser => this.setState({ user: newUser }));
    }

    render() {

        if (!this.state.user)
            return <LinkContainer to={"auth/login"}><NavItem  className="sf-login">{AuthMessage.Login.niceToString() }</NavItem></LinkContainer>;

        return (
            <NavDropdown className="sf-user" title={this.state.user.userName} id="sfUserDropDown">
                <LinkContainer to={"auth/changePassword"}><MenuItem>{AuthMessage.ChangePassword.niceToString() }</MenuItem></LinkContainer>
                <MenuItem onSelect={() => AuthClient.logout() }>{AuthMessage.Logout.niceToString() }</MenuItem>
            </NavDropdown>);
    }
}
