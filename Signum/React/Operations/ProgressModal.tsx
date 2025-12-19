import * as React from 'react'
import { openModal, IModalProps } from '../Modals';
import { Operations } from '../Operations';
import { Modal, ProgressBar } from 'react-bootstrap';
import { Entity, EntityControlMessage, JavascriptMessage, Lite, liteKey, OperationMessage } from '../Signum.Entities';
import { useForceUpdate, useThrottle } from '../Hooks';
import { getTypeInfo, OperationInfo } from '../Reflection';
import { useState } from 'react';
import { softCast } from '../Globals';
import { jsonObjectStream } from './jsonObjectStream';
import { ServiceError } from '../Services';
import MessageModal from '../Modals/MessageModal';
import ErrorModal from '../Modals/ErrorModal';


interface ProgressModalProps<T> extends IModalProps<Operations.API.ProgressStep<T> | undefined> {
  abortController: AbortController;
  options: ProgressModalOptions;
  makeRequest: () => Promise<Response>;
}

export function ProgressModal<T>(p: ProgressModalProps<T>): React.ReactElement {

  const [show, setShow] = React.useState(true);
  const forceUpdate = useForceUpdate();
  const lastStepRef = React.useRef<Operations.API.ProgressStep<T> | undefined>(undefined);


  const [requestStarted, setRequestStarted] = React.useState<boolean>(false)

  const oldRequestStarted = useThrottle(requestStarted, 1000);

  async function consumeReader() {
    setRequestStarted(true);
    var resp = await p.makeRequest();

    var generator = jsonObjectStream<Operations.API.ProgressStep<T>>(resp.body!.getReader());
    for await (const val of generator) {
      lastStepRef.current = val;
      forceUpdate();
    }
  }

  React.useEffect(() => {
    consumeReader()
      .then(() => setShow(false),
        e => ErrorModal.showErrorModal(e).then(() => setShow(false)));
  }, []);

  async function handleCancelClicked() {

    if (p.options.showCloseWarningMessage) {
      var btn = await MessageModal.show({
        message: OperationMessage.AreYouSureYouWantToCancelTheOperation.niceToString(),
        title: OperationMessage.CancelOperation.niceToString(),
        buttons: "yes_no",
        style: "warning"
      });

      if (btn == "no")
        return;
    }

    p.abortController.abort();
  }

  function handleOnExited() {
    p.onExited!(p.abortController.signal.aborted ? undefined : lastStepRef.current);
  }


  const step = lastStepRef.current;
  return (
    <Modal show={show} className="message-modal" backdrop="static" onExited={handleOnExited}>
      <div className="modal-header">
        <h1 className="modal-title h5">{p.options.title}</h1>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label={EntityControlMessage.Close.niceToString()} onClick={handleCancelClicked} />
      </div>
      <div className="modal-body">
        <p>{p.options.message}</p>
        {step == undefined || step.position == null || step.position == -1 || step.max == null || step.min == null ?
          <ProgressBar now={100} variant="info" animated={oldRequestStarted} striped={oldRequestStarted} key={1} /> :
          <ProgressBar min={step.min ?? 0} max={step.max ?? 100} now={step.position ?? 0}
            label={step.min != 0 ?
              `[${step.position}/${step.min}...${step.min}]` :
              `[${step.position}/${step.max}]`}
            key={2} />
        }
        {step?.currentTask && <p className="text-success">{step.currentTask}</p>}
      </div>
      {p.options.showCloseWarningMessage &&
        <div className="modal-footer">
          <small className="text-muted">{OperationMessage.ClosingThisModalOrBrowserTabWillCancelTheOperation.niceToString()}</small>
          <button type="button" className="btn btn-tertiary sf-entity-button sf-close-button" onClick={handleCancelClicked}>
            {JavascriptMessage.cancel.niceToString()}
          </button>
        </div>
      }
    </Modal>
  );
}

export namespace ProgressModal {
  export function show<T>(abortController: AbortController, modalOptions: ProgressModalOptions | undefined, makeRequest: () => Promise<Response>): Promise<T> {

    if (modalOptions) {

      return openModal<Operations.API.ProgressStep<T> | undefined>(<ProgressModal options={modalOptions} makeRequest={makeRequest} abortController={abortController} />)
        .then(r => {
          if (r == null)
            throw new Error("Operation cancelled");

          if (r.error)
            throw new ServiceError(r.error);
          return r.result!;
        });

    } else {

      return makeRequest().then(r => r.json()).then(obj => {
        var results = obj as Operations.API.ProgressStep<T>[];
        var last = results.last();
        if (last.error)
          throw new ServiceError(last.error);
        return last.result!;
      });

    }
  };
}


export interface ProgressModalOptions {
  title: React.ReactNode;
  message: React.ReactNode;
  showCloseWarningMessage: boolean;
}
