
import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes, Dic } from '@framework/Globals'
import { FormGroup } from '@framework/Lines'
import { Typeahead } from '@framework/Components'
import { TypeContext } from '@framework/TypeContext'
import { namedColors } from '../Color'
import { useForceUpdate } from '@framework/Hooks'
import { TypeaheadOptions } from '@framework/Components/Typeahead'


export function ColorTypeaheadLine(p : { ctx: TypeContext<string | null | undefined>; onChange?: () => void }){

  const forceUpdate = useForceUpdate();

  function handleOnChange(newColor: string | undefined | null) {
    p.ctx.value = newColor;
    if (p.onChange)
      p.onChange();
    forceUpdate();

  }

  var ctx = p.ctx;

  return (
    <FormGroup ctx={ctx} labelText={ctx.niceName()} >
      <ColorTypeahead color={ctx.value}
        formControlClass={ctx.formControlClass}
        onChange={handleOnChange} />
    </FormGroup>
  );
}

interface ColorTypeaheadProps {
  color: string | null | undefined;
  onChange: (newColor: string | null | undefined) => void;
  formControlClass: string | undefined;
}

export function ColorTypeahead(p : ColorTypeaheadProps){
  const forceUpdate = useForceUpdate();
  function handleGetItems(query: string) {
    if (!query)
      return Promise.resolve([
        "black",
        "#00000"
      ]);

    const result = Dic.getKeys(namedColors)
      .filter(k => k.toLowerCase().contains(query.toLowerCase()))
      .orderBy(a => a.length)
      .filter((k, i) => i < 5);

    if (result.length == 0) {
      if (query.match(/^(#[0-9A-F]{3}|#[0-9A-F]{6}|#[0-9A-F]{8})$/i))
        result.push(query);
    }

    return Promise.resolve(result);
  }

  function handleSelect(item: unknown | string) {
    p.onChange(item as string);
    forceUpdate();
    return item as string;
  }

  function handleRenderItem(item: unknown, query: string) {
    return (
      <span>
        <FontAwesomeIcon icon="square" className="icon" color={item as string} />
        {TypeaheadOptions.highlightedText(item as string, query)}
      </span>
    );
  }

  return (
    <Typeahead
      value={p.color ?? ""}
      inputAttrs={{ className: classes(p.formControlClass, "sf-entity-autocomplete") }}
      getItems={handleGetItems}
      onSelect={handleSelect}
      onChange={handleSelect}
      renderItem={handleRenderItem}
      minLength={0}
    />
  );
}



