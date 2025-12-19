import * as React from 'react'
import { useForceUpdate } from '@framework/Hooks';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { TypeContext, TextAreaLine } from '@framework/Lines';
import { classes } from '@framework/Globals';
import { HelpMessage } from '../Signum.Help';
import { LinkButton } from '@framework/Basics/LinkButton';


export function EditableText({ ctx, defaultText, onChange, defaultEditable }: { ctx: TypeContext<string | null>, defaultText?: string, onChange?: () => void, defaultEditable?: boolean }): React.JSX.Element {
  var [editable, setEditable] = React.useState(defaultEditable || false);
  var forceUpdate = useForceUpdate();

  return (
    <span className="sf-edit-container">
      {
        (editable ? <TextAreaLine ctx={ctx} formGroupStyle="SrOnly" onChange={() => { forceUpdate(); onChange && onChange(); }} placeholderLabels={false} valueHtmlAttributes={{ placeholder: defaultText || ctx.niceName() }} formGroupHtmlAttributes={{ style: { display: "inline-block" } }} /> :
          ctx.value ? <span>{ctx.value}</span> :
            defaultText ? <span>{defaultText}</span> :
              <span className="sf-no-text">[{ctx.niceName()}]</span>)
      }
      {!ctx.readOnly && <LinkButton className={classes("sf-edit-button", editable && "active")} title={(editable ? HelpMessage.Close : HelpMessage.Edit).niceToString()} onClick={e => { setEditable(!editable); }}>
        <FontAwesomeIcon icon={editable ? "close" : "pen-to-square"} className="ms-2" /> {(editable ? HelpMessage.Close : HelpMessage.Edit).niceToString()}
      </LinkButton>}
    </span>
  );
}
