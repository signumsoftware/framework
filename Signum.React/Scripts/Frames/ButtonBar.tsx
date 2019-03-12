import * as React from 'react'
import { classes } from '../Globals'
import { IRenderButtons, ButtonsContext, ButtonBarElement } from '../TypeContext'

export interface ButtonBarProps extends ButtonsContext {
  align?: "left" | "right";
}

export default class ButtonBar extends React.Component<ButtonBarProps>{
  static clearButtonBarRenderer() {
    ButtonBar.onButtonBarRender.clear();
  }

  static onButtonBarRender: Array<(ctx: ButtonsContext) => Array<ButtonBarElement | undefined> | undefined> = [];

  container!: HTMLDivElement;

  componentDidMount() {
    this.container = (this.props.frame.frameComponent as any).getMainDiv()!;
    this.container.addEventListener("keydown", this.hanldleKeyDown);
  }

  componentWillUnmount() {
    this.container.removeEventListener("keydown", this.hanldleKeyDown);
  }

  hanldleKeyDown = (e: KeyboardEvent) => {

    var s = this.shortcuts;
    if (s != null) {
      for (var i = 0; i < s.length; i++) {
        if (s[i](e)) {
          debugger;
          e.cancelBubble = true;
          if (e.stopPropagation) e.stopPropagation();
          e.preventDefault();
          return;
        }
      }
    }
  }

  shortcuts: ((e: KeyboardEvent) => boolean)[] = [];

  render() {
    const ctx: ButtonsContext = this.props;
    const rb = ctx.frame.entityComponent as any as IRenderButtons;

    const buttons = ButtonBar.onButtonBarRender.flatMap(func => func(this.props) || [])
      .concat(rb && rb.renderButtons ? rb.renderButtons(ctx) : [])
      .filter(a => a != null)
      .orderBy(a => a!.order || 0);

    this.shortcuts = buttons.filter(a => a!.shortcut != null).map(a => a!.shortcut!);
    
    return React.cloneElement(<div className={classes("btn-toolbar", "sf-button-bar", this.props.align == "right" ? "justify-content-end" : undefined)} />,
      undefined,
      ...buttons.map(a => a!.button)
    );
  }
}

