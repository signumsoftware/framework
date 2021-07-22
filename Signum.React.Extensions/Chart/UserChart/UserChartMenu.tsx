import * as React from 'react'
import { Dropdown } from 'react-bootstrap'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { Lite, toLite, newMListElement, SearchMessage } from '@framework/Signum.Entities'
import { is } from '@framework/Signum.Entities'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import { UserChartEntity, ChartRequestModel, ChartMessage, ChartColumnEmbedded, UserChartOperation } from '../Signum.Entities.Chart'
import * as UserChartClient from './UserChartClient'
import { ChartRequestViewHandle } from '../Templates/ChartRequestView'
import * as UserAssetClient from '../../UserAssets/UserAssetClient'
import { useForceUpdate } from '@framework/Hooks'
import { tryGetOperationInfo } from '@framework/Operations'

export interface UserChartMenuProps {
  chartRequestView: ChartRequestViewHandle;
}

export default function UserChartMenu(p: UserChartMenuProps) {

  const forceUpdate = useForceUpdate();
  const [filter, setFilter] = React.useState<string>();
  const [isOpen, setIsOpen] = React.useState(false);
  const [userCharts, setUserCharts] = React.useState<Lite<UserChartEntity>[] | undefined>(undefined);

  React.useEffect(() => {
    if (!isOpen && userCharts == undefined) {
      reloadList().done();
    }
  }, [isOpen, p.chartRequestView && p.chartRequestView.userChart]);

  function reloadList(): Promise<Lite<UserChartEntity>[]>  {
    return UserChartClient.API.forQuery(p.chartRequestView.chartRequest.queryKey)
      .then(list => {
        setUserCharts(list);
        const userChart = p.chartRequestView.userChart;

        if (userChart && userChart.toStr == null) {
          const similar = list.singleOrNull(a => is(a, userChart));
          if (similar) {
            userChart.toStr = similar.toStr;
            forceUpdate();
          } else {
            Navigator.API.fillToStrings(userChart)
              .then(() => forceUpdate())
              .done();
          }
        }
        return list;
      });
  }

  function handleSelect(uc: Lite<UserChartEntity>) {
    var crv = p.chartRequestView;

    Navigator.API.fetchAndForget(uc).then(userChart => {
      const cr = crv.chartRequest;
      const newCR = ChartRequestModel.New({ queryKey: cr.queryKey });
      UserChartClient.Converter.applyUserChart(newCR, userChart, undefined)
        .then(newChartRequest => { crv.onChange(newChartRequest, toLite(userChart)); crv.hideFiltersAndSettings(); })
        .done();
    }).done();
  }

  function handleEdit() {
    var crv = p.chartRequestView;

    Navigator.API.fetchAndForget(crv.userChart!)
      .then(userChart => Navigator.view(userChart))
      .then(() => reloadList())
      .then(list => {
        if (!list.some(a => is(a, crv.userChart)))
          crv.onChange(p.chartRequestView.chartRequest, undefined);
        else
          handleSelect(crv.userChart!);
      })
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
      owner: AppContext.currentUser && toLite(AppContext.currentUser),
      query: query,
      chartScript: cr.chartScript,
      maxRows: cr.maxRows,
      filters: qfs.map(f => newMListElement(UserAssetClient.Converter.toQueryFilterEmbedded(f))),
      columns: cr.columns.map(a => newMListElement(JSON.parse(JSON.stringify(a.element)))),
      parameters: cr.parameters.map(p => newMListElement(JSON.parse(JSON.stringify(p.element)))),
    }));

    if (uc?.id) {
      crView.onChange(cr, toLite(uc));
      crView.hideFiltersAndSettings();
    }
  }

  const crView = p.chartRequestView;
  const labelText = !crView.userChart ? UserChartEntity.nicePluralName() : crView.userChart.toStr

  var canSave = tryGetOperationInfo(UserChartOperation.Save, UserChartEntity) != null;

  return (
    <Dropdown onToggle={() => setIsOpen(!isOpen)} show={isOpen}>
      <Dropdown.Toggle id="userQueriesDropDown" className="sf-userquery-dropdown" variant="light">
        <span><FontAwesomeIcon icon="chart-bar" /> &nbsp; {labelText}</span>
      </Dropdown.Toggle>
      <Dropdown.Menu>
        {userCharts && userCharts.length > 10 &&
          <div>
            <input type="text"
              className="form-control form-control-sm"
              value={filter}
              placeholder={SearchMessage.Search.niceToString()}
              onChange={e => setFilter(e.currentTarget.value)} />
            <Dropdown.Divider />
          </div>}
        <div style={{ maxHeight: "300px", overflowX: "auto" }}>
          {userCharts?.map((uc, i) => {
            if (filter == undefined || uc.toStr?.search(new RegExp(RegExp.escape(filter), "i")) != -1)
              return (
                <Dropdown.Item key={i}
                  className={classes("sf-userquery", is(uc, crView.userChart) && "active")}
                  onClick={() => handleSelect(uc)}>
                  {uc.toStr}
                </Dropdown.Item>)
          })}
        </div>
        {Boolean(userCharts?.length) && <Dropdown.Divider />}
        {crView.userChart && canSave && <Dropdown.Item onClick={handleEdit}><FontAwesomeIcon icon={["fas", "edit"]} className="mr-2" />{ChartMessage.Edit.niceToString()}</Dropdown.Item>}
        {canSave && <Dropdown.Item onClick={() => onCreate().done()}><FontAwesomeIcon icon={["fas", "plus"]} className="mr-2" />{ChartMessage.CreateNew.niceToString()}</Dropdown.Item>}
      </Dropdown.Menu>
    </Dropdown>
  );
}
