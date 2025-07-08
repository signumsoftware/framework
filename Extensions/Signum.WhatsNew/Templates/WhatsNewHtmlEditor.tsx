import * as React from 'react'
import { Binding, PropertyRoute, getSymbol } from '@framework/Reflection';
import HtmlEditor from '../../Signum.HtmlEditor/HtmlEditor';
import { toFileEntity } from '../../Signum.Files/Components/FileUploader';
import { ModifiableEntity } from '@framework/Signum.Entities';
import { FilePathEmbedded, FileTypeSymbol, IFile } from '../../Signum.Files/Signum.Files';
import { FileImage } from '../../Signum.Files/Components/FileImage';
import { ReadonlyBinding } from '@framework/Lines';
import { ErrorBoundary } from '@framework/Components';
import { WhatsNewEntity } from '../Signum.WhatsNew';
import { ImageModal } from '../../Signum.Files/Components/ImageModal';
import { LexicalEditor } from "lexical";
import { BasicCommandsExtensions } from '../../Signum.HtmlEditor/Extensions/BasicCommandsExtension';
import { ImageConverter } from '../../Signum.HtmlEditor/Extensions/ImageExtension/ImageConverter';
import { ImageExtension } from '../../Signum.HtmlEditor/Extensions/ImageExtension';
import { ListExtension } from '../../Signum.HtmlEditor/Extensions/ListExtension';
import { LinkExtension } from '../../Signum.HtmlEditor/Extensions/LinkExtension';

export default function WhatsNewHtmlEditor(p: {
  binding: Binding<string | undefined | null>;
  readonly?: boolean
  innerRef?: React.Ref<LexicalEditor>;
}): React.JSX.Element {

  return (
    <ErrorBoundary>
      <HtmlEditor binding={p.binding} readOnly={p.readonly} innerRef={p.innerRef} plugins={[
        new LinkExtension(),
        new ImageExtension(new AttachmentImageConverter())
      ]} />
    </ErrorBoundary>
  );
}

export function HtmlViewer(p: { text: string; }): React.JSX.Element {

  var binding = new ReadonlyBinding(p.text, "");

  return (
    <div className="html-viewer" >
      <ErrorBoundary>
        <HtmlEditor readOnly binding={binding} small plugins={[
          new LinkExtension(),
          new ImageExtension(new AttachmentImageConverter())
        ]} />
      </ErrorBoundary>
    </div>
  );
}

export interface ImageInfo {
  attachmentId?: string;
  binaryFile?: string;
  fileName?: string;
}

export class AttachmentImageConverter implements ImageConverter<ImageInfo>{

  pr: PropertyRoute;
  constructor() {
    this.pr = WhatsNewEntity.propertyRouteAssert(a => a.attachment);
  }

  toElement(val: ImageInfo): HTMLElement | undefined {
    const img = document.createElement("img");
    if (val.binaryFile) {
      img.setAttribute("data-binary-file", val.binaryFile);
      img.setAttribute("data-file-name", val.fileName || "");
      return img;
    }

    if (val.attachmentId) {
      img.setAttribute("data-attachment-id", val.attachmentId);
      return img;
    }
  }
  uploadData(blob: Blob): Promise<ImageInfo> {

    var file = blob instanceof File ? blob :
      new File([blob], "pastedImage." + blob.type.after("/"));

    return toFileEntity(file, {
      type: FilePathEmbedded, accept: "image/*",
      maxSizeInBytes: this.pr.member!.defaultFileTypeInfo!.maxSizeInBytes ?? undefined
    })
      .then(att => ({
        binaryFile: att.binaryFile ?? undefined,
        fileName: att.fileName ?? undefined
      }));
  }

  renderImage(info: ImageInfo): React.ReactElement<any, string | ((props: any) => React.ReactElement<any, string | any | (new (props: any) => React.Component<any, any, any>)> | null) | (new (props: any) => React.Component<any, any, any>)> {
    var fp = FilePathEmbedded.New({
      binaryFile: info.binaryFile,
      entityId: info.attachmentId,
      mListRowId: null,
      fileType: getSymbol(FileTypeSymbol, this.pr.member!.defaultFileTypeInfo!.key),
      rootType: this.pr.findRootType().name,
      propertyRoute: this.pr.propertyPath()
    });

    if (fp.entityId == null && fp.binaryFile == null)
      return <div className="alert alert-danger">{JSON.stringify(info)}</div>;

    return <FileImage file={fp} className="mw-100 whatsnew-image" onClick={e => ImageModal.show(fp as IFile & ModifiableEntity, e)} />;
  }

  toHtml(val: ImageInfo): string | undefined {

    if (val.binaryFile)
      return `<img data-binary-file="${val.binaryFile}" data-file-name="${val.fileName}" />`;

    if (val.attachmentId)
      return `<img data-attachment-id="${val.attachmentId}" />`;

    return undefined;
  }

  fromElement(element: HTMLDivElement): ImageInfo | undefined {
    if (element.tagName == "IMG") {
      return {
        binaryFile: element.dataset["binaryFile"],
        fileName: element.dataset["fileName"],
        attachmentId: element.dataset["attachmentId"],
      };
    }

    return undefined;
  }
}
