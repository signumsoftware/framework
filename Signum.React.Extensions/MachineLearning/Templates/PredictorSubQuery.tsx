import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTable } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../Files/FileLine'
import { PredictorSubQueryEntity, PredictorColumnEmbedded, PredictorGroupKeyEmbedded, PredictorEntity, PredictorMainQueryEmbedded } from '../Signum.Entities.MachineLearning'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { API } from '../PredictorClient';
import FilterBuilderEmbedded from './FilterBuilderEmbedded';
import { TypeReference } from '../../../../Framework/Signum.React/Scripts/Reflection';

export default class PredictorSubQuery extends React.Component<{ ctx: TypeContext<PredictorSubQueryEntity>, targetType: TypeReference, mainQuery: PredictorMainQueryEmbedded }> {

    handleOnChange = () => {
        const e = this.props.ctx.value;
        e.additionalFilters = [];
        e.groupKeys = [];
        e.aggregates = [];
        this.forceUpdate();
    }

    render() {
        const ctx = this.props.ctx;
        const ctxxs = ctx.subCtx({ formGroupSize: "ExtraSmall" });
        const entity = ctx.value;
        const queryKey = entity.query && entity.query.key;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(f => f.name)} onTextboxBlur={() => ctx.findParentCtx(PredictorEntity).frame!.entityComponent.forceUpdate()} />
                <EntityLine ctx={ctx.subCtx(f => f.query)} remove={ctx.value.isNew} onChange={this.handleOnChange} />
                {queryKey &&
                    <div>
                        <FilterBuilderEmbedded ctx={ctxxs.subCtx(a => a.additionalFilters)} queryKey={queryKey}
                            subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />
                        <EntityTable ctx={ctxxs.subCtx(e => e.groupKeys)} columns={EntityTable.typedColumns<PredictorGroupKeyEmbedded>([
                            {
                                header: "Token",
                                template: (qctx, row) => <QueryTokenEntityBuilder
                                    ctx={qctx.subCtx(a => a.token)}
                                    queryKey={this.props.ctx.value.query!.key}
                                    subTokenOptions={SubTokensOptions.CanElement}
                                    helpBlock={row.props.index == 0 ? "The first groupKey must be of type " + Finder.getTypeNiceName(this.props.targetType) : undefined}
                                />,
                                headerHtmlAttributes: { style: { width: "100%" } },
                            },
                        ])} />

                    <EntityTable ctx={ctxxs.subCtx(e => e.aggregates)} columns={EntityTable.typedColumns<PredictorColumnEmbedded>([
                            { property: a => a.usage },
                            {
                                property: a => a.token,
                                template: cctx => <QueryTokenEntityBuilder
                                    ctx={cctx.subCtx(a => a.token)}
                                    queryKey={this.props.ctx.value.query!.key}
                                    subTokenOptions={SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />,
                                headerHtmlAttributes: { style: { width: "60%" } },
                            },
                            { property: a => a.encoding },
                        ])} />

                    </div>}
            </div>
        );
    }
}
