import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { LoginAuthMessage, UserEntity } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'
import { LinkContainer } from '@framework/Components';
import { Dropdown, NavItem, NavDropdown, Nav } from 'react-bootstrap';
import { Lite, toLite, is } from '@framework/Signum.Entities';
import * as CultureClient from '../../Translation/CultureClient'
import { SmallProfilePhoto } from '../Templates/ProfilePhoto';


export default function LoginDropdown(p: {
  renderName?: (u: UserEntity) => React.ReactChild;
  changePasswordVisible?: boolean;
  switchUserVisible?: boolean;
  profileVisible?: boolean;
  extraMenuItems?: (user: UserEntity) => React.ReactNode;
}) {

  const currentCulture = CultureClient.currentCulture;
  const user = AuthClient.currentUser();

  if (!user)
    return (
      <LinkContainer to="~/auth/login" className="sf-login">
        <Nav.Link><i className="sf-login-custom-icon"></i><span>{LoginAuthMessage.Login.niceToString()}</span></Nav.Link>
      </LinkContainer>
    );

  const cpv = p.changePasswordVisible ?? true;
  const suv = p.switchUserVisible ?? true;
  const pv = p.profileVisible ?? true;


  function handleProfileClick() {
    import("@framework/Navigator")
      .then(Navigator =>
        Navigator.API.fetchEntityPack(toLite(user))
          .then(pack => Navigator.view(pack))
          .then(u => u && AuthClient.API.fetchCurrentUser(true).then(nu => AuthClient.setCurrentUser(u))));
  }

  var extraButtons = p.extraMenuItems && p.extraMenuItems(user);

  return (
    <NavDropdown className="sf-login-dropdown" id="sfLoginDropdown" title={<span className="d-inline-flex align-items-center"><i className="sf-login-custom-icon"></i><SmallProfilePhoto user={toLite(user)} /> &nbsp;{p.renderName ? p.renderName(user) : user.userName!}</span>} align="end">
      {pv && <NavDropdown.Item id="sf-auth-profile" onClick={handleProfileClick}><FontAwesomeIcon icon="user-pen" fixedWidth className="me-2" /> {LoginAuthMessage.MyProfile.niceToString()}</NavDropdown.Item>}
      {cpv && <LinkContainer to="~/auth/changePassword">
        <NavDropdown.Item><FontAwesomeIcon icon="key" fixedWidth className="me-2" /> {LoginAuthMessage.ChangePassword.niceToString()}</NavDropdown.Item>
      </LinkContainer>} 
      {extraButtons}
      {(cpv || pv || extraButtons) && <NavDropdown.Divider />}
      {suv && <LinkContainer to="~/auth/login"><NavDropdown.Item><FontAwesomeIcon icon="user-group" className="me-2" /> {LoginAuthMessage.SwitchUser.niceToString()}</NavDropdown.Item></LinkContainer>}
      <NavDropdown.Item id="sf-auth-logout" onClick={() => AuthClient.logout()}><FontAwesomeIcon icon="right-from-bracket" fixedWidth className="me-2"/> {LoginAuthMessage.Logout.niceToString()}</NavDropdown.Item>
    </NavDropdown>
  );
}





