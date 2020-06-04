import * as React from 'react';
import * as draftjs from 'draft-js';
import { stateToHTML, Options as ExportOptions } from 'draft-js-export-html';
import { stateFromHTML, Options as ImportOptions } from 'draft-js-import-html';
import { IContentStateConverter, HtmlEditorController } from "./HtmlEditor"
import { HtmlContentStateConverter } from './HtmlContentStateConverter';


export interface ImageConverter<T extends object> {
  uploadData(blob: Blob): Promise<T>;
  renderImage(val: T): React.ReactElement;
  toHtml(val: T): string;
  fromElement(val: HTMLElement): T | undefined;
}

export function imagePlugin(props: draftjs.EditorProps, controller: HtmlEditorController, imageConverter: ImageConverter<any>) {

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
          props: { imageConverter }
        };
      }
    //}

    return null;
  };

  function addImage(editorState: draftjs.EditorState, data: Object): draftjs.EditorState {

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

  props.handlePastedFiles = files => {
    const imageFiles = files.filter(a => a.type.startsWith("image/"));
    if (imageFiles.length == 0)
      return "not-handled";

    Promise.all(imageFiles.map(blob => imageConverter.uploadData(blob)))
      .then(datas => {
        var newState = datas.reduce<draftjs.EditorState>((state, data) => addImage(state, data), controller.editorState);
        controller.setEditorState(newState);
      }).done();

    return "handled"
  }

  props.handleDroppedFiles = (selection, files) => {
    const imageFiles = files.filter(a => a.type.startsWith("image/"));
    if (imageFiles.length == 0)
      return "not-handled";

    const editorStateWithSelection = draftjs.EditorState.acceptSelection(controller.editorState, selection);
    Promise.all(imageFiles.map(blob => imageConverter.uploadData(blob)))
      .then(datas => {
        var newState = datas.reduce<draftjs.EditorState>((state, data) => addImage(state, data), editorStateWithSelection);
        controller.setEditorState(newState);
      }).done();

    return "handled"
  }


  if (controller.converter instanceof HtmlContentStateConverter) {
    const { importOptions, exportOptions } = controller.converter;

    //@ts-ignore
    var { atomic: oldAtomic, ...otherBlockRenderers } = exportOptions.blockRenderers ?? {};
    exportOptions.blockRenderers = {
      atomic: block => {
        if (oldAtomic) {
          var result = oldAtomic(block);
          if (result)
            return result;
        }

        var entityKey = block.getEntityAt(0);
        var entity = controller.editorState.getCurrentContent().getEntity(entityKey);
        return imageConverter.toHtml(entity.getData());
      },
      ...otherBlockRenderers
    };

    var oldCustomInlinekFn = importOptions.customInlineFn;
    importOptions.customInlineFn = (element, factory) => {
      if (oldCustomInlinekFn) {
        var result = oldCustomInlinekFn(element, factory);
        if (result != null)
          return result;
      }

      var data = imageConverter.fromElement(element as HTMLElement);
      if (data != null) {
        //@ts-ignore
        return factory.Entity("IMAGE", data, "IMMUTABLE");
      }
      return undefined;
    }

  }

  return { addImage };
}

export function ImageComponent(p: { contentState: draftjs.ContentState, block: draftjs.ContentBlock, blockProps: { imageConverter: ImageConverter<any> } }) {
  const data = p.contentState.getEntity(p.block.getEntityAt(0)).getData();
  return p.blockProps.imageConverter!.renderImage(data);
}

