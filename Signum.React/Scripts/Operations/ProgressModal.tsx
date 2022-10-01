import * as React from 'react'
import { openModal, IModalProps } from './Modals';
import { Modal } from 'react-bootstrap';
import { isStarted } from '../Modals';


interface ProgressModalProps extends IModalProps<boolean | undefined> {
  text: string;
  response: Response;
}

export function ProgressModal(p: ProgressModalProps) {

  const [show, setShow] = React.useState(true);
  const answerRef = React.useRef<boolean | undefined>(undefined);

  const resultRef = React.useRef([] as OperationResult[]);

  React.useEffect(async () => {
    p.response.body?.getReader().


  }, [])

  function handleOkClicked() {
    answerRef.current = true;
    setShow(false);
  }

  function handleCancelClicked() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(answerRef.current);
  }

  return (
    <Modal onHide={handleCancelClicked} show={show} className="message-modal" onExited={handleOnExited}>
      <div className="modal-header">
        <h5 className="modal-title">Important Question</h5>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCancelClicked} />
      </div>
      <div className="modal-body">
        {p.question}
      </div>
      <div className="modal-footer">
        <button className="btn btn-primary sf-entity-button sf-ok-button" onClick={handleOkClicked}>
          {JavascriptMessage.ok.niceToString()}
        </button>
        <button className="btn btn-light sf-entity-button sf-close-button" onClick={handleCancelClicked}>
          {JavascriptMessage.cancel.niceToString()}
        </button>
      </div>
    </Modal>
  );
}

ProgressModal.show = (question: string): Promise<boolean | undefined> => {
  return openModal<boolean | undefined>(<ProgressModal question={question} />);
};

var startObject = /^\s*/;

const getOperationResults = async function* (reader: ReadableStreamDefaultReader<Uint8Array>) {

  var pair = await reader.read();
  var decoder = new TextDecoder();
  var str = ""; 

  var isStart = true;

  while (!pair.done) {
    str += decoder.decode(pair.value);
    var index = 0;
    if (isStart) {
      if (str.startsWith("[")) {
        str = str.after("[");
      }

      isStart = false;
    }

    var index =  

    while (str.startsWith("{")) {

    }


    str.indent.indexOf("}");



    pair = await reader.read();
    }



};

function findBalancedCloseBrace(text: string) {

}
