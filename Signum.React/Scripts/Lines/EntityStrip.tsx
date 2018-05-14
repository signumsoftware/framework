import * as React from 'react'
import { classes, Dic } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext, EntityFrame } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, runTasks, } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { ModifiableEntity, Lite, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString, isEntity, isLite } from '../Signum.Entities'
import { Typeahead } from '../Components'
import { EntityListBase, EntityListBaseProps, DragConfig } from './EntityListBase'
import { AutocompleteConfig } from './AutocompleteConfig'



export interface EntityStripProps extends EntityListBaseProps {
    vertical?: boolean;
    iconStart?: boolean;
    autoComplete?: AutocompleteConfig<any> | null;
    onRenderItem?: (item: Lite<Entity> | ModifiableEntity) => React.ReactNode;
    onItemHtmlAttributes?: (item: Lite<Entity> | ModifiableEntity) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
    extraButtons?: () => (React.ReactElement<any> | null | undefined | false)[];
}

export class EntityStrip extends EntityListBase<EntityStripProps, EntityStripProps> {

    calculateDefaultState(state: EntityStripProps) {
        super.calculateDefaultState(state);
    }

    componentWillUnmount() {
        this.state.autoComplete && this.state.autoComplete.abort();
    }

    overrideProps(state: EntityStripProps, overridenProps: EntityStripProps) {
        super.overrideProps(state, overridenProps);
        if (state.autoComplete === undefined) {
            const type = state.type!;
            state.autoComplete = Navigator.getAutoComplete(type, state.findOptions);
        }
    }
    renderInternal() {

        const s = this.state;
        const readOnly = this.state.ctx.readOnly;
        return (
            <FormGroup ctx={s.ctx!}
                labelText={s.labelText}
                labelHtmlAttributes={s.labelHtmlAttributes}
                helpText={s.helpText}
                htmlAttributes={{ ...this.baseHtmlAttributes(), ...this.state.formGroupHtmlAttributes }}>
                <div className="SF-entity-strip SF-control-container">
                    <ul className={classes("sf-strip", this.props.vertical ? "sf-strip-vertical" : "sf-strip-horizontal")}>
                        {
                            mlistItemContext(s.ctx).map((mlec, i) =>
                                (<EntityStripElement key={i}
                                    ctx={mlec}
                                    iconStart={s.iconStart}
                                    autoComplete={s.autoComplete}
                                    onRenderItem={s.onRenderItem}
                                    drag={this.canMove(mlec.value) && !readOnly ? this.getDragConfig(i, this.props.vertical ? "v" : "h") : undefined}
                                    onItemHtmlAttributes={s.onItemHtmlAttributes}
                                    onRemove={this.canRemove(mlec.value) && !readOnly ? e => this.handleRemoveElementClick(e, i) : undefined}
                                    onView={this.canView(mlec.value) ? e => this.handleViewElement(e, i) : undefined}
                                />))
                        }
                        <li className={classes(s.ctx.inputGroupClass, "sf-strip-input")}>
                            {this.renderAutoComplete()}
                            <span>
                                {this.renderCreateButton(false)}
                                {this.renderFindButton(false)}
                                {this.props.extraButtons && this.props.extraButtons().map((btn, i) => {
                                    return btn && React.cloneElement(btn, { key: i });
                                })}
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
            const pr = ctx.propertyRoute.add(a => a[0]);

            const promise = this.props.onView ?
                this.props.onView(entity, pr) :
                this.defaultView(entity, pr);

            if (promise == null)
                return;

            promise.then(e => {
                if (e == undefined)
                    return;

                this.convert(e).then(m => {
                    if (is(list[index].element as Entity, e as Entity)) {
                        list[index].element = m;
                        if (e.modified)
                            this.setValue(list);
                        this.forceUpdate();
                    } else {
                        list[index] = { rowId: null, element: m };
                        this.setValue(list);
                    }

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
                itemAttrs={item => {
                    const entity = ac!.getEntityFromItem(item);
                    const key = isLite(entity) ? liteKey(entity) :
                        (entity as Entity).id ? liteKey(toLite(entity as Entity)) :
                            undefined;

                    return ({ 'data-entity-key': key }) as any;
                }}
                onSelect={this.handleOnSelect} />
        );
    }
}


export interface EntityStripElementProps {
    iconStart?: boolean;
    onRemove?: (event: React.MouseEvent<any>) => void;
    onView?: (event: React.MouseEvent<any>) => void;
    ctx: TypeContext<Lite<Entity> | ModifiableEntity>;
    autoComplete?: AutocompleteConfig<any> | null;
    onRenderItem?: (item: Lite<Entity> | ModifiableEntity) => React.ReactNode;
    onItemHtmlAttributes?: (item: Lite<Entity> | ModifiableEntity) => React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
    drag?: DragConfig;
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

        var drag = this.props.drag;
        const htmlAttributes = this.props.onItemHtmlAttributes && this.props.onItemHtmlAttributes(this.props.ctx.value);

        var val = this.props.ctx.value;

        //Till https://github.com/facebook/react/issues/8529 gets fixed
        var url = isEntity(val) ? Navigator.navigateRoute(val) :
            isLite(val) ? Navigator.navigateRoute(val) : "#";

        return (
            <li className="sf-strip-element"
                {...EntityListBase.entityHtmlAttributes(this.props.ctx.value) }>
                <div className={classes(drag && "sf-strip-dropable", drag && drag.dropClass)}
                    onDragEnter={drag && drag.onDragOver}
                    onDragOver={drag && drag.onDragOver}
                    onDrop={drag && drag.onDrop}

                >
                    {this.props.iconStart && <span style={{ marginRight: "5px" }}>{this.removeIcon()}&nbsp;{this.dragIcon()}</span>}
                    {
                        this.props.onView ?
                            <a href={url} className="sf-entitStrip-link" onClick={this.props.onView} {...htmlAttributes}>
                                {toStr}
                            </a>
                            :
                            <span className="sf-entitStrip-link" {...htmlAttributes}>
                                {toStr}
                            </span>
                    }
                    {!this.props.iconStart && <span>{this.removeIcon()}&nbsp;{this.dragIcon()}</span>}
                </div>
            </li>
        );
    }

    removeIcon() {
        return this.props.onRemove &&
                        <span>
                            <a className="sf-line-button sf-remove"
                                onClick={this.props.onRemove}
                                href="#"
                                title={EntityControlMessage.Remove.niceToString()}>
                                <span className="fa fa-remove"></span>
                            </a>
                        </span>
                    }

    dragIcon() {
        var drag = this.props.drag;
        return drag && <span className={classes("sf-line-button", "sf-move")}
                        draggable={true}
                        onDragStart={drag.onDragStart}
                        onDragEnd={drag.onDragEnd}
                        title={EntityControlMessage.Move.niceToString()}>
                        <span className="fa fa-bars" />
        </span>;
    }
}

