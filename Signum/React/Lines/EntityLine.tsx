import * as React from 'react'
import { Navigator } from '../Navigator'
import { classes } from '../Globals'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { ModifiableEntity, Lite, Entity, JavascriptMessage, toLite, liteKey, getToString, isLite, is, parseLiteList } from '../Signum.Entities'
import { Typeahead } from '../Components'
import { Aprox, EntityBaseController, EntityBaseProps } from './EntityBase'
import { AutocompleteConfig } from './AutoCompleteConfig'
import { TextHighlighter, TypeaheadController } from '../Components/Typeahead'
import { useAPI, useMounted } from '../Hooks'
import { genericMemo, useController } from './LineBase'
import { getTimeMachineIcon } from './TimeMachineIcon'


export interface EntityLineProps<V extends ModifiableEntity | Lite<Entity> | null> extends EntityBaseProps<V> {
  avoidLink?: boolean;
  avoidViewButton?: boolean;
  avoidCreateButton?: boolean;
  autocomplete?: AutocompleteConfig<unknown> | null;
  renderItem?: React.ReactNode;
  showType?: boolean;
  inputAttributes?: React.InputHTMLAttributes<HTMLInputElement>,
  itemHtmlAttributes?: React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
  ref?: React.Ref<EntityLineController<NoInfer<V>>>;
}

interface ItemPair {
  entity: ModifiableEntity | Lite<Entity>;
  item?: unknown;
}

export class EntityLineController<V extends ModifiableEntity | Lite<Entity> | null> extends EntityBaseController<EntityLineProps<V>,V> {
  currentItem!: ItemPair | undefined;
  setCurrentItem!: (v: ItemPair | undefined) => void;
  focusNext!: React.RefObject<boolean>;
  typeahead!: React.RefObject<TypeaheadController | null>;

  init(pro: EntityLineProps<V>): void {
    super.init(pro);

    [this.currentItem, this.setCurrentItem] = React.useState<ItemPair | undefined>();
    const mounted = useMounted();
    this.focusNext = React.useRef(false);
    this.typeahead = React.useRef<TypeaheadController>(null);
    React.useEffect(() => {
      const p = this.props;
      if (p.autocomplete) {
        var entity = p.ctx.value;

        if (entity == null) {
          if (this.currentItem)
            this.setCurrentItem(undefined);
        } else {
          if (!this.currentItem || !is(this.currentItem.entity as Entity | Lite<Entity>, entity as Entity | Lite<Entity>) || getToString(this.currentItem.entity) != getToString(entity)) {
            var ci = { entity: entity!, item: undefined as unknown }
            this.setCurrentItem(ci);

            var fillItem = (newEntity: ModifiableEntity | Lite<Entity>) => {
              const autocomplete = this.props.autocomplete;
              autocomplete?.getItemFromEntity(newEntity)
                .then(item => {
                  if (mounted.current) {
                    if (autocomplete == this.props.autocomplete) {
                      ci.item = item;
                      this.forceUpdate();
                    } else {
                      fillItem(newEntity);
                    }
                  }
                });
            };

            fillItem(entity);
            return () => { p.autocomplete && p.autocomplete.abort(); };
          }
        }
      }

      return undefined;
    }, [pro.ctx.value]);

  }

  overrideProps(p: EntityLineProps<V>, overridenProps: EntityLineProps<V>): void {
    super.overrideProps(p, overridenProps);
    if (p.autocomplete === undefined && p.type) {
      p.autocomplete = Navigator.getAutoComplete(p.type, p.findOptions, p.findOptionsDictionary,  p.ctx, p.create!, p.showType);
    }
  }

  setValue(val: any, event?: React.SyntheticEvent): void {
    if (val != null)
      this.focusNext.current = true;

    super.setValue(val, event);

    if (val == null) {
      window.setTimeout(() => {
        this.writeInTypeahead("");
      }, 100);
    }
  }

  writeInTypeahead(query: string): void {
    this.typeahead.current && this.typeahead.current.writeInInput(query);
  }

  handleOnSelect = (item: unknown, event: React.SyntheticEvent<any>) => {
    this.props.autocomplete!.getEntityFromItem(item)
      .then(entity => entity &&
        this.convert(entity as Aprox<V>)
          .then(entity => {
            return this.props.autocomplete!.getItemFromEntity(entity!) //newItem could be different to item on create new case
              .then(newItem => {
                this.setCurrentItem({ entity: entity!, item: newItem });
                this.setValue(entity, event);
              });
          }));

    return "";
  }
}


export const EntityLine: <V extends ModifiableEntity | Lite<Entity> | null>(props: EntityLineProps<V>) => React.ReactNode | null
  = genericMemo(function EntityLine<V extends ModifiableEntity | Lite<Entity> | null>(props: EntityLineProps<V>): React.ReactElement | null {
    const c = useController<EntityLineController<V>, EntityLineProps<V>, V>(EntityLineController, props);
    const p = c.props;

    if (c.isHidden)
      return null;

    const hasValue = !!p.ctx.value;

    const buttons = (
      <>
        {c.props.extraButtonsBefore && c.props.extraButtonsBefore(c)}
        {!hasValue && !p.avoidViewButton && c.renderCreateButton(true)}
        {!hasValue && c.renderFindButton(true)}
        {hasValue && !p.avoidViewButton && c.renderViewButton(true)}
        {hasValue && c.renderRemoveButton(true)}
        {c.renderPasteButton(true)}
        {c.props.extraButtons && c.props.extraButtons(c)}
      </>
    );

    const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
    const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

    return (
      <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop}
        htmlAttributes={{ ...c.baseHtmlAttributes(), ...EntityBaseController.entityHtmlAttributes(p.ctx.value!), ...p.formGroupHtmlAttributes }}
        labelHtmlAttributes={p.labelHtmlAttributes}>
        {inputId => <div className="sf-entity-line">
          {getTimeMachineIcon({ ctx: p.ctx })}
          {
            !EntityBaseController.hasChildrens(buttons) ?
              (hasValue ? renderLink(inputId) : renderAutoComplete(inputId)) :
              (hasValue ?
                <div className={p.ctx.inputGroupClass}>
                  {renderLink(inputId)}
                  {buttons}
                </div> :
                renderAutoComplete(inputId, input => <div className={p.ctx.inputGroupClass}>
                  {input}
                  {buttons}
                </div>)
              )
          }
        </div>}
      </FormGroup>
    );

    function handleOnPaste(e: React.ClipboardEvent<HTMLInputElement>) {
      const text = e.clipboardData.getData("text");
      const lites = parseLiteList(text);
      if (lites.length == 0)
        return;

      e.preventDefault();
      c.paste(text);
    }

    function renderAutoComplete(inputId: string, renderInput?: (input: React.ReactElement) => React.ReactElement) {

      const ctx = p.ctx;

      var ac = p.autocomplete;

      if (ac == null || ctx.readOnly) {
        var fcr = <FormControlReadonly id={inputId} ctx={ctx} className={classes(ctx.formControlClass, "sf-entity-autocomplete", c.mandatoryClass)}>{ctx.value && Navigator.renderLiteOrEntity(ctx.value)}</FormControlReadonly>;
        return renderInput ? renderInput(fcr) : fcr;
      }

      return (
        <Typeahead ref={c.typeahead}
          inputId={inputId}
          inputAttrs={{
            className: classes(ctx.formControlClass, "sf-entity-autocomplete", c.mandatoryClass),
            placeholder: ctx.placeholderLabels ? p.ctx.niceName() : undefined,
            onPaste: p.paste == false ? undefined : handleOnPaste,
            ...p.inputAttributes
          }}
          getItems={query => ac!.getItems(query)}
          itemsDelay={ac.getItemsDelay()}
          minLength={ac.getMinLength()}
          renderItem={(item, hl) => ac!.renderItem(item, hl)}
          renderList={ac!.renderList && (ta => ac!.renderList!(ta))}
          itemAttrs={item => ({ 'data-entity-key': ac!.getDataKeyFromItem(item) }) as React.HTMLAttributes<HTMLButtonElement>}
          onSelect={c.handleOnSelect}
          renderInput={renderInput}
        />
      );
    }

    function renderLink(inputId: string) {

      var value = p.ctx.value!;

      const str =
        p.renderItem ? p.renderItem :
          c.currentItem && c.currentItem.item && p.autocomplete ? p.autocomplete.renderItem(c.currentItem.item, new TextHighlighter(undefined)) :
            getToString(value);

      if (p.ctx.readOnly)
        return <FormControlReadonly id={inputId} ctx={p.ctx}>{str}</FormControlReadonly>

      if (p.view && !p.avoidLink) {
        return (
          <a ref={e => setLinkOrSpan(e)}
            href="#" onClick={c.handleViewClick}
            className={classes(p.ctx.formControlClass, "sf-entity-line-entity")}
            title={p.ctx.titleLabels ? JavascriptMessage.navigate.niceToString() : undefined} {...p.itemHtmlAttributes}>
            {str}
          </a>
        );
      } else {
        return (
          <span tabIndex={0} ref={e => setLinkOrSpan(e)} className={classes(p.ctx.formControlClass, "sf-entity-line-entity")} {...p.itemHtmlAttributes}>
            {str}
          </span>
        );
      }
    }

    function setLinkOrSpan(linkOrSpan?: HTMLElement | null) {
      if (c.focusNext.current && linkOrSpan != null) {
        linkOrSpan.focus();
      }
      c.focusNext.current = false;
    }
  }, (prev, next) => EntityBaseController.propEquals(prev, next));
