import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import { getTypeInfo, getQueryNiceName, getQueryKey, getTypeName, Type, tryGetTypeInfo } from './Reflection'
import { classes, Dic } from './Globals'
import { FindOptions } from './FindOptions'
import * as Finder from './Finder'
import * as AppContext from './AppContext'
import * as Navigator from './Navigator'
import { ModifiableEntity, QuickLinkMessage, Lite, Entity, toLiteFat, is } from './Signum.Entities'
import { onWidgets, WidgetContext } from './Frames/Widgets'
import { onContextualItems, ContextualItemsContext, MenuItemBlock } from './SearchControl/ContextualItems'
import { useAPI } from './Hooks';
import { StyleContext } from './Lines'
import { Dropdown } from 'react-bootstrap'
import DropdownToggle from 'react-bootstrap/DropdownToggle'
import { BsColor } from './Components'

export function start() {

  onWidgets.push(getQuickLinkWidget);
  onContextualItems.push(getQuickLinkContextMenus);

  AppContext.clearSettingsActions.push(clearQuickLinks);
}

export interface QuickLinkContext<T extends Entity> {
  lite: Lite<T>;
  lites: Lite<T>[];
  widgetContext?: WidgetContext<T>;
  contextualContext?: ContextualItemsContext<T>;
}

type Seq<T> = (T | undefined)[] | T | undefined;

export function clearQuickLinks() {
  onGlobalQuickLinks.clear();
  Dic.clear(onQuickLinks);
}

export interface RegisteredQuickLink<T extends Entity> {
  factory: (ctx: QuickLinkContext<T>) => Seq<QuickLink> | Promise<Seq<QuickLink>>;
  options?: QuickLinkRegisterOptions;
}


export const onGlobalQuickLinks: Array<RegisteredQuickLink<Entity>> = [];
export function registerGlobalQuickLink(quickLinkGenerator: (ctx: QuickLinkContext<Entity>) => Seq<QuickLink> | Promise<Seq<QuickLink>>, options?: QuickLinkRegisterOptions) {
  onGlobalQuickLinks.push({ factory: quickLinkGenerator, options: options });
}

export const onQuickLinks: { [typeName: string]: Array<RegisteredQuickLink<any>> } = {};
export function registerQuickLink<T extends Entity>(type: Type<T>, quickLinkGenerator: (ctx: QuickLinkContext<T>) => Seq<QuickLink> | Promise<Seq<QuickLink>>, options?: QuickLinkRegisterOptions) {
  const typeName = getTypeName(type);

  const col = onQuickLinks[typeName] || (onQuickLinks[typeName] = []);

  col.push({ factory: quickLinkGenerator, options: options });
}

export var ignoreErrors = false;

export function setIgnoreErrors(value: boolean) {
  ignoreErrors = value;
}

export function getQuickLinks(ctx: QuickLinkContext<Entity>): Promise<QuickLink[]> {

  let promises = onGlobalQuickLinks.filter(a => a.options && a.options.allowsMultiple || ctx.lites.length == 1).map(f => safeCall(f.factory, ctx));

  if (onQuickLinks[ctx.lite.EntityType]) {
    const specificPromises = onQuickLinks[ctx.lite.EntityType].filter(a => a.options && a.options.allowsMultiple || ctx.lites.length == 1).map(f => safeCall(f.factory, ctx));

    promises = promises.concat(specificPromises);
  }

  return Promise.all(promises).then(links => links.flatMap(a => a ?? []).filter(a => a?.isVisible).orderBy(a => a.order));
}


function safeCall(f: (ctx: QuickLinkContext<Entity>) => Seq<QuickLink> | Promise<Seq<QuickLink>>, ctx: QuickLinkContext<Entity>): Promise<QuickLink[]> {
  if (!ignoreErrors)
    return asPromiseArray<QuickLink>(f(ctx));
  else {
    try {
      return asPromiseArray<QuickLink>(f(ctx)).catch(e => {
        console.error(e);
        return [];
      })
    } catch (e) {
      console.error(e);
      return Promise.resolve([]);
    }
  }
}

function asPromiseArray<T>(value: Seq<T> | Promise<Seq<T>>): Promise<T[]> {

  if (!value)
    return Promise.resolve([] as T[]);

  if ((value as Promise<Seq<T>>).then != undefined)
    return (value as Promise<Seq<T>>).then(a => asArray(a));

  return Promise.resolve(asArray(value as Seq<T>))
}

function asArray<T>(valueOrArray: Seq<T>): T[] {
  if (!valueOrArray)
    return [];

  if (Array.isArray(valueOrArray))
    return valueOrArray.filter(a => a != null).map(a => a!);
  else
    return [valueOrArray];
}

export function getQuickLinkWidget(ctx: WidgetContext<ModifiableEntity>): React.ReactElement<any> {

  return <QuickLinkWidget wc={ctx} />;
}

export function getQuickLinkContextMenus(ctx: ContextualItemsContext<Entity>): Promise<MenuItemBlock | undefined> {

  if (ctx.lites.length == 0)
    return Promise.resolve(undefined);

  return getQuickLinks({
    lite: ctx.lites[0],
    lites: ctx.lites,
    contextualContext: ctx
  }).then(links => {

    if (links.length == 0)
      return undefined;

    return {
      header: QuickLinkMessage.Quicklinks.niceToString(),
      menuItems: links.map(ql => ql.toDropDownItem())
    } as MenuItemBlock;
  });
}

export interface QuickLinkWidgetProps {
  wc: WidgetContext<ModifiableEntity>
}

export function QuickLinkWidget(p: QuickLinkWidgetProps) {

  const entity = p.wc.ctx.value;

  const links = useAPI(signal => {
    if (entity.isNew || !tryGetTypeInfo(entity.Type)?.entityKind)
      return Promise.resolve([]);
    else
      return getQuickLinks({
        lite: toLiteFat(entity as Entity),
        lites: [toLiteFat(entity as Entity)],
        widgetContext: p.wc as WidgetContext<Entity>
      });
  }, [entity], { avoidReset: true });

  if (links == undefined)
    return <span>…</span>;

  if (links.length == 0)
    return null;

  const DDToggle = Dropdown.Toggle as any;

  return (
    <>
      {!links ? [] : links.filter(a => a.group !== undefined).orderBy(a => a.order)
        .groupBy(a => a.group?.name ?? a.name)
        .map((gr, i) => {
          var first = gr.elements[0];

          if (first.group == null)
            return (
              <a key={i}
                className={classes("badge badge-pill sf-quicklinks", "bg-" + first.color, first.color == "light" ? undefined : "text-white")}
                title={StyleContext.default.titleLabels ? gr.elements[0].text() : undefined}
                role="button"
                href="#"
                data-toggle="dropdown"
                onClick={e => { e.preventDefault(); first.handleClick(e); }}>
                {first.icon && <FontAwesomeIcon icon={first.icon} color={first.color ? undefined : first.iconColor} />}
                {first.icon && "\u00A0"}
                {first.text()}
              </a>
            );

          else {
            var dd = first.group;

            return (
              <Dropdown id={p.wc.frame.prefix + "_" + dd.name} key={i}>
                <DDToggle as={QuickLinkToggle}
                  title={QuickLinkMessage.Quicklinks.niceToString()}
                  badgeColor={dd.color}
                  content={<>
                  {dd.icon && <FontAwesomeIcon icon={dd.icon} />}
                  {dd.icon && "\u00A0"}
                  {dd.text(gr.elements)}
                </>} />
                <Dropdown.Menu align="end">
                  {gr.elements.orderBy(a => a.order).map((a, i) => React.cloneElement(a.toDropDownItem(), { key: i }))}
                </Dropdown.Menu>
              </Dropdown>
            );
          }
        })}
    </>
  );
}


const QuickLinkToggle = React.forwardRef(function CustomToggle(p: { onClick?: React.MouseEventHandler, title: string, content: React.ReactNode, badgeColor: BsColor }, ref: React.Ref<HTMLAnchorElement>) {

  var textColor = p.badgeColor == "warning" || p.badgeColor == "info" || p.badgeColor == "light" ? "text-dark" : undefined;

  return (
    <a
      ref={ref}
      className={classes("badge badge-pill sf-quicklinks", "btn-" + p.badgeColor, textColor)}
      title={StyleContext.default.titleLabels ? QuickLinkMessage.Quicklinks.niceToString() : undefined}
      role="button"
      href="#"
      data-toggle="dropdown"
      onClick={e => { e.preventDefault(); p.onClick!(e); }}>
      {p.content}
    </a>
  );
});

export interface QuickLinkGroup {
  name: string;
  title: (links: QuickLink[]) => string;
  text: (links: QuickLink[]) => string;
  icon: IconProp;
  color: BsColor;
}

export interface QuickLinkOptions {
  isVisible?: boolean;
  text?: (nothing?: undefined /*TS 4.1 Bug*/) => string; //To delay niceName and avoid exceptions
  order?: number;
  icon?: IconProp;
  iconColor?: string;
  color?: BsColor;
  group?: QuickLinkGroup | null;
  openInAnotherTab?: boolean;

}
export interface QuickLinkRegisterOptions {
  allowsMultiple?: boolean;
}

export abstract class QuickLink {
  isVisible!: boolean;
  text!: () => string;
  order!: number;
  name: string;
  icon?: IconProp;
  iconColor?: string;
  color?: BsColor;
  group?: QuickLinkGroup;
  openInAnotherTab?: boolean;
  

  static defaultGroup: QuickLinkGroup = {
    name: "quickLinks",
    icon: "star",
    text: links => links.length.toString(),
    title: () => QuickLinkMessage.Quicklinks.niceToString(),
    color: "light"
  };

  constructor(name: string, options?: QuickLinkOptions) {
    this.name = name;

    Dic.assign(this, { isVisible: true, text: () => "", order: 0, ...options });

    if (this.group === undefined)
      this.group = QuickLink.defaultGroup;
  }

  toDropDownItem() {
    return (
      <Dropdown.Item data-name={this.name} className="sf-quick-link" onMouseUp={this.handleClick}>
        {this.renderIcon()}&nbsp;{this.text()}
      </Dropdown.Item>
    );
  }

  abstract handleClick(e: React.MouseEvent<any>): void;

  renderIcon() {
    if (this.icon == undefined)
      return undefined;

    return (
      <FontAwesomeIcon icon={this.icon} className="icon" color={this.iconColor} />
    );
  }
}

export class QuickLinkAction extends QuickLink {
  action: (e: React.MouseEvent<any>) => void;

  constructor(name: string, text: () => string, action: (e: React.MouseEvent<any>) => void, options?: QuickLinkOptions) {
    super(name, options);
    this.text = text;
    this.action = action;
  }

  handleClick = (e: React.MouseEvent<any>) => {
    e.persist();
    this.action(e);
  }
}

export class QuickLinkLink extends QuickLink {
  url: string | (() => Promise<string>);

  constructor(name: string, text: () => string, url: string | (()=> Promise<string>), options?: QuickLinkOptions) {
    super(name, options);
    this.text = text;
    this.url = url;
  }

  handleClick = (e: React.MouseEvent<any>) => {
    if (typeof this.url === "string") {
      if (this.openInAnotherTab)
        window.open(this.url);
      else
        AppContext.pushOrOpenInTab(this.url, e);
    }
    else {
      e.persist();
      this.url()
        .then(url => {
          if (this.openInAnotherTab)
            window.open(url);
          else
            AppContext.pushOrOpenInTab(url, e)
        });
    }
  }
}

export class QuickLinkExplore extends QuickLink {
  findOptions: FindOptions;

  constructor(findOptions: FindOptions, options?: QuickLinkOptions) {
    super(getQueryKey(findOptions.queryName), {
      isVisible: Finder.isFindable(findOptions.queryName, false),
      text: () => getQueryNiceName(findOptions.queryName),
      ...options
    });

    this.findOptions = findOptions;
  }

  handleClick = (e: React.MouseEvent<any>) => {
    if (e.button == 2)
      return;

    if (e.ctrlKey || e.button == 1)
      window.open(Finder.findOptionsPath(this.findOptions));
    else
      Finder.explore(this.findOptions);
  }
}

export class QuickLinkExplorePromise extends QuickLink {
  findOptionsPromise: Promise<FindOptions>;

  constructor(queryName: any, findOptionsPromise: Promise<FindOptions>, options?: QuickLinkOptions) {
    super(getQueryKey(queryName), {
      isVisible: Finder.isFindable(queryName, false),
      text: () => getQueryNiceName(queryName),
      ...options
    });

    this.findOptionsPromise = findOptionsPromise;
  }

  handleClick = (e: React.MouseEvent<any>) => {
    if (e.button == 2)
      return;

    e.persist();

    this.findOptionsPromise.then(fo => {
      if (e.ctrlKey || e.button == 1)
        window.open(Finder.findOptionsPath(fo));
      else
        Finder.explore(fo);
    });
  }
}


export class QuickLinkNavigate extends QuickLink {
  lite: Lite<Entity>;
  viewName?: string;

  constructor(lite: Lite<Entity>, viewName?: string, options?: QuickLinkOptions) {
    super(lite.EntityType, {
      isVisible: Navigator.isViewable(lite.EntityType),
      text: () => getTypeInfo(lite.EntityType).niceName!,
      ...options
    });

    this.lite = lite;
    this.viewName = viewName;
  }



  handleClick = (e: React.MouseEvent<any>) => {
    if (e.button == 2)
      return;

    const es = Navigator.getSettings(this.lite.EntityType);
    if (e.ctrlKey || e.button == 1 || es?.avoidPopup)
      window.open(Navigator.navigateRoute(this.lite, this.viewName));
    else
      Navigator.view(this.lite, { buttons: "close", getViewPromise: this.viewName ? (e => this.viewName) : undefined });
  }
}
