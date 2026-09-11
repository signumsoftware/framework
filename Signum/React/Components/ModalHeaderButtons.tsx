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
  closeBeforeTitle?: boolean;
  htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  closeButtonProps?: ModalIconProps;
  children?: React.ReactNode;
  stickyHeader?: boolean;
  /** Put on the title heading so the surrounding <Modal> can point aria-labelledby at it, which is what
   * gives the dialog its accessible name (WCAG 4.1.2). Generate it with React.useId() in the modal, so
   * nested dialogs — this application opens up to three at once — never collide on the same id. */
  titleId?: string;
}

export function ModalHeaderButtons(p: ModalHeaderButtonsProps): React.ReactElement {

  // Localized: the literal "Close" was announced in English regardless of the interface language (WCAG 3.1.2).
  var close = p.onClose &&
    <button type="button" className="btn-close" aria-label={JavascriptMessage.Close.niceToString()} onClick={p.onClose}/>

  return (
    <div className={classes("modal-header align-items-start", p.stickyHeader && "sf-sticky-header")} {...p.htmlAttributes } >
      {p.closeBeforeTitle && close}
      <h1 className="modal-title h4" id={p.titleId}>
        {p.children}
      </h1>
      {!p.closeBeforeTitle && close}
    </div>
  );
}


interface ModalFooterButtonsProps {
  onOk?: () => void;
  okDisabled?: boolean;
  onCancel?: () => void;
  htmlAttributes?: React.HTMLAttributes<HTMLDivElement>;
  okButtonProps?: ModalIconProps;
  cancelButtonProps?: ModalIconProps;
  children?: React.ReactNode;
}


export function ModalFooterButtons(p: ModalFooterButtonsProps): React.ReactElement {

  return (
    <div className="modal-footer" {...p.htmlAttributes}>
      <h1 className="modal-title h4" >
        {p.children}
      </h1>
      {(p.onCancel || p.onOk) &&
        <div className="btn-toolbar" style={{ flexWrap: "nowrap" }}>
          {p.onOk && <button
            type="button"
            className={classes("btn", "btn-" + (p.okButtonProps?.color ?? "primary"), "sf-entity-button sf-close-button sf-ok-button", p.okButtonProps?.classes)}
            disabled={p.okDisabled}
            aria-disabled={p.okDisabled}
            onClick={p.onOk}>
            {renderButton(JavascriptMessage.ok.niceToString(), p.okButtonProps)}
          </button>
          }
          {p.onCancel && <button
            type="button"
            className={classes("btn", "btn-" + (p.cancelButtonProps?.color ?? "light"), "sf-entity-button sf-close-button sf-cancel-button", p.cancelButtonProps?.classes)}
            onClick={p.onCancel}>
          {renderButton(JavascriptMessage.cancel.niceToString(), p.cancelButtonProps)}
          </button>
          }
        </div>
      }
    </div>
  );

  function renderButton(text: string, mip?: ModalIconProps) {
    if (mip?.icon) {
      switch (mip.iconAlign) {
        case "right": return (<span>{text} <FontAwesomeIcon aria-hidden={true} icon={mip.icon} className="fa-fw" /></span>);
        default: return (<span><FontAwesomeIcon aria-hidden={true} icon={mip.icon} className="fa-fw" /> {text}</span>);
      }
    }
    else
      return text;
  }
}
