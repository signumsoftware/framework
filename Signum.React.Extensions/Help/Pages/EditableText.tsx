import * as React from 'react'
import { useForceUpdate } from '@framework/Hooks';
import * as HelpClient from '../HelpClient';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { TypeContext, ValueLine } from '@framework/Lines';
import { classes } from '@framework/Globals';
import * as AppContext from '@framework/AppContext';

export function EditableComponent({ ctx, markdown, defaultText, inline, onChange, defaultEditable }: { ctx: TypeContext<string | undefined | null>, markdown?: boolean, defaultText?: string, inline?: boolean, onChange?: () => void, defaultEditable?: boolean }) {

  var [editable, setEditable] = React.useState(defaultEditable || false);
  var forceUpdate = useForceUpdate();


  var Tag: (keyof React.ReactHTML) = inline ? "span" : "div";

  return (
    <Tag className="sf-edit-container">
      {markdown ?
        (ctx.value ? <MarkdownText text={ctx.value} /> :
          defaultText ? <span>{defaultText}</span> :
            <span className="sf-no-text">[{ctx.niceName()}]</span>) :

        (ctx.value ? <span>{ctx.value}</span> :
          defaultText ? <span>{defaultText}</span> :
            <span className="sf-no-text">[{ctx.niceName()}]</span>)
      }
      {!ctx.readOnly && <a href="#" className={classes("sf-edit-button", editable && "active", markdown && ctx.value && "block")} onClick={e => { e.preventDefault(); setEditable(!editable); }}><FontAwesomeIcon icon="pen-to-square" className="ms-2" /></a>}
      {editable && <ValueLine ctx={ctx} formGroupStyle="SrOnly" onChange={() => { forceUpdate(); onChange && onChange(); }} />}
    </Tag>
  );
}

export function MarkdownText({ text, className }: { text: string | null | undefined, className?: string }) {
  var markdownText = React.useMemo(() => HelpClient.toHtml(text ?? ""), [text]);

  function handleOnMouseUp(e: React.MouseEvent) {
    var a = e.target as HTMLAnchorElement;
    if (a?.nodeName == "A" && !e.ctrlKey && e.button == 0) {
      var href = a.getAttribute("href");
      if (href != null && href.startsWith(AppContext.toAbsoluteUrl("~/"))) {
        e.preventDefault();
        e.stopPropagation();
        AppContext.history.push(href);
      }
    }
  }

  return <div onMouseUp={handleOnMouseUp} className={className} style={{ marginBottom: "-15px" }} dangerouslySetInnerHTML={{ __html: markdownText }} />;
}
