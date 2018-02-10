import * as React from 'react'
import * as ReactDOM from 'react-dom'
import { classes, Dic } from '../Globals'
import { Popper, Manager, Target } from 'react-popper';



export interface TypeaheadProps {
    value?: string;
    onChange?: (newValue: string) => void;
    onBlur?: () => void;
    getItems: (query: string) => Promise<any[]>;
    getItemsDelay?: number;
    minLength?: number;
    renderList?: (typeAhead: Typeahead) => React.ReactNode;
    renderItem?: (item: any, query: string) => React.ReactNode;
    onSelect?: (item: any, e: React.KeyboardEvent<any> | React.MouseEvent<any>) => string | null;
    scrollHeight?: number;
    spanAttrs?: React.HTMLAttributes<HTMLSpanElement>;
    inputAttrs?: React.InputHTMLAttributes<HTMLInputElement>;
    liAttrs?: (item: any) => React.LiHTMLAttributes<HTMLLIElement>;
    noResultsMessage?: string;
}

export interface TypeaheadState {
    shown?: boolean;
    items?: any[];
    query?: string;
    selectedIndex?: number;
}


export default class Typeahead extends React.Component<TypeaheadProps, TypeaheadState>
{
    constructor(props: TypeaheadProps) {
        super(props);
        this.state = {
            shown: false,
            items: undefined,
            selectedIndex: undefined,
        };
    }

    static highlightedText = (val: string, query?: string): React.ReactNode => {

        if (query == undefined)
            return val;

        const index = val.toLowerCase().indexOf(query.toLowerCase());
        if (index == -1)
            return val;

        return [
            val.substr(0, index),
            <strong key={0}>{val.substr(index, query.length)}</strong>,
            val.substr(index + query.length)
        ];
    }

    static defaultProps: TypeaheadProps = {
        getItems: undefined as any,
        getItemsDelay: 200,
        minLength: 1,
        renderItem: Typeahead.highlightedText,
        onSelect: (elem, event) => elem,
        scrollHeight: 0,
        noResultsMessage: " - No results -",
    };

    handle: number | undefined;

    lookup() {
        if (!this.props.getItemsDelay) {
            this.populate();
        }
        else {
            if (this.handle != undefined)
                clearTimeout(this.handle);

            this.handle = setTimeout(() => this.populate(), this.props.getItemsDelay);
        }
    }

    componentWillUnmount() {
        if (this.handle != undefined)
            clearTimeout(this.handle);
    }

    populate() {

        if (this.props.minLength == null || this.input.value.length < this.props.minLength) {
            this.setState({ shown: false, items: undefined, selectedIndex: undefined });
            return;
        }

        //this.setState({ shown: true, items: undefined });

        const query = Typeahead.normalizeString(this.input.value);
        this.props.getItems(query).then(items => this.setState({
            items: items,
            shown: true,
            query: query,
            selectedIndex: 0,
        })).done();
    }

    static normalizeString(str: string): string {
        return str;
    }

    select(e: React.KeyboardEvent<any> | React.MouseEvent<any>): boolean {
        if (this.state.items!.length == 0)
            return false;

        const val = this.props.onSelect!(this.state.items![this.state.selectedIndex || 0], e);

        this.input.value = val || "";
        if (this.props.onChange)
            this.props.onChange(this.input.value);

        this.setState({ shown: false });
        return val != null;
    }

    //public
    writeInInput(query: string) {
        this.input.value = query;
        this.input.focus();
        this.lookup();
    }

    focused = false;
    handleFocus = () => {
        if (!this.focused) {
            this.focused = true;
            if (this.props.minLength == 0 && !this.input.value)
                this.lookup();
        }
    }

    handleBlur = () => {
        this.focused = false;

        //if (!this.mouseover && this.state.shown)
        //    this.setState({ shown: false });

        if (this.props.onBlur)
            this.props.onBlur();
    }


    blur() {
        this.input.blur();
    }

    handleKeyDown = (e: React.KeyboardEvent<any>) => {
        if (!this.state.shown)
            return;

        switch (e.keyCode) {
            case 9: // tab
            case 13: // enter
            case 27: // escape
                e.preventDefault();
                break;

            case 38: // up arrow
                {
                    e.preventDefault();
                    const newIndex = ((this.state.selectedIndex || 0) - 1 + this.state.items!.length) % this.state.items!.length;
                    this.setState({ selectedIndex: newIndex });
                    break;
                }
            case 40: // down arrow
                {
                    e.preventDefault();
                    const newIndex = ((this.state.selectedIndex || 0) + 1) % this.state.items!.length;
                    this.setState({ selectedIndex: newIndex });
                    break;
                }
        }

        e.stopPropagation();
    }

    handleKeyUp = (e: React.KeyboardEvent<any>) => {
        switch (e.keyCode) {
            case 40: // down arrow
            case 38: // up arrow
            case 16: // shift
            case 17: // ctrl
            case 18: // alt
                break;

            case 9: // tab
            case 13: // enter
                if (this.state.selectedIndex == undefined || !this.state.shown)
                    return;

                if (this.state.query != this.input.value)
                    return;

                this.select(e);
                break;

            case 27: // escape
                if (!this.state.shown)
                    return;
                this.setState({ shown: false });
                break;

            default:
                this.lookup();
        }
    }


    handleMenuClick = (e: React.MouseEvent<any>, index: number) => {
        e.preventDefault();
        e.persist();
        this.setState({
            selectedIndex: index
        }, () => {
            if (this.select(e))
                this.input.focus()
        });
    }

    mouseover = true;
    handleElementMouseEnter = (event: React.MouseEvent<any>, index: number) => {
        this.mouseover = true;
        this.setState({
            selectedIndex: index
        });
    }

    handleElementMouseLeave = (event: React.MouseEvent<any>, index: number) => {
        this.mouseover = false;
        this.setState({ selectedIndex: undefined });
        if (!this.focused && this.state.shown)
            this.setState({ shown: false });
    }

    input!: HTMLInputElement;

    //handlePopupLoaded = (elem: HTMLElement | null) => {
    //    if (!this.input)
    //        return;

    //    const rec = this.input.getBoundingClientRect();
    //    if (elem) {
    //        elem.style.top = (rec.height) + "px";

    //        if (getComputedStyle(elem).direction == "rtl")
    //            elem.style.right = "0px";
    //        else
    //            elem.style.left = "0px";

    //        elem.style.display = "table";
    //    }
    //}

    handleOnChange = () => {
        if (this.props.onChange)
            this.props.onChange(this.input.value);
    }

    render() {

        return (
            <Manager>
                <Target component="span" {...this.props.spanAttrs} className={classes(this.props.spanAttrs && this.props.spanAttrs.className, "sf-typeahead")}>
                    <input type="text" autoComplete="off" ref={inp => this.input = inp!} {...this.props.inputAttrs}
                        value={this.props.value}
                        onFocus={this.handleFocus}
                        onBlur={this.handleBlur}
                        onKeyUp={this.handleKeyUp}
                        onKeyDown={this.handleKeyDown}
                        onChange={this.handleOnChange}
                    />
                </Target>
                {this.state.shown && <Popper placement="bottom-start" style={{zIndex: 1000}}>{this.props.renderList ? this.props.renderList(this) : this.renderDefaultList()}</Popper>}
            </Manager>

        );
    }


    toggleEvents(isOpen: boolean | undefined) {
        if (isOpen) {
            document.addEventListener('click', this.handleDocumentClick, true);
            document.addEventListener('touchstart', this.handleDocumentClick, true);
        } else {
            document.removeEventListener('click', this.handleDocumentClick, true);
            document.removeEventListener('touchstart', this.handleDocumentClick, true);
        }
    }

    componentDidMount() {
        this.toggleEvents(this.state.shown);
    }

    componentWillUpdate(nextProps: TypeaheadProps, nextState: TypeaheadState) {
        if (nextState.shown != this.state.shown)
            this.toggleEvents(nextState.shown);
    }

    handleDocumentClick = (e: MouseEvent | TouchEvent) => {
        console.log(e);
        if (e.which === 3)
            return;

        const container = ReactDOM.findDOMNode(this);
        if (container.contains(e.target as Node) &&
            container !== e.target) {
            return;
        }

        this.setState({ shown: false });
    }

    renderDefaultList() {
        return (<ul className="typeahead dropdown-menu show">
            {
                !this.state.items!.length ? <li className="no-results"><a><small>{this.props.noResultsMessage}</small></a></li> :
                    this.state.items!.map((item, i) => <li key={i} className={classes("dropdown-item", i == this.state.selectedIndex ? "active" : undefined)}
                        onMouseEnter={e => this.handleElementMouseEnter(e, i)}
                        onMouseLeave={e => this.handleElementMouseLeave(e, i)}
                        {...this.props.liAttrs && this.props.liAttrs(item) }>
                        <a className="sf-pointer" onMouseUp={e => this.handleMenuClick(e, i)}>{this.props.renderItem!(item, this.state.query!)}</a>
                    </li>)
            }
        </ul>);
    }
}