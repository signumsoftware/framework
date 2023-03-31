import * as React from 'react'
import { openModal, IModalProps } from '../Modals';
import { classes } from '../Globals';
import { JavascriptMessage, BooleanEnum, EntityPack, ModifiableEntity, Entity, isEntity, SaveChangesMessage } from '../Signum.Entities'
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import "./Modals.css"
import { BsSize } from '../Components';
import { Modal } from 'react-bootstrap';
import { getTypeInfo, OperationInfo, tryGetTypeInfo, TypeInfo } from '../Reflection';
import { ButtonsContext, EntityFrame } from '../TypeContext';
import { EntityOperationContext, operationInfos } from '../Operations';
import { PropertyRoute } from '../Lines';
import { OperationButton } from '../Operations/EntityOperations';

type SaveChangesResult = EntityOperationContext<any> | "loseChanges" | "cancel";

interface SaveChangesModalProps extends IModalProps<SaveChangesResult | undefined> {
  eocs: EntityOperationContext<any>[];
}

export default function SaveChangesModal(p: SaveChangesModalProps) {

  const [show, setShow] = React.useState(true);

  const selectedValue = React.useRef<SaveChangesResult | undefined>(undefined);

  function handleButtonClicked(val: SaveChangesResult) {
    selectedValue.current = val;
    setShow(false);
  }

  function handleCancelClicked() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(selectedValue.current);
  }

  return (
    <Modal show={show} onExited={handleOnExited}
      dialogClassName={classes("message-modal")}
      onHide={handleCancelClicked} autoFocus={true}>
      <div className={classes("modal-header", "dialog-header-wait")}>
        <span>
          {SaveChangesMessage.ThereAreChanges.niceToString()}
        </span>
      </div>
      <div className="modal-body">
        {SaveChangesMessage.YoureTryingToCloseAnEntityWithChanges.niceToString()}
      </div>
      <div className="modal-footer">
        <div className="btn-toolbar">
          {p.eocs.map(eoc => <OperationButton key={eoc.operationInfo.key} eoc={eoc} avoidAlternatives onOperationClick={() => handleButtonClicked(eoc)} />)}
          <button
            className="btn btn-secondary sf-close-button sf-no-button"
            onClick={() => handleButtonClicked("loseChanges")}
            name="no">
            <FontAwesomeIcon icon={"arrow-rotate-left"} />&nbsp;{SaveChangesMessage.LoseChanges.niceToString()}
          </button>
          <button
            className="btn btn-secondary sf-close-button sf-cancel-button"
            onClick={() => handleButtonClicked("cancel")}
            name="cancel">
            {JavascriptMessage.cancel.niceToString()}
          </button>
        </div>
      </div>
    </Modal>
  );
}

SaveChangesModal.show = (options: SaveChangesModalProps): Promise<SaveChangesResult | undefined> => {
  return openModal(<SaveChangesModal {...options} />);
}

