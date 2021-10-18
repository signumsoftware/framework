
import * as React from 'react'
import { Dic } from '@framework/Globals';
import { openModal, IModalProps } from '@framework/Modals';
import * as NodeUtils from './NodeUtils'
import { BaseNode } from './Nodes'
import { Modal } from 'react-bootstrap';

interface ShowCodeModalProps extends IModalProps<undefined> {
  typeName: string;
  node: BaseNode;
}

export default function ShowCodeModal(p: ShowCodeModalProps) {

  const [show, setShow] = React.useState<boolean>(true);

  function handleCancelClicked() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(undefined);
  }

  return (
    <Modal size="lg" onHide={handleCancelClicked} show={show} onExited={handleOnExited} className="sf-selector-modal">
      <div className="modal-header">
        <h5 className="modal-title">{p.typeName + "Component code"}</h5>
        <button type="button" className="close" data-dismiss="modal" aria-label="Close" onClick={handleCancelClicked}>
          <span aria-hidden="true">&times;</span>
        </button>
      </div>
      <div className="modal-body">
        <pre>
          {renderFile(p.typeName, p.node)}
        </pre>
      </div>
    </Modal>
  );
}

ShowCodeModal.showCode = (typeName: string, node: BaseNode): Promise<void> => {
  return openModal<void>(<ShowCodeModal typeName={typeName} node={node} />);
}

function renderFile(typeName: string, node: BaseNode): string {

  var cc = new NodeUtils.CodeContext("ctx", [], {}, []);

  var text = NodeUtils.renderCode(node, cc).indent(4);

  return (
    `
import * as React from 'react'
import { Dic } from '@framework/Globals'
import { getMixin } from '@framework/Signum.Entities'
import { ${typeName}Entity } from '../[your namespace]'
import { ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip,
         EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, EntityTable } from '@framework/Lines'
import { SearchControl, ValueSearchControl } from '@framework/Search'
${Dic.getValues(cc.imports.toObjectDistinct(a => a)).join("\n")}

export default function ${typeName}(p: { ctx: TypeContext<${typeName}Entity> }) {
  const ctx = p.ctx;
${Dic.map(cc.assignments, (k, v) => `const ${k} = ${v};`).join("\n").indent(2)}
  return (
${text}
  );
}`
  );

}





