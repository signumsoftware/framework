import { ImageNodeBase } from "../../Signum.HtmlEditor/Extensions/ImageExtension/ImageNodeBase";
import { HelpImageHandler } from "./HelpImageHandler";

export class HelpImageNode extends ImageNodeBase {
  static {
    this.converter = new HelpImageHandler();
    this.dataImageIdAttribute = "data-help-image-id";
  }
}
