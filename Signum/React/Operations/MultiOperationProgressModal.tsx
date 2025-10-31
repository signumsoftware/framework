import * as React from 'react'
import { openModal, IModalProps } from '../Modals';
import { Operations } from '../Operations';
import { Modal, ProgressBar } from 'react-bootstrap';
import { Entity, EntityControlMessage, JavascriptMessage, Lite, liteKey, OperationMessage } from '../Signum.Entities';
import { useForceUpdate, useThrottle } from '../Hooks';
import { getOperationInfo, getTypeInfo, OperationInfo } from '../Reflection';
import { useState } from 'react';
import { softCast } from '../Globals';
import { jsonObjectStream } from './jsonObjectStream';
import { CollectionMessage } from '../Signum.External';
import { OperationSymbol } from '../Signum.Operations';


interface MultiOperationProgressModalProps extends IModalProps<Operations.API.ErrorReport> {
  operation: OperationInfo;
  lites: Lite<Entity>[];
  makeRequest: () => Promise<Response>;
  abortController: AbortController;
}

export function MultiOperationProgressModal(p: MultiOperationProgressModalProps): React.ReactElement {

  const [show, setShow] = React.useState(true);
  const forceUpdate = useForceUpdate();
  const operationResultsRef = React.useRef([] as Operations.API.OperationResult[]);


  const [requestStarted, setRequestStarted] = React.useState<boolean>(false)

  const oldReuestStarted = useThrottle(requestStarted, 1000);

  async function consumeReader() {
    setRequestStarted(true);
    var resp = await p.makeRequest();

    var generator = jsonObjectStream<Operations.API.OperationResult>(resp.body!.getReader());
    for await (const val of generator) {
      operationResultsRef.current.push(val);
      forceUpdate();
    }
  }

  var typeNiceName = React.useMemo(() =>
    p.lites.map(a => a.EntityType).distinctBy(a => a).map(a => p.lites.length == 1 ? getTypeInfo(a).niceName : getTypeInfo(a).nicePluralName).joinComma(CollectionMessage.And.niceToString()), [p.lites]);

  React.useEffect(() => {
    consumeReader().finally(() => {
      setShow(false);
    })
  }, [])

  function handleCancelClicked() {
    p.abortController.abort();
  }

  function handleOnExited() {
    p.onExited!({ errors: operationResultsRef.current.toObject(a => liteKey(a.entity), a => a.error) });
  }

  var errors = operationResultsRef.current.filter(a => a.error != null);

  return (
    <Modal show={show} className="message-modal" backdrop="static" onExited={handleOnExited}>
      <div className="modal-header">
        <h5 className="modal-title">{
          p.operation.operationType == "Delete" ? OperationMessage.Deleting.niceToString() :
            p.operation.operationType == "ConstructorFrom" ? p.operation.niceName :
            OperationMessage.Executing0.niceToString(p.operation.niceName)}</h5>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label={EntityControlMessage.Close.niceToString()} onClick={handleCancelClicked} />
      </div>
      <div className="modal-body">
        <p><strong>{p.lites.length}</strong> {typeNiceName}</p>
        {operationResultsRef.current.length == 0 && oldReuestStarted ?
          <ProgressBar now={100} variant="info" animated striped key={1} /> :
          <ProgressBar min={0} max={p.lites.length} now={operationResultsRef.current.length}
            label={`[${operationResultsRef.current.length}/${p.lites.length}]`} key={2}/>
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

export namespace MultiOperationProgressModal {
  export function show(lites: Lite<Entity>[], operationKey: string | OperationSymbol, progressModal: boolean | undefined, abortController: AbortController, makeRequest: () => Promise<Response>): Promise<Operations.API.ErrorReport> {

    if (progressModal ?? lites.length > 1) {
      var oi = getOperationInfo(operationKey, lites[0].EntityType);
      return openModal<Operations.API.ErrorReport>(<MultiOperationProgressModal operation={oi} lites={lites} makeRequest={makeRequest} abortController={abortController} />);
    } else {
      return makeRequest().then(r => r.json()).then(obj => {
        var a = obj as Operations.API.OperationResult;
        return softCast<Operations.API.ErrorReport>({ errors: { [liteKey(a.entity)]: a.error } });
      });
    }
  };
}
