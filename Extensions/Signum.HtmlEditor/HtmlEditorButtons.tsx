import * as React from 'react'
import * as draftjs from 'draft-js';
import { IBinding } from '@framework/Reflection';
import { HtmlContentStateConverter } from './HtmlContentStateConverter';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconProp } from "@fortawesome/fontawesome-svg-core";
import { classes } from '@framework/Globals';
import { HtmlEditorController } from './HtmlEditor';

export function Separator() {
  return <div className="sf-html-separator" />
}

export function HtmlEditorButton(p: { icon?: IconProp, content?: React.ReactChild, isActive?: boolean, title?: string, onClick: (e: React.MouseEvent) => void }) {
  return (
    <div className="sf-draft-button-wrapper" onMouseDown={e => e.preventDefault()} >
      <button className={classes("sf-draft-button", p.isActive && "sf-draft-active ")} onClick={p.onClick} title={p.title}>
        {p.content ?? <FontAwesomeIcon icon={p.icon!} />}
      </button>
    </div>
  );
}

HtmlEditorButton.defaultProps = { icon: "question" };


export function InlineStyleButton(p: { controller: HtmlEditorController, style: string, icon?: IconProp, content?: React.ReactChild, title?: string }) {

  const isActive = p.controller.editorState.getCurrentInlineStyle().has(p.style);

  function toggleStyle(e: React.MouseEvent) {
    e.preventDefault();
    p.controller.setEditorState(draftjs.RichUtils.toggleInlineStyle(p.controller.editorState, p.style));
  }

  return <HtmlEditorButton isActive={isActive} onClick={toggleStyle} icon={p.icon} content={p.content} title={p.title} />
}

export function BlockStyleButton(p: { controller: HtmlEditorController, blockType: string, icon?: IconProp, content?: React.ReactChild, title?: string }) {

  const isActive = p.controller.editorState.getCurrentContent()
    .getBlockForKey(p.controller.editorState.getSelection().getStartKey()).getType() == p.blockType;

  function toggleBlock(e: React.MouseEvent) {
    e.preventDefault();
    p.controller.setEditorState(draftjs.RichUtils.toggleBlockType(p.controller.editorState, p.blockType));
  }

  return <HtmlEditorButton isActive={isActive} onClick={toggleBlock} icon={p.icon} content={p.content} title={p.title} />;
}


export function SubMenuButton(p: { controller: HtmlEditorController, icon?: IconProp, content?: React.ReactChild, title?: string, children: React.ReactNode }) {

  function handleOnClick() {
    p.controller.setOverrideToolbar(<SubMenu controller={p.controller}>{p.children}</SubMenu>);
  }

  return <HtmlEditorButton onClick={handleOnClick} icon={p.icon} content={p.content} title={p.title} />; 
}

export function SubMenu(p: { controller: HtmlEditorController, children: React.ReactNode }) {
  React.useEffect(() => {
    function onWindowClick() {
      p.controller.setOverrideToolbar(undefined);
    }
    window.setTimeout(() => { window.addEventListener('click', onWindowClick); });
    return () => window.removeEventListener('click', onWindowClick);
  }, []);

  return p.children as React.ReactElement;
}
