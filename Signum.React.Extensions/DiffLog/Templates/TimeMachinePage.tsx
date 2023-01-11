import * as React from 'react'
import { DateTime } from 'luxon'
import { RouteComponentProps } from 'react-router'
import { RenderEntity, TypeContext } from '@framework/Lines'
import * as Finder from '@framework/Finder'
import EntityLink from '@framework/SearchControl/EntityLink'
import { SearchControl } from '@framework/Search'
import { Entity, getToString, JavascriptMessage } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { TimeMachineMessage } from '../Signum.Entities.DiffLog'
import { Lite } from '@framework/Signum.Entities'
import { newLite, QueryTokenString, toFormatWithFixes, toLuxonFormat } from '@framework/Reflection'
import { getTypeInfo } from '@framework/Reflection'
import { Tabs, Tab, Modal } from 'react-bootstrap'
import { DiffDocument, LineOrWordsChange } from './DiffDocument'
import * as DiffLogClient from '../DiffLogClient'
import { SearchControlHandler } from '@framework/SearchControl/SearchControl'
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { IModalProps, openModal } from '@framework/Modals'
import { EntityDump } from '../DiffLogClient'


export default function TimeMachinePage(p: RouteComponentProps<{ type: string; id: string }>) {

  var params = p.match.params;

  const lite = useAPI(() => {
    var lite = newLite(params.type, params.id!);

    return Navigator.API.fillLiteModels(lite)
      .then(() => lite)
      .catch(() => { lite.model = TimeMachineMessage.EntityDeleted.niceToString(); lite.ModelType = undefined; return lite });
  }, [])

  const searchControl = React.useRef<SearchControlHandler>(null);
  const forceUpdate = useForceUpdate();
  const queryDescription = useAPI(() => Finder.getQueryDescription(params.type), [params.type]);

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
          <EntityLink lite={lite}>{"{0} {1}".formatWith(getTypeInfo(lite.EntityType).niceName, lite.id)}</EntityLink>
          &nbsp;<span style={{ color: "#aaa" }}>{getToString(lite)}</span>
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
          systemTime: { mode: "All", joinMode: "FirstCompatible" }
        }}
          onSelectionChanged={() => forceUpdate()}
        />
      }

      <br />
      <h5>Selected Versions</h5>
      {scl?.state.selectedRows &&
        <TimeMachineTabs lite={lite} versionDatesUTC={scl.state.selectedRows.map(sr => sr.columns[colIndex!] as string).map(d => d)} />
      }
    </div>
  );
}

interface RenderEntityVersionProps {
  current: ()=> Promise<EntityDump>;
  previous: (() => Promise<EntityDump>) | undefined;
  next: (() => Promise<EntityDump>) | undefined;
}

export function RenderEntityVersion(p: RenderEntityVersionProps) {
  var current = useAPI(signal => p.current(), [p.current]);
  var previous = useAPI(signal => p.previous == null ? Promise.resolve(null) : p.previous(), [p.previous]);
  var next = useAPI(signal => p.next == null ? Promise.resolve(null) : p.next(), [p.next]);

  if (!current || previous === undefined || next === undefined)
    return <h3>{JavascriptMessage.loading.niceToString()}</h3>;

  return (
    <div>
      <RenderEntity ctx={TypeContext.root(current.entity, { readOnly: true })} />
    </div>
  );
}

interface DiffEntityVersionProps {
  first: () => Promise<EntityDump>;
  second: () => Promise<EntityDump>;
}

export function DiffEntityVersion(p: DiffEntityVersionProps) {

  var first = useAPI(signal => p.first(), [p.first]);

  var second = useAPI(signal => p.second(), [p.second]);

  if (first == undefined || second  == undefined)
    return <h3>{JavascriptMessage.loading.niceToString()}</h3>;

  return (
    <DiffDocument first={first.dump} second={second.dump} />
  );
}

export function TimeMachineTabs(p: { lite: Lite<Entity>, versionDatesUTC: string[] }) {

  function memoized(dateUtc: string): () => Promise<EntityDump> {

    var memo: Promise<EntityDump>;

    return () => (memo ??= DiffLogClient.API.getEntityDump(p.lite, dateUtc));
  }

  var luxonFormat = toLuxonFormat("o", "DateTime");

  var refs = React.useRef<{ [versionDateUTC: string]: () => Promise<EntityDump> }>({});

  refs.current = p.versionDatesUTC.toObject(a => a, a => refs.current[a] ?? memoized(a));

  return (
    <Tabs id="timeMachineTabs">
      {p.versionDatesUTC.orderBy(a => a).flatMap((d, i, dates) => [
        <Tab title={toFormatWithFixes(DateTime.fromISO(d), luxonFormat)} key={d} eventKey={d}>
          <RenderEntityVersion
            previous={i == 0 ? undefined : refs.current[dates[i - 1]]}
            current={refs.current[d]}
            next={i == dates.length - 1 ? undefined : refs.current[dates[i + 1]]} />
        </Tab>,
        (i < dates.length - 1) && <Tab title="<- Diff ->" key={"diff-" + d + "-" + dates[i + 1]} eventKey={"diff-" + d + "-" + dates[i + 1]}>
          <DiffEntityVersion first={refs.current[d]} second={refs.current[dates[i+1]]} />
        </Tab>
      ])}
    </Tabs>
  );
}


interface TimeMachineModalProps extends IModalProps<boolean | undefined> {
  lite: Lite<Entity>;
  versionDatesUTC: string[]
}

export function TimeMachineModal(p: TimeMachineModalProps) {

  const [show, setShow] = React.useState(true);
  const answerRef = React.useRef<boolean | undefined>(undefined);

  function handleCloseClicked() {
    setShow(false);
  }

  function handleOnExited() {
    p.onExited!(answerRef.current);
  }

  return (
    <Modal onHide={handleCloseClicked} show={show} className="message-modal" onExited={handleOnExited} size="xl">
      <div className="modal-header">
        <h5 className="modal-title">{TimeMachineMessage.CompareVersions.niceToString()}</h5>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCloseClicked}/>
      </div>
      <div className="modal-body">
        <TimeMachineTabs lite={p.lite} versionDatesUTC={p.versionDatesUTC} />
      </div>
    </Modal>
  );
}

TimeMachineModal.show = (lite: Lite<Entity>, versionDatesUTC: string[]): Promise<boolean | undefined> => {
  return openModal<boolean | undefined>(<TimeMachineModal lite={lite} versionDatesUTC={versionDatesUTC} />);
};


