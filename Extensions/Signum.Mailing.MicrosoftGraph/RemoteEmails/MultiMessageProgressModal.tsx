import * as React from 'react'
import { openModal, IModalProps } from '@framework/Modals';
import { Operations } from '@framework/Operations';
import { Modal, ProgressBar } from 'react-bootstrap';
import { Entity, EntityControlMessage, JavascriptMessage, Lite, liteKey, OperationMessage } from '@framework/Signum.Entities';
import { useForceUpdate, useThrottle } from '@framework/Hooks';
import { getOperationInfo, getTypeInfo, OperationInfo } from '@framework/Reflection';
import { useState } from 'react';
import { softCast } from '@framework/Globals';
import { jsonObjectStream } from '@framework/Operations/jsonObjectStream';
import { CollectionMessage } from '@framework/Signum.External';
import { OperationSymbol } from '@framework/Signum.Operations';
import ErrorModal from '@framework/Modals/ErrorModal';
import { RemoteEmailMessageMessage } from './Signum.Mailing.MicrosoftGraph.RemoteEmails';
import { EmailResult } from './RemoteEmailsClient';


interface MultiMessageProgressModalProps extends IModalProps<Operations.API.ErrorReport> {
  messages: string[];
  title: string;
  makeRequest: () => Promise<Response>;
  abortController: AbortController;
}

export function MultiMessageProgressModal(p: MultiMessageProgressModalProps): React.ReactElement {

  const [show, setShow] = React.useState(true);
  const forceUpdate = useForceUpdate();
  const messageResultRef = React.useRef([] as EmailResult[]);


  const [requestStarted, setRequestStarted] = React.useState<boolean>(false)

  const oldReuestStarted = useThrottle(requestStarted, 1000);

  async function consumeReader() {
    setRequestStarted(true);
    var resp = await p.makeRequest();

    var generator = jsonObjectStream<EmailResult>(resp.body!.getReader());
    for await (const val of generator) {
      messageResultRef.current.push(val);
      forceUpdate();
    }
  }

  React.useEffect(() => {
    consumeReader()
      .then(() => setShow(false),
        e => ErrorModal.showErrorModal(e).then(() => setShow(false)));
  }, []);

  function handleCancelClicked() {
    p.abortController.abort();
  }

  function handleOnExited() {
    p.onExited!({ errors: messageResultRef.current.toObject(a => a.id, a => a.error) });
  }

  var errors = messageResultRef.current.filter(a => a.error != null);

  return (
    <Modal show={show} className="message-modal" backdrop="static" onExited={handleOnExited}>
      <div className="modal-header">
        <h5 className="modal-title">{p.title}</h5>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label={EntityControlMessage.Close.niceToString()} onClick={handleCancelClicked} />
      </div>
      <div className="modal-body">
        <p><strong>{p.messages.length}</strong> {RemoteEmailMessageMessage.Messages.niceToString()}</p>
        {messageResultRef.current.length == 0 && oldReuestStarted ?
          <ProgressBar now={100} variant="info" animated striped key={1} /> :
          <ProgressBar min={0} max={p.messages.length} now={messageResultRef.current.length}
            label={`[${messageResultRef.current.length}/${p.messages.length}]`} key={2} />
        }
        {errors.length > 0 && <p className="text-danger">{OperationMessage._0Errors.niceToString().forGenderAndNumber(errors.length).formatHtml(<strong>{errors.length}</strong>)}</p>}

      </div>
      <div className="modal-footer">
        <button type="button" className="btn btn-tertiary sf-entity-button sf-close-button" onClick={handleCancelClicked}>
          {JavascriptMessage.cancel.niceToString()}
        </button>
      </div>
    </Modal>
  );
}

export namespace MultiMessageProgressModal {
  export function show(messages: string[], title: string, abortController: AbortController, makeRequest: () => Promise<Response>): Promise<Operations.API.ErrorReport> {
    
    if (messages.length > 1) {
      return openModal<Operations.API.ErrorReport>(<MultiMessageProgressModal messages={messages} title={title} makeRequest={makeRequest} abortController={abortController} />);  
    } else {
      return makeRequest().then(r => r.json()).then(obj => {
        var a = obj as EmailResult;
        return softCast<Operations.API.ErrorReport>({ errors: { [a.id]: a.error } });
      });
    }
  }
}
