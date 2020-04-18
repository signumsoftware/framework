import * as React from 'react'
import { Nav } from 'react-bootstrap'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { JoyrideComponentHandle } from "./JoyrideComponent";

export interface JoyrideNavItemProps {
  getJoyrideComponent: () => JoyrideComponentHandle;
}

export default function JoyrideNavItem(p : JoyrideNavItemProps){
  
  function onClick() {
    const joyrideComponent = p.getJoyrideComponent();

    if (joyrideComponent?.joyride)
      joyrideComponent.joyride.reset(true);
  }

  return (
    <Nav.Item id="help-nav-item" onClick={onClick}>
      <a className="nav-link">
        <FontAwesomeIcon icon={"question-circle"} />
      </a>
    </Nav.Item>
  );
}
