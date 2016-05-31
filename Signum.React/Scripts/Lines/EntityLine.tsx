import * as React from 'react'
import { Link } from 'react-router'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityBase, EntityBaseProps} from './EntityBase'



export interface EntityLineProps extends EntityBaseProps {

    ctx: TypeContext<ModifiableEntity | Lite<Entity>>;

    autoComplete?: boolean;
    autoCompleteGetItems?: (query: string) => Promise<Lite<Entity>[]>;
    autoCompleteRenderItem?: (lite: Lite<Entity>, query: string) => React.ReactNode;
}

export interface EntityLineState extends EntityBaseProps {

    ctx: TypeContext<ModifiableEntity | Lite<Entity>>;

    autoComplete?: boolean;
}

export class EntityLine extends EntityBase<EntityLineProps, EntityLineState> {

    calculateDefaultState(state: EntityLineProps) {
        super.calculateDefaultState(state);
        state.autoComplete = !state.type.isEmbedded && state.type.name != IsByAll;
    }
    
    defaultAutoCompleteGetItems = (query: string) => Finder.API.findLiteLike({
        types: this.state.type.name,
        subString: query,
        count: 5
    });

    defaultAutCompleteRenderItem = (lite, query) => Typeahead.highlightedText(lite.toStr, query);
    

    handleOnSelect = (lite: Lite<Entity>, event: React.SyntheticEvent) => {
        this.convert(lite)
            .then(entity => this.setValue(entity))
            .done();
        return lite.toStr;
    }

    renderInternal() {

        const s = this.state;

        const hasValue = !!s.ctx.value;

        var buttons = (
            <span className="input-group-btn">
                {!hasValue && this.renderCreateButton(true) }
                {!hasValue && this.renderFindButton(true) }
                {hasValue && this.renderViewButton(true) }
                {hasValue && this.renderRemoveButton(true) }
            </span>
        );

        if (!buttons.props.children.some(a => a))
            buttons = null;

        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={this.withPropertyPath(s.formGroupHtmlProps)} labelProps={s.labelHtmlProps}>
                <div className="SF-entity-line">
                    <div className={buttons ? "input-group" : null}>
                        { hasValue ? this.renderLink() : this.renderAutoComplete() }
                        {buttons}
                    </div>
                </div>
            </FormGroup>
        );
    }

    renderAutoComplete() {


        var ctx = this.state.ctx;

        if (!this.state.autoComplete || ctx.readOnly)
            return <FormControlStatic ctx={ctx}>{ctx.value && ctx.value.toStr}</FormControlStatic>;

        return (
            <Typeahead
                inputAttrs={{ className: "form-control sf-entity-autocomplete" }}
                getItems={this.props.autoCompleteGetItems || this.defaultAutoCompleteGetItems}
                renderItem={this.props.autoCompleteRenderItem || this.defaultAutCompleteRenderItem}
                onSelect={this.handleOnSelect}/>
        );
    }

    renderLink() {

        const s = this.state;

        if (s.ctx.readOnly)
            return <FormControlStatic ctx={s.ctx}>{getToString(s.ctx.value) }</FormControlStatic>

        if (s.navigate && s.view) {
            return (
                <a href="#" onClick={this.handleViewClick}
                    className="form-control btn-default sf-entity-line-entity"
                    title={JavascriptMessage.navigate.niceToString() }>
                    {  s.ctx.value.toStr }
                </a>
            );
        } else {
            return (
                <span className="form-control btn-default sf-entity-line-entity">
                    {s.ctx.value.toStr }
                </span>
            );
        }
    }
}

