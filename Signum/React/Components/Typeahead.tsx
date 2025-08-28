import * as React from 'react'
import * as ReactDOM from 'react-dom'
import { classes, Dic, softCast } from '../Globals'
import { Dropdown } from 'react-bootstrap';
import DropdownMenu from 'react-bootstrap/DropdownMenu';
import { useStateWithPromise } from '../Hooks';

export interface TypeaheadProps {
  value?: string;
  onChange?: (newValue: string) => void;
  onBlur?: () => void;
  getItems: (query: string) => Promise<unknown[]>;
  itemsDelay?: number;
  minLength?: number;
  renderList?: (typeahead: TypeaheadController) => React.ReactNode;
  renderItem?: (item: unknown, highlighter: TextHighlighter) => React.ReactNode;
  isDisabled?: (item: unknown) => boolean;
  isHeader?: (item: unknown) => boolean;
  onSelect?: (item: unknown, e: React.KeyboardEvent<any> | React.MouseEvent<any>) => string | null;
  scrollHeight?: number;
  inputAttrs?: React.InputHTMLAttributes<HTMLInputElement>;
  itemAttrs?: (item: unknown) => React.LiHTMLAttributes<HTMLElement>;
  noResultsMessage?: string;
  renderInput?: (input: React.ReactElement) => React.ReactElement;
  inputId?: string;
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

  init(props: TypeaheadProps) : void {
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

    this.props = {
      itemsDelay: 200,
      minLength: 1,
      renderItem: (item, highlighter) => highlighter.highlight(item as string),
      onSelect: (elem, event) => (elem as string),
      scrollHeight: 0,
      noResultsMessage: " - No results -",
      ...Dic.simplify(props)
    };
  }

  setInput = (input: HTMLInputElement | null | undefined): void => {
    this.input = input
  }

  lookup = (): void => {
    if (!this.props.itemsDelay) {
      this.populate();
    }
    else {
      if (this.timeoutHandle != undefined)
        clearTimeout(this.timeoutHandle);

      this.timeoutHandle = window.setTimeout(() => this.populate(), this.props.itemsDelay);
    }
  }

  populate = (): void => {

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
      this.setSelectedIndex(0);
    });
  }

  select = (e: React.KeyboardEvent<any> | React.MouseEvent<any>): boolean => {
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
  writeInInput = (query: string): void => {
    this.input!.value = query;
    this.input!.focus();
    this.lookup();
  }

  handleFocus = (): void => {
    if (!this.focused) {
      this.focused = true;
      if (this.props.minLength == 0 && !this.input!.value)
        this.lookup();
    }
  }

  handleBlur = (): void => {
    this.focused = false;

    if (this.props.onBlur)
      this.props.onBlur();
  }


  blur = (): void => {
    this.input!.blur();
  }

  handleKeyDown = (e: React.KeyboardEvent<any>): void => {
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
          this.setSelectedIndex(newIndex);
          break;
        }
      case 40: // down arrow
        {
          e.preventDefault();
          const newIndex = ((this.selectedIndex ?? 0) + 1) % this.items!.length;
          this.setSelectedIndex(newIndex);
          break;
        }
    }

    e.stopPropagation();
  }

  handleKeyUp = (e: React.KeyboardEvent<any>): void => {
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


  handleMenuMouseUp = (e: React.MouseEvent<any>, index: number): void => {
    e.preventDefault();
    this.setSelectedIndex(index).then(() => {
      if (this.select(e))
        this.input!.focus()
    });
  }

  handleElementMouseEnter = (event: React.MouseEvent<any>, index: number): void => {
    this.setSelectedIndex(index);
  }

  handleElementMouseLeave = (event: React.MouseEvent<any>, index: number): void => {
    this.setSelectedIndex(undefined);
    if (!this.focused && this.shown)
      this.setShown(false);
  }

  handleOnChange = (): void => {
    if (this.props.onChange)
      this.props.onChange(this.input!.value);
  }
}


export const Typeahead: React.ForwardRefExoticComponent<TypeaheadProps & React.RefAttributes<TypeaheadController>> = React.forwardRef(function Typeahead(p: TypeaheadProps, ref: React.Ref<TypeaheadController>) {

  const controller = React.useMemo(() => new TypeaheadController(), []);
  controller.init(p);
  React.useImperativeHandle(ref, () => controller, []);

  const input =
    <input ref={controller.setInput} type="text" autoComplete="off" {...p.inputAttrs}
      id={p.inputId}
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
        {(p.renderList ? p.renderList(controller) : renderDefaultList())}
      </Dropdown>
    </>
  );

  function renderDefaultList() {
    var items = controller.items;

    var highlighter = TextHighlighter.fromString(controller.query);
    return (
      <Dropdown.Menu align={controller.rtl ? "end" : undefined} className="typeahead">
        {
          !items ? null :
            items.length == 0 ? <div className="no-results dropdown-item disabled"><small>{p.noResultsMessage}</small></div> :
              items!.map((item, i) =>
                p.isHeader?.(item) ? <div key={i} className="dropdown-header"  {...p.itemAttrs && p.itemAttrs(item)}>{p.renderItem!(item, highlighter)}</div> : 
                  p.isDisabled?.(item) ? <div key={i} className="dropdown-item disabled" {...p.itemAttrs && p.itemAttrs(item)}>{p.renderItem!(item, highlighter)}</div> :
                <button key={i}
                className={classes("dropdown-item", i == controller.selectedIndex ? "active" : undefined)}
                onMouseEnter={e => controller.handleElementMouseEnter(e, i)}
                onMouseLeave={e => controller.handleElementMouseLeave(e, i)}
                onMouseUp={e => controller.handleMenuMouseUp(e, i)}
                {...p.itemAttrs && p.itemAttrs(item)}>
                {p.renderItem!(item, highlighter)}
              </button>)
        }
      </Dropdown.Menu>
    );
  }
});



export namespace TypeaheadOptions {

  export function normalizeString(str: string): string {
    return str;
  }
}

export class TextHighlighter {
  query?: string;
  parts?: string[];
  regex?: RegExp;

  static fromString(query: string | undefined): TextHighlighter {
    var hl = new TextHighlighter(query?.split(" "));
    hl.query = query;
    return hl;
  }

  constructor(parts: string[] | undefined) {
    this.parts = parts?.filter(a => a != null && a.length > 0).orderByDescending(a => a.length);
    if (this.parts?.length)
      this.regex = new RegExp(this.parts.map(p => RegExp.escape(p)).join("|"), "gi");
  }

  highlight(text: string): React.ReactElement | string {
    if (!text || !this.regex)
      return text;

    var matches = Array.from(text.matchAll(this.regex));

    if (matches.length == 0)
      return text;

    var result = [];

    var pos = 0;
    for (var i = 0; i < matches.length; i++) {
      var m = matches[i];

      if (pos < m.index!) {
        result.push(text.substring(pos, m.index));
      }

      pos = m.index! + m[0].length;
      result.push(<strong>{text.substring(m.index!, pos)}</strong>);
    }

    if (pos < text.length)
      result.push(text.substring(pos));

    return React.createElement(React.Fragment, undefined, ...result);
  }

  highlightHtml(text: string): string {
    if (!text || !this.regex)
      return text;

    var matches = Array.from(text.matchAll(this.regex));

    if (matches.length == 0)
      return text;

    var result = [];

    var pos = 0;
    for (var i = 0; i < matches.length; i++) {
      var m = matches[i];

      if (pos < m.index!) {
        result.push(text.substring(pos, m.index));
      }

      pos = m.index! + m[0].length;
      result.push("<strong>");
      result.push(text.substring(m.index!, pos))
      result.push("</strong>");
    }

    if (pos < text.length)
      result.push(text.substring(pos));

    return result.join("");
  }
}
