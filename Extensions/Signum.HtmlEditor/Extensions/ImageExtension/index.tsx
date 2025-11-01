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

    const unsubscribeUpdateListener = controller.editor.registerUpdateListener(
      () => {
        if (!controller.editor || !this.imageConverter) return;
        this.replaceImagePlaceholders(controller);
      }
    );

    return () => {
      abortController.abort();
      unsubscribeUpdateListener();
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

  replaceImagePlaceholders(controller: HtmlEditorController): void {
    //const attachments = (() => {
    //  const value = controller.binding.getValue();
    //  if (value)
    //    return [...value.matchAll(/data-attachment-id="(\d+)"/g)].map(
    //      (m) => m[1]
    //    );
    //  return [];
    //})();

    //if (!attachments.length) return;

    const editorState = controller.editor.getEditorState();
    let hasUpdatedNodes = false;

    controller.editor.update(() => {
      const nodes = Array.from(editorState._nodeMap.values());
      if (!nodes.some((v) => isImagePlaceholderRegex(v.getTextContent())))
        return;
      nodes.forEach((node) => {
        if (node.getType() === "text") {
          const text = node.getTextContent();
          const match = text.match(IMAGE_PLACEHOLDER_REGEX);

          if (match) {
            const before = text.slice(0, match.index!);
            const after = text.slice(match.index! + match[0].length);
            const imageId = match[1];

            const imageNode = $createImageNode({ imageId: imageId } as ImageInfo, this.imageConverter);

            // Replace the text node with the image node
            const replaced = node.replace(imageNode);

            // Insert neighbors around the image
            if (before) replaced.insertBefore($createTextNode(before));
            if (after) replaced.insertAfter($createTextNode(after));

            hasUpdatedNodes = true;
          }
        }
      });
    }, { discrete: true });

    if (hasUpdatedNodes) controller.saveHtml();
  }
}

export const IMAGE_PLACEHOLDER_REGEX: RegExp = /\[IMAGE_([^\]]+)\]/;

export function isImagePlaceholderRegex(text: string): boolean {
  var result = IMAGE_PLACEHOLDER_REGEX.test(text);

  return result;
}
