import * as React from 'react'
import { Link } from 'react-router'
import { Dic, classes } from '../Globals'
import { ModifiableEntity, Lite, IEntity, Entity, MListElement, MList, EntityControlMessage, JavascriptMessage, toLite, is, liteKey } from '../Signum.Entities'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from '../Lines/LineBase'
import Typeahead from '../Lines/Typeahead'
import SelectorPopup from '../SelectorPopup'
import { EntityBase, EntityBaseProps} from './EntityBase'


export interface EntityListBaseProps extends EntityBaseProps {
    move?: boolean;
    onFindMany?: () => Promise<(ModifiableEntity | Lite<IEntity>)[]>;

    ctx?: TypeContext<MList<Lite<Entity> | ModifiableEntity>>;
}


export abstract class EntityListBase<T extends EntityListBaseProps, S extends EntityListBaseProps> extends EntityBase<T, S>
{
    calculateDefaultState(props: S) {

        if (props.onFind)
            throw new Error(`'onFind' property is not applicable to '${this}'. Use 'onFindMany' instead`);

        super.calculateDefaultState(props);
    }

    setValue(list: MList<Lite<Entity> | ModifiableEntity>) {
        super.setValue(list as any);
    }

    moveUp(index: number) {
        const list = this.props.ctx.value;

        const entity = list[index]
        list.removeAt(index);
        list.insertAt(index - 1, entity);
        this.setValue(list);
    }
    renderMoveUp(btn: boolean, index: number) {
        if (!this.state.move || this.state.ctx.readOnly)
            return null;

        return (
            <a className={classes("sf-line-button", "sf-move", btn ? "btn btn-default" : null) }
                onClick={() => this.moveUp(index) }
                title={EntityControlMessage.MoveUp.niceToString() }>
                <span className="glyphicon glyphicon-chevron-up"/>
            </a>
        );
    }

    moveDown(index: number) {
        const list = this.props.ctx.value;
        const entity = list[index]
        list.removeAt(index);
        list.insertAt(index + 1, entity);
        this.setValue(list);
    }
    renderMoveDown(btn: boolean, index: number) {
        if (!this.state.move || this.state.ctx.readOnly)
            return null;

        return (
            <a className={classes("sf-line-button", "sf-move", btn ? "btn btn-default" : null) }
                onClick={() => this.moveDown(index) }
                title={EntityControlMessage.MoveUp.niceToString() }>
                <span className="glyphicon glyphicon-chevron-down"/>
            </a>);
    }

    handleCreateClick = (event: React.SyntheticEvent) => {
        const onCreate = this.props.onCreate ?
            this.props.onCreate() : this.defaultCreate();

        onCreate.then(e => {

            if (e == null)
                return null;

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
                const list = this.props.ctx.value;
                list.push({ element: e, rowId: null });
                this.setValue(list);
            });
        });
    };

    defaultFindMany(): Promise<(ModifiableEntity | Lite<IEntity>)[]> {
        return this.chooseType(Finder.isFindable)
            .then(qn => qn == null ? null : Finder.findMany({ queryName: qn } as FindOptions));
    }


    handleFindClick = (event: React.SyntheticEvent) => {
        const result = this.state.onFindMany ? this.state.onFindMany() : this.defaultFindMany();

        result.then(lites => {
            if (!lites)
                return;

            Promise.all(lites.map(a => this.convert(a))).then(entites => {
                const list = this.props.ctx.value;
                entites.forEach(e => list.push({ element: e, rowId: null }));
                this.setValue(list);
            });
        });
    };

    handleRemoveElementClick = (event: React.SyntheticEvent, index: number) => {
        var mle = this.props.ctx.value[index];

        (this.props.onRemove ? this.props.onRemove(mle.element) : Promise.resolve(true))
            .then(result => {
                if (result == false)
                    return;

                this.props.ctx.value.remove(mle);
                this.forceUpdate();
            });
    }; 

}