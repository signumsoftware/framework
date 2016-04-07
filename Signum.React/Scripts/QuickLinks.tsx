import * as React from 'react'
import { Dropdown, MenuItem } from 'react-bootstrap'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from './TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo, getQueryNiceName, getQueryKey, PseudoType, getTypeName, Type} from './Reflection'
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
    contextualContext?: ContextualItemsContext;
}

export var onGlobalQuickLinks: Array<(ctx: QuickLinkContext<Entity>) => QuickLink | QuickLink[] | Promise<QuickLink> | Promise<QuickLink[]>> = [];
export function registerGlobalQuickLink(quickLinkGenerator: (ctx: QuickLinkContext<Entity>) => QuickLink | QuickLink[] | Promise<QuickLink> | Promise<QuickLink[]>)
{
    onGlobalQuickLinks.push(quickLinkGenerator);
}

export var onQuickLinks: { [typeName: string]: Array<(ctx: QuickLinkContext<Entity>) => QuickLink | QuickLink[] | Promise<QuickLink> | Promise<QuickLink[]>> } = {};
export function registerQuickLink<T extends Entity>(type: Type<T>, quickLinkGenerator: (ctx: QuickLinkContext<T>) => QuickLink | QuickLink[] | Promise<QuickLink> | Promise<QuickLink[]>)
{
    var typeName = getTypeName(type);

    var col = onQuickLinks[typeName] || (onQuickLinks[typeName] = []);

    col.push(quickLinkGenerator);
}

export function getQuickLinks(ctx: QuickLinkContext<Entity>): Promise<QuickLink[]>{

    var promises = onGlobalQuickLinks.map(f => asPromiseArray<QuickLink>(f(ctx)));

    if (onQuickLinks[ctx.lite.EntityType]) {
        var specificPromises = onQuickLinks[ctx.lite.EntityType].map(f => asPromiseArray<QuickLink>(f(ctx)));

        promises = promises.concat(specificPromises);
    }

    return Promise.all(promises).then(links => links.flatMap(a => a || []).filter(a => a && a.isVisible).orderBy(a => a.order));
}

function asPromiseArray<T>(valueOrPromiseOrArray: T | T[] | Promise<T> | Promise<T[]>): Promise<T[]> {

    if (!valueOrPromiseOrArray)
        return Promise.resolve(null);

    if ((valueOrPromiseOrArray as Promise<T | T[]>).then)
        return (valueOrPromiseOrArray as Promise<T | T[]>).then(a => asArray(a));

    return Promise.resolve(asArray(valueOrPromiseOrArray as T | T[]))
}

function asArray<T>(valueOrArray: T | T[]): T[] {
    if (Array.isArray(valueOrArray))
        return valueOrArray;
    else
        return [valueOrArray];
}

export function getQuickLinkWidget(ctx: WidgetContext): React.ReactElement<any> {

    return <QuickLinkWidget ctx={ctx}/>;
}

export function getQuickLinkContextMenus(ctx: ContextualItemsContext): Promise<MenuItemBlock> {

    if (ctx.lites.length != 1)
        return Promise.resolve(null);

    return getQuickLinks({
        lite: ctx.lites[0],
        contextualContext: ctx
    }).then(links => {

        if (links.length == 0)
            return null;

        return {
            header: QuickLinkMessage.Quicklinks.niceToString(),
            menuItems: links.map((ql, i) => ql.toMenuItem(i))
        } as MenuItemBlock;
    });
}


export class QuickLinkWidget extends React.Component<{ ctx: WidgetContext }, { links: QuickLink[] }> {

    constructor(props) {
        super(props);
        this.state = { links: null };
    }

    componentWillMount() {
        this.makeRequest(this.props);
    }

    componentWillReceiveProps(newProps: { ctx: WidgetContext }) {
        if (!is(newProps.ctx.pack.entity as Entity, this.props.ctx.pack.entity as Entity)) {
            this.makeRequest(newProps);
        }
    }

    makeRequest(props: { ctx: WidgetContext }) {
        this.setState({ links: null });

        if (props.ctx.pack.entity.isNew) {
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

        var links = this.state.links;

        if (links != null && links.length == 0)
            return null;

        var a = (
            <a
                className={classes("badge", "sf-widgets-active", "sf-quicklinks") }
                title={QuickLinkMessage.Quicklinks.niceToString() }
                role="button"
                href="#"
                data-toggle="dropdown"
                onClick={e => e.preventDefault() } >
                { links && <span className="glyphicon glyphicon-star"></span>}
                { links ? "\u00A0" + links.length : "…"}
            </a >
        );

        return (
            <Dropdown id="quickLinksWidget" pullRight>
                {React.cloneElement(a, { "bsRole": "toggle" }) }
                <Dropdown.Menu>
                    { links && links.orderBy(a => a.order).map((a, i) => a.toMenuItem(i)) }
                </Dropdown.Menu>
            </Dropdown>
        );
    }
}

export interface QuickLinkOptions {
    isVisible?: boolean;
    text?: string;
    order?: number;
    glyphicon?: string;
    glyphiconColor?: string;
}

export abstract class QuickLink {
    isVisible: boolean;
    text: string;
    order: number;
    name: string;
    glyphicon: string;
    glyphiconColor: string;
    ctx: QuickLinkContext<Entity>;

    constructor(name: string, options: QuickLinkOptions) {
        this.name = name;

        Dic.extend(this, { isVisible: true, text: "", order: 0 } as QuickLinkOptions, options);
    }

    abstract toMenuItem(key: any): React.ReactElement<any>;

    icon() {
        if (this.glyphicon == null)
            return null;

        return (
            <span
                className={classes("glyphicon", this.glyphicon) }
                style={{ color: this.glyphiconColor }}>
            </span>
        );
    }

    onclick: () => void;

}

export class QuickLinkAction extends QuickLink {
    action: (e: React.MouseEvent) => void;

    constructor(name: string, text: string, action: (e: React.MouseEvent) => void, options?: QuickLinkOptions) {
        super(name, options);
        this.text = text;
        this.action = action;
    }

    toMenuItem(key: any) {

        return (
            <MenuItem data-name={this.name} className="sf-quick-link" key={key} onClick={this.action}>
                {this.icon()}
                {this.text}
            </MenuItem>
        );
    }
}

export class QuickLinkExplore extends QuickLink {
    findOptions: FindOptions;

    constructor(findOptions: FindOptions, options?: QuickLinkOptions) {
        super(getQueryKey(findOptions.queryName), Dic.extend({
            isVisible: Finder.isFindable(findOptions.queryName),
            text: getQueryNiceName(findOptions.queryName),
        }, options));

        this.findOptions = findOptions;
    }

    toMenuItem(key: any) {
        return (
            <MenuItem data-name={this.name} className="sf-quick-link" key={key} onClick={this.exploreOrPopup}>
                {this.icon() }
                {this.text}
            </MenuItem>
        );
    }

    exploreOrPopup = (e: React.MouseEvent) => {
        if (e.ctrlKey || e.button == 2)
            window.open(Finder.findOptionsPath(this.findOptions));
        else
            Finder.explore(this.findOptions);
    }
}


export class QuickLinkNavigate extends QuickLink {
    lite: Lite<Entity>;

    constructor(lite: Lite<Entity>, options?: QuickLinkOptions) {
        super(lite.EntityType, Dic.extend({
            isVisible: Navigator.isNavigable(lite.EntityType),
            text: getTypeInfo(lite.EntityType).niceName,
        }, options));

        this.lite = lite;
    }

    toMenuItem(key: any) {
        return (
            <MenuItem data-name={this.name} className="sf-quick-link" key={key} onClick={this.navigateOrPopup}>
                {this.icon() }
                {this.text}
            </MenuItem>
        );
    }

    navigateOrPopup = (e: React.MouseEvent) => {
        if (e.ctrlKey || e.button == 2 || Navigator.getSettings(this.lite.EntityType).avoidPopup)
            window.open(Navigator.navigateRoute(this.lite));
        else
            Navigator.navigate(this.lite);
    }
}
