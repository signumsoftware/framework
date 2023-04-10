
import * as React from 'react'
import { classes, Dic } from '../Globals'
import { FormGroup } from '../Lines'
import { Typeahead } from '../Components'
import { TypeContext } from '../TypeContext'
import { library } from '@fortawesome/fontawesome-svg-core'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useForceUpdate } from '../Hooks'
import { TypeaheadOptions } from './Typeahead'
import { IconName, IconProp, IconPrefix } from "@fortawesome/fontawesome-svg-core";

export interface IconTypeaheadLineProps {
  ctx: TypeContext<string | null | undefined>;
  onChange?: () => void;
  extraIcons?: string[];
  inputAttrs?: React.InputHTMLAttributes<HTMLInputElement>;
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
    <FormGroup ctx={ctx} label={ctx.niceName()}>
      {inputId => <IconTypeahead icon={ctx.value}
        placeholder={p.ctx.placeholderLabels ? p.ctx.niceName() : undefined}
        extraIcons={p.extraIcons}
        formControlClass={ctx.formControlClass}
        inputAttrs={p.inputAttrs}
        onChange={handleChange} />}
    </FormGroup>
  );
}

export interface IconTypeaheadProps {
  icon: string | null | undefined;
  onChange: (newIcon: string | null | undefined) => void;
  extraIcons?: string[];
  formControlClass: string | undefined;
  placeholder?: string;

  inputAttrs?: React.InputHTMLAttributes<HTMLInputElement>;
}

function toFamilyName(prefix: string) {
  switch (prefix) {
    case "fas": return "fa-solid"; 
    case "fab": return "fa-brands"; 
    case "far": return "fa-regular"; 
    case "fal": return "fa-light"; 
    case "fat": return "fa-thin"; 
    case "fad": return "fa-duotone";
    default: break;
  };
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

  var fontAwesome = Dic.getKeys(lib.definitions).flatMap(prefix => Dic.getKeys(lib.definitions[prefix]).map(name => `${toFamilyName(prefix)} fa-${name}`));
  var icons = ([] as string[]).concat(p.extraIcons ?? []).concat(fontAwesome);

  function handleGetItems(query: string) {
    if (!query)
      return Promise.resolve(([] as string[]).concat(p.extraIcons ?? []).concat(["fa-regular fa-", "fa-solid fa-"]));

    var words = query.toLowerCase().split(" ");

    const result = icons
      .filter(k => {
        var lk = k.toLowerCase();
        return words.every(w => lk.contains(w));
      })
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
        {TypeaheadOptions.highlightedTextAll(item as string, query)}
      </span>
    );
  }

  return (
    <Typeahead
      value={(p.icon ?? "")}
      inputAttrs={{ className: classes(p.formControlClass, "sf-entity-autocomplete"), placeholder: p.placeholder, ...p.inputAttrs }}
      getItems={handleGetItems}
      onSelect={handleSelect}
      onChange={handleSelect}
      renderItem={handleRenderItem}
      minLength={0}
    />
  );
}

export function parseIcon(iconName: string | undefined | null): IconProp | undefined {

  if (iconName == "none")
    return null as any as undefined;

  if (iconName == null)
    return undefined;

  var result = {
    prefix: iconName.tryBefore(" ") as IconPrefix,
    iconName: iconName.tryAfter(" fa-") as IconName,
  };

  return result.iconName && result.prefix && result;
}

export function iconToString(icon: IconProp) {
  return typeof icon == "string" ? "fas fa-" + icon :
    Array.isArray(icon) ? icon[0] + " fa-" + icon[1] :
      icon.prefix + " fa-" + icon.iconName;
}

