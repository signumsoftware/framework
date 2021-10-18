import * as React from 'react'
import { openModal, IModalProps } from '@framework/Modals';
import { Lite } from '@framework/Signum.Entities'
import { getTypeInfo } from '@framework/Reflection'
import { TreeEntity } from './Signum.Entities.Tree'
import * as TreeClient from './TreeClient'
import { TreeNode } from './TreeClient'
import { TreeViewer } from './TreeViewer'
import { FilterOption } from "@framework/FindOptions";
import { Modal } from 'react-bootstrap';
import { ModalHeaderButtons } from '@framework/Components/ModalHeaderButtons';
import { useForceUpdate } from '@framework/Hooks'

interface TreeModalProps extends  IModalProps<TreeNode | undefined> {
  typeName: string;
  filterOptions: FilterOption[];
  title?: React.ReactNode;
}

export default function TreeModal(p : TreeModalProps){
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
        <span className="sf-entity-title"> {p.title ?? getTypeInfo(p.typeName).nicePluralName}</span>
        &nbsp;
                      <a className="sf-popup-fullscreen" href="#" onClick={(e) => treeViewRef.current
          && treeViewRef.current.handleFullScreenClick(e)}>
          <span className="fa fa-external-link"></span>
        </a>
      </ModalHeaderButtons>

      <div className="modal-body">
        <TreeViewer
          filterOptions={p.filterOptions}
          avoidChangeUrl={true}
          typeName={p.typeName}
          onSelectedNode={handleSelectedNode}
          onDoubleClick={handleDoubleClick}
          ref={treeViewRef}
        />
      </div>
    </Modal>
  );
}

TreeModal.open = (typeName: string, filterOptions: FilterOption[], options ?: TreeClient.TreeModalOptions): Promise < Lite<TreeEntity> | undefined > => {
  return openModal<TreeNode>(<TreeModal
    filterOptions={filterOptions}
    typeName={typeName}
    title={options?.title}
  />)
    .then(tn => tn?.lite);
}



