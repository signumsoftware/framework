import * as React from "react"
import { Router, Route, Redirect } from "react-router"

import { Dic } from '../Globals';
import { Lite, Entity, liteKey, ModifiableEntity } from '../Signum.Entities';
import * as Navigator from '../Navigator';
import { Link  } from 'react-router-dom';

export interface EntityLinkProps extends React.HTMLAttributes<HTMLAnchorElement>, React.Props<EntityLink> {
    lite: Lite<Entity>;
    inSearch?: boolean;
    onNavigated?: (lite: Lite<Entity>) => void;
    getViewPromise?: (e: ModifiableEntity) => undefined | string | Navigator.ViewPromise<ModifiableEntity>;
}


export default class EntityLink extends React.Component<EntityLinkProps>{

    render() {
        const { lite, inSearch, children, onNavigated, getViewPromise, ...htmlAtts } = this.props;

        if (!Navigator.isNavigable(lite.EntityType, undefined, this.props.inSearch || false))
            return <span data-entity={liteKey(lite) }>{this.props.children || lite.toStr}</span>;


        return (
            <Link
                to={Navigator.navigateRoute(lite)}
                title={this.props.title || lite.toStr}
                onClick={this.handleClick}
                data-entity={liteKey(lite)}
                {...(htmlAtts as React.HTMLAttributes<HTMLAnchorElement>) }>
                {children || lite.toStr}                
            </Link>
        );
    }

    handleClick = (event: React.MouseEvent<any>) => {
       
        const lite = this.props.lite;

        const s = Navigator.getSettings(lite.EntityType)

        const avoidPopup = s != undefined && s.avoidPopup;

        event.preventDefault();
        
        if (event.ctrlKey || event.button == 1 || avoidPopup) {
            window.open(Navigator.navigateRoute(lite));
            return;
        }
        
        Navigator.navigate(lite, { getViewPromise: this.props.getViewPromise }).then(() => {
            this.props.onNavigated && this.props.onNavigated(lite);
        }).done();
    }
}