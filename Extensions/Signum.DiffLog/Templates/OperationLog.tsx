import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Navigator } from '@framework/Navigator'
import { AutoLine, EntityLine } from '@framework/Lines'
import { Entity, getMixin, is, JavascriptMessage, Lite } from '@framework/Signum.Entities'
import { OperationLogEntity } from '@framework/Signum.Operations'
import { DiffLogMixin, DiffLogMessage } from '../Signum.DiffLog'
import { DiffLogClient } from '../DiffLogClient'
import { TypeContext } from '@framework/TypeContext'
import { DiffDocument } from './DiffDocument'
import { Tabs, Tab } from 'react-bootstrap';
import { LinkContainer } from '@framework/Components';
import "./DiffLog.css"
import { useAPI } from '@framework/Hooks'
import { clearSettingsActions, toAbsoluteUrl } from '@framework/AppContext'

export default function OperationLog(p : { ctx: TypeContext<OperationLogEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const ctx6 = ctx.subCtx({ labelColumns: { sm: 3 } });

  return (
    <div>
      <div className="row">
        <div className="col-sm-6">
          <EntityLine ctx={ctx6.subCtx(f => f.target)} />
          <EntityLine ctx={ctx6.subCtx(f => f.operation)} />
          <EntityLine ctx={ctx6.subCtx(f => f.origin)} />
          <EntityLine ctx={ctx6.subCtx(f => f.user)} />
        </div>
        <div className="col-sm-6">
          <AutoLine ctx={ctx6.subCtx(f => f.start)} />
          <AutoLine ctx={ctx6.subCtx(f => f.end)} />
          <EntityLine ctx={ctx6.subCtx(f => f.exception)} />
        </div>
      </div>
      <div className="code-container">
        <DiffMixinTabs ctx={ctx} />
      </div>
    </div>
  );
}

export function DiffMixinTabs(p: { ctx: TypeContext<OperationLogEntity> }): React.JSX.Element {

  var [simplify, setSimplify] = React.useState(true);

  var log = p.ctx.value;
  var mixin = getMixin(log, DiffLogMixin);

  const prev = useAPI(() => mixin.initialState.text == null ? Promise.resolve(null) : DiffLogClient.API.getPreviousOperationLog(log.id!), [log]);  

  const next = useAPI(() => mixin.finalState.text == null ? Promise.resolve(null) : DiffLogClient.API.getNextOperationLog(log.id!), [log]);  

  var mctx = p.ctx.subCtx(DiffLogMixin);


  function renderPrev(prev: Lite<OperationLogEntity>) {
    return (
      <Tab eventKey="prev" className="linkTab" title={
        <LinkContainer to={Navigator.navigateRoute(prev)} onClick={e => {
          if (!(e.ctrlKey || e.button == 1)) {
            Navigator.API.fetchEntityPack(prev).then(ep => p.ctx.frame!.onReload(ep));
            e.preventDefault();
          }
        }}>
          <span title={DiffLogMessage.NavigatesToThePreviousOperationLog.niceToString()}>
             <FontAwesomeIcon aria-hidden={true} icon="circle-arrow-left" />
            &nbsp;
            {DiffLogMessage.PreviousLog.niceToString()}
          </span>
        </LinkContainer> as any
      }>
      </Tab>
    );
  }

  function renderPrevDisabled() {
    return (
      <Tab eventKey="prev" disabled title={
          <span title={DiffLogMessage.NavigatesToThePreviousOperationLog.niceToString()}>
            <FontAwesomeIcon aria-hidden={true} icon="circle-arrow-left" />
            &nbsp;
            {DiffLogMessage.PreviousLog.niceToString()}
          </span>
      }>
      </Tab>
    );
  }



  function renderPrevDiff(prev: string, current: string) {
    const eq = prev == current;

    const title = (
      <span title={DiffLogMessage.DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState.niceToString()}>
        <FontAwesomeIcon aria-hidden={true} icon="backward-fast" className={`colorIcon red ${eq ? "mini" : ""}`} />
        <FontAwesomeIcon aria-hidden={true} icon="backward-step" className={`colorIcon green ${eq ? "mini" : ""}`} />
      </span>
    );

    return (
      <Tab eventKey="prevDiff" title={title as any}>
        <DiffDocument first={prev} second={current} />
      </Tab>
    );
  }

  function renderPrevDiffDisabled() {

    const title = (
      <span title={DiffLogMessage.DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState.niceToString()}>
        <FontAwesomeIcon aria-hidden={true} icon="backward-fast" className={`colorIcon gray`} />
        <FontAwesomeIcon aria-hidden={true} icon="backward-step" className={`colorIcon gray`} />
      </span>
    );

    return (
      <Tab eventKey="prevDiff" title={title as any} disabled>
      </Tab>
    );
  }

  function renderInitialState(initialState : string) {
    return (
      <Tab eventKey="initialState" title={mctx.niceName(d => d.initialState)}>
        <pre><code>{initialState}</code></pre>
      </Tab>
    );
  }

  function renderDiff(initialState: string, finalState: string) {

    const eq = initialState == finalState;

    const title = (
      <span title={DiffLogMessage.DifferenceBetweenInitialStateAndFinalState.niceToString()}>
        <FontAwesomeIcon aria-hidden={true} icon="backward-step" className={`colorIcon red ${eq ? "mini" : ""}`} />
        <FontAwesomeIcon aria-hidden={true} icon="forward-step" className={`colorIcon green ${eq ? "mini" : ""}`} />
      </span>
    );

    return (
      <Tab eventKey="diff" title={title as any}>
        <DiffDocument first={initialState} second={finalState} />
      </Tab>
    );
  }

  function renderFinalState(finalState: string) {
    return (
      <Tab eventKey="finalState" title={mctx.niceName(d => d.finalState)}>
        <pre><code>{finalState}</code></pre>
      </Tab>
    );
  }

  function renderNextDiff(current: string, next: string) {
    const eq = current == next;

    const title = (
      <span title={DiffLogMessage.DifferenceBetweenFinalStateAndTheInitialStateOfNextLog.niceToString()}>

        <FontAwesomeIcon aria-hidden={true} icon="forward-step" className={`colorIcon red ${eq ? "mini" : ""}`} />
        <FontAwesomeIcon aria-hidden={true} icon="forward-fast" className={`colorIcon green ${eq ? "mini" : ""}`} />
      </span>
    );

    return (
      <Tab eventKey="nextDiff" title={title as any}>
        <DiffDocument first={current} second={next} />
      </Tab>
    );
  }

  function renderNext(next: Lite<OperationLogEntity>) {
    return (
      <Tab eventKey="next" className="linkTab" title={
        <LinkContainer to={Navigator.navigateRoute(next)} onClick={e => {
          if (!(e.ctrlKey || e.button == 1)) {
            Navigator.API.fetchEntityPack(next).then(ep => p.ctx.frame!.onReload(ep));
            e.preventDefault();
          }
        }}>
          <span title={DiffLogMessage.NavigatesToTheNextOperationLog.niceToString()}>
            {DiffLogMessage.NextLog.niceToString()}
            &nbsp;
              <FontAwesomeIcon aria-hidden={true} icon="circle-arrow-right" />
          </span>
        </LinkContainer> as any}>
      </Tab>
    );
  }

  function renderCurrentEntity(target: Lite<Entity>) {
    return (
      <Tab eventKey="next" className="linkTab" title={
        <LinkContainer to={Navigator.navigateRoute(target)} onClick={e => {
          e.preventDefault();
          window.open(toAbsoluteUrl(Navigator.navigateRoute(target)));
        }}>
          <span title={DiffLogMessage.NavigatesToTheCurrentEntity.niceToString()}>
            {DiffLogMessage.CurrentEntity.niceToString()}
            &nbsp;
            <FontAwesomeIcon aria-hidden={true} icon="up-right-from-square" />
          </span>
        </LinkContainer> as any}>
      </Tab>
    );
  }
  const target = p.ctx.value.target;

  var prevSimple = React.useMemo(() => simplifyDump(prev?.dump, simplify), [prev, simplify]);
  var initialSimple = React.useMemo(() => simplifyDump(mixin.initialState.text, simplify), [mixin.initialState.text, simplify]);
  var finalSimple = React.useMemo(() => simplifyDump(mixin.finalState.text, simplify), [mixin.finalState.text, simplify]);
  var nextSimple = React.useMemo(() => simplifyDump(next?.dump, simplify), [next, simplify]);

  return (
    <div>
      <label>
        <input type="checkbox" className="form-check-input" checked={simplify} onChange={e => setSimplify(e.currentTarget.checked)} /> Simplify Changes
      </label>
      <Tabs id="diffTabs" defaultActiveKey="diff" key={p.ctx.value.id} mountOnEnter>
        {prev ? renderPrev(prev.operationLog) : renderPrevDisabled()}
        {prevSimple && initialSimple ? renderPrevDiff(prevSimple, initialSimple) : renderPrevDiffDisabled()}
        {initialSimple && renderInitialState(initialSimple)}
        {initialSimple && finalSimple && renderDiff(initialSimple, finalSimple)}
        {finalSimple && renderFinalState(finalSimple)}
        {finalSimple && nextSimple && renderNextDiff(finalSimple, nextSimple)}
        {next === undefined ? undefined : (next?.operationLog ? renderNext(next.operationLog) : target && renderCurrentEntity(target))}
      </Tabs>
    </div>
  );
}


const LiteImpRegex = /^(?<space> *)(?<prop>\w[\w\d_]+) = new LiteImp</;

function simplifyDump(text: string | null | undefined, simplifyFatLites: boolean) {

  if (text == null)
    return null;

  var lines = text.replaceAll("\r", "").split("\n");
  
  if (!simplifyFatLites)
    return lines.join("\n");

  for (var i = 0; i < lines.length; i++) {
    var current = lines[i];
    if (current.contains("= new LiteImp<") && !current.endsWith(",")) {
      var match = LiteImpRegex.exec(current);
      if (match) {
        var spaces = match.groups!["space"];
        if (lines[i + 1] == spaces + "{") {
          var lastIndex = lines.indexOf(spaces + "},", i + 1);

          if (lastIndex != -1) {
            lines.splice(i + 1, lastIndex - (i + 1) + 1);
          }

          lines[i] = current + " { Entity = /* Loaded */ },";
        }
      }
    }
  }

  return lines.join("\n");
}

