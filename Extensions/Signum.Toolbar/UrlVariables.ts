export const urlVariables: { [name: string]: () => string } = {};

export function registerUrlVariable(name: string, getValue: () => string): void {
  urlVariables[name] = getValue;
}
