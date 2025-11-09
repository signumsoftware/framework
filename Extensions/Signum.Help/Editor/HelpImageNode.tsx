import { ImageNodeBase } from "../../Signum.HtmlEditor/Extensions/ImageExtension/ImageNodeBase";
import { ImageHandlerBase, ImageInfo } from '../../Signum.HtmlEditor/Extensions/ImageExtension/ImageHandlerBase';
import { PropertyRoute } from "@framework/Lines";
import { getSymbol } from "@framework/Reflection";
import { toFileEntity } from "../../Signum.Files/Components/FileUploader";
import { FileImage } from "../../Signum.Files/Files";
import { FilePathEmbedded, FileTypeSymbol } from "../../Signum.Files/Signum.Files";
import { HelpImageEntity } from "../Signum.Help";

export class HelpImageHandler implements ImageHandlerBase {

  pr: PropertyRoute = HelpImageEntity.propertyRouteAssert(a => a.file);

  toElement(val: ImageInfo): HTMLElement | undefined {
    const img = document.createElement("img");

    val.binaryFile && img.setAttribute("data-binary-file", val.binaryFile);
    img.setAttribute("data-file-name", val.fileName || "");
    val.imageId && img.setAttribute("data-help-image-id", val.imageId);

    return img;
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
        fileName: att.fileName ?? undefined,
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


export class HelpImageNode extends ImageNodeBase {
  static {
    this.converter = new HelpImageHandler();
    this.dataImageIdAttribute = "data-help-image-id";
  }

  static getType(): string {
    return "help-image";
  }

  static clone(node: HelpImageNode): HelpImageNode {
    return new HelpImageNode(node.imageInfo, node.__key);
  }

  static importJSON(serializedNode: ImageInfo): HelpImageNode {
    return new HelpImageNode(serializedNode );
  }
}
