import * as React from 'react'
import { ErrorBoundary } from '../Components';
import Markdown, { Options } from 'react-markdown';
import { TextAreaLine, TextAreaLineProps } from './TextAreaLine';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { LinkButton } from '../Basics/LinkButton';
import { MarkdownMessage } from '../Signum.Entities';

export interface MarkdownLineProps extends TextAreaLineProps {
  markdownOption?: Options;
}

export function MarkdownLine({ ctx, markdownOption, readOnly, label, valueHtmlAttributes, ...p }: MarkdownLineProps): React.JSX.Element {
  const [preview, setPreview] = React.useState(false);

  return (
    <ErrorBoundary>
      <div>
        <label>{label ?? ctx.niceName()}
          <LinkButton className='ms-1' title={!preview ? MarkdownMessage._0IsCurrentlyEditable?.niceToString(ctx.niceName()) : MarkdownMessage._0IsCurrentlyViewableOnly?.niceToString(ctx.niceName())}
            onClick={e => {
              setPreview(a => !a);
            }}>
            <FontAwesomeIcon aria-hidden={true} icon={preview ? "edit" : "eye"} />
          </LinkButton>
        </label>
        {preview ? <div className='form-control form-control-sm'><Markdown>{ctx.value}</Markdown></div> :
          <TextAreaLine
            ctx={ctx.subCtx({ formGroupStyle: 'None' })}
            readOnly={readOnly}
            {...p}
            valueHtmlAttributes={{
              ...valueHtmlAttributes,
              style: { minHeight: 80, ...valueHtmlAttributes?.style },
            }} />}
      </div>
    </ErrorBoundary>
  );
}
