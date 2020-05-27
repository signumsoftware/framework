import * as React from 'react'
import * as ReactDOM from 'react-dom'
import { classes, softCast } from '../Globals'
import { Dropdown } from 'react-bootstrap';
import DropdownMenu from 'react-bootstrap/DropdownMenu';
import { useStateWithPromise } from '../Hooks';

export interface TypeaheadProps {
  value?: string;
  onChange?: (newValue: string) => void;
  onBlur?: () => void;
  getItems: (query: string) => Promise<unknown[]>;
  getItemsDelay?: number;
  minLength?: number;
  renderList?: (typeahead: TypeaheadController) => React.ReactNode;
  renderItem?: (item: unknown, query: string) => React.ReactNode;
  onSelect?: (item: unknown, e: React.KeyboardEvent<any> | React.MouseEvent<any>) => string | null;
  scrollHeight?: number;
  inputAttrs?: React.InputHTMLAttributes<HTMLInputElement>;
  itemAttrs?: (item: unknown) => React.LiHTMLAttributes<HTMLButtonElement>;
  noResultsMessage?: string;
  renderInput?: (input: React.ReactElement<any>) => React.ReactElement<any>
}

export class TypeaheadController {

  query!: string | undefined;
  setQuery!: (v: string | undefined) => void;

  shown!: boolean;
  setShown!: (v: boolean) => void;

  items!: any[] | undefined;
  setItem!: (v: any[] | undefined) => void;

  selectedIndex!: number | undefined;
  setSelectedIndex!: (v: number | undefined) => Promise<number | undefined>;

  timeoutHandle: number | undefined;
  rtl!: boolean;
  input: HTMLInputElement | undefined | null;
  focused!: boolean;
  props!: TypeaheadProps;

  constructor() {
    this.focused = false;
    this.rtl = document.body.classList.contains("rtl");
  }

  init(props: TypeaheadProps) {
    [this.query, this.setQuery] = React.useState<string | undefined>(undefined);
    [this.shown, this.setShown] = React.useState<boolean>(false);
    [this.items, this.setItem] = React.useState<any[] | undefined>(undefined);
    [this.selectedIndex, this.setSelectedIndex] = useStateWithPromise<number | undefined>(undefined);

    React.useEffect(() => {
      return () => {
        if (this.timeoutHandle != undefined)
          clearTimeout(this.timeoutHandle);
      };
    }, []);

    this.props = props;
  }

  setInput = (input: HTMLInputElement | null | undefined) => {
    this.input = input
  }

  lookup = () => {
    if (!this.props.getItemsDelay) {
      this.populate();
    }
    else {
      if (this.timeoutHandle != undefined)
        clearTimeout(this.timeoutHandle);

      this.timeoutHandle = setTimeout(() => this.populate(), this.props.getItemsDelay);
    }
  }

  populate = () => {

    if (this.props.minLength == null || this.input!.value.length < this.props.minLength) {
      this.setShown(false);
      this.setItem(undefined);
      this.setSelectedIndex(undefined);
      return;
    }

    //this.setState({ shown: true, items: undefined });

    const query = TypeaheadOptions.normalizeString(this.input!.value);
    this.props.getItems(query).then(items => {
      this.setItem(items);
      this.setShown(true);
      this.setQuery(query);
      this.setSelectedIndex(0).done();
    }).done();
  }

  select = (e: React.KeyboardEvent<any> | React.MouseEvent<any>) => {
    if (this.items!.length == 0)
      return false;

    const val = this.props.onSelect!(this.items![this.selectedIndex ?? 0], e);

    this.input!.value = val ?? "";
    if (this.props.onChange)
      this.props.onChange(this.input!.value);

    this.setShown(false);
    return val != null;
  }

  //public
  writeInInput = (query: string) => {
    this.input!.value = query;
    this.input!.focus();
    this.lookup();
  }

  handleFocus = () => {
    if (!this.focused) {
      this.focused = true;
      if (this.props.minLength == 0 && !this.input!.value)
        this.lookup();
    }
  }

  handleBlur = () => {
    this.focused = false;

    if (this.props.onBlur)
      this.props.onBlur();
  }


  blur = () => {
    this.input!.blur();
  }

  handleKeyDown = (e: React.KeyboardEvent<any>) => {
    if (!this.shown)
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
          const newIndex = ((this.selectedIndex ?? 0) - 1 + this.items!.length) % this.items!.length;
          this.setSelectedIndex(newIndex).done();
          break;
        }
      case 40: // down arrow
        {
          e.preventDefault();
          const newIndex = ((this.selectedIndex ?? 0) + 1) % this.items!.length;
          this.setSelectedIndex(newIndex).done();
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
        if (this.selectedIndex == undefined || !this.shown)
          return;

        if (this.query != this.input!.value)
          return;

        this.select(e);
        break;

      case 27: // escape
        if (!this.shown)
          return;
        this.setShown(false);
        break;

      default:
        this.lookup();
    }
  }


  handleMenuMouseUp = (e: React.MouseEvent<any>, index: number) => {
    e.preventDefault();
    e.persist();
    this.setSelectedIndex(index).then(() => {
      if (this.select(e))
        this.input!.focus()
    }).done();
  }

  handleElementMouseEnter = (event: React.MouseEvent<any>, index: number) => {
    this.setSelectedIndex(index);
  }

  handleElementMouseLeave = (event: React.MouseEvent<any>, index: number) => {
    this.setSelectedIndex(undefined);
    if (!this.focused && this.shown)
      this.setShown(false);
  }

  handleOnChange = () => {
    if (this.props.onChange)
      this.props.onChange(this.input!.value);
  }
}


export const Typeahead = React.forwardRef(function Typeahead(p: TypeaheadProps, ref: React.Ref<TypeaheadController>) {

  const controller = React.useMemo(() => new TypeaheadController(), []);
  controller.init(p);
  React.useImperativeHandle(ref, () => controller, []);

  const input =
    <input ref={controller.setInput} type="text" autoComplete="asdfsdf" {...p.inputAttrs}
      value={p.value}
      onFocus={controller.handleFocus}
      onBlur={controller.handleBlur}
      onKeyUp={controller.handleKeyUp}
      onKeyDown={controller.handleKeyDown}
      onChange={controller.handleOnChange}
    />;

  return (
    <>
      {p.renderInput ? p.renderInput(input) : input}
      <Dropdown show={controller.shown} onToggle={(isOpen: boolean) => controller.setShown(isOpen)}>
        <Dropdown.Toggle id="dropdown" as={CustomToggle}></Dropdown.Toggle>
        {(p.renderList ? p.renderList(controller) : renderDefaultList())}
      </Dropdown>
    </>
  );

  function renderDefaultList() {
    var items = controller.items;
    return (
      <Dropdown.Menu alignRight={controller.rtl} className="typeahead">
        {
          !items ? null :
            items.length == 0 ? <button className="no-results dropdown-item"><small>{p.noResultsMessage}</small></button> :
              items!.map((item, i) => <button key={i}
                className={classes("dropdown-item", i == controller.selectedIndex ? "active" : undefined)}
                onMouseEnter={e => controller.handleElementMouseEnter(e, i)}
                onMouseLeave={e => controller.handleElementMouseLeave(e, i)}
                onMouseUp={e => controller.handleMenuMouseUp(e, i)}
                {...p.itemAttrs && p.itemAttrs(item)}>
                {p.renderItem!(item, controller.query!)}
              </button>)
        }
      </Dropdown.Menu>
    );
  }
});


const CustomToggle = React.forwardRef(function CustomToggle(p: { children?: React.ReactNode, onClick?: React.MouseEventHandler }, ref: React.Ref<HTMLAnchorElement>) {

  return (
    <a
      ref={ref}
      href=""
      onClick={e => { e.preventDefault(); p.onClick!(e); }}>
      {p.children}
    </a>
  );
});

Typeahead.defaultProps = {
  getItems: undefined as any,
  getItemsDelay: 200,
  minLength: 1,
  renderItem: (item, query) => TypeaheadOptions.highlightedText(item as string, query),
  onSelect: (elem, event) => (elem as string),
  scrollHeight: 0,

  noResultsMessage: " - No results -",
} as TypeaheadProps;


export namespace TypeaheadOptions {
  export function highlightedText(val: string, query?: string): React.ReactNode {

    if (query == undefined)
      return val;

    const index = val.toLowerCase().indexOf(query.toLowerCase());
    if (index == -1)
      return val;

    return (
      <>
        {val.substr(0, index)}
        <strong key={0}>{val.substr(index, query.length)}</strong>
        {val.substr(index + query.length)}
      </>
    );
  }

  export function highlightedTextAll(val: string, query?: string): React.ReactNode {
    if (query == undefined)
      return val;

    const parts = query.toLocaleLowerCase().split(" ").filter(a => a.length > 0).orderByDescending(a => a.length);

    function splitText(str: string, partIndex: number): React.ReactNode {

      if (str.length == 0)
        return str;

      if (parts.length <= partIndex)
        return str;

      var part = parts[partIndex];

      const index = str.toLowerCase().indexOf(part);
      if (index == -1)
        return splitText(str, partIndex + 1);

      return (
        <>
          {splitText(str.substr(0, index), index + 1)}
          <strong key={0}>{str.substr(index, part.length)}</strong>
          {splitText(str.substr(index + part.length), index + 1)}
        </>
      );
    }

    return splitText(val, 0);
  }

  export function normalizeString(str: string): string {
    return str;
  }
}
