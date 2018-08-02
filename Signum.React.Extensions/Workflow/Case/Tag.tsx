import * as React from 'react'
import { Dic } from '@framework/Globals'
import { getMixin } from '@framework/Signum.Entities'
import { ColorTypeaheadLine } from '../../Basics/Templates/ColorTypeahead'
import { CaseTagTypeEntity } from '../Signum.Entities.Workflow'
import {
    ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip,
    EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, EntityTable
} from '@framework/Lines'
import { SearchControl, ValueSearchControl } from '@framework/Search'
import { Color } from '../../Basics/Color'

import "./Tag.css"


export default class Tag extends React.Component<{ tag: CaseTagTypeEntity }> {
    render() {
        const tag = this.props.tag;
        var color = Color.tryParse(tag.color!) || Color.Black;

        return (
            <span className="case-tag" style={{
                color: color.opositePole().toString(),
                borderColor: color.lerp(0.5, Color.Black).toString(),
                backgroundColor: color.toString(),
            }} title={tag.name || ""}>{tag.name}</span>
        );
    }
}