import * as React from 'react'
import * as moment from 'moment'
import { RouteComponentProps } from 'react-router'
import { StyleContext, RenderEntity, TypeContext } from '@framework/Lines'
import * as Finder from '@framework/Finder'
import EntityLink from '@framework/SearchControl/EntityLink'
import { SearchControl, ColumnOption } from '@framework/Search'
import { QueryDescription } from '@framework/FindOptions'
import { Entity, JavascriptMessage } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { TimeMachineMessage } from '../Signum.Entities.DiffLog'
import { Lite } from '@framework/Signum.Entities'
import { newLite, QueryTokenString } from '@framework/Reflection'
import { EngineMessage } from '@framework/Signum.Entities'
import { NormalWindowMessage } from '@framework/Signum.Entities'
import { Dic } from '@framework/Globals'
import { getTypeInfo } from '@framework/Reflection'
import { Tabs, Tab } from 'react-bootstrap'
import { is } from '@framework/Signum.Entities'
import * as DiffLogClient from '../DiffLogClient'
import { DiffDocument } from './DiffDocument'
import { SearchControlHandler } from '@framework/SearchControl/SearchControl'
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { asUTC } from '@framework/SearchControl/SystemTimeEditor'

export default function TimeMachinePage(p: RouteComponentProps<{ type: string; id?: string }>) {

  var params = p.match.params;

  const lite = useAPI(() => {
    var lite = newLite(params.type, params.id!);

    return Navigator.API.fillToStrings(lite)
      .then(() => lite)
      .catch(() => { lite.toStr = TimeMachineMessage.EntityDeleted.niceToString(); return lite });
  }, [])

  const searchControl = React.useRef<SearchControlHandler>(null);
  const forceUpdate = useForceUpdate();
  const queryDescription = useAPI(() => Finder.getQueryDescription(params.type), [params.type]);

  var ctx = new StyleContext(undefined, undefined);
  if (lite == null)
    return <h4><span className="display-6">{JavascriptMessage.loading.niceToString()}</span></h4>;

  var scl = searchControl.current?.searchControlLoaded ?? undefined;
  var colIndex = scl?.props.findOptions.columnOptions.findIndex(a => a.token != null && a.token.fullKey == "Entity.SystemValidFrom");

  return (
    <div>
      <h4>
        <span className="display-5">{TimeMachineMessage.TimeMachine.niceToString()}</span>
        <br />
        <small className="sf-type-nice-name">
          <EntityLink lite={lite}>{NormalWindowMessage.Type0Id1.niceToString().formatWith(getTypeInfo(lite.EntityType).niceName, lite.id)}</EntityLink>
          &nbsp;
                        <span style={{ color: "#aaa" }}>{lite.toStr}</span>
        </small>
      </h4>

      <br />
      <h5>All Versions</h5>
      {
        queryDescription && <SearchControl ref={searchControl} findOptions={{
          queryName: lite.EntityType,
          filterOptions: [{ token: QueryTokenString.entity(), operation: "EqualTo", value: lite }],
          columnOptions: [
            { token: QueryTokenString.entity().expression("SystemValidFrom") },
            { token: QueryTokenString.entity().expression("SystemValidTo") },
          ],
          columnOptionsMode: "InsertStart",
          orderOptions: [{ token: QueryTokenString.entity().expression("SystemValidFrom"), orderType: "Ascending" }],
          systemTime: { mode: "All" }
        }}
          onSelectionChanged={() => forceUpdate()}
        />
      }

      <br />
      <h5>Selected Versions</h5>
      <Tabs id="timeMachineTabs">
        {scl?.state.selectedRows && scl.state.selectedRows.map(sr => sr.columns[colIndex!] as string).map(d => asUTC(d)).orderBy(a => a).flatMap((d, i, dates) => [
          <Tab title={d.replace("T", " ")} key={d} eventKey={d}>
            <RenderEntityVersion lite={lite} asOf={d} />
          </Tab>,
          (i < dates.length - 1) && <Tab title="<- Diff ->" key={"diff-" + d + "-" + dates[i + 1]} eventKey={"diff-" + d + "-" + dates[i + 1]}>
            <DiffEntityVersion lite={lite} validFrom={d} validTo={dates[i + 1]} />
          </Tab>
        ])}
      </Tabs>
    </div>
  );
}

interface RenderEntityVersionProps {
  lite: Lite<Entity>;
  asOf: string;
}

interface RenderEntityVersionState {
  entity?: Entity;
}

export function RenderEntityVersion(p: RenderEntityVersionProps) {

  const entity = useAPI(signal => DiffLogClient.API.retrieveVersion(p.lite, p.asOf), [p.lite, p.asOf]);

  if (!entity)
    return <h3>{JavascriptMessage.loading.niceToString()}</h3>;

  return (
    <div>
      <RenderEntity ctx={TypeContext.root(entity, { readOnly: true })} />
    </div>
  );
}

interface DiffEntityVersionProps {
  lite: Lite<Entity>;
  validFrom: string;
  validTo: string;
}

export function DiffEntityVersion(p: DiffEntityVersionProps) {

  const diffBlock = useAPI(() => DiffLogClient.API.diffVersions(p.lite, p.validFrom, p.validTo), [p.lite, p.validFrom, p.validTo]);

  if (!diffBlock)
    return <h3>{JavascriptMessage.loading.niceToString()}</h3>;

  return (
    <div>
      <DiffDocument diff={diffBlock} />
    </div>
  );
}


