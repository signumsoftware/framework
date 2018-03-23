
import * as React from 'react'
import { Link } from 'react-router-dom'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, RenderEntity } from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage, EntityPack } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import FileLine, { FileTypeSymbol } from '../../Files/FileLine'
import { DashboardEntity, PanelPartEmbedded, IPartEntity } from '../Signum.Entities.Dashboard'
import DashboardView from './DashboardView'
import { RouteComponentProps } from "react-router";
import * as QueryString from 'query-string'


import "../Dashboard.css"

interface DashboardPageProps extends RouteComponentProps<{ dashboardId: string }> {

}

interface DashboardPageState {
    dashboard?: DashboardEntity;
    entity?: Entity;
}

function getQueryEntity(props: DashboardPageProps): string {
    return QueryString.parse(props.location.search).entity as string;
}

export default class DashboardPage extends React.Component<DashboardPageProps, DashboardPageState> {

    state = { dashboard: undefined, entity: undefined } as DashboardPageState;

    componentWillMount() {
        this.loadDashboard(this.props);
        this.loadEntity(this.props);
    }

    componentWillReceiveProps(nextProps: DashboardPageProps) {
        if (this.props.match.params.dashboardId != nextProps.match.params.dashboardId)
            this.loadDashboard(nextProps);

        if (getQueryEntity(this.props) != getQueryEntity(nextProps))
            this.loadEntity(nextProps);
    }

    loadDashboard(props: DashboardPageProps) {
        this.setState({ dashboard: undefined });
        Navigator.API.fetchEntity(DashboardEntity, props.match.params.dashboardId)
            .then(d => this.setState({ dashboard: d }))
            .done();
    }

    loadEntity(props: DashboardPageProps) {
        this.setState({ entity: undefined });
        const entityKey = getQueryEntity(props);
        if (entityKey)
            Navigator.API.fetchAndForget(parseLite(entityKey))
                .then(e => this.setState({ entity: e }))
                .done();
    }

    render() {

        const dashboard = this.state.dashboard;
        const entity = this.state.entity;

        const entityKey = getQueryEntity(this.props);

        return (
            <div>
                {entityKey &&
                    <div className="row">
                        <div style={{ float: "right", textAlign: "right" }}>
                            {!entity ? <h3>{JavascriptMessage.loading.niceToString()}</h3> :
                                <h3>
                                    {Navigator.isNavigable({ entity: entity, canExecute: {} } as EntityPack<Entity>) ?
                                        <Link className="display-6" to={Navigator.navigateRoute(entity)}>{getToString(entity)}</Link> :
                                        <span className="display-6">{getToString(entity)}</span>
                                    }
                                    <br />
                                    <small className="sf-type-nice-name">{Navigator.getTypeTitle(entity, undefined)}</small>
                                </h3>
                            }
                        </div>
                    </div>
                }

                {!dashboard ? <h2 className="display-5">{JavascriptMessage.loading.niceToString()}</h2> :
                    <div className="sf-show-hover">
                        {Navigator.isNavigable({ entity: dashboard, canExecute: {} } as EntityPack<Entity>) &&
                            <Link className="sf-hide pull-right flip mt-3" style={{ textDecoration: "none" }} to={Navigator.navigateRoute(dashboard)}><span className="fa fa-edit"></span>&nbsp;Edit</Link>
                        }
                        <h2 className="display-5">{getToString(dashboard)}</h2>
                    </div>}
                {dashboard && (!entityKey || entity) && <DashboardView dashboard={dashboard} entity={entity} />}
            </div>
        );
    }
}



