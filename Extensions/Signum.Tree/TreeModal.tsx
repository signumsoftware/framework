import * as React from 'react'
import { openModal, IModalProps } from '@framework/Modals';
import { FrameMessage, Lite } from '@framework/Signum.Entities'
import { getTypeInfo } from '@framework/Reflection'
import { TreeEntity } from './Signum.Tree'
import { TreeClient, TreeNode, TreeOptions } from './TreeClient'
import { TreeViewer } from './TreeViewer'
import { FilterOption } from "@framework/FindOptions";
import { Modal } from 'react-bootstrap';
import { ModalHeaderButtons } from '@framework/Components/ModalHeaderButtons';
import { useForceUpdate } from '@framework/Hooks'
import { LinkButton } from '@framework/Basics/LinkButton';

interface TreeModalProps extends  IModalProps<TreeNode | undefined> {
  treeOptions: TreeOptions;
  title?: React.ReactNode;
}

function TreeModal(p : TreeModalProps): React.JSX.Element {
  const forceUpdate = useForceUpdate();

  const [show, setShow] = React.useState(true);

  const selectedNodeRef = React.useRef<TreeNode | undefined>(undefined);

  const okPressedRef = React.useRef<boolean>(false);

  const treeViewRef = React.useRef<TreeViewer>(null);

  function handleSelectedNode(selected: TreeNode | undefined) {
    selectedNodeRef.current = selected;
    forceUpdate();
  }

  function handleOkClicked() {
    okPressedRef.current = true;
    setShow(false);
  }

  function handleCancelClicked() {
    okPressedRef.current = false;
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(okPressedRef.current ? selectedNodeRef.current : undefined);
  }

  function handleDoubleClick(selectedNode: TreeNode, e: React.MouseEvent<any>) {
    e.preventDefault();
    selectedNodeRef.current = selectedNode;
    okPressedRef.current = true;
    setShow(false);
  }

  const okEnabled = selectedNodeRef.current != null;

  return (
    <Modal size="lg" onHide={handleCancelClicked} show={show} onExited={handleOnExited}>
      <ModalHeaderButtons onClose={handleCancelClicked}>
        <span className="sf-entity-title"> {p.title ?? getTypeInfo(p.treeOptions.typeName).nicePluralName}</span>
        &nbsp;
        <LinkButton className="sf-popup-fullscreen" title={FrameMessage.Fullscreen.niceToString()} onClick={(e) => treeViewRef.current
          && treeViewRef.current.handleFullScreenClick(e)}>
          <span className="fa fa-external-link"></span>
        </LinkButton>
      </ModalHeaderButtons>

      <div className="modal-body">
        <TreeViewer
          treeOptions={p.treeOptions}
          avoidChangeUrl={true}
          onSelectedNode={handleSelectedNode}
          onDoubleClick={handleDoubleClick}
          ref={treeViewRef}
        />
      </div>
    </Modal>
  );
}

namespace TreeMap {
  export function open(treeOptions: TreeOptions, options?: TreeClient.TreeModalOptions): Promise<Lite<TreeEntity> | undefined> {
    return openModal<TreeNode>(<TreeModal
      treeOptions={treeOptions}
      title={options?.title}
    />)
      .then(tn => tn?.lite);
  }
}

export default TreeMap;



