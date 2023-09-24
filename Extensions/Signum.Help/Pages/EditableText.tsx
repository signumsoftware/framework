import * as React from 'react'
import { useForceUpdate, useWindowEvent } from '@framework/Hooks';
import * as HelpClient from '../HelpClient';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { ReadonlyBinding, TypeContext, ValueLine } from '@framework/Lines';
import { classes } from '@framework/Globals';
import * as AppContext from '@framework/AppContext';
import { EntityControlMessage } from '@framework/Signum.Entities';
import { HelpMessage } from '../Signum.Help';
import HtmlEditor from '../../Signum.HtmlEditor/HtmlEditor';
import HtmlViewer from './HtmlViewer';

export function EditableTextComponent({ ctx, defaultText, onChange, defaultEditable }: { ctx: TypeContext<string | undefined | null>, defaultText?: string, onChange?: () => void, defaultEditable?: boolean }) {
  var [editable, setEditable] = React.useState(defaultEditable || false);
  var forceUpdate = useForceUpdate();

  return (
    <span className="sf-edit-container">
      {
        (editable ? <ValueLine ctx={ctx} formGroupStyle="SrOnly" onChange={() => { forceUpdate(); onChange && onChange(); }} placeholderLabels={false} valueHtmlAttributes={{ placeholder: defaultText || ctx.niceName() }} formGroupHtmlAttributes={{ style: { display: "inline-block" } }} /> : 
        ctx.value ? <span>{ctx.value}</span> :
          defaultText ? <span>{defaultText}</span> :
            <span className="sf-no-text">[{ctx.niceName()}]</span>)
      }
      {!ctx.readOnly && <a href="#" className={classes("sf-edit-button", editable && "active")} onClick={e => { e.preventDefault(); setEditable(!editable); }}>
        <FontAwesomeIcon icon={editable ? "close" : "pen-to-square"} className="ms-2" title={(editable ? HelpMessage.Close : HelpMessage.Edit).niceToString()} /> {(editable ? HelpMessage.Close : HelpMessage.Edit).niceToString()}
      </a>}
    </span>
  );
}
  

export function EditableHtmlComponent({ ctx, defaultText, onChange, defaultEditable }: { ctx: TypeContext<string | undefined | null>, defaultText?: string, onChange?: () => void, defaultEditable?: boolean }) {

  var [editable, setEditable] = React.useState(defaultEditable || false);
  var forceUpdate = useForceUpdate();

  return (
    <div className="sf-edit-container">
      {/*{*/}
      {/*  (ctx.value ? <MarkdownText text={ctx.value} /> :*/}
      {/*    defaultText ? <span>{defaultText}</span> :*/}
      {/*      <span className="sf-no-text">[{ctx.niceName()}]</span>) */}
      {/*}*/}

      {editable ? <HtmlEditor binding={ctx.binding} /> :<HtmlViewer text={ctx.value} />
      }

      {!ctx.readOnly && <a href="#" className={classes("sf-edit-button", editable && "active", ctx.value && "block")} onClick={e => { e.preventDefault(); setEditable(!editable); }}>
        <FontAwesomeIcon icon={editable ? "close" : "pen-to-square"} className="ms-2" title={(editable ? HelpMessage.Close : HelpMessage.Edit).niceToString()} /> {(editable ? HelpMessage.Close : HelpMessage.Edit).niceToString()}
      </a>}
{/*      {editable && <ValueLine ctx={ctx} formGroupStyle="SrOnly" onChange={() => { forceUpdate(); onChange && onChange(); }} />}*/}
    </div>
  );
}

//export function HtmlViewer({ text, className }: { text: string | null | undefined, className?: string }) {
//  var htmlText = React.useMemo(() => HelpClient.replaceHtmlLinks(text ?? ""), [text]);
//  //const divRef = React.useRef<HTMLDivElement>(null);
//  //function handleOnClick(e: MouseEvent) {
//  //  debugger;
//  //  var a = e.target as HTMLAnchorElement;
//  //  if (a?.nodeName == "A" && !e.ctrlKey && e.button == 0) {
//  //    var href = a.getAttribute("href");
//  //    if (href != null && href.startsWith(AppContext.toAbsoluteUrl("/"))) {
//  //      e.preventDefault();
//  //      e.stopPropagation();
//  //      AppContext.navigate(href);
//  //    }
//  //  }
//  //}

//  //React.useEffect(() => {
//  //  if (divRef.current) {
//  //    divRef.current.addEventListener('click', handleOnClick);
//  //    return () => {
//  //      divRef.current?.removeEventListener('click', handleOnClick);
//  //    }
//  //  }

//  //}, []);

//  return <HtmlEditor binding={new ReadonlyBinding(htmlText, "")} /*innerRef={divRef} className={className}*/  />;
//}

