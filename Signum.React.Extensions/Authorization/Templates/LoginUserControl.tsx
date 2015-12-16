/// <reference path="../../../../framework/signum.react/scripts/globals.ts" />

import * as React from 'react'
import { Link } from 'react-router'
import { NavDropdown, MenuItem, NavItem } from 'react-bootstrap'
import { LinkContainer } from 'react-router-bootstrap'
import { AuthMessage, UserEntity } from 'Extensions/Signum.React.Extensions/Authorization/Signum.Entities.Authorization'
import * as AuthClient from 'Extensions/Signum.React.Extensions/Authorization/AuthClient'

export default class LoginUserControl extends React.Component<{}, { user: UserEntity }> {

    constructor(props) {
        super(props);
        this.state = { user: AuthClient.currentUser() };
    }

    componentWillMount() {
        document.addEventListener(AuthClient.CurrentUserChangedEvent,
            () => this.setState({ user: AuthClient.currentUser() }));

        if (!this.state.user)
            AuthClient.Api.currentUser().then(u=> AuthClient.setCurrentUser(u));
    }

    render() {

        if (!this.state.user)
            return <LinkContainer to={"auth/login"}><NavItem  className="sf-login">{AuthMessage.Login.niceToString() }</NavItem></LinkContainer>;

        return <NavDropdown className="sf-user" title={this.state.user.userName} id="sfUserDropDown">
          <LinkContainer to={"auth/changPassword"}><MenuItem>{AuthMessage.ChangePassword.niceToString() }</MenuItem></LinkContainer>
          <MenuItem onSelect={() => AuthClient.logout() }>{AuthMessage.Logout.niceToString() }</MenuItem>
            </NavDropdown>;
    }
}
