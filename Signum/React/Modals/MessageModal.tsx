import * as React from 'react'
import { openModal, IModalProps } from '../Modals';
import { classes } from '../Globals';
import { JavascriptMessage, BooleanEnum } from '../Signum.Entities'
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import "./Modals.css"
import { BsSize } from '../Components';
import { Modal } from 'react-bootstrap';
import { AutoFocus } from '../Components/AutoFocus';

export type MessageModalStyle = "success" | "info" | "warning" | "error";

export type MessageModalIcon = "info" | "question" | "warning" | "error" | "success";

export type MessageModalButtons = "ok" | "cancel" | "ok_cancel" | "yes_no" | "yes_cancel" | "yes_no_cancel";

export type MessageModalResult = "ok" | "cancel" | "yes" | "no";

export interface MessageModalHandler {
  handleButtonClicked(m: MessageModalResult): void;
}

interface MessageModalProps extends IModalProps<MessageModalResult | undefined> {
  title: string | React.ReactElement;
  message: string | React.ReactElement | ((ctx: MessageModalHandler) => string | React.ReactElement);
  style?: MessageModalStyle;
  buttons: MessageModalButtons;
  buttonContent?: (button: MessageModalResult) => string | React.ReactElement | null | undefined;
  buttonHtmlAttributes?: (button: MessageModalResult) => React.ButtonHTMLAttributes<any> | null | undefined;
  buttonClass?: (button: MessageModalResult) => string | undefined;
  onButtonClicked?: (button: MessageModalResult) => void;
  icon?: MessageModalIcon | null;
  customIcon?: IconProp;
  size?: BsSize;
  shouldSelect?: boolean;
  additionalDialogClassName?: string;
  modalRef?: React.RefObject<MessageModalHandler | null>; //For closing the modal imperatively
  autoFocusonTitle?: boolean;
}


function MessageModal(p: MessageModalProps): React.ReactElement {

  const [show, setShow] = React.useState(true);

  const selectedValue = React.useRef<MessageModalResult | undefined>(undefined);

  React.useImperativeHandle(p.modalRef, () => {

    return {
      handleButtonClicked
    }
  }, []);

  function handleButtonClicked(val: MessageModalResult) {
    selectedValue.current = val;
    setShow(false);
  }

  function handleCancelClicked() {
    if (p.shouldSelect && !selectedValue)
      return;

    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(selectedValue.current);
  }

  function getButtonContent(button: MessageModalResult) {
    const content = p.buttonContent && p.buttonContent(button);
    if (content)
      return content

    switch (button) {
      case "ok": return JavascriptMessage.ok.niceToString();
      case "cancel": return JavascriptMessage.cancel.niceToString();
      case "yes": return BooleanEnum.niceToString("True");
      case "no": return BooleanEnum.niceToString("False");
    }
  }

  function setFocus(e: HTMLButtonElement | null) {
    if (e) {
      window.setTimeout(() => e.focus(), 200);
    }
  }

  function getButton(res: MessageModalResult) {

    const htmlAtts = p.buttonHtmlAttributes && p.buttonHtmlAttributes(res);

    const baseButtonClass = classes("btn", p.buttonClass ? p.buttonClass(res) : (res == 'yes' || res == 'ok' ? "btn-primary" : "btn-secondary"), `sf-close-button sf-${res}-button ms-1`)

    return (
      <button
        {...htmlAtts}
        ref={((res == 'yes' || res == 'ok') && !p.autoFocusonTitle) ? setFocus : undefined}
        className={classes(htmlAtts?.className, baseButtonClass)}
        onClick={() => {
          if (p.onButtonClicked)
            p.onButtonClicked(res);
          else
            handleButtonClicked(res);
        }}
        name={res}>
        {getButtonContent(res)}
      </button>
    );
  }

  function renderButtons(buttons: MessageModalButtons) {
    switch (buttons) {
      case "ok": return getButton('ok');
      case "cancel": return getButton('cancel');
      case "ok_cancel": return (<div className="btn-toolbar"> {getButton('ok')} {getButton('cancel')} </div>);
      case "yes_no": return (<div className="btn-toolbar"> {getButton('yes')} {getButton('no')} </div>);
      case "yes_cancel": return (<div className="btn-toolbar"> {getButton('yes')} {getButton('cancel')} </div>);
      case "yes_no_cancel": return (<div className="btn-toolbar"> {getButton('yes')} {getButton('no')} {getButton('cancel')} </div>);
    }
  }

  function getIcon(): IconProp | undefined {

    if (p.customIcon)
      return p.customIcon;


    if (p.icon) {
      switch (p.icon) {
        case "info": return "info-circle";
        case "error": return "exclamation-circle";
        case "question": return "question-circle";
        case "success": return "check-circle";
        case "warning": return "exclamation-triangle";
        case null: return undefined;
      }
    }

    if (p.style) {
      switch (p.style) {
        case "info": return "info-circle";
        case "error": return "exclamation-circle";
        //case "question": return "question-circle";
        case "success": return "check-circle";
        case "warning": return "exclamation-triangle";
      }
    }

    return undefined;
  }

  function renderTitle() {
    var icon = getIcon();

    var iconSpan = icon && <FontAwesomeIcon aria-hidden={true} icon={icon} />;

    const titleRef = React.useRef<HTMLHeadingElement>(null);

    React.useEffect(() => {
      if (p.autoFocusonTitle && titleRef.current) {
        setTimeout(() => titleRef.current?.focus(), 200);
      }
    }, [p.autoFocusonTitle]);  

    return (
      <h5 ref={titleRef} tabIndex={0} className="modal-title">
        {iconSpan}{iconSpan && <span>&nbsp;&nbsp;</span>}{p.title}
      </h5>
    );
  }

  return (
    <Modal show={show} onExited={handleOnExited} backdrop={p.shouldSelect ? 'static' : undefined}
      dialogClassName={classes("message-modal", p.size && "modal-" + p.size, p.additionalDialogClassName)}
      onHide={handleCancelClicked} autoFocus={true}>
      <div className={classes("modal-header", dialogHeaderClass(p.style))}>
        {renderTitle()}
      </div>
      <div className="modal-body">
        {
          typeof p.message == "string" ? p.message.split("\n").map((line, i) => <p key={i}>{line}</p>) :
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

namespace MessageModal {
  export function show(options: MessageModalProps): Promise<MessageModalResult> {
    return openModal<MessageModalResult>(<MessageModal {...options} />);
  }

  export function showError(message: React.ReactElement | string, title?: string): Promise<undefined> {
    return MessageModal.show({ buttons: "ok", icon: "error", style: "error", title: title ?? JavascriptMessage.error.niceToString(), message: message })
      .then(() => undefined);
  }
}

export default MessageModal;

function dialogHeaderClass(style: MessageModalStyle | undefined) {
  switch (style) {
    case "success": return "dialog-header-success";
    case "info": return "dialog-header-info";
    case "warning": return "dialog-header-warning";
    case "error": return "dialog-header-error";
    default: return "bg-primary text-light";
  }
}

