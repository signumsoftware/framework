import * as React from 'react'
import * as draftjs from 'draft-js';
import { IBinding, Binding, PropertyRoute, getSymbol } from '@framework/Reflection';
import '../../HtmlEditor/HtmlEditor.css';
import HtmlEditor, { HtmlEditorProps, HtmlEditorController } from '../../HtmlEditor/HtmlEditor';
import BasicCommandsPlugin from '../../HtmlEditor/Plugins/BasicCommandsPlugin';
import ImagePlugin, { ImageConverter } from '../../HtmlEditor/Plugins/ImagePlugin';
import LinksPlugin from '../../HtmlEditor/Plugins/LinksPlugin';
import { FileUploader, toFileEntity } from '../../Files/FileUploader';
import { Lite, MList, ModifiableEntity, toLite } from '@framework/Signum.Entities';
import { FilePathEmbedded, FileTypeSymbol, IFile } from '../../Files/Signum.Entities.Files';
import { FileImage } from '../../Files/FileImage';
import { TypeContext, ReadonlyBinding } from '@framework/Lines';
import { ErrorBoundary } from '@framework/Components';
import { ImageModal } from '../../../../Framework/Signum.React.Extensions/Files/ImageModal';
import { WhatsNewEntity } from '../Signum.Entities.WhatsNew';

export default function WhatsNewHtmlEditor(p: {
  binding: Binding<string | undefined | null>;
  readonly?: boolean
  innerRef?: React.Ref<draftjs.Editor>;
}) {

  return (
    <ErrorBoundary>
      <HtmlEditor binding={p.binding} readOnly={p.readonly} innerRef={p.innerRef} plugins={[
        new LinksPlugin(),
        new BasicCommandsPlugin(),
        new ImagePlugin(new AttachmentImageConverter())
      ]} />
    </ErrorBoundary>
  );
}

export function HtmlViewer(p: { text: string; }) {

  var binding = new ReadonlyBinding(p.text, "");

  return (
    <div className="html-viewer" >
      <ErrorBoundary>
        <HtmlEditor readOnly binding={binding} toolbarButtons={c => null} plugins={[
          new LinksPlugin(),
          new BasicCommandsPlugin(),
          new ImagePlugin(new AttachmentImageConverter())
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
