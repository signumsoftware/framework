import * as React from 'react'
import * as moment from 'moment'
import { RouteComponentProps } from 'react-router'
import { StyleContext, RenderEntity, TypeContext } from '@framework/Lines';
import * as Finder from '@framework/Finder'
import EntityLink from '@framework/SearchControl/EntityLink'
import { ValueSearchControl, SearchControl, ValueSearchControlLine, ColumnOption } from '@framework/Search'
import { QueryDescription, SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '@framework/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '@framework/Signum.Entities'
import * as Operations from '@framework/Operations'
import * as Navigator from '@framework/Navigator'
import { Type } from '@framework/Reflection'
import { TimeMachineMessage } from '../Signum.Entities.DiffLog';
import { Lite } from '@framework/Signum.Entities';
import { newLite } from '@framework/Reflection';
import { EngineMessage } from '@framework/Signum.Entities';
import { NormalWindowMessage } from '@framework/Signum.Entities';
import { getTypeNiceName } from '@framework/Finder';
import { Dic } from '@framework/Globals';
import { getTypeInfo} from '@framework/Reflection';
import { UncontrolledTabs, Tab } from '@framework/Components';
import { is } from '@framework/Signum.Entities';
import * as DiffLogClient from '../DiffLogClient';
import { DiffDocument } from './DiffDocument';

interface TimeMachinePageProps extends RouteComponentProps<{ type: string; id?: string }> {

}

export interface TimeMachinePageState {
    lite?: Lite<Entity>;
    queryDescription?: QueryDescription;
}

export default class TimeMachinePage extends React.Component<TimeMachinePageProps, TimeMachinePageState> {

    constructor(props: TimeMachinePageProps) {
        super(props);

        this.state = { lite: undefined, queryDescription: undefined };
    }

    componentWillMount() {
        var p = this.props.match.params;
        var lite = newLite(p.type, p.id);

        Navigator.API.fillToStrings(lite).then(() => {
            this.setState({ lite });
        }).catch(a => {
            lite.toStr = EngineMessage.EntityWithType0AndId1NotFound.niceToString();
            this.setState({ lite });
            });

        Finder.getQueryDescription(p.type)
            .then(qd => this.setState({ queryDescription: qd }))
            .done();
    }

    searchControl?: SearchControl | null;

    render() {
        var ctx = new StyleContext(undefined, undefined);
        const lite = this.state.lite;
        if (lite == null)
            return <h4><span className="display-6">{JavascriptMessage.loading.niceToString()}</span></h4>;

        var scl = this.searchControl && this.searchControl.searchControlLoaded || undefined;
        var colIndex = scl && scl.props.findOptions.columnOptions.findIndex(a => a.token != null && a.token.fullKey == "Entity.SystemValidFrom");

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

                <br/>
                <h5>All Versions</h5>
                {   
                    this.state.queryDescription && <SearchControl ref={sc => this.searchControl = sc} findOptions={{
                        queryName: lite.EntityType,
                        filterOptions: [{ token: "Entity", operation: "EqualTo", value: lite }],
                        columnOptions: [
                            { token: "Entity.SystemValidFrom" },
                            ...Dic.getValues(this.state.queryDescription.columns).map(c => ({ token: c.name }) as ColumnOption)
                        ],
                        columnOptionsMode: "Replace",
                        orderOptions: [{ token: "Entity.SystemValidFrom", orderType: "Ascending" }],
                        systemTime: { mode: "All" }
                    }}
                        onSelectionChanged={() => this.forceUpdate()}
                    />
                }

                <br />
                <h5>Selected Versions</h5>
                <UncontrolledTabs hideOnly>
                    {scl && scl.state.selectedRows && scl.state.selectedRows.map(sr => sr.columns[colIndex!] as string).orderBy(a => a).flatMap((d, i, dates) => [
                        <Tab title={d.replace("T", " ")} key={d} eventKey={d}>
                            <RenderEntityVersion lite={lite} asOf={d} />
                        </Tab>,
                        (i < dates.length - 1) && <Tab title="<- Diff ->" key={"diff-" + d + "-" + dates[i + 1]} eventKey={"diff-" + d + "-" + dates[i+1]}>
                            <DiffEntityVersion lite={lite} validFrom={d} validTo={dates[i+1]} />
                        </Tab>
                    ])}
                </UncontrolledTabs>
            </div>
        );
    }
}



interface RenderEntityVersionProps {
    lite: Lite<Entity>;
    asOf: string;
}

interface RenderEntityVersionState {
    entity?: Entity;
}

export class RenderEntityVersion extends React.Component<RenderEntityVersionProps, RenderEntityVersionState> {

    constructor(props: RenderEntityVersionProps) {
        super(props);
        this.state = {};
    }

    componentWillMount() {
        this.loadData(this.props);
    }

    componentWillReceiveProps(newProps: RenderEntityVersionProps) {
        if (!is(newProps.lite, this.props.lite) || newProps.asOf != this.props.asOf)
            this.loadData(newProps);
    }

    loadData(props: RenderEntityVersionProps) {
        DiffLogClient.API.retrieveVersion(props.lite, props.asOf)
            .then(entity => this.setState({ entity }))
            .done();
    }

    render() {
        if (!this.state.entity)
            return <h3>{JavascriptMessage.loading.niceToString()}</h3>;

        return (
            <div>
                <RenderEntity ctx={TypeContext.root(this.state.entity, { readOnly: true })} />
            </div>
        );
    }
}



interface DiffEntityVersionProps {
    lite: Lite<Entity>;
    validFrom: string;
    validTo: string;
}

interface DiffEntityVersionState {
    diffBlock?: DiffLogClient.DiffBlock;
}

export class DiffEntityVersion extends React.Component<DiffEntityVersionProps, DiffEntityVersionState> {

    constructor(props: DiffEntityVersionProps) {
        super(props);
        this.state = {};
    }

    componentWillMount() {
        this.loadData(this.props);
    }

    componentWillReceiveProps(newProps: DiffEntityVersionProps) {
        if (!is(newProps.lite, this.props.lite) ||
            newProps.validFrom != this.props.validFrom ||
            newProps.validTo != this.props.validTo)
            this.loadData(newProps);
    }

    loadData(props: DiffEntityVersionProps) {
        DiffLogClient.API.diffVersions(props.lite, props.validFrom, props.validTo)
            .then(diffBlock => this.setState({ diffBlock }))
            .done();
    }

    render() {
        if (!this.state.diffBlock)
            return <h3>{JavascriptMessage.loading.niceToString()}</h3>;

        return (
            <div>
                <DiffDocument diff={this.state.diffBlock} />
            </div>
        );
    }
}


