
import * as React from 'react'
import { useLocation, useParams } from 'react-router-dom'
import { Tab, Tabs } from 'react-bootstrap'
import { CacheClient } from './CacheClient'
import { useAPI, useAPIWithReload } from '@framework/Hooks'
import { SearchControl } from '@framework/Search'
import { ExceptionEntity } from '@framework/Signum.Basics'
import { CacheMessage } from './Signum.Caching'
import { AccessibleRow, AccessibleTable } from '../../Signum/React/Basics/AccessibleTable'

export default function CacheStatisticsPage(): React.JSX.Element {

  var [state, reloadState] = useAPIWithReload(() => CacheClient.API.view(), [], { avoidReset: true });

  function handleDisabled(e: React.MouseEvent<any>) {
    CacheClient.API.disable().then(() => reloadState());
  }

  function handleEnabled(e: React.MouseEvent<any>) {
    CacheClient.API.enable().then(() => reloadState());
  }

  function handleClear(e: React.MouseEvent<any>) {
    CacheClient.API.clear().then(() => reloadState());
  }

  if (state == null)
    return (
      <div>
        <h2>{CacheMessage.Loading.niceToString()}...</h2>
      </div>
    );


  return (
    <div>
      <h2>{CacheMessage.CacheStatistics.niceToString()}</h2>
      <div className="btn-toolbar">
        {state.isEnabled == true && <button onClick={handleDisabled} className="sf-button btn btn-tertiary" style={{ color: "var(--bs-danger)" }}>{CacheMessage.Disable.niceToString()}</button>}
        {state.isEnabled == false && <button onClick={handleEnabled} className="sf-button btn btn-tertiary" style={{ color: "var(--bs-success)" }}>{CacheMessage.Enable.niceToString()}</button>}
        {<button onClick={handleClear} className="sf-button btn btn-tertiary" style={{ color: "var(--bs-primary)" }}>{CacheMessage.Clear.niceToString()}</button>}
      </div >
      <div className="m-2">
        <strong>{CacheMessage.ServerBroadcast.niceToString()}:</strong> <code>{state.serverBroadcast}</code>
        <br />
        <strong>{CacheMessage.SqlDependency.niceToString()}:</strong> <code>{state.sqlDependency.toString()}</code>
        <br />
      </div>
      <Tabs id="tabs">
        {state.tables &&
          <Tab title={CacheMessage.Tables.niceToString()} eventKey="table">
            {renderTables(state)}
          </Tab>}
        {state.lazies &&
          <Tab title={CacheMessage.Lazies.niceToString()} eventKey="lazy">
            {renderLazies(state)}
          </Tab>
        }

        {state.serverBroadcast &&
          <Tab title={CacheMessage.InvalidationExceptions.niceToString()} eventKey="exceptions">
            <SearchControl findOptions={{
              queryName: ExceptionEntity,
              filterOptions: [
                { token: ExceptionEntity.token(a => a.entity.controllerName), value: state.serverBroadcast.before("(") }
              ]
            }} />
          </Tab>
        }
      </Tabs>
    </div>
  );

  function renderLazies(state: CacheClient.CacheState) {
    return (
      <AccessibleTable
        caption={CacheMessage.LazyStats.niceToString()}
        className="table table-sm"
        multiselectable={false}>
        <thead>
          <tr>
            <th>{CacheMessage.Type.niceToString()}</th>
            <th>{CacheMessage.Hits.niceToString()}</th>
            <th>{CacheMessage.Invalidations.niceToString()}</th>
            <th>{CacheMessage.Loads.niceToString()}</th>
            <th>{CacheMessage.LoadTime.niceToString()}</th>
          </tr>
        </thead>
        <tbody>
          {state.lazies.map((lazy, i) => <tr key={i}>
            <td> {lazy.typeName} </td>
            <td> {lazy.hits} </td>
            <td> {lazy.invalidations}</td>
            <td> {lazy.loads}</td>
            <td> {lazy.sumLoadTime} </td>
          </tr>)}
        </tbody>
        </AccessibleTable>);
  }

  function renderTables(state: CacheClient.CacheState) {

    return (
      <AccessibleTable
        caption={CacheMessage.TableStats.niceToString()}
        className="table table-sm"
        mapCustomComponents={new Map([[RenderTree, "tr"]])}
        multiselectable={false}>
        <thead>
          <tr>
            <th>{CacheMessage.Table.niceToString()}</th>
            <th>{CacheMessage.Type.niceToString()}</th>
            <th>{CacheMessage.Count.niceToString()}</th>
            <th>{CacheMessage.Hits.niceToString()}</th>
            <th>{CacheMessage.Invalidations.niceToString()}</th>
            <th>{CacheMessage.Loads.niceToString()}</th>
            <th>{CacheMessage.LoadTime.niceToString()}</th>
          </tr>
        </thead>
        <tbody>
          {state.tables.map(m => <RenderTree table={m} depth={0} />)}
        </tbody>
      </AccessibleTable>);
  }

  function RenderTree(p: { table: CacheClient.CacheTableStats, depth: number }): React.JSX.Element {
    const table = p.table;
    const depth = p.depth;

    const opacity =
      depth == 0 ? 1 :
        depth == 1 ? .7 :
          depth == 2 ? .5 :
            depth == 3 ? .4 : .3;

    return (
      <>
        <AccessibleRow style={{ opacity: opacity }} key={table.tableName}>
          <td> {Array.repeat(depth, " → ").join("") + table.tableName}</td >
          <td> {table.typeName} </td>
          <td> {table.count != undefined ? table.count.toString() : `-- ${CacheMessage.NotLoaded.niceToString()} --`} </td>
          <td> {table.hits} </td>
          <td> {table.invalidations}</td>
          <td> {table.loads}</td>
          <td> {table.sumLoadTime} </td>
        </AccessibleRow>
        {table.subTables && table.subTables.map(st => <RenderTree table={st} depth={depth+1} />)}
      </>
    );
  }
}
