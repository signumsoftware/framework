
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

export default class UserChartMenu extends React.Component<UserChartMenuProps, { currentUserChart?: Lite<UserChartEntity>, userCharts?: Lite<UserChartEntity>[] }> {

    constructor(props) {
        super(props);
        this.state = { };
    }

    componentWillMount() {
        
        var userChart = window.location.search.tryAfter("userChart=");
        if (userChart) {
            var uc = parseLite(decodeURIComponent(userChart.tryBefore("&") || userChart)) as Lite<UserChartEntity>;
            Navigator.API.fillToStrings([uc])
                .then(() => this.setState({ currentUserChart: uc }))
                .done();
        }
    }

    handleSelectedToggle = (isOpen: boolean) => {

        if (isOpen && this.state.userCharts == null)
            this.reloadList().done();
    }

    reloadList(): Promise<void> {
        return UserChartClient.API.forQuery(this.props.chartRequestView.state.chartRequest.queryKey)
            .then(list => this.setState({ userCharts: list }));
    }


    handleSelect = (uc: Lite<UserChartEntity>) => {

        Navigator.API.fetchAndForget(uc).then(userQuery => {
            var oldFindOptions = this.props.chartRequestView.state.chartRequest;
            UserChartClient.Converter.applyUserChart(oldFindOptions, userQuery, null)
                .then(newChartRequest => {
                    this.props.chartRequestView.setState({ chartRequest: newChartRequest });
                    this.setState({ currentUserChart: uc,  });
                })
                .done();
        }).then();
    }

    handleEdit = () => {
        Navigator.API.fetchAndForget(this.state.currentUserChart)
            .then(userQuery => Navigator.navigate(userQuery))
            .then(() => this.reloadList())
            .done();
    }


    handleCreate = () => {

        UserChartClient.API.fromChartRequest(this.props.chartRequestView.state.chartRequest)
            .then(userQuery => Navigator.view(userQuery))
            .then(uc => {
                if (uc && uc.id) {
                    this.reloadList()
                        .then(() => this.setState({ currentUserChart: toLite(uc) }))
                        .done();
                }
            }).done();
    }

    render() {
        const label = UserChartEntity.nicePluralName();
        var userCharts = this.state.userCharts;
        return (
            <DropdownButton title={label} label={label} id="userQueriesDropDown" className="sf-userquery-dropdown"
                onToggle={this.handleSelectedToggle}>
                {
                    userCharts && userCharts.map((uc, i) =>
                        <MenuItem key={i}
                            className={classes("sf-userquery", is(uc, this.state.currentUserChart) && "active") }
                            onSelect={() => this.handleSelect(uc) }>
                            { uc.toStr }
                        </MenuItem>)
                }
                { userCharts && userCharts.length > 0 && <MenuItem divider/> }
                { this.state.currentUserChart && <MenuItem onSelect={this.handleEdit} >{ChartMessage.EditUserChart.niceToString() }</MenuItem> }
                <MenuItem onSelect={this.handleCreate}>{ChartMessage.CreateNew.niceToString() }</MenuItem>
            </DropdownButton>
        );
    }
 
}
