import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import "./AuthAdmin.css"

interface ColorRadioProps {
  checked: boolean;
  onClicked: (e: React.MouseEvent<HTMLAnchorElement>) => void;
  color: string;
  title?: string;
  icon?: IconProp | null;
}

export class ColorRadio extends React.Component<ColorRadioProps> {
  render() {
    return (
      <a onClick={e => { e.preventDefault(); this.props.onClicked(e); }} title={this.props.title}
        className="sf-auth-chooser"
        style={{ color: this.props.checked ? this.props.color : "#aaa" }}>
        <FontAwesomeIcon icon={this.props.icon || ["far", (this.props.checked ? "dot-circle" : "circle")]} />
      </a>
    );
  }
}

export class GrayCheckbox extends React.Component<{ checked: boolean, onUnchecked: () => void }> {
  render() {
    return (
      <span className="sf-auth-checkbox" onClick={this.props.checked ? this.props.onUnchecked : undefined}>
        <FontAwesomeIcon icon={["far", this.props.checked ? "check-square" : "square"]} />
      </span>
    );
  }
}




