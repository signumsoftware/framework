import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"

import { Dic } from '../Globals';
import { Lite, Entity, liteKey } from '../Signum.Entities';
import * as Navigator from '../Navigator';
import { Link  } from 'react-router';

export interface EntityLinkProps extends React.HTMLAttributes, React.Props<EntityLink> {
    lite: Lite<Entity>;
    inSearch?: boolean;
    onNavigated?: (lite: Lite<Entity>) => void;
}


export default class EntityLink extends React.Component<EntityLinkProps, void>{

    render() {
        const { lite, inSearch, children, ...htmlAtts} = this.props;

        if (!Navigator.isNavigable(lite.EntityType, undefined, this.props.inSearch || false))
            return <span data-entity={liteKey(lite) }>{this.props.children || lite.toStr}</span>;


        return (
            <Link
                to={Navigator.navigateRoute(lite) }
                title={lite.toStr}
                onClick={this.handleClick}
                data-entity={liteKey(lite)}
                {...(htmlAtts as React.HTMLAttributes) }>
                {children || lite.toStr}                
            </Link>
        );
    }

    handleClick = (event: React.MouseEvent) => {

        const lite = this.props.lite;

        const s = Navigator.getSettings(lite.EntityType)

        const avoidPopup = s != undefined && s.avoidPopup;

        if (avoidPopup || event.ctrlKey || event.button == 1)
            return;

        event.preventDefault();

        Navigator.navigate(lite).then(() => {
            this.props.onNavigated && this.props.onNavigated(lite);
        }).done();
    }
}