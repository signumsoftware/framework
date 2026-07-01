import * as React from 'react'
import { Nav } from 'react-bootstrap'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Driver } from 'driver.js';

export interface TourNavItemProps {
  getDriver: () => Driver | null;
}

export default function TourNavItem(p: TourNavItemProps) : React.ReactElement {

  function onClick() {
    const driver = p.getDriver();

    if (driver)
      driver.drive();
  }

  return (
    <Nav.Item id="tour-nav-item" onClick={onClick}>
      <a className="nav-link">
        <FontAwesomeIcon icon="question-circle" />
      </a>
    </Nav.Item>
  );
}
