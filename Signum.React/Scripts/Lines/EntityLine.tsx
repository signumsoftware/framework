import * as React from 'react'
import * as Navigator from '../Navigator'
import { classes } from '../Globals'
import { TypeContext } from '../TypeContext'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { ModifiableEntity, Lite, Entity, JavascriptMessage, toLite, liteKey, getToString, isLite, is, parseLiteList } from '../Signum.Entities'
import { Typeahead } from '../Components'
import { EntityBaseController, EntityBaseProps } from './EntityBase'
import { AutocompleteConfig } from './AutoCompleteConfig'
import { TypeaheadController } from '../Components/Typeahead'
import { useAPI, useMounted } from '../Hooks'
import { useController } from './LineBase'

export interface EntityLineProps extends EntityBaseProps {
  ctx: TypeContext<ModifiableEntity | Lite<Entity> | undefined | null>;
  avoidLink?: boolean;
  avoidViewButton?: boolean;
  avoidCreateButton?: boolean;
  autocomplete?: AutocompleteConfig<unknown> | null;
  renderItem?: React.ReactNode;
  showType?: boolean;
  inputAttributes?: React.InputHTMLAttributes<HTMLInputElement>,
  itemHtmlAttributes?: React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
}

interface ItemPair {
  entity: ModifiableEntity | Lite<Entity>;
  item?: unknown;
}

export class EntityLineController extends EntityBaseController<EntityLineProps> {
  currentItem!: ItemPair | undefined;
  setCurrentItem!: (v: ItemPair | undefined) => void;
  focusNext!: React.MutableRefObject<boolean>;
  typeahead!: React.RefObject<TypeaheadController>;

  init(pro: EntityLineProps) {
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

  overrideProps(p: EntityLineProps, overridenProps: EntityLineProps) {
    super.overrideProps(p, overridenProps);
    if (p.autocomplete === undefined && p.type) {
      p.autocomplete = Navigator.getAutoComplete(p.type, p.findOptions, p.findOptionsDictionary,  p.ctx, p.create!, p.showType);
    }
  }

  setValue(val: any, event?: React.SyntheticEvent) {
    if (val != null)
      this.focusNext.current = true;

    super.setValue(val, event);

    if (val == null) {
      setTimeout(() => {
        this.writeInTypeahead("");
      }, 100);
    }
  }

  writeInTypeahead(query: string) {
    this.typeahead.current && this.typeahead.current.writeInInput(query);
  }

  handleOnSelect = (item: any, event: React.SyntheticEvent<any>) => {
    this.props.autocomplete!.getEntityFromItem(item)
      .then(entity => entity &&
        this.convert(entity)
          .then(entity => {
            return this.props.autocomplete!.getItemFromEntity(entity) //newItem could be different to item on create new case
              .then(newItem => {
                this.setCurrentItem({ entity: entity, item: newItem });
                this.setValue(entity, event);
              });
          }));

    return "";
  }
}


export const EntityLine = React.memo(React.forwardRef(function EntityLine(props: EntityLineProps, ref: React.Ref<EntityLineController>) {
  const c = useController(EntityLineController, props, ref);
  const p = c.props;

  if (c.isHidden)
    return null;

  const hasValue = !!p.ctx.value;

  const buttons = (
    <>
      {c.props.extraButtonsBefore && c.props.extraButtonsBefore(c)}
      {!hasValue && !p.avoidViewButton && c.renderCreateButton(true)}
      {!hasValue && c.renderFindButton(true)}
      {hasValue && !p.avoidViewButton && c.renderViewButton(true, p.ctx.value!)}
      {hasValue && c.renderRemoveButton(true, p.ctx.value!)}
      {c.renderPasteButton(true)}
      {c.props.extraButtonsAfter && c.props.extraButtonsAfter(c)}
    </>
  );

  return (
    <FormGroup ctx={p.ctx} label={p.label} helpText={p.helpText}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...EntityBaseController.entityHtmlAttributes(p.ctx.value!), ...p.formGroupHtmlAttributes }}
      labelHtmlAttributes={p.labelHtmlAttributes}>
      <div className="sf-entity-line">
        {
          !EntityBaseController.hasChildrens(buttons) ?
            (hasValue ? renderLink() : renderAutoComplete()) :
            (hasValue ?
              <div className={p.ctx.inputGroupClass}>
                {renderLink()}
                {buttons}
              </div> :
              renderAutoComplete(input => <div className={p.ctx.inputGroupClass}>
                {input}
                {buttons}
              </div>)
            )
        }
      </div>
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

  function renderAutoComplete(renderInput?: (input: React.ReactElement<any>) => React.ReactElement<any>) {

    const ctx = p.ctx;

    var ac = p.autocomplete;

    if (ac == null || ctx.readOnly) {
      var fcr = <FormControlReadonly ctx={ctx} className={classes(ctx.formControlClass, "sf-entity-autocomplete", c.mandatoryClass)}>{ctx.value && Navigator.renderLiteOrEntity(ctx.value)}</FormControlReadonly>;
      return renderInput ? renderInput(fcr) : fcr;
    }

    return (
      <Typeahead ref={c.typeahead}
        inputAttrs={{
          className: classes(ctx.formControlClass, "sf-entity-autocomplete", c.mandatoryClass),
          placeholder: ctx.placeholderLabels ? p.ctx.niceName() : undefined,
          onPaste: p.paste == false ? undefined : handleOnPaste,
          ...p.inputAttributes
        }}
        getItems={query => ac!.getItems(query)}
        itemsDelay={ac.getItemsDelay()}
        minLength={ac.getMinLength()}
        renderItem={(item, query) => ac!.renderItem(item, query)}
        renderList={ac!.renderList && (ta => ac!.renderList!(ta))}
        itemAttrs={item => ({ 'data-entity-key': ac!.getDataKeyFromItem(item) }) as React.HTMLAttributes<HTMLButtonElement>}
        onSelect={c.handleOnSelect}
        renderInput={renderInput}
      />
    );
  }

  function renderLink() {

    var value = p.ctx.value!;

    const str =
      p.renderItem ? p.renderItem :
        c.currentItem && c.currentItem.item && p.autocomplete ? p.autocomplete.renderItem(c.currentItem.item) :
          getToString(value);

    if (p.ctx.readOnly)
      return <FormControlReadonly ctx={p.ctx}>{str}</FormControlReadonly>

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
}), (prev, next) => EntityBaseController.propEquals(prev, next));
