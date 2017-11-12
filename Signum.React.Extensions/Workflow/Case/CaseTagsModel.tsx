import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { getMixin } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ColorTypeaheadLine } from '../../Basics/Templates/ColorTypeahead'
import { CaseTagsModel, CaseTagTypeEntity } from '../Signum.Entities.Workflow'
import {
    ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip,
    EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, EntityTable
} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, ValueSearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
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