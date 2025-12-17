import { ImageNodeBase } from "../../Signum.HtmlEditor/Extensions/ImageExtension/ImageNodeBase";
import { HelpImageHandler } from "./HelpImageHandler";
import { ImageInfo } from '../../Signum.HtmlEditor/Extensions/ImageExtension/ImageHandlerBase';

export class HelpImageNode extends ImageNodeBase {
  static {
    this.dataImageIdAttribute = "data-help-image-id";
    this.handler = new HelpImageHandler();
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
