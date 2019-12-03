import * as React from 'react'
import { ModifiableEntity, } from '@framework/Signum.Entities'
import { IFile } from './Signum.Entities.Files'
import { Modal } from 'react-bootstrap';
import "./Files.css"
import { FileImage } from './FileImage';
import { IModalProps, openModal } from '@framework/Modals'

interface ImageModalProps extends IModalProps<undefined> {
  imageHtmlAttributes?: React.ImgHTMLAttributes<HTMLImageElement>;
  file: IFile & ModifiableEntity;
  title?: string;
}

export function ImageModal(p: ImageModalProps) {

  const [showImage, setShow] = React.useState(true);

  function handleCancelClicked() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(undefined);
  }

  return (
    <Modal onHide={handleCancelClicked} show={showImage} className="message-modal" size="lg" onExited={handleOnExited}>
      <div className="modal-header">
        <h4 className="modal-title">
          {p.title || p.file.fileName}
        </h4>
        <button type="button" className="close" data-dismiss="modal" aria-label="Close" onClick={handleCancelClicked}>
          <span aria-hidden="true">&times;</span>
        </button>
      </div>
      <div className="modal-body">
        <FileImage file={p.file} style={{ maxWidth: "100%", marginLeft: "auto", marginRight: "auto", display: "block" }} {...p.imageHtmlAttributes}/>
      </div>
    </Modal>
  );
}


ImageModal.show = (file: IFile & ModifiableEntity, title?: string, imageHtmlAttributes?: React.ImgHTMLAttributes<HTMLImageElement>) => {
  openModal(<ImageModal file={file} title={title} imageHtmlAttributes={imageHtmlAttributes} />);
} 


