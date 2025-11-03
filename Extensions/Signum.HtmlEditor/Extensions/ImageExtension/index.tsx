import { $createTextNode, $getRoot, LexicalEditor } from "lexical";
import { HtmlEditorController } from "../../HtmlEditorController";
import {
  HtmlEditorExtension,
  LexicalConfigNode,
  OptionalCallback,
} from "../types";
import { ImageConverter, ImageInfo } from "./ImageConverter";
import { $createImageNode, ImageNode } from "./ImageNode";

export class ImageExtension
  implements HtmlEditorExtension
{
  name = "ImageExtension";
  constructor(public imageConverter: ImageConverter) {}

  registerExtension(controller: HtmlEditorController): OptionalCallback {
    const abortController = new AbortController();
    const element = controller.editableElement;

    if (!element) return;

    element.addEventListener("dragenter", (event) => {
      if (!controller.editor.isEditable()) {
        event.dataTransfer!.dropEffect = "none";
      }
      else {
        event.dataTransfer!.dropEffect = "copy";
      }
    }, { signal: abortController.signal });

    element.addEventListener(
      "dragover",
      (event) => {
        event.preventDefault();

      if (!controller.editor.isEditable()) {
        event.dataTransfer!.dropEffect = "none";
        return;
      }

        this.insertImageNodes(files, controller.editor, this.imageConverter);
      event.dataTransfer!.dropEffect = "copy";
    }, { signal: abortController.signal });

    element.addEventListener("drop", (event) => {
      if (!controller.editor.isEditable())
        return;

        this.insertImageNodes(files, controller.editor, this.imageConverter);
      event.preventDefault();

      const files = event.dataTransfer?.files;
      if (!files?.length) return;

    }, { signal: abortController.signal });

    element.addEventListener("paste", (event) => {
      if (!controller.editor.isEditable()) return;
      const files = event.clipboardData?.files;
      if (!files?.length) return;

      event.preventDefault();
    }, { signal: abortController.signal });

    return () => {
      abortController.abort();
    };
  }

  getNodes(): LexicalConfigNode {
    return [ImageNode];
  }

  async insertImageNodes(
    files: FileList,
    editor: LexicalEditor,
    imageConverter: ImageConverter
  ): Promise<void> {
    const uploadPromises = Array.from(files)
      .filter((file) => file.type.startsWith("image/"))
      .map((file) => {
        try {
          return imageConverter.uploadData(file);
        } catch (error) {
          console.error("Image uploade failed.", error);
          return null;
        }
      });

    const uploadedFiles = (await Promise.all(uploadPromises)).filter(
      (v) => v !== null
    );
    if (!uploadedFiles.length) return;

    editor.update(() => {
      uploadedFiles.forEach((file) => {
        const imageNode = $createImageNode(file, imageConverter);
        $getRoot().append(imageNode);
      });
    });

}
