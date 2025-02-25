//import * as React from 'react';
//import * as draftjs from 'draft-js';
//import { IContentStateConverter, HtmlEditorController, HtmlEditorPlugin } from "../HtmlEditor"
//import { HtmlContentStateConverter } from '../HtmlContentStateConverter';

//export interface ImageConverter<T extends object> {
//  uploadData(blob: Blob): Promise<T>;
//  renderImage(val: T): React.ReactElement;
//  toHtml(val: T): string | undefined;
//  fromElement(val: HTMLElement): T | undefined;
//}

//export default class ImagePlugin implements HtmlEditorPlugin{

//  constructor(public imageConverter: ImageConverter<any>) {
//  }

//  addImage(editorState: draftjs.EditorState, data: Object): draftjs.EditorState {

//    const contentState = editorState.getCurrentContent();
//    const contentStateWithEntity = contentState.createEntity(
//      'IMAGE',
//      'IMMUTABLE',
//      data
//    );
//    const entityKey = contentStateWithEntity.getLastCreatedEntityKey();
//    const newEditorState = draftjs.AtomicBlockUtils.insertAtomicBlock(
//      editorState,
//      entityKey,
//      ' '
//    );

//    return draftjs.EditorState.forceSelection(
//      newEditorState,
//      newEditorState.getCurrentContent().getSelectionAfter()
//    );
//  }

//  expandConverter(converter: IContentStateConverter): void {
//    if (converter instanceof HtmlContentStateConverter) {
//      const { draftToHtmlOptions, htmlToDraftOptions } = converter;

//      //@ts-ignore
//      var oldCustomEntityTransformer = draftToHtmlOptions.customEntityTransform;
//      draftToHtmlOptions.customEntityTransform = (entity, text) => {
//        if (oldCustomEntityTransformer) {
//          var result = oldCustomEntityTransformer(entity, text);
//          if (result)
//            return result;
//        }
//        return this.imageConverter.toHtml(entity.data);
//      };

//      var oldCustomChunkRenderer = htmlToDraftOptions.customChunkRenderer;
//      htmlToDraftOptions.customChunkRenderer = (nodeName, node) => {
//        if (oldCustomChunkRenderer) {
//          var result = oldCustomChunkRenderer(nodeName, node);
//          if (result != null)
//            return result;
//        }

//        var data = this.imageConverter.fromElement(node);
//        if (data != null) {
//          return {
//            type: "IMAGE",
//            data: data,
//            mutability: "IMMUTABLE"
//          };
//        }
//        return undefined;
//      }

//    }
//  }

//  expandEditorProps(props: draftjs.EditorProps, controller: HtmlEditorController): void {
//    var oldRenderer = props.blockRendererFn;
//    props.blockRendererFn = (block) => {

//      if (oldRenderer) {
//        const result = oldRenderer(block);
//        if (result)
//          return result;
//      }

//      //if (block.getType() === 'atomic') {
//      const contentState = controller.editorState.getCurrentContent();
//      const entity = block.getEntityAt(0);
//      if (!entity)
//        return null;

//      const type = contentState.getEntity(entity).getType();
//      if (type === 'IMAGE') {
//        return {
//          component: ImageComponent,
//          editable: false,
//          props: { imageConverter: this.imageConverter }
//        };
//      }
//      //}

//      return null;
//    };

//    props.handlePastedFiles = files => {
//      const imageFiles = files.filter(a => a.type.startsWith("image/"));
//      if (imageFiles.length == 0)
//        return "not-handled";

//      Promise.all(imageFiles.map(blob => this.imageConverter.uploadData(blob)))
//        .then(datas => {
//          var newState = datas.reduce<draftjs.EditorState>((state, data) => this.addImage(state, data), controller.editorState);
//          controller.setEditorState(newState);
//        });

//      return "handled"
//    }

//    var oldPasteText = props.handlePastedText;
//    props.handlePastedText = (text, html, editorState) => {
//      if (html) {
//        var node = document.createElement('html')
//        node.innerHTML = html;
//        var array = Array.from(node.getElementsByTagName("img"));
//        if (array.length && array.every(a => a.src.startsWith("data:"))) {
//          var blobs = array.map(a => dataURItoBlob(a.src));
//          Promise.all(blobs.map(img => this.imageConverter.uploadData(img)))
//            .then(datas => {
//              var newState = datas.reduce<draftjs.EditorState>((state, data) => this.addImage(state, data), controller.editorState);
//              controller.setEditorState(newState);
//            });

//          return "handled";
//        }
//      }

//      if (oldPasteText)
//        return oldPasteText(text, html, editorState);

//      return "not-handled";
//    };
//    props.handleDroppedFiles = (selection, files) => {
//      const imageFiles = files.filter(a => a.type.startsWith("image/"));
//      if (imageFiles.length == 0)
//        return "not-handled";

//      const editorStateWithSelection = draftjs.EditorState.acceptSelection(controller.editorState, selection);
//      Promise.all(imageFiles.map(blob => this.imageConverter.uploadData(blob)))
//        .then(datas => {
//          var newState = datas.reduce<draftjs.EditorState>((state, data) => this.addImage(state, data), editorStateWithSelection);
//          controller.setEditorState(newState);
//        });

//      return "handled"
//    }
//  }
//}

//function dataURItoBlob(dataURI: string) {
//  // convert base64 to raw binary data held in a string
//  // doesn't handle URLEncoded DataURIs - see SO answer #6850276 for code that does this
//  var byteString = atob(dataURI.after(','));

//  // separate out the mime component
//  var mimeString = dataURI.between('data:', ";");

//  // write the bytes of the string to an ArrayBuffer
//  var ab = new ArrayBuffer(byteString.length);

//  // create a view into the buffer
//  var ia = new Uint8Array(ab);

//  // set the bytes of the buffer to the correct values
//  for (var i = 0; i < byteString.length; i++) {
//    ia[i] = byteString.charCodeAt(i);
//  }

//  // write the ArrayBuffer to a blob, and you're done
//  var blob = new Blob([ab], { type: mimeString });
//  return blob;

//}

//function ImageComponent(p: { contentState: draftjs.ContentState, block: draftjs.ContentBlock, blockProps: { imageConverter: ImageConverter<any> } }) {
//  const data = p.contentState.getEntity(p.block.getEntityAt(0)).getData();
//  return p.blockProps.imageConverter!.renderImage(data);
//}
