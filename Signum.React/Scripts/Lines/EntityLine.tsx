import * as React from 'react'
import { Link } from 'react-router'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { Dic } from '../Globals'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityBase, EntityBaseProps} from './EntityBase'



export interface EntityLineProps extends EntityBaseProps {

    ctx: TypeContext<ModifiableEntity | Lite<Entity> | undefined | null>;

    autoComplete?: boolean;
    autoCompleteGetItems?: (query: string) => Promise<Lite<Entity>[]>;
    autoCompleteRenderItem?: (lite: Lite<Entity>, query: string) => React.ReactNode;
}

export interface EntityLineState extends EntityBaseProps {

    ctx: TypeContext<ModifiableEntity | Lite<Entity> | undefined | null>;

    autoComplete?: boolean;
}

export class EntityLine extends EntityBase<EntityLineProps, EntityLineState> {

    calculateDefaultState(state: EntityLineProps) {
        super.calculateDefaultState(state);
        state.autoComplete = !state.type!.isEmbedded && state.type!.name != IsByAll;
    }
    
    defaultAutoCompleteGetItems = (query: string) => Finder.API.findLiteLike({
        types: this.state.type!.name,
        subString: query,
        count: 5
    });

    defaultAutCompleteRenderItem = (lite: Lite<Entity>, query: string) => Typeahead.highlightedText(lite.toStr || "" , query);
    

    handleOnSelect = (lite: Lite<Entity>, event: React.SyntheticEvent) => {
        this.convert(lite)
            .then(entity => this.setValue(entity))
            .done();

        return lite.toStr || "";
    }

    renderInternal() {

        const s = this.state;

        const hasValue = !!s.ctx.value;

        const buttons = (
            <span className="input-group-btn">
                {!hasValue && this.renderCreateButton(true) }
                {!hasValue && this.renderFindButton(true) }
                {hasValue && this.renderViewButton(true) }
                {hasValue && this.renderRemoveButton(true) }
            </span>
        );
        
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(this.baseHtmlProps(), EntityBase.entityHtmlProps(s.ctx.value!), s.formGroupHtmlProps) } labelProps={s.labelHtmlProps}>
                <div className="SF-entity-line">
                    <div className={buttons ? "input-group" : undefined}>
                        {hasValue ? this.renderLink() : this.renderAutoComplete()}
                        {React.Children.count(buttons) ? buttons : undefined}
                    </div>
                </div>
            </FormGroup>
        );
    }

    renderAutoComplete() {

        const ctx = this.state.ctx;

        if (!this.state.autoComplete || ctx.readOnly)
            return <FormControlStatic ctx={ctx}>{ctx.value && ctx.value.toStr}</FormControlStatic>;

        return (
            <Typeahead
                inputAttrs={{ className: "form-control sf-entity-autocomplete" }}
                getItems={this.props.autoCompleteGetItems || this.defaultAutoCompleteGetItems}
                renderItem={this.props.autoCompleteRenderItem || this.defaultAutCompleteRenderItem}
                liAttrs={lite => ({ 'data-entity-key': liteKey(lite) }) }
                onSelect={this.handleOnSelect}/>
        );
    }

    renderLink() {

        const s = this.state;

        var value = s.ctx.value!;

        if (s.ctx.readOnly)
            return <FormControlStatic ctx={s.ctx}>{getToString(value) }</FormControlStatic>

        if (s.navigate && s.view) {
            return (
                <a href="#" onClick={this.handleViewClick}
                    className="form-control btn-default sf-entity-line-entity"
                    title={JavascriptMessage.navigate.niceToString() }>
                    { value.toStr }
                </a>
            );
        } else {
            return (
                <span className="form-control btn-default sf-entity-line-entity">
                    { value.toStr }
                </span>
            );
        }
    }
}

