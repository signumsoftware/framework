
import * as React from 'react'
import { classes, Dic } from '@framework/Globals'
import { FormGroup } from '@framework/Lines'
import { Typeahead } from '@framework/Components'
import { TypeContext } from '@framework/TypeContext'
import { library } from '@fortawesome/fontawesome-svg-core'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { parseIcon } from '../../Dashboard/Admin/Dashboard';
import { useForceUpdate } from '@framework/Hooks'
import { TypeaheadOptions } from '../../../../Framework/Signum.React/Scripts/Components/Typeahead'

export interface IconTypeaheadLineProps {
  ctx: TypeContext<string | null | undefined>;
  onChange?: () => void;
  extraIcons?: string[];
}

export function IconTypeaheadLine(p : IconTypeaheadLineProps){
  const forceUpdate = useForceUpdate();
  function handleChange(newIcon: string | undefined | null) {
    p.ctx.value = newIcon;
    if (p.onChange)
      p.onChange();
    forceUpdate();
  }

  var ctx = p.ctx;

  return (
    <FormGroup ctx={ctx} labelText={ctx.niceName()} >
      <IconTypeahead icon={ctx.value}
        extraIcons={p.extraIcons}
        formControlClass={ctx.formControlClass}
        onChange={handleChange} />
    </FormGroup>
  );
}

export interface IconTypeaheadProps {
  icon: string | null | undefined;
  onChange: (newIcon: string | null | undefined) => void;
  extraIcons?: string[];
  formControlClass: string | undefined;
}

export function IconTypeahead(p: IconTypeaheadProps) {
  const forceUpdate = useForceUpdate();

  var lib = library as any as {
    definitions: {
      [iconPrefix: string]: {
        [iconName: string]: any;
      }
    }
  };

  var fontAwesome = Dic.getKeys(lib.definitions).flatMap(prefix => Dic.getKeys(lib.definitions[prefix]).map(name => `${prefix} fa-${name}`));
  var icons = ([] as string[]).concat(p.extraIcons ?? []).concat(fontAwesome);

  function handleGetItems(query: string) {
    if (!query)
      return Promise.resolve(([] as string[]).concat(p.extraIcons ?? []).concat(["far fa-", "fas fa-"]));

    const result = icons
      .filter(k => k.toLowerCase().contains(query.toLowerCase()))
      .orderBy(a => a.length)
      .filter((k, i) => i < 5);

    return Promise.resolve(result);
  }

  function handleSelect(item: string | unknown) {
    p.onChange(item as string);
    forceUpdate();
    return item as string;
  }

  function handleRenderItem(item: unknown, query: string) {
    var icon = parseIcon(item as string);

    return (
      <span>
        {icon && <FontAwesomeIcon icon={icon} className="icon" style={{ width: "12px", height: "12px" }} />}
        {TypeaheadOptions.highlightedText(item as string, query)}
      </span>
    );
  }

  return (
    <Typeahead
      value={(p.icon ?? "")}
      inputAttrs={{ className: classes(p.formControlClass, "sf-entity-autocomplete") }}
      getItems={handleGetItems}
      onSelect={handleSelect}
      onChange={handleSelect}
      renderItem={handleRenderItem}
      minLength={0}
    />
  );
}
