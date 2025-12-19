import { $createTextNode, $getRoot, LexicalEditor } from "lexical";
import { HtmlEditorController } from "../../HtmlEditorController";
import {
  HtmlEditorExtension,
  LexicalConfigNode,
  OptionalCallback,
} from "../types";
import { ImageHandlerBase, ImageInfo } from "./ImageHandlerBase";
import { $createImageNode, ImageNode } from "./ImageNode";

export class ImageExtension implements HtmlEditorExtension {

  name = "ImageExtension";
  constructor(public imageHandler: ImageHandlerBase) { }

  registerExtension(controller: HtmlEditorController): OptionalCallback {
    const abortController = new AbortController();
    const element = controller.editableElement;

    if (!element)
      return;

    if (controller.editor && controller.editor.imageHandler != this.imageHandler)
      controller.editor.imageHandler = this.imageHandler;

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

      event.dataTransfer!.dropEffect = "copy";
    }, { signal: abortController.signal });

    element.addEventListener("drop", (event) => {
      if (!controller.editor.isEditable())
        return;

      event.preventDefault();

      const files = event.dataTransfer?.files;
      if (!files?.length) return;

      this.insertImageNodes(files, controller, controller.editor.imageHandler!);
    }, { signal: abortController.signal });

    element.addEventListener("paste", (event) => {
      if (!controller.editor.isEditable()) return;
      const files = event.clipboardData?.files;
      if (!files?.length) return;

      event.preventDefault();
      this.insertImageNodes(files, controller, controller.editor.imageHandler!);
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
    controller: HtmlEditorController,
    handler: ImageHandlerBase
  ): Promise<void> {

    const uploadPromises: Promise<ImageInfo>[] = Array.from(files)
      .filter(file => file.type.startsWith("image/"))
      .map(async (file) => {
        try {
          return await handler.uploadData(file);
        } catch (error) {
          console.error("Image upload failed.", error);
          throw error;
        }
      });

    const uploadedFiles = await Promise.allSettled(uploadPromises);
    const successfulFiles: ImageInfo[] = uploadedFiles
      .filter((r): r is PromiseFulfilledResult<Awaited<ImageInfo>> => r.status === "fulfilled")
      .map(r => r.value);

    if (!successfulFiles.length) return;

    controller.editor.update(() => {
      for (const file of successfulFiles) {
        const imageNode = $createImageNode(file, ImageNode);
        $getRoot().append(imageNode);
      }
    });

    controller.saveHtml(); //onBlur is not reliable
    }
}
