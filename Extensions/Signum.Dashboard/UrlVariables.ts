export const urlVariables: { [name: string]: () => string } = {};

export function registerUrlVariable(name: string, getValue: () => string) {
  urlVariables[name] = getValue;
}
