import * as React from 'react'
import { Tabs, Tab } from 'react-bootstrap'
import * as numbro from 'numbro'
import * as moment from 'moment'
import { classes } from '../../../Framework/Signum.React/Scripts/Globals'
import { StyleContext } from '../../../Framework/Signum.React/Scripts/TypeContext'
import { ajaxPost } from '../../../Framework/Signum.React/Scripts/Services'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { CountSearchControl, SearchControl } from '../../../Framework/Signum.React/Scripts/Search'
import EntityLink from '../../../Framework/Signum.React/Scripts/SearchControl/EntityLink'
import { QueryDescription, SubTokensOptions } from '../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { API, Options, CompilationError } from './DynamicClient'
import CSharpCodeMirror from '../Codemirror/CSharpCodeMirror'
import * as AuthClient from '../Authorization/AuthClient'
import { DynamicPanelPermission } from './Signum.Entities.Dynamic'

require("!style!css!./DynamicPanelPage.css");

interface DynamicPanelProps extends ReactRouter.RouteComponentProps<{}, {}> {

}

export default class DynamicPanelPage extends React.Component<DynamicPanelProps, void> {

    render() {

        AuthClient.asserPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel);
        
        var tabList = [<Tab eventKey="compile" title="Compile">
            <DynamicCompileTab />
        </Tab>]
            .concat(Options.onGetDynamicTab.map(a => a()));

        var tabs = React.cloneElement(<Tabs defaultActiveKey="compile" id="dynamicPanelTabs" />, undefined, ...tabList);
        
        return (
            <div>
                <h2>Dynamic Panel</h2>

                {tabs}
            </div>
        );
    }
}


interface DynamicCompileTabState {
    complationErrors?: CompilationError[];
    selectedErrorIndex?: number;
    applicationRestarting?: moment.Moment;
}

export class DynamicCompileTab extends React.Component<{}, DynamicCompileTabState>{

    constructor(props: any) {
        super(props);
        this.state = {};
    }

    handleCompile = (e: React.MouseEvent) => {
        e.preventDefault();
        API.getCompilationErrors()
            .then(errors => this.changeState(s => { s.complationErrors = errors; s.selectedErrorIndex = undefined; }))
            .done();
    }
    

    handleRestartApplication = (e: React.MouseEvent) => {
        e.preventDefault();
        API.restartApplication()
            .then(() => {
                this.changeState(s => s.applicationRestarting = moment());
                API.pingApplication()
                    .then(s => {
                        this.changeState(s => s.applicationRestarting = undefined);
                        if (confirm("Server restarted. Page should be reloaded")) {
                            window.location.reload(true); 
                        }
                    })
                    .done();
            })
            .done();
    }


    render() {

        var sc = new StyleContext(undefined, { labelColumns: { sm: 2 } });

        const lines = Options.onGetDynamicLine.map(f => f(sc));
        const lineContainer = React.cloneElement(<div className="form-horizontal" />, undefined, ...lines);

        const errors = this.state.complationErrors;

        return (
            <div>
                {lineContainer}
                <br />
                {<a href="#" className="sf-button btn btn-success" onClick={this.handleCompile}>Compile</a>}
                {errors && this.renderCompileResult(errors)}
            </div>
        );
    }

   

    renderCompileResult(errors: CompilationError[]) {

        return (
            <div>
                <br/>
                <div className={`alert alert-${errors.length == 0 ? "success" : "danger"}`} role="alert">
                    <strong>{errors.length} Errors!</strong> {errors.length == 0 ?
                        "The dynamic code compiled successfully" :
                        "Please fix this errors in the dynamic entities"}
                </div>
                <br />
                {errors.length > 0 ? this.renderErrorTable(errors) : this.renderRestart()}
            </div>
        );
    }

    renderErrorTable(errors: CompilationError[]) {
        var err = this.state.selectedErrorIndex == null ? undefined : errors[this.state.selectedErrorIndex]

        return (
            <div>
                <table className="table table-condensed">
                    <thead style={{ color: "#a94464" }}>
                        <tr>
                            <th>Error Number</th>
                            <th>Error Text</th>
                            <th>File</th>
                        </tr>
                    </thead>
                    <tbody>
                        {
                            errors.map((e, i) =>
                                <tr key={i}
                                    onClick={() => this.changeState(s => s.selectedErrorIndex = i)}
                                    className={classes("dynamic-error-line", i == this.state.selectedErrorIndex ? "active" : undefined)}>
                                    <td>{e.errorNumber}</td>
                                    <td>{e.errorText}</td>
                                    <td>{e.fileName}({e.line}:{e.column})</td>
                                </tr>
                            )
                        }
                    </tbody>
                </table>
                {err && <div>
                    <h4>{err.fileName}</h4>
                    <CSharpCodeMirror
                        script={err.fileContent}
                        isReadOnly={true}
                        errorLineNumber={err.line} />
                </div>
                }
            </div>
        );
    }

    renderRestart() {
        return (
            <div>
                {
                    this.state.applicationRestarting ?
                        this.renderProgress(this.state.applicationRestarting) :
                        AuthClient.isPermissionAuthorized(DynamicPanelPermission.RestartApplication) &&
                        <a href="#" className="sf-button btn btn-danger" onClick={this.handleRestartApplication}>Restart Application</a>
                }
            </div>
        );
    }
    

    renderProgress(since: moment.Moment) {

        return (
            <div className="progress">
                <div className="progress-bar progress-bar-striped progress-bar-warning active" role="progressbar" style={{ width: "100%" }}>
                    <span>Restarting...</span>
                </div>
            </div>
        );
    }
}




