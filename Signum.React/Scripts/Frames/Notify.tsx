import * as React from 'react'
import { Dic } from '../Globals'
import * as Navigator from '../Navigator'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { Entity, Lite, is, toLite, JavascriptMessage, getToString, EntityPack, ModelState, ModifiableEntity } from '../Signum.Entities'
import { TypeContext, StyleOptions } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos } from '../Reflection'
import Transition from 'react-overlays/lib/Transition'

require("!style!css!./Notify.css");

type NotifyType = "warning" | "error" | "success" | "loading";

interface NotifyOptions
{
    text: React.ReactChild;
    type: NotifyType;
}

interface NotifyState {
    text?: React.ReactChild;
    type?: NotifyType;
}


export default class Notify extends React.Component<void, NotifyState>{

    static singletone: Notify;

    constructor(props: void) {
        super(props);
        this.state = { text: undefined, type: undefined };

        Notify.singletone = this;
    }

    _isMounted: boolean;
    componentDidMount() {
        this._isMounted = true;
    }

    componentWillUnmount() {
        this._isMounted = false;
    }

    handler: number;
    notifyTimeout(options: NotifyOptions, timeout: number = 2000) {
        this.notify(options);
        this.handler = setTimeout(() => this.clear(), timeout);
    }

    notify(options: NotifyOptions) {
        if (!this._isMounted)
            return;
        clearTimeout(this.handler);
        this.setState(options);
    }

    clear() {
        if (!this._isMounted)
            return;
        clearTimeout(this.handler);
        this.setState({ text: undefined, type: undefined })
    }


    notifyPendingRequest(pending: number) {
        if (pending)
            this.notify({ text: JavascriptMessage.loading.niceToString(), type: "loading" });
        else
            this.clear();
    }
    

    render() {
        
        return (
            <div id="sfNotify">
                <Transition in={this.state.text != undefined} className='notify' enteredClassName='in' enteringClassName='in' >
                    <span className={this.state.type}>{this.state.text}</span>
                </Transition>
            </div>
        );
    }
}

