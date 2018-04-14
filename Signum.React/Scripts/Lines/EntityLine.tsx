import * as React from 'react'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { Dic, classes } from '../Globals'
import { FindOptions, QueryDescription, FilterOptionParsed, FilterRequest } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, getQueryKey } from '../Reflection'
import { LineBase, LineBaseProps, runTasks } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString, isLite, isEntity, isModifiableEntity } from '../Signum.Entities'
import { Typeahead } from '../Components'
import { EntityBase, EntityBaseProps } from './EntityBase'
import { AutocompleteConfig } from './AutocompleteConfig'

export interface EntityLineProps extends EntityBaseProps {

    ctx: TypeContext<ModifiableEntity | Lite<Entity> | undefined | null>;
    autoComplete?: AutocompleteConfig<any> | null;
    renderItem?: React.ReactNode; 
    itemHtmlAttributes?: React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
    extraButtons?: () => (React.ReactElement<any> | null | undefined | false)[];
}

export interface EntityLineState extends EntityLineProps {
    currentItem?: { entity: ModifiableEntity | Lite<Entity>, item?: any };
}

export class EntityLine extends EntityBase<EntityLineProps, EntityLineState> {

    overrideProps(state: EntityLineState, overridenProps: EntityLineProps) {
        super.overrideProps(state, overridenProps);
        if (state.autoComplete === undefined) {
            const type = state.type!;
            state.autoComplete = Navigator.getAutoComplete(type, state.findOptions);
        }

        if (!state.currentItem) {
            if (this.state && this.state.currentItem && this.state.currentItem.entity == state.ctx.value)
                state.currentItem = this.state.currentItem;
        }
    }

    componentWillUnmount() {
        this.state.autoComplete && this.state.autoComplete.abort();
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
                    this.setState({ currentItem: undefined });
            } else {
                if (!this.state.currentItem || this.state.currentItem.entity !== newEntity) {
                    var ci = { entity: newEntity!, item: undefined }
                    this.setState({ currentItem: ci });
                    this.state.autoComplete.getItemFromEntity(newEntity)
                        .then(item => {
                            ci.item = item;
                            this.forceUpdate()
                        })
                        .done();
                }
            }
        }
    }


    typeahead?: Typeahead | null;
    writeInTypeahead(query: string) {
        this.typeahead!.writeInInput(query);
    }

    handleOnSelect = (item: any, event: React.SyntheticEvent<any>) => {

        var entity = this.state.autoComplete!.getEntityFromItem(item);

        this.convert(entity)
            .then(entity => {
                this.setState({ currentItem: { entity: entity, item: item } }); //Optimization
                this.setValue(entity);
            })
            .done();

        return entity.toStr || "";
    }

    setValue(val: any) {
        super.setValue(val);
        this.refreshItem(this.props);
    }

    renderInternal() {

        const s = this.state;

        const hasValue = !!s.ctx.value;

        const buttons = (
            <span className="input-group-append">
                {!hasValue && this.renderCreateButton(true)}
                {!hasValue && this.renderFindButton(true)}
                {hasValue && this.renderViewButton(true, s.ctx.value!)}
                {hasValue && this.renderRemoveButton(true, s.ctx.value!)}
                {this.props.extraButtons && this.props.extraButtons().map((btn, i) => {
                    return btn && React.cloneElement(btn, { key: i });
                })}
            </span>
        );

        var linkOrAutocomplete = hasValue ? this.renderLink() : this.renderAutoComplete();

        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText}
                htmlAttributes={{ ...this.baseHtmlAttributes(), ...EntityBase.entityHtmlAttributes(s.ctx.value!), ...s.formGroupHtmlAttributes }}
                labelHtmlAttributes={s.labelHtmlAttributes}>
                <div className="SF-entity-line">
                    {
                        !EntityBase.hasChildrens(buttons) ?
                            <div style={{ position: "relative" }}>{linkOrAutocomplete}</div> :
                            <div className={s.ctx.inputGroupClass}>
                                {linkOrAutocomplete}
                                {buttons}
                            </div>
                    }
                </div>
            </FormGroup>
        );
    }

    renderAutoComplete() {

        const ctx = this.state.ctx;

        var ac = this.state.autoComplete;

        if (ac == null || ctx.readOnly)
            return <FormControlReadonly ctx={ctx}>{ctx.value && ctx.value.toStr}</FormControlReadonly>;

        return (
            <Typeahead ref={ta => this.typeahead = ta}
                inputAttrs={{ className: classes(ctx.formControlClass, "sf-entity-autocomplete") }}
                getItems={query => ac!.getItems(query)}
                getItemsDelay={ac.getItemsDelay}
                minLength={ac.minLength}
                renderItem={(item, query) => ac!.renderItem(item, query)}
                renderList={ac!.renderList && (ta => ac!.renderList!(ta))}
                itemAttrs={item => {
                    const entity = ac!.getEntityFromItem(item);
                    const key = isLite(entity) ? liteKey(entity) :
                        (entity as Entity).id ? liteKey(toLite(entity as Entity)) :
                            undefined;

                    return ({ 'data-entity-key': key }) as React.HTMLAttributes<HTMLButtonElement>;
                }}
                onSelect={this.handleOnSelect}/>
        );
    }

    renderLink() {

        const s = this.state;

        var value = s.ctx.value!;

        const str =
            s.renderItem ? s.renderItem :
            s.currentItem && s.currentItem.item && s.autoComplete ? s.autoComplete.renderItem(s.currentItem.item) :
                    getToString(value);

        if (s.ctx.readOnly)
            return <FormControlReadonly ctx={s.ctx}>{ str }</FormControlReadonly>

        if (s.navigate && s.view) {
            return (
                <a href="#" onClick={this.handleViewClick}
                    className={classes(s.ctx.formControlClass, "sf-entity-line-entity")}
                    title={JavascriptMessage.navigate.niceToString()} {...s.itemHtmlAttributes}>
                    {str}
                </a>
            );
        } else {
            return (
                <span className={classes(s.ctx.formControlClass, "sf-entity-line-entity")} { ...s.itemHtmlAttributes } >
                    {str }
                </span>
            );
        }
    }
}





