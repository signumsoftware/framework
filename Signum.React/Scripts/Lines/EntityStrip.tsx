import * as React from 'react'
import { Link } from 'react-router'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { FindOptions } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle, mlistItemContext } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, ReadonlyBinding, LambdaMemberType } from '../Reflection'
import { LineBase, LineBaseProps, FormGroup, FormControlStatic, runTasks, } from '../Lines/LineBase'
import { EntityComponentProps, EntityFrame } from '../Lines'
import { ModifiableEntity, Lite, IEntity, Entity, MList, MListElement, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString } from '../Signum.Entities'
import Typeahead from '../Lines/Typeahead'
import { EntityListBase, EntityListBaseProps } from './EntityListBase'

export interface EntityStripProps extends EntityListBaseProps {
    vertical?: boolean;
    autoComplete?: boolean;

    autoCompleteGetItems?: (query: string) => Promise<Lite<IEntity>[]>;
    autoCompleteRenderItem?: (lite: Lite<IEntity>, query: string) => React.ReactNode;
}

export class EntityStrip extends EntityListBase<EntityStripProps, EntityStripProps> {

    calculateDefaultState(state: EntityStripProps) {
        super.calculateDefaultState(state);
        state.autoComplete = !state.type.isEmbedded && state.type.name != IsByAll;
        state.create = false;
        state.find = false;
    }

    defaultAutoCompleteGetItems = (query: string) => Finder.API.findLiteLike({
        types: this.state.type.name,
        subString: query,
        count: 5
    });

    defaultAutCompleteRenderItem = (lite, query) => Typeahead.highlightedText(lite.toStr, query);


    renderInternal() {

        const s = this.state;

        var buttons = (
            <span className="input-group-btn">
                { this.renderCreateButton(true) }
                { this.renderFindButton(true) }
            </span>
        );

        if (!buttons.props.children.some(a => a))
            buttons = null;

        return (
            <FormGroup ctx={s.ctx} title={s.labelText}>
                <div className="SF-entity-strip SF-control-container">
                    <ul className={classes("sf-strip", this.props.vertical ? "sf-strip-vertical" : "sf-strip-horizontal") }>
                        {
                            mlistItemContext(this.state.ctx).map((mlec, i) =>
                                (<EntityStripElement key={i}
                                    ctx={mlec}
                                    onRemove={this.state.remove ? e => this.handleRemoveElementClick(e, i) : null} />))
                        }
                        <li className="sf-strip-input">
                            <div className={buttons ? "input-group" : null}>
                                {this.renderAutoComplete() }
                                { buttons }
                            </div>
                        </li>
                    </ul>
                </div>
            </FormGroup>
        );

    }

    handleOnSelect = (lite: Lite<IEntity>, event: React.SyntheticEvent) => {
        this.convert(lite)
            .then(e => {
                const list = this.props.ctx.value;
                list.push({ element: e, rowId: null });
                this.setValue(list);
            }).done();
        return "";
    }


    renderAutoComplete() {

        if (!this.state.autoComplete || this.state.ctx.readOnly)
            return <FormControlStatic ctx={this.state.ctx}></FormControlStatic>;

        return (
            <Typeahead
                inputAttrs={{ className: "form-control sf-entity-autocomplete" }}
                getItems={this.props.autoCompleteGetItems || this.defaultAutoCompleteGetItems}
                renderItem={this.props.autoCompleteRenderItem || this.defaultAutCompleteRenderItem}
                onSelect={this.handleOnSelect}/>
        );
    }
}


export interface EntityStripElementProps {
    onRemove: (event: React.MouseEvent) => void;
    ctx: TypeContext<Lite<Entity> | ModifiableEntity>;
}

export class EntityStripElement extends React.Component<EntityStripElementProps, void>
{
    render() {
        return (
            <li className="sf-strip-element input-group">
                <span className="sf-entitStrip-link">
                    {this.props.ctx.value.toStr}
                </span>
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

