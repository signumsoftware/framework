import * as React from "react";
import { BsColor } from "./Basic";
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { classes } from "../Globals";
import { JavascriptMessage } from "../Signum.Entities";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";

interface ModalIconProps {
  color?: BsColor;
  classes?: string;
  icon?: IconProp;
  iconAlign?: string;
}

interface ModalHeaderButtonsProps {
  onClose?: () => void;
  onOk?: () => void;
  okDisabled?: boolean;
  onCancel?: () => void;
  closeBeforeTitle?: boolean;
  htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  okButtonProps?: ModalIconProps;
  closeButtonProps?: ModalIconProps;
}

export class ModalHeaderButtons extends React.Component<ModalHeaderButtonsProps> {

  render() {
    const p = this.props;
    var close = this.props.onClose &&
      <button type="button" className="close" aria-label="Close" onClick={this.props.onClose}>
        <span aria-hidden="true">×</span>
      </button>;

    return (
      <div className="modal-header" {...p.htmlAttributes}>
        {p.closeBeforeTitle && close}
        <h4 className="modal-title" >
          {this.props.children}
        </h4>
        {!p.closeBeforeTitle && close}
        {(this.props.onCancel || this.props.onOk) &&
          <div className="btn-toolbar" style={{ flexWrap: "nowrap" }}>
            {this.props.onOk && <button
              className={classes("btn", "btn-" + ((this.props.okButtonProps && this.props.okButtonProps.color) || "primary"), "sf-entity-button sf-close-button sf-ok-button", this.props.okButtonProps && this.props.okButtonProps.classes)}
              disabled={this.props.okDisabled} onClick={this.props.onOk}>
              {this.renderButton(JavascriptMessage.ok.niceToString(), this.props.okButtonProps)}
            </button>
            }
            {this.props.onCancel && <button
              className={classes("btn", "btn-" + ((this.props.closeButtonProps && this.props.closeButtonProps.color) || "light"), "sf-entity-button sf-close-button sf-cancel-button", this.props.closeButtonProps && this.props.closeButtonProps.classes)}
              onClick={this.props.onCancel}>
              {this.renderButton(JavascriptMessage.cancel.niceToString(), this.props.closeButtonProps)}
            </button>
            }
          </div>
        }
      </div>
    );
  }

  renderButton(text: string, mip?: ModalIconProps) {
    if (mip && mip.icon) {
      switch (mip.iconAlign) {
        case "right": return (<span>{text} <FontAwesomeIcon icon={mip.icon} fixedWidth /></span>);
        default: return (<span><FontAwesomeIcon icon={mip.icon} fixedWidth /> {text}</span>);
      }
    }
    else
      return text;
  }
}
