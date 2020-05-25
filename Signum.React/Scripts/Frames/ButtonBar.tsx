import * as React from 'react'
import { classes } from '../Globals'
import { IRenderButtons, ButtonsContext, ButtonBarElement } from '../TypeContext'
import { namespace } from 'd3';
import { FunctionalAdapter } from '../Modals';

export interface ButtonBarProps extends ButtonsContext {
  align?: "left" | "right";
}

export interface ButtonBarHandle {
  handleKeyDown(e: KeyboardEvent): void;
}


export const ButtonBar = React.forwardRef(function ButtonBar(p: ButtonBarProps, ref: React.Ref<ButtonBarHandle>) {

  const ctx: ButtonsContext = p;
  const rb = FunctionalAdapter.innerRef(ctx.frame.entityComponent) as IRenderButtons | null;

  const buttons = ButtonBarManager.onButtonBarRender.flatMap(func => func(p) ?? [])
    .concat(rb?.renderButtons ? rb.renderButtons(ctx) : [])
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
  React.useImperativeHandle(ref, () => ({
    handleKeyDown
  }));

  return React.cloneElement(<div className={classes("btn-toolbar", "sf-button-bar", p.align == "right" ? "justify-content-end" : undefined)} />,
    undefined,
    ...buttons.map(a => a!.button)
  );
});

export namespace ButtonBarManager {

  export const onButtonBarRender = [] as ((c: ButtonsContext) => Array<ButtonBarElement | undefined> | undefined)[];

  export function clearButtonBarRenderer(){
    onButtonBarRender.clear();
  }
}
