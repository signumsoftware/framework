
import * as React from 'react'
import { Link } from 'react-router'
import { FormGroup, FormControlStatic, EntityComponent, EntityComponentProps, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, EntityFrame, RenderEntity} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import SelectorPopup from '../../../../Framework/Signum.React/Scripts/SelectorPopup'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import FileLine, {FileTypeSymbol} from '../../Files/FileLine'
import { DashboardEntity, PanelPartEntity, IPartEntity } from '../Signum.Entities.Dashboard'
import DashboardView from './DashboardView'



require("!style!css!../Dashboard.css");

interface DashboardPageProps extends ReactRouter.RouteComponentProps<{}, { dashboardId: string }> {

}

export default class DashboardPage extends React.Component<DashboardPageProps, { dashboard?: DashboardEntity, entity?: Entity }> {

    state = { dashboard: null, entity: null };

    componentWillMount() {
        this.loadDashboard(this.props);
        this.loadEntity(this.props);
    }

    componentWillReceiveProps(nextProps: DashboardPageProps) {
        if (this.props.routeParams.dashboardId != nextProps.routeParams.dashboardId)
            this.loadDashboard(nextProps);

        if (this.props.location.query["entity"] != nextProps.location.query["entity"])
            this.loadEntity(nextProps);
    }

    loadDashboard(props: DashboardPageProps) {
        this.setState({ dashboard: null });
        Navigator.API.fetchEntity(DashboardEntity, props.routeParams.dashboardId)
            .then(d => this.setState({ dashboard: d }))
            .done();
    }

    loadEntity(props: DashboardPageProps) {
        this.setState({ entity: null });
        if (props.location.query["entity"])
            Navigator.API.fetchAndForget(parseLite(props.location.query["entity"]))
                .then(e => this.setState({ entity: e }))
                .done();
    }

    render() {

        const dashboard = this.state.dashboard;
        const entity = this.state.entity;

        const withEntity = this.props.location.query["entity"];

        return (
            <div>
                { withEntity &&
                    <div style={{ float: "right", textAlign: "right" }}>
                        {!entity ? <h3>{JavascriptMessage.loading.niceToString() }</h3> :
                            <h3>
                                { Navigator.isNavigable(entity) ?
                                    <Link className="sf-entity-title" to={Navigator.navigateRoute(entity) }>{getToString(entity) }</Link> :
                                    <span className="sf-entity-title">{getToString(entity) }</span>
                                }
                                <br />
                                <small className="sf-type-nice-name">{Navigator.getTypeTitle(entity, null) }</small>
                            </h3>
                        }
                    </div> }

                {!dashboard ? <h2>{JavascriptMessage.loading.niceToString() }</h2> :
                    <h2>
                        {Navigator.isNavigable(dashboard) ?
                            <Link  to={Navigator.navigateRoute(dashboard) }>{getToString(dashboard) }</Link> :
                            <span>{getToString(dashboard) }</span>
                        }
                    </h2>}
                { dashboard && (!withEntity || entity) && <DashboardView dashboard={dashboard} entity={entity}/> }
            </div>
        );
    }
}



