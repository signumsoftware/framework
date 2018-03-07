import * as React from 'react'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { Entity, getMixin, is, JavascriptMessage, Lite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { OperationLogEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { DiffLogMixin, DiffLogMessage } from '../Signum.Entities.DiffLog'
import { API, DiffLogResult, DiffPair } from '../DiffLogClient'
import {SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { DiffDocument } from './DiffDocument'
import "./DiffLog.css"
import { UncontrolledTabs, Tab, LinkContainer } from '../../../../Framework/Signum.React/Scripts/Components';

export default class OperationLog extends React.Component<{ ctx: TypeContext<OperationLogEntity> }> {

    render() {
        
        const ctx = this.props.ctx;
        const ctx6 = ctx.subCtx({ labelColumns: { sm: 3 } });

        return (
            <div>
                <div className="row">
                    <div className="col-sm-6">
                        <EntityLine ctx={ctx6.subCtx(f => f.target) }  />
                        <EntityLine ctx={ctx6.subCtx(f => f.operation) }  />
                        <EntityLine ctx={ctx6.subCtx(f => f.origin) }  />
                        <EntityLine ctx={ctx6.subCtx(f => f.user) }  />
                    </div>
                    <div className="col-sm-6">
                        <ValueLine ctx={ctx6.subCtx(f => f.start) }  />
                        <ValueLine ctx={ctx6.subCtx(f => f.end) }  />
                        <EntityLine ctx={ctx6.subCtx(f => f.exception) }  />
                    </div>
                </div>
                <div className="code-container">
                    <DiffMixinTabs ctx={ctx} />
                </div>
            </div>
        );
    }
}

export class DiffMixinTabs extends React.Component<{ ctx: TypeContext<OperationLogEntity> }, { result?: DiffLogResult }>
{
    constructor(props: any) {
        super(props);
        this.state = { result: undefined };
    }

    componentWillMount() {
        API.diffLog(this.props.ctx.value.id)
            .then(result => this.setState({ result }))
            .done();
    }


    componentWillReceiveProps(nextProps: { ctx: TypeContext<OperationLogEntity> }) {
        if (!is(nextProps.ctx.value, this.props.ctx.value))
            API.diffLog(nextProps.ctx.value.id)
                .then(result => this.setState({ result }))
                .done();
    }

    mctx()
    {
        return this.props.ctx.subCtx(DiffLogMixin);
    }

    render() {
        const result = this.state.result;
        const target = this.props.ctx.value.target;
        return (
            <UncontrolledTabs id="diffTabs" defaultEventKey="diff">
                {result && result.prev && this.renderPrev(result.prev)}
                {result && result.diffPrev && this.renderPrevDiff(result.diffPrev)}
                {this.renderInitialState() }
                {this.renderDiff() }
                {this.renderFinalState()}
                {result && result.diffNext && this.renderNextDiff(result.diffNext)}
                {result && (result.next ? this.renderNext(result.next) : target && this.renderCurrentEntity(target))}            
            </UncontrolledTabs>
        );
    }

    renderPrev(prev: Lite<OperationLogEntity>) {
        return (
            <Tab eventKey="prev" className="linkTab" title={
                <LinkContainer to={Navigator.navigateRoute(prev) }>
                    <span title={DiffLogMessage.NavigatesToThePreviousOperationLog.niceToString() }>
                        {DiffLogMessage.PreviousLog.niceToString() }
                        &nbsp;
                        <span className="fa fa-external-link"></span>
                    </span>
                </LinkContainer> as any
            }>
            </Tab>
        );
    }

    renderPrevDiff(diffPrev: Array<DiffPair<Array<DiffPair<string>>>>) {

        const eq = isEqual(diffPrev);

        const title =  (
            <span title={ DiffLogMessage.DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState.niceToString()}>
                <span className={`fa fa-fast-backward colorIcon red ${eq ? "mini" : ""}`}></span>
                <span className={`fa fa-step-backward colorIcon green ${eq ? "mini" : ""}`}></span>
            </span>
        );

        return (
            <Tab eventKey="prevDiff" title={title as any}>
                <DiffDocument  diff={diffPrev} />
            </Tab>
        );
    }

    renderInitialState() {
        return (
            <Tab eventKey="initialState" title={this.mctx().niceName(d => d.initialState)}>
                <pre><code>{this.mctx().value.initialState}</code></pre>
            </Tab>
        );
    }

    renderDiff() {

        if (!this.state.result) {
            return <Tab eventKey="diff" title={JavascriptMessage.loading.niceToString() } />
        }

        const eq = !this.state.result.diff || isEqual(this.state.result.diff);

        const title = (
            <span title={ DiffLogMessage.DifferenceBetweenInitialStateAndFinalState.niceToString() }>
                <span className={`fa fa-step-backward colorIcon red ${eq ? "mini" : ""}`}></span>
                <span className={`fa fa-step-forward colorIcon green ${eq ? "mini" : ""}`}></span>
            </span>
        );

        return (
            <Tab eventKey="diff" title={title as any}>
                {this.state.result.diff && <DiffDocument diff={this.state.result.diff}/> }
            </Tab>
        );
    }

    renderFinalState() {
        return (
            <Tab eventKey="finalState" title={this.mctx().niceName(d => d.finalState) }>
                <pre><code>{this.mctx().value.finalState}</code></pre>
            </Tab>
        );
    }

    renderNextDiff(diffNext: Array<DiffPair<Array<DiffPair<string>>>>) {

        const eq = isEqual(diffNext);

        const title = (
            <span title={ DiffLogMessage.DifferenceBetweenFinalStateAndTheInitialStateOfNextLog.niceToString() }>
                <span className={`fa fa-step-forward colorIcon red ${eq ? "mini" : ""}`}></span>
                <span className={`fa fa-fast-forward colorIcon green ${eq ? "mini" : ""}`}></span>
            </span>
        );

        return (
            <Tab eventKey="nextDiff" title={title as any}>
                <DiffDocument diff={diffNext} />
            </Tab>
        );
    }

    renderNext(next: Lite<OperationLogEntity>) {
        return (
            <Tab eventKey="next" className="linkTab" title={
                <LinkContainer to={Navigator.navigateRoute(next) }>
                    <span title={DiffLogMessage.NavigatesToTheNextOperationLog.niceToString() }>
                        {DiffLogMessage.NextLog.niceToString() }
                        &nbsp;
                        <span className="fa fa-external-link"></span>
                    </span>
                </LinkContainer> as any}>
            </Tab>
        ); 
    }

    renderCurrentEntity(target: Lite<Entity>) {
        return (
            <Tab eventKey="next" className="linkTab" title={
                <LinkContainer to={Navigator.navigateRoute(target) }>
                    <span title={DiffLogMessage.NavigatesToTheCurrentEntity.niceToString() }>
                        {DiffLogMessage.CurrentEntity.niceToString() }
                        &nbsp;
                        <span className="fa fa-external-link"></span>
                    </span>
                </LinkContainer> as any}>
            </Tab>
        );
    }
}

function isEqual(diff: Array<DiffPair<Array<DiffPair<string>>>>) {
    return diff.every(a => a.Action == "Equal" && a.Value.every(b => b.Action == "Equal"));
}


