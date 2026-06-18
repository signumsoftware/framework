import * as React from 'react'
import { ErrorBoundary } from '@framework/Components';
import Markdown, { Options } from 'react-markdown';
import { TextAreaLine, TextAreaLineProps } from '@framework/Lines/TextAreaLine';
import { FormGroup } from '@framework/Lines/FormGroup';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { LinkButton } from '@framework/Basics/LinkButton';
import { MarkdownMessage } from '@framework/Signum.Entities';

export interface MarkdownLineProps extends TextAreaLineProps {
  markdownOption?: Options;
}

export function MarkdownLine({ ctx, markdownOption, readOnly, label, valueHtmlAttributes, ...p }: MarkdownLineProps): React.JSX.Element {
  const [preview, setPreview] = React.useState(false);

  const toggle = (
    <LinkButton className='ms-1' title={!preview ? MarkdownMessage.Preview0?.niceToString(ctx.niceName()) : MarkdownMessage.Edit0?.niceToString(ctx.niceName())}
      onClick={e => {
        setPreview(a => !a);
      }}>
      <FontAwesomeIcon aria-hidden={true} icon={preview ? "edit" : "eye"} />
    </LinkButton>
  );

  return (
    <ErrorBoundary>
      <FormGroup ctx={ctx} label={label ?? ctx.niceName()} labelIcon={toggle}>
        {inputId => preview ? <div className='form-control form-control-sm'><Markdown>{ctx.value}</Markdown></div> :
          <TextAreaLine
            ctx={ctx.subCtx({ formGroupStyle: 'None' })}
            readOnly={readOnly}
            {...p}
            valueHtmlAttributes={{
              ...valueHtmlAttributes,
              style: { minHeight: 80, ...valueHtmlAttributes?.style },
            }} />}
      </FormGroup>
    </ErrorBoundary>
  );
}
