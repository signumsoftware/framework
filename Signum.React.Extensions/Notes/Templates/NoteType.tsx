import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '@framework/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '@framework/FindOptions'
import { SearchControl } from '@framework/Search'
import { getToString, getMixin } from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { NoteTypeEntity } from '../Signum.Entities.Notes'
import { TemplateTokenMessage } from '../../Templating/Signum.Entities.Templating'
import FileLine from '../../Files/FileLine'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import TemplateControls from '../../Templating/TemplateControls'
import ValueLineModal from '@framework/ValueLineModal'

export default class NoteType extends React.Component<{ ctx: TypeContext<NoteTypeEntity> }> {

    render() {

        const e = this.props.ctx;

        const ec = e.subCtx({ labelColumns: { sm: 2 } });
        const sc = ec.subCtx({ formGroupStyle: "Basic" });


        return (
            <div>
                <ValueLine ctx={ec.subCtx(n => n.name)} />
            </div>
        );
    }
}
