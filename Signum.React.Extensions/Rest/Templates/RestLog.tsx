import * as React from 'react'
import * as moment from 'moment'
import { Tabs, Tab, Button, Form, FormControl } from 'react-bootstrap'
import { RestLogEntity } from '../Signum.Entities.Rest'
import { TypeContext, ValueLine, ValueLineType, EntityLine, EntityRepeater } from "../../../../Framework/Signum.React/Scripts/Lines";
import { } from "../../../../Framework/Signum.React/Scripts/ConfigureReactWidgets";
import {RestLogDiff, API }from '../RestClient'
import { DiffDocument } from '../../DiffLog/Templates/DiffDocument';
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'

export interface RestLogState  
{
    diff? : RestLogDiff,
    newURL : string
}

export default class RestLog extends React.Component<{ ctx: TypeContext<RestLogEntity> },RestLogState> {
 
    constructor(props:{ ctx: TypeContext<RestLogEntity> }){
        super(props);
        var prefix = Navigator.toAbsoluteUrl("~/api");
        var suffix = props.ctx.subCtx(f => f.url).value.after("/api");
        this.state = {
            
            newURL: location.protocol + "//" +location.hostname + prefix  + suffix
        }
    }
    
    render() {
        const ctx = this.props.ctx;
        return (
            <div>
                <ValueLine ctx={ctx.subCtx(f => f.startDate)} unitText={moment(ctx.value.startDate).toUserInterface().fromNow()} />
                <ValueLine ctx={ctx.subCtx(f => f.endDate)} />

                <EntityLine ctx={ctx.subCtx(f => f.user)}/>
                <ValueLine ctx={ctx.subCtx(f => f.url)} />
                <ValueLine ctx={ctx.subCtx(f => f.controller)} />
                <ValueLine ctx={ctx.subCtx(f => f.userHostAddress)} />
                <ValueLine ctx={ctx.subCtx(f => f.userHostName)} />
                <ValueLine ctx={ctx.subCtx(f => f.referrer)} />
                <ValueLine ctx={ctx.subCtx(f => f.action)}/>


                <EntityLine ctx={ctx.subCtx(f => f.exception)}/>

                <EntityRepeater ctx={ctx.subCtx(f => f.queryString)}/>
                <Form>
                    <Button bsStyle="info" onClick={() =>{ API.replayRestLog(ctx.value.id, encodeURIComponent(this.state.newURL)).then(d => this.setState({diff: d})).done()}}>Replay</Button>
                    <FormControl type="text" defaultValue={this.state.newURL}/>
                </Form>
                {this.renderCode(ctx.subCtx(f => f.requestBody))}
                
                <fieldset>
                <legend>{ctx.subCtx(f => f.responseBody).niceName()}</legend>
                <Tabs id="restTabs" defaultActiveKey="prev">
                    <Tab title="prev" eventKey="prev" className="linkTab">{this.renderPre(ctx.subCtx(f => f.responseBody).value!)}</Tab>
                    {this.state.diff && <Tab title="diff" eventKey="diff" className="linkTab">{ this.renderDiff()}</Tab>}
                    {this.state.diff && this.state.diff.current && <Tab title="curr" eventKey="curr" className="linkTab">{this.renderPre(this.state.diff.current)}</Tab>}
                </Tabs>
                </fieldset>
            </div> 
        );
    }

    renderCode(ctx: TypeContext<string | null>) {
        return (
            <fieldset>
                <legend>{ctx.niceName()}</legend>
                {this.renderPre(ctx.value!)}
            </fieldset>
        );

    }
    renderPre(text:string )
    {
        return <pre><code>{text}</code></pre>
    }
    renderDiff(): any {
        return <DiffDocument diff={this.state.diff!.diff}/>
    }
}

