import * as React from 'react'
import * as Modals from '../Modals';
import { Dic } from '../Globals';
import { ajaxPost, ExternalServiceError, ServiceError, ValidationError } from '../Services';
import { JavascriptMessage, FrameMessage, ConnectionMessage } from '../Signum.Entities'
import { ClientErrorModel, ExceptionEntity } from '../Signum.Entities.Basics'
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
  const ese = e instanceof ExternalServiceError ? (e as ExternalServiceError) : undefined;
  const ve = e instanceof ValidationError ? (e as ValidationError) : undefined;

  return (
    <Modal show={show} onExited={handleOnExited} onHide={handleCloseClicked} size="lg">
      <div className="modal-header dialog-header-error">
        <h5 className="modal-title">
          {
            se ? renderServiceTitle(se) :
              ve ? renderValidationTitle(ve) :
                ese ? renderExternalServiceTitle(ese) :
                  renderTitle(e)
          }
        </h5>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCloseClicked}/>
      </div>

      <div className="modal-body">
        {se ? ErrorModalOptions.renderServiceMessage(se) :
          ve ? ErrorModalOptions.renderValidationMessage(ve) :
            ese ? ErrorModalOptions.renderExternalServiceMessage(ese) :
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
      <span><FontAwesomeIcon icon="triangle-exclamation" /> Error </span>
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

  function renderExternalServiceTitle(ese: ExternalServiceError) {
    return (
      <span>
        <strong>{ese.serviceName}: </strong>
        {ese.title}
      </span>
    );
  }


  function renderValidationTitle(ve: ValidationError) {
    return (
      <span>
        <FontAwesomeIcon icon="triangle-exclamation" /> {FrameMessage.ThereAreErrors.niceToString()}
      </span>
    );
  }
}



var lastError: { model: ClientErrorModel, date: Date } | undefined;

function logError(error: Error) {

  if (error instanceof ServiceError || error instanceof ValidationError)
    return; 

  if (error instanceof DOMException && error.name == "AbortError")
    return;

  var errorModel = ClientErrorModel.New({
    errorType: (error as Object).constructor.name,
    message: error.message || error.toString(),
    stack: error.stack ?? null,
    name: error.name,
  });

  var date = new Date();

  if (lastError != null) {
    if (
      lastError.model.errorType == errorModel.errorType &&
      lastError.model.message == errorModel.message &&
      lastError.model.stack == errorModel.stack &&
      lastError.model.errorType == errorModel.errorType &&
      ((date.valueOf() - lastError.date.valueOf()) / 1000) < 10 
    ) {
      return;
    }
  } 

  lastError = { model: errorModel, date: date };
  ajaxPost({ url: "~/api/registerClientError" }, errorModel)
    .catch(e => {
      console.error("Unable to save client-side error:", error, e);
    });
}

ErrorModal.register = () => {
  window.onunhandledrejection = p => {
    var error = p.reason;
    logError(error);
    if (Modals.isStarted()) {
      ErrorModal.showErrorModal(error);
    }
    else {
      window.onerror?.("error", undefined, undefined, undefined, error);
      console.error("Unhandled promise rejection:", error);
    }
  };

  var oldOnError = window.onerror;

  window.onerror = (message: Event | string, filename?: string, lineno?: number, colno?: number, error?: Error) => {

    if (error != null)
      logError(error);

    if (Modals.isStarted()) 
      ErrorModal.showErrorModal(error);
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
          <button className="btn btn-warning" onClick={e => { e.preventDefault(); window.location.reload(); }}>
            <FontAwesomeIcon icon="rotate" />
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

export function RenderExternalServiceMessageDefault(p: { error: ExternalServiceError }) {

  const [showDetails, setShowDetails] = React.useState(false);

  function handleShowDetails(e: React.MouseEvent<any>) {
    e.preventDefault();
    setShowDetails(!showDetails);
  }

  return (
    <div>
      {textDanger(p.error.message)}
      {p.error.additionalInfo && ErrorModalOptions.isExceptionViewable() &&
        <div>
          <a href="#" onClick={handleShowDetails}>StackTrace</a>
          {showDetails && <pre>{p.error.additionalInfo}</pre>}
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
      {textDanger(e.message ? e.message : e.toString())}
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
    return <RenderServiceMessageDefault error={se} />;
  }

  export function renderExternalServiceMessage(ese: ExternalServiceError): React.ReactNode {
    return <RenderExternalServiceMessageDefault error={ese} />;
  }

  export function renderValidationMessage(ve: ValidationError): React.ReactNode {
    return <RenderValidationMessageDefault error={ve} />;
  }

  export function renderMessage(e: any): React.ReactNode {
    return <RenderMessageDefault error={e} />;
  }
}
