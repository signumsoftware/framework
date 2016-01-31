import * as React from 'react'
import { Link } from 'react-router'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks} from '../Lines/LineBase'
import { ModifiableEntity, Lite, IEntity, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityBase, EntityBaseProps} from './EntityBase'



export interface EntityLineProps extends EntityBaseProps {

    ctx: TypeContext<ModifiableEntity | Lite<IEntity>>;

    autoComplete?: boolean;
    autoCompleteGetItems?: (query: string) => Promise<Lite<IEntity>[]>;
    autoCompleteRenderItem?: (lite: Lite<IEntity>, query: string) => React.ReactNode;
}

const a = 2;

export class EntityLine extends EntityBase<EntityLineProps> {

    calculateDefaultState(state: EntityLineProps) {
        super.calculateDefaultState(state);
        state.autoComplete = !state.type.isEmbedded && state.type.name != IsByAll;
        state.autoCompleteGetItems = (query) => Finder.API.findLiteLike({
            types: state.type.name,
            subString: query,
            count: 5
        });

        state.autoCompleteRenderItem = (lite, query) => Typeahead.highlightedText(lite.toStr, query);
    }

    handleOnSelect = (lite: Lite<IEntity>, event: React.SyntheticEvent) => {
        this.convert(lite)
            .then(entity => this.setValue(entity));
        return lite.toStr;
    }

    renderInternal() {

        const s = this.state;

        const hasValue = !!s.ctx.value;

        return <FormGroup ctx={s.ctx} title={s.labelText}>
            <div className="SF-entity-line">
                <div className="input-group">
                    { hasValue ? this.renderLink() : this.renderAutoComplete() }
                    <span className="input-group-btn">
                        {!hasValue && this.renderCreateButton(true) }
                        {!hasValue && this.renderFindButton(true) }
                        {hasValue && this.renderViewButton(true) }
                        {hasValue && this.renderRemoveButton(true) }
                    </span>
                </div>
            </div>
        </FormGroup>;
    }

    renderAutoComplete() {

        const s = this.state;

        if (!s.autoComplete || s.ctx.readOnly)
            return <FormControlStatic ctx={s.ctx}></FormControlStatic>;

        return <Typeahead
            inputAttrs={{ className: "form-control sf-entity-autocomplete" }}
            getItems={s.autoCompleteGetItems}
            renderItem={s.autoCompleteRenderItem}
            onSelect={this.handleOnSelect}/>;
    }

    renderItem = (item: Lite<IEntity>, query: string) => {
        return
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

