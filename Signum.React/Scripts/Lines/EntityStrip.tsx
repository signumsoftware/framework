import * as React from 'react'
import { classes, Dic } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString, isLite } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityListBase, EntityListBaseProps } from './EntityListBase'
import { AutocompleteConfig } from './AutocompleteConfig'



export interface EntityStripProps extends EntityListBaseProps {
    vertical?: boolean;
    autoComplete?: AutocompleteConfig<any> | null;
    onRenderItem?: (item: Lite<Entity> | ModifiableEntity) => React.ReactNode; 
    onItemHtmlAttributes?: (item: Lite<Entity> | ModifiableEntity) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
}

export class EntityStrip extends EntityListBase<EntityStripProps, EntityStripProps> {

    calculateDefaultState(state: EntityStripProps) {
        super.calculateDefaultState(state);   
    }


    overrideProps(state: EntityStripProps, overridenProps: EntityStripProps) {
        super.overrideProps(state, overridenProps);
        if (state.autoComplete === undefined) {
            const type = state.type!;
            state.autoComplete = Navigator.getAutoComplete(type, overridenProps.findOptions);
        }
    }
    renderInternal() {

        const s = this.state;
        const readOnly = this.state.ctx.readOnly;
        return (
            <FormGroup ctx={s.ctx!}
                labelText={s.labelText}
                labelHtmlAttributes={s.labelHtmlAttributes}
                helpBlock={s.helpBlock}
                htmlAttributes={{ ...this.baseHtmlAttributes(), ...this.state.formGroupHtmlAttributes }}>
                <div className="SF-entity-strip SF-control-container">
                    <ul className={classes("sf-strip", this.props.vertical ? "sf-strip-vertical" : "sf-strip-horizontal") }>
                        {
                            mlistItemContext(s.ctx).map((mlec, i) =>
                                (<EntityStripElement key={i}
                                    ctx={mlec}
                                    autoComplete={s.autoComplete}
                                    onRenderItem={s.onRenderItem}
                                    onItemHtmlAttributes={s.onItemHtmlAttributes}
                                    onRemove={this.canRemove(mlec.value) && !readOnly ? e => this.handleRemoveElementClick(e, i) : undefined}
                                    onView={this.canView(mlec.value) ? e => this.handleViewElement(e, i) : undefined}
                                    />))
                        }
                        <li className="sf-strip-input input-group">
                            {this.renderAutoComplete() }
                            <span>
                                { this.renderCreateButton(false) }
                                { this.renderFindButton(false) }
                            </span>
                        </li> 
                    </ul>
                </div>
            </FormGroup>
        );

    }
    
    handleOnSelect = (lite: Lite<Entity>, event: React.SyntheticEvent<any>) => {
        this.convert(lite)
            .then(e => this.addElement(e))
            .done();
        return "";

    }

    handleViewElement = (event: React.MouseEvent<any>, index: number) => {

        event.preventDefault();

        const ctx = this.state.ctx;
        const list = ctx.value!;
        const mle = list[index];
        const entity = mle.element;

        const openWindow = (event.button == 1 || event.ctrlKey) && !this.state.type!.isEmbedded;
        if (openWindow) {
            event.preventDefault();
            const route = Navigator.navigateRoute(entity as Lite<Entity> /*or Entity*/);
            window.open(route);
        }
        else {
            const promise = this.props.onView ?
                this.props.onView(entity, ctx.propertyRoute) :
                this.defaultView(entity, ctx.propertyRoute);

            if (promise == null)
                return;

            promise.then(e => {
                if (e == undefined)
                    return;

                this.convert(e).then(m => {
                    if (is(list[index].element as Entity, e as Entity))
                        list[index].element = m;
                    else
                        list[index] = { rowId: null, element: m };

                    this.setValue(list);
                }).done();
            }).done();
        }
    }


    renderAutoComplete() {

        var ac = this.state.autoComplete;

        if (!ac || this.state.ctx!.readOnly)
            return undefined;

        return (
            <Typeahead
                inputAttrs={{ className: "sf-entity-autocomplete" }}
                getItems={q => ac!.getItems(q)}
                getItemsDelay={ac.getItemsDelay}
                renderItem={(e, str) => ac!.renderItem(e, str)}
                liAttrs={item => {
                    const entity = ac!.getEntityFromItem(item);
                    const key = isLite(entity) ? liteKey(entity) :
                        (entity as Entity).id ? liteKey(toLite(entity as Entity)) :
                            undefined;

                    return ({ 'data-entity-key': key });
                }}
                onSelect={this.handleOnSelect}/>
        );
    }
}


export interface EntityStripElementProps {
    onRemove?: (event: React.MouseEvent<any>) => void;
    onView?: (event: React.MouseEvent<any>) => void;
    ctx: TypeContext<Lite<Entity> | ModifiableEntity>;
    autoComplete?: AutocompleteConfig<any> | null;
    onRenderItem?: (item: Lite<Entity> | ModifiableEntity) => React.ReactNode;
    onItemHtmlAttributes?: (item: Lite<Entity> | ModifiableEntity) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
}

export interface EntityStripElementState {
    currentItem?: { entity: ModifiableEntity | Lite<Entity>, item?: any };
}

export class EntityStripElement extends React.Component<EntityStripElementProps, EntityStripElementState>
{
    constructor(props: EntityStripElementProps) {
        super(props);
        this.state = {};
    }

    componentWillMount() {
        this.refreshItem(this.props);
    }

    componentWillReceiveProps(newProps: EntityStripElementProps, nextContext: any) {
        this.refreshItem(newProps);
    }

    refreshItem(props: EntityStripElementProps) {
        if (this.props.autoComplete) {
            var newEntity = props.ctx.value;
            if (!this.state.currentItem || this.state.currentItem.entity !== newEntity) {
                var ci = { entity: newEntity!, item: undefined }
                this.setState({ currentItem: ci });
                this.props.autoComplete.getItemFromEntity(newEntity)
                    .then(item => {
                        ci.item = item;
                        this.forceUpdate();
                    })
                    .done();
            }
        }
    }
    
    render() {

        const toStr =
            this.props.onRenderItem ? this.props.onRenderItem(this.props.ctx.value) :
                this.state.currentItem && this.state.currentItem.item ? this.props.autoComplete!.renderItem(this.state.currentItem.item) :
                    getToString(this.props.ctx.value);

        const htmlAttributes = this.props.onItemHtmlAttributes && this.props.onItemHtmlAttributes(this.props.ctx.value);

        return (
            <li className="sf-strip-element input-group" {...EntityListBase.entityHtmlAttributes(this.props.ctx.value) }>
                {
                    this.props.onView ?
                        <a className="sf-entitStrip-link" href="" onClick={this.props.onView} {...htmlAttributes}>
                            {toStr}
                        </a>
                        :
                        <span className="sf-entitStrip-link" {...htmlAttributes}>
                            {toStr}
                        </span>
                }
               
                {this.props.onRemove &&
                    <span>
                        <a className="sf-line-button sf-remove" 
                            onClick={this.props.onRemove}
                            title={EntityControlMessage.Remove.niceToString() }>
                            <span className="glyphicon glyphicon-remove"></span></a>
                    </span>
                }
            </li>
        );
    }
}

