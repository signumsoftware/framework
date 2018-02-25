
import * as React from 'react'
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
import { Dropdown, DropdownToggle, DropdownMenu, DropdownItem } from '../../../../Framework/Signum.React/Scripts/Components';

export interface UserChartMenuProps {
    chartRequestView: ChartRequestView;
}


interface UserChartMenuState {
    userCharts?: Lite<UserChartEntity>[];
    isOpen: boolean;
}


export default class UserChartMenu extends React.Component<UserChartMenuProps, UserChartMenuState> {

    constructor(props: UserChartMenuProps) {
        super(props);
        this.state = {
            isOpen: false
        };
    }


    handleSelectedToggle = () => {

        if (!this.state.isOpen && this.state.userCharts == undefined)
            this.reloadList().done();

        this.setState({ isOpen: !this.state.isOpen });
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

        const label = <span><i className="fa fa-bar-chart"></i> &nbsp; {labelText}</span>;
        return (
            <Dropdown id="userQueriesDropDown" className="sf-userquery-dropdown"
                toggle={this.handleSelectedToggle} isOpen={this.state.isOpen}>
                <DropdownToggle color="light" caret>{label as any}</DropdownToggle>
                <DropdownMenu>
                    {
                        userCharts && userCharts.map((uc, i) =>
                            <DropdownItem key={i}
                                className={classes("sf-userquery", is(uc, crView.props.userChart) && "active")}
                                onClick={() => this.handleSelect(uc)}>
                                {uc.toStr}
                            </DropdownItem>)
                    }
                    {userCharts && userCharts.length > 0 && <DropdownItem divider />}
                    {crView.props.userChart && <DropdownItem onClick={this.handleEdit}>{ChartMessage.EditUserChart.niceToString()}</DropdownItem>}
                    <DropdownItem onClick={this.handleCreate}>{ChartMessage.CreateNew.niceToString()}</DropdownItem>
                </DropdownMenu>
            </Dropdown>
        );
    }

}
