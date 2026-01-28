import * as React from 'react'
import { classes, Dic } from '../Globals'
import { FormGroup } from '../Lines'
import { TypeContext } from '../TypeContext'
import { library, config, IconLookup, CssStyleClass, IconStyle } from '@fortawesome/fontawesome-svg-core'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useForceUpdate } from '../Hooks'
import { TextHighlighter, Typeahead, TypeaheadOptions } from './Typeahead'
import { IconName, IconProp, IconPrefix } from "@fortawesome/fontawesome-svg-core";

export interface IconTypeaheadLineProps {
  ctx: TypeContext<string | null | undefined>;
  onChange?: () => void;
  extraIcons?: string[];
  inputAttrs?: React.InputHTMLAttributes<HTMLInputElement>;
  extraButtons?: () => React.ReactNode;
}

export function IconTypeaheadLine(p : IconTypeaheadLineProps): React.ReactElement{
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
      {inputId => <>
        <IconTypeahead icon={ctx.value}
        placeholder={p.ctx.placeholderLabels ? p.ctx.niceName() : undefined}
        extraIcons={p.extraIcons}
        formControlClass={ctx.formControlClass}
        inputAttrs={p.inputAttrs}
          onChange={handleChange} />
        {p.extraButtons?.()}
      </>
      }
    </FormGroup>
  );
}

var lib = library as any as {
  definitions: {
    [iconPrefix: string]: {
      [iconName: string]: any;
    }
  }
};

var allIcons: IconName[];

function getAllIcons(): IconName[] {
  if (allIcons)
    return allIcons;

  allIcons = Dic.getValues(lib.definitions).flatMap(dic => Dic.getKeys(dic)).distinctBy(a => a).orderBy(a => a) as IconName[];
  return allIcons;
}

function getPrefixes(iconName: IconName): IconPrefix[] {
  return Dic.getKeys(lib.definitions).filter(prefix => iconName && lib.definitions[prefix]?.[iconName]) as IconPrefix[];
}


export interface IconTypeaheadProps {
  icon: string | null | undefined;
  onChange: (newIcon: string | null | undefined) => void;
  extraIcons?: string[];
  formControlClass: string | undefined;
  placeholder?: string;

  inputAttrs?: React.InputHTMLAttributes<HTMLInputElement>;
}

function toLongPrefix(prefix: IconPrefix): string | undefined{
  switch (prefix) {
    case "fas": return "fa-solid"; 
    case "fab": return "fa-brands"; 
    case "far": return "fa-regular"; 
    case "fal": return "fa-light"; 
    case "fat": return "fa-thin"; 
    case "fad": return "fa-duotone";
    default: return undefined;
  };
}

function toSortPrefix(family: CssStyleClass | IconStyle | IconPrefix): IconPrefix  {
  switch (family) {
    case "fa-solid": return "fas";
    case "fa-brands": return "fab";
    case "fa-regular": return "far";
    case "fa-light": return "fal";
    case "fa-thin": return "fat"; 
    case "fa-duotone": return "fad"; 
    case "solid": return "fas";
    case "brands": return "fab";
    case "regular": return "far";
    case "light": return "fal";
    case "thin": return "fat";
    case "duotone": return "fad"; 
    default: return family;
  };
}

export function IconTypeahead(p: IconTypeaheadProps): React.ReactElement {
  const forceUpdate = useForceUpdate();

  var parsedIcon = parseIcon(p.icon);

  var fontAwesome = getAllIcons();
  var icons = ([] as string[]).concat(p.extraIcons ?? []).concat(fontAwesome);

  function handleGetItems(query: string) {
    if (!query)
      return Promise.resolve(([] as string[]).concat(p.extraIcons ?? []));

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

  function toValidIcon(iconName: string | null) : string | null {
    if (!iconName)
      return null;

    if (p.extraIcons?.contains(iconName))
      return iconName;

    var prefixes = getPrefixes(iconName as IconName);
    if (prefixes.some(a => a != "fab"))
      return iconName;

    return `${toLongPrefix(prefixes[0])} fa-${iconName}`; //brands
  }

  function handleSelectIcon(item: string | unknown) {

    const validIcon = toValidIcon(item as string | null);
    p.onChange(validIcon as string);
    forceUpdate();
    return item as string;
  }

  function handleSelectFamily(e: React.ChangeEvent<HTMLSelectElement>) {

    var iconName: IconName = typeof parsedIcon == "string" ? parsedIcon : (parsedIcon as IconLookup).iconName;
    var iconPrefix = e.currentTarget.value as IconPrefix;
    if (e.currentTarget.value == "")
      p.onChange(iconName);
    else
      p.onChange(`${iconPrefix} fa-${iconName}`);

    forceUpdate();
  }

  function handleRenderItem(item: unknown, hl: TextHighlighter) {
    var icon = parseIcon(toValidIcon(item as string));

    return (
      <span>
        {icon && <FontAwesomeIcon aria-hidden={true} icon={icon} className="icon" style={{ width: "12px", height: "12px" }} />}
        {hl.highlight(item as string)}
      </span>
    );
  }

  var currentPrefix: IconPrefix | "" = !parsedIcon ? "" :
    typeof parsedIcon == "string" ? "" :
      typeof parsedIcon == "object" ? (parsedIcon as IconLookup).prefix : "";

  var currentIcon = !parsedIcon ? null :
    typeof parsedIcon == "string" ? parsedIcon :
      typeof parsedIcon == "object" ? (parsedIcon as IconLookup).iconName : null;

  var prefixes = currentIcon && getPrefixes(currentIcon);
  var hasDefault = prefixes?.some(p => p != "fab");

  return (
    <div className="row g-2">
      <div className="col-sm-7">
        <Typeahead
          value={(currentIcon ?? "")}
          inputAttrs={{ className: classes(p.formControlClass, "sf-entity-autocomplete"), placeholder: p.placeholder, ...p.inputAttrs }}
          getItems={handleGetItems}
          onSelect={handleSelectIcon}
          onChange={handleSelectIcon}
          renderItem={handleRenderItem}
          minLength={0}
        />
      </div>
      <div className="col-sm-5">
        <select className={p.formControlClass?.replaceAll("control", "select")}
          value={currentPrefix && (toLongPrefix(currentPrefix) || currentPrefix)}
          disabled={!(p.icon && (hasDefault || prefixes != null && prefixes.length > 1))}
          onChange={handleSelectFamily}>
          {hasDefault && <option value={""} > ({config.styleDefault})</option>}
          {prefixes?.map((prefix, i) => <option key={prefix} value={toLongPrefix(prefix)}>{toLongPrefix(prefix)}</option>)}
        </select>
      </div>
    </div>
  );
}

export function parseIcon(iconName: string | undefined | null): IconProp | undefined {

  if (iconName == "none")
    return null as any as undefined;

  if (iconName == null)
    return undefined;

  if (iconName.contains(" "))
    return (
      {
        prefix: iconName.tryBefore(" ") as IconPrefix,
        iconName: iconName.tryAfter(" fa-") as IconName,
      } satisfies IconLookup);

  return (iconName.tryAfter("fa-") ?? iconName) as IconName;
}

export function fallbackIcon(icon: IconProp) : IconProp {
  if (isIconDefined(icon))
    return icon;

  console.error("Icon not found " + JSON.stringify(icon));

  return ({ prefix: "fas", iconName: "question" });
}

export function isIconDefined(icon: IconProp): any {

  if (Array.isArray(icon))
    return lib.definitions[toSortPrefix(icon[0])]?.[icon[1]]

  if (typeof icon == "object")
    return lib.definitions[toSortPrefix(icon.prefix)]?.[icon.iconName];

  if (typeof icon == "string") {
    if (lib.definitions[toSortPrefix(config.styleDefault)]?.[icon])
      return true;
    else {
      return false;
    }
  }
  return false;
}

export function iconToString(icon: IconProp): string {
  return typeof icon == "string" ? "fas fa-" + icon :
    Array.isArray(icon) ? icon[0] + " fa-" + icon[1] :
      icon.prefix + " fa-" + icon.iconName;
}

