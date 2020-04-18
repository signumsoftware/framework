import * as React from 'react'
import { Dic } from '@framework/Globals';
import { openModal, IModalProps } from '@framework/Modals';
import { DynamicViewMessage } from '../Signum.Entities.Dynamic'
import * as NodeUtils from './NodeUtils'
import { BaseNode } from './Nodes'
import { Modal } from 'react-bootstrap';
import { ModalHeaderButtons } from '@framework/Components/ModalHeaderButtons';

export default function NodeSelectorModal(p: IModalProps<any | undefined>) {

  const [show, setShow] = React.useState<boolean>(true);

  const selectedValue = React.useRef<any>(undefined)

  function handleButtonClicked(val: any) {
    selectedValue.current = val;
    setShow(false);
  }

  function handleCancelClicked() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(selectedValue!.current);
  }

  const columnWidth = "200px";

  const nodes = Dic.getValues(NodeUtils.registeredNodes);

  var columns = nodes
    .filter(n => n.group != null)
    .groupBy(n => n.group!)
    .groupsOf(nodes.length / 3, g => g.elements.length);

  return (
    <Modal size="lg" onHide={handleCancelClicked} show={show} onExited={handleOnExited} className="sf-selector-modal">
      <ModalHeaderButtons onClose={handleCancelClicked} >
        {DynamicViewMessage.SelectATypeOfComponent.niceToString()}
      </ModalHeaderButtons>
      <div className="modal-body">
        <div className="row">
          {
            columns.map((c, i) =>
              <div key={i} className={"col-sm-" + (12 / columns.length)}>
                {
                  c.map(gr => <fieldset key={gr.key}>
                    <legend>{gr.key}</legend>
                    {
                      gr.elements.orderBy(n => n.order!).map(n =>
                        <button key={n.kind} type="button" onClick={() => handleButtonClicked(n)}
                          className="sf-chooser-button sf-close-button btn btn-light">
                          {n.kind}
                        </button>)
                    }
                  </fieldset>)
                }
              </div>)
          }
        </div>
      </div>
    </Modal>
  );
}

NodeSelectorModal.chooseElement = (parentNode: string): Promise<NodeUtils.NodeOptions<BaseNode> | undefined> => {
  var o = NodeUtils.registeredNodes[parentNode];
  if (o.validChild)
    return Promise.resolve(NodeUtils.registeredNodes[o.validChild]);

  return openModal<NodeUtils.NodeOptions<BaseNode>>(<NodeSelectorModal />);
}
