import * as React from 'react'
import { classes, Dic } from '../Globals'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll } from '../Reflection'
import { LineBase, LineBaseProps, runTasks } from './LineBase'
import { FormGroup } from './FormGroup'
import { FormControlReadonly } from './FormControlReadonly'
import { EntityListBase, EntityListBaseProps} from './EntityListBase'


export interface EntityListProps extends EntityListBaseProps {
    size?: number;
}


export abstract class EntityList extends EntityListBase<EntityListProps, EntityListProps>
{
    static defaultProps: EntityListProps = {
        size: 5,
        ctx: undefined as any,
    };

    moveUp(index: number) {
        super.moveUp(index);
        this.forceUpdate();
    }

    moveDown(index: number) {
        super.moveDown(index);
        this.forceUpdate();
    }

    handleOnSelect = (e: React.FormEvent<HTMLSelectElement>) => {
        this.forceUpdate();
    }


    selectElement?: HTMLSelectElement | null;
    handleSelectLoad = (sel: HTMLSelectElement | null) => {
        let refresh = this.selectElement == undefined && sel;

        this.selectElement = sel;

        if (refresh)
            this.forceUpdate();
    }

    getSelectedIndex(): number | undefined {
        if (this.selectElement == null || this.selectElement.selectedIndex == -1)
            return undefined;


        var list = this.state.ctx.value;
        if (list.length <= this.selectElement.selectedIndex)
            return undefined;

        return this.selectElement.selectedIndex;
    }

    renderInternal() {

        const s = this.state;
        const list = this.state.ctx.value!;

        const selectedIndex = this.getSelectedIndex();

        return (
            <FormGroup ctx={s.ctx} labelText={s.labelText}
                htmlAttributes={{ ...this.baseHtmlAttributes(), ...this.state.formGroupHtmlAttributes }}
                labelHtmlAttributes={s.labelHtmlAttributes}>
                <div className="SF-entity-line">
                    <div className={s.ctx.inputGroupClass}>
                        <select className={s.ctx.formControlClass} size={this.props.size} onChange={this.handleOnSelect} ref={this.handleSelectLoad}>
                            {list.map((e, i) => <option  key={i} title={this.getTitle(e.element)} {...EntityListBase.entityHtmlAttributes(e.element) }>{getToString(e.element)}</option>)}
                        </select>
                        <span className="input-group-append input-group-vertical">
                            {this.renderCreateButton(true)}
                            {this.renderFindButton(true)}
                            {selectedIndex != undefined && this.renderViewButton(true, list[selectedIndex].element)}
                            {selectedIndex != undefined && this.renderRemoveButton(true, list[selectedIndex].element)}
                            {selectedIndex != undefined && this.state.move && selectedIndex != null && selectedIndex > 0 && this.renderMoveUp(true, selectedIndex!)}
                            {selectedIndex != undefined && this.state.move && selectedIndex != null && selectedIndex < list.length - 1 && this.renderMoveDown(true, selectedIndex!)}
                        </span>
                    </div>
                </div>
            </FormGroup>
        );
    }

    handleRemoveClick = (event: React.SyntheticEvent<any>) => {

        event.preventDefault();

        const s = this.state;

        var list = s.ctx.value!;

        var selectedIndex = this.getSelectedIndex()!;

        (s.onRemove ? s.onRemove(list[selectedIndex].element) : Promise.resolve(true))
            .then(result => {
                if (result == false)
                    return;

                list.removeAt(selectedIndex!);

                this.setValue(list);
            })
            .done();
    };

    handleViewClick = (event: React.MouseEvent<any>) => {

        event.preventDefault();

        const ctx = this.state.ctx;
        const selectedIndex = this.getSelectedIndex()!;
        const list = ctx.value!;
        const entity = list[selectedIndex].element;

        const pr = ctx.propertyRoute.addLambda(a => a[0]);

        const openWindow = (event.button == 1 || event.ctrlKey) && !this.state.type!.isEmbedded;

        const promise = this.state.onView ?
            this.state.onView(entity, pr) :
            this.defaultView(entity, pr);

        if (promise == null)
            return;

        promise.then(e => {
            if (e == undefined)
                return;

            this.convert(e).then(m => {
                if (is(list[selectedIndex].element as Entity, e as Entity)) {
                    list[selectedIndex].element = m;
                    if (e.modified)
                        this.setValue(list);
                }
                else {
                    list[selectedIndex] = { rowId: null, element: m };
                    this.setValue(list);
                }
            }).done();
        }).done();
    }

    getTitle(e: Lite<Entity> | ModifiableEntity) {

        const pr = this.props.ctx.propertyRoute;

        const type = pr && pr.member && pr.member.typeNiceName || (e as Lite<Entity>).EntityType || (e as ModifiableEntity).Type;

        const id = (e as Lite<Entity>).id || (e as Entity).id;

        return type + (id ? " " + id : "");
    }
}