import { ModifiableEntity } from "@framework/Signum.Entities";
import { $getRoot, $getSelection, LexicalEditor } from "lexical";
import { IFile } from "../../../Signum.Files/Signum.Files";
import { HtmlEditorController } from "../../HtmlEditorController";
import { HtmlEditorExtension, LexicalConfigNode, OptionalCallback } from "../types";
import { ImageConverter } from "./ImageConverter";
import { $createImageNode, ImageNode } from "./ImageNode";

export class ImageExtension implements HtmlEditorExtension {
  constructor(public imageConverter: ImageConverter<any>) {}

  registerExtension(controller: HtmlEditorController): OptionalCallback {
    const abortController = new AbortController();
    const element = controller.editableElement;

    if(!element) {
      console.warn("ImageExtension does not work properly without the editable element.");
      return;
    }

    element.addEventListener("dragover", (event) => {
      event.preventDefault(); 
    }, { signal: abortController.signal });

    element.addEventListener("drop", async (event) => {
      event.preventDefault();
      const files = event.dataTransfer?.files;

      if(!files?.length) return;

      for(let i = 0; i < files.length; i++) {
        const file = files[i];

        if(!file.type.startsWith("image/")) continue;

        const uploadedFile: IFile & ModifiableEntity = await this.imageConverter?.uploadData(file);
        controller.editor.update(() => {
          const imageNode = $createImageNode(uploadedFile, this.imageConverter);
          const selection = $getSelection();

          if(selection) {
            selection.insertNodes([imageNode]);
          } else {
            $getRoot().append(imageNode)
          }

        });
      }
    }, { signal: abortController.signal })

    const unsubscribeUpdateListener = controller.editor.registerUpdateListener(() => replaceImagePlaceholders(controller.editor,  this.imageConverter));

    return () => {
      abortController.abort();
      unsubscribeUpdateListener();
    };
  }
  getNodes(): LexicalConfigNode {
    return [ImageNode]
  }
}

function replaceImagePlaceholders(editor: LexicalEditor, imageConverter: ImageConverter<any>) {
  editor.update(() => {
    const editorState = editor.getEditorState();
    editorState._nodeMap.forEach((node) => {
        const text = node.getTextContent();
        if(node.getType() === "text" && isImagePlaceholderRegex(text)) {
          const attachmentId = extractAttachmentId(text);
          node.replace($createImageNode({ attachmentId }, imageConverter));
        }
    })
  })
}

const IMAGE_PLACEHOLDER_REGEX = /^\[IMAGE_(\d+)\]$/;

function extractAttachmentId(text: string) {
  const match = text.match(IMAGE_PLACEHOLDER_REGEX);
  return match ? match[1] : null
}

function isImagePlaceholderRegex(text: string) {
  return IMAGE_PLACEHOLDER_REGEX.test(text);
}

