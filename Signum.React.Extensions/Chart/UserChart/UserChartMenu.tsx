import * as React from 'react'
import { Dropdown, DropdownButton } from 'react-bootstrap'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { Lite, toLite, newMListElement } from '@framework/Signum.Entities'
import { is } from '@framework/Signum.Entities'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import SearchControl from '@framework/SearchControl/SearchControl'
import { UserChartEntity, ChartRequestModel, ChartMessage, ChartColumnEmbedded } from '../Signum.Entities.Chart'
import * as UserChartClient from './UserChartClient'
import ChartRequestView, { ChartRequestViewHandle } from '../Templates/ChartRequestView'
import { getQueryKey } from '@framework/Reflection';
import * as UserAssetClient from '../../UserAssets/UserAssetClient'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets';
import { useForceUpdate, useAPI } from '@framework/Hooks'

export interface UserChartMenuProps {
  chartRequestView: ChartRequestViewHandle;
}

interface UserChartMenuState {
  userCharts?: Lite<UserChartEntity>[];
  isOpen: boolean;
}

export default function UserChartMenu(p : UserChartMenuProps){
  const forceUpdate = useForceUpdate();
  const [isOpen, setIsOpen] = React.useState(false);
  const [userCharts, setUserCharts] = React.useState<Lite<UserChartEntity>[] | undefined>(undefined);

  React.useEffect(() => {
    if (!isOpen && userCharts == undefined) {
      reloadList();
    }
  }, [isOpen, p.chartRequestView && p.chartRequestView.userChart]);

  React.useEffect(() => {
    var uc = p.chartRequestView.userChart;
    if (uc?.toStr == null) {
      Navigator.API.fillToStrings(uc)
        .then(() => forceUpdate())
        .done();
    }
  }, [p.chartRequestView!.userChart])

  function reloadList() {
    UserChartClient.API.forQuery(p.chartRequestView.chartRequest.queryKey)
      .then(list => setUserCharts(list))
      .done();
  }

  function handleSelect(uc: Lite<UserChartEntity>) {
    var crv = p.chartRequestView;

    Navigator.API.fetchAndForget(uc).then(userChart => {
      const cr = crv.chartRequest;
      const newCR = ChartRequestModel.New({ queryKey: cr.queryKey });
      UserChartClient.Converter.applyUserChart( newCR, userChart, undefined)
        .then(newChartRequest => crv.onChange(newChartRequest, toLite(userChart)))
        .done();
    }).done();
  }

  function handleEdit() {
    Navigator.API.fetchAndForget(p.chartRequestView.userChart!)
      .then(userChart => Navigator.navigate(userChart))
      .then(() => reloadList())
      .done();
  }


 async function onCreate() {
    const crView = p.chartRequestView;

    const cr = crView.chartRequest;

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

    if (uc?.id) {
      crView.onChange(cr, toLite(uc));
    }
  }

  const crView = p.chartRequestView;
  const labelText = !crView.userChart ? UserChartEntity.nicePluralName() : crView.userChart.toStr

  return (
    <Dropdown onToggle={() => setIsOpen(!isOpen)} show={isOpen}>
      <Dropdown.Toggle id="userQueriesDropDown" className="sf-userquery-dropdown" variant="light">
        <span><FontAwesomeIcon icon="chart-bar" /> &nbsp; {labelText}</span>
      </Dropdown.Toggle>
      <Dropdown.Menu>
        {
          userCharts?.map((uc, i) =>
            <Dropdown.Item key={i}
              className={classes("sf-userquery", is(uc, crView.userChart) && "active")}
              onClick={() => handleSelect(uc)}>
              {uc.toStr}
            </Dropdown.Item>)
        }
        {userCharts?.length && <Dropdown.Divider />}
        {crView.userChart && <Dropdown.Item onClick={handleEdit}>{ChartMessage.EditUserChart.niceToString()}</Dropdown.Item>}
        <Dropdown.Item onClick={() => onCreate().done()}>{ChartMessage.CreateNew.niceToString()}</Dropdown.Item>
      </Dropdown.Menu>
    </Dropdown>
  );
}
