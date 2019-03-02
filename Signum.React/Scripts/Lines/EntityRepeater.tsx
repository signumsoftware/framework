import * as React from 'react'
import { classes } from '../Globals'
import * as Navigator from '../Navigator'
import { TypeContext, mlistItemContext } from '../TypeContext'
import { ModifiableEntity, Lite, Entity, EntityControlMessage } from '../Signum.Entities'
import { EntityBase, TitleManager } from './EntityBase'
import { EntityListBase, EntityListBaseProps, DragConfig } from './EntityListBase'
import { RenderEntity } from './RenderEntity'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { getTypeInfos, getTypeInfo } from '../Reflection';

export interface EntityRepeaterProps extends EntityListBaseProps {
  createAsLink?: boolean | ((er: EntityRepeater) => React.ReactElement<any>);
  avoidFieldSet?: boolean;
  createMessage?: string;
}

export class EntityRepeater extends EntityListBase<EntityRepeaterProps, EntityRepeaterProps> {

  calculateDefaultState(state: EntityRepeaterProps) {
    super.calculateDefaultState(state);
    state.viewOnCreate = false;
    state.createAsLink = true;
  }

  renderInternal() {

    let ctx = this.state.ctx;

    if (this.props.avoidFieldSet == true)
      return (
        <div className={classes("SF-repeater-field SF-control-container", ctx.errorClassBorder)}
          {...{ ...this.baseHtmlAttributes(), ...this.state.formGroupHtmlAttributes }}>
          {this.renderButtons()}
          {this.renderElements()}
        </div>
      );

    return (
      <fieldset className={classes("SF-repeater-field SF-control-container", ctx.errorClass)}
        {...{ ...this.baseHtmlAttributes(), ...this.state.formGroupHtmlAttributes }}>
        <legend>
          <div>
            <span>{this.state.labelText}</span>
            {this.renderButtons()}
          </div>
        </legend>
        {this.renderElements()}
      </fieldset>
    );
  }

  renderButtons() {
    const buttons = (
      <span className="float-right">
        {this.state.createAsLink == false && this.renderCreateButton(false, this.props.createMessage)}
        {this.renderFindButton(false)}
        {this.props.extraButtons && this.props.extraButtons(this)}
      </span>
    );

    return EntityBase.hasChildrens(buttons) ? buttons : undefined;
  }

  renderElements() {
    const ctx = this.state.ctx;
    const readOnly = ctx.readOnly;
    const showType = getTypeInfos(ctx.propertyRoute.typeReference().name).length > 1;
    return (
      <div className="sf-repater-elements">
        {
          mlistItemContext(ctx).map((mlec, i) =>
            (<EntityRepeaterElement key={i}
              onRemove={this.canRemove(mlec.value) && !readOnly ? e => this.handleRemoveElementClick(e, i) : undefined}
              ctx={mlec}
              draggable={this.canMove(mlec.value) && !readOnly ? this.getDragConfig(i, "v") : undefined}
              getComponent={this.props.getComponent}
              getViewPromise={this.props.getViewPromise}
              title={showType ? <span className="sf-type-badge">{getTypeInfo(mlec.value.Type || mlec.value.EntityType).niceName}</span> : undefined} />))
}
        {
          this.state.createAsLink && this.state.create && !readOnly &&
          (typeof this.state.createAsLink == "function" ? this.state.createAsLink(this) :
            <a href="#" title={TitleManager.useTitle ? EntityControlMessage.Create.niceToString() : undefined}
              className="sf-line-button sf-create"
              onClick={this.handleCreateClick}>
              <FontAwesomeIcon icon="plus" className="sf-create" />&nbsp;{this.props.createMessage || EntityControlMessage.Create.niceToString()}
            </a>)
        }
      </div>
    );
  }
}


export interface EntityRepeaterElementProps {
  ctx: TypeContext<Lite<Entity> | ModifiableEntity>;
  getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
  getViewPromise?: (entity: ModifiableEntity) => undefined | string | Navigator.ViewPromise<ModifiableEntity>;
  onRemove?: (event: React.MouseEvent<any>) => void;
  draggable?: DragConfig;
  title?: React.ReactElement<any>;
}

export class EntityRepeaterElement extends React.Component<EntityRepeaterElementProps>
{
  render() {
    const drag = this.props.draggable;

    return (
      <div className={drag && drag.dropClass}
        onDragEnter={drag && drag.onDragOver}
        onDragOver={drag && drag.onDragOver}
        onDrop={drag && drag.onDrop}>
        <fieldset className="sf-repeater-element"
          {...EntityListBase.entityHtmlAttributes(this.props.ctx.value)}>
          <legend>
            <div className="item-group">
              {this.props.onRemove && <a href="#" className={classes("sf-line-button", "sf-remove")}
                onClick={this.props.onRemove}
                title={TitleManager.useTitle ? EntityControlMessage.Remove.niceToString() : undefined}>
                <FontAwesomeIcon icon="times" />
              </a>}
              &nbsp;
            {drag && <a href="#" className={classes("sf-line-button", "sf-move")}
                draggable={true}
                onDragStart={drag.onDragStart}
                onDragEnd={drag.onDragEnd}
                title={TitleManager.useTitle ? EntityControlMessage.Move.niceToString() : undefined}>
                <FontAwesomeIcon icon="bars" />
              </a>}
              {this.props.title && '\xa0'}
              {this.props.title}
            </div>
          </legend>
          <div className="sf-line-entity">
            <RenderEntity ctx={this.props.ctx} getComponent={this.props.getComponent} getViewPromise={this.props.getViewPromise} />
          </div>
        </fieldset>
      </div>
    );
  }
}

