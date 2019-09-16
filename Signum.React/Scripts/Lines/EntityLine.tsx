import * as React from 'react'
import * as Navigator from '../Navigator'
import { classes } from '../Globals'
import { TypeContext } from '../TypeContext'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { ModifiableEntity, Lite, Entity, JavascriptMessage, toLite, liteKey, getToString, isLite } from '../Signum.Entities'
import { Typeahead } from '../Components'
import { EntityBase, EntityBaseProps, TitleManager } from './EntityBase'
import { AutocompleteConfig } from './AutoCompleteConfig'

export interface EntityLineProps extends EntityBaseProps {
  ctx: TypeContext<ModifiableEntity | Lite<Entity> | undefined | null>;
  autocomplete?: AutocompleteConfig<unknown> | null;
  renderItem?: React.ReactNode;
  showType?: boolean;
  itemHtmlAttributes?: React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
}

export interface EntityLineState extends EntityLineProps {
  currentItem?: { entity: ModifiableEntity | Lite<Entity>, item?: unknown };
}

export class EntityLine extends EntityBase<EntityLineProps, EntityLineState> {
  overrideProps(state: EntityLineState, overridenProps: EntityLineProps) {
    super.overrideProps(state, overridenProps);
    if (state.autocomplete === undefined) {
      const type = state.type!;
      state.autocomplete = Navigator.getAutoComplete(type, state.findOptions, state.ctx, state.create!, state.showType);
    }

    if (!state.currentItem) {
      if (this.state && this.state.currentItem && this.state.currentItem.entity == state.ctx.value)
        state.currentItem = this.state.currentItem;
    }
  }

  componentWillUnmount() {
    this.state.autocomplete && this.state.autocomplete.abort();
  }

  componentWillMount() {
    this.refreshItem(this.props);
  }

  componentWillReceiveProps(newProps: EntityLineProps, nextContext: any) {

    super.componentWillReceiveProps(newProps, nextContext);

    this.refreshItem(newProps);
  }

  refreshItem(props: EntityLineProps) {
    if (this.state.autocomplete) {
      var newEntity = props.ctx.value;

      if (newEntity == null) {
        if (this.state.currentItem)
          this.setState({ currentItem: undefined });
      } else {
        if (!this.state.currentItem || this.state.currentItem.entity !== newEntity) {
          var ci = { entity: newEntity!, item: undefined as unknown }
          this.setState({ currentItem: ci });
          var fillItem = (newEntity: ModifiableEntity | Lite<Entity>) => {
            const autocomplete = this.state.autocomplete;
            autocomplete && autocomplete.getItemFromEntity(newEntity)
              .then(item => {
                if (autocomplete == this.state.autocomplete) {
                  ci.item = item;
                  this.forceUpdate();
                } else {
                  fillItem(newEntity);
                }
              })
              .done();
          };
          fillItem(newEntity);

        }
      }
    }
  }


  typeahead?: Typeahead | null;
  writeInTypeahead(query: string) {
    this.typeahead!.writeInInput(query);
  }

  handleOnSelect = (item: any, event: React.SyntheticEvent<any>) => {
    this.state.autocomplete!.getEntityFromItem(item)
      .then(entity => entity == null ? undefined :
        this.convert(entity)
          .then(entity => {
            this.state.autocomplete!.getItemFromEntity(entity).then(newItem => //newItem could be different to item on create new case
              this.setState({ currentItem: { entity: entity, item: newItem } }));
            
            this.setValue(entity);
          }))
      .done();

    return "";
  }

  setValue(val: any) {
    if (val != null)
      this.focusNext = true;

    super.setValue(val);
    this.refreshItem(this.props);

    if (val == null)
      this.writeInTypeahead("");
  }

  renderInternal() {

    const s = this.state;

    const hasValue = !!s.ctx.value;

    const buttons = (
      <span className="input-group-append">
        {!hasValue && this.renderCreateButton(true)}
        {!hasValue && this.renderFindButton(true)}
        {hasValue && this.renderViewButton(true, s.ctx.value!)}
        {hasValue && this.renderRemoveButton(true, s.ctx.value!)}
        {this.props.extraButtons && this.props.extraButtons(this)}
      </span>
    );

    var linkOrAutocomplete = hasValue ? this.renderLink() : this.renderAutoComplete();

    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText}
        htmlAttributes={{ ...this.baseHtmlAttributes(), ...EntityBase.entityHtmlAttributes(s.ctx.value!), ...s.formGroupHtmlAttributes }}
        labelHtmlAttributes={s.labelHtmlAttributes}>
        <div className="SF-entity-line">
          {
            !EntityBase.hasChildrens(buttons) ?
              <div style={{ position: "relative" }}>{linkOrAutocomplete}</div> :
              <div className={s.ctx.inputGroupClass}>
                {linkOrAutocomplete}
                {buttons}
              </div>
          }
        </div>
      </FormGroup>
    );
  }

  renderAutoComplete() {

    const ctx = this.state.ctx;

    var ac = this.state.autocomplete;

    if (ac == null || ctx.readOnly)
      return <FormControlReadonly ctx={ctx}>{ctx.value && ctx.value.toStr}</FormControlReadonly>;

    return (
      <Typeahead ref={ta => this.typeahead = ta}
        inputAttrs={{ className: classes(ctx.formControlClass, "sf-entity-autocomplete", this.mandatoryClass) }}
        getItems={query => ac!.getItems(query)}
        getItemsDelay={ac.getItemsDelay}
        minLength={ac.minLength}
        renderItem={(item, query) => ac!.renderItem(item, query)}
        renderList={ac!.renderList && (ta => ac!.renderList!(ta))}
        itemAttrs={item => ({ 'data-entity-key': ac!.getDataKeyFromItem(item) }) as React.HTMLAttributes<HTMLButtonElement>}
        onSelect={this.handleOnSelect} />
    );
  }

  focusNext?: boolean;

  setLinkOrSpan(linkOrSpan?: HTMLElement | null) {
    if (this.focusNext && linkOrSpan != null) {
      linkOrSpan.focus();
    }
    this.focusNext = undefined;
  }

  renderLink() {

    const s = this.state;

    var value = s.ctx.value!;

    const str =
      s.renderItem ? s.renderItem :
        s.currentItem && s.currentItem.item && s.autocomplete ? s.autocomplete.renderItem(s.currentItem.item) :
          getToString(value);

    if (s.ctx.readOnly)
      return <FormControlReadonly ctx={s.ctx}>{str}</FormControlReadonly>

    if (s.navigate && s.view) {
      return (
        <a ref={e => this.setLinkOrSpan(e)}
          href="#" onClick={this.handleViewClick}
          className={classes(s.ctx.formControlClass, "sf-entity-line-entity")}
          title={TitleManager.useTitle ? JavascriptMessage.navigate.niceToString() : undefined} {...s.itemHtmlAttributes}>
          {str}
        </a>
      );
    } else {
      return (
        <span tabIndex={0} ref={e => this.setLinkOrSpan(e)} className={classes(s.ctx.formControlClass, "sf-entity-line-entity")} {...s.itemHtmlAttributes}>
          {str}
        </span>
      );
    }
  }
}





