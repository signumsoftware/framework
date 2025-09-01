
declare global {

  namespace JSX {
    // Fallback to React's JSX namespace
    interface Element extends React.JSX.Element { }
    interface IntrinsicElements extends React.JSX.IntrinsicElements { }
  }

  interface RegExpConstructor {
    escape(s: string): string;
  }

  interface Window {
    __allowNavigatorWithoutUser?: boolean;
    __baseName: string;
    dataForChildWindow?: any;
    dataForCurrentWindow?: any;
    exploreGraphDebugMode: boolean;
  }

  type Comparable = number | string | {valueOf: ()=> any };


  interface RegExpConstructor {
    escape(str: string): string;
  }
  

  interface Array<T> {
    groupBy<K extends string>(this: Array<T>, keySelector: (element: T) => K): { key: K; elements: T[] } [];
    groupBy<K>(this: Array<T>, keySelector: (element: T) => K, keyStringifier?: (key: K) => string): { key: K; elements: T[] }[];
    groupBy<K, E>(this: Array<T>, keySelector: (element: T) => K, keyStringifier: ((key: K) => string) | undefined, elementSelector: (element: T) => E): { key: K; elements: E[] }[];
    groupToObject(this: Array<T>, keySelector: (element: T) => string): { [key: string]: T[] }; // Remove by Object.groupBy when safary supports it https://caniuse.com/?search=Object.groupby
    groupToObject<E>(this: Array<T>, keySelector: (element: T) => string, elementSelector: (element: T) => E): { [key: string]: E[] };
    groupToMap<K>(this: Array<T>, keySelector: (element: T) => K): Map<K, T[]>; // Remove by MAp.groupBy when safary supports it https://caniuse.com/?search=Map.groupby
    groupToMap<K, E>(this: Array<T>, keySelector: (element: T) => K, elementSelector: (element: T) => E): Map<K, E[]>;
    groupWhen(this: Array<T>, condition: (element: T) => boolean, includeKeyInGroup?: boolean, beforeFirstKey?: "throw" | "skip" | "defaultGroup"): { key: T, elements: T[] }[];
    groupWhenChange<K>(this: Array<T>, keySelector: (element: T) => K, keyStringifier?: (key: K) => string): { key: K, elements: T[] }[];

    orderBy<V extends Comparable>(this: Array<T>, keySelector: (element: T) => V | null | undefined): T[];
    orderByDescending<V extends Comparable>(this: Array<T>, keySelector: (element: T) => V | null | undefined): T[];


    toObject(this: Array<T>, keySelector: (element: T) => string): { [key: string]: T };
    toObject<V>(this: Array<T>, keySelector: (element: T) => string, valueSelector: (element: T) => V): { [key: string]: V };
    toObjectDistinct(this: Array<T>, keySelector: (element: T) => string): { [key: string]: T };
    toObjectDistinct<V>(this: Array<T>, keySelector: (element: T) => string, valueSelector: (element: T) => V): { [key: string]: V };


    toMap<K>(this: Array<T>, keySelector: (element: T) => K): Map<K, T>;
    toMap<K, V>(this: Array<T>, keySelector: (element: T) => K, valueSelector: (element: T) => V):  Map<K, V>;
    toMapDistinct<K>(this: Array<T>, keySelector: (element: T) => K): Map<K, T>;
    toMapDistinct<K, V>(this: Array<T>, keySelector: (element: T) => V, valueSelector: (element: T) => V): Map<K, V>;

    distinctBy(this: Array<T>, keySelector?: (element: T) => unknown): T[];

    clear(this: Array<T>): void;
    groupsOf(this: Array<T>, groupSize: number, elementSize?: (item: T) => number): T[][];

    minBy<V>(this: Array<T>, keySelector: (element: T) => V): T | undefined;
    maxBy<V>(this: Array<T>, keySelector: (element: T) => V): T | undefined;
    max<V extends Comparable>(this: Array<V | null | undefined>): V | null;
    max<V extends Comparable>(this: Array<T>, selector: (element: T, index: number, array: T[]) => V | null | undefined): V | null;
    min<V extends Comparable>(this: Array<V | null | undefined>): V | null;
    min<V extends Comparable>(this: Array<T>, selector: (element: T, index: number, array: T[]) => V | null | undefined): V | null;
    
    sum(this: Array<number>): number;
    sum(this: Array<T>, selector: (element: T, index: number, array: T[]) => number): number;

    count(this: Array<T>, predicate: (element: T, index: number, array: T[]) => boolean): number;

    first(this: Array<T>, errorContext?: string): T;
    first<S extends T>(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => element is S): S;
    first(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => unknown): T;

    firstOrNull(this: Array<T>): T | null;
    firstOrNull<S extends T>(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => element is S): S | null;
    firstOrNull(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => unknown): T | null;

    last(this: Array<T>, errorContext?: string): T;
    last<S extends T>(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => element is S): T;
    last(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => unknown): T;

    lastOrNull(this: Array<T>): T | null;
    lastOrNull<S extends T>(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => element is S): S | null;
    lastOrNull(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => unknown): T | null;

    single(this: Array<T>, errorContext?: string): T;
    single<S extends T>(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => element is S): S;
    single(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => unknown): T;

    singleOrNull(this: Array<T>, errorContext?: string): T | null;
    singleOrNull<S extends T>(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => element is S): S | null;
    singleOrNull(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => unknown): T | null;

    onlyOrNull(this: Array<T>): T | null;
    onlyOrNull<S extends T>(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => element is S): S | null;
    onlyOrNull(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => unknown): T | null;

    contains(this: Array<T>, element: T): boolean;
    remove(this: Array<T>, element: T): boolean;
    removeAt(this: Array<T>, index: number): void;
    moveUp(this: Array<T>, index: number): number;
    moveDown(this: Array<T>, index: number): number;
    insertAt(this: Array<T>, index: number, element: T): void;

    notNull(this: Array<T>): NonNullable<T>[];
    clone(this: Array<T>, ): T[];
    joinComma(this: Array<T>, lastSeparator: string): string;
    extract(this: Array<T>, filter: (element: T) => boolean): T[];
    findIndex(this: Array<T>, filter: (element: T, index: number, obj: Array<T>) => boolean): number;
    findLastIndex(this: Array<T>, filter: (element: T) => boolean): number;
    toTree(this: Array<T>, getKey: (element: T) => string, getParentKey: (element: T) => string | null | undefined): TreeNode<T>[];
  }

  interface TreeNode<T> {
    value: T;
    children: TreeNode<T>[];
  }

  interface ArrayConstructor {

    range(min: number, maxNotIncluded: number): number[];
    toArray<T>(arrayish: { length: number;[index: number]: T }): T[];
    repeat<T>(count: number, value: T): T[];
  }

  interface String {
    contains(this: string, str: string): boolean;
    startsWith(this: string, str: string): boolean;
    endsWith(this: string, str: string): boolean;
    formatWith(this: string, ...parameters: any[]): string;
    splitByRegex(this: string, regex: RegExp): { isMatch: boolean, value: string }[];
    forGenderAndNumber(this: string, number: number): string;
    forGenderAndNumber(this: string, gender: string | undefined): string;
    forGenderAndNumber(this: string, gender: any, number?: number): string;
    indent(this: string, numChars: number): string;
    between(this: string, separator: string): string;
    between(this: string, firstSeparator: string, secondSeparator: string): string;
    tryBetween(this: string, separator: string): string | undefined;
    tryBetween(this: string, firstSeparator: string, secondSeparator: string): string | undefined;
    after(this: string, separator: string): string;
    before(this: string, separator: string): string;
    tryAfter(this: string, separator: string): string | undefined;
    tryBefore(this: string, separator: string): string | undefined;
    afterLast(this: string, separator: string): string;
    beforeLast(this: string, separator: string): string;
    tryAfterLast(this: string, separator: string): string | undefined;
    tryBeforeLast(this: string, separator: string): string | undefined;
    etc(this: string, maxLength: number, etcString?: string): string;

    firstUpper(this: string): string;
    firstLower(this: string, ): string;

    repeat(this: string, n: number): string;
  }

  interface RequestInit {
    abortSignal?: AbortSignal;
  }
}
Array.prototype.clear = function (): void {
  this.length = 0;
};

Array.prototype.groupBy = function (this: any[],
  keySelector: (element: any) => any,
  keyStringifier?: (element: any) => string,
  elementSelector?: (element: any) => unknown):
  { key: any /*string*/; elements: any[] }[] {

  const result: { key: any; elements: any[] }[] = [];
  const obj: { [key: string]: any[] } = {};
  keyStringifier ??= v => v.toString();

  for (const elem of this) {

    var key = keySelector(elem);
    var elem2 = elementSelector == null ? elem : elementSelector(elem);
    var gr = obj[keyStringifier(key)];
    if (gr) {
      gr.push(elem2);
    } else {
      var group = {
        key: key,
        elements: [elem2]
      };
      result.push(group);
      obj[keyStringifier(key)] = group.elements;
    }
  }
  return result;
};

Array.prototype.groupToObject = function (this: any[], keySelector: (element: any) => string | number, elementSelector?: (element: any) => unknown): { [key: string | number]: any[] } {
  const result: { [key: string | number]: any[] } = {};

  for (let i = 0; i < this.length; i++) {
    const element: any = this[i];
    const key = keySelector(element);
    if (!result[key])
      result[key] = [];
    result[key].push(elementSelector ? elementSelector(element) : element);
  }
  return result;
};


Array.prototype.groupToMap = function (this: any[], keySelector: (element: any) => string, elementSelector?: (element: any) => unknown): Map<any, any[]> {
  const result = new Map<any, any[]>();

  for (let i = 0; i < this.length; i++) {
    const element: any = this[i];
    const key = keySelector(element);
    if (!result.has(key))
      result.set(key, []);
    result.get(key)!.push(elementSelector ? elementSelector(element) : element);
  }
  return result;
};

Array.prototype.groupWhen = function (this: any[], isGroupKey: (element: any) => boolean, includeKeyInGroup = false, beforeFirstKey : "throw" | "skip" | "defaultGroup" = "throw"): { key: any, elements: any[] }[] {
  const result: { key: any, elements: any[] }[] = [];

  let group: { key: any, elements: any[] } | undefined = undefined;

  for (let i = 0; i < this.length; i++) {
    const item: any = this[i];
    if (isGroupKey(item)) {
      group = { key: item, elements: includeKeyInGroup ? [item] : [] };
      result.push(group);
    }
    else {
      if (group == undefined) {
        switch (beforeFirstKey) {
          case "throw": throw new Error("Parameter initialGroup is false");
          case "skip": break;
          case "defaultGroup": {
            group = { key: undefined, elements: [] };
            result.push(group);
            group!.elements.push(item);
          }
        }
      }
      else {
        group!.elements.push(item);
      }
    }
  }
  return result;
};

Array.prototype.groupWhenChange = function (this: any[], keySelector: (element: any) => any, keyStringifier?: (key: any) => string): { key: any, elements: any[] }[] {
  const result: { key: any, keyString: string, elements: any[] }[] = [];

  keyStringifier ??= a => a.toString();

  let current: { key: any, keyString: string, elements: any[] } | undefined = undefined;

  for (let i = 0; i < this.length; i++) {
    const item: any = this[i];
    const key = keySelector(item);
    const keyString = keyStringifier(key);
    if (current == undefined) {

      current = { key: key, keyString: keyString , elements: [item] };
    }
    else if (current.keyString == keyString) {
      current.elements.push(item);
    }
    else {
      result.push(current);
      current = { key: key, keyString: keyString, elements: [item] };
    }
  }

  if (current != undefined)
    result.push(current);

  return result;
};

Array.prototype.orderBy = function (this: any[], keySelector: (element: any) => any): any[] {
  const cloned = this.slice(0);
  cloned.sort((e1, e2) => {
    const v1 = keySelector(e1);
    const v2 = keySelector(e2);
    if (v1 > v2)
      return 1;
    if (v1 < v2)
      return -1;
    return 0;
  });
  return cloned;
};

Array.prototype.orderByDescending = function (this: any[], keySelector: (element: any) => any): any[] {
  const cloned = this.slice(0);
  cloned.sort((e1, e2) => {
    const v1 = keySelector(e1);
    const v2 = keySelector(e2);
    if (v1 < v2)
      return 1;
    if (v1 > v2)
      return -1;
    return 0;
  });
  return cloned;
};


Array.prototype.minBy = function (this: any[], keySelector: (element: any) => any): any {
  if (this.length == 0)
    return undefined;

  var min = keySelector(this[0]);
  var result = this[0];
  for (var i = 1; i < this.length; i++) {
    var val = keySelector(this[i]);
    if (val < min) {
      min = val;
      result = this[i];
    }
  }
  return result;
};

Array.prototype.maxBy = function (this: any[], keySelector: (element: any) => any): any {
  if (this.length == 0)
    return undefined;

  var max = keySelector(this[0]);
  var result = this[0];
  for (var i = 1; i < this.length; i++) {
    var val = keySelector(this[i]);
    if (val > max) {
      max = val;
      result = this[i];
    }
  }
  return result;
};

Array.prototype.toObject = function (this: any[], keySelector: (element: any) => any, valueSelector?: (element: any) => any): any {
  const obj: any = {};

  this.forEach(item => {
    const key = keySelector(item);

    if (obj[key])
      throw new Error("Repeated key {0}".formatWith(key));

    obj[key] = valueSelector ? valueSelector(item) : item;
  });

  return obj;
};

Array.prototype.toObjectDistinct = function (this: any[], keySelector: (element: any) => any, valueSelector?: (element: any) => any): any {
  const obj: any = {};

  this.forEach(item => {
    const key = keySelector(item);

    obj[key] = valueSelector ? valueSelector(item) : item;
  });

  return obj;
};

Array.prototype.toMap = function (this: any[], keySelector: (element: any) => any, valueSelector?: (element: any) => any): any {
  const map = new Map();

  this.forEach(item => {
    const key = keySelector(item);

    if (map.has(key))
      throw new Error("Repeated key {0}".formatWith(key));

    map.set(key,  valueSelector ? valueSelector(item) : item);
  });

  return map;
};

Array.prototype.toMapDistinct = function (this: any[], keySelector: (element: any) => any, valueSelector?: (element: any) => any): any {
  const map = new Map();

  this.forEach(item => {
    const key = keySelector(item);

    map.set(key, valueSelector ? valueSelector(item) : item);
  });

  return map;
};

Array.prototype.distinctBy = function (this: any[], keySelector: (element: any) => unknown): any[] {
  const keysFound = new Set<unknown>();

  keySelector ??= a => a.toString();

  const result: any[] = [];

  this.forEach(item => {
    const key = keySelector(item);

    if (!keysFound.has(key)) {
      result.push(item);
      keysFound.add(key);
    }
  });

  return result;
};

Array.prototype.groupsOf = function (this: any[], groupSize: number, elementSize?: (item: any) => number) {

  const result: any[][] = [];
  let newList: any[] = [];


  if (elementSize == null) {
    this.forEach(item => {
      newList.push(item);
      if (newList.length == groupSize) {
        result.push(newList);
        newList = [];
      }
    });
  }
  else {
    var accumSize = 0;
    this.forEach(item => {
      var size = elementSize(item);

      if ((accumSize + size) > groupSize && newList.length > 0) {
        result.push(newList);
        newList = [item];
        accumSize = size;
      }
      else {
        accumSize += size;
        newList.push(item);
      }
    });
  }

  if (newList.length != 0)
    result.push(newList);

  return result;
}

Array.prototype.max = function (this: any[], selector?: (element: any, index: number, array: any[]) => any) {

  var array: number[]= selector ?
    this.map(selector).filter(a => a != null) :
    this.filter(a => a != null);
  
  if (array.length == 0)
    return null;

  var max = array[0];
  for (var i = 1; i < array.length; i++) {
    var val = array[i];
    if (max < val) {
      max = val;
    }
  }
  return max;
};

Array.prototype.min = function (this: any[], selector?: (element: any, index: number, array: any[]) => any) {

  var array : number[] = selector ?
    this.map(selector).filter(a => a != null) :
    this.filter(a => a != null);

  if (array.length == 0)
    return null;

  var min = array[0];
  for (var i = 1; i < array.length; i++) {
    var val = array[i];
    if (val < min) {
      min = val;
    }
  }
  return min;
};

Array.prototype.sum = function (this: any[], selector?: (element: any, index: number, array: any[]) => any) {

  if (this.length == 0)
    return 0;

  var result = 0;
  if (selector) {
    for (var i = 0; i < this.length; i++) {
      result += selector(this[i], i, this) ?? 0;
    }
  } else {
    for (var i = 0; i < this.length; i++) {
      result +=  this[i];
    }
  }

  return result;
};

Array.prototype.count = function (this: any[], predicate: (element: any, index: number, array: any[]) => any) {

  if (this.length == 0)
    return 0;

  var result = 0;
  for (var i = 0; i < this.length; i++) {
    if (predicate(this[i], i, this))
      result++;
  }

  return result;
};


Array.prototype.first = function (this: any[], errorContextOrPredicate?: string | ((element: any, index: number, array: any[]) => unknown)) {

  var array = typeof errorContextOrPredicate == "function" ? this.filter(errorContextOrPredicate) : this;

  if (array.length == 0)
    throw new Error("No " + (typeof errorContextOrPredicate == "string" ? errorContextOrPredicate : "element") + " found");

  return array[0];
};


Array.prototype.firstOrNull = function (this: any[], predicate?: ((element: any, index: number, array: any[]) => unknown)) {

  var array = typeof predicate == "function" ? this.filter(predicate) : this;

  if (array.length == 0)
    return null;

  return array[0];
};

Array.prototype.last = function (this: any[], errorContextOrPredicate?: string | ((element: any, index: number, array: any[]) => unknown)) {

  var array = typeof errorContextOrPredicate == "function" ? this.filter(errorContextOrPredicate) : this;

  if (array.length == 0)
    throw new Error("No " + (typeof errorContextOrPredicate == "string" ? errorContextOrPredicate : "element") + " found");

  return array[array.length - 1];
};


Array.prototype.lastOrNull = function (this: any[], predicate?: ((element: any, index: number, array: any[]) => unknown)) {

  var array = typeof predicate == "function" ? this.filter(predicate) : this;

  if (array.length == 0)
    return null;

  return array[array.length - 1];
};

Array.prototype.single = function (this: any[], errorContextOrPredicate?: string | ((element: any, index: number, array: any[]) => unknown)) {

  var array = typeof errorContextOrPredicate == "function" ? this.filter(errorContextOrPredicate) : this;

  if (array.length == 0)
    throw new Error("No " + (typeof errorContextOrPredicate == "string" ? errorContextOrPredicate : "element") + " found");

  if (array.length > 1)
    throw new Error("More than one " + (typeof errorContextOrPredicate == "string" ? errorContextOrPredicate : "element") + " found");

  return array[0];
};

Array.prototype.singleOrNull = function (this: any[], errorContextOrPredicate?: string | ((element: any, index: number, array: any[]) => unknown)) {

  var array = typeof errorContextOrPredicate == "function" ? this.filter(errorContextOrPredicate) : this;

  if (array.length == 0)
    return null;

  if (array.length > 1)
    throw new Error("More than one " + (typeof errorContextOrPredicate == "string" ? errorContextOrPredicate : "element") + " found");

  return array[0];
};


Array.prototype.onlyOrNull = function (this: any[], predicate?: (element: any, index: number, array: any[]) => unknown) {

  var array = predicate ? this.filter(predicate) : this;

  if (array.length == 0)
    return null;

  if (array.length > 1)
    return null;

  return array[0];
};

Array.prototype.contains = function (this: any[], element: any) {
  return this.indexOf(element) !== -1;
};

if (!Array.prototype.includes) {
  Array.prototype.includes = function (this: any[], element: any, fromIndex?: number) {
    return this.indexOf(element, fromIndex) !== -1;
  };
}

Array.prototype.removeAt = function (this: any[], index: number) {
  this.splice(index, 1);
};

Array.prototype.moveUp = function (this: any[], index: number) {
  if (index == 0)
    return 0;

  const entity = this[index]
  this.removeAt(index);
  this.insertAt(index - 1, entity);
  return index - 1;
};

Array.prototype.moveDown = function (this: any[], index: number) {
  if (index == this.length - 1)
    return this.length - 1;

  const entity = this[index]
  this.removeAt(index);
  this.insertAt(index + 1, entity);
  return index + 1;
};

Array.prototype.remove = function (this: any[], element: any) {

  const index = this.indexOf(element);
  if (index == -1)
    return false;

  this.splice(index, 1);
  return true;
};

Array.prototype.insertAt = function (this: any[], index: number, element: any) {
  this.splice(index, 0, element);
};

Array.prototype.clone = function (this: any[]) {
  return this.slice(0);
};

Array.prototype.notNull = function (this: any[]) {
  return this.filter(a => a != null);
};

Array.prototype.joinComma = function (this: any[], lastSeparator: string) {
  const array = this as any[];

  if (array.length == 0)
    return "";

  if (array.length == 1)
    return array[0] == undefined ? "" : array[0].toString();

  const lastIndex = array.length - 1;

  const rest = array.slice(0, lastIndex).join(", ");

  return rest + lastSeparator + (array[lastIndex] == undefined ? "" : array[lastIndex].toString());
};

Array.prototype.extract = function (this: any[], predicate: (element: any) => boolean) {
  const result = this.filter(predicate);

  result.forEach(element => { this.remove(element) });

  return result;
};

if (!Array.prototype.find) {
  Array.prototype.find = function (this: any[], predicate: (element: any, index: number, array: Array<any>) => boolean, thisArg?: any) {
    for (var i = 0; i < this.length; i++) {
      if (predicate.call(thisArg, this[i], i, this)) {
        return this[i];
      }
    }
    return undefined;
  };
}

if (!Array.prototype.findIndex) {
  Array.prototype.findIndex = function (this: any[], predicate: (element: any, index: number, array: Array<any>) => boolean, thisArg?: any) {
    for (var i = 0; i < this.length; i++)
      if (predicate.call(thisArg, this[i], i, this))
        return i;

    return -1;
  };
}

if (!Array.prototype.findLastIndex) {
  Array.prototype.findLastIndex = function (this: any[], predicate: (element: any, index: number, array: Array<any>) => boolean, thisArg?: any) {
    for (var i = this.length - 1; i >= 0; i--)
      if (predicate.call(thisArg, this[i], i, this))
        return i;

    return -1;
  };
}

if (!Array.prototype.toTree) {
  Array.prototype.toTree = function toTree(this: any[], getKey: (element: any) => string, getParentKey: (element: any) => string | null | undefined) {

    var top: TreeNode<any> = { value: null, children: [] };

    var dic: { [key: string]: TreeNode<any> } = {};

    function createNode(item: any) {

      var key = getKey(item);
      if (dic[key])
        return dic[key];

      var itemNode: TreeNode<any> = { value: item, children: [] };

      var parentKey = getParentKey(item);
      var parent = parentKey ? dic[parentKey] : top;
      parent.children.push(itemNode);
      return dic[key] = itemNode;
    }

    this.forEach(n => createNode(n));

    return top.children;
  }
}

Array.range = function (min: number, maxNotIncluded: number) {
  const length = maxNotIncluded - min;

  const result = new Array(length);
  for (let i = 0; i < length; i++) {
    result[i] = min + i;
  }

  return result;
}

Array.repeat = function (count: number, val: any): any[] {

  const result = new Array(count);
  for (let i = 0; i < count; i++) {
    result[i] = val;
  }

  return result;
}

Array.toArray = function (arrayish: { length: number;[index: number]: any }): any[] {
  var result: any[] = [];
  for (var i = 0; i < arrayish.length; i++)
    result.push(arrayish[i]);
  return result;
}

if (!String.prototype.includes) {
  String.prototype.includes = function (this: string, str: string, start?: number) {
    return this.indexOf(str, start) !== -1;
  };
}

String.prototype.contains = function (this: string, str: string) {
  return this.indexOf(str) !== -1;
}

String.prototype.startsWith = function (this: string, str: string) {
  return this.indexOf(str) === 0;
}

String.prototype.endsWith = function (this: string, str: string) {
  const index = this.lastIndexOf(str);
  return index !== -1 && index === (this.length - str.length); //keep it
}

String.prototype.formatWith = function () {
  const regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

  const args: any = arguments;

  return (this as string).replace(regex, match => {
    //match will look like {sample-match}
    //key will be 'sample-match';
    const key = match.substr(1, match.length - 2);

    return args[key];
  });
};

String.prototype.splitByRegex = function splitByRegex(this: string, regex: RegExp): { isMatch: boolean, value: string }[] {
  const result: { isMatch: boolean, value: string }[] = [];
  let lastIndex = 0;

  // Iterate over matches
  for (const match of this.matchAll(regex)) {
    const matchStart = match.index;
    const matchEnd = matchStart + match[0].length;

    // Add the non-matching part before this match
    if (lastIndex < matchStart) {
      result.push({ isMatch: false, value: this.slice(lastIndex, matchStart) });
    }

    // Add the matching part
    result.push({ isMatch: true, value: match[0] });

    // Update lastIndex
    lastIndex = matchEnd;
  }

  // Add any remaining non-matching part at the end
  if (lastIndex < this.length) {
    result.push({ isMatch: false, value: this.slice(lastIndex) });
  }

  return result;
};

String.prototype.forGenderAndNumber = function (this: string, gender: any, number?: number) {
  
  if (!number && !isNaN(parseFloat(gender))) {
    number = gender;
    gender = undefined;
  }

  if ((gender == undefined || gender == "") && number == undefined)
    return this;

  function replacePart(textToReplace: string, ...prefixes: string[]): string {
    return textToReplace.replace(/\[[^\]\|]+(\|[^\]\|]+)*\]/g, m => {
      const captures = m.substring(1, m.length - 1).split("|");

      for (let i = 0; i < prefixes.length; i++) {
        const pr = prefixes[i];
        const capture = captures.filter(c => c.startsWith(pr)).firstOrNull();
        if (capture != undefined)
          return capture.substring(pr.length);
      }

      return "";
    });
  }


  if (number == undefined)
    return replacePart(this, gender + ":");

  if (gender == undefined) {
    if (number == 1)
      return replacePart(this, "1:");

    return replacePart(this, number + ":", ":", "");
  }

  if (number == 1)
    return replacePart(this, "1" + gender + ":", "1:");

  return replacePart(this, gender + number + ":", gender + ":", number + ":", ":");


};


export function isNumber(n: any): boolean {
  return !isNaN(parseFloat(n)) && isFinite(n);
}

export function softCast<T>(val: T): T {
  return val;
}

String.prototype.indent = function (this: string, numChars: number) {
  const indent = " ".repeat(numChars);
  return this.split("\n").map(a => indent + a).join("\n");
};

String.prototype.between = function (this: string, firstSeparator: string, secondSeparator?: string) {

  if (!secondSeparator)
    secondSeparator = firstSeparator;

  const index = this.indexOf(firstSeparator);
  if (index == -1)
    throw Error("{0} not found".formatWith(firstSeparator));

  var from = index + firstSeparator.length;
  const index2 = this.indexOf(secondSeparator, from);
  if (index2 == -1)
    throw Error("{0} not found".formatWith(secondSeparator));

  return this.substring(from, index2);
};

String.prototype.tryBetween = function (this: string, firstSeparator: string, secondSeparator?: string) {

  if (!secondSeparator)
    secondSeparator = firstSeparator;

  const index = this.indexOf(firstSeparator);
  if (index == -1)
    return undefined;

  var from = index + firstSeparator.length;
  const index2 = this.indexOf(secondSeparator, from);
  if (index2 == -1)
    return undefined;

  return this.substring(from, index2);
};

String.prototype.after = function (this: string, separator: string) {
  const index = this.indexOf(separator);
  if (index == -1)
    throw Error("{0} not found".formatWith(separator));

  return this.substring(index + separator.length);
};

String.prototype.before = function (this: string, separator: string) {
  const index = this.indexOf(separator);
  if (index == -1)
    throw Error("{0} not found".formatWith(separator));

  return this.substring(0, index);
};

String.prototype.tryAfter = function (this: string, separator: string) {
  const index = this.indexOf(separator);
  if (index == -1)
    return undefined;

  return this.substring(index + separator.length);
};

String.prototype.tryBefore = function (this: string, separator: string) {
  const index = this.indexOf(separator);
  if (index == -1)
    return undefined;

  return this.substring(0, index);
};

String.prototype.beforeLast = function (this: string, separator: string) {
  const index = this.lastIndexOf(separator);
  if (index == -1)
    throw Error("{0} not found".formatWith(separator));

  return this.substring(0, index);
};

String.prototype.afterLast = function (this: string, separator: string) {
  const index = this.lastIndexOf(separator);
  if (index == -1)
    throw Error("{0} not found".formatWith(separator));

  return this.substring(index + separator.length);
};

String.prototype.tryBeforeLast = function (this: string, separator: string) {
  const index = this.lastIndexOf(separator);
  if (index == -1)
    return undefined;

  return this.substring(0, index);
};

String.prototype.tryAfterLast = function (this: string, separator: string) {
  const index = this.lastIndexOf(separator);
  if (index == -1)
    return undefined;

  return this.substring(index + separator.length);
};

String.prototype.etc = function (this: string, maxLength: number, etcString: string = "(…)") {
  let str = this;

  if (str.length > maxLength)
    str = str.substr(0, maxLength - etcString.length) + etcString;

  return str;
};

String.prototype.firstUpper = function () {
  return this[0].toUpperCase() + this.substring(1);
};

String.prototype.firstLower = function () {
  return this[0].toLowerCase() + this.substring(1);
};

String.prototype.repeat = function (this: string, n: number) {
  let result = "";
  for (let i = 0; i < n; i++)
    result += this;
  return result;
};

export module Dic {

  var simplesTypes = ["number", "boolean", "string"];
  export const skipClasses: Function[] = [];

  export function equals<V>(objA: V, objB: V, deep: boolean, depth = 0, visited: any[] = []): boolean {

    if (objA === objB)
      return true;

    if (objA == null || objB == null)
      return false;

    if (simplesTypes.contains(typeof objA) ||
      simplesTypes.contains(typeof objB))
      return false;

    if (objA instanceof Date && objB instanceof Date)
      return objA.valueOf() === objB.valueOf();

    if (Array.isArray(objA) !== Array.isArray(objB))
      return false;

    if (visited.indexOf(objB) != -1)
      return false;
    visited.push(objB);

    if (visited.indexOf(objA) != -1)
      return false;
    visited.push(objA);

    if (Array.isArray(objA)) {
      var ar = objA as any as any[];
      var br = objB as any as any[];

      if (ar.length != br.length)
        return false;

      return Array.range(0, ar.length).every(i => equals(ar[i], br[i], deep, depth + 1, visited));
    }

    if (Object.getPrototypeOf(objA) !== Object.getPrototypeOf(objB))
      return false;

    if (skipClasses.some(c => objA instanceof c))
      return false;

    const akeys = Dic.getKeys(objA);
    const bkeys = Dic.getKeys(objB);

    if (akeys.length != bkeys.length)
      return false;

    return akeys.every(k => equals((objA as any)[k], (objB as any)[k], deep, depth + 1, visited));
  }

  export function assign<O extends P, P extends {}>(obj: O, other: P | undefined): void {
    if (!other)
      return;

    for (const key in other) {
      if (other.hasOwnProperty == null || other.hasOwnProperty(key))
        (obj as any)[key] = other[key];
    }
  }


  export function getValues<V>(obj: { [key: string]: V }): V[] {
    const result: V[] = [];

    for (const name in obj) {
      if (obj.hasOwnProperty == null || obj.hasOwnProperty(name)) {
        result.push(obj[name]);
      }
    }

    return result;
  }

  export function getKeys(obj: { [key: string]: any }): string[] {
    const result: string[] = [];

    for (const name in obj) {
      if (obj.hasOwnProperty == null || obj.hasOwnProperty(name)) {
        result.push(name);
      }
    }

    return result;
  }

  export function clear(obj: { [key: string]: any }): void {
    for (const name in obj) {
      if (obj.hasOwnProperty == null || obj.hasOwnProperty(name)) {
        delete obj[name];
      }
    }
  }

  export function except(obj: { [key: string]: any }, keys: string[]): { [key: string]: any } {
    var result : { [key: string]: any } = {};
    for (const name in obj) {
      if (obj.hasOwnProperty == null || obj.hasOwnProperty(name)) {
        if (!keys.contains(name))
          result[name] = obj[name];
      }
    }
    return result; 
  }

  export function map<V, R>(obj: { [key: string]: V }, selector: (key: string, value: V, index: number) => R): R[] {
    let index = 0;
    const result: R[] = [];
    for (const name in obj) {
      if (obj.hasOwnProperty == null || obj.hasOwnProperty(name)) {
        result.push(selector(name, obj[name], index++));
      }
    }
    return result;
  }

  export function mapObject<V, R>(obj: { [key: string]: V }, selector: (key: string, value: V, index: number) => R): {[key: string] : R} {
    let index = 0;
    const result: { [key: string]: R } = {};
    for (const name in obj) {
      if (obj.hasOwnProperty == null || obj.hasOwnProperty(name)) {
        result[name] = selector(name, obj[name], index++);
      }
    }
    return result;
  }

  export function foreach<V>(obj: { [key: string]: V }, action: (key: string, value: V) => void): void {

    for (const name in obj) {
      if (obj.hasOwnProperty == null || obj.hasOwnProperty(name)) {
        action(name, obj[name]);
      }
    }
  }


  export function addOrThrow<V>(dic: { [key: string]: V }, key: string, value: V, errorContext?: string): void {
    if (dic[key])
      throw new Error(`Key ${key} already added` + (errorContext ? "in " + errorContext : ""));

    dic[key] = value;
  }

  export function simplify<T extends {}>(a: T): T;
  export function simplify<T extends {}>(a: undefined): undefined;
  export function simplify<T extends {}>(a: T | undefined): T | undefined;
  export function simplify<T extends {}>(a: T | undefined): T | undefined{
    if (a == null)
      return a;

    var result: T = {} as any;
    for (const key in a) {
      if ((a.hasOwnProperty == null || a.hasOwnProperty(key)) && a[key] !== undefined)
        result[key] = a[key];
    }
    return result;
  }

  export function deepFreeze<T extends object>(object: T): T {

    // Abrufen der definierten Eigenschaftsnamen des Objekts
    var propNames = Object.getOwnPropertyNames(object);

    // Eigenschaften vor dem eigenen Einfrieren einfrieren
    var result = {};

    for (let name of propNames) {
      let value = (object as any)[name];

      (result as any)[name] = value && typeof value === "object" ?
        deepFreeze(value) : value;
    }

    return Object.freeze(object);
  }
}

export function coalesce<T>(value: T | undefined | null, defaultValue: T): T {
  return value != null ? value : defaultValue;
}

export function classes(...classNames: (string | null | undefined | boolean /*false*/)[]): string {
  return classNames.filter(a => a && a != "").join(" ");
}
export function combineFunction<F extends Function>(func1?: F | null, func2?: F | null): F | null | undefined {
  if (!func1)
    return func2;

  if (!func2)
    return func1;

  return function combined(this: any, ...args: any[]) {
    func1.apply(this, args);
    func2.apply(this, args);
  } as any;
}

RegExp.escape = function (s: string) {
  return s.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&');
};


export function areEqual<T>(a: T | undefined, b: T | undefined, field: (value: T) => any): boolean {
  if (a == undefined)
    return b == undefined;

  if (b == undefined)
    return false;

  return field(a) == field(b);
}

export function ifError<E, T>(ErrorClass: { new(...args: any[]): E }, onError: (error: E) => T): (error: any) => T {
  return error => {
    if (error instanceof ErrorClass)
      return onError((error as E));
    throw error;
  };
}

export function bytesToSize(bytes: number): string {
  var sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
  if (bytes == 0) return '0 Bytes';
  var unit = parseInt(Math.floor(Math.log(bytes) / Math.log(1024)) as any);
  return Math.round((bytes / Math.pow(1024, unit)) * 100) / 100 + ' ' + sizes[unit];
};

export module DomUtils {
  export function matches(elem: HTMLElement, selector: string): boolean {
    // Vendor-specific implementations of `Element.prototype.matches()`.
    const proto = Element.prototype as any;
    const nativeMatches = proto.matches ||
      proto.webkitMatchesSelector ||
      proto.mozMatchesSelector ||
      proto.msMatchesSelector ||
      proto.oMatchesSelector;

    if (!elem || elem.nodeType !== 1) {
      return false;
    }

    const parentElem = elem.parentNode as HTMLElement;

    // use native 'matches'
    if (nativeMatches) {
      return nativeMatches.call(elem, selector);
    }

    // native support for `matches` is missing and a fallback is required
    const nodes = parentElem.querySelectorAll(selector);
    const len = nodes.length;

    for (let i = 0; i < len; i++) {
      if (nodes[i] === elem) {
        return true;
      }
    }

    return false;
  }

  export function closest(element: HTMLElement, selector: string, context?: Node): HTMLElement | undefined {
    context = context || document;
    // guard against orphans
    while (!matches(element, selector)) {
      if (element == context || element == undefined)
        return undefined;

      element = element.parentNode as HTMLElement;
    }

    return element;
  }

  export function offsetParent(element: HTMLElement): HTMLElement | undefined {

    const isRelativeOrAbsolute = (str: string | null) => str === "relative" || str === "absolute";

    // guard against orphans
    while (!isRelativeOrAbsolute(window.getComputedStyle(element).position)) {
      if (element.parentNode == document)
        return undefined;

      element = element.parentNode as HTMLElement;
    }

    return element;
  }
}

export class KeyGenerator {
  map: Map<object, number> = new Map<object, number>();
  maxIndex = 0;
  getKey(o: object): number {
    var result = this.map.get(o);
    if (result == undefined) {
      result = this.maxIndex++;
      this.map.set(o, result);
    }
    return result;
  }
}

export function roundTwoDecimals(num: number): number {

  var round3 = Math.round(num * 1000000) / 1000000; //convert 0.0049999999999 -> 0.005

  var round3m100 = round3 * 100;

  var mod = round3m100 % 10; //Simulate Midpoint to Even (C# decimal default) instead of Midpoint to +Inf (JS behaviour)
  if (mod == 0.5 || mod == 2.5 || mod == 4.5 || mod == 6.5 || mod == 8.5)
    round3m100 -= 0.001;

  return Math.round(round3m100) / 100; //https://stackoverflow.com/questions/11832914/round-to-at-most-2-decimal-places-only-if-necessary
}


export function getColorContrasColorBWByHex (hexcolor: string): "black" | "white" {
  hexcolor = hexcolor.replace("#", "");
  var r = parseInt(hexcolor.substr(0, 2), 16);
  var g = parseInt(hexcolor.substr(2, 2), 16);
  var b = parseInt(hexcolor.substr(4, 2), 16);
  var yiq = ((r * 299) + (g * 587) + (b * 114)) / 1000;
  return (yiq >= 128) ? 'black' : 'white';
}

export function isPromise(value: any): value is Promise<any> {
  return value != null && value.then != null;
}

export function toPromise<T>(value: T | Promise<T>): Promise<T> {
  return isPromise(value) ? value : Promise.resolve(value);
}
