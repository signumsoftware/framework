import * as React from 'react'
import { Tab, Tabs } from 'react-bootstrap'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, getMixin } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { WordTemplateTableSourceEntity, WordTemplateMessage } from '../Signum.Entities.Word'
import { TemplateTokenMessage } from '../../Templating/Signum.Entities.Templating'
import FileLine from '../../Files/FileLine'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import TemplateControls from '../../Templating/TemplateControls'
import ValueLineModal from '../../../../Framework/Signum.React/Scripts/ValueLineModal'

export default class WordTemplateTableSource extends React.Component<{ ctx: TypeContext<WordTemplateTableSourceEntity> }, void> {

    handleOnChange = () => {
        var entity = this.props.ctx.value;
        entity.key = entity.source && entity.source.toStr;
        this.forceUpdate();
    }

    render() {
        let ec = this.props.ctx;

        return (
            <div>
                <EntityLine ctx={ec.subCtx(f => f.source)} helpBlock={WordTemplateMessage.SelectTheSourceOfDataForYourTableOrChart.niceToString()} onChange={this.handleOnChange} />
                {ec.value.source && <ValueLine ctx={ec.subCtx(f => f.key)} helpBlock={WordTemplateMessage.WriteThisKeyAsTileInTheAlternativeTextOfYourTableOrChart.niceToString()} />}
            </div>
        );
    }
}
