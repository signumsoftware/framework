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

export const Typeahead = React.forwardRef((p: TypeaheadProps, ref: React.Ref<TypeaheadHandle>) => {
  const [query, setQuery] = React.useState<string | undefined>(undefined);
  const [shown, setShown] = React.useState<boolean>(false);
  const [items, setItem] = React.useState<any[] | undefined>(undefined);
  const [selectedIndex, setSelectedIndex] = useStateWithPromise<number | undefined>(undefined);

  const rtl = React.useMemo(() => document.body.classList.contains("rtl"), []);

  const handle = React.useRef<number | undefined>(undefined);
  const inputRef = React.useRef<HTMLInputElement>(null);
  const input = inputRef.current!;

  const focused = React.useRef<boolean>(false);
  const container = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    if (shown) {
      document.addEventListener('click', handleDocumentClick, true);
      document.addEventListener('touchstart', handleDocumentClick, true);

      return () => {
        document.removeEventListener('click', handleDocumentClick, true);
        document.removeEventListener('touchstart', handleDocumentClick, true);
      };
    }
  }, [shown]);

  React.useImperativeHandle(ref, () => ({
    items,
    selectedIndex,
    blur: blur
  } as TypeaheadHandle), [items, selectedIndex]);


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

    if (p.minLength == null || input.value.length < p.minLength) {
      setShown(false);
      setItem(undefined);
      setSelectedIndex(undefined);
      return;
    }

    //this.setState({ shown: true, items: undefined });

    const query = TypeaheadOptions.normalizeString(input.value);
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

    input.value = val || "";
    if (p.onChange)
      p.onChange(input.value);

    setShown(false);
    return val != null;
  }

  //public
  function writeInInput(query: string) {
    input.value = query;
    input.focus();
    lookup();
  }

  function handleFocus() {
    if (!focused.current) {
      focused.current = true;
      if (p.minLength == 0 && !input.value)
        lookup();
    }
  }

  function handleBlur() {
    focused.current = false;

    if (p.onBlur)
      p.onBlur();
  }


  function blur() {
    input.blur();
  }

  function handleKeyDown(e: React.KeyboardEvent<any>) {
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

        if (query != input.value)
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
        input.focus()
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
      p.onChange(input.value);
  }


  return (
    <div ref={container}>
      <Dropdown show={shown} drop="down">
        <input ref={inputRef} type="text" autoComplete="asdfsdf" {...p.inputAttrs}
          value={p.value}
          onFocus={handleFocus}
          onBlur={handleBlur}
          onKeyUp={handleKeyUp}
          onKeyDown={handleKeyDown}
          onChange={handleOnChange}
        />
        {shown ? (p.renderList ? p.renderList({ blur, items, selectedIndex, writeInInput } as TypeaheadHandle) : renderDefaultList()) : null}
      </Dropdown>
    </div>
  );

  function handleDocumentClick(e: MouseEvent | TouchEvent) {
    if ((e as MouseEvent).which === 3)
      return;

    if (container.current!.contains(e.target as Node) && container.current !== e.target) {
      return;
    }

    setShown(false);
  }

  function renderDefaultList() {
    return (
      <div className={classes("typeahead dropdown-menu show", rtl && "dropdown-menu-right")} >
        {
          !items!.length ? <button className="no-results dropdown-item"><small>{p.noResultsMessage}</small></button> :
            items!.map((item, i) => <button key={i}
              className={classes("dropdown-item", i == selectedIndex ? "active" : undefined)}
              onMouseEnter={e => handleElementMouseEnter(e, i)}
              onMouseLeave={e => handleElementMouseLeave(e, i)}
              onMouseUp={e => handleMenuMouseUp(e, i)}
              {...p.itemAttrs && p.itemAttrs(item)}>
              {p.renderItem!(item, query!)}
            </button>)
        }
      </div>
    );
  }
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
