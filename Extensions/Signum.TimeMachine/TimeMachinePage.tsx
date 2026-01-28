import * as React from 'react'
import { DateTime } from 'luxon'
import { useLocation, useParams } from 'react-router'
import { RenderEntity, TypeContext } from '@framework/Lines'
import { Finder } from '@framework/Finder'
import EntityLink from '@framework/SearchControl/EntityLink'
import { SearchControl, SearchControlLoaded } from '@framework/Search'
import { Entity, EntityControlMessage, getToString, JavascriptMessage } from '@framework/Signum.Entities'
import { Navigator } from '@framework/Navigator'
import { Lite } from '@framework/Signum.Entities'
import { newLite, QueryTokenString, toFormatWithFixes, toLuxonFormat } from '@framework/Reflection'
import { getTypeInfo } from '@framework/Reflection'
import { Tabs, Tab, Modal } from 'react-bootstrap'
import { DiffDocument } from '../Signum.DiffLog/Templates/DiffDocument'
import { SearchControlHandler } from '@framework/SearchControl/SearchControl'
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { IModalProps, openModal } from '@framework/Modals'
import MessageModal from '@framework/Modals/MessageModal'
import { ResultRow } from '@framework/FindOptions'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { classes } from '@framework/Globals'
import { TimeMachineClient } from './TimeMachineClient'
import { TimeMachineMessage } from './Signum.TimeMachine'
import { OperationLogEntity } from '@framework/Signum.Operations'


export default function TimeMachinePage(): React.JSX.Element {
  const params = useParams() as { type: string; id: string };

  const lite = useAPI(() => {
    var lite = newLite(params.type, params.id!);

    return Navigator.API.fillLiteModels(lite)
      .then(() => lite)
      .catch(() => { lite.model = TimeMachineMessage.EntityDeleted.niceToString(); lite.ModelType = undefined; return lite });
  }, [params.type, params.id])

  if (lite == undefined)
    return <h1 className="h4"><span className="display-6">{JavascriptMessage.loading.niceToString()}</span></h1>;

  return (<TimeMachine lite={lite} />);
}


export function TimeMachine(p: { lite: Lite<Entity>, isModal?: boolean }): React.JSX.Element {

  const searchControl = React.useRef<SearchControlHandler>(null);
  const forceUpdate = useForceUpdate();
  const queryDescription = useAPI(() => Finder.getQueryDescription(p.lite.EntityType), [p.lite]);

  var scl = searchControl.current?.searchControlLoaded ?? undefined;
  var colIndex = scl?.props.findOptions.columnOptions.findIndex(a => a.token != null && a.token.fullKey == "Entity.SystemValidFrom");

  function renderCheckBox(sc: SearchControlLoaded, row: ResultRow, rowIndex: number) {
    var checked = Boolean(sc.state.selectedRows?.contains(row));
    return (
      <input type="radio" className={classes("form-check-input",
        checked && sc.state.selectedRows!.maxBy(a => a.columns[colIndex!])! != row && "bg-secondary border-secondary"
      )} checked={checked} onChange={e => { }} onClick={e => {
        if (e.ctrlKey) {
          if (checked) {
            sc.state.selectedRows?.remove(row)
            sc.notifySelectedRowsChanged("toggle")
          } else {
            if (sc.state.selectedRows && sc.state.selectedRows.length >= 2)
              return MessageModal.showError(TimeMachineMessage.YouCanNotSelectMoreThanTwoVersionToCompare.niceToString())
            sc.state.selectedRows?.push(row);
            sc.notifySelectedRowsChanged("toggle");
          }
        }
        else {
          sc.state.selectedRows?.clear();
          var rows = sc.state.resultTable!.rows;

          if (rowIndex + 1 < rows.length) {
            const nextRow = rows[rowIndex + 1];
            sc.state.selectedRows?.push(nextRow);
          }
          sc.state.selectedRows?.push(row);
          sc.notifySelectedRowsChanged("toggle");
        }
      }}
      />
    );
  }

  var prevLogToken = QueryTokenString.entity().expression<OperationLogEntity>("PreviousOperationLog");

  return (
    <div>
      {!p.isModal && <h1 className="h4">
        <span className="display-5">{TimeMachineMessage.TimeMachine.niceToString()}</span>
        <br />
        <small className="sf-type-nice-name">
          <EntityLink lite={p.lite}>{"{0} {1}".formatWith(getTypeInfo(p.lite.EntityType).niceName, p.lite.id)}</EntityLink>
          &nbsp;<span style={{ color: "#aaa" }}>{getToString(p.lite)}</span>
        </small>
        <br />
      </h1>}

      <h2 className="h5">{TimeMachineMessage.AllVersions.niceToString()}</h2>
      {
        queryDescription && <SearchControl ref={searchControl} findOptions={{
          queryName: p.lite.EntityType,
          filterOptions: [{ token: QueryTokenString.entity(), operation: "EqualTo", value: p.lite }],
          columnOptions: [
            { token: prevLogToken.append(a => a.start) },
            { token: prevLogToken.append(a => a.user) },
            { token: prevLogToken.append(a => a.operation) },
            { token: QueryTokenString.entity().expression("SystemValidFrom") },
            { token: QueryTokenString.entity().expression("SystemValidTo") },
          ],
          columnOptionsMode: "ReplaceAll",
          orderOptions: [{ token: QueryTokenString.entity().expression("SystemValidFrom"), orderType: "Descending" }],
          systemTime: { mode: "All", joinMode: "FirstCompatible" },
          pagination: { mode: "All" },
        }}
          onSelectionChanged={() => forceUpdate()}
          view={false}
          showSelectedButton={false}
          showContextMenu={() => "Basic"}
          allowSelection="single"
          selectionFromatter={renderCheckBox}
          searchOnLoad={true}
          create={false}
        />
      }

      <br />

      {scl?.state.selectedRows &&
        <TimeMachineTabs
          lite={p.lite}
          versionDatesUTC={scl.state.selectedRows.map(sr => sr.columns[colIndex!] as string).map(d => d)}
        />
      }
    </div>
  );
}

interface RenderEntityVersionProps {
  current: () => Promise<TimeMachineClient.EntityDump>;
  previous: (() => Promise<TimeMachineClient.EntityDump>) | undefined;
  currentDate?: string;
  previousDate?: string;
}

export function RenderEntityVersion(p: RenderEntityVersionProps): React.JSX.Element {
  var pair = useAPI(async signal => {
    var curr = p.current();
    var prev = p.previous == null ? Promise.resolve(null) : p.previous();
    return ({ curr: await curr, prev: await prev });
  }, [p.current, p.previous], { avoidReset: true });

  console.log(pair);

  if (pair === undefined)
    return <h1 className="h3">{JavascriptMessage.loading.niceToString()}</h1>;

  var ctx = TypeContext.root(pair.curr.entity, { readOnly: true });

  if (pair.prev)
    ctx.previousVersion = { value: pair.prev?.entity };

  return (
    <div>
      <RenderEntity ctx={ctx} currentDate={p.currentDate} previousDate={p.previousDate} />
    </div>
  );
}

interface DiffEntityVersionProps {
  previous?: () => Promise<TimeMachineClient.EntityDump>;
  current: () => Promise<TimeMachineClient.EntityDump>;
}

export function DiffEntityVersion(p: DiffEntityVersionProps): React.JSX.Element {

  var pair = useAPI(async signal => {
    var curr = p.current();
    var prev = p.previous == null ? Promise.resolve(null) : p.previous();
    return ({ curr: await curr, prev: await prev });
  }, [p.current, p.previous], { avoidReset: true });

  if (pair === undefined)
    return <h1 className="h3">{JavascriptMessage.loading.niceToString()}</h1>;

  if (pair.prev == null)
    return <pre>{pair.curr.dump}</pre>;

  return <DiffDocument first={pair.prev.dump} second={pair.curr.dump} />;
}

export function TimeMachineTabs(p: { lite: Lite<Entity>, versionDatesUTC: string[] }): React.JSX.Element | null {

  if (p.versionDatesUTC == null || p.versionDatesUTC.length < 1)
    return null;

  function memoized(dateUtc: string): () => Promise<TimeMachineClient.EntityDump> {

    var memo: Promise<TimeMachineClient.EntityDump>;

    return () => (memo ??= TimeMachineClient.API.getEntityDump(p.lite, dateUtc));
  }

  var hasPrevious = p.versionDatesUTC.length > 1;

  var refs = React.useRef<{ [versionDateUTC: string]: () => Promise<TimeMachineClient.EntityDump> }>({});

  refs.current = p.versionDatesUTC.toObject(a => a, a => refs.current[a] ?? memoized(a));
  var dates = p.versionDatesUTC.orderBy(a => a);
  var current = hasPrevious ? refs.current[dates[1]] : refs.current[dates[0]];
  var previous = hasPrevious ? refs.current[dates[0]] : undefined;

  return (
    <Tabs id="timeMachineTabs">
      <Tab title={<span>
        {hasPrevious ? TimeMachineMessage.UIDifferences.niceToString() : TimeMachineMessage.UISnapshot.niceToString()}
        <span className="ms-2">
          <FontAwesomeIcon aria-hidden={true} icon="eye" color="lightblue" />
          {hasPrevious && <FontAwesomeIcon aria-hidden={true} icon="circle" transform="shrink-10 left-25 up-5" color="red" />}
        </span>
      </span>}
        key={"ui"} eventKey={"ui"}>
        <RenderEntityVersion
          previous={previous}
          current={current}
          currentDate={hasPrevious ? dates[1] : dates[0]}
          previousDate={hasPrevious ? dates[0] : undefined}
        />
      </Tab>
      <Tab title={hasPrevious ?
        <span>{TimeMachineMessage.DataDifferences.niceToString()}
          <FontAwesomeIcon aria-hidden={true} icon="plus" color="green" transform="up-5 right-7" />
          <FontAwesomeIcon aria-hidden={true} icon="minus" color="red" transform="down-5 left-7" />
        </span> : <span>{TimeMachineMessage.DataSnapshot.niceToString()}
          <FontAwesomeIcon aria-hidden={true} className="ms-2" icon="align-left" color="lightblue" />
        </span>}
        key={"data"} eventKey={"data"}>
        <DiffEntityVersion previous={previous} current={current} />
      </Tab>
    </Tabs>
  );
}


interface TimeMachineModalProps extends IModalProps<boolean | undefined> {
  lite: Lite<Entity>
}

export function TimeMachineModal(p: TimeMachineModalProps): React.JSX.Element {

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
        <h1 className="h4">
          <span className="display-5">{TimeMachineMessage.TimeMachine.niceToString()}</span>
          <br />
          <small className="sf-type-nice-name">
            <span style={{ color: "#aaa" }}>{getToString(p.lite)}</span>
          </small>
        </h1>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCloseClicked} />
      </div>
      <div className="modal-body">
        <TimeMachine lite={p.lite} isModal={true} />
      </div>
    </Modal>
  );
}
export namespace TimeMachineModal {
  export function show(lite: Lite<Entity>): Promise<boolean | undefined> {
    return openModal<boolean | undefined>(<TimeMachineModal lite={lite} />);
  };
}

interface TimeMachineModalCompareProps extends IModalProps<boolean | undefined> {
  lite: Lite<Entity>;
  versionDatesUTC: string[];
}

export function TimeMachineCompareModal(p: TimeMachineModalCompareProps): React.JSX.Element {

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
        <h1 className="modal-title h5">{TimeMachineMessage.CompareVersions.niceToString()}</h1>
        <button type="button" className="btn-close" data-dismiss="modal" aria-label="Close" onClick={handleCloseClicked} />
      </div>
      <div className="modal-body">
        <TimeMachineTabs lite={p.lite} versionDatesUTC={p.versionDatesUTC} />
      </div>
    </Modal>
  );
}
export namespace TimeMachineCompareModal {
  export function show(lite: Lite<Entity>, versionDatesUTC: string[]): Promise<boolean | undefined> {
    return openModal<boolean | undefined>(<TimeMachineCompareModal lite={lite} versionDatesUTC={versionDatesUTC} />);
  }
}
