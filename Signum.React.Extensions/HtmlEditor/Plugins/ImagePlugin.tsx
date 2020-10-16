import * as React from 'react';
import * as draftjs from 'draft-js';
import { IContentStateConverter, HtmlEditorController, HtmlEditorPlugin } from "../HtmlEditor"
import { HtmlContentStateConverter } from '../HtmlContentStateConverter';

export interface ImageConverter<T extends object> {
  uploadData(blob: Blob): Promise<T>;
  renderImage(val: T): React.ReactElement;
  toHtml(val: T): string | undefined;
  fromElement(val: HTMLElement): T | undefined;
}

export default class ImagePlugin implements HtmlEditorPlugin{

  constructor(public imageConverter: ImageConverter<any>) {
  }

  addImage(editorState: draftjs.EditorState, data: Object): draftjs.EditorState {

    const contentState = editorState.getCurrentContent();
    const contentStateWithEntity = contentState.createEntity(
      'IMAGE',
      'IMMUTABLE',
      data
    );
    const entityKey = contentStateWithEntity.getLastCreatedEntityKey();
    const newEditorState = draftjs.AtomicBlockUtils.insertAtomicBlock(
      editorState,
      entityKey,
      ' '
    );

    return draftjs.EditorState.forceSelection(
      newEditorState,
      newEditorState.getCurrentContent().getSelectionAfter()
    );
  }

  expandConverter(converter: IContentStateConverter) {
    if (converter instanceof HtmlContentStateConverter) {
      const { draftToHtmlOptions, htmlToDraftOptions } = converter;

      //@ts-ignore
      var oldCustomEntityTransformer = draftToHtmlOptions.customEntityTransform;
      draftToHtmlOptions.customEntityTransform = (entity, text) => {
        if (oldCustomEntityTransformer) {
          var result = oldCustomEntityTransformer(entity, text);
          if (result)
            return result;
        }
        return this.imageConverter.toHtml(entity.data);
      };

      var oldCustomChunkRenderer = htmlToDraftOptions.customChunkRenderer;
      htmlToDraftOptions.customChunkRenderer = (nodeName, node) => {
        if (oldCustomChunkRenderer) {
          var result = oldCustomChunkRenderer(nodeName, node);
          if (result != null)
            return result;
        }

        var data = this.imageConverter.fromElement(node);
        if (data != null) {
          return {
            type: "IMAGE",
            data: data,
            mutability: "IMMUTABLE"
          };
        }
        return undefined;
      }

    }
  }

  expandEditorProps(props: draftjs.EditorProps, controller: HtmlEditorController) {
    var oldRenderer = props.blockRendererFn;
    props.blockRendererFn = (block) => {

      if (oldRenderer) {
        const result = oldRenderer(block);
        if (result)
          return result;
      }

      //if (block.getType() === 'atomic') {
      const contentState = controller.editorState.getCurrentContent();
      const entity = block.getEntityAt(0);
      if (!entity)
        return null;

      const type = contentState.getEntity(entity).getType();
      if (type === 'IMAGE') {
        return {
          component: ImageComponent,
          editable: false,
          props: { imageConverter: this.imageConverter }
        };
      }
      //}

      return null;
    };

    props.handlePastedFiles = files => {
      const imageFiles = files.filter(a => a.type.startsWith("image/"));
      if (imageFiles.length == 0)
        return "not-handled";

      Promise.all(imageFiles.map(blob => this.imageConverter.uploadData(blob)))
        .then(datas => {
          var newState = datas.reduce<draftjs.EditorState>((state, data) => this.addImage(state, data), controller.editorState);
          controller.setEditorState(newState);
        }).done();

      return "handled"
    }

    props.handleDroppedFiles = (selection, files) => {
      const imageFiles = files.filter(a => a.type.startsWith("image/"));
      if (imageFiles.length == 0)
        return "not-handled";

      const editorStateWithSelection = draftjs.EditorState.acceptSelection(controller.editorState, selection);
      Promise.all(imageFiles.map(blob => this.imageConverter.uploadData(blob)))
        .then(datas => {
          var newState = datas.reduce<draftjs.EditorState>((state, data) => this.addImage(state, data), editorStateWithSelection);
          controller.setEditorState(newState);
        }).done();

      return "handled"
    }
  }
}

function ImageComponent(p: { contentState: draftjs.ContentState, block: draftjs.ContentBlock, blockProps: { imageConverter: ImageConverter<any> } }) {
  const data = p.contentState.getEntity(p.block.getEntityAt(0)).getData();
  return p.blockProps.imageConverter!.renderImage(data);
}
