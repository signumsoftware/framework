import * as React from 'react'
import { Link } from 'react-router'
import * as moment from 'moment'
import { Input, Tab } from 'react-bootstrap'
//import { DatePicker } from 'react-widgets'
import { ModifiableEntity, Lite, IEntity, Entity, EntityControlMessage, JavascriptMessage } from 'Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from 'Framework/Signum.React/Scripts/Navigator'
import * as Finder from 'Framework/Signum.React/Scripts/Finder'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from 'Framework/Signum.React/Scripts/TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll} from 'Framework/Signum.React/Scripts/Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from 'Framework/Signum.React/Scripts/Lines/LineBase'


export interface EntityBaseProps extends LineBaseProps {
    view?: boolean;
    navigate?: boolean;
    create?: boolean;
    find?: boolean;
    remove?: boolean;
    partialViewName?: string;
    ctx: TypeContext<ModifiableEntity | Lite<IEntity>>;
}


export abstract class EntityBase<T extends EntityBaseProps, S> extends LineBase<T, S>
{
    static configureEntityButtons(props: EntityBaseProps) {

        var type = props.ctx.propertyRoute.member.type;

        props.create = type.isEmbedded ? Navigator.isCreable(type.name, false) :
            type.name == IsByAll ? false :
                getTypeInfos(type).some(ti=> Navigator.isCreable(ti, false));

        props.view = type.isEmbedded ? Navigator.isViewable(type.name, props.partialViewName) :
            type.name == IsByAll ? true :
                getTypeInfos(type).some(ti=> Navigator.isViewable(ti, props.partialViewName));

        props.navigate = type.isEmbedded ? Navigator.isNavigable(type.name, props.partialViewName) :
            type.name == IsByAll ? true :
                getTypeInfos(type).some(ti=> Navigator.isNavigable(ti, props.partialViewName));

        props.find = type.isEmbedded ? false :
            type.name == IsByAll ? false :
                getTypeInfos(type).some(ti=> Navigator.isFindable(ti));
    }




    handleViewClick: (event: React.SyntheticEvent) => {};
    renderViewButton(props: EntityBaseProps, btn: boolean) {
        if (!props.view)
            return null;

        return <a className={classes("sf-line-button", "sf-view", btn ? "btn btn-default" : null) }
            onClick={this.handleViewClick}
            title={EntityControlMessage.View.niceToString() }>
            <span className="glyphicon glyphicon-arrow-right"/>
            </a>;
    }

    handleCreateClick: (event: React.SyntheticEvent) => {};
    renderCreateButton(props: EntityBaseProps, btn: boolean) {
        if (!props.create)
            return null;

        return <a className={classes("sf-line-button", "sf-create", btn ? "btn btn-default" : null) }
            onClick={this.handleCreateClick}
            title={EntityControlMessage.Create.niceToString() }>
            <span className="glyphicon glyphicon-plus"/>
            </a>;
    }

    handleFindClick: (event: React.SyntheticEvent) => {};
    renderFindButton(props: EntityBaseProps, btn: boolean) {
        if (!props.find)
            return null;

        return <a className={classes("sf-line-button", "sf-find", btn ? "btn btn-default" : null) }
            onClick={this.handleFindClick}
            title={EntityControlMessage.Find.niceToString() }>
            <span className="glyphicon glyphicon-search"/>
            </a>;
    }

    handleRemoveClick: (event: React.SyntheticEvent) => {};
    renderRemoveButton(props: EntityBaseProps, btn: boolean) {
        if (!props.remove)
            return null;

        return <a className={classes("sf-line-button", "sf-remove", btn ? "btn btn-default" : null) }
            onClick={this.handleFindClick}
            title={EntityControlMessage.Remove.niceToString() }>
            <span className="glyphicon glyphicon-remove"/>
            </a>;
    }
}



export interface AutocompleteProps extends React.HTMLAttributes {
    placeholder?: string

}

export class Autocomplete extends React.Component<AutocompleteProps, {}>
{
    render() {
        return <input type="textbox" className="form-control sf-entity-autocomplete" autoComplete="off"
            {...this.props}
            ></input>
    }
}

export interface EntityLineProps extends EntityBaseProps {
    autoComplete?: boolean;
}



export class EntityLine extends EntityBase<EntityLineProps, {}> {
    renderInternal() {

        var props = { ctx: this.props.ctx } as EntityLineProps;
        
        EntityBase.configureEntityButtons(props);

        var type = props.ctx.propertyRoute.member.type;        
        props.autoComplete = !type.isEmbedded && type.name != IsByAll;

        runTasks(this, props);

        props = Dic.extend(props, this.props);


        if (props.visible == false || props.hideIfNull && props.ctx.value == null)
            return null;
        
        var hasValue = !!props.ctx.value;

        var linkOrSpan = props.navigate || props.view ?
            <Link to={Navigator.navigateRoute(props.ctx.value as IEntity /*| Lite */) }>{props.ctx.value}</Link> :
            <span className={props.ctx.readOnly ? null : "form-control btn-default sf-entity-line-entity" }></span>
        return <FormGroup ctx={props.ctx} title={props.labelText}>
            <div className="SF-entity-line SF-control-container">
                <div className="input-group">
                    { hasValue ? this.renderAutoComplete(props) : this.renderLink(props) }
                    <span className="input-group-btn">
                        {!hasValue && this.renderCreateButton(props, true) }
                        {!hasValue && this.renderFindButton(props, true) }
                        {hasValue && this.renderViewButton(props, true) }
                        {hasValue && this.renderRemoveButton(props, true) }
                        </span>
                    </div>
                </div>
            </FormGroup>;
    }

    renderAutoComplete(props: EntityLineProps) {
        if (props.autoComplete)
            return <FormControlStatic ctx={props.ctx}></FormControlStatic>;

        return <Autocomplete/>;
    }


    renderLink(props: EntityLineProps) {
        if (props.ctx.readOnly)
            return <FormControlStatic ctx={props.ctx}>{props.ctx.value.toStr }</FormControlStatic>

        if (props.navigate && props.view) {
            return <a href="#" onClick={this.handleViewClick}
                className="form-control btn-default sf-entity-line-entity"
                title={JavascriptMessage.navigate.niceToString() }>
                {props.ctx.value.toStr }
                </a>;
        } else {
            return <span className="form-control btn-default sf-entity-line-entity">
                {props.ctx.value.toStr }
                </span>;
        }
    }
}


export interface EntityListBaseProps extends EntityBaseProps {
    move?: boolean;
}


export abstract class EntityListBase<T extends EntityListBaseProps> extends LineBase<T, {}>
{



}