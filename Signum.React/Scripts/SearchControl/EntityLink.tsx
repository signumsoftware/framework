import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"

import { Lite, Entity, liteKey } from '../Signum.Entities';
import { navigateRoute, isNavigable, currentHistory, getSettings, view, navigate } from '../Navigator';
import { Link  } from 'react-router';

export interface EntityLinkProps extends React.Props<EntityLink> {
    lite: Lite<Entity>;
}


export default class EntityLink extends React.Component<EntityLinkProps, void>{

    render() {
        var lite = this.props.lite;

        if (!isNavigable(lite.EntityType))
            return <span data-entity={liteKey(lite) }>{this.props.children || lite.toStr}</span>;
        
        return (
            <Link
                to={navigateRoute(lite) }
                title={lite.toStr}
                onClick={this.handleClick}
                data-entity={liteKey(lite) }>
                {this.props.children || lite.toStr}
            </Link>
        );
    }

    handleClick = (event: React.MouseEvent) => {

        var lite = this.props.lite;

        var s = getSettings(lite.EntityType)

        var avoidPopup = s != null && s.avoidPopup;

        if (avoidPopup || event.ctrlKey || event.button == 1)
            return;

        event.preventDefault();

        navigate(lite);
    }
}