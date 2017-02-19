import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { getMixin } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ColorTypeaheadLine } from '../../Basics/Templates/ColorTypeahead'
import { CaseTagEntity } from '../Signum.Entities.Workflow'
import {
    ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip,
    EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, EntityTable
} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, ValueSearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { Color } from '../../Basics/Color'

require("!style!css!./Tag.css");


export default class Tag extends React.Component<{ tag: CaseTagEntity }, void> {
    render() {
        const tag = this.props.tag;
        var color = Color.parse(tag.color!) || Color.Black;

        return (
            <span className="case-tag" style={{
                color: color.opositePole().toString(),
                borderColor: color.lerp(0.5, Color.Black).toString(),
                backgroundColor: color.toString(),
            }} title={tag.name || ""}>{tag.name}</span>
        );
    }
}