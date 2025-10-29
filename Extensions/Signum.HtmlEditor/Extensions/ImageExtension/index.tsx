import { $getRoot, LexicalEditor } from "lexical";
import { HtmlEditorController } from "../../HtmlEditorController";
import { HtmlEditorExtension, LexicalConfigNode, OptionalCallback } from "../types";
import { ImageConverter, ImageInfoBase } from "../../HtmlEditorClient";
import { $createImageNode, ImageNode } from "./ImageNode";

export class ImageExtension<T extends ImageInfoBase> implements HtmlEditorExtension {
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

    const unsubscribeUpdateListener = controller.editor.registerUpdateListener(() => {
      if(!controller.editor || !this.imageConverter) return;
      this.replaceImagePlaceholders(controller);
    });

    return () => {
      abortController.abort();
      unsubscribeUpdateListener();
    };
  }

  getNodes(): LexicalConfigNode {
    return [ImageNode]
  
  }

  async insertImageNodes<T extends ImageInfoBase>(
    files: FileList,
    editor: LexicalEditor,
    imageConverter: ImageConverter<T>
  ): Promise<void> {

    const uploadPromises: Promise<T>[] = Array.from(files)
      .filter(file => file.type.startsWith("image/"))
      .map(async (file) => {
        try {
          return await imageConverter.uploadData(file);
        } catch (error) {
          console.error("Image upload failed.", error);
          throw error;
        }
      });

    const uploadedFiles = await Promise.allSettled(uploadPromises);
    const successfulFiles: T[] = uploadedFiles
      .filter((r): r is PromiseFulfilledResult<Awaited<T>> => r.status === "fulfilled")
      .map(r => r.value);

    if (!successfulFiles.length) return;

    editor.update(() => {
      for (const file of successfulFiles) {
        const imageNode = $createImageNode(file, imageConverter);
        $getRoot().append(imageNode);
      }
    });
  }


  replaceImagePlaceholders(controller: HtmlEditorController): void {
    const attachments = (() => {
      const binding = controller.binding;
      if('parentObject' in binding) {
        const parentObject = binding.parentObject as object;
        if('attachments' in parentObject) {
          const attachments = parentObject.attachments as { rowId: number }[];
          return attachments.map(att => att.rowId?.toString()) ?? []
        }
      }

      return [];
    })();
    
    if(!attachments.length) return;
    
    const editorState =  controller.editor.getEditorState();
    let hasUpdatedNodes = false
    controller.editor.update(() => {
      const nodes = Array.from(editorState._nodeMap.values());
      if(!nodes.some(v => isImagePlaceholderRegex(v.getTextContent()))) return;
      editorState._nodeMap.forEach((node) => {
          const text = node.getTextContent();
          
          if(node.getType() === "text" && isImagePlaceholderRegex(text)) {
            const attachmentId = extractAttachmentId(text);
            if (attachmentId && !attachments.includes(attachmentId)) return;
            node.replace($createImageNode({ attachmentId } as ImageInfoBase, this.imageConverter));
            hasUpdatedNodes = true;
          }
      });
    }, { discrete: true });

    if(hasUpdatedNodes) controller.saveHtml();
  }
}

export const IMAGE_PLACEHOLDER_REGEX: RegExp = /^\[IMAGE_(\d+)\]$/;

export function extractAttachmentId(text: string): string | null {
  const match = text.match(IMAGE_PLACEHOLDER_REGEX);
  return match ? match[1] : null
}

export function isImagePlaceholderRegex(text: string): boolean {
  return IMAGE_PLACEHOLDER_REGEX.test(text);
}

