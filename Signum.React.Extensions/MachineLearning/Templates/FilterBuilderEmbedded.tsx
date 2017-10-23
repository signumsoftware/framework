import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityDetail, EntityCombo, EntityList, EntityRepeater, EntityTable, IRenderButtons } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, FilterOptionParsed } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle, ButtonsContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import FileLine from '../../../../Extensions/Signum.React.Extensions/Files/FileLine'
import { PredictorEntity, PredictorColumnEmbedded, PredictorMessage, PredictorMultiColumnEntity } from '../Signum.Entities.MachineLearning'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryTokenEmbedded} from '../../UserAssets/Signum.Entities.UserAssets'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { API, initializers } from '../PredictorClient';
import { toLite } from "../../../../Framework/Signum.React/Scripts/Signum.Entities";
import FilterBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/FilterBuilder';
import { MList, newMListElement } from '../../../../Framework/Signum.React/Scripts/Signum.Entities';
import { TokenCompleter } from '../../../../Framework/Signum.React/Scripts/Finder';


interface FilterBuilderEmbeddedProps {
    ctx: TypeContext<MList<QueryFilterEmbedded>>;
    queryKey: string;
    subTokenOptions: SubTokensOptions;
    onChanged?: () => void;
}

interface FilterBuilderEmbeddedState {
    filterOptions?: FilterOptionParsed[];
    queryDescription?: QueryDescription;
}

export default class FilterBuilderEmbedded extends React.Component<FilterBuilderEmbeddedProps, FilterBuilderEmbeddedState> {

    constructor(props: FilterBuilderEmbeddedProps) {
        super(props);
        this.state = {
            filterOptions: undefined,
            queryDescription: undefined
        };
    }

    componentWillMount() {
        this.loadData(this.props).done()
    }

    componentWillReceiveProps(newProps: FilterBuilderEmbeddedProps) {
        if (newProps.ctx.value != this.props.ctx.value || newProps.queryKey != this.props.queryKey)
            this.setState({ queryDescription: undefined, filterOptions: undefined }, () => {
                this.loadData(newProps).done();
            });
    }

    async loadData(props: FilterBuilderEmbeddedProps): Promise<void> {

        var qd = await Finder.getQueryDescription(this.props.queryKey);

        const completer = new TokenCompleter(this.state.queryDescription!);

        props.ctx.value.forEach(mle => {
            if (mle.element.token != null && mle.element.token.token == null)
                completer.request(mle.element.token.tokenString, this.props.subTokenOptions);
        });
        
        await completer.finished();

        const filterOptions = props.ctx.value.map(mle =>
        ({
            token: mle.element.token && (mle.element.token.token || completer.get(mle.element.token.tokenString)),
            operation: mle.element.operation,
            value: mle.element.valueString
        }) as FilterOptionParsed);

        await Finder.parseFilterValues(filterOptions);

        this.setState({ queryDescription: qd, filterOptions: filterOptions });
    }

    render() {
        return (
            <div>
                {
                    this.state.queryDescription != null &&
                    <FilterBuilder
                        title={this.props.ctx.niceName()}
                        queryDescription={this.state.queryDescription}
                        filterOptions={this.state.filterOptions || []}
                        subTokensOptions={this.props.subTokenOptions}
                        onFiltersChanged={this.handleFiltersChanged} />
                }
            </div>
        );
    }

    handleFiltersChanged = () => {

        var ctx = this.props.ctx;

        ctx.value.clear();

        ctx.value.push(...this.state.filterOptions!.filter(a => a.token != null).map(a => newMListElement(QueryFilterEmbedded.New({
            token: a.token && QueryTokenEmbedded.New({ token: a.token, tokenString: a.token.fullKey }),
            operation: a.operation,
            valueString: Finder.Encoder.stringValue(a.value)
        }))));

        ctx.binding.setValue(ctx.value); //force change 

        if (this.props.onChanged)
            this.props.onChanged();

        this.forceUpdate();
    } 
}
