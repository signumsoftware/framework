import * as React from 'react'
import { Link } from 'react-router'
import { ModifiableEntity, Lite, IEntity, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey } from '../Signum.Entities'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from '../Lines/LineBase'
import { EntityListBase, EntityListBaseProps} from './EntityListBase'


export interface EntityListProps extends EntityListBaseProps {
    selectedIndex?: number;
}


export abstract class EntityList extends EntityListBase<EntityListProps>
{   
    moveUp(index: number) {
        super.moveUp(index);
        this.setState({ selectedIndex: this.state.selectedIndex - 1 } as any);
    }

    moveDown(index: number) {
        super.moveDown(index);
        this.setState({ selectedIndex: this.state.selectedIndex + 1 } as any);
    }

    renderInternal() {

        var s = this.state;
        var list = this.state.ctx.value;

        var hasSelected = s.selectedIndex != null;

        return <FormGroup ctx={s.ctx} title={s.labelText}>
            <div className="SF-entity-line">
                <div className="input-group">
                    <select className="form-control" size={6}>
                       {s.ctx.value.map((e, i) => <option  key={i} title={this.getTitle(e.element) }>{e.element.toStr}</option>) }
                        </select>
                    <span className="input-group-btn btn-group-vertical">
                        { this.renderCreateButton(true) }
                        { this.renderFindButton(true) }
                        { hasSelected && this.renderViewButton(true) }
                        { hasSelected && this.renderRemoveButton(true) }
                        { hasSelected && this.state.move && s.selectedIndex > 0 && this.renderMoveUp(true, s.selectedIndex) }
                        { hasSelected && this.state.move && s.selectedIndex < list.length - 1 && this.renderMoveDown(true, s.selectedIndex) }
                        </span>
                    </div>
                </div>
            </FormGroup>;
    }

    handleRemoveClick = (event: React.SyntheticEvent) => {
        var s = this.state;

        (s.onRemove ? s.onRemove(s.ctx.value[s.selectedIndex].element) : Promise.resolve(true))
            .then(result=> {
                if (result == false)
                    return;
                
                s.ctx.value.removeAt(s.selectedIndex);
                if (s.ctx.value.length == s.selectedIndex)
                    s.selectedIndex--;

                if (s.selectedIndex == -1)
                    s.selectedIndex = null;

                this.setValue(s.ctx.value);
            });
    };

    handleViewClick = (event: React.SyntheticEvent) => {

        var ctx = this.state.ctx;
        var selectedIndex = this.state.selectedIndex;
        var entity = ctx.value[selectedIndex].element;

        var onView = this.state.onView ?
            this.state.onView(entity, ctx.propertyRoute) :
            this.defaultView(entity);

        onView.then(e => {
            if (e == null)
                return;

            this.convert(e).then(m => {
                if (is(ctx.value[selectedIndex].element, e))
                    ctx.value[selectedIndex].element = m;
                else
                    ctx.value[selectedIndex] = { element: m };

                this.setValue(ctx.value);
            });
        });
    }

    getTitle(e: Lite<Entity> | ModifiableEntity) {

        var pr = this.props.ctx.propertyRoute;

        var type = pr && pr.member && pr.member.typeNiceName || (e as Lite<Entity>).EntityType || (e as ModifiableEntity).Type;

        var id = (e as Lite<Entity>).id || (e as Entity).id;
        
        return type + (id ? " " + id : "");
    }
}