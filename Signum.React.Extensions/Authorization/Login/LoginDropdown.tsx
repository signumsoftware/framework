import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { LoginAuthMessage, UserEntity } from '../Signum.Entities.Authorization'
import * as AuthClient from '../AuthClient'
import { LinkContainer } from '@framework/Components';
import { Dropdown, NavItem, NavDropdown, Nav } from 'react-bootstrap';
import { Lite, toLite, is } from '@framework/Signum.Entities';
import * as CultureClient from '../../Translation/CultureClient'
import { simplifyName } from '../../Translation/CultureDropdown';
import { useAPI } from '@framework/Hooks';
import { Dic } from '../../../Signum.React/Scripts/Globals';
import { CultureInfoEntity } from '../../Basics/Signum.Entities.Basics';


export default function LoginDropdown(p: {
  renderName?: (u: UserEntity) => React.ReactChild;
  changePasswordVisible?: boolean;
  switchUserVisible?: boolean;
  profileVisible?: boolean;
  extraButons?: (user: UserEntity) => React.ReactNode;
}) {

  const currentCulture = CultureClient.currentCulture;
  const user = AuthClient.currentUser();

  if (!user)
    return (
      <LinkContainer to="~/auth/login" className="sf-login">
        <Nav.Link>{LoginAuthMessage.Login.niceToString()}</Nav.Link>
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
          .then(u => u && AuthClient.API.fetchCurrentUser(true).then(nu => AuthClient.setCurrentUser(u))))
      .done();
  }

  var extraButtons = p.extraButons && p.extraButons(user);

  return (
    <NavDropdown className="sf-login-dropdown" id="sfLoginDropdown" title={p.renderName ? p.renderName(user) : user.userName!} align="end">
      {pv && <NavDropdown.Item id="sf-auth-profile" onClick={handleProfileClick}><FontAwesomeIcon icon="user-edit" fixedWidth className="me-2" /> {LoginAuthMessage.MyProfile.niceToString()}</NavDropdown.Item>}
      {cpv && <LinkContainer to="~/auth/changePassword">
        <NavDropdown.Item><FontAwesomeIcon icon="key" fixedWidth className="me-2" /> {LoginAuthMessage.ChangePassword.niceToString()}</NavDropdown.Item>
      </LinkContainer>} 
      {extraButtons}

      {(cpv || pv || extraButtons) && <NavDropdown.Divider />}

      <CustomCultureDropdown fullName={true} />

      {suv && <LinkContainer to="~/auth/login"><NavDropdown.Item><FontAwesomeIcon icon="user-friends" className="me-2" /> {LoginAuthMessage.SwitchUser.niceToString()}</NavDropdown.Item></LinkContainer>}
      <NavDropdown.Item id="sf-auth-logout" onClick={() => AuthClient.logout()}><FontAwesomeIcon icon="sign-out-alt" fixedWidth className="me-2"/> {LoginAuthMessage.Logout.niceToString()}</NavDropdown.Item>
    </NavDropdown>
  );
}

function CustomCultureDropdown(props: { fullName?: boolean }) {
  var [show, setShow] = React.useState(false);

  var cultures = useAPI(signal => CultureClient.getCultures(null), []);

  if (!cultures)
    return null;

  const current = CultureClient.currentCulture;

  const pair = Dic.map(cultures, (name, c) => ({ name, c })).singleOrNull(p => is(p.c, current));
  function handleSelect(c: Lite<CultureInfoEntity>) {
    CultureClient.changeCurrentCulture(c);
  }

  function simplifyName(name: string) {
    return name.tryBefore("(")?.trim() ?? name;
  }

  return (
    <div>
      <div className={"dropdown-item"}
        style={{ cursor: "pointer", userSelect: "none", display: "flex", alignItems: "center" }}
        onClick={() => setShow(!show)}>
        <FontAwesomeIcon icon="globe" fixedWidth className="mr-2" /> <span style={{ width: "100%" }}>{CultureInfoEntity.niceName()}</span> <FontAwesomeIcon icon={!show ? "caret-down" : "caret-up"} />
      </div>
      <div style={{ display: show ? "block" : "none" }}>
        {Dic.map(cultures, (name, c, i) =>
          <NavDropdown.Item key={i} data-culture={name} disabled={is(c, current)} onClick={() => handleSelect(c)}>
            {props.fullName ? c.toStr : simplifyName(c.toStr!)}
          </NavDropdown.Item>
        )}
      </div>
    </div>
  );
} 



