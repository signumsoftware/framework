//import * as React from 'react';
//import * as AppContext from '@framework/AppContext';
//import * as draftjs from 'draft-js';
//import { IconProp } from "@fortawesome/fontawesome-svg-core";
//import { IContentStateConverter, HtmlEditorController, HtmlEditorPlugin } from "../HtmlEditor"
//import { HtmlEditorButton } from '../HtmlEditorButtons';
//import { Finder } from '@framework/Finder';

//function extractLinks(text: string): { from: number, to: number }[]{
//  const linkRegex = /https?:\/\/([-a-zA-Z0-9@:%_\+.~#?&//=]*)/g;
//  const result = [];
//  let m: RegExpExecArray | null;
//  do {
//    m = linkRegex.exec(text);
//    if (m) {
//      result.push({ from: m.index, to: m.index + m[0].length });
//    }
//  } while (m);

//  return result;
//}

//export default class LinksPlugin implements HtmlEditorPlugin {

//  setLink(controller: HtmlEditorController) : void {
//    const editorState = controller.editorState;
//    const selection = editorState.getSelection();
//    const contentState = editorState.getCurrentContent();
//    const block = contentState.getBlockForKey(selection.getStartKey());
//    const entityKey = block.getEntityAt(selection.getStartOffset());
//    const entity = entityKey ? contentState.getEntity(entityKey) : null;

//    const link = window.prompt('Paste the link -', entity?.getData().url);
//    let newSelection = selection;
//    if (newSelection.isCollapsed()) {
//      if (entity != null) {
//        block.findEntityRanges(
//          meta => meta.getEntity() == entityKey,
//          (start, end) => newSelection = newSelection.merge({ anchorOffset: start, focusOffset: end }) as draftjs.SelectionState)
//      }
//    }

//    if (!link) {
//      controller.setEditorState(draftjs.RichUtils.toggleLink(editorState, newSelection, null));
//      return;
//    }
//    const content = editorState.getCurrentContent();
//    const contentWithEntity = content.createEntity('LINK', 'MUTABLE', { url: link });
//    const newEditorState = draftjs.EditorState.push(editorState, contentWithEntity, "apply-entity");
//    const newEntityKey = contentWithEntity.getLastCreatedEntityKey();
//    controller.setEditorState(draftjs.RichUtils.toggleLink(newEditorState, newSelection, newEntityKey))
//  }

//  getDecorators(controller: HtmlEditorController): draftjs.DraftDecorator[] {
//    return [
//      {
//        component: DraftLink,
//        strategy: (contentBlock, callback, contentState) => {
//          contentBlock.findEntityRanges(
//            (character) => {
//              const entityKey = character.getEntity();
//              return (entityKey !== null && contentState.getEntity(entityKey).getType() === 'LINK');
//            },
//            callback
//          );
//        }
//      },
//      {
//        component: AutoDraftLink,
//        strategy: (contentBlock, callback, contentState) => {
//          var links = extractLinks(contentBlock.getText());
//          for (const link of links) {
//            callback(link.from, link.to);
//          }
//        }
//      }
//    ];
//  }

//  getToolbarButtons(controller: HtmlEditorController): React.JSX.Element {
//    return <LinkButton controller={controller} setLink={() => this.setLink(controller)} icon="link" />;
//  }

//  expandEditorProps(props: draftjs.EditorProps, controller: HtmlEditorController): void {
//    var prevKeyCommand = props.handleKeyCommand;
//    props.handleKeyCommand = (command, state, timeStamp) => {

//      if (prevKeyCommand) {
//        var result = prevKeyCommand(command, state, timeStamp);
//        if (result == "handled")
//          return result;
//      }

//      if (command !== 'add-link') {
//        return 'not-handled';
//      }

//      this.setLink(controller);
//      return 'handled';
//    }

//    var prevKeyBindingFn = props.keyBindingFn;
//    props.keyBindingFn = (event) => {
//      if (prevKeyBindingFn) {
//        var result = prevKeyBindingFn(event);
//        if (result)
//          return result;
//      }

//      const editorState = controller.editorState;
//      const selection = editorState.getSelection();
//      if (selection.isCollapsed()) {
//        return null;
//      }
//      if (draftjs.KeyBindingUtil.hasCommandModifier(event) && event.which === 75 /*k*/) {
//        return 'add-link' as draftjs.DraftEditorCommand;
//      }

//      return null;
//    }
//  }
//}



//export function DraftLink({ contentState, entityKey, children }: { contentState: draftjs.ContentState, decoratedText: string, entityKey: string, children: React.ReactNode }): React.JSX.Element {
//  const { url } = contentState.getEntity(entityKey)?.getData();

//  return (
//    <a
//      className="link"
//      href={url}
//      title="Press [Ctrl] + click to follow the link"
//      rel="noopener noreferrer"
//      target="_blank"
//      onClick={e => {
//        if (e.ctrlKey) {
//          e.preventDefault();
//          window.open(url);
//        }
//        var start = AppContext.toAbsoluteUrl("/");
//        if (url.startsWith(start)) {
//          e.preventDefault();
//          AppContext.navigate(url)
//        }

//      }}
//      aria-label={url}
//    >
//      {children}
//    </a>
//  );
//}

//export function AutoDraftLink({ decoratedText, children }: { contentState: draftjs.ContentState, decoratedText: string, entityKey: string, children: React.ReactNode }): React.JSX.Element {

//  return (
//    <a
//      className="link"
//      href={decoratedText}
//      title="Press [Ctrl] + click to follow the link"
//      rel="noopener noreferrer"
//      target="_blank"
//      onClick={e => {
//        if (e.ctrlKey) {
//          e.preventDefault();
//          window.open(decoratedText);
//        }

//        var start = AppContext.toAbsoluteUrl("/");
//        if (decoratedText.startsWith(start)) {
//          e.preventDefault();
//          AppContext.navigate(decoratedText)
//        }
//      }}
//      aria-label={decoratedText}
//    >
//      {children}
//    </a>
//  );
//}


//function isLinkActive(editorState: draftjs.EditorState) {
//  var selection = editorState.getSelection();
//  var contentState = editorState.getCurrentContent();
//  var block = contentState.getBlockForKey(selection.getStartKey());
//  var entityKey = block.getEntityAt(selection.getStartOffset());
//  if (!entityKey)
//    return false;

//  const entity = contentState.getEntity(entityKey)
//  return entity.getType() == "LINK";
//}

//export function LinkButton(p: { controller: HtmlEditorController, icon?: IconProp, content?: React.ReactNode, title?: string, setLink: () => void }): React.JSX.Element {

//  const isActive = isLinkActive(p.controller.editorState);

//  function handleOnClick(e: React.MouseEvent) {
//    e.preventDefault();
//    p.setLink();
//  }

//  return <HtmlEditorButton isActive={isActive} onClick={handleOnClick} icon={p.icon} content={p.content} title={p.title} />
//}
