import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { Lite, toLite, newMListElement } from '@framework/Signum.Entities'
import { is } from '@framework/Signum.Entities'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import SearchControl from '@framework/SearchControl/SearchControl'
import { UserChartEntity, ChartRequestModel, ChartMessage, ChartColumnEmbedded } from '../Signum.Entities.Chart'
import * as UserChartClient from './UserChartClient'
import ChartRequestView from '../Templates/ChartRequestView'
import { Dropdown, DropdownToggle, DropdownMenu, DropdownItem } from '@framework/Components';
import { getQueryKey } from '@framework/Reflection';
import * as UserAssetClient from '../../UserAssets/UserAssetClient'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets';

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


  async onCreate() {

    const crView = this.props.chartRequestView;

    const cr = crView.props.chartRequest!;

    const query = await Finder.API.fetchQueryEntity(cr.queryKey);

    const fos = Finder.toFilterOptions(cr.filterOptions);

    const qfs = await UserAssetClient.API.stringifyFilters({
      canAggregate: true,
      queryKey: cr.queryKey,
      filters: fos.map(fo => UserAssetClient.Converter.toFilterNode(fo))
    });

    const uc = await Navigator.view(UserChartEntity.New({
      owner: Navigator.currentUser && toLite(Navigator.currentUser),
      query: query,
      chartScript: cr.chartScript,
      filters: qfs.map(f => newMListElement(UserAssetClient.Converter.toQueryFilterEmbedded(f))),
      columns: cr.columns.map(a => newMListElement(JSON.parse(JSON.stringify(a.element)))),
      parameters: cr.parameters.map(p => newMListElement(JSON.parse(JSON.stringify(p.element)))),
    }));
    
    if (uc && uc.id) {
      await this.reloadList();

      crView.props.onChange(cr, toLite(uc));
    }
  }

  render() {
    const userCharts = this.state.userCharts;
    const crView = this.props.chartRequestView;
    const labelText = !crView.props.userChart ? UserChartEntity.nicePluralName() : crView.props.userChart.toStr

    const label = <span><FontAwesomeIcon icon="chart-bar" /> &nbsp; {labelText}</span>;
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
          <DropdownItem onClick={() => this.onCreate().done()}>{ChartMessage.CreateNew.niceToString()}</DropdownItem>
        </DropdownMenu>
      </Dropdown>
    );
  }

}
