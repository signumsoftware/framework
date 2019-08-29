import * as React from 'react'
import { useForceUpdate } from '@framework/Hooks';
import * as HelpClient from '../HelpClient';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { TypeContext, ValueLine } from '@framework/Lines';
import { classes } from '@framework/Globals';

export function EditableComponent({ ctx, markdown, defaultText, inline, onChange }: { ctx: TypeContext<string | undefined | null>, markdown?: boolean, defaultText?: string, inline?: boolean, onChange?: () => void }) {

  
  var [editable, setEditable] = React.useState(false);
  var markdownText = React.useMemo(() => markdown ? HelpClient.Options.toHtml(ctx.value || "") : undefined, [ctx.value]);
  var forceUpdate = useForceUpdate();

  var Tag: React.ReactType = inline ? "span" : "div";

  return (
    <Tag className="sf-edit-container">
      {markdown ?
        (markdownText ? <div dangerouslySetInnerHTML={{ __html: markdownText }} /> :
          defaultText ? <span>{defaultText}</span> :
            <span className="text-muted">[{ctx.niceName()}]</span>) :

        (ctx.value ? <span>{ctx.value}</span> :
          defaultText ? <span>{defaultText}</span> :
            <span className="text-muted">[{ctx.niceName()}]</span>)
      }
      {!ctx.readOnly && <a href="#" className={classes("sf-edit-button", editable && "active", markdownText && "block")} onClick={e => { e.preventDefault(); setEditable(!editable); }}><FontAwesomeIcon icon="edit" className="ml-2" /></a>}
      {editable && <ValueLine ctx={ctx} formGroupStyle="SrOnly" onChange={() => { forceUpdate(); onChange && onChange(); }} />}
    </Tag>
  );
}
