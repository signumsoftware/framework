import * as React from 'react'
import { Link } from 'react-router'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../Lines/LineBase'
import { EntityComponentProps, EntityFrame } from '../Lines'
import { ModifiableEntity, Lite, IEntity, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityBase, EntityBaseProps} from './EntityBase'
import { RenderEntity } from './RenderEntity'

export interface EntityDetailProps extends EntityBaseProps {
    ctx?: TypeContext<ModifiableEntity | Lite<IEntity>>;
    getComponent?: (mod: ModifiableEntity) => Promise<React.ComponentClass<EntityComponentProps<ModifiableEntity>>>;
}

export class EntityDetail extends EntityBase<EntityDetailProps, EntityDetailProps> {

    calculateDefaultState(state: EntityDetailProps) {
        super.calculateDefaultState(state);
        state.viewOnCreate = false;
    }

    renderInternal() {

        const s = this.state;

        const hasValue = !!s.ctx.value;

        return (
            <fieldset className={classes("sf-entity-line-details", s.ctx.binding.errorClass) }>
                <legend>
                    <div>
                        <span>{s.labelText}</span>
                        <span className="pull-right">
                            {!hasValue && this.renderCreateButton(false) }
                            {!hasValue && this.renderFindButton(false) }
                            {hasValue && this.renderRemoveButton(false) }
                        </span>
                    </div>
                </legend>
                <div>
                    <RenderEntity ctx={this.state.ctx} getComponent={this.props.getComponent}/>
                </div>
            </fieldset>
        );
    }
}

