import { ModifiableEntity } from "@framework/Signum.Entities";
import { $getRoot, $getSelection } from "lexical";
import React from 'react';
import { ImageModal } from "../../../Signum.Files/Files";
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

        const reader = new FileReader();
        const uploadedFile: IFile & ModifiableEntity = await this.imageConverter?.convert(file);

        reader.onload = () => {
          controller.editor.update(() => {
            const src = reader.result as string;
            const handleImageClick =  (event: React.MouseEvent<HTMLImageElement>) => {
              if(uploadedFile) {
                ImageModal.show(uploadedFile, event);
              }
            }
            const imageNode = $createImageNode(src, "Image", handleImageClick);
            const selection = $getSelection();

            if(selection) {
              selection.insertNodes([imageNode]);
            } else {
              $getRoot().append(imageNode)
            }

          });
        };

        reader.readAsDataURL(file);
      }
    }, { signal: abortController.signal })

    return () => abortController.abort();
  }
  getNodes(): LexicalConfigNode {
    return [ImageNode]
  }
}
