import * as React from 'react'
import { CaseTagsModel, CaseTagTypeEntity } from '../Signum.Entities.Workflow'
import { EntityStrip, TypeContext } from '@framework/Lines'
import Tag from './Tag'

export default class CaseTagsModelComponent extends React.Component<{ ctx: TypeContext<CaseTagsModel> }> {
  render() {
    var ctx = this.props.ctx;
    return (
      <EntityStrip ctx={ctx.subCtx(a => a.caseTags)}
        onItemHtmlAttributes={tag => ({ style: { textDecoration: "none" } })}
        onRenderItem={tag => <Tag tag={tag as CaseTagTypeEntity} />}
      />
    );
  }
}
