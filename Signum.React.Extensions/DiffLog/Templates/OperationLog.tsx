import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as Navigator from '@framework/Navigator'
import { ValueLine, EntityLine } from '@framework/Lines'
import { Entity, is, JavascriptMessage, Lite } from '@framework/Signum.Entities'
import { OperationLogEntity } from '@framework/Signum.Entities.Basics'
import { DiffLogMixin, DiffLogMessage } from '../Signum.Entities.DiffLog'
import { API, DiffLogResult, DiffPair } from '../DiffLogClient'
import { TypeContext } from '@framework/TypeContext'
import { DiffDocument } from './DiffDocument'
import { Tabs, Tab } from 'react-bootstrap';
import { LinkContainer } from '@framework/Components';
import "./DiffLog.css"
import { useAPI } from '../../../../Framework/Signum.React/Scripts/Hooks'

export default function OperationLog(p : { ctx: TypeContext<OperationLogEntity> }){
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
          <ValueLine ctx={ctx6.subCtx(f => f.start)} />
          <ValueLine ctx={ctx6.subCtx(f => f.end)} />
          <EntityLine ctx={ctx6.subCtx(f => f.exception)} />
        </div>
      </div>
      <div className="code-container">
        <DiffMixinTabs ctx={ctx} />
      </div>
    </div>
  );
}

export function DiffMixinTabs(p: { ctx: TypeContext<OperationLogEntity> }) {

  const result = useAPI(() => API.diffLog(p.ctx.value.id!), [p.ctx.value.id]);

  var mctx = p.ctx.subCtx(DiffLogMixin);


  function renderPrev(prev: Lite<OperationLogEntity>) {
    return (
      <Tab eventKey="prev" className="linkTab" title={
        <LinkContainer to={Navigator.navigateRoute(prev)}>
          <span title={DiffLogMessage.NavigatesToThePreviousOperationLog.niceToString()}>
            {DiffLogMessage.PreviousLog.niceToString()}
            &nbsp;
                        <FontAwesomeIcon icon="external-link-alt" />
          </span>
        </LinkContainer> as any
      }>
      </Tab>
    );
  }

  function renderPrevDiff(diffPrev: Array<DiffPair<Array<DiffPair<string>>>>) {
    const eq = isEqual(diffPrev);

    const title = (
      <span title={DiffLogMessage.DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState.niceToString()}>
        <FontAwesomeIcon icon="fast-backward" className={`colorIcon red ${eq ? "mini" : ""}`} />
        <FontAwesomeIcon icon="step-backward" className={`colorIcon green ${eq ? "mini" : ""}`} />
      </span>
    );

    return (
      <Tab eventKey="prevDiff" title={title as any}>
        <DiffDocument diff={diffPrev} />
      </Tab>
    );
  }

  function renderInitialState() {
    return (
      <Tab eventKey="initialState" title={mctx.niceName(d => d.initialState)}>
        <pre><code>{mctx.value.initialState}</code></pre>
      </Tab>
    );
  }

  function renderDiff() {
    if (!result) {
      return <Tab eventKey="diff" title={JavascriptMessage.loading.niceToString()} />
    }

    const eq = !result.diff || isEqual(result.diff);

    const title = (
      <span title={DiffLogMessage.DifferenceBetweenInitialStateAndFinalState.niceToString()}>
        <FontAwesomeIcon icon="step-backward" className={`colorIcon red ${eq ? "mini" : ""}`} />
        <FontAwesomeIcon icon="step-forward" className={`colorIcon green ${eq ? "mini" : ""}`} />
      </span>
    );

    return (
      <Tab eventKey="diff" title={title as any}>
        {result.diff && <DiffDocument diff={result.diff} />}
      </Tab>
    );
  }

  function renderFinalState() {
    return (
      <Tab eventKey="finalState" title={mctx.niceName(d => d.finalState)}>
        <pre><code>{mctx.value.finalState}</code></pre>
      </Tab>
    );
  }

  function renderNextDiff(diffNext: Array<DiffPair<Array<DiffPair<string>>>>) {
    const eq = isEqual(diffNext);

    const title = (
      <span title={DiffLogMessage.DifferenceBetweenFinalStateAndTheInitialStateOfNextLog.niceToString()}>

        <FontAwesomeIcon icon="step-forward" className={`colorIcon red ${eq ? "mini" : ""}`} />
        <FontAwesomeIcon icon="fast-forward" className={`colorIcon green ${eq ? "mini" : ""}`} />
      </span>
    );

    return (
      <Tab eventKey="nextDiff" title={title as any}>
        <DiffDocument diff={diffNext} />
      </Tab>
    );
  }

  function renderNext(next: Lite<OperationLogEntity>) {
    return (
      <Tab eventKey="next" className="linkTab" title={
        <LinkContainer to={Navigator.navigateRoute(next)}>
          <span title={DiffLogMessage.NavigatesToTheNextOperationLog.niceToString()}>
            {DiffLogMessage.NextLog.niceToString()}
            &nbsp;
                        <FontAwesomeIcon icon="external-link-alt" />
          </span>
        </LinkContainer> as any}>
      </Tab>
    );
  }

  function renderCurrentEntity(target: Lite<Entity>) {
    return (
      <Tab eventKey="next" className="linkTab" title={
        <LinkContainer to={Navigator.navigateRoute(target)}>
          <span title={DiffLogMessage.NavigatesToTheCurrentEntity.niceToString()}>
            {DiffLogMessage.CurrentEntity.niceToString()}
            &nbsp;
                        <FontAwesomeIcon icon="external-link-alt" />
          </span>
        </LinkContainer> as any}>
      </Tab>
    );
  }
  const target = p.ctx.value.target;
  return (
    <Tabs id="diffTabs" defaultActiveKey="diff">
      {result?.prev && renderPrev(result.prev)}
      {result?.diffPrev && renderPrevDiff(result.diffPrev)}
      {renderInitialState()}
      {renderDiff()}
      {renderFinalState()}
      {result?.diffNext && renderNextDiff(result.diffNext)}
      {result && (result.next ? renderNext(result.next) : target && renderCurrentEntity(target))}
    </Tabs>
  );
}

function isEqual(diff: Array<DiffPair<Array<DiffPair<string>>>>) {
  return diff.every(a => a.action == "Equal" && a.value.every(b => b.action == "Equal"));
}


