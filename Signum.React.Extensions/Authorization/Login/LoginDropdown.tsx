import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { AuthMessage, UserEntity } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'
import { LinkContainer } from '@framework/Components';
import { Dropdown, NavItem, NavDropdown, Nav } from 'react-bootstrap';


export default function LoginDropdown(p: { renderName?: (u: UserEntity) => React.ReactChild, changePasswordVisible?: boolean }) {

  const user = AuthClient.currentUser();

  if (!user)
    return (
      <LinkContainer to="~/auth/login" className="sf-login">
        <Nav.Link>{AuthMessage.Login.niceToString()}</Nav.Link>
      </LinkContainer>
    );

  const cpv = p.changePasswordVisible == null ? true : p.changePasswordVisible;

  return (
    <NavDropdown className="sf-login-dropdown" id="sfLoginDropdown" title={p.renderName ? p.renderName(user) : user.userName!} alignRight >
      {cpv && <LinkContainer to="~/auth/changePassword">
        <NavDropdown.Item><FontAwesomeIcon icon="key" fixedWidth /> {AuthMessage.ChangePassword.niceToString()}</NavDropdown.Item>
      </LinkContainer>}
      {cpv && <NavDropdown.Divider />}
      <LinkContainer to="~/auth/login"><NavDropdown.Item><FontAwesomeIcon icon="user-plus" /> {AuthMessage.SwitchUser.niceToString()}</NavDropdown.Item></LinkContainer>
      <NavDropdown.Item id="sf-auth-logout" onClick={() => AuthClient.logout()}><FontAwesomeIcon icon="sign-out-alt" fixedWidth /> {AuthMessage.Logout.niceToString()}</NavDropdown.Item>
    </NavDropdown>
  );
}
