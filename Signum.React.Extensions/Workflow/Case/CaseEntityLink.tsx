import * as React from 'react'
import * as moment from 'moment'
import { Link } from "react-router"
import { Button } from "react-bootstrap"
import { Binding, LambdaMemberType } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { newMListElement, Lite, liteKey, Entity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { InboxFilterModel, InboxFilterModelMessage, CaseNotificationState, CaseActivityEntity } from '../Signum.Entities.Workflow'
import { TypeContext, ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, EnumCheckboxList, FormGroup, FormGroupStyle, FormGroupSize } from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, ValueSearchControl, FilterOperation, OrderType, PaginationMode, ISimpleFilterBuilder, FilterOption, FindOptionsParsed } from '../../../../Framework/Signum.React/Scripts/Search'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as WorkflowClient from '../WorkflowClient'


export interface CaseEntityLinkProps extends React.Props<CaseEntityLink> {
    lite: Lite<CaseActivityEntity>;
    inSearch?: boolean;
    onNavigated?: (lite: Lite<Entity>) => void;
}

export default class CaseEntityLink extends React.Component<CaseEntityLinkProps, void>{

    render() {
        var lite = this.props.lite;

        if (!Navigator.isNavigable(lite.EntityType, undefined, this.props.inSearch || false))
            return <span data-entity={liteKey(lite)}>{this.props.children || lite.toStr}</span>;

        return (
            <Link
                to={"~/workflow/activity/" + lite.id}
                title={lite.toStr}
                onClick={this.handleClick}
                data-entity={liteKey(lite)}>
                {this.props.children || lite.toStr}
            </Link>
        );
    }

    handleClick = (event: React.MouseEvent) => {

        var lite = this.props.lite;
        var s = Navigator.getSettings(lite.EntityType)
        var avoidPopup = s != null && s.avoidPopup;

        if (avoidPopup || event.ctrlKey || event.button == 1)
            return;

        event.preventDefault();
        WorkflowClient.navigateCase(lite).then(() => {
            this.props.onNavigated && this.props.onNavigated(lite);
        }).done();;
    }
}
