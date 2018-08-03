import * as React from 'react'
import { Dic, classes } from '../Globals'
import { ModifiableEntity, Lite, Entity, MListElement, MList, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, newMListElement, isLite } from '../Signum.Entities'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, getTypeName } from '../Reflection'
import { LineBase, LineBaseProps, runTasks } from '../Lines/LineBase'
import { EntityBase, EntityBaseProps } from './EntityBase'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';


export interface EntityListBaseProps extends EntityBaseProps {
    move?: boolean | ((item: ModifiableEntity | Lite<Entity>) => boolean);
    onFindMany?: () => Promise<(ModifiableEntity | Lite<Entity>)[] | undefined> | undefined;

    ctx: TypeContext<MList<any /*Lite<Entity> | ModifiableEntity*/>>;
}

export interface EntityListBaseState extends EntityListBaseProps{
    dragIndex?: number,
    dropBorderIndex?: number,
}

export abstract class EntityListBase<T extends EntityListBaseProps, S extends EntityListBaseState> extends EntityBase<T, S>
{
    calculateDefaultState(state: S) {

        if (state.onFind)
            throw new Error(`'onFind' property is not applicable to '${this}'. Use 'onFindMany' instead`);

        if(state.ctx.value == undefined)
            state.ctx.value = [];

        super.calculateDefaultState(state);
    }

    setValue(list: MList<Lite<Entity> | ModifiableEntity>) {
        super.setValue(list as any);
    }

    moveUp(index: number) {
        const list = this.props.ctx.value!;
        list.moveUp(index);
        this.setValue(list);
    }
    renderMoveUp(btn: boolean, index: number) {
        if (!this.canMove(this.state.ctx.value[index].element) || this.state.ctx.readOnly)
            return undefined;

        return (
            <a href="#" className={classes("sf-line-button", "sf-move", btn ? "btn btn-light" : undefined)}
                onClick={e => { e.preventDefault(); this.moveUp(index); }}
                title={EntityControlMessage.MoveUp.niceToString()}>
                <FontAwesomeIcon icon="chevron-up" />
            </a>
        );
    }

    moveDown(index: number) {
        const list = this.props.ctx.value!;
        list.moveDown(index);
        this.setValue(list);
    }

    renderMoveDown(btn: boolean, index: number) {
        if (!this.canMove(this.state.ctx.value[index].element) || this.state.ctx.readOnly)
            return undefined;

        return (
            <a href="#" className={classes("sf-line-button", "sf-move", btn ? "btn btn-light" : undefined)}
                onClick={e => { e.preventDefault(); this.moveDown(index); }}
                title={EntityControlMessage.MoveUp.niceToString() }>
                <FontAwesomeIcon icon="chevron-down"/>
            </a>);
    }

    handleCreateClick = (event: React.SyntheticEvent<any>) => {

        event.preventDefault();
        event.stopPropagation();

        const promise = this.props.onCreate ?
            this.props.onCreate() : this.defaultCreate();

        if (promise == null)
            return;

        promise
            .then<ModifiableEntity | Lite<Entity> | undefined>(e => {

                if (e == undefined)
                    return undefined;

                if (!this.state.viewOnCreate)
                    return Promise.resolve(e);

                const pr = this.state.ctx.propertyRoute.addLambda(a => a[0]);

                return this.state.onView ?
                    this.state.onView(e, pr) :
                    this.defaultView(e, pr);

            }).then(e => {

                if (!e)
                    return;

                this.convert(e)
                    .then(m => this.addElement(m))
                    .done();
            }).done();
    };

    defaultFindMany(): Promise<(ModifiableEntity | Lite<Entity>)[] | undefined> {

        if (this.state.findOptions) {
            return Finder.findMany(this.state.findOptions);
        }

        return this.chooseType(ti => Finder.isFindable(ti, false))
            .then<(ModifiableEntity | Lite<Entity>)[] | undefined>(qn => qn == undefined ? undefined : Finder.findMany({ queryName: qn } as FindOptions));
    }

    addElement(entityOrLite: Lite<Entity> | ModifiableEntity) {

        if (isLite(entityOrLite) != (this.state.type!.isLite || false))
            throw new Error("entityOrLite should be already converted");

        const list = this.props.ctx.value!;
        list.push(newMListElement(entityOrLite));
        this.setValue(list);
    }


    handleFindClick = (event: React.SyntheticEvent<any>) => {

        event.preventDefault();

        const promise = this.state.onFindMany ? this.state.onFindMany() : this.defaultFindMany();

        if (promise == null)
            return;

        promise.then(lites => {
            if (!lites)
                return;

            Promise.all(lites.map(a => this.convert(a))).then(entites => {
                entites.forEach(e => this.addElement(e));
            }).done();
        }).done();
    };

    handleRemoveElementClick = (event: React.SyntheticEvent<any>, index: number) => {

        event.preventDefault();

        const list = this.props.ctx.value!;
        const mle = list[index];

        (this.props.onRemove ? this.props.onRemove(mle.element) : Promise.resolve(true))
            .then(result => {
                if (result == false)
                    return;

                this.removeElement(mle)
            }).done();
    };

    removeElement(mle: MListElement<ModifiableEntity | Lite<Entity>>)
    {
        const list = this.props.ctx.value!;
        list.remove(mle);
        this.setValue(list);
    }

    canMove(item: ModifiableEntity | Lite<Entity>): boolean | undefined {

        const move = this.state.move;

        if (move == undefined)
            return undefined;

        if (typeof move === "function")
            return move(item);

        return move;
    }


    handleDragStart = (de: React.DragEvent<any>, index: number) => {
        de.dataTransfer.setData('text', "start"); //cannot be empty string
        de.dataTransfer.effectAllowed = "move";
        this.state.dragIndex = index;
        this.forceUpdate();
    }

    handleDragEnd = (de: React.DragEvent<any>) => {
        this.state.dragIndex = undefined;
        this.state.dropBorderIndex = undefined;
        this.forceUpdate();
    }

    getOffsetHorizontal(dragEvent: DragEvent, rect: ClientRect) {

        const margin = Math.min(50, rect.width / 2);

        const width = rect.width;
        const offsetX = dragEvent.pageX - rect.left;

        if (offsetX < margin)
            return 0;

        if (offsetX > (width - margin))
            return 1;

        return undefined;
    }

    getOffsetVertical(dragEvent: DragEvent, rect: ClientRect) {

        var margin = Math.min(50, rect.height / 2);

        const height = rect.height;
        const offsetY = dragEvent.pageY - rect.top;

        if (offsetY < margin)
            return 0;

        if (offsetY > (height - margin))
            return 1;

        return undefined;
    }

    handlerDragOver = (de: React.DragEvent<any>, index: number, orientation: "h" | "v") => {
        de.preventDefault();

        const th = de.currentTarget as HTMLElement;
        

        const size = th.scrollWidth;

        const offset = orientation == "v" ? 
            this.getOffsetVertical((de.nativeEvent as DragEvent), th.getBoundingClientRect()):
            this.getOffsetHorizontal((de.nativeEvent as DragEvent), th.getBoundingClientRect());

        let dropBorderIndex = offset == undefined ? undefined : index + offset;

        if (dropBorderIndex == this.state.dragIndex || dropBorderIndex == this.state.dragIndex! + 1)
            dropBorderIndex = undefined;

        //de.dataTransfer.dropEffect = dropBorderIndex == undefined ? "none" : "move";

        if (this.state.dropBorderIndex != dropBorderIndex) {
            this.state.dropBorderIndex = dropBorderIndex;
            this.forceUpdate();
        }
    }

    getDragConfig(index: number, orientation: "h" | "v"): DragConfig{
        return {
            dropClass: classes(
                index == this.state.dragIndex && "sf-dragging",
                this.dropClass(index, orientation)),
            onDragStart: e => this.handleDragStart(e, index),
            onDragEnd: this.handleDragEnd,
            onDragOver: e => this.handlerDragOver(e, index, orientation),
            onDrop: this.handleDrop,
        };
    }

    dropClass(index: number, orientation: "h" | "v") {
        const dropBorderIndex = this.state.dropBorderIndex;

        return dropBorderIndex != null && index == dropBorderIndex ? (orientation == "h" ? "drag-left" : "drag-top") :
            dropBorderIndex != null && index == dropBorderIndex - 1 ? (orientation == "h" ? "drag-right" : "drag-bottom") :
                undefined;
    }

    handleDrop = (de: React.DragEvent<any>) => {

        de.preventDefault();
        const dropBorderIndex = this.state.dropBorderIndex!;
        if (dropBorderIndex == null)
            return;

        const dragIndex = this.state.dragIndex!;
        const list = this.props.ctx.value!;
        const temp = list[dragIndex!];
        list.removeAt(dragIndex!);
        const rebasedDropIndex = dropBorderIndex > dragIndex ? dropBorderIndex - 1 : dropBorderIndex;
        list.insertAt(rebasedDropIndex, temp);


        this.state.dropBorderIndex = undefined;
        this.state.dragIndex = undefined;
        this.forceUpdate();
    }

    
}

export interface DragConfig {
    onDragStart?: React.DragEventHandler<any>;
    onDragEnd?: React.DragEventHandler<any>;
    onDragOver?: React.DragEventHandler<any>;
    onDrop?: React.DragEventHandler<any>;
    dropClass?: string;
}