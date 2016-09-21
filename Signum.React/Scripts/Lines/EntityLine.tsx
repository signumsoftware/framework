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
import { EntityBase, EntityBaseProps} from './EntityBase'

export interface EntityLineProps extends EntityBaseProps {

    ctx: TypeContext<ModifiableEntity | Lite<Entity> | undefined | null>;

    autoComplete?: AutocompleteConfig<any> | null;
}

export interface EntityLineState extends EntityBaseProps {

    ctx: TypeContext<ModifiableEntity | Lite<Entity> | undefined | null>;

    autoComplete?: AutocompleteConfig<any> | null;

    currentItem?: { entity: ModifiableEntity | Lite<Entity>, item: any };
}

export class EntityLine extends EntityBase<EntityLineProps, EntityLineState> {

    overrideProps(state: EntityLineState, overridenProps: EntityLineProps) {
        super.overrideProps(state, overridenProps);
        if (state.autoComplete === undefined) {
            const type = state.type!;
            state.autoComplete = type.isEmbedded || type.name == IsByAll ? null :
                overridenProps.findOptions ? new FindOptionsAutocompleteConfig(overridenProps.findOptions, 5, false) :
                    new LiteAutocompleteConfig((subStr: string) => Finder.API.findLiteLike({
                        types: type.name,
                        subString: subStr,
                        count: 5
                    }), false);
        }
    }


    componentWillReceiveProps(newProps: EntityLineProps, nextContext: any) {

        super.componentWillReceiveProps(newProps, nextContext);

        if (this.state.autoComplete) {

            var newEntity = newProps.ctx.value; 

            if (newEntity == null) {
                if (this.state.currentItem)
                    this.changeState(s => s.currentItem = undefined);
            } else {
                if (!this.state.currentItem || this.state.currentItem.entity !== newEntity) {
                    this.changeState(s => s.currentItem = undefined);
                    this.state.autoComplete.getItemFromEntity(newEntity)
                        .then(item => this.changeState(s => s.currentItem = { entity: newEntity!, item }))
                        .done();
                }
            }
        }
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
                {!hasValue && this.renderFindButton(true) }
                {hasValue && this.renderViewButton(true) }
                {hasValue && this.renderRemoveButton(true) }
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
            <Typeahead
                inputAttrs={{ className: "form-control sf-entity-autocomplete" }}
                getItems={ac.getItems}
                renderItem={ac.renderItem}
                liAttrs={lite => ({ 'data-entity-key': liteKey(lite) }) }
                onSelect={this.handleOnSelect}/>
        );
    }

    renderLink() {

        const s = this.state;

        var value = s.ctx.value!;

        const str = this.state.currentItem && this.state.autoComplete ?
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

export interface AutocompleteConfig<T> {
    getItems: (subStr: string) => Promise<T[]>;
    renderItem: (item: T, subStr?: string) => React.ReactNode
    getEntityFromItem: (item: T) => Lite<Entity> | ModifiableEntity;
    getItemFromEntity: (entity: Lite<Entity> | ModifiableEntity) => Promise<T>;
}

export class LiteAutocompleteConfig implements AutocompleteConfig<Lite<Entity>>{

    constructor(
        public getItems: (subStr: string) => Promise<Lite<Entity>[]>,
        public withCustomToString: boolean) {
    }

    renderItem(item: Lite<Entity>, subStr: string) {
        return Typeahead.highlightedText(item.toStr || "", subStr)
    }

    getEntityFromItem(item: Lite<Entity>) {
        return item;
    }

    getItemFromEntity(entity: Lite<Entity> | ModifiableEntity): Promise<Lite<Entity>> {

        var lite = this.convertToLite(entity);;

        if (!this.withCustomToString)
            return Promise.resolve(lite);

        if (lite.id == undefined)
            return Promise.resolve(lite);

        return this.getItems(lite.id!.toString()).then(lites => {

            const result = lites.filter(a => a.id == lite.id).firstOrNull();

            if (!result)
                throw new Error("Impossible to getInitialItem with the current implementation of getItems");

            return result;
        });
    }

    convertToLite(entity: Lite<Entity> | ModifiableEntity) {
        
        if (isLite(entity))
            return entity;

        if (isEntity(entity))
            return toLite(entity);

        throw new Error("Impossible to convert to Lite");
    }
}

export class FindOptionsAutocompleteConfig implements AutocompleteConfig<Lite<Entity>>{

    constructor(
        public findOptions: FindOptions,
        public count: number,
        public withCustomToString: boolean) {
    }

    parsedFilters?: FilterOptionParsed[];

    getParsedFilters(): Promise<FilterOptionParsed[]> {
        if (this.parsedFilters)
            return Promise.resolve(this.parsedFilters);

        return Finder.getQueryDescription(this.findOptions.queryName)
            .then(qd => Finder.parseFilterOptions(this.findOptions.filterOptions || [], qd))
            .then(filters => this.parsedFilters = filters);
    }

    getItems = (subStr: string): Promise<Lite<Entity>[]> => {
        return this.getParsedFilters()
            .then(filters => Finder.API.findLiteLikeWithFilters({
                queryKey: getQueryKey(this.findOptions.queryName),
                filters: filters.map(f => ({ token: f.token!.fullKey, operation: f.operation, value: f.value }) as FilterRequest),
                count: this.count,
                subString: subStr
            }));
    }

    renderItem(item: Lite<Entity>, subStr: string) {
        return Typeahead.highlightedText(item.toStr || "", subStr)
    }

    getEntityFromItem(item: Lite<Entity>) {
        return item;
    }

    getItemFromEntity(entity: Lite<Entity> | ModifiableEntity): Promise<Lite<Entity>> {

        var lite = this.convertToLite(entity);;

        if (!this.withCustomToString)
            return Promise.resolve(lite);

        if (lite.id == undefined)
            return Promise.resolve(lite);

        return Finder.API.findLiteLikeWithFilters({
            queryKey: getQueryKey(this.findOptions.queryName),
            filters:  [{ token: "Entity.Id", operation: "EqualTo", value: lite.id }],
            count: 1,
            subString: ""
        }).then(lites => {

            const result = lites.filter(a => a.id == lite.id).firstOrNull();

            if (!result)
                throw new Error("Impossible to getInitialItem with the current implementation of getItems");

            return result;
        });
    }

    convertToLite(entity: Lite<Entity> | ModifiableEntity) {

        if (isLite(entity))
            return entity;

        if (isEntity(entity))
            return toLite(entity);

        throw new Error("Impossible to convert to Lite");
    }
}




