import * as React from 'react'
import { isModifiableEntity, ModifiableEntity, } from '@framework/Signum.Entities'
import { IFile } from './Signum.Entities.Files'
import { Modal } from 'react-bootstrap';
import "./Files.css"
import { FileImage } from './FileImage';
import { IModalProps, openModal } from '@framework/Modals'
import { PropertyRoute } from '@framework/Lines';
import  * as Services from '@framework/Services';
import { configurations } from './FileDownloader';

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
    <Modal onHide={handleCancelClicked} show={showImage} className="message-modal" size="xl" onExited={handleOnExited}>
      <div className="modal-header">
        <h4 className="modal-title">
          {p.title || p.file.fileName}
        </h4>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCancelClicked}/>
      </div>
      <div className="modal-body">
        <FileImage file={p.file} style={{ maxWidth: "100%", marginLeft: "auto", marginRight: "auto", display: "block" }} {...p.imageHtmlAttributes} />
      </div>
    </Modal>
  );
}

ImageModal.show = (file: IFile & ModifiableEntity, event: React.MouseEvent<HTMLImageElement, MouseEvent>, title?: string, imageHtmlAttributes?: React.ImgHTMLAttributes<HTMLImageElement>) => {
  if (event.ctrlKey || event.button == 1) {

    var w = window.open("")!;

    if (w == null)
      return; 

    var url =
        configurations[file.Type].fileUrl!(file);

    Services.ajaxGetRaw({ url: url })
      .then(resp => resp.blob())
      .then(blob => {

        var image = new Image();
        image.src = URL.createObjectURL(blob);
        w!.document.write(image.outerHTML);
        w!.document.title = document.title;
      });
  }
  else {
    openModal(<ImageModal file={file} title={title} imageHtmlAttributes={imageHtmlAttributes} />);
  }
} 


