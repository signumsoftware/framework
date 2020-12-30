import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { LoginAuthMessage, UserEntity } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'
import { LinkContainer } from '@framework/Components';
import { Dropdown, NavItem, NavDropdown, Nav } from 'react-bootstrap';


export default function LoginDropdown(p: { renderName?: (u: UserEntity) => React.ReactChild, changePasswordVisible?: boolean, switchUserVisible?: boolean, extraButons?: (user: UserEntity) => React.ReactNode }) {

  const user = AuthClient.currentUser();

  if (!user)
    return (
      <LinkContainer to="~/auth/login" className="sf-login">
        <Nav.Link>{LoginAuthMessage.Login.niceToString()}</Nav.Link>
      </LinkContainer>
    );

  const cpv = p.changePasswordVisible == null ? true : p.changePasswordVisible;
  const suv = p.switchUserVisible == null ? true : p.switchUserVisible;

  return (
    <NavDropdown className="sf-login-dropdown" id="sfLoginDropdown" title={p.renderName ? p.renderName(user) : user.userName!} alignRight >
      {cpv && <LinkContainer to="~/auth/changePassword">
        <NavDropdown.Item><FontAwesomeIcon icon="key" fixedWidth className="mr-2" /> {LoginAuthMessage.ChangePassword.niceToString()}</NavDropdown.Item>
      </LinkContainer>}
      {p.extraButons && p.extraButons(user)}
      {cpv && <NavDropdown.Divider />}
      {suv && <LinkContainer to="~/auth/login"><NavDropdown.Item><FontAwesomeIcon icon="user-plus" className="mr-2" /> {LoginAuthMessage.SwitchUser.niceToString()}</NavDropdown.Item></LinkContainer>}
      <NavDropdown.Item id="sf-auth-logout" onClick={() => AuthClient.logout()}><FontAwesomeIcon icon="sign-out-alt" fixedWidth className="mr-2"/> {LoginAuthMessage.Logout.niceToString()}</NavDropdown.Item>
    </NavDropdown>
  );
}
