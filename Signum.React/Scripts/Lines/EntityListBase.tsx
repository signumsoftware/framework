import * as React from 'react'
import { Link } from 'react-router'
import { Dic, classes } from '../Globals'
import { ModifiableEntity, Lite, Entity, MListElement, MList, EntityControlMessage, JavascriptMessage, toLite, is, liteKey } from '../Signum.Entities'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from '../Lines/LineBase'
import Typeahead from '../Lines/Typeahead'
import { EntityBase, EntityBaseProps} from './EntityBase'


export interface EntityListBaseProps extends EntityBaseProps {
    move?: boolean;
    onFindMany?: () => Promise<(ModifiableEntity | Lite<Entity>)[] | undefined>;

    ctx: TypeContext<MList<Lite<Entity> | ModifiableEntity>>;
}


export abstract class EntityListBase<T extends EntityListBaseProps, S extends EntityListBaseProps> extends EntityBase<T, S>
{
    calculateDefaultState(props: S) {

        if (props.onFind)
            throw new Error(`'onFind' property is not applicable to '${this}'. Use 'onFindMany' instead`);


        if(props.ctx.value == undefined)
            props.ctx.value = [];

        super.calculateDefaultState(props);
    }

    setValue(list: MList<Lite<Entity> | ModifiableEntity>) {
        super.setValue(list as any);
    }

    moveUp(index: number) {
        const list = this.props.ctx.value!;
        if (index == 0)
            return;

        const entity = list[index]
        list.removeAt(index);
        list.insertAt(index - 1, entity);
        this.setValue(list);
    }
    renderMoveUp(btn: boolean, index: number) {
        if (!this.state.move || this.state.ctx.readOnly)
            return undefined;

        return (
            <a className={classes("sf-line-button", "sf-move", btn ? "btn btn-default" : undefined) }
                onClick={() => this.moveUp(index) }
                title={EntityControlMessage.MoveUp.niceToString() }>
                <span className="glyphicon glyphicon-chevron-up"/>
            </a>
        );
    }

    moveDown(index: number) {
        const list = this.props.ctx.value!;
        if (index == list.length - 1)
            return;
  
        const entity = list[index]
        list.removeAt(index);
        list.insertAt(index + 1, entity);
        this.setValue(list);
    }

    renderMoveDown(btn: boolean, index: number) {
        if (!this.state.move || this.state.ctx.readOnly)
            return undefined;

        return (
            <a className={classes("sf-line-button", "sf-move", btn ? "btn btn-default" : undefined) }
                onClick={() => this.moveDown(index) }
                title={EntityControlMessage.MoveUp.niceToString() }>
                <span className="glyphicon glyphicon-chevron-down"/>
            </a>);
    }

    handleCreateClick = (event: React.SyntheticEvent) => {

        event.preventDefault();

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

                const pr = this.state.ctx.propertyRoute.add(a => a[0]);

                return this.state.onView ?
                    this.state.onView(e, pr) :
                    this.defaultView(e, pr);

            }).then(e => {

                if (!e)
                    return;

                this.convert(e).then(m => {
                    const list = this.props.ctx.value!;
                    list.push({ rowId: null, element: m });
                    this.setValue(list);
                }).done();
            }).done();
    };

    defaultFindMany(): Promise<(ModifiableEntity | Lite<Entity>)[] | undefined> {
        return this.chooseType(Finder.isFindable)
            .then<(ModifiableEntity | Lite<Entity>)[] | undefined>(qn => qn == undefined ? undefined : Finder.findMany({ queryName: qn } as FindOptions));
    }


    handleFindClick = (event: React.SyntheticEvent) => {

        event.preventDefault();

        const result = this.state.onFindMany ? this.state.onFindMany() : this.defaultFindMany();

        result.then(lites => {
            if (!lites)
                return;

            Promise.all(lites.map(a => this.convert(a))).then(entites => {
                const list = this.props.ctx.value!;
                entites.forEach(e => list.push({ rowId: null, element: e }));
                this.setValue(list);
            }).done();
        }).done();
    };

    handleRemoveElementClick = (event: React.SyntheticEvent, index: number) => {

        event.preventDefault();


        const list = this.props.ctx.value!;
        const mle = list[index];

        (this.props.onRemove ? this.props.onRemove(mle.element) : Promise.resolve(true))
            .then(result => {
                if (result == false)
                    return;

                list.remove(mle);
                this.forceUpdate();
            }).done();
    };

}