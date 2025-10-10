import * as React from 'react'
import { Dic } from '../Globals';
import { Lite, is, getToString } from '../Signum.Entities'
import { CultureInfoEntity } from '../Signum.Basics'
import { CultureClient } from './CultureClient'
import { NavDropdown } from 'react-bootstrap';
import { useAPI } from '../Hooks';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { IconName } from '@fortawesome/fontawesome-svg-core';

export default function CultureDropdown(p: { fullName?: boolean; isMobile?: boolean }): React.ReactElement | null {

  var cultures = useAPI(signal => CultureClient.getCultures(false), []);

  if (!cultures)
    return null;

  const current = CultureClient.currentCulture;

  function handleSelect(c: Lite<CultureInfoEntity>) {
    CultureClient.changeCurrentCulture(c);
  }
  const dropdownTitle = p.isMobile
    ? <FontAwesomeIcon icon={"globe"} />
    : (p.fullName ? current.nativeName : simplifyName(current.nativeName));
  return (
    <NavDropdown data-culture={current.name} title={dropdownTitle} className="sf-culture-dropdown">
      {Dic.map(cultures, (name, c, i) =>
        <NavDropdown.Item key={i} data-culture={name} active={is(c, current)} onClick={() => handleSelect(c)}>
          {p.fullName ? getToString(c) : simplifyName(getToString(c)!)}
        </NavDropdown.Item>
      )}
    </NavDropdown >
  );
}

function simplifyName(name: string) {
  return name.tryBefore("(")?.trim() ?? name;
}

export function CultureDropdownMenuItem(props: {
  fullName?: boolean,
  label?: string;
  iconProps?: {
    chevronIcon?: {
      open: IconName;
      close: IconName;
    }
  }
}): React.ReactElement | null {
  var [show, setShow] = React.useState(false);

  var cultures = useAPI(signal => CultureClient.getCultures(false), []);

  if (!cultures)
    return null;

  const current = CultureClient.currentCulture;

  function handleSelect(c: Lite<CultureInfoEntity>) {
    CultureClient.changeCurrentCulture(c);
  }

  function getChevronIcon() {
    const chevronIcon = props.iconProps?.chevronIcon;
    if (chevronIcon) {
      return show ? chevronIcon.close : chevronIcon.open;
    }

    return show ? "caret-down" : "caret-up"
  }

  return (
    <div>
      <div className={"dropdown-item"}
        style={{ cursor: "pointer", userSelect: "none", display: "flex", alignItems: "center" }}
        onClick={() => setShow(!show)}>
        <FontAwesomeIcon icon="globe" fixedWidth className="me-2" /> <span style={{ width: "100%" }}>{props.label || CultureInfoEntity.niceName()}</span> <FontAwesomeIcon icon={getChevronIcon()} />
      </div>
      <div style={{ display: show ? "block" : "none" }}>
        {Dic.map(cultures, (name, c, i) =>
          <NavDropdown.Item key={i} data-culture={name} active={is(c, current)} onClick={() => handleSelect(c)}>
            {props.fullName ? getToString(c) : simplifyName(getToString(c)!)}
          </NavDropdown.Item>
        )}
      </div>
    </div>
  );
} 
