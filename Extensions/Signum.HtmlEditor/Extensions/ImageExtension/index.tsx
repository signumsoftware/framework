import { $getRoot, $getSelection, LexicalEditor } from "lexical";
import { HtmlEditorController } from "../../HtmlEditorController";
import { HtmlEditorExtension, LexicalConfigNode, OptionalCallback } from "../types";
import { ImageConverter } from "./ImageConverter";
import { $createImageNode, ImageNode } from "./ImageNode";

export class ImageExtension<T extends object = {}> implements HtmlEditorExtension {
  constructor(public imageConverter: ImageConverter<T>) {}

  registerExtension(controller: HtmlEditorController): OptionalCallback {
    const abortController = new AbortController();
    const element = controller.editableElement;

    if(!element) return;

    element.addEventListener("dragover", (event) => {
      event.preventDefault(); 
    }, { signal: abortController.signal });

    element.addEventListener("drop", (event) => {
      event.preventDefault();
      const files = event.dataTransfer?.files;

      if(!files?.length) return;
      this.insertImageNodes(files, controller.editor, this.imageConverter);
    }, { signal: abortController.signal });

    element.addEventListener("paste", (event) => {
      const files = event.clipboardData?.files;
      
      if(!files?.length) return;
      event.preventDefault();
      this.insertImageNodes(files, controller.editor, this.imageConverter);
    }, { signal: abortController.signal });

    const unsubscribeUpdateListener = controller.editor.registerUpdateListener(() => replaceImagePlaceholders(controller.editor,  this.imageConverter));

    return () => {
      abortController.abort();
      unsubscribeUpdateListener();
    };
  }

  getNodes(): LexicalConfigNode {
    return [ImageNode]
  
  }

  async insertImageNodes(files: FileList, editor: LexicalEditor, imageConverter: ImageConverter<T>): Promise<void> {
    const uploadPromises = Array.from(files).filter(file => file.type.startsWith("image/")).map(file => {
      try {
        return imageConverter.uploadData(file)
      } catch (error) {
        console.error("Image uploade failed.", error)
        return null;
      }
    });
  
    const uploadedFiles = (await Promise.all(uploadPromises)).filter(v => v !== null);
    if(!uploadedFiles.length) return;
  
    editor.update(() => {
      uploadedFiles.forEach(file => {
        const imageNode = $createImageNode(file, imageConverter);
        const selection = $getSelection();
  
        if(selection) {
          selection.insertNodes([imageNode]);
        } else {
          $getRoot().append(imageNode)
        }
      })
    });
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

