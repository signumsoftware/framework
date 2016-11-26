
import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { TypeContext, StyleOptions, EntityFrame } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { TypeInfo, getTypeInfo, parseId, GraphExplorer, PropertyRoute, ReadonlyBinding, } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import { EntityPack, Entity, Lite, JavascriptMessage, entityInfo, getToString } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { renderWidgets, renderEmbeddedWidgets, WidgetContext } from '../../../../Framework/Signum.React/Scripts/Frames/Widgets'
import ValidationErrors from '../../../../Framework/Signum.React/Scripts/Frames/ValidationErrors'
import ButtonBar from '../../../../Framework/Signum.React/Scripts/Frames/ButtonBar'
import { ToolbarElementEntity, ToolbarElementType, ToolbarMenuEntity } from '../Signum.Entities.Toolbar'
import * as ToolbarClient from '../ToolbarClient'

import { Navbar, Nav, NavItem, NavDropdown, MenuItem } from 'react-bootstrap'
import { LinkContainer, IndexLinkContainer } from 'react-router-bootstrap'

export default class ToolbarRenderer extends React.Component<void, { response?: ToolbarClient.ToolbarResponse }>
{
    constructor(props: any) {
        super(props);
        this.state = {};
    }

    componentWillMount() {
        ToolbarClient.API.getCurrentToolbar()
            .then(res => this.changeState(s => s.response = res))
            .done();
    }

    render() {

        const r = this.state.response;

        if (!r)
            return null;

        return (
            <ul className="nav navbar-nav">
                {r.elements && r.elements.map((res, i) => this.renderMenu(res, i))}
            </ul>
        );
    }

    renderMenu(res: ToolbarClient.ToolbarResponse, index: number) {



        return (
            <NavDropdown title={res.label || res.lite!.toStr} id={"menu-" + index} >
            </NavDropdown>
        );
    }

    renderMenuItem(res: ToolbarClient.ToolbarResponse, index: number) {

    }

    renderSubMenuItem(res: ToolbarClient.ToolbarResponse) {

    }
}

