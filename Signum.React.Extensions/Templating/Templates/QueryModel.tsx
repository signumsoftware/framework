import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '@framework/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '@framework/FindOptions'
import { SearchControl } from '@framework/Search'
import { getToString, getMixin } from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { TemplateTokenMessage, QueryModelMessage, QueryModel } from '../../Templating/Signum.Entities.Templating'
import FileLine from '../../Files/FileLine'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import TemplateControls from '../../Templating/TemplateControls'
import ValueLineModal from '@framework/ValueLineModal'


interface QueryModelComponentProps {
    ctx: TypeContext<QueryModel>
}

export default class QueryModelComponent extends React.Component<QueryModelComponentProps> {
    
    handleOnSearch = () => {
        const qr = this.searchControl.searchControlLoaded!.getQueryRequest();
        const model = this.props.ctx.value;
        model.filters = qr.filters;
        model.orders = qr.orders;
        model.pagination = qr.pagination;
        model.modified = true;
    }

    searchControl!: SearchControl;
    render() {
        const ctx = this.props.ctx;
        return (
            <div>
                <p>{QueryModelMessage.ConfigureYourQueryAndPressSearchBeforeOk.niceToString()}</p>
                <SearchControl ref={sc => this.searchControl = sc!}
                    hideButtonBar={true}
                    showContextMenu="Basic"
                    allowSelection={false}
                    findOptions={{ queryName: ctx.value.queryKey }}
                    onSearch={this.handleOnSearch} />
            </div>
        );
    }
}
