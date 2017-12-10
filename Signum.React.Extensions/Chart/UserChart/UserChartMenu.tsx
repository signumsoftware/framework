
import * as React from 'react'
import { DropdownButton, MenuItem, } from 'react-bootstrap'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { Lite, toLite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite, is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import SearchControl from '../../../../Framework/Signum.React/Scripts/SearchControl/SearchControl'
import { UserChartEntity, ChartRequest, ChartMessage } from '../Signum.Entities.Chart'
import * as UserChartClient from './UserChartClient'
import ChartRequestView from '../Templates/ChartRequestView'

export interface UserChartMenuProps {
    chartRequestView: ChartRequestView;
}

export default class UserChartMenu extends React.Component<UserChartMenuProps, { userCharts?: Lite<UserChartEntity>[] }> {

    constructor(props: UserChartMenuProps) {
        super(props);
        this.state = { };
    }


    handleSelectedToggle = (isOpen: boolean) => {

        if (isOpen && this.state.userCharts == undefined)
            this.reloadList().done();
    }

    reloadList(): Promise<void> {
        return UserChartClient.API.forQuery(this.props.chartRequestView.props.chartRequest!.queryKey)
            .then(list => this.setState({ userCharts: list }));
    }

    componentWillMount() {
        this.loadString();
    }

    componentWillUpdate() {
        this.loadString();
    }

    loadString() {
        var uc = this.props.chartRequestView.props.userChart;
        if (uc && uc.toStr == null) {
            Navigator.API.fillToStrings(uc)
                .then(() => this.forceUpdate())
                .done();
        }
    }

    handleSelect = (uc: Lite<UserChartEntity>) => {

        var crv = this.props.chartRequestView;

        Navigator.API.fetchAndForget(uc).then(userChart => {
            const chartRequest = crv.props.chartRequest!;
            UserChartClient.Converter.applyUserChart(chartRequest, userChart, undefined)
                .then(newChartRequest => crv.setState({ chartResult: undefined, lastChartRequest: undefined },
                    () => crv.props.onChange(newChartRequest, toLite(userChart))))
                .done();
        }).then();
    }

    handleEdit = () => {
        Navigator.API.fetchAndForget(this.props.chartRequestView.props.userChart!)
            .then(userChart => Navigator.navigate(userChart))
            .then(() => this.reloadList())
            .done();
    }


    handleCreate = () => {

        var crView = this.props.chartRequestView;

        var cr = crView.props.chartRequest!;

        UserChartClient.API.fromChartRequest(cr)
            .then(userChart => Navigator.view(userChart))
            .then(uc => {
                if (uc && uc.id) {
                    this.reloadList()
                        .then(() => crView.props.onChange(cr, toLite(uc)))
                        .done();
                }
            }).done();
    }

    render() {
        const userCharts = this.state.userCharts;
        const crView = this.props.chartRequestView;
        const labelText = !crView.props.userChart ? UserChartEntity.nicePluralName() : crView.props.userChart.toStr

        const label = <span><i className="glyphicon glyphicon-stats"></i> &nbsp; {labelText}</span>;
        return (
            <DropdownButton title={label as any} id="userQueriesDropDown" className="sf-userquery-dropdown"
                onToggle={this.handleSelectedToggle}>
                {
                    userCharts && userCharts.map((uc, i) =>
                        <MenuItem key={i}
                            className={classes("sf-userquery", is(uc, crView.props.userChart) && "active")}
                            onSelect={() => this.handleSelect(uc) }>
                            { uc.toStr }
                        </MenuItem>)
                }
                { userCharts && userCharts.length > 0 && <MenuItem divider/> }
                {crView.props.userChart && <MenuItem onSelect={this.handleEdit} >{ChartMessage.EditUserChart.niceToString() }</MenuItem> }
                <MenuItem onSelect={this.handleCreate}>{ChartMessage.CreateNew.niceToString() }</MenuItem>
            </DropdownButton>
        );
    }
 
}
