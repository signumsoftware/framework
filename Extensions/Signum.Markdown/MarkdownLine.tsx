import * as React from 'react'
import { ErrorBoundary } from '@framework/Components';
import Markdown, { Options } from 'react-markdown';
import { TextAreaLine, TextAreaLineProps } from '@framework/Lines/TextAreaLine';
import { FormGroup } from '@framework/Lines/FormGroup';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { LinkButton } from '@framework/Basics/LinkButton';
import { MarkdownMessage } from '@framework/Signum.Entities';
import { OverlayTrigger, Popover } from 'react-bootstrap';

export interface MarkdownLineProps extends TextAreaLineProps {
  markdownOption?: Options;
}

export function MarkdownLine({ ctx, markdownOption, readOnly, label, valueHtmlAttributes, ...p }: MarkdownLineProps): React.JSX.Element {
  const [preview, setPreview] = React.useState(ctx.readOnly);

  React.useEffect(() => {
    setPreview(ctx.readOnly);
  }, [ctx.readOnly]);

  const markdownHelp = (
    <OverlayTrigger trigger="click" placement="top" rootClose overlay={
      <Popover id="markdown-syntax-popover">
        <Popover.Header>Markdown Syntax</Popover.Header>
        <Popover.Body>
          <table className="table table-sm table-borderless mb-0" style={{ fontSize: '0.8em' }}>
            <tbody>
              <tr><td><code>**bold**</code></td><td><strong>bold</strong></td></tr>
              <tr><td><code>*italic*</code></td><td><em>italic</em></td></tr>
              <tr><td><code># H1</code></td><td><strong>H1</strong></td></tr>
              <tr><td><code>## H2</code></td><td><strong>H2</strong></td></tr>
              <tr><td><code>[text](url)</code></td><td>link</td></tr>
              <tr><td><code>- item</code></td><td>list</td></tr>
              <tr><td><code>`code`</code></td><td><code>code</code></td></tr>
              <tr><td><code>---</code></td><td>rule</td></tr>
            </tbody>
          </table>
        </Popover.Body>
      </Popover>
    }>
      <span className="ms-1 me-1" style={{ cursor: 'pointer', color: 'var(--bs-secondary)' }}>
        <FontAwesomeIcon aria-hidden={true} icon={['fab', 'markdown']} />
      </span>
    </OverlayTrigger>
  );

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
      <FormGroup ctx={ctx} label={<>{markdownHelp}{label ?? ctx.niceName()}</>} labelIcon={toggle}>
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
