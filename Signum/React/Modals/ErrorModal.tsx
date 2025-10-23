import * as React from 'react'
import { Link } from 'react-router-dom'
import * as Modals from '../Modals';
import { Dic } from '../Globals';
import { ajaxPost, ExternalServiceError, ServiceError, ValidationError } from '../Services';
import { JavascriptMessage, FrameMessage, ConnectionMessage, EntityControlMessage } from '../Signum.Entities'
import { ClientErrorModel, ExceptionEntity } from '../Signum.Basics'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import "./Modals.css"
import { newLite } from '../Reflection';
import { Modal } from 'react-bootstrap';
import MessageModal from './MessageModal';
import { namespace } from 'd3';
import { LinkButton } from '../Basics/LinkButton';

//http://codepen.io/m-e-conroy/pen/ALsdF
interface ErrorModalProps extends Modals.IModalProps<undefined> {
  error: any;
  beforeOkClicked?:()=> Promise<void>
}

const ErrorModal: {
  (p: ErrorModalProps): React.ReactElement;
  register: () => void;
  showErrorModal: (error: any, beforeOkClicked?: ()=> Promise<void>) => Promise<void>;
} = function (p: ErrorModalProps) {

  const [show, setShow] = React.useState(true);

  function handleOnExited() {
    p.onExited!(undefined);
  }

  async function handleCloseClicked() {
    await p.beforeOkClicked?.();

    setShow(false);
  }


  const e = p.error;

  const se = e instanceof ServiceError ? (e as ServiceError) : undefined;
  const ese = e instanceof ExternalServiceError ? (e as ExternalServiceError) : undefined;
  const ve = e instanceof ValidationError ? (e as ValidationError) : undefined;

  return (
    <Modal show={show} onExited={handleOnExited} onHide={handleCloseClicked} size="lg" dialogClassName="error-modal">
      <div className="modal-header dialog-header-error">
        <h5 className="modal-title">
          {
            se ? renderServiceTitle(se) :
              ve ? renderValidationTitle(ve) :
                ese ? renderExternalServiceTitle(ese) :
                  renderTitle(e)
          }
        </h5>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCloseClicked} />
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
      <span><FontAwesomeIcon aria-hidden={true} icon="triangle-exclamation" /> Error </span>
    );
  }

  function renderServiceTitle(se: ServiceError) {
    return (
      <span>
        <FontAwesomeIcon aria-hidden={true} icon={se.defaultIcon} />&nbsp; <span>{se.httpError.exceptionType}</span>
        {se.httpError.exceptionId && <span>({
          ErrorModalOptions.isExceptionViewable() ?
            <Link to={ErrorModalOptions.getExceptionUrl(se.httpError.exceptionId!)!}>{se.httpError.exceptionId}</Link> :
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
        <FontAwesomeIcon aria-hidden={true} icon="triangle-exclamation" /> {FrameMessage.ThereAreErrors.niceToString()}
      </span>
    );
  }
}

export default ErrorModal;

var lastError: { model: ClientErrorModel, date: Date } | undefined;

function logError(error: Error) {

  if (error instanceof ServiceError || error instanceof ValidationError)
    return; 

  if (error instanceof DOMException && error.name == "AbortError")
    return;

  var errorModel = ClientErrorModel.New({
    url: (error as any)?.url,
    errorType: (error as Object).constructor.name,
    message: error.message || error.toString(),
    stack: error.stack ?? null,
    name: error.name,
  });

  var date = new Date();

  if (lastError != null) {
    if (
      lastError.model.url == errorModel.url &&
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
  ajaxPost({ url: "/api/registerClientError" }, errorModel)
    .catch(e => {
      console.error("Unable to save client-side error:", error, e);
    });
}

ErrorModal.register = () => {
  window.onunhandledrejection = p => {
    var error = p.reason;

    if (error.alreadyConsumed)
      return;

    error.alreadyConsumed = true;

    logError(error);
    if (Modals.isStarted()) {
      ErrorModal.showErrorModal(error);
    }
    else {
      error.alreadyConsumed = false;
      window.onerror?.("error", undefined, undefined, undefined, error);
      console.error("Unhandled promise rejection:", error);
    }
  };

  var oldOnError = window.onerror;

  window.onerror = (message: Event | string, filename?: string, lineno?: number, colno?: number, error?: Error) => {

    if (error != null) {
      if ((error as any).alreadyConsumed)
        return;

      (error as any).alreadyConsumed = true;
    }

    if (error != null)
      logError(error);

    if (Modals.isStarted()) {
      console.error(error);
      ErrorModal.showErrorModal(error);
    }
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

ErrorModal.showErrorModal = (error: any, beforeOkClicked?: ()=> Promise<void>): Promise<void> => {
  if (error == null || error.code === 20) //abort
    return Promise.resolve();

  if (new RegExp(/^Loading chunk?/i).test(error.message))
    return MessageModal.show({
      title: ConnectionMessage.OutdatedClientApplication.niceToString(),
      message:
        <div>
          {ConnectionMessage.ANewVersionHasJustBeenDeployedConsiderReload.niceToString()}&nbsp;
          <button className="btn btn-warning" onClick={e => { e.preventDefault(); window.location.reload(); }} title={EntityControlMessage.Reload.niceToString()}>
            <FontAwesomeIcon aria-hidden={true} icon="rotate" />
          </button>
        </div>,
      buttons: "cancel",
      style: "warning"
    }).then(() => undefined);

  return Modals.openModal<void>(<ErrorModal error={error} beforeOkClicked={beforeOkClicked} />);
}

function textDanger(message: string | null | undefined): React.ReactNode {

  if (typeof message == "string")
    return message.split("\n").map((s, i) => <p key={i} className="text-danger">{s}</p>);

  return message;
}

export function RenderServiceMessageDefault(p: { error: ServiceError }): React.ReactElement {

  const [showDetails, setShowDetails] = React.useState(false);

  function handleShowStackTrace(e: React.MouseEvent<any>) {
    e.preventDefault();
    setShowDetails(!showDetails);
  }

  return (
    <div>

      {ErrorModalOptions.preferPreFormated(p.error) ? <pre style={{ whiteSpace: "pre-wrap" }}>{p.error.httpError.exceptionMessage}</pre> : textDanger(p.error.httpError.exceptionMessage)}
      {p.error.httpError.stackTrace && ErrorModalOptions.isExceptionViewable() &&
        <div>
          <LinkButton title={undefined} onClick={handleShowStackTrace}>StackTrace</LinkButton>
          {showDetails && <pre>{p.error.httpError.stackTrace}</pre>}
        </div>}
    </div>
  );
}

export function RenderExternalServiceMessageDefault(p: { error: ExternalServiceError }): React.ReactElement {

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
          <LinkButton title={undefined} onClick={handleShowDetails}>StackTrace</LinkButton>
          {showDetails && <pre>{p.error.additionalInfo}</pre>}
        </div>}
    </div>
  );
}

export function RenderValidationMessageDefault(p: { error: ValidationError }): React.ReactElement {
  return (
    <div>
      {textDanger(Dic.getValues(p.error.modelState).join("\n"))}
    </div>
  );
}

export function RenderMessageDefault(p: { error: any }): React.ReactElement {
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

  export function isExceptionViewable(): boolean {
    return false;
  }

  export function preferPreFormated(se: ServiceError): boolean {
    return se.httpError.exceptionType.contains("FieldReaderException");
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
