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

    element.addEventListener(
      "dragover",
      (event) => {
        event.preventDefault();
      },
      { signal: abortController.signal }
    );

    element.addEventListener(
      "drop",
      (event) => {
        event.preventDefault();
        const files = event.dataTransfer?.files;

        if (!files?.length) return;
        this.insertImageNodes(files, controller.editor, this.imageConverter);
      },
      { signal: abortController.signal }
    );

    element.addEventListener(
      "paste",
      (event) => {
        const files = event.clipboardData?.files;

        if (!files?.length) return;
        event.preventDefault();
        this.insertImageNodes(files, controller.editor, this.imageConverter);
      },
      { signal: abortController.signal }
    );


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
