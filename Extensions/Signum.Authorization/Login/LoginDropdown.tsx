import * as React from "react";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { LoginAuthMessage, UserEntity } from "../Signum.Authorization";
import { AuthClient } from "../AuthClient";
import { LinkContainer } from "@framework/Components";
import { Dropdown, NavItem, NavDropdown, Nav } from "react-bootstrap";
import { Lite, toLite, is, getToString } from "@framework/Signum.Entities";
import { CultureClient } from "@framework/Basics/CultureClient";
import { SmallProfilePhoto } from "../Templates/ProfilePhoto";

function LoginDropdown(p: {
  renderName?: (u: UserEntity) => React.ReactElement | string | null;
  renderIcon?: (u: UserEntity) => React.ReactNode;
  changePasswordVisible?: boolean;
  switchUserVisible?: boolean;
  profileVisible?: boolean;
  extraMenuItems?: (user: UserEntity) => React.ReactNode | undefined | null;
}): React.JSX.Element {
  const currentCulture = CultureClient.currentCulture;
  const user = AuthClient.currentUser();

  if (!user)
    return (
      <LinkContainer to="/auth/login" className="sf-login">
        <Nav.Link>
          {LoginDropdown.customLoginIcon(user)}
          <span> {LoginAuthMessage.Login.niceToString()}</span>
        </Nav.Link>
      </LinkContainer>
    );

  const cpv = p.changePasswordVisible ?? true;
  const suv = p.switchUserVisible ?? true;
  const pv = p.profileVisible ?? true;

  function handleProfileClick() {
    import("@framework/Navigator").then((file) =>
      file.Navigator.API.fetchEntityPack(toLite(user))
        .then((pack) => file.Navigator.view(pack))
        .then(
          (u) =>
            u &&
            AuthClient.API.fetchCurrentUser(true).then((nu) =>
              AuthClient.setCurrentUser(u)
            )
        )
    );
  }

  var extraButtons = p.extraMenuItems && p.extraMenuItems(user);

  return (
    <NavDropdown
      className="sf-login-dropdown"
      id="sfLoginDropdown"
      title={
        <span className="d-inline-flex align-items-center">
          {p.renderIcon
            ? p.renderIcon(user)
            : LoginDropdown.customLoginIcon(user)}
          &nbsp;
          {p.renderName ? p.renderName(user) : getToString(user)}
        </span>
      }
      align="end"
    >
      {pv && (
        <NavDropdown.Item id="sf-auth-profile" onClick={handleProfileClick}>
          <FontAwesomeIcon icon="user-pen" fixedWidth className="me-2" />{" "}
          {LoginAuthMessage.MyProfile.niceToString()}
        </NavDropdown.Item>
      )}
      {cpv && (
        <LinkContainer to="/auth/changePassword">
          <NavDropdown.Item>
            <FontAwesomeIcon icon="key" fixedWidth className="me-2" />{" "}
            {LoginAuthMessage.ChangePassword.niceToString()}
          </NavDropdown.Item>
        </LinkContainer>
      )}
      {extraButtons}
      {(cpv || pv || extraButtons) && <NavDropdown.Divider />}
      {suv && (
        <LinkContainer to="/auth/login">
          <NavDropdown.Item>
            <FontAwesomeIcon icon="user-group" className="me-2" />{" "}
            {LoginAuthMessage.SwitchUser.niceToString()}
          </NavDropdown.Item>
        </LinkContainer>
      )}
      <NavDropdown.Item id="sf-auth-logout" onClick={() => AuthClient.logout()}>
        <FontAwesomeIcon
          icon="right-from-bracket"
          fixedWidth
          className="me-2"
        />{" "}
        {LoginAuthMessage.Logout.niceToString()}
      </NavDropdown.Item>
    </NavDropdown>
  );
}

namespace LoginDropdown {
  export function customLoginIcon(
    user: UserEntity | null | undefined
  ): React.JSX.Element {
    return user ? (
      <SmallProfilePhoto user={toLite(user)} />
    ) : (
      <FontAwesomeIcon icon="user" className="me-1" />
    );
  }
}

export default LoginDropdown;
