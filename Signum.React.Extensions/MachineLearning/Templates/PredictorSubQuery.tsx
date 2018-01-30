import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityTable } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, FindOptions, FilterOption, ColumnOption } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../Files/FileLine'
import { PredictorSubQueryEntity, PredictorSubQueryColumnEmbedded, PredictorEntity, PredictorMainQueryEmbedded, PredictorMessage, PredictorSubQueryColumnUsage } from '../Signum.Entities.MachineLearning'
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
        e.filters = [];
        e.columns = [];
        this.forceUpdate();
    }

    handlePreviewSubQuery = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        e.persist();

        var sq = this.props.ctx.value;

        Finder.getQueryDescription(sq.query!.key).then(sqd =>
            FilterBuilderEmbedded.toFilterOptionParsed(sqd!, (this.getMainFilters(sqd) || []).concat(sq.filters), SubTokensOptions.CanElement | SubTokensOptions.CanAggregate)
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
                            .concat(sq.columns.map(mle => ({ columnName: mle.element.token && mle.element.token.tokenString, } as ColumnOption))),
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

        var parentKey = sq.columns.singleOrNull(a => a.element.usage == "ParentKey");

        if (parentKey == null)
            return null;

        var t = parentKey.element.token;

        var prefix = t && t.token && t.token.fullKey;

        if (prefix == null)
            return null;

        return this.props.mainQuery.filters.map(f => newMListElement(QueryFilterEmbedded.New(
            {
                token: f.element.token && QueryTokenEmbedded.New({ tokenString: prefix + "." + f.element.token.tokenString }),
                operation: f.element.operation,
                valueString: f.element.valueString,
            })));
    }

    handleChangeUsage = (ctx: TypeContext<PredictorSubQueryColumnEmbedded>) => {

        var col = ctx.value;
        if (isInputOutput(ctx.value.usage)) {
            initializeColumn(this.props.ctx.findParent(PredictorEntity), col);
        } else {
            col.encoding = null;
            col.nullHandling = null;
        }

        this.forceUpdate();
    }

    render() {
        const ctx = this.props.ctx;
        const ctxxs = ctx.subCtx({ formSize: "ExtraSmall" });
        const entity = ctx.value;
        const queryKey = entity.query && entity.query.key;
        const targetType = this.props.mainQueryDescription.columns["Entity"].type;

        const parentCtx = ctx.findParentCtx(PredictorEntity);

        var mq = parentCtx.value.mainQuery;

        var tokens = mq.groupResults ? mq.columns.map(mle => mle.element.token).filter(t => t != null && t.token != null && t.token.queryTokenType != "Aggregate").map(t => t!.token!.niceTypeName) :
            [this.props.mainQueryDescription.columns["Entity"].niceTypeName];

        var parentKeyColumns = ctx.value.columns.map(mle => mle.element).filter(col => col.usage == "ParentKey");

        var getParentKeyMessage = (col: PredictorSubQueryColumnEmbedded) => {
            var index = parentKeyColumns.indexOf(col);
            if (index == -1)
                return undefined;

            if (index < tokens.length)
                return PredictorMessage.ShouldBeOfType0.niceToString(tokens[index]);

            return PredictorMessage.TooManyParentKeys.niceToString();
        };


        return (
            <div>
                <ValueLine ctx={ctx.subCtx(f => f.name)} onTextboxBlur={() => parentCtx.frame!.entityComponent!.forceUpdate()} />
                <EntityLine ctx={ctx.subCtx(f => f.query)} remove={ctx.value.isNew} onChange={this.handleOnChange} />
                {queryKey &&
                    <div>
                        <FilterBuilderEmbedded ctx={ctxxs.subCtx(a => a.filters)} queryKey={queryKey}
                            subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />
                        <EntityTable ctx={ctxxs.subCtx(e => e.columns)} columns={EntityTable.typedColumns<PredictorSubQueryColumnEmbedded>([
                            {
                                property: a => a.usage, template: colCtx => <ValueLine ctx={colCtx.subCtx(a => a.usage)} onChange={() => this.handleChangeUsage(colCtx)} />
                            },
                            {
                                property: a => a.token,
                                template: colCtx => <QueryTokenEntityBuilder
                                    ctx={colCtx.subCtx(a => a.token)}
                                    queryKey={this.props.ctx.value.query!.key}
                                    subTokenOptions={SubTokensOptions.CanElement | SubTokensOptions.CanAggregate}
                                    onTokenChanged={() => this.handleChangeUsage(colCtx)}
                                    helpText={getParentKeyMessage(colCtx.value)}
                                />,
                                headerHtmlAttributes: { style: { width: "50%" } },
                            },
                            { property: a => a.encoding, template: colCtx => isInputOutput(colCtx.value.usage) && <ValueLine ctx={colCtx.subCtx(a => a.encoding)} /> },
                            { property: a => a.nullHandling, template: colCtx => isInputOutput(colCtx.value.usage) && <ValueLine ctx={colCtx.subCtx(a => a.nullHandling)} /> },
                        ])} />

                        {ctx.value.query && <a href="#" onClick={this.handlePreviewSubQuery}>{PredictorMessage.Preview.niceToString()}</a>}
                    </div>}
            </div>
        );
    }
}

function isInputOutput(usage: PredictorSubQueryColumnUsage | undefined): boolean {
    return usage == "Input" || usage == "Output";
}
