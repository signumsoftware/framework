import * as React from 'react'
import { Link } from 'react-router'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { Dic } from '../Globals'
import { FindOptions, QueryDescription, FilterOptionParsed, FilterRequest } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, getQueryKey } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString, isLite, isEntity } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityBase, EntityBaseProps } from './EntityBase'
import { AutocompleteConfig, FindOptionsAutocompleteConfig, LiteAutocompleteConfig } from './AutocompleteConfig'

export interface EntityLineProps extends EntityBaseProps {

    ctx: TypeContext<ModifiableEntity | Lite<Entity> | undefined | null>;

    autoComplete?: AutocompleteConfig<any> | null;
}

export interface EntityLineState extends EntityBaseProps {

    ctx: TypeContext<ModifiableEntity | Lite<Entity> | undefined | null>;

    autoComplete?: AutocompleteConfig<any> | null;

    currentItem?: { entity: ModifiableEntity | Lite<Entity>, item?: any };
}

export class EntityLine extends EntityBase<EntityLineProps, EntityLineState> {

    overrideProps(state: EntityLineState, overridenProps: EntityLineProps) {
        super.overrideProps(state, overridenProps);
        if (state.autoComplete === undefined) {
            const type = state.type!;
            state.autoComplete = Navigator.getAutoComplete(type, overridenProps.findOptions);
        }

        if (!state.currentItem) {
            if (this.state && this.state.currentItem && this.state.currentItem.entity == state.ctx.value)
                state.currentItem = this.state.currentItem;
        }
    }

    componentWillMount() {
        this.refreshItem(this.props);
    }

    componentWillReceiveProps(newProps: EntityLineProps, nextContext: any) {

        super.componentWillReceiveProps(newProps, nextContext);

        this.refreshItem(newProps);
    }

    refreshItem(props: EntityLineProps) {
        if (this.state.autoComplete) {
            var newEntity = props.ctx.value;

            if (newEntity == null) {
                if (this.state.currentItem)
                    this.changeState(s => s.currentItem = undefined);
            } else {
                if (!this.state.currentItem || this.state.currentItem.entity !== newEntity) {
                    var ci = { entity: newEntity!, item: undefined }
                    this.changeState(s => s.currentItem = ci);
                    this.state.autoComplete.getItemFromEntity(newEntity)
                        .then(item => this.changeState(s => ci.item = item))
                        .done();
                }
            }
        }
    }


    typeahead?: Typeahead;
    writeInTypeahead(query: string) {
        this.typeahead!.writeInInput(query);
    }

    handleOnSelect = (item: any, event: React.SyntheticEvent) => {

        var lite = this.state.autoComplete!.getEntityFromItem(item);

        this.convert(lite)
            .then(entity => {
                this.changeState(s => s.currentItem = { entity: entity, item: item }); //Optimization
                this.setValue(entity);
            })
            .done();

        return lite.toStr || "";
    }

    renderInternal() {

        const s = this.state;

        const hasValue = !!s.ctx.value;

        const buttons = (
            <span className="input-group-btn">
                {!hasValue && this.renderCreateButton(true) }
                {!hasValue && this.renderFindButton(true)}
                {hasValue && this.renderViewButton(true, s.ctx.value!)}
                {hasValue && this.renderRemoveButton(true, s.ctx.value!) }
            </span>
        );
        
        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} htmlProps={Dic.extend(this.baseHtmlProps(), EntityBase.entityHtmlProps(s.ctx.value!), s.formGroupHtmlProps) } labelProps={s.labelHtmlProps}>
                <div className="SF-entity-line">
                    <div className={EntityBase.hasChildrens(buttons) ? "input-group" : undefined}>
                        {hasValue ? this.renderLink() : this.renderAutoComplete()}
                        {EntityBase.hasChildrens(buttons) ? buttons : undefined}
                    </div>
                </div>
            </FormGroup>
        );
    }

    renderAutoComplete() {

        const ctx = this.state.ctx;

        var ac = this.state.autoComplete;

        if (!ac || ctx.readOnly)
            return <FormControlStatic ctx={ctx}>{ctx.value && ctx.value.toStr}</FormControlStatic>;

        return (
            <Typeahead ref={ta => this.typeahead = ta}
                inputAttrs={{ className: "form-control sf-entity-autocomplete" }}
                getItems={ac.getItems}
                renderItem={ac.renderItem}
                renderList={ac.renderList}
                liAttrs={lite => ({ 'data-entity-key': liteKey(lite) }) }
                onSelect={this.handleOnSelect}/>
        );
    }

    renderLink() {

        const s = this.state;

        var value = s.ctx.value!;

        const str = this.state.currentItem && this.state.currentItem.item && this.state.autoComplete ?
            this.state.autoComplete.renderItem(this.state.currentItem.item) :
            getToString(value);

        if (s.ctx.readOnly)
            return <FormControlStatic ctx={s.ctx}>{ str }</FormControlStatic>

        if (s.navigate && s.view) {
            return (
                <a href="#" onClick={this.handleViewClick}
                    className="form-control btn-default sf-entity-line-entity"
                    title={JavascriptMessage.navigate.niceToString() }>
                    {str}
                </a>
            );
        } else {
            return (
                <span className="form-control btn-default sf-entity-line-entity">
                    {str }
                </span>
            );
        }
    }
}





