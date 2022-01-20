import * as React from 'react'
import { openModal, IModalProps } from '../Modals';
import { classes } from '../Globals';
import { JavascriptMessage, BooleanEnum } from '../Signum.Entities'
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import "./Modals.css"
import { BsSize } from '../Components';
import { Modal } from 'react-bootstrap';

export type MessageModalStyle = "success" | "info" | "warning" | "error";

export type MessageModalIcon = "info" | "question" | "warning" | "error" | "success";

export type MessageModalButtons = "ok" | "cancel" | "ok_cancel" | "yes_no" | "yes_cancel" | "yes_no_cancel";

export type MessageModalResult = "ok" | "cancel" | "yes" | "no";

interface MessageModalContext {
  handleButtonClicked(m: MessageModalResult) : void;
}

interface MessageModalProps extends IModalProps<MessageModalResult | undefined> {
  title: React.ReactChild;
  message: React.ReactChild | ((ctx: MessageModalContext) => React.ReactChild);
  style?: MessageModalStyle;
  buttons: MessageModalButtons;
  buttonContent?: (button: MessageModalResult) => React.ReactChild | null | undefined;
  icon?: MessageModalIcon;
  customIcon?: IconProp;
  size?: BsSize;
}

export default function MessageModal(p: MessageModalProps) {

  const [show, setShow] = React.useState(true);

  const selectedValue = React.useRef<MessageModalResult | undefined>(undefined);

  function handleButtonClicked(val: MessageModalResult) {
    selectedValue.current = val;
    setShow(false);
  }

  function handleCancelClicked() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(selectedValue.current);
  }

  function getButtonContent(button: MessageModalResult)
  {
    
    const content = p.buttonContent && p.buttonContent(button);
    if (content)
      return content

    switch (button) {
      case "ok":
        return JavascriptMessage.ok.niceToString();
      case "cancel":
        return JavascriptMessage.cancel.niceToString();
      case "yes":
        return BooleanEnum.niceToString("True");
      case "no":
        return BooleanEnum.niceToString("False");
    }
  }

  function okButton() {
    return <button
      className="btn btn-primary sf-close-button sf-ok-button"
      onClick={() => handleButtonClicked("ok")}
      name="accept">
      {getButtonContent("ok")}
    </button>
  }

  function cancelButton() {
    return <button
      className="btn btn-secondary sf-close-button sf-cancel-button"
      onClick={() => handleButtonClicked("cancel")}
      name="cancel">
      {getButtonContent("cancel")}
    </button>
  }

  function yesButton() {
    return <button
      className="btn btn-primary sf-close-button sf-yes-button"
      onClick={() => handleButtonClicked("yes")}
      name="yes">
      {getButtonContent("yes")}
    </button>
  }

  function noButton() {
    return <button
      className="btn btn-secondary sf-close-button sf-no-button"
      onClick={() => handleButtonClicked("no")}
      name="no">
      {getButtonContent("no")}
    </button>
  }

  function renderButtons(buttons: MessageModalButtons) {
    switch (buttons) {
      case "ok":
        return okButton();
      case "cancel":
        return cancelButton();
      case "ok_cancel":
        return (
          <div className="btn-toolbar"> {okButton()} {cancelButton()} </div>);
      case "yes_no":
        return (<div className="btn-toolbar"> {yesButton()} {noButton()} </div>);
      case "yes_cancel":
        return (<div className="btn-toolbar"> {yesButton()} {cancelButton()} </div>);
      case "yes_no_cancel":
        return (<div className="btn-toolbar"> {yesButton()} {noButton()} {cancelButton()} </div>);
    }
  }

  function getIcon() {
    var icon: IconProp | undefined;

    if (p.customIcon)
      icon = p.customIcon;

    if (p.icon) {
      switch (p.icon) {
        case "info":
          icon = "info-circle";
          break;
        case "error":
          icon = "exclamation-circle";
          break;
        case "question":
          icon = "question-circle";
          break;
        case "success":
          icon = "check-circle";
          break;
        case "warning":
          icon = "exclamation-triangle";
          break;
      }
    }

    return icon;
  }

  function renderTitle() {
    var icon = getIcon();

    var iconSpan = icon && <FontAwesomeIcon icon={icon} />;

    return (
      <span>
        {iconSpan && iconSpan}{iconSpan && <span>&nbsp;&nbsp;</span>}{p.title}
      </span>
    );
  }

  return (
    <Modal show={show} onExited={handleOnExited}
      dialogClassName={classes("message-modal", p.size && "modal-" + p.size)}
      onHide={handleCancelClicked} autoFocus={true}>
      <div className={classes("modal-header", dialogHeaderClass(p.style))}>
        {renderTitle()}
      </div>
      <div className="modal-body">
        {
          typeof p.message == "string" ? p.message.split("\n").map((line, i) => <p key={i} className={dialogTextClass(p.style)}>{line}</p>) :
            typeof p.message == "function" ? p.message({ handleButtonClicked }) :
              p.message
        }
      </div>
      <div className="modal-footer">
        {renderButtons(p.buttons)}
      </div>
    </Modal>
  );
}

MessageModal.show = (options: MessageModalProps): Promise<MessageModalResult | undefined> => {
  return openModal<MessageModalResult>(
    <MessageModal
      title={options.title}
      message={options.message}
      buttons={options.buttons}
      buttonContent={options.buttonContent}
      icon={options.icon}
      size={options.size}
      customIcon={options.customIcon}
      style={options.style}
    />
  );
}

MessageModal.showError = (message: React.ReactChild, title?: string): Promise<undefined> => {
  return MessageModal.show({ buttons: "ok", icon: "error", style: "error", title: title ?? JavascriptMessage.error.niceToString(), message: message })
    .then(() => undefined);
}

function dialogHeaderClass(style: MessageModalStyle | undefined) {
  switch (style) {
    case "success":
      return "dialog-header-success";
    case "info":
      return "dialog-header-info";
    case "warning":
      return "dialog-header-warning";
    case "error":
      return "dialog-header-error";
    default:
      return "bg-primary text-light";
  }
}

function dialogTextClass(style?: MessageModalStyle) {

  return undefined;

  //switch (style) {
  //    case "success":
  //        return "text-success";
  //    case "info":
  //        return "text-info";
  //    case "warning":
  //        return "text-warning";
  //    case "error":
  //        return "text-danger";
  //    default:
  //        return "text-primary";
  //}
}
