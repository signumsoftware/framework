import * as React from 'react'
import { Link } from 'react-router'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, subModelState, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../Lines/LineBase'
import { EntityComponentProps, EntityFrame } from '../Lines'
import { ModifiableEntity, Lite, IEntity, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityBase, EntityBaseProps} from './EntityBase'



export interface EntityLineDetailProps extends EntityBaseProps {
    ctx?: TypeContext<ModifiableEntity | Lite<IEntity>>;
    component?: React.ComponentClass<EntityComponentProps<ModifiableEntity>>;
}

export interface EntityLineDetailState extends EntityLineDetailProps {
    fullEntity?: ModifiableEntity;
}

export class EntityLineDetail extends EntityBase<EntityLineDetailProps, EntityLineDetailState> {

    componentWillMount() {
        this.loadEntity()
            .then(() => this.loadComponent());
    }

    loadEntity() {
        if (this.state.fullEntity)
            return Promise.resolve(this.state.fullEntity);

        var m = this.state.ctx.value as ModifiableEntity;
        if (m.Type) {
            this.setState({ fullEntity: m });
            this.state.fullEntity = m;
            this.forceUpdate();
            return Promise.resolve(m);
        }

        return Navigator.API.fetchEntity(this.state.ctx.value as Lite<Entity>).then(e => {
            this.state.fullEntity = m;
            this.forceUpdate();
            return e;
        });
    }

    loadComponent() {

        var e = this.state.fullEntity

        const promise = this.props.component ? Promise.resolve(this.props.component) :
            Navigator.getSettings(e.Type).onGetComponent(e);

        return promise
            .then(c => this.setState({ component: c }));
    }

    renderInternal() {

        const s = this.state;

        const hasValue = !!s.ctx.value;

     

        return (
            <fieldset className={classes("sf-entity-line-details", s.ctx.hasErrorClass())}>
                <legend>
                    <div>
                        <span>{s.labelText}</span>
                        <span className="pull-right">
                            {!hasValue && this.renderCreateButton(true) }
                            {!hasValue && this.renderFindButton(true) }
                            {hasValue && this.renderRemoveButton(true) }
                        </span>
                    </div>
                </legend>
                <div>

                </div>
            </fieldset>
        );
    }

    renderComponent() {
        if (this.state.fullEntity == null || this.state.component == null)
            return null;

        var pr = this.state.type.isEmbedded ? this.state.ctx.propertyRoute : PropertyRoute.root(getTypeInfo(this.state.fullEntity.Type));

        var ms = this.state.type.isLite ? subModelState(this.state.ctx.modelState, { name: "entity", type: LambdaMemberType.Member }) : this.state.ctx.modelState;

        var ctx = new TypeContext(this.state.ctx, null, pr, new ReadonlyBinding(this.state.fullEntity), ms);
        
        var frame: EntityFrame<ModifiableEntity> = {
            onClose: () => { },
            onReload: pack => { this.setValue(pack.entity); },
            setError: error => { },
        }; 

        return React.createElement<EntityComponentProps<ModifiableEntity>>(this.state.component, {
            ctx: ctx,
            frame: frame
        });
    }


    renderLink() {

        const s = this.state;

        if (s.ctx.readOnly)
            return <FormControlStatic ctx={s.ctx}>{getToString(s.ctx.value) }</FormControlStatic>

        if (s.navigate && s.view) {
            return (
                <a href="#" onClick={this.handleViewClick}
                    className="form-control btn-default sf-entity-line-entity"
                    title={JavascriptMessage.navigate.niceToString() }>
                    {  s.ctx.value.toStr }
                </a>
            );
        } else {
            return (
                <span className="form-control btn-default sf-entity-line-entity">
                    {s.ctx.value.toStr }
                </span>
            );
        }
    }
}

