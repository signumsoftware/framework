import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import "./AuthAdmin.css"
import { classes } from '@framework/Globals';
import { AuthAdminMessage } from './Signum.Authorization.Rules';

interface ColorRadioProps {
  checked: boolean;
  readOnly: boolean;
  onClicked: (e: React.MouseEvent<HTMLAnchorElement>) => void;
  color: string;
  title?: string;
  icon?: IconProp;
}

export function ColorRadio(p : ColorRadioProps): React.JSX.Element{
  return (
    <a
      title={p.title}
      role="button"
      tabIndex={0}
      onKeyDown={e => {
        if (!p.readOnly && (e.key === "Enter" || e.key === " ")) {
          e.preventDefault();
          p.onClicked(e as any);
        }
      }}
      onClick={e => { e.preventDefault(); !p.readOnly && p.onClicked(e); }}
      className={classes("sf-auth-chooser", p.readOnly && "sf-not-allowed")}
      style={{ color: p.checked ? p.color : "var(--bs-secondary-text)" }}>
      <FontAwesomeIcon aria-hidden={true} icon={p.icon ?? ["far", (p.checked ? "circle-dot" : "circle")]!} />
    </a>
  );
}

export function GrayCheckbox(p : { checked: boolean, onUnchecked: () => void, readOnly: boolean }): React.JSX.Element{
  return (
    <span
      className={classes("sf-auth-checkbox", p.readOnly && "sf-not-allowed")}  
      role="checkbox"
      aria-checked={p.checked}
      tabIndex={p.readOnly ? -1 : 0}
      onKeyDown={e => {
        if (!p.readOnly && (e.key === "Enter" || e.key === " ")) {
          e.preventDefault();
          p.onUnchecked();
        }
      }}
      onClick={p.checked && !p.readOnly ? p.onUnchecked : undefined}>
      <FontAwesomeIcon role="img" icon={["far", p.checked ? "square-check" : "square"]}
        title={p.checked ? AuthAdminMessage.Uncheck.niceToString() : AuthAdminMessage.Check.niceToString()} />
    </span>
  );
}




