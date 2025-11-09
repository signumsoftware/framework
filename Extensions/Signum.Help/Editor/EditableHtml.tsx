import * as React from 'react'
import { useForceUpdate, useWindowEvent } from '@framework/Hooks';
import { HelpClient } from '../HelpClient';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { ReadonlyBinding, TypeContext, AutoLine, TextAreaLine } from '@framework/Lines';
import { classes } from '@framework/Globals';
import { HelpMessage } from '../Signum.Help';
import HtmlEditor from '../../Signum.HtmlEditor/HtmlEditor';
import { ErrorBoundary } from '@framework/Components';
import { IBinding, getSymbol } from '@framework/Reflection';
import { ImageExtension } from '../../Signum.HtmlEditor/Extensions/ImageExtension';
import { LinkExtension } from '../../Signum.HtmlEditor/Extensions/LinkExtension';
import { LinkButton } from '@framework/Basics/LinkButton';
import { HelpImageNode } from './HelpImageNode';


export function EditableHtml({ ctx, onChange, defaultEditable }: { ctx: TypeContext<string | undefined | null>, onChange?: () => void, defaultEditable?: boolean }): React.JSX.Element {

  const [editable, setEditable] = React.useState(defaultEditable || false);
  const readOnly = ctx.readOnly || !editable

  return (
    <div className={classes("sf-edit-container", readOnly && "html-viewer")} >

      {editable ? <HelpHtmlEditor binding={ctx.binding} /> : <HtmlViewer text={ctx.value} />}

      {!ctx.readOnly && <LinkButton title={undefined} className={classes("sf-edit-button", editable && "active", ctx.value && "block")} onClick={e => { setEditable(!editable); }}>
        <FontAwesomeIcon icon={editable ? "close" : "pen-to-square"} className="ms-2" aria-hidden /> {(editable ? HelpMessage.Close : HelpMessage.Edit).niceToString()}
      </LinkButton>}
    </div>
  );
}

export function HelpHtmlEditor(p: { binding: IBinding<string | null | undefined>; readOnly?: boolean }): React.JSX.Element {
  return (
    <ErrorBoundary>
      <HtmlEditor
        binding={p.binding}
        readOnly={p.readOnly}
        plugins={[
          new LinkExtension(),
          new ImageExtension(HelpImageNode)
        ]} />
    </ErrorBoundary>
  );
}

export function HtmlViewer(p: { text: string | null | undefined; htmlAttributes?: React.HTMLAttributes<HTMLDivElement>; }): React.JSX.Element | null {

  var htmlText = React.useMemo(() => HelpClient.replaceHtmlLinks(p.text ?? ""), [p.text]);
  if (!htmlText)
    return null;

  var binding = new ReadonlyBinding(htmlText, "");

  return (
    <div className="html-viewer">
      <ErrorBoundary>
        <HtmlEditor readOnly
          binding={binding as any}
          htmlAttributes={p.htmlAttributes}
          small
          plugins={[
            new LinkExtension(),
            new ImageExtension(HelpImageNode)
          ]} />
      </ErrorBoundary>
    </div>
  );
}
