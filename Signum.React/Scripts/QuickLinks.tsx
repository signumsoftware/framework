import * as React from 'react'
import { Dropdown, MenuItem } from 'react-bootstrap'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from './TypeContext'
import { PropertyRouteType, MemberInfo, getTypeInfo, TypeInfo, getQueryNiceName, getQueryKey, PseudoType, getTypeName} from './Reflection'
import { classes, Dic } from './Globals'
import { FindOptions } from './FindOptions'
import * as Finder from './Finder'
import * as Navigator from './Navigator'
import { ModifiableEntity, EntityPack, ModelState, QuickLinkMessage, Lite, Entity, toLiteFat } from './Signum.Entities'
import { onWidgets, WidgetContext } from './Frames/Widgets'
import { onContextualItems, ContextualItemsContext, MenuItemBlock } from './SearchControl/ContextualItems'

export function start() {

    onWidgets.push(getQuickLinkWidget);
    onContextualItems.push(getQuickLinkContextMenus);
}

export interface QuickLinkContext {
    lite: Lite<Entity>;
    widgetContext?: WidgetContext;
    contextualContext?: ContextualItemsContext;

}


export var onGlobalQuickLinks: Array<(ctx: QuickLinkContext) => QuickLink | QuickLink[]> = [];
export function registerGlobalQuickLink(quickLinkGenerator: (ctx: QuickLinkContext) => QuickLink | QuickLink[])
{
    onGlobalQuickLinks.push(quickLinkGenerator);
}

export var onQuickLinks: { [typeName: string]: Array<(ctx: QuickLinkContext) => QuickLink | QuickLink[]> } = {};
export function registerQuickLink(type: PseudoType, quickLinkGenerator: (ctx: QuickLinkContext) => QuickLink | QuickLink[])
{
    var typeName = getTypeName(type);

    var col = onQuickLinks[typeName] || (onQuickLinks[typeName] = []);

    col.push(quickLinkGenerator);
}

export function getQuickLinks(ctx: QuickLinkContext){

    var links = onGlobalQuickLinks.flatMap(f => asArray(f(ctx)));

    if (onQuickLinks[ctx.lite.EntityType])
        links.concat(onQuickLinks[ctx.lite.EntityType].flatMap(f => asArray(f(ctx))));

    return links.filter(a => a && a.isVisible).orderBy(a => a.order);
}

function asArray<T>(valueOrArray: T | T[]): T[] {
    if (Array.isArray(valueOrArray))
        return valueOrArray;
    else
        return [valueOrArray];
}

export function getQuickLinkWidget(ctx: WidgetContext): React.ReactElement<any> {

    var links = getQuickLinks({
        lite: toLiteFat(ctx.pack.entity),
        widgetContext: ctx
    });

    if (links.length == 0)
        return null;

    return <QuickLinkWidget quickLinks={links}/>;
}

export function getQuickLinkContextMenus(ctx: ContextualItemsContext): Promise<MenuItemBlock> {

    if (ctx.lites.length != 1)
        return Promise.resolve(null);

    var links = getQuickLinks({
        lite: ctx.lites[0],
        contextualContext: ctx
    });

    if (links.length == 0)
        return Promise.resolve(null);

    return Promise.resolve({
        header: QuickLinkMessage.Quicklinks.niceToString(),
        menuItems: links.map((ql, i) => ql.toMenuItem(i))
    } as MenuItemBlock);
}


export class QuickLinkWidget extends React.Component<{ quickLinks: QuickLink[] }, void> {
    render() {

        var a = (
            <a 
                className={classes("badge", "sf-widgets-active", "sf-quicklinks") }
                title={QuickLinkMessage.Quicklinks.niceToString() }
                role="button"
                href="#"
                data-toggle="dropdown"
                onClick={e => e.preventDefault() } >
                {this.props.quickLinks.length}
            </a >
        );

        return (
            <Dropdown id="quickLinksWidget" pullRight>
                {React.cloneElement(a, { "bsRole": "toggle" }) }
                <Dropdown.Menu>
                    { this.props.quickLinks.orderBy(a => a.order).map((a, i) => a.toMenuItem(i)) }
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
    ctx: QuickLinkContext;

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
    action: (e: React.MouseEvent, ctx: QuickLinkContext) => void;

    constructor(name: string, text: string, action: (e: React.MouseEvent, ctx: QuickLinkContext) => void, options?: QuickLinkOptions) {
        super(name, options);
        this.text = text;
        this.action = action;
    }

    toMenuItem(key: any) {

        return (
            <MenuItem data-name={this.name} className="sf-quick-link" key={key} onClick={e => this.action(e, this.ctx) }>
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
            <MenuItem data-name={this.name} className="sf-quick-link" key={key} onClick={e => Finder.explore(this.findOptions) }>
                {this.icon() }
                {this.text}
            </MenuItem>
        );
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
