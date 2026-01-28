import * as React from 'react'
import { Dropdown } from 'react-bootstrap'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import { Lite, toLite, newMListElement, SearchMessage, MList, getToString, translated } from '@framework/Signum.Entities'
import { is } from '@framework/Signum.Entities'
import { Finder } from '@framework/Finder'
import { Navigator } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import { ChartRequestModel, ChartMessage, ChartColumnEmbedded, ChartTimeSeriesEmbedded } from '../Signum.Chart'
import { UserChartClient } from './UserChartClient'
import { ChartRequestViewHandle } from '../Templates/ChartRequestView'
import { UserAssetClient } from '../../Signum.UserAssets/UserAssetClient'
import { useForceUpdate } from '@framework/Hooks'
import { AutoFocus } from '@framework/Components/AutoFocus'
import { KeyNames } from '@framework/Components'
import { UserQueryMerger } from '../../Signum.UserQueries/UserQueryMenu'
import { UserChartEntity, UserChartOperation } from '../UserChart/Signum.Chart.UserChart'
import { clone } from '@framework/Reflection'
import { ChartClient } from '../ChartClient'

export interface UserChartMenuProps {
  chartRequestView: ChartRequestViewHandle;
}

export default function UserChartMenu(p: UserChartMenuProps): React.JSX.Element {

  const forceUpdate = useForceUpdate();
  const [filter, setFilter] = React.useState<string>();
  const [isOpen, setIsOpen] = React.useState(false);
  const [userCharts, setUserCharts] = React.useState<Lite<UserChartEntity>[] | undefined>(undefined);

  React.useEffect(() => {
    if (!isOpen && userCharts == undefined) {
      reloadList();
    }
  }, [isOpen, p.chartRequestView && p.chartRequestView.userChart]);

  function reloadList(): Promise<Lite<UserChartEntity>[]>  {
    return UserChartClient.API.forQuery(p.chartRequestView.chartRequest.queryKey)
      .then(list => {
        setUserCharts(list);
        const userChart = p.chartRequestView.userChart;

        if (userChart && userChart.model == null) {
          const similar = list.singleOrNull(a => is(a, userChart));
          if (similar) {
            userChart.model = similar.model;
            forceUpdate();
          } else {
            Navigator.API.fillLiteModels(userChart)
              .then(() => forceUpdate());
          }
        }
        return list;
      });
  }

  function handleSelect(uc: Lite<UserChartEntity>) {
    var crv = p.chartRequestView;

    Navigator.API.fetch(uc).then(userChart => {
      const cr = crv.chartRequest;
      const newCR = ChartRequestModel.New({ queryKey: cr.queryKey });
      UserChartClient.Converter.applyUserChart(newCR, userChart, undefined)
        .then(newChartRequest => { crv.onChange(newChartRequest, toLite(userChart, undefined, translated(userChart, a => a.displayName))); crv.hideFiltersAndSettings(); });
    });
  }

  async function applyChanges(): Promise<UserChartEntity> {

    var crv = p.chartRequestView;

    const ucOld = await Navigator.API.fetch(crv.userChart!);
    const crmOld = await UserChartClient.Converter.toChartRequest(ucOld, undefined)

    const ucNew = await createUserChart();
    const crmNew = crv.chartRequest;

    const sd = await import("../../Signum.UserQueries/StringDistance").then(mod => new mod.default());

    ucOld.chartScript = ucNew.chartScript;
    ucOld.maxRows = ucNew.maxRows;
    ucOld.filters = UserQueryMerger.mergeFilters(ucOld.filters, ucNew.filters,
      Finder.toFilterOptions(crmOld.filterOptions ?? []),
      Finder.toFilterOptions(crmNew.filterOptions ?? []), 0, sd);
    ucOld.columns = UserChartMerger.mergeColumns(ucOld.columns, ucNew.columns);
    ucOld.parameters = ucNew.parameters;
    ucOld.customDrilldowns = ucNew.customDrilldowns;

    return ucOld;
  }

  function handleApplyChanges() {
    var crv = p.chartRequestView;
    applyChanges()
      .then(userChart => Navigator.view(userChart))
      .then(() => reloadList())
      .then(list => {
        if (!list.some(a => is(a, crv.userChart)))
          crv.onChange(p.chartRequestView.chartRequest, undefined);
        else
          handleSelect(crv.userChart!);
      });
  }

  function handleEdit() {
    var crv = p.chartRequestView;

    Navigator.API.fetch(crv.userChart!)
      .then(userChart => Navigator.view(userChart))
      .then(() => reloadList())
      .then(list => {
        if (!list.some(a => is(a, crv.userChart)))
          crv.onChange(p.chartRequestView.chartRequest, undefined);
        else
          handleSelect(crv.userChart!);
      });
  }


  function handleCreate() {

    createUserChart()
      .then(uc => Navigator.view(uc))
      .then(uc => {
        if (uc?.id) {
          crView.onChange(crView.chartRequest, toLite(uc, undefined, translated(uc, a => a.displayName)));
          crView.hideFiltersAndSettings();
        }
      });
  }

  async function createUserChart() : Promise<UserChartEntity> {
    const crView = p.chartRequestView;

    const cr = crView.chartRequest;

    const query = await Finder.API.fetchQueryEntity(cr.queryKey);

    const fos = Finder.toFilterOptions(cr.filterOptions);

    const qfs = await UserAssetClient.API.stringifyFilters({
      canTimeSeries: cr.chartTimeSeries != null,
      canAggregate: true,
      queryKey: cr.queryKey,
      filters: fos.map(fo => UserAssetClient.Converter.toFilterNode(fo))
    });

    var ts = cr.chartTimeSeries;
    
    const uc = UserChartEntity.New({
      owner: AppContext.currentUser && toLite(AppContext.currentUser),
      query: query,
      chartScript: cr.chartScript,
      chartTimeSeries: !ts ? null : ChartTimeSeriesEmbedded.New({
        timeSeriesUnit: ts?.timeSeriesUnit,
        startDate: ts.startDate && await UserAssetClient.API.stringifyDate(ts.startDate),
        endDate: ts.endDate && await UserAssetClient.API.stringifyDate(ts.endDate),
        timeSeriesStep: ts.timeSeriesStep,
        timeSeriesMaxRowsPerStep: ts.timeSeriesMaxRowsPerStep,
        splitQueries: ts.splitQueries,
      }),
      maxRows: cr.maxRows,
      filters: qfs.map(f => newMListElement(UserAssetClient.Converter.toQueryFilterEmbedded(f))),
      columns: cr.columns.map(a => newMListElement(JSON.parse(JSON.stringify(a.element)))),
      parameters: cr.parameters.map(p => newMListElement(JSON.parse(JSON.stringify(p.element)))),
    });

    return uc;
  }

  const crView = p.chartRequestView;
  const label = !crView.userChart ? UserChartEntity.nicePluralName() : getToString(crView.userChart)

  var canSave = UserChartEntity.tryOperationInfo(UserChartOperation.Save) != null;

  return (
    <Dropdown onToggle={() => setIsOpen(!isOpen)} show={isOpen}>
      <Dropdown.Toggle id="userQueriesDropDown" variant="tertiary">
        <span><FontAwesomeIcon icon="chart-bar" /> &nbsp; {label}</span>
      </Dropdown.Toggle>
      <Dropdown.Menu>
        {userCharts && userCharts.length > 10 &&
          <div>
            <AutoFocus disabled={!isOpen}>
              <input
                type="text"
                className="form-control form-control-sm"
                value={filter}
                placeholder={SearchMessage.Search.niceToString()}
                onChange={e => setFilter(e.currentTarget.value)}
                onKeyDown={handleSearchKeyDown} />
            </AutoFocus>
            <Dropdown.Divider />
          </div>}
        <div id="userchart-items-container" style={{ maxHeight: "300px", overflowX: "auto" }}>
          {userCharts?.map((uc, i) => {
            if (filter == undefined || getToString(uc)?.search(new RegExp(RegExp.escape(filter), "i")) != -1)
              return (
                <Dropdown.Item key={i}
                  className={classes("sf-userquery", is(uc, crView.userChart) && "active")}
                  onClick={() => handleSelect(uc)}>
                  {getToString(uc)}
                </Dropdown.Item>)
          })}
        </div>
        {Boolean(userCharts?.length) && <Dropdown.Divider />}
        {crView.userChart && canSave && <Dropdown.Item onClick={handleApplyChanges} ><FontAwesomeIcon aria-hidden={true} icon={"share-from-square"} className="me-2" />{ChartMessage.ApplyChanges.niceToString()}</Dropdown.Item>}
        {crView.userChart && canSave && <Dropdown.Item onClick={handleEdit}><FontAwesomeIcon aria-hidden={true} icon={"pen-to-square"} className="me-2" />{ChartMessage.Edit.niceToString()}</Dropdown.Item>}
        {canSave && <Dropdown.Item onClick={handleCreate}><FontAwesomeIcon aria-hidden={true} icon={"plus"} className="me-2" />{ChartMessage.CreateNew.niceToString()}</Dropdown.Item>}
      </Dropdown.Menu>
    </Dropdown>
  );

  function handleSearchKeyDown(e: React.KeyboardEvent<any>) {

    if (!e.shiftKey && e.key == KeyNames.arrowDown) {

      e.preventDefault();
      const div = document.getElementById("userchart-items-container")!;
      var item = Array.from(div.querySelectorAll("a.dropdown-item")).firstOrNull();
      if (item)
        (item as HTMLAnchorElement).focus();
    }
  }
}

export namespace UserChartMerger {
  export function mergeColumns(oldUqColumns: MList<ChartColumnEmbedded>, newUqColumns: MList<ChartColumnEmbedded>): MList<ChartColumnEmbedded> {
    newUqColumns.forEach((newMle, i) => {

      var oldMle = oldUqColumns[i];
      if (oldMle) {
        newMle.rowId = oldMle.rowId;

        if (newMle.element.displayName == translated(oldMle.element, a => a.displayName))
          newMle.element.displayName = oldMle.element.displayName;
      }
    });

    return newUqColumns;
  }
}
