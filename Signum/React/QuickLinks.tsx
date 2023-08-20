import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import { getTypeInfo, getQueryNiceName, getQueryKey, getTypeName, Type, tryGetTypeInfo, PseudoType, QueryKey } from './Reflection'
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

  const [quickLink, setQuickLink] = React.useState<QuickLink<any> | null>(null);

  React.useEffect(() => {
    getQuickLinkByKey(p.quickLinkKey, { lite: p.lite, lites: [p.lite] })
      .then(l => l ? setQuickLink(l) : setQuickLink(null));
  }, [p]);

  if (!quickLink)
    return null
  return (<a className={classes("badge badge-pill sf-quicklinks", "bg-" + quickLink.color, quickLink.color == "light" ? undefined : "text-white")}
    title={StyleContext.default.titleLabels ? quickLink.text() : undefined}
    role="button"
    href="#"
    data-toggle="dropdown"
    onClick={e => { e.preventDefault(); quickLink.handleClick({ lite: p.lite, lites: [p.lite] }, e); }}>
    {quickLink.icon && <FontAwesomeIcon icon={quickLink.icon} color={quickLink.color ? undefined : quickLink.iconColor} />}
    {quickLink.icon && "\u00A0"}
    {quickLink.text()}
  </a>)
}

export function clearQuickLinks() {
  Dic.clear(globalQuickLinks);
  Dic.clear(typeQuickLinks);
}

export interface QuickLinkContext<T extends Entity> {
  lite: Lite<T>;
  lites: Lite<T>[];
  widgetContext?: WidgetContext<T>;
  contextualContext?: ContextualItemsContext<T>;
}

type Seq<T> = (T | undefined)[] | T | undefined;
type PromiSeq<T> = Promise<Seq<T>> | Seq<T>;

const globalQuickLinks: Array<(entityType: string) => (Promise<{ [key: string]: QuickLink<Entity> }>)> = [];
const typeQuickLinks: { [entityType: string]: { [key: string]: QuickLink<Entity> } } = {};

export function registerGlobalQuickLink(f: (entityType: string) => (PromiSeq<QuickLink<Entity>>)) {

  globalQuickLinks.push(entityType => asPromiseArray(f(entityType)).then(qls => qls.toObject(ql => ql.key)));
}

export function registerQuickLink<T extends Entity>(type: Type<T>, quickLink: QuickLink<T>) {
  const typeName = getTypeName(type);
  const qls = typeQuickLinks[typeName] ?? {};
  Dic.addOrThrow(qls, quickLink.key, quickLink);
  typeQuickLinks[typeName] = qls;
}

function getAllRegistered(entityType: string) {

  return Promise.all(globalQuickLinks.map(a => a(entityType)))
    .then(globalLinks =>
      globalLinks.concat(typeQuickLinks[entityType] ?? {}))
    .then(allQuickLinks =>
      Object.assign({}, ...allQuickLinks) as { [key: string]: QuickLink<Entity> });
}

export function getQuickLinks(ctx: QuickLinkContext<Entity>): PromiSeq<QuickLink<Entity>> {

  return getAllRegistered(ctx.lite.EntityType)
    .then(gs => 
      Dic.getValues(gs)
      .filter(ql => ql && ql.isVisible && !ql.hideInAutos)
      .orderBy(ql => ql!.order));
}

function getQuickLinkTokens(entityType: string): Promise<ManualToken[]> {

  return getAllRegistered(entityType)
    .then(ql => toManualTokens(ql))
}

function getQuickLinkByKey(key: string, ctx: QuickLinkContext<Entity>): Promise<QuickLink<any> | undefined> {

  return getAllRegistered(ctx.lite.EntityType)
    .then(qlDic => qlDic && qlDic[key]);
}

function toManualTokens(qlDic: { [key: string]: QuickLink<Entity> }) {

  return qlDic && Object.entries(qlDic)
    .filter(([key, quicklink]) => key && quicklink.text && quicklink.text() && (quicklink.isVisible === undefined || quicklink.isVisible))
    .map(([key, quicklink]) => ({
      key: key,
      toStr: quicklink.text(),
      niceName: quicklink.text(),
      typeColor: quicklink.color,
      niceTypeName: "Cell quick link",
    }));
}

export var ignoreErrors = false;

export function setIgnoreErrors(value: boolean) {
  ignoreErrors = value;
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

  const qlCtx = {
    lite: ctx.lites[0],
    lites: ctx.lites,
    contextualContext: ctx
  };

  return asPromiseArray(getQuickLinks(qlCtx)).then(links => {

    if (links.length == 0)
      return undefined;

    return {
      header: QuickLinkMessage.Quicklinks.niceToString(),
      menuItems: links.map(ql => ql.toDropDownItem(qlCtx))
    } as MenuItemBlock;
  });
}

export interface QuickLinkWidgetProps {
  wc: WidgetContext<ModifiableEntity>
}

export function QuickLinkWidget(p: QuickLinkWidgetProps) {

  const entity = p.wc.ctx.value;

  const qlCtx = {
    lite: toLiteFat(entity as Entity),
    lites: [toLiteFat(entity as Entity)],
    widgetContext: p.wc as WidgetContext<Entity>
  };

  const links = useAPI(signal => {
    if (entity.isNew || !tryGetTypeInfo(entity.Type)?.entityKind)
      return Promise.resolve([]);
    else
      return asPromiseArray(getQuickLinks(qlCtx));
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
                onClick={e => { e.preventDefault(); first.handleClick(qlCtx, e); }}>
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
                  {gr.elements.orderBy(a => a.order).map((a, i) => React.cloneElement(a.toDropDownItem(qlCtx), { key: i }))}
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
  title: (links: QuickLink<any>[]) => string;
  text: (links: QuickLink<any>[]) => string;
  icon: IconProp;
  color: BsColor;
}

export interface QuickLinkOptions {
  key?: string;
  text?: (nothing?: undefined /*TS 4.1 Bug*/) => string; //To delay niceName and avoid exceptions
  isVisible?: boolean;
  hideInAutos?: boolean;
  order?: number;
  icon?: IconProp;
  iconColor?: string;
  color?: BsColor;
  group?: QuickLinkGroup | null;
  openInAnotherTab?: boolean;
  allowsMultiple?: boolean;
}
export abstract class QuickLink<T extends Entity> {
  key!: string;
  text!: () => string;
  isVisible!: boolean;
  hideInAutos?: boolean;
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

  toDropDownItem(ctx: QuickLinkContext<T>) {
    return (
      <Dropdown.Item data-key={this.key} className="sf-quick-link" onMouseUp={e => this.handleClick(ctx, e)}>
        {this.renderIcon()}&nbsp;{this.text()}
      </Dropdown.Item>
    );
  }

  abstract handleClick(ctx: QuickLinkContext<T>, e: React.MouseEvent<any>): void;

  renderIcon() {
    if (this.icon == undefined)
      return undefined;

    return (
      <FontAwesomeIcon icon={this.icon} className="icon" color={this.iconColor} />
    );
  }
}

export class QuickLinkAction<T extends Entity> extends QuickLink<T> {
  action: (ctx: QuickLinkContext<T>, e: React.MouseEvent<any>) => void;

  constructor(action: (ctx: QuickLinkContext<T>, e: React.MouseEvent<any>) => void, options?: QuickLinkOptions) {
    super(options);
    this.action = action;
  } 

  handleClick = (ctx: QuickLinkContext<T>, e: React.MouseEvent<any>) => {
    this.action(ctx, e);
  }
}

export class QuickLinkLink<T extends Entity> extends QuickLink<T> {
  url: (ctx: QuickLinkContext<T>) => (string | Promise<string>);

  constructor(url: (ctx: QuickLinkContext<T>) => (string | Promise<string>), options?: QuickLinkOptions) {
    super(options);
    this.url = url;
  }

  handleClick = async (ctx: QuickLinkContext<T>, e: React.MouseEvent<any>) => {
    var url = typeof this.url === "string" ? this.url : await this.url(ctx); 

    if (this.openInAnotherTab)
      window.open(AppContext.toAbsoluteUrl(url));
    else
      AppContext.pushOrOpenInTab(url, e);
  }
}

interface findOptionsByCtx<T extends Entity> {
  (ctx: QuickLinkContext<T>): FindOptions;
}

export class QuickLinkExplore<T extends Entity> extends QuickLink<T> {
  findOptionsFunc: findOptionsByCtx<T>;

  constructor(queryName: PseudoType | QueryKey, findOptionsFunc: findOptionsByCtx<T>, options?: QuickLinkOptions) {
    super({
      key: getQueryKey(queryName),
      isVisible: Finder.isFindable(queryName, false),
      text: () => getQueryNiceName(queryName),
      ...options
    });

    this.findOptionsFunc = findOptionsFunc;
  }

  handleClick = (ctx: QuickLinkContext<T>, e: React.MouseEvent<any>) => {
    if (e.button == 2)
      return;

    if (e.ctrlKey || e.button == 1)
      window.open(AppContext.toAbsoluteUrl(Finder.findOptionsPath(this.findOptionsFunc(ctx))));
    else
      Finder.explore(this.findOptionsFunc(ctx));
  }
}

export class QuickLinkExplorePromise<T extends Entity> extends QuickLink<T> {
  findOptionsPromise: Promise<findOptionsByCtx<T>>;

  constructor(queryName: PseudoType | QueryKey, findOptionsPromise: Promise<findOptionsByCtx<T>>, options?: QuickLinkOptions) {
    super({
      key: getQueryKey(queryName),
      isVisible: Finder.isFindable(queryName, false),
      text: () => getQueryNiceName(queryName),
      ...options
    });

    this.findOptionsPromise = findOptionsPromise;
  }

  handleClick = (ctx: QuickLinkContext<T>, e: React.MouseEvent<any>) => {
    if (e.button == 2)
      return;

    this.findOptionsPromise.then(fo => {
      if (e.ctrlKey || e.button == 1)
        window.open(AppContext.toAbsoluteUrl(Finder.findOptionsPath(fo(ctx))));
      else
        Finder.explore(fo(ctx));
    });
  }
}


export class QuickLinkNavigate<T extends Entity> extends QuickLink<T> {
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

  handleClick = (ctx: QuickLinkContext<T>, e: React.MouseEvent<any>) => {
    if (e.button == 2)
      return;

    const es = Navigator.getSettings(this.lite.EntityType);
    if (e.ctrlKey || e.button == 1 || es?.avoidPopup)
      window.open(AppContext.toAbsoluteUrl(Navigator.navigateRoute(this.lite, this.viewName)));
    else
      Navigator.view(this.lite, { buttons: "close", getViewPromise: this.viewName ? (e => this.viewName) : undefined });
  }
}
