import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../../../Extensions/Signum.React.Extensions/Files/FileLine'
import { PredictorEntity, PredictorInputEntity, PredictorOutputEntity } from '../Signum.Entities.MachineLearning'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'

export default class Predictor extends React.Component<{ ctx: TypeContext<PredictorEntity> }> {

    handleOnChange = () => {
        const e = this.props.ctx.value;
        e.filters = [];
        e.inputs = [];
        e.output = [];
        this.forceUpdate();
    }

    render() {

        const ctx = this.props.ctx;
        const entity = ctx.value;
        const queryKey = entity.query && entity.query.key;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(e => e.name)} />
                <EntityLine ctx={ctx.subCtx(f => f.query)} remove={ctx.value.isNew} onChange={this.handleOnChange} />
                {queryKey && <div>
                    {
                        Finder.isFindable(queryKey, false) ?
                            <a className="form-control-static" href={Finder.findOptionsPath({ queryName: queryKey })}>{getQueryNiceName(queryKey)}</a> :
                            <span>{getQueryNiceName(queryKey)}</span>
                    }

                    <div className="form-xs">
                        <div className="repeater-inline form-inline sf-filters-list ">
                            <EntityRepeater ctx={ctx.subCtx(e => e.filters)} getComponent={this.renderFilter} />
                        </div>
                    </div>
                    <div className="form-xs">
                        <div className="repeater-inline form-inline sf-filters-list ">
                            <EntityRepeater ctx={ctx.subCtx(e => e.inputs)} getComponent={this.renderInput} />
                        </div>
                    </div>
                    <div className="form-xs">
                        <div className="repeater-inline form-inline sf-filters-list ">
                            <EntityRepeater ctx={ctx.subCtx(e => e.output)} getComponent={this.renderOutput} />
                        </div>
                    </div>
                </div>}
            </div>
        );
    }

    renderFilter = (ctx: TypeContext<QueryFilterEmbedded>) => {
        const ctx2 = ctx.subCtx({ formGroupStyle: "None" });
        return (
            <div>
                <QueryTokenEntityBuilder
                    ctx={ctx2.subCtx(a => a.token, { formGroupStyle: "None" })}
                    queryKey={this.props.ctx.value.query!.key}
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />
                <span style={{ margin: "0px 10px" }}>
                    <ValueLine ctx={ctx2.subCtx(e => e.operation)} />
                </span>
                <ValueLine ctx={ctx2.subCtx(e => e.valueString)} valueHtmlAttributes={{ size: 50 }} />
            </div>
        );
    }

    renderInput = (ctx: TypeContext<PredictorInputEntity>) => {
        const ctx2 = ctx.subCtx({ formGroupStyle: "None" });
        return (
            <div>
                <QueryTokenEntityBuilder
                    ctx={ctx2.subCtx(a => a.token, { formGroupStyle: "None" })}
                    queryKey={this.props.ctx.value.query!.key}
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />
            </div>
        );
    }

    renderOutput = (ctx: TypeContext<PredictorOutputEntity>) => {
        const ctx2 = ctx.subCtx({ formGroupStyle: "None" });
        return (
            <div>
                <QueryTokenEntityBuilder
                    ctx={ctx2.subCtx(a => a.token, { formGroupStyle: "None" })}
                    queryKey={this.props.ctx.value.query!.key}
                    subTokenOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate} />
            </div>
        );
    }
}

