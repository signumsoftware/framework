import * as React from 'react'
import { AutoLine, Binding,  } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { TextPartEntity, TextPartType } from '../Signum.Dashboard';
import { Entity } from '@framework/Signum.Entities';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import Markdown from 'react-markdown';
import HtmlEditor from '../../Signum.HtmlEditor/HtmlEditor'
import { ErrorBoundary } from '@framework/Components/ErrorBoundary';
import { ReadonlyBinding } from '@framework/Lines'

export function HtmlViewer(p: { text: string; htmlAttributes?: React.HTMLAttributes<HTMLDivElement> }): React.JSX.Element {

  var binding = new ReadonlyBinding(p.text, "");

  return (
    <div className="html-viewer" >
        <HtmlEditor readOnly
          binding={binding}
          htmlAttributes={p.htmlAttributes}
          toolbarButtons={c => null} plugins={[
          ]} />
    </div>
  );
}

export default function TextPart(p: { ctx: TypeContext<TextPartEntity> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  const [isPreview, setIsPreview] = useIsPreview(p.ctx.value);

  const [textPartType, setTextPartType] = React.useState<TextPartType>("Text");

  function useIsPreview(entity: Entity): [boolean, (val: boolean) => void] {

    const [isPreview, setIsPreview] = React.useState(!entity.isNew);
    

    React.useEffect(() => {
      if (isPreview != !entity.isNew)
        setIsPreview(!entity.isNew);
    }, [entity]);

    return [isPreview, setIsPreview]
  } 

  function onChangeTextPartType() {
    setTextPartType(ctx.value.textPartType);
  }

  function EditType(): React.JSX.Element {
    if (textPartType == "Text")
      return (<AutoLine ctx={ctx.subCtx(s => s.textContent)} />)

    if (textPartType == "Markdown")
      return (<AutoLine ctx={ctx.subCtx(s => s.textContent)} />)

    if (textPartType == "HTML")
      return (<HtmlEditor binding={Binding.create(ctx.value, e => e.textContent)} />)

    return (<AutoLine ctx={ctx.subCtx(s => s.textContent)} />)
  }


  function PreviewType(): React.JSX.Element {
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
      <a href="#" onClick={e => { e.preventDefault(); setIsPreview(!isPreview); }} >
        <FontAwesomeIcon icon={isPreview ? "edit" : "eye"} className="me-1" />{isPreview ? "Edit" : "Preview"}
      </a>
    </div>
    <div className="row">
      <div className="col-sm-4">
        <AutoLine ctx={ctx.subCtx(s => s.textPartType)} onChange={() => onChangeTextPartType() } />
      </div>
    </div>
    <div className="form-inline">
      {isPreview ?
        <PreviewType /> :
        <EditType />
      }
    </div>
  </div>
  );
}

