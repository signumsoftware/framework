import * as React from 'react';
import { ValueBaseController, ValueBaseProps } from './ValueBase'

export interface TextBaseProps<V = any> extends ValueBaseProps<V> {
  autoTrimString?: boolean;
  autoFixString?: boolean;
  triggerChange?: "onBlur";
}

export class TextBaseController<T extends TextBaseProps<V>, V> extends ValueBaseController<T, V> {

  tempValueRef!: React.RefObject<V | null>;
  override init(p: T): void {
    super.init(p);
    this.tempValueRef = React.useRef<V>(null);
  }

  setTempValue(value: V): void {
    (this.tempValueRef as React.RefObject<V>).current = value;
    this.forceUpdate();
  }

  getValue(): V {
    return this.tempValueRef.current ?? this.props.ctx.value;
  }
}

