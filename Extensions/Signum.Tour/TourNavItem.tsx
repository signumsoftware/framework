import * as React from 'react'
import { Nav } from 'react-bootstrap'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { TourHelpers } from "./TourComponent";

export interface TourNavItemProps {
  getTourHelpers: () => TourHelpers | null;
}

export default function TourNavItem(p: TourNavItemProps) {
  
  function onClick() {
    const helpers = p.getTourHelpers();

    if (helpers)
      helpers.start();
  }

  return (
    <Nav.Item id="tour-nav-item" onClick={onClick}>
      <a className="nav-link">
        <FontAwesomeIcon icon="question-circle" />
      </a>
    </Nav.Item>
  );
}
