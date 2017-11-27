import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTable } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, FindOptions, FilterOption, ColumnOption } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../Files/FileLine'
import { PredictorSubQueryEntity, PredictorColumnEmbedded, PredictorGroupKeyEmbedded, PredictorEntity, PredictorMainQueryEmbedded, PredictorMessage } from '../Signum.Entities.MachineLearning'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { API } from '../PredictorClient';
import FilterBuilderEmbedded from './FilterBuilderEmbedded';
import { TypeReference } from '../../../../Framework/Signum.React/Scripts/Reflection';
import { initializeColumn } from './Predictor';
import { newMListElement } from '../../../../Framework/Signum.React/Scripts/Signum.Entities';
import { is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities';

export default class PredictorSubQuery extends React.Component<{ ctx: TypeContext<PredictorSubQueryEntity>, mainQuery: PredictorMainQueryEmbedded, mainQueryDescription: QueryDescription }> {

    handleOnChange = () => {
        const e = this.props.ctx.value;
        e.additionalFilters = [];
        e.groupKeys = [];
        e.aggregates = [];
        this.forceUpdate();
    }

    handlePreviewSubQuery = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        e.persist();

        var sq = this.props.ctx.value;


        

        Finder.getQueryDescription(sq.query!.key).then(sqd =>
            FilterBuilderEmbedded.toFilterOptionParsed(sqd!, (this.getMainFilters(sqd) || []).concat(sq.additionalFilters), SubTokensOptions.CanElement | SubTokensOptions.CanAggregate)
                .then(filters => {
                    var fo: FindOptions = {
                        queryName: sq.query!.key,
                        groupResults: true,
                        filterOptions: filters.map(f => ({
                            columnName: f.token!.fullKey,
                            operation: f.operation,
                            value: f.value
                        }) as FilterOption),
                        columnOptions: [{ columnName: "Count" } as ColumnOption]
                                .concat(sq.groupKeys.map(mle => ({ columnName: mle.element.token && mle.element.token.tokenString, } as ColumnOption)))
                                .concat(sq.aggregates.orderBy(mle => mle.element.usage == "Input" ? 0 : 1).map(mle => ({ columnName: mle.element.token && mle.element.token.tokenString, } as ColumnOption))),
                        columnOptionsMode: "Replace",
                    };

                    Finder.exploreWindowsOpen(fo, e);
                }))
            .done();
    }

    getMainFilters(sqd: QueryDescription) {
        const mq = this.props.mainQuery;
        const sq = this.props.ctx.value;
        if (is(mq.query, this.props.ctx.value.query))
            return mq.filters;


        if (sq.groupKeys.length == 0)
            return null;

        var firstKey = sq.groupKeys[0].element.token

        var prefix = firstKey && firstKey.token && firstKey.token.fullKey;

        if (prefix == null)
            return null;
        
        return this.props.mainQuery.filters.map(f => newMListElement(QueryFilterEmbedded.New(
            {
                token: f.element.token && QueryTokenEmbedded.New({ tokenString: prefix + "." + f.element.token.tokenString }),
                operation : f.element.operation,
                valueString: f.element.valueString,
            })));
    }

    render() {
        const ctx = this.props.ctx;
        const ctxxs = ctx.subCtx({ formGroupSize: "ExtraSmall" });
        const entity = ctx.value;
        const queryKey = entity.query && entity.query.key;
        const targetType = this.props.mainQueryDescription.columns["Entity"].type;

        var parentCtx = ctx.findParentCtx(PredictorEntity);

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(f => f.name)} onTextboxBlur={() => parentCtx.frame!.entityComponent.forceUpdate()} />
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
                                    helpBlock={row.props.index == 0 ? "The first groupKey must be of type " + Finder.getTypeNiceName(targetType) : undefined}
                                />,
                                headerHtmlAttributes: { style: { width: "100%" } },
                            },
                        ])} />

                        <EntityTable ctx={ctxxs.subCtx(e => e.aggregates)} columns={EntityTable.typedColumns<PredictorColumnEmbedded>([
                            { property: a => a.usage },
                            {
                                property: a => a.token,
                                template: (cctx, row) => <QueryTokenEntityBuilder
                                    ctx={cctx.subCtx(a => a.token)}
                                    queryKey={this.props.ctx.value.query!.key}
                                    subTokenOptions={SubTokensOptions.CanElement | SubTokensOptions.CanAggregate}
                                    onTokenChanged={qt => { initializeColumn(parentCtx.value, qt, cctx.value); row.forceUpdate() }}/>,
                                headerHtmlAttributes: { style: { width: "60%" } },
                            },
                            { property: a => a.encoding },
                            { property: a => a.nullHandling },
                        ])} />
                        {ctx.value.query && <a href="#" onClick={this.handlePreviewSubQuery}>{PredictorMessage.Preview.niceToString()}</a>}
                    </div>}
            </div>
        );
    }
}
