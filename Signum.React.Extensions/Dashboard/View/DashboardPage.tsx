import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'

import { Link } from 'react-router-dom'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, RenderEntity } from '@framework/Lines'
import * as Finder from '@framework/Finder'
import { QueryDescription, SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '@framework/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage, EntityPack } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import FileLine, { FileTypeSymbol } from '../../Files/FileLine'
import { DashboardEntity, PanelPartEmbedded, IPartEntity } from '../Signum.Entities.Dashboard'
import DashboardView from './DashboardView'
import { RouteComponentProps } from "react-router";
import * as QueryString from 'query-string'
import * as AuthClient from '../../Authorization/AuthClient'

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
                        {!AuthClient.navigatorIsReadOnly(DashboardEntity, { entity: dashboard, canExecute: {} } as EntityPack<Entity>) &&
                            <Link className="sf-hide float-right flip mt-3" style={{ textDecoration: "none" }} to={Navigator.navigateRoute(dashboard)}><FontAwesomeIcon icon="edit" />&nbsp;Edit</Link>
                        }
                        <h2 className="display-5">{getToString(dashboard)}</h2>
                    </div>}
                {dashboard && (!entityKey || entity) && <DashboardView dashboard={dashboard} entity={entity} />}
            </div>
        );
    }
}



