import { PropertyRoute } from "@framework/Lines";
import { getSymbol } from "@framework/Reflection";
import { useAPI } from "@framework/Hooks";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { FileImage } from "../../Signum.Files/Components/FileImage";
import { toFileEntity } from "../../Signum.Files/Components/FileUploader";
import { FilePathEmbedded, FileTypeSymbol } from "../../Signum.Files/Signum.Files";
import { ImageHandlerBase, ImageInfo } from "../../Signum.HtmlEditor/Extensions/ImageExtension/ImageHandlerBase";
import { HelpImageEntity, HelpMessage } from "../Signum.Help";
import { HelpImageNode } from "./HelpImageNode";
import { ImageNodeBase } from "../../Signum.HtmlEditor/Extensions/ImageExtension/ImageNodeBase";
import { HelpClient } from "../HelpClient";

export class HelpImageHandler implements ImageHandlerBase {

  private _pr?: PropertyRoute;

  get pr(): PropertyRoute {
    return this._pr ??= HelpImageEntity.propertyRouteAssert(a => a.file)
  }

  getNodeType(): typeof ImageNodeBase { return HelpImageNode }

  toElement(val: ImageInfo): HTMLElement | undefined {
    const img = document.createElement("img");

    val.binaryFile && img.setAttribute("data-binary-file", val.binaryFile);
    img.setAttribute("data-file-name", val.fileName || "");
    val.key && img.setAttribute("data-help-image-guid", val.key);

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

  renderImage(info: ImageInfo): React.ReactElement {
    return <InlineImage info={info} pr={this.pr} />;
  }

  toHtml(val: ImageInfo): string | undefined {
    if (val.binaryFile)
      return `<img data-binary-file="${val.binaryFile}" data-file-name="${val.fileName}" />`;

    if (val.key)
      return `<img data-help-image-guid="${val.key}" />`;

    return undefined;
  }

  fromElement(element: HTMLDivElement): ImageInfo | undefined {
    if (element.tagName == "IMG") {
      return {
        binaryFile: element.getAttribute("data-binary-file") ?? undefined,
        fileName: element.getAttribute("data-file-name") ?? undefined,
        key: element.getAttribute("data-help-image-guid") ?? undefined,
      };
    }

    return undefined;
  }
}

function InlineImage(p: { info: ImageInfo, pr: PropertyRoute }): React.ReactElement | undefined
{
  const imageId = useAPI(() => p.info.key && HelpClient.API.getImageId(p.info.key), []);

  if (!imageId && !p.info.binaryFile)
  return (
    <div className="alert alert-info d-inline-block" >
      <span>
        <FontAwesomeIcon icon="gear" className="fa-fw me-2" style={{ fontSize: "larger" }} spin />
        {HelpMessage.LoadingImage.niceToString()}...
      </span>
    </div>
  );

  const fp = FilePathEmbedded.New({
    binaryFile: p.info.binaryFile,
    entityId: imageId,
    mListRowId: null,
    fileType: getSymbol(FileTypeSymbol, p.pr.member!.defaultFileTypeInfo!.key),
    rootType: p.pr.findRootType().name,
    propertyRoute: p.pr.propertyPath()
  });

  return <FileImage file={fp} />;

}
