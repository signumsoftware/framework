import * as React from 'react'
import { useForceUpdate, useWindowEvent } from '@framework/Hooks';
import { HelpClient } from '../HelpClient';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Binding, PropertyRoute, ReadonlyBinding, TypeContext, AutoLine, TextAreaLine } from '@framework/Lines';
import { classes } from '@framework/Globals';
import { HelpImageEntity, HelpMessage } from '../Signum.Help';
import HtmlEditor from '../../Signum.HtmlEditor/HtmlEditor';
import { ErrorBoundary } from '@framework/Components';
import { FilePathEmbedded, FileTypeSymbol } from '../../Signum.Files/Signum.Files';
import type { FilesClient } from '../../Signum.Files/FilesClient';
import { IBinding, getSymbol } from '@framework/Reflection';
import { FileImage } from '../../Signum.Files/Components/FileImage';
import { toFileEntity } from '../../Signum.Files/Components/FileUploader';
import { ImageConverter, ImageInfo } from '../../Signum.HtmlEditor/Extensions/ImageExtension/ImageConverter';
import { ImageExtension } from '../../Signum.HtmlEditor/Extensions/ImageExtension';
import { LinkExtension } from '../../Signum.HtmlEditor/Extensions/LinkExtension';
import { LinkButton } from '@framework/Basics/LinkButton';


export function EditableTextComponent({ ctx, defaultText, onChange, defaultEditable }: { ctx: TypeContext<string | null>, defaultText?: string, onChange?: () => void, defaultEditable?: boolean }): React.JSX.Element {
  var [editable, setEditable] = React.useState(defaultEditable || false);
  var forceUpdate = useForceUpdate();

  return (
    <span className="sf-edit-container">
      {
        (editable ? <TextAreaLine ctx={ctx} formGroupStyle="SrOnly" onChange={() => { forceUpdate(); onChange && onChange(); }} placeholderLabels={false} valueHtmlAttributes={{ placeholder: defaultText || ctx.niceName() }} formGroupHtmlAttributes={{ style: { display: "inline-block" } }} /> : 
        ctx.value ? <span>{ctx.value}</span> :
          defaultText ? <span>{defaultText}</span> :
            <span className="sf-no-text">[{ctx.niceName()}]</span>)
      }
      {!ctx.readOnly && <LinkButton className={classes("sf-edit-button", editable && "active")} title={(editable ? HelpMessage.Close : HelpMessage.Edit).niceToString()} onClick={e => { setEditable(!editable); }}>
        <FontAwesomeIcon icon={editable ? "close" : "pen-to-square"} className="ms-2"  /> {(editable ? HelpMessage.Close : HelpMessage.Edit).niceToString()}
      </LinkButton>}
    </span>
  );
}
  

export function EditableHtmlComponent({ ctx, defaultText, onChange, defaultEditable }: { ctx: TypeContext<string | undefined | null>, defaultText?: string, onChange?: () => void, defaultEditable?: boolean }): React.JSX.Element{

  var [editable, setEditable] = React.useState(defaultEditable || false);
  var forceUpdate = useForceUpdate();

  return (
    <div className="sf-edit-container">

      {editable ? <HelpHtmlEditor binding={ctx.binding} /> : <HtmlViewer text={ctx.value} /> }

      {!ctx.readOnly && <LinkButton title={undefined} className={classes("sf-edit-button", editable && "active", ctx.value && "block")} onClick={e => { setEditable(!editable); }}>
        <FontAwesomeIcon icon={editable ? "close" : "pen-to-square"} className="ms-2" aria-aria-hidden /> {(editable ? HelpMessage.Close : HelpMessage.Edit).niceToString()}
      </LinkButton>}
    </div>
  );
}

export function HelpHtmlEditor(p: { binding: IBinding<string | null | undefined> }): React.JSX.Element {

  return (
    <ErrorBoundary>
      <HtmlEditor
        binding={p.binding}
        plugins={[
          new LinkExtension(),
          new ImageExtension(new InlineImageConverter())
        ]} />
    </ErrorBoundary>
  );
}


export function HtmlViewer(p: { text: string | null | undefined; htmlAttributes?: React.HTMLAttributes<HTMLDivElement>; }): React.JSX.Element | null {

  var htmlText = React.useMemo(() => HelpClient.replaceHtmlLinks(p.text ?? ""), [p.text]);
  if (!htmlText)
    return null;

  var binding = new ReadonlyBinding(htmlText, "");

  return (
    <div className="html-viewer">
      <ErrorBoundary>
        <HtmlEditor readOnly
          binding={binding as any}
          htmlAttributes={p.htmlAttributes}
          small
          plugins={[
            new LinkExtension(),
            new ImageExtension(new InlineImageConverter())
          ]} />
      </ErrorBoundary>
    </div>
  );
}



export class InlineImageConverter implements ImageConverter{

  dataImageIdAttribute = "data-help-image-id";
  pr: PropertyRoute;
  constructor() {
    this.pr = HelpImageEntity.propertyRouteAssert(a => a.file);;
  }

  toElement(val: ImageInfo): HTMLElement | undefined {
    const img = document.createElement("img");
    if (val.binaryFile) {
      img.setAttribute("data-binary-file", val.binaryFile);
      img.setAttribute("data-file-name", val.fileName || "");
      return img;
    }

    if (val.imageId) {
      img.setAttribute("data-help-image-id", val.imageId);
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
      entityId: info.imageId,
      mListRowId: null,
      fileType: getSymbol(FileTypeSymbol, this.pr.member!.defaultFileTypeInfo!.key),
      rootType: this.pr.findRootType().name,
      propertyRoute: this.pr.propertyPath()
    });

    if (fp.entityId == null && fp.binaryFile == null)
      return <div className="alert alert-danger">{JSON.stringify(info)}</div>;

    return <FileImage file={fp} />;
  }

  toHtml(val: ImageInfo): string | undefined {
    if (val.binaryFile)
      return `<img data-binary-file="${val.binaryFile}" data-file-name="${val.fileName}" />`;

    if (val.imageId)
      return `<img data-help-image-id="${val.imageId}" />`;

    return undefined;
  }

  fromElement(element: HTMLDivElement): ImageInfo | undefined {
    if (element.tagName == "IMG") {
      return {
        binaryFile: element.dataset["binaryFile"],
        fileName: element.dataset["fileName"],
        imageId: element.dataset["helpImageId"],
      };
    }

    return undefined;
  }
}
