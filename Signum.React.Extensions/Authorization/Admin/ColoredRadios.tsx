import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import "./AuthAdmin.css"
import { coalesceIcon } from '@framework/Operations/ContextualOperations';

interface ColorRadioProps {
  checked: boolean;
  onClicked: (e: React.MouseEvent<HTMLAnchorElement>) => void;
  color: string;
  title?: string;
  icon?: IconProp;
}

export function ColorRadio(p : ColorRadioProps){
  return (
    <a onClick={e => { e.preventDefault(); p.onClicked(e); }} title={p.title}
      className="sf-auth-chooser"
      style={{ color: p.checked ? p.color : "#aaa" }}>
      <FontAwesomeIcon icon={coalesceIcon(p.icon, ["far", (p.checked ? "dot-circle" : "circle")])!} />
    </a>
  );
}

export function GrayCheckbox(p : { checked: boolean, onUnchecked: () => void }){
  return (
    <span className="sf-auth-checkbox" onClick={p.checked ? p.onUnchecked : undefined}>
      <FontAwesomeIcon icon={["far", p.checked ? "check-square" : "square"]} />
    </span>
  );
}




