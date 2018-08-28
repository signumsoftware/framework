import * as React from 'react'
import * as moment from 'moment'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityDetail, EntityCombo, EntityList, EntityRepeater, EntityTable, IRenderButtons } from '@framework/Lines'
import { SearchControl, FilterOptionParsed } from '@framework/Search'
import { TypeContext, FormGroupStyle, ButtonsContext } from '@framework/TypeContext'
import FileLine from '../../Files/FileLine'
import { PredictorEntity, PredictorColumnEmbedded, PredictorMessage, PredictorSubQueryEntity } from '../Signum.Entities.MachineLearning'
import * as Finder from '@framework/Finder'
import { getQueryNiceName } from '@framework/Reflection'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import { QueryTokenEmbedded} from '../../UserAssets/Signum.Entities.UserAssets'
import { QueryFilterEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryDescription, SubTokensOptions, isFilterGroupOptionParsed } from '@framework/FindOptions'
import { API, initializers } from '../PredictorClient';
import { toLite } from "@framework/Signum.Entities";
import FilterBuilder from '@framework/SearchControl/FilterBuilder';
import { MList, newMListElement } from '@framework/Signum.Entities';
import { TokenCompleter } from '@framework/Finder';


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

        var filterOptions = await FilterBuilderEmbedded.toFilterOptionParsed(qd, this.props.ctx.value, this.props.subTokenOptions);

        this.setState({ queryDescription: qd, filterOptions: filterOptions });
    }

    static async toFilterOptionParsed(qd: QueryDescription, filters: MList<QueryFilterEmbedded>, subTokenOptions: SubTokensOptions): Promise<FilterOptionParsed[]> {
        const completer = new TokenCompleter(qd);

        filters.forEach(mle => {
            if (mle.element.token != null && mle.element.token.token == null)
                completer.request(mle.element.token.tokenString, subTokenOptions);
        });

        await completer.finished();


        const filterOptions = filters.map(mle => {

            const token = mle.element.token && (mle.element.token.token || completer.get(mle.element.token.tokenString))

            const valueString = token && token.filterType == "DateTime" ? moment(mle.element.valueString!, serverFormat).format() : mle.element.valueString;

            return ({
                token: token,
                operation: mle.element.operation,
                value: mle.element.valueString
            }) as FilterOptionParsed;
        });

        await Finder.parseFilterValues(filterOptions);

        return filterOptions;
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
                        readOnly={this.props.ctx.readOnly}
                        onFiltersChanged={this.handleFiltersChanged} />
                }
            </div>
        );
    }

    handleFiltersChanged = () => {

        var ctx = this.props.ctx;

        ctx.value.clear();


        function pushFilter(fo: FilterOptionParsed, identation: number) {
            if (isFilterGroupOptionParsed(fo)) {
                ctx.value.push(newMListElement(QueryFilterEmbedded.New({
                    isGroup: true,
                    indentation: identation,
                    groupOperation: fo.groupOperation,
                    token: fo.token && QueryTokenEmbedded.New({ token: fo.token, tokenString: fo.token.fullKey })
                })));

                fo.filters.forEach(f => pushFilter(f, identation + 1));
            } else {

                const valueString = Finder.Encoder.stringValue(fo.token && fo.token.filterType == "DateTime" ? moment(fo.value).format(serverFormat) : fo.value);

                ctx.value.push(newMListElement(QueryFilterEmbedded.New({
                    token: fo.token && QueryTokenEmbedded.New({ token: fo.token, tokenString: fo.token.fullKey }),
                    operation: fo.operation,
                    valueString: valueString,
                })));
            }
        }

        this.state.filterOptions!.forEach(fo => pushFilter(fo, 0))

        ctx.binding.setValue(ctx.value); //force change 

        if (this.props.onChanged)
            this.props.onChanged();

        this.forceUpdate();
    } 
}

const serverFormat = "YYYY/MM/DD hh:mm:ss";