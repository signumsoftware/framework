import * as React from 'react'
import { Modal, ProgressBar } from 'react-bootstrap';
import { useState } from 'react';
import { useForceUpdate, useThrottle } from '@framework/Hooks';
import { IModalProps, openModal } from '@framework/Modals';
import { jsonObjectStream } from '@framework/Operations/jsonObjectStream';
import { JavascriptMessage, liteKey } from '@framework/Signum.Entities';
import { ImportFromExcelReport, ImportResult } from './ExcelClient';
import { ImportFromExcelMessage } from './Signum.Entities.Excel';
import { TypeInfo } from '@framework/Reflection';




interface ImportExcelProgressModalProps extends IModalProps<ImportFromExcelReport> {
  typeInfo: TypeInfo;

  makeRequest: () => Promise<Response>;
  abortController: AbortController;
}

export function ImportExcelProgressModal(p: ImportExcelProgressModalProps) {

  const [show, setShow] = React.useState(true);
  const forceUpdate = useForceUpdate();
  const importResultsRef = React.useRef([] as ImportResult[]);
  const errorRef = React.useRef(null as any);


  const [requestStarted, setRequestStarted] = React.useState<boolean>(false)

  const oldReuestStarted = useThrottle(requestStarted, 1000);

  async function consumeReader() {
    setRequestStarted(true);
    var resp = await p.makeRequest();

    var generator = jsonObjectStream<ImportResult>(resp.body!.getReader());
    for await (const val of generator) {
      importResultsRef.current.push(val);
      forceUpdate();
    }
  }

  React.useEffect(() => {
    consumeReader()
      .catch(error => {
        errorRef.current = error;
      })
      .finally(() => {
      setShow(false);
    })
  }, [])

  function handleCancelClicked() {
    p.abortController.abort();
  }

  function handleOnExited() {
    p.onExited!({ results: importResultsRef.current.map(a => a), error: errorRef.current });
  }

  var errors = importResultsRef.current.filter(a => a.error != null);

  var totalRows = importResultsRef.current[0]?.totalRows;

  return (
    <Modal show={show} className="message-modal" backdrop="static" onExited={handleOnExited}>
      <div className="modal-header">
        <h5 className="modal-title">
          {ImportFromExcelMessage.Importing0.niceToString(p.typeInfo.nicePluralName)}
        </h5>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCancelClicked} />
      </div>
      <div className="modal-body">
        <p><strong>{totalRows}</strong> {totalRows == 1 ? p.typeInfo.niceName :  p.typeInfo.nicePluralName}</p>
        {importResultsRef.current.length == 0 && oldReuestStarted ?
          <ProgressBar now={100} variant="info" animated striped key={1} /> :
          <ProgressBar min={0} max={totalRows} now={importResultsRef.current.length}
            label={`[${importResultsRef.current.length}/${totalRows}]`} key={2}/>
        }
        {errors.length > 0 && <p className="text-danger">{ImportFromExcelMessage._0Errors.niceToString().forGenderAndNumber(errors.length).formatHtml(<strong>{errors.length}</strong>)}</p>}

      </div>
      <div className="modal-footer">
        <button className="btn btn-light sf-entity-button sf-close-button" onClick={handleCancelClicked}>
          {JavascriptMessage.cancel.niceToString()}
        </button>
      </div>
    </Modal>
  );
}

ImportExcelProgressModal.show = (abortController: AbortController, typeInfo: TypeInfo, makeRequest: () => Promise<Response>): Promise<ImportFromExcelReport> => {
  return openModal<ImportFromExcelReport>(<ImportExcelProgressModal makeRequest={makeRequest} abortController={abortController} typeInfo={typeInfo} />);
};
