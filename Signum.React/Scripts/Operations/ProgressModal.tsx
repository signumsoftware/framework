import * as React from 'react'
import { openModal, IModalProps } from '../Modals';
import { API, getOperationInfo } from '../Operations';
import { Modal, ProgressBar } from 'react-bootstrap';
import { Entity, External, JavascriptMessage, Lite, liteKey, OperationMessage, OperationSymbol } from '../Signum.Entities';
import { useForceUpdate, useThrottle } from '../Hooks';
import { getTypeInfo, OperationInfo } from '../Reflection';
import { useState } from 'react';
import { softCast } from '../Globals';


interface ProgressModalProps extends IModalProps<API.ErrorReport> {
  operation: OperationInfo;
  lites: Lite<Entity>[];
  makeRequest: () => Promise<Response>;
  abortController: AbortController;
}

export function ProgressModal(p: ProgressModalProps) {

  const [show, setShow] = React.useState(true);
  const forceUpdate = useForceUpdate();
  const operationResultsRef = React.useRef([] as API.OperationResult[]);


  const [requestStarted, setRequestStarted] = React.useState<boolean>(false)

  const oldReuestStarted = useThrottle(requestStarted, 1000);

  async function consumeReader() {
    setRequestStarted(true);
    var resp = await p.makeRequest();

    var generator = jsonObjectStream<API.OperationResult>(resp.body!.getReader());
    for await (const val of generator) {
      operationResultsRef.current.push(val);
      forceUpdate();
    }
  }

  var typeNiceName = React.useMemo(() =>
    p.lites.map(a => a.EntityType).distinctBy(a => a).map(a => p.lites.length == 1 ? getTypeInfo(a).niceName : getTypeInfo(a).nicePluralName).joinComma(External.CollectionMessage.And.niceToString()), [p.lites]);

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
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCancelClicked} />
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
        <button className="btn bg-light sf-entity-button sf-close-button" onClick={handleCancelClicked}>
          {JavascriptMessage.cancel.niceToString()}
        </button>
      </div>
    </Modal>
  );
}

ProgressModal.showIfNecessary = (lites: Lite<Entity>[], operationKey: string | OperationSymbol, progressModal: boolean | undefined, abortController: AbortController, makeRequest: ()=> Promise<Response>): Promise<API.ErrorReport> => {

  if (progressModal ?? lites.length > 1) {
    var oi = getOperationInfo(operationKey, lites[0].EntityType);
    return openModal<API.ErrorReport>(<ProgressModal operation={oi} lites={lites} makeRequest={makeRequest} abortController={abortController} />);
  } else {
    return makeRequest().then(r => r.json()).then(obj => {
      var results = obj as API.OperationResult[];
      return softCast<API.ErrorReport>({ errors: results.toObject(a => liteKey(a.entity), a => a.error) });
    });
  }
};

const jsonObjectStream = async function* <T>(reader: ReadableStreamDefaultReader<Uint8Array>): AsyncGenerator<T> {

  const  decoder = new TextDecoder();
  //let totalStr = ""; 
  let str = ""; 
  let isStart = true;

  while (true) {
    debugger;
    var pair = await reader.read();
    if (pair.done)
      return;

    var newPart = decoder.decode(pair.value);
    //totalStr += newPart;
    str += newPart;

    if (isStart) {
      const index = consumeSpaces(str, 0);

      if (index == null)
        continue; 

      if (str[index] != "[")
        throw new Error("Start of array not found");

      str = str.substring(index + 1);

      isStart = false;
    }



    while (true) { //Invariatn \s*{object}\s),
      const index = consumeSpaces(str, 0);

      if (index == null)
        break;

      if (str[index] == ']')
        return;

      const index2 = consumeObject(str, index);

      if (index2 == null)
        break;

      var objStr = str.substring(index, index2);

      const index3 = consumeSpaces(str, index2);
      if (index3 == null)
        break;

      var terminator = str[index3];
      if (terminator != "]" && terminator != ",")
        throw new Error("List separator not found");

      var obj = JSON.parse(objStr) as T;
      yield obj;

      if (terminator == "]")
        return;
      else //if (terminator == ",")
        str = str.substring(index3 + 1);
    }
  }
};

function consumeSpaces(text: string, startIndex: number) : number | null {

  for (var i = startIndex; i < text.length; i++) {
    var c = text[i];
    if (!(c == " " || c == "\n" || c == "\r" || c == "\t"))
      return i;
  }

  return null; 
}

function consumeStringLiteral(text: string, startIndex: number): number | null {
  var lastIsSlash = false;

  for (var i = startIndex + 1; i < text.length; i++) {
    var c = text[i];

    if (c == "\"" && !lastIsSlash)
      return i + 1;

    lastIsSlash = c == "\\";
  }

  return null;
}

function consumeObject(str: string, startIndex: number): number  | null{
  var level = 0;

  if (str[startIndex] != "{")
    throw new Error("Start of object not found");

  for (var i = startIndex; i < str.length; i++) {
    var c = str[i];

    switch (c) {
      case "\"": {
        var newIndex = consumeStringLiteral(str, i);
        if (newIndex == null)
          return null;

        i = newIndex
        break;
      }
      case "{": {
        level++;
        break;
      }
      case "}": {
        level--;
        if (level == 0)
          return i + 1;
        break;
      }
    }
  }

  return null;
}
