import * as React from 'react'
import * as ReactDOM from 'react-dom'
import { classes } from '../Globals'
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
  renderList?: (typeahead: TypeaheadHandle) => React.ReactNode;
  renderItem?: (item: unknown, query: string) => React.ReactNode;
  onSelect?: (item: unknown, e: React.KeyboardEvent<any> | React.MouseEvent<any>) => string | null;
  scrollHeight?: number;
  inputAttrs?: React.InputHTMLAttributes<HTMLInputElement>;
  itemAttrs?: (item: unknown) => React.LiHTMLAttributes<HTMLButtonElement>;
  noResultsMessage?: string;
  renderInput?: (input: React.ReactElement<any>) => React.ReactElement<any>
}

export interface TypeaheadState {
  shown?: boolean;
  items?: any[];
  query?: string;
  selectedIndex?: number;
}

export interface TypeaheadHandle {
  items: any[] | undefined; 
  selectedIndex: number | undefined; 
  blur(): void; 
  writeInInput(query: string) : void;
}

export const Typeahead = React.forwardRef(function Typeahead(p: TypeaheadProps, ref: React.Ref<TypeaheadHandle>) {
  const [query, setQuery] = React.useState<string | undefined>(undefined);
  const [shown, setShown] = React.useState<boolean>(false);
  const [items, setItem] = React.useState<any[] | undefined>(undefined);
  const [selectedIndex, setSelectedIndex] = useStateWithPromise<number | undefined>(undefined);

  const rtl = React.useMemo(() => document.body.classList.contains("rtl"), []);

  const handle = React.useRef<number | undefined>(undefined);
  const inputRef = React.useRef<HTMLInputElement>(null);

  const focused = React.useRef<boolean>(false);
  const container = React.useRef<HTMLDivElement>(null);

  React.useImperativeHandle(ref, () => {
    return ({
      items,
      selectedIndex,
      blur: blur,
      writeInInput: writeInInput
    } as TypeaheadHandle);
  }, [items, selectedIndex]);


  React.useEffect(() => {
    return () => {
      if (handle.current != undefined)
        clearTimeout(handle.current);
    }
  }, []);


  function lookup() {
    if (!p.getItemsDelay) {
      populate();
    }
    else {
      if (handle.current != undefined)
        clearTimeout(handle.current);

      handle.current = setTimeout(() => populate(), p.getItemsDelay);
    }
  }



  function populate() {

    if (p.minLength == null || inputRef.current!.value.length < p.minLength) {
      setShown(false);
      setItem(undefined);
      setSelectedIndex(undefined);
      return;
    }

    //this.setState({ shown: true, items: undefined });

    const query = TypeaheadOptions.normalizeString(inputRef.current!.value);
    p.getItems(query).then(items => {
      setItem(items);
      setShown(true);
      setQuery(query);
      setSelectedIndex(0).done();
    }).done();
  }



  function select(e: React.KeyboardEvent<any> | React.MouseEvent<any>): boolean {
    if (items!.length == 0)
      return false;

    const val = p.onSelect!(items![selectedIndex || 0], e);

    inputRef.current!.value = val || "";
    if (p.onChange)
      p.onChange(inputRef.current!.value);

    setShown(false);
    return val != null;
  }

  //public
  function writeInInput(query: string) {
    inputRef.current!.value = query;
    inputRef.current!.focus();
    lookup();
  }

  function handleFocus() {
    if (!focused.current) {
      focused.current = true;
      if (p.minLength == 0 && !inputRef.current!.value)
        lookup();
    }
  }

  function handleBlur() {
    focused.current = false;

    if (p.onBlur)
      p.onBlur();
  }


  function blur() {
    inputRef.current!.blur();
  }

  function handleKeyDown(e: React.KeyboardEvent<any>) {
    debugger;
    if (!shown)
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
          const newIndex = ((selectedIndex || 0) - 1 + items!.length) % items!.length;
          setSelectedIndex(newIndex).done();
          break;
        }
      case 40: // down arrow
        {
          e.preventDefault();
          const newIndex = ((selectedIndex || 0) + 1) % items!.length;
          setSelectedIndex(newIndex).done();
          break;
        }
    }

    e.stopPropagation();
  }

  function handleKeyUp(e: React.KeyboardEvent<any>) {
    switch (e.keyCode) {
      case 40: // down arrow
      case 38: // up arrow
      case 16: // shift
      case 17: // ctrl
      case 18: // alt
        break;

      case 9: // tab
      case 13: // enter
        if (selectedIndex == undefined || !shown)
          return;

        if (query != inputRef.current!.value)
          return;

        select(e);
        break;

      case 27: // escape
        if (!shown)
          return;
        setShown(false);
        break;

      default:
        lookup();
    }
  }


  function handleMenuMouseUp(e: React.MouseEvent<any>, index: number) {
    e.preventDefault();
    e.persist();
    setSelectedIndex(index).then(() => {
      if (select(e))
        inputRef.current!.focus()
    }).done();
  }

  function handleElementMouseEnter(event: React.MouseEvent<any>, index: number) {
    setSelectedIndex(index);
  }

  function handleElementMouseLeave(event: React.MouseEvent<any>, index: number) {
    setSelectedIndex(undefined);
    if (!focused.current && shown)
      setShown(false);
  }

  function handleOnChange() {
    if (p.onChange)
      p.onChange(inputRef.current!.value);
  }

  const input =
    <input ref={inputRef} type="text" autoComplete="asdfsdf" {...p.inputAttrs}
      value={p.value}
      onFocus={handleFocus}
      onBlur={handleBlur}
      onKeyUp={handleKeyUp}
      onKeyDown={handleKeyDown}
      onChange={handleOnChange}
    />;


  return (
    <>
      {p.renderInput ? p.renderInput(input) : input}
      <Dropdown show={shown} onToggle={(isOpen: boolean) => setShown(isOpen)}>
        <Dropdown.Toggle id="dropdown" as={CustomToggle}></Dropdown.Toggle>
        {(p.renderList ? p.renderList({ blur, items, selectedIndex, writeInInput } as TypeaheadHandle) : renderDefaultList())}
      </Dropdown>
    </>
  );

  function renderDefaultList() {
    return (
      <Dropdown.Menu alignRight={rtl}>
        {
          !items ? null :
            items.length == 0 ? <button className="no-results dropdown-item"><small>{p.noResultsMessage}</small></button> :
              items!.map((item, i) => <button key={i}
                className={classes("dropdown-item", i == selectedIndex ? "active" : undefined)}
                onMouseEnter={e => handleElementMouseEnter(e, i)}
                onMouseLeave={e => handleElementMouseLeave(e, i)}
                onMouseUp={e => handleMenuMouseUp(e, i)}
                {...p.itemAttrs && p.itemAttrs(item)}>
                {p.renderItem!(item, query!)}
              </button>)
        }
      </Dropdown.Menu>
    );
  }
});

interface CustomToggleProps {
  onClick?: (e: React.MouseEvent<any>) => void;
}

class CustomToggle extends React.Component<CustomToggleProps> {
  constructor(props: CustomToggleProps, context: any) {
    super(props, context);
  }

  handleClick = (e: React.MouseEvent<any>) => {
    e.preventDefault();
    this.props.onClick!(e);
  }

  render() {
    return (
      <a href="" onClick={this.handleClick}>
        {this.props.children}
      </a>
    );
  }
}


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

    return [
      val.substr(0, index),
      <strong key={0}>{val.substr(index, query.length)}</strong>,
      val.substr(index + query.length)
    ];
  }


  export function normalizeString(str: string): string {
    return str;
  }
}
