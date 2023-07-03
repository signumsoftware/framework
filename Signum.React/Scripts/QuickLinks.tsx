import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import { getTypeInfo, getQueryNiceName, getQueryKey, getTypeName, Type, tryGetTypeInfo } from './Reflection'
import { classes, Dic } from './Globals'
import { FindOptions, ManualCellDto, ManualToken, QueryToken, toQueryToken } from './FindOptions'
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
import { CellFormatter } from './Finder'
import { registerManualSubTokens } from './SearchControl/QueryTokenBuilder'

export function start() {

  onWidgets.push(getQuickLinkWidget);
  onContextualItems.push(getQuickLinkContextMenus);

  AppContext.clearSettingsActions.push(clearQuickLinks);

  registerManualSubTokens("[QuickLinks]", getQuickLinkTokens);

  Finder.formatRules.push({
    name: "CellQuickLink",
    isApplicable: qt => qt.parent?.key == "[QuickLinks]",

    formatter: (c, sc) => new CellFormatter((dto: ManualCellDto, ctx, token) => (dto.manualTokenKey && dto.lite && <CellQuickLink quickLinkKey = { dto.manualTokenKey } lite = { dto.lite } />), false),
  });
}

function CellQuickLink(p: { quickLinkKey: string, lite: Lite<Entity> }) {

  const [quickLink, setQuickLink] = React.useState<QuickLink | null>(null);

  React.useEffect(() => {
    getQuickLink(p.quickLinkKey, { lite: p.lite, lites: [p.lite] })
      .then(l => l ? setQuickLink(l) : setQuickLink(null));
  }, [p]);

  if (!quickLink)
    return null
  return (<a className={classes("badge badge-pill sf-quicklinks", "bg-" + quickLink.color, quickLink.color == "light" ? undefined : "text-white")}
    title={StyleContext.default.titleLabels ? quickLink.text() : undefined}
    role="button"
    href="#"
    data-toggle="dropdown"
    onClick={e => { e.preventDefault(); quickLink.handleClick(e); }}>
    {quickLink.icon && <FontAwesomeIcon icon={quickLink.icon} color={quickLink.color ? undefined : quickLink.iconColor} />}
    {quickLink.icon && "\u00A0"}
    {quickLink.text()}
  </a>)
}

export interface QuickLinkContext<T extends Entity> {
  lite: Lite<T>;
  lites: Lite<T>[];
  widgetContext?: WidgetContext<T>;
  contextualContext?: ContextualItemsContext<T>;
}

type Seq<T> = (T | undefined)[] | T | undefined;
type PromiSeq<T> = Promise<Seq<T>> | Seq<T>;

export function clearQuickLinks() {
  Dic.clear(globalQuickLinks);
  Dic.clear(typeQuickLinks);
}

export interface GlobalQuickLink<T extends Entity> {
  key: string,
  generator: QuickLinkGenerator<T>
}

export interface TypeQuickLink<T extends Entity> extends GlobalQuickLink<T> {
  type: Type<T>,
}

export interface QuickLinkGenerator<T extends Entity> {
  factory: (ctx: QuickLinkContext<T>) => QuickLink | undefined,
  options?: QuickLinkOptions,
}

const globalQuickLinks: Array<(entityType: string) => (Promise<{ [key: string]: QuickLinkGenerator<Entity> }>)> = [];
const typeQuickLinks: Array<TypeQuickLink<any>> = [];

const quickLinksCache: { [entityType: string]: Promise<{ [key: string]: QuickLinkGenerator<Entity> }> } = {};

export function registerGlobalQuickLink(f: (entityType: string) => (PromiSeq<GlobalQuickLink<Entity>>)) {

  globalQuickLinks.push((t: string) => asPromiseArray(f(t)).then(kg => Dic.toDic(kg.map(kg => ({ key: kg.key, value: kg.generator })))));
}

export function registerQuickLink<T extends Entity>(factory: TypeQuickLink<T>) {
  typeQuickLinks.push(factory);
}

function getCachedOrAdd(entityType: string) {

  return quickLinksCache[entityType] ??=
    Promise.all(globalQuickLinks.map(a => a(entityType)))
      .then(globalLinks => {

        const typeLinks = Dic.toDic(typeQuickLinks.filter(tql => getTypeName(tql.type) == entityType)
          .map(tql => ({ key: tql.key, value: tql.generator })));

        return globalLinks.concat(typeLinks)
      }).then((allQuickLinks) => Dic.concat(allQuickLinks));
}

export function getQuickLinks(ctx: QuickLinkContext<Entity>): PromiSeq<QuickLink> {

  return getCachedOrAdd(ctx.lite.EntityType)
    .then(gs =>
      Dic.map(gs, (k, g) => {
        const qLink = g.factory(ctx);
        applyKeyAndOptions(k, qLink, g.options);

        return qLink;
      }).filter(ql => ql && ql.isVisible).orderBy(ql => ql!.order));
}

function getQuickLinkTokens(entityType: string): Promise<ManualToken[]> {
  const qls = getCachedOrAdd(entityType);

  return qls.then(ql => toManualTokens(ql))
}

function getQuickLink(key: string, ctx: QuickLinkContext<Entity>): Promise<QuickLink | undefined> {
  const entityType = ctx.lite.EntityType;

  return getCachedOrAdd(entityType)
    .then(gs => gs && gs[key])
    .then(g => {
      const qLink = g && g.factory(ctx);
      applyKeyAndOptions(key, qLink, g.options);

      return qLink;
    });
}

function applyKeyAndOptions(key: string, quickLink?: QuickLink, options?: QuickLinkOptions) {
  quickLink && Dic.assign(quickLink, { isVisible: true, text: () => "", order: 0, ...options, key: key })
}

function toManualTokens(qlDic: { [key: string]: QuickLinkGenerator<Entity> }) {
  return qlDic && Dic.filter(qlDic, (kv) => kv.value.options?.text).map((kv) => ({
    toStr: kv.value.options!.text!(),
    niceName: kv.value.options!.text!(),
    key: kv.key,
    typeColor: kv.value.options!.color,
    niceTypeName: "Cell quick link",
  }));
};

export const onQuickLinks_New: { [typeName: string]: { [key: string]: QuickLinkGenerator<Entity> } } = {};
export function registerQuickLink_New1<T extends Entity>(type: Type<T>, factory: Promise<{ [key: string]: QuickLinkGenerator<T> }>) {

  const typeName = getTypeName(type);

  const typeDic = onQuickLinks_New[typeName];

  factory.then(fs => {
    Dic.foreach(fs, (k, f) => Dic.addOrThrow(typeDic, k, f))
  });
}

export var ignoreErrors = false;

export function setIgnoreErrors(value: boolean) {
  ignoreErrors = value;
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

function asPromiseArray<T>(value: Seq<T> | PromiSeq<T>): Promise<T[]> {

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

  return asPromiseArray(getQuickLinks({
    lite: ctx.lites[0],
    lites: ctx.lites,
    contextualContext: ctx
  })).then(links => {

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
      return asPromiseArray(getQuickLinks({
        lite: toLiteFat(entity as Entity),
        lites: [toLiteFat(entity as Entity)],
        widgetContext: p.wc as WidgetContext<Entity>
      }));
  }, [entity], { avoidReset: true });

  if (links == undefined)
    return <span>…</span>;

  if (links.length == 0)
    return null;

  const DDToggle = Dropdown.Toggle as any;

  return (
    <>
      {!links ? [] : links.filter(a => a.group !== undefined).orderBy(a => a.order)
        .groupBy(a => a.group?.name ?? a.key)
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
  key?: string;
  text?: (nothing?: undefined /*TS 4.1 Bug*/) => string; //To delay niceName and avoid exceptions
  isVisible?: boolean;
  order?: number;
  icon?: IconProp;
  iconColor?: string;
  color?: BsColor;
  group?: QuickLinkGroup | null;
  openInAnotherTab?: boolean;
  allowsMultiple?: boolean;
}
export abstract class QuickLink {
  key!: string;
  isVisible!: boolean;
  text!: () => string;
  order!: number;
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

  constructor(options?: QuickLinkOptions) {

    Dic.assign(this, { isVisible: true, text: () => "", order: 0, ...options });

    if (this.group === undefined)
      this.group = QuickLink.defaultGroup;
  }

  toDropDownItem() {
    return (
      <Dropdown.Item data-key={this.key} className="sf-quick-link" onMouseUp={this.handleClick}>
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

  constructor(action: (e: React.MouseEvent<any>) => void, options?: QuickLinkOptions) {
    super(options);
    this.action = action;
  }

  handleClick = (e: React.MouseEvent<any>) => {
    e.persist();
    this.action(e);
  }
}

export class QuickLinkLink extends QuickLink {
  url: string | (() => Promise<string>);

  constructor(url: string | (()=> Promise<string>), options?: QuickLinkOptions) {
    super(options);
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
    super({
      key: getQueryKey(findOptions.queryName),
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
    super({
      key: getQueryKey(queryName),
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
    super({
      key: lite.EntityType, 
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
