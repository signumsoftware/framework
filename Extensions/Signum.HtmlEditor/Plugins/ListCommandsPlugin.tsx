//import * as draftjs from 'draft-js';
//import { HtmlEditorPlugin, HtmlEditorController } from '../HtmlEditor';
//import { KeyNames } from '@framework/Components';
//import { ContentBlock, EditorState, genKey, getDefaultKeyBinding, KeyBindingUtil, Modifier, RichUtils } from 'draft-js';

//export default class ListCommandsPlugin implements HtmlEditorPlugin {

//  expandEditorProps?(props: draftjs.EditorProps, controller: HtmlEditorController): draftjs.EditorProps {
//    var prevKeyCommand = props.handleKeyCommand;
//    props.handleKeyCommand = (command, state, timeStamp) => {

//      if (prevKeyCommand) {
//        var result = prevKeyCommand(command, state, timeStamp);
//        if (result == "handled")
//          return result;
//      }

//      if (command === 'unordered-list-item' || command === 'ordered-list-item') {
//        const block = getSelectedBlock(state);
//        if (shouldStartList(block)) {
//          const newEditorState = startList(state, block, command);
//          controller.setEditorState(newEditorState);

//          return 'handled';
//        }
//      } else if (command === 'end-list') {
//        const newEditorState = endList(state);
//        controller.setEditorState(newEditorState);

//        return 'handled';
//      }

//      return "not-handled";
//    }

//    var prevKeyBindingFn = props.keyBindingFn;
//    props.keyBindingFn = (e) => {

//      if ((e.key == KeyNames.space || e.key == KeyNames.backspace || e.key == KeyNames.tab)) {
//        var block = getSelectedBlock(controller.editorState);
//        var blockText = block.getText();
//        var blockType = block.getType();

//        if (e.key === KeyNames.tab) {
//          const newEditorState = draftjs.RichUtils.onTab(e, controller.editorState, 6 /* maxDepth */)
//          if (newEditorState !== controller.editorState) {
//            controller.setEditorState(newEditorState);
//          }
//          return null;
//        }

//        if (e.key == KeyNames.space && blockText && blockText.length <= 2) {
//          if (blockText == "*") {
//            return 'unordered-list-item';
//          }
//          else if (blockText == "1.") {
//            return 'ordered-list-item';
//          }
//        }
//        else if (e.key == KeyNames.backspace && (blockType == 'unordered-list-item' || blockType == 'ordered-list-item') && blockText.length == 0) {
//          return 'end-list';
//        }
//      } 
//      if (prevKeyBindingFn)
//        return prevKeyBindingFn(e);

//      return getDefaultKeyBinding(e);
//    }

//    return props
//  }
//}

//const getSelectedBlock = (editorState: EditorState) => {
//  const selection = editorState.getSelection();
//  const contentState = editorState.getCurrentContent();
//  const blockStartKey = selection.getStartKey();

//  return contentState.getBlockMap().get(blockStartKey);
//}

//function shouldStartList(block: ContentBlock) {
//  return block.getType() === 'unstyled' && (block.getText() === '*' || block.getText() === '1.');
//}

//function startList(editorState: EditorState, block: ContentBlock, command: string) {

//  const listType = command;
//  const newEditorState = RichUtils.toggleBlockType(editorState, listType);
//  const contentState = newEditorState.getCurrentContent();
//  const selection = newEditorState.getSelection();

//  const blockSelection = selection.merge({
//    anchorOffset: 0,
//    focusOffset: block.getLength()
//  });

//  const newContentState = Modifier.replaceText(
//    contentState,
//    blockSelection,
//    ''
//  );

//  return EditorState.push(newEditorState, newContentState, 'change-block-type');
//};

//function endList(editorState: EditorState) {
//  return EditorState.push(editorState, RichUtils.tryToRemoveBlockStyle(editorState)!, 'change-block-type');
//};
