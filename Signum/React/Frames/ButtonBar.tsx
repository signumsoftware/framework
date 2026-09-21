import * as React from 'react'
import { classes, Dic } from '../Globals'
import { Navigator } from '../Navigator'
import { IRenderButtons, ButtonsContext, ButtonBarElement } from '../TypeContext'
import { namespace } from 'd3';
import { FunctionalAdapter } from '../Modals';
import { AutoFocus } from '../Components/AutoFocus';
import { SearchMessage } from '../Signum.Entities';

export interface ButtonBarProps extends ButtonsContext {
  ref?: React.Ref<ButtonBarHandle>;
  align?: "left" | "right";
}

export interface ButtonBarHandle {
  handleKeyDown(e: KeyboardEvent): void;
}

export function ButtonBar(p: ButtonBarProps): React.JSX.Element {

  const qualifiedOperations = React.useMemo(() => {
    if (!p.operations)
      return undefined;

    const currents = Dic.getKeys(p.pack.canExecute);
    const qos = p.operations.split("~").filter(o => currents.some(c => c.toLowerCase().endsWith(`.${o.toLowerCase()}`)));
    if (qos.length == 0)
      return undefined;

    return qos.join("~");
  }, [p.operations]);

  const [currentFilter, setCurrentFilter] = React.useState<string | undefined>(() => {
    if (qualifiedOperations)
      return undefined;

    if (p.operations)
      return p.operations;

    return p.filter;
  });

  const [text, setText] = React.useState<string | undefined>(currentFilter);

  const ctx: ButtonsContext = { ...p, operations: qualifiedOperations, filter: currentFilter };
  const rb = FunctionalAdapter.innerRef(ctx.frame.entityComponent) as IRenderButtons | null;

  const es = Navigator.getSettings(p.pack.entity.Type);

  const buttons = ButtonBarManager.onButtonBarRender.flatMap(func => func(ctx) ?? [])
      .concat(rb?.renderButtons ? rb.renderButtons(ctx) : [])
      .concat(es?.extraToolbarButtons ? es.extraToolbarButtons(ctx) : [])
      .filter(a => a != null)
      .orderBy(a => a!.order ?? 0);

  var shortcuts = buttons.filter(a => a!.shortcut != null).map(a => a!.shortcut!);

  function handleKeyDown(e: KeyboardEvent) {
    var s = shortcuts;
    if (s != null) {
      for (var i = 0; i < s.length; i++) {
        if (s[i](e)) {
          e.preventDefault();
          return;
        }
      }
    }
  }

  React.useImperativeHandle(p.ref, () => ({
    handleKeyDown
  }));

  return (
    <div className={classes("btn-toolbar", "sf-button-bar", p.align == "right" ? "justify-content-end" : undefined)}>
      {!qualifiedOperations && ButtonBarManager.showSearch(p.pack.entity.Type, Dic.getKeys(p.pack.canExecute)) && renderSearch()}
      {buttons.map(a => a!.button)}
    </div>
  );

  function renderSearch() {
    return (
      <div className="btn-toolbar-search mb-2 w-100">
        <AutoFocus>
          <label className="label-xs d-inline-flex align-items-center gap-2">
            <span>{SearchMessage.Search.niceToString()}</span>
            <input type="text" className="form-control form-control-xs" value={text} onChange={e => {
              setText(e.currentTarget.value);

              if (e.currentTarget.value.length >= ButtonBarManager.minCharsToSearch(p.pack.entity.Type, Dic.getKeys(p.pack.canExecute)))
                setCurrentFilter(e.currentTarget.value);
              else
                setCurrentFilter(undefined);
            }} />
          </label>
        </AutoFocus>
      </div>
    );
  }
}

export namespace ButtonBarManager {

  export const onButtonBarRender = [] as ((c: ButtonsContext) => Array<ButtonBarElement | undefined> | undefined)[];

  export function clearButtonBarRenderer(): void{
    onButtonBarRender.clear();
  }

  export function showSearch(type: string, operations: string[]): boolean { return false };
  export function minCharsToSearch(type: string, operations: string[]): number { return 3 };
}
