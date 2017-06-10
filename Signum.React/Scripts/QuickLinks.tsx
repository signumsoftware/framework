import * as React from 'react'
import { Dropdown, MenuItem } from 'react-bootstrap'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from './TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo, getQueryNiceName, getQueryKey, PseudoType, getTypeName, Type } from './Reflection'
import { classes, Dic } from './Globals'
import { FindOptions } from './FindOptions'
import * as Finder from './Finder'
import * as Navigator from './Navigator'
import { ModifiableEntity, EntityPack, ModelState, QuickLinkMessage, Lite, Entity, toLiteFat, is } from './Signum.Entities'
import { onWidgets, WidgetContext } from './Frames/Widgets'
import { onContextualItems, ContextualItemsContext, MenuItemBlock } from './SearchControl/ContextualItems'

export function start() {

    onWidgets.push(getQuickLinkWidget);
    onContextualItems.push(getQuickLinkContextMenus);
}

export interface QuickLinkContext<T extends Entity> {
    lite: Lite<T>;
    widgetContext?: WidgetContext;
    contextualContext?: ContextualItemsContext<T>;
}

type Seq<T> = (T | undefined)[] | T | undefined;

export const onGlobalQuickLinks: Array<(ctx: QuickLinkContext<Entity>) => Seq<QuickLink> | Promise<Seq<QuickLink>>> = [];
export function registerGlobalQuickLink(quickLinkGenerator: (ctx: QuickLinkContext<Entity>) => Seq<QuickLink> | Promise<Seq<QuickLink>>) {
    onGlobalQuickLinks.push(quickLinkGenerator);
}

export const onQuickLinks: { [typeName: string]: Array<(ctx: QuickLinkContext<Entity>) => Seq<QuickLink> | Promise<Seq<QuickLink>>> } = {};
export function registerQuickLink<T extends Entity>(type: Type<T>, quickLinkGenerator: (ctx: QuickLinkContext<T>) => Seq<QuickLink> | Promise<Seq<QuickLink>>) {
    const typeName = getTypeName(type);

    const col = onQuickLinks[typeName] || (onQuickLinks[typeName] = []);

    col.push(quickLinkGenerator);
}

export function getQuickLinks(ctx: QuickLinkContext<Entity>): Promise<QuickLink[]> {

    let promises = onGlobalQuickLinks.map(f => asPromiseArray<QuickLink>(f(ctx)));

    if (onQuickLinks[ctx.lite.EntityType]) {
        const specificPromises = onQuickLinks[ctx.lite.EntityType].map(f => asPromiseArray<QuickLink>(f(ctx)));

        promises = promises.concat(specificPromises);
    }

    return Promise.all(promises).then(links => links.flatMap(a => a || []).filter(a => a && a.isVisible).orderBy(a => a.order));
}

function asPromiseArray<T>(value: Seq<T> | Promise<Seq<T>>): Promise<T[]> {

    if (!value)
        return Promise.resolve([] as T[]);

    if ((value as Promise<Seq<T>>).then)
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

export function getQuickLinkWidget(ctx: WidgetContext): React.ReactElement<any> {

    return <QuickLinkWidget ctx={ctx} />;
}

export function getQuickLinkContextMenus(ctx: ContextualItemsContext<Entity>): Promise<MenuItemBlock | undefined> {

    if (ctx.lites.length != 1)
        return Promise.resolve(undefined);

    return getQuickLinks({
        lite: ctx.lites[0],
        contextualContext: ctx
    }).then(links => {

        if (links.length == 0)
            return undefined;

        return {
            header: QuickLinkMessage.Quicklinks.niceToString(),
            menuItems: links.map((ql, i) => ql.toMenuItem(i))
        } as MenuItemBlock;
    });
}

export interface QuickLinkWidgetProps {
    ctx: WidgetContext
}

export class QuickLinkWidget extends React.Component<QuickLinkWidgetProps, { links?: QuickLink[] }> {

    constructor(props: QuickLinkWidgetProps) {
        super(props);
        this.state = { links: undefined };
    }

    componentWillMount() {
        this.makeRequest(this.props);
    }

    componentWillReceiveProps(newProps: QuickLinkWidgetProps) {
        if (!is(newProps.ctx.pack.entity as Entity, this.props.ctx.pack.entity as Entity)) {
            this.makeRequest(newProps);
        }
    }

    makeRequest(props: QuickLinkWidgetProps) {
        this.setState({ links: undefined });

        const entity = props.ctx.pack.entity;

        if (entity.isNew || !getTypeInfo(entity.Type) || !getTypeInfo(entity.Type).entityKind) {
            this.setState({ links: [] });
        } else {
            getQuickLinks({
                lite: toLiteFat(props.ctx.pack.entity as Entity),
                widgetContext: props.ctx
            }).then(links => this.setState({ links }))
                .done();
        }
    }

    render() {

        const links = this.state.links;

        if (links != undefined && links.length == 0)
            return null;

        const a = (
            <a
                className={classes("badge", "sf-widgets-active", "sf-quicklinks")}
                title={QuickLinkMessage.Quicklinks.niceToString()}
                role="button"
                href="#"
                data-toggle="dropdown"
                onClick={e => e.preventDefault()} >
                {links && <span className="glyphicon glyphicon-star"></span>}
                {links ? "\u00A0" + links.length : "…"}
            </a >
        );

        return (
            <Dropdown id="quickLinksWidget" pullRight>
                <QuickLinkToggle bsRole="toggle" links={links} />
                <Dropdown.Menu>
                    {links && links.orderBy(a => a.order).map((a, i) => a.toMenuItem(i))}
                </Dropdown.Menu>
            </Dropdown>

        );
    }
}

class QuickLinkToggle extends React.Component<{ bsRole: string, onClick?: (e: React.MouseEvent<any>) => void, links: any[] | undefined }, void>{

    handleOnClick = (e: React.MouseEvent<any>) => {
        e.preventDefault();

        this.props.onClick!(e);
    }

    render() {
        var links = this.props.links;

        return (
            <a
                className={classes("badge", "sf-widgets-active", "sf-quicklinks")}
                title={QuickLinkMessage.Quicklinks.niceToString()}
                role="button"
                data-toggle="dropdown"
                onClick={this.handleOnClick} >
                {links && <span className="glyphicon glyphicon-star"></span>}
                {links ? "\u00A0" + links.length : "…"}
            </a >
        );
    }
}

export interface QuickLinkOptions {
    isVisible?: boolean;
    text?: string;
    order?: number;
    icon?: string;
    iconColor?: string;
}

export abstract class QuickLink {
    isVisible: boolean;
    text: string;
    order: number;
    name: string;
    icon: string;
    iconColor: string;
    ctx: QuickLinkContext<Entity>;

    constructor(name: string, options?: QuickLinkOptions) {
        this.name = name;

        Dic.assign(this, { isVisible: true, text: "", order: 0, ...options });
    }

    abstract toMenuItem(key: any): React.ReactElement<any>;

    renderIcon() {
        if (this.icon == undefined)
            return undefined;

        return (
            <span
                className={classes("icon", this.icon)}
                style={{ color: this.iconColor }}>
            </span>
        );
    }

    onclick: () => void;

}

export class QuickLinkAction extends QuickLink {
    action: (e: React.MouseEvent<any>) => void;

    constructor(name: string, text: string, action: (e: React.MouseEvent<any>) => void, options?: QuickLinkOptions) {
        super(name, options);
        this.text = text;
        this.action = action;
    }

    toMenuItem(key: any) {

        return (
            <MenuItem data-name={this.name} className="sf-quick-link" key={key} onMouseUp={this.action}>
                {this.renderIcon()}
                {this.text}
            </MenuItem>
        );
    }
}

export class QuickLinkLink extends QuickLink {
    url: string;

    constructor(name: string, text: string, url: string, options?: QuickLinkOptions) {
        super(name, options);
        this.text = text;
        this.url = url;
    }

    toMenuItem(key: any) {

        return (
            <MenuItem data-name={this.name} className="sf-quick-link" key={key} onMouseUp={this.handleClick}>
                {this.renderIcon()}
                {this.text}
            </MenuItem>
        );
    }

    handleClick = (e: React.MouseEvent<any>) => {
        Navigator.pushOrOpen(this.url, e);
    }
}

export class QuickLinkExplore extends QuickLink {
    findOptions: FindOptions;

    constructor(findOptions: FindOptions, options?: QuickLinkOptions) {
        super(getQueryKey(findOptions.queryName), {
            isVisible: Finder.isFindable(findOptions.queryName),
            text: getQueryNiceName(findOptions.queryName),
            ...options
        });

        this.findOptions = findOptions;
    }

    toMenuItem(key: any) {
        return (
            <MenuItem data-name={this.name} className="sf-quick-link" key={key} onMouseUp={this.exploreOrPopup}>
                {this.renderIcon()}
                {this.text}
            </MenuItem>
        );
    }

    exploreOrPopup = (e: React.MouseEvent<any>) => {
        if (e.ctrlKey || e.button == 1)
            window.open(Finder.findOptionsPath(this.findOptions));
        else
            Finder.explore(this.findOptions);
    }
}


export class QuickLinkNavigate extends QuickLink {
    lite: Lite<Entity>;

    constructor(lite: Lite<Entity>, options?: QuickLinkOptions) {
        super(lite.EntityType, {
            isVisible: Navigator.isNavigable(lite.EntityType),
            text: getTypeInfo(lite.EntityType).niceName,
            ...options
        });

        this.lite = lite;
    }

    toMenuItem(key: any) {
        return (
            <MenuItem data-name={this.name} className="sf-quick-link" key={key} onMouseUp={this.navigateOrPopup}>
                {this.renderIcon()}
                {this.text}
            </MenuItem>
        );
    }

    navigateOrPopup = (e: React.MouseEvent<any>) => {
        const es = Navigator.getSettings(this.lite.EntityType);
        if (e.ctrlKey || e.button == 1 || es && es.avoidPopup)
            window.open(Navigator.navigateRoute(this.lite));
        else
            Navigator.navigate(this.lite);
    }
}
