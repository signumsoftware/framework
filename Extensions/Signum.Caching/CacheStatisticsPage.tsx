
import * as React from 'react'
import { useLocation, useParams } from 'react-router-dom'
import { Tab, Tabs } from 'react-bootstrap'
import { CacheClient } from './CacheClient'
import { useAPI, useAPIWithReload } from '@framework/Hooks'
import { SearchControl } from '@framework/Search'
import { ExceptionEntity } from '@framework/Signum.Basics'

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
        <h2>Loading...</h2>
      </div>
    );


  return (
    <div>
      <h2>Cache Statistics</h2>
      <div className="btn-toolbar">
        {state.isEnabled == true && <button onClick={handleDisabled} className="sf-button btn btn-tertiary" style={{ color: "var(--bs-danger)" }}>Disable</button>}
        {state.isEnabled == false && <button onClick={handleEnabled} className="sf-button btn btn-tertiary" style={{ color: "var(--bs-success)" }}>Enabled</button>}
        {<button onClick={handleClear} className="sf-button btn btn-tertiary" style={{ color: "var(--bs-primary)" }}>Clear</button>}
      </div >
      <div className="m-2">
        <strong>Server Broadcast:</strong> <code>{state.serverBroadcast}</code>
        <br />
        <strong>SqlDependency:</strong> <code>{state.sqlDependency.toString()}</code>
        <br />
      </div>
      <Tabs id="tabs">
        {state.tables &&
          <Tab title="Tables" eventKey="table">
            {renderTables(state)}
          </Tab>}
        {state.lazies &&
          <Tab title="Lazies" eventKey="lazy">
            {renderLazies(state)}
          </Tab>
        }

        {state.serverBroadcast &&
          <Tab title="Invalidation Exceptions" eventKey="exceptions">
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
      <table className="table table-sm">
        <thead>
          <tr>
            <th>Type</th>
            <th>Hits</th>
            <th>Invalidations</th>
            <th>Loads</th>
            <th>LoadTime</th>
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
      </table>);
  }

  function renderTables(state: CacheClient.CacheState) {

    return (
      <table className="table table-sm">
        <thead>
          <tr>
            <th>Table</th>
            <th>Type</th>
            <th>Count</th>
            <th>Hits</th>
            <th>Invalidations</th>
            <th>Loads</th>
            <th>LoadTime</th>
          </tr>
        </thead>
        <tbody>
          {state.tables.map(m => renderTree(m, 0))}
        </tbody>
      </table>);
  }


  function renderTree(table: CacheClient.CacheTableStats, depth: number): React.ReactNode {

    const opacity =
      depth == 0 ? 1 :
        depth == 1 ? .7 :
          depth == 2 ? .5 :
            depth == 3 ? .4 : .3;

    return (
      <React.Fragment key={table.tableName}>
        <tr style={{ opacity: opacity }} key={table.tableName}>
          <td> {Array.repeat(depth, " → ").join("") + table.tableName}</td >
          <td> {table.typeName} </td>
          <td> {table.count != undefined ? table.count.toString() : "-- not loaded --"} </td>
          <td> {table.hits} </td>
          <td> {table.invalidations}</td>
          <td> {table.loads}</td>
          <td> {table.sumLoadTime} </td>
        </tr>
        {table.subTables && table.subTables.map(st => renderTree(st, depth + 1))}
      </React.Fragment>
    );
  }
}
