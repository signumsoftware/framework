import * as React from 'react'
import { AutoLine, Binding, } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { TextPartEntity, TextPartType } from '../Signum.Dashboard';
import { Entity } from '@framework/Signum.Entities';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import Markdown from 'react-markdown';
import HtmlEditor from '../../Signum.HtmlEditor/HtmlEditor'
import { ErrorBoundary } from '@framework/Components/ErrorBoundary';
import { ReadonlyBinding } from '@framework/Lines'
import { useForceUpdate } from '../../../Signum/React/Hooks';
import { LinkButton } from '@framework/Basics/LinkButton';

export default function TextPart(p: { ctx: TypeContext<TextPartEntity> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  const [isPreview, setIsPreview] = React.useState(false);
  React.useEffect(() => {
    if (!p.ctx.value.isNew && p.ctx.value.textPartType == "Markdown")
      setIsPreview(true);
    else
      setIsPreview(false);
  }, [p.ctx.value]);

  const forceUpdate = useForceUpdate();


  function getEditType(): React.JSX.Element {
    if (p.ctx.value.textPartType == "Text")
      return (<AutoLine ctx={ctx.subCtx(s => s.textContent)} />)

    if (p.ctx.value.textPartType == "Markdown")
      return (<AutoLine ctx={ctx.subCtx(s => s.textContent)} />)

    if (p.ctx.value.textPartType == "HTML")
      return (<HtmlEditor binding={Binding.create(ctx.value, e => e.textContent)}  />)

    return (<AutoLine ctx={ctx.subCtx(s => s.textContent)} />)
  }


  function getPreviewType(): React.JSX.Element {
    if (ctx.value.textPartType == "Text")
      return (<text>{ctx.value.textContent}</text>)

    if (ctx.value.textPartType == "Markdown")
      return (<Markdown>{ctx.value.textContent}</Markdown>)

    if (ctx.value.textPartType == "HTML" && ctx.value.textContent != null)
      return (<HtmlViewer text={ctx.value.textContent} />)

    return (<text>{ctx.value.textContent}</text>)
  }


  return (
    <div>
      <div style={{ marginBottom: "20px" }}>
      </div>
      <div className="row">
        <div className="col-sm-4">
          <AutoLine ctx={ctx.subCtx(s => s.textPartType)} onChange={() => forceUpdate()} />
        </div>
        <div className="col-sm-4">
          {p.ctx.value.textPartType == "Markdown" ? <LinkButton title={undefined} onClick={e => { setIsPreview(!isPreview); }}>
            <FontAwesomeIcon aria-hidden icon={isPreview ? "edit" : "eye"} className="me-1" />{isPreview ? "Edit" : "Preview"}
          </LinkButton> : null}
        </div>
      </div>
      <div className="form-inline">
        {isPreview ?
          getPreviewType() :
          getEditType()
        }
      </div>
    </div>
  );
}

export function HtmlViewer(p: { text: string; htmlAttributes?: React.HTMLAttributes<HTMLDivElement> }): React.JSX.Element {

  var binding = new ReadonlyBinding(p.text, "");

  return (
    <div className="html-viewer" >
      <HtmlEditor readOnly
        binding={binding}
        htmlAttributes={p.htmlAttributes}
        small
        plugins={[]}
      />
    </div>
  );
}
