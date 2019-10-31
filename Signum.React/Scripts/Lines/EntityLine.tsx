import * as React from 'react'
import * as Navigator from '../Navigator'
import { classes } from '../Globals'
import { TypeContext } from '../TypeContext'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { ModifiableEntity, Lite, Entity, JavascriptMessage, toLite, liteKey, getToString, isLite, is } from '../Signum.Entities'
import { Typeahead } from '../Components'
import { EntityBaseController, EntityBaseProps } from './EntityBase'
import { AutocompleteConfig } from './AutoCompleteConfig'
import { TypeaheadHandle } from '../Components/Typeahead'
import { useAPI, useMounted } from '../Hooks'

export interface EntityLineProps extends EntityBaseProps {
  ctx: TypeContext<ModifiableEntity | Lite<Entity> | undefined | null>;
  autocomplete?: AutocompleteConfig<unknown> | null;
  renderItem?: React.ReactNode;
  showType?: boolean;
  itemHtmlAttributes?: React.HTMLAttributes<HTMLSpanElement | HTMLAnchorElement>;
}

interface ItemPair {
  entity: ModifiableEntity | Lite<Entity>;
  item?: unknown;
}


export class EntityLineController extends EntityBaseController<EntityLineProps> {
  unmounted = false;
  currentItem: ItemPair | undefined;
  setCurrentItem: (v: ItemPair | undefined) => void;
  focusNext: React.MutableRefObject<boolean>;
  typeahead: React.RefObject<TypeaheadHandle>;

  constructor(p: EntityLineProps) {
    super(p);

    var s = this.props;
    var [currentItem, setCurrentItem] = React.useState<ItemPair | undefined>();
    this.currentItem = currentItem;
    this.setCurrentItem = setCurrentItem;
    const mounted = useMounted();
    this.focusNext = React.useRef(false);
    React.useEffect(() => {
      return () => { this.unmounted = true; };
    }, []);
    this.typeahead = React.useRef<TypeaheadHandle>(null);
    React.useEffect(() => {
      if (s.autocomplete) {
        var entity = s.ctx.value;

        if (entity == null) {
          if (currentItem)
            setCurrentItem(undefined);
        } else {
          if (!currentItem || is(currentItem.entity as Entity | Lite<Entity>, entity as Entity | Lite<Entity>)) {
            var ci = { entity: entity!, item: undefined as unknown }
            setCurrentItem(ci);

            var fillItem = (newEntity: ModifiableEntity | Lite<Entity>) => {
              const autocomplete = s.autocomplete;
              autocomplete && autocomplete.getItemFromEntity(newEntity)
                .then(item => {
                  if (mounted.current) {
                    if (autocomplete == s.autocomplete) {
                      ci.item = item;
                      if (!this.unmounted)
                        this.forceUpdate();
                    } else {
                      fillItem(newEntity);
                    }
                  }
                })
                .done();
            };

            fillItem(entity);
            return () => { s.autocomplete && s.autocomplete.abort(); };
          }
        }
      }

      return undefined;
    }, [s.ctx.value]);

  }

  overrideProps(p: EntityLineProps, overridenProps: EntityLineProps) {
    super.overrideProps(p, overridenProps);
    if (p.autocomplete === undefined) {
      const type = p.type!;
      p.autocomplete = Navigator.getAutoComplete(type, p.findOptions, p.ctx, p.create!, p.showType);
    }
  }

  setValue(val: any) {
    if (val != null)
      this.focusNext.current = true;

    super.setValue(val);

    if (val == null)
      this.writeInTypeahead("");
  }

  writeInTypeahead(query: string) {
    this.typeahead.current && this.typeahead.current.writeInInput(query);
  }
}


export function EntityLine(props: EntityLineProps) {
  const c = new EntityLineController(props);
  const p = c.props;

  function handleOnSelect(item: any, event: React.SyntheticEvent<any>) {
    p.autocomplete!.getEntityFromItem(item)
      .then(entity => entity &&
        c.convert(entity)
          .then(entity => {
            p.autocomplete!.getItemFromEntity(entity)
              .then(newItem => c.setCurrentItem({ entity: entity, item: newItem })); //newItem could be different to item on create new case

            c.setValue(entity);
          }))
      .done();

    return "";
  }

  if (c.isHidden)
    return null;

  const hasValue = !!p.ctx.value;

  const buttons = (
    <span className="input-group-append">
      {!hasValue && c.renderCreateButton(true)}
      {!hasValue && c.renderFindButton(true)}
      {hasValue && c.renderViewButton(true, p.ctx.value!)}
      {hasValue && c.renderRemoveButton(true, p.ctx.value!)}
      {c.props.extraButtons && c.props.extraButtons(c)}
    </span>
  );

  var linkOrAutocomplete = hasValue ? renderLink() : renderAutoComplete();

  return (
    <FormGroup ctx={p.ctx} labelText={p.labelText} helpText={p.helpText}
      htmlAttributes={{ ...c.baseHtmlAttributes(), ...EntityBaseController.entityHtmlAttributes(p.ctx.value!), ...p.formGroupHtmlAttributes }}
      labelHtmlAttributes={p.labelHtmlAttributes}>
      <div className="SF-entity-line">
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

  function renderAutoComplete(renderInput?: (input: React.ReactElement<any>) => React.ReactElement<any>) {

    const ctx = p.ctx;

    var ac = p.autocomplete;

    if (ac == null || ctx.readOnly)
      return <FormControlReadonly ctx={ctx}>{ctx.value && ctx.value.toStr}</FormControlReadonly>;

    return (
      <Typeahead ref={c.typeahead}
        inputAttrs={{ className: classes(ctx.formControlClass, "sf-entity-autocomplete", c.mandatoryClass) }}
        getItems={query => ac!.getItems(query)}
        getItemsDelay={ac.getItemsDelay}
        minLength={ac.minLength}
        renderItem={(item, query) => ac!.renderItem(item, query)}
        renderList={ac!.renderList && (ta => ac!.renderList!(ta))}
        itemAttrs={item => ({ 'data-entity-key': ac!.getDataKeyFromItem(item) }) as React.HTMLAttributes<HTMLButtonElement>}
        onSelect={handleOnSelect}
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

    if (p.navigate && p.view) {
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
}
