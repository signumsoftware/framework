import * as React from 'react'
import * as Modals from '../Modals';
import { Dic } from '../Globals';
import { ServiceError, ValidationError } from '../Services';
import { JavascriptMessage, NormalWindowMessage, ConnectionMessage } from '../Signum.Entities'
import { ExceptionEntity } from '../Signum.Entities.Basics'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import "./Modals.css"
import { newLite } from '../Reflection';
import { Modal } from 'react-bootstrap';
import MessageModal from './MessageModal';
import { namespace } from 'd3';

//http://codepen.io/m-e-conroy/pen/ALsdF
interface ErrorModalProps extends Modals.IModalProps<undefined> {
  error: any;
}

export default function ErrorModal(p: ErrorModalProps) {

  const [show, setShow] = React.useState(true);

  function handleOnExited() {
    p.onExited!(undefined);
  }

  function handleCloseClicked() {
    setShow(false);
  }


  const e = p.error;

  const se = e instanceof ServiceError ? (e as ServiceError) : undefined;
  const ve = e instanceof ValidationError ? (e as ValidationError) : undefined;

  return (
    <Modal show={show} onExited={handleOnExited} onHide={handleCloseClicked} size="lg">
      <div className="modal-header dialog-header-error text-danger">
        <h5 className="modal-title"> {se ? renderServiceTitle(se) :
          ve ? renderValidationTitle(ve) :
            renderTitle(e)}</h5>
        <button type="button" className="close" data-dismiss="modal" aria-label="Close" onClick={handleCloseClicked}>
          <span aria-hidden="true">&times;</span>
        </button>
      </div>

      <div className="modal-body">
        {se ? ErrorModalOptions.renderServiceMessage(se) :
          ve ? ErrorModalOptions.renderValidationMessage(ve) :
            ErrorModalOptions.renderMessage(e)}
      </div>

      <div className="modal-footer">
        <button className="btn btn-primary sf-close-button sf-ok-button" onClick={handleCloseClicked}>
          {JavascriptMessage.ok.niceToString()}</button>
      </div>
    </Modal>
  );

  function renderTitle(e: any) {
    return (
      <span><FontAwesomeIcon icon="exclamation-triangle" /> Error </span>
    );
  }

  function renderServiceTitle(se: ServiceError) {
    return (
      <span>
        <FontAwesomeIcon icon={se.defaultIcon} />&nbsp; <span>{se.httpError.exceptionType}</span>
        {se.httpError.exceptionId && <span>({
          ErrorModalOptions.isExceptionViewable() ?
            <a href={ErrorModalOptions.getExceptionUrl(se.httpError.exceptionId!)}>{se.httpError.exceptionId}</a> :
            <strong>{se.httpError.exceptionId}</strong>
        })</span>}
      </span>
    );
  }


  function renderValidationTitle(ve: ValidationError) {
    return (
      <span>
        <FontAwesomeIcon icon="exclamation-triangle" /> {NormalWindowMessage.ThereAreErrors.niceToString()}
      </span>
    );
  }
}

ErrorModal.register = () => {

  var oldOnError = window.onerror;
  window.onerror = (message: Event | string, filename?: string, lineno?: number, colno?: number, error?: Error) => {

    if (Modals.isStarted())
      ErrorModal.showErrorModal(error).done();
    else if (oldOnError != null) {
      if (error instanceof ServiceError)
        oldOnError(message, filename, lineno, colno, {
          name: error.httpError.exceptionType! + " (ExceptionID " + error.httpError.exceptionId! + ")",
          message: error.httpError.exceptionMessage!,
          stack: error.httpError.stackTrace!,
        });
      else
        oldOnError(message, filename, lineno, colno, error);
    }
  };
}

ErrorModal.showErrorModal = (error: any): Promise<void> => {
  if (error == null || error.code === 20) //abort
    return Promise.resolve();

  if (new RegExp(/^Loading chunk?/i).test(error.message))
    return MessageModal.show({
      title: ConnectionMessage.OutdatedClientApplication.niceToString(),
      message:
        <div>
          {ConnectionMessage.ANewVersionHasJustBeenDeployedConsiderReload.niceToString()}&nbsp;
          <button className="btn btn-warning" onClick={e => { e.preventDefault(); window.location.reload(true); }}>
            <FontAwesomeIcon icon="sync-alt" />
          </button>
        </div>,
      buttons: "cancel",
      style: "warning"
    }).then(() => undefined);

  return Modals.openModal<void>(<ErrorModal error={error} />);
}

function textDanger(message: string | null | undefined): React.ReactFragment | null | undefined {

  if (typeof message == "string")
    return message.split("\n").map((s, i) => <p key={i} className="text-danger">{s}</p>);

  return message;
}

export function RenderServiceMessageDefault(p: { error: ServiceError }) {

  const [showDetails, setShowDetails] = React.useState(false);

  function handleShowStackTrace(e: React.MouseEvent<any>) {
    e.preventDefault();
    setShowDetails(!showDetails);
  }

  return (
    <div>
      {textDanger(p.error.httpError.exceptionMessage)}
      {p.error.httpError.stackTrace && ErrorModalOptions.isExceptionViewable() &&
        <div>
          <a href="#" onClick={handleShowStackTrace}>StackTrace</a>
          {showDetails && <pre>{p.error.httpError.stackTrace}</pre>}
        </div>}
    </div>
  );
}

export function RenderValidationMessageDefault(p: { error: ValidationError }) {
  return (
    <div>
      {textDanger(Dic.getValues(p.error.modelState).join("\n"))}
    </div>
  );
}

export function RenderMessageDefault(p: { error: any }) {
  const e = p.error;
  return (
    <div>
      {textDanger(e.message ? e.message : e)}
    </div>
  );
}

export namespace ErrorModalOptions {
  export function getExceptionUrl(exceptionId: number | string): string | undefined {
    return undefined;
  }

  export function isExceptionViewable() {
    return false;
  }

  export function renderServiceMessage(se: ServiceError): React.ReactNode {
    return undefined;
  }

  export function renderValidationMessage(ve: ValidationError): React.ReactNode {
    return undefined;
  }

  export function renderMessage(e: any): React.ReactNode {
    return undefined;
  }
}
