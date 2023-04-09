import * as React from 'react'
import { ValueLine, EntityLine, EntityCombo, EntityDetail, EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import ValueLineModal from '@framework/ValueLineModal'
import { useForceUpdate } from '@framework/Hooks'
import { WordTemplateEntity } from '../Signum.Word'
import { TemplateApplicableEval } from '../../Templating/Signum.Entities.Templating'
import { FileLine } from '../../Files/FileLine'
import TemplateControls from '../../Templating/TemplateControls'
import TemplateApplicable from '../../Templating/Templates/TemplateApplicable'
import { QueryOrderEmbedded } from '../../Signum.UserQueries/Signum.Entities.UserQueries'
import FilterBuilderEmbedded from '../../UserAssets/Templates/FilterBuilderEmbedded'
import { Tabs, Tab } from 'react-bootstrap';
import CollapsableCard from '../../Basics/Templates/CollapsableCard'
import { SubTokensOptions } from '@framework/FindOptions'
import QueryTokenEmbeddedBuilder from '../../UserAssets/Templates/QueryTokenEmbeddedBuilder'

export default function WordTemplate(p: { ctx: TypeContext<WordTemplateEntity> }) {

  const forceUpdate = useForceUpdate();

  const ctx = p.ctx;
  const ctx4 = p.ctx.subCtx({ labelColumns: 4 });
  const canAggregate = ctx.value.groupResults ? SubTokensOptions.CanAggregate : 0;

  return (
    <div>

      <div className="row">
        <div className="col-sm-6">
          <ValueLine ctx={ctx4.subCtx(f => f.name)} />
          <EntityLine ctx={ctx4.subCtx(f => f.query)} onChange={forceUpdate} />
          <EntityCombo ctx={ctx4.subCtx(f => f.model)} />

        </div>
        <div className="col-sm-6">
          <EntityCombo ctx={ctx4.subCtx(f => f.wordTransformer)} />
          <EntityCombo ctx={ctx4.subCtx(f => f.wordConverter)} />
          <EntityCombo ctx={ctx4.subCtx(f => f.culture)} />
        </div>
      </div>

      {ctx.value.query &&
        <Tabs id={ctx.prefix + "tabs"} mountOnEnter={true}>
          <Tab eventKey="template" title={ctx.niceName(a => a.template)}>
            <ValueLine ctx={ctx.subCtx(f => f.fileName)} />
            <div className="card form-xs" style={{ marginTop: "10px", marginBottom: "10px" }}>
            <div className="card-header" style={{ padding: "5px" }}>
              <TemplateControls queryKey={ctx.value.query.key} forHtml={false} widgetButtons={true} />
              </div>
            </div>
            <FileLine ctx={ctx.subCtx(e => e.template)} />
          </Tab>
          <Tab eventKey="query" title={<span style={{ fontWeight: ctx.value.groupResults || ctx.value.filters.length > 0 || ctx.value.orders.length ? "bold" : undefined }}>
            {ctx.niceName(a => a.query)}
          </span>}>
            <div className="row">
              <div className="col-sm-4">
                <ValueLine ctx={ctx.subCtx(e => e.disableAuthorization)} inlineCheckbox />
              </div>
              <div className="col-sm-4">
                <ValueLine ctx={ctx.subCtx(e => e.groupResults)} inlineCheckbox onChange={forceUpdate} />
              </div>
              <div className="col-sm-4">
              </div>
            </div>
            <FilterBuilderEmbedded ctx={ctx.subCtx(e => e.filters)} onChanged={forceUpdate}
              subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate}
              queryKey={ctx.value.query!.key}/>
            <EntityTable ctx={ctx.subCtx(e => e.orders)} onChange={forceUpdate} columns={EntityTable.typedColumns<QueryOrderEmbedded>([
              {
                property: a => a.token,
                template: ctx => <QueryTokenEmbeddedBuilder
                  ctx={ctx.subCtx(a => a.token, { formGroupStyle: "SrOnly" })}
                  queryKey={p.ctx.value.query!.key}
                  subTokenOptions={SubTokensOptions.CanElement | canAggregate} />
              },
              { property: a => a.orderType }
            ])} />
          </Tab>
          <Tab eventKey="applicable" title={
            <span style={{ fontWeight: ctx.value.applicable ? "bold" : undefined }}>
              {ctx.niceName(a => a.applicable)}
            </span>}>
            <EntityDetail ctx={ctx.subCtx(e => e.applicable)} onChange={forceUpdate}
              getComponent={(ctx2: TypeContext<TemplateApplicableEval>) => <TemplateApplicable ctx={ctx2} query={ctx.value.query!} />} />
          </Tab>
        </Tabs>
      }
    </div>
  );
}
