import * as React from 'react'
import { ValueLine, EntityLine, EntityCombo, EntityDetail } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { WordTemplateEntity } from '../Signum.Entities.Word'
import { TemplateApplicableEval } from '../../Templating/Signum.Entities.Templating'
import { FileLine } from '../../Files/FileLine'
import TemplateControls from '../../Templating/TemplateControls'
import TemplateApplicable from '../../Templating/Templates/TemplateApplicable'
import ValueLineModal from '@framework/ValueLineModal'

export default function WordTemplate(p : { ctx: TypeContext<WordTemplateEntity> }){
  function handleOnInsert(newCode: string) {
    ValueLineModal.show({
      type: { name: "string" },
      initialValue: newCode,
      title: "Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    }).done();
  }

  const ctx = p.ctx;
  const sc = ctx.subCtx({ formGroupStyle: "Basic" });

  return (
    <div>
      <ValueLine ctx={ctx.subCtx(f => f.name)} />
      <EntityLine ctx={ctx.subCtx(f => f.query)} />
      <EntityCombo ctx={ctx.subCtx(f => f.model)} />
      <EntityCombo ctx={ctx.subCtx(f => f.culture)} />
      <EntityCombo ctx={ctx.subCtx(f => f.wordTransformer)} />
      <EntityCombo ctx={ctx.subCtx(f => f.wordConverter)} />
      <ValueLine ctx={ctx.subCtx(f => f.fileName)} />
      <ValueLine ctx={ctx.subCtx(f => f.disableAuthorization)} />

      {sc.value.query &&
        <div>
          <div>
            <div className="card form-xs" style={{ marginTop: "10px", marginBottom: "10px" }}>
              <div className="card-header" style={{ padding: "5px" }}>
                <TemplateControls queryKey={sc.value.query.key} forHtml={false} onInsert={handleOnInsert} />
              </div>
            </div>
            <FileLine ctx={ctx.subCtx(e => e.template)} />
            <EntityDetail ctx={ctx.subCtx(e => e.applicable)}
              getComponent={(ctx2: TypeContext<TemplateApplicableEval>) => <TemplateApplicable ctx={ctx2} query={sc.value.query!} />} />
          </div>
        </div>
      }
    </div>
  );
}
