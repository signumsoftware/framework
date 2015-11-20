/// <reference path="../../../framework/signum.react/scripts/globals.ts" />

import * as React from 'react'
import * as Finder from 'Framework/Signum.React/Scripts/Finder'
import { History } from 'history'
import { FindOptions } from 'Framework/Signum.React/Scripts/FindOptions'


export interface WebMenuItemProps extends React.Props<WebMenuItem>
{
    liAtts?: React.HTMLAttributes;
    aProps?: React.HTMLAttributes;
    link?: any;
    id?: string;
    text?: string;
    visible?: boolean;
}


export default class WebMenuItem extends React.Component<WebMenuItemProps, { visible?: boolean }> {

    componentWillMount() {

        var isVisible = this.calculateVisible();
        
        this.setState({ visible: isVisible });
    }

    calculateVisible(): boolean{
        if (this.props.visible != null)
            return this.props.visible;
       
        var subMenus = this.getSubMenus();
        if (subMenus.length)
            return subMenus.every(a=> a.state.visible);

        if (this.props.link && (this.props.link as FindOptions).queryName)
            return Finder.isFindable((this.props.link as FindOptions).queryName);

        return true;
    }


    getSubMenus() : Array<WebMenuItem>{

        var acum: Array<WebMenuItem> = [];
        var recursive = (module) =>
        {
            if (!module)
                return;

            if (module instanceof WebMenuItem)
            {
                acum.push(module);
            }
            else if (Array.isArray(module))
            {
                for (var a of module)
                {
                    recursive(a);
                }
            }
        }; 

        recursive(this.props.children);

        return acum;
    }


    getUrl(): string {

        if (typeof this.props.link == 'string')
            return this.props.link;

        if ((this.props.link as FindOptions).queryName)
            return Finder.findRoute(this.props.link);

        throw new Error("Unexpected link " + this.props.link);
    }

    getText(): string {

        if (this.props.text)
            return this.props.text;

        if ((this.props.link as FindOptions).queryName)
            return Finder.niceName((this.props.link as FindOptions).queryName);

        throw new Error("Unexpected link " + this.props.link);
    }

    isActive(): boolean {

        var subMenus = this.getSubMenus();

        if (subMenus.length)
            return subMenus.some(a=> a.isActive());
        
        var history: History = (this.context as any).history;

        var url = this.getUrl();

        return (history as any).isActive(url, null);
    }

    render() {

        if (!this.props.visible)
            return null;
        
        var history: History = (this.context as any).history;

        var url = this.getUrl();
        var text = this.getText();

        var isActive: boolean = (history as any).isActive(url, null);

        var aProps = Dic.copy(this.props.aProps);

        aProps.className += this.isActive() ? " active" : "";
        aProps.className += this.props.children ? " dropdown" : "";

        return (<li {...this.props.liAtts}>
            <a href={url} {...aProps}>{text}</a>
            </li>);
    }
}