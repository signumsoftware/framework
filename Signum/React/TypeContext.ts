import * as React from 'react'
import { PropertyRoute, PropertyRouteType, getLambdaMembers, IBinding, ReadonlyBinding, createBinding, MemberType, Type, PseudoType, getTypeName, Binding, getFieldMembers, LambdaMember, IType, isType, tryGetTypeInfo, MemberInfo, TypeInfo, getTypeInfos, getTypeInfo } from './Reflection'
import { ModelState, MList, ModifiableEntity, EntityPack, Entity, MixinEntity, ModelEntity, BooleanEnum } from './Signum.Entities'
import { EntityOperationContext } from './Operations'
import { MListElementBinding } from "./Reflection";
import { classes } from './Globals';
import { EmbeddedWidget } from './Frames/Widgets';
import { ViewPromise } from './Navigator';

export type FormGroupStyle =
  "None" |  /// Only the value is rendered.
  "Basic" |   /// Label on top, value below.
  "BasicDown" |  /// Value on top, label below.
  "SrOnly" |    /// Label visible only for Screen-Readers.
  "LabelColumns" | 
  "FloatingLabel"; /// (default) Label on the left, value on the right (exept RTL). Affected by labelColumns / valueColumns

export type FormSize =
  "xs" |
  "sm" |
  "md" |
  "lg";


export class StyleContext {
  styleOptions: StyleOptions;
  parent: StyleContext;

  constructor(parent: StyleContext | undefined, styleOptions: StyleOptions | undefined) {
    this.parent = parent || StyleContext.default;
    this.styleOptions = styleOptions || {};

    if (this.styleOptions.labelColumns && !this.styleOptions.valueColumns)
      this.styleOptions.valueColumns = StyleContext.bsColumnsInvert(toBsColumn(this.styleOptions.labelColumns));
  }

  static default: StyleContext = new StyleContext(undefined,
    {
      formGroupStyle: "LabelColumns",
      formSize: "sm",
      labelColumns: { sm: 2 },
      readOnly: false,
      placeholderLabels: false,
      titleLabels: true,
      readonlyAsPlainText: false,
      frame: undefined,
    });

  get formGroupStyle(): FormGroupStyle {
    return this.styleOptions.formGroupStyle != undefined ? this.styleOptions.formGroupStyle : this.parent.formGroupStyle;
  }

  get formSize(): FormSize {
    return this.styleOptions.formSize != undefined ? this.styleOptions.formSize : this.parent.formSize;
  }

  get formGroupClass(): string | undefined {
    switch (this.formSize) {
      case "xs": return "form-group form-group-xs";
      case "sm": return "form-group form-group-sm";
      case "md": return "form-group";
      case "lg": return "form-group form-group-lg";
      default: throw new Error("Unexpected formSize " + this.formSize);
    }
  }

  get colFormLabelClass(): string | undefined {
    switch (this.formSize) {
      case "xs": return "col-form-label col-form-label-xs";
      case "sm": return "col-form-label col-form-label-sm";
      case "md": return "col-form-label";
      case "lg": return "col-form-label col-form-label-lg";
      default: throw new Error("Unexpected formSize " + this.formSize);
    }
  }

  get labelClass(): string | undefined {
    switch (this.formSize) {
      case "xs": return "label-xs";
      case "sm": return "label-sm";
      case "md": return undefined;
      case "lg": return undefined;
      default: throw new Error("Unexpected formSize " + this.formSize);
    }
  }

  get rwWidgetClass(): string | undefined {
    switch (this.formSize) {
      case "xs": return "rw-widget-xs";
      case "sm": return "rw-widget-sm";
      case "md": return "";
      case "lg": return "rw-widget-lg";
      default: throw new Error("Unexpected formSize " + this.formSize);
    }
  }

  get inputGroupClass(): string | undefined {
    switch (this.formSize) {
      case "xs": return "input-group input-group-xs";
      case "sm": return "input-group input-group-sm";
      case "md": return "input-group";
      case "lg": return "input-group input-group-lg";
      default: throw new Error("Unexpected formSize " + this.formSize);
    }
  }

  inputGroupVerticalClass(mode: "before" | "after"): string | undefined {
    switch (this.formSize) {
      case "xs": return "input-group-vertical " + mode + " input-group-xs";
      case "sm": return "input-group-vertical " + mode + " input-group-sm";
      case "md": return "input-group-vertical " + mode ;
      case "lg": return "input-group-vertical " + mode + " input-group-lg";
      default: throw new Error("Unexpected formSize " + this.formSize);
    }
  }

  get formControlClass(): string | undefined {
    switch (this.formSize) {
      case "xs": return "form-control form-control-xs";
      case "sm": return "form-control form-control-sm";
      case "md": return "form-control";
      case "lg": return "form-control form-control-lg";
      default: throw new Error("Unexpected formSize " + this.formSize);
    }
  }

  get formSelectClass(): string | undefined {
    switch (this.formSize) {
      case "xs": return "form-select form-select-xs";
      case "sm": return "form-select form-select-sm";
      case "md": return "form-select";
      case "lg": return "form-select form-select-lg";
      default: throw new Error("Unexpected formSize " + this.formSize);
    }
  }

  get formControlPlainTextClass(): string | undefined {
    switch (this.formSize) {
      case "xs": return "form-control-plaintext form-control-xs";
      case "sm": return "form-control-plaintext form-control-sm";
      case "md": return "form-control-plaintext";
      case "lg": return "form-control-plaintext form-control-lg";
      default: throw new Error("Unexpected formSize " + this.formSize);
    }
  }

  get buttonClass(): string | undefined {
    switch (this.formSize) {
      case "xs": return "btn-xs";
      case "sm": return "btn-sm";
      case "md": return undefined;
      case "lg": return "btn-lg";
      default: throw new Error("Unexpected formSize " + this.formSize);
    }
  }

  get placeholderLabels(): boolean {
    return this.styleOptions.placeholderLabels != undefined ? this.styleOptions.placeholderLabels : this.parent.placeholderLabels;
  }

  get titleLabels(): boolean {
    return this.styleOptions.titleLabels != undefined ? this.styleOptions.titleLabels : this.parent.titleLabels;
  }

  get readonlyAsPlainText(): boolean {
    return this.styleOptions.readonlyAsPlainText != undefined ? this.styleOptions.readonlyAsPlainText : this.parent.readonlyAsPlainText;
  }

  get labelColumns(): BsColumns {
    return this.styleOptions.labelColumns != undefined ? toBsColumn(this.styleOptions.labelColumns) : this.parent.labelColumns;
  }



  get labelColumnsCss(): string {
    return StyleContext.bsColumnsCss(this.labelColumns);
  }

  get valueColumns(): BsColumns {
    return this.styleOptions.valueColumns != undefined ? toBsColumn(this.styleOptions.valueColumns) : this.parent.valueColumns;
  }

  get valueColumnsCss(): string {
    return StyleContext.bsColumnsCss(this.valueColumns);
  }

  get readOnly(): boolean {
    return this.styleOptions.readOnly != undefined ? this.styleOptions.readOnly :
      this.parent ? this.parent.readOnly : false;
  }

  set readOnly(value: boolean) {
    this.styleOptions.readOnly = value;
  }

  get frame(): EntityFrame | undefined {
    if (this.styleOptions.frame)
      return this.styleOptions.frame;

    if (this.parent)
      return this.parent.frame;

    return undefined;
  }


  static bsColumnsCss(bsColumns: BsColumns): string {
    return [
      (bsColumns.xs ? "col-xs-" + bsColumns.xs : ""),
      (bsColumns.sm ? "col-sm-" + bsColumns.sm : ""),
      (bsColumns.md ? "col-md-" + bsColumns.md : ""),
      (bsColumns.lg ? "col-lg-" + bsColumns.lg : ""),
    ].filter(a => a != "").join(" ");
  }

  static bsColumnsInvert(bs: BsColumns): BsColumns {
    return {
      xs: bs.xs ? (12 - bs.xs) : undefined,
      sm: (12 - bs.sm),
      md: bs.md ? (12 - bs.md) : undefined,
      lg: bs.lg ? (12 - bs.lg) : undefined,
    };
  }
}

function toBsColumn(bsColumnOrNumber: BsColumns | number): BsColumns {
  return typeof (bsColumnOrNumber) == "number" ? { sm: bsColumnOrNumber } : bsColumnOrNumber;
}

export interface StyleOptions {
  formGroupStyle?: FormGroupStyle;
  formSize?: FormSize;
  placeholderLabels?: boolean;
  titleLabels?: boolean;
  readonlyAsPlainText?: boolean;
  labelColumns?: BsColumns | number;
  valueColumns?: BsColumns | number;
  readOnly?: boolean;
  frame?: EntityFrame;
}



export interface BsColumns {
  xs?: number;
  sm: number;
  md?: number;
  lg?: number;
}



export class TypeContext<T> extends StyleContext {

  propertyRoute: PropertyRoute | undefined; /*Because of optional TypeInfo*/
  binding: IBinding<T>; //Could be null on removed elements in Time Machine
  previousVersion?: { value: T, oldIndex?: number, isMoved?: boolean }; //Used for Time Machine
  prefix: string;

  get value() {
    if (this.binding == undefined)
      return undefined as any; //React Dev Tools

      return this.binding.getValue();
  }

  set value(val: T) {
    this.binding.setValue(val);
  }

  get error() {
    if (this.binding == undefined)
      return undefined as any; //React Dev Tools

      return this.binding.getError();
  }

  set error(val: string | undefined) {
    this.binding.setError(val);
  }

  get index(): number | undefined {
    return (this.binding as MListElementBinding<any>)?.index ??
      (this.parent as TypeContext<any>)?.index;
  }

  static root<T extends ModifiableEntity>(value: T, styleOptions?: StyleOptions, parent?: StyleContext): TypeContext<T> {
    return new TypeContext(parent, styleOptions, PropertyRoute.root(value.Type), new ReadonlyBinding<T>(value, ""));
  }

  constructor(parent: StyleContext | undefined, styleOptions: StyleOptions | undefined, propertyRoute: PropertyRoute | undefined, binding: IBinding<T>, prefix?: string) {
    super(parent, styleOptions);
    this.propertyRoute = propertyRoute;
    this.binding = binding;

    this.prefix = prefix || ((parent && (parent as TypeContext<any>).prefix || "") + binding?.suffix);
  }

  subCtx(styleOptions: StyleOptions): TypeContext<T>
  subCtx<R>(property: (val: T) => R, styleOptions?: StyleOptions): TypeContext<R>
  subCtx<M extends MixinEntity>(mixin: Type<M>, styleOptions?: StyleOptions): TypeContext<M> //Only id T extends Entity!
  subCtx(field: string, styleOptions?: StyleOptions): TypeContext<any>
  subCtx(arg: ((val: T) => any) | IType | string | StyleOptions, styleOptions?: StyleOptions): TypeContext<any> {
    if (typeof arg == "object" && !isType(arg)) {
      var nc = new TypeContext<T>(this, arg, this.propertyRoute, this.binding, this.prefix);
      nc.previousVersion = this.previousVersion;

      return nc;
    }

    const lambdaMembers =
      typeof arg == "function" ? getLambdaMembers(arg) :
        isType(arg) ? [{ type: "Mixin", name: arg.typeName } as LambdaMember] :
          getFieldMembers(arg);

    const subRoute = lambdaMembers.reduce<PropertyRoute | undefined>((pr, m) => pr && pr.tryAddLambdaMember(m), this.propertyRoute);

    const binding = createBinding(this.value, lambdaMembers);

    const result = new TypeContext<any>(this, styleOptions, subRoute, binding);

    if (this.previousVersion && this.previousVersion.value) {
      result.previousVersion = { value: createBinding(this.previousVersion.value, lambdaMembers).getValue() }; 
    }

    return result;
  }

  cast<R extends T & ModifiableEntity>(type: Type<R>): TypeContext<R>;
  cast(): TypeContext<any>;
  cast(type?: Type<any>): TypeContext<any> 
  {
    const entity = this.value as any as Entity;

    const typeName = type?.typeName ?? entity.Type;
    if (typeName != entity.Type)
      throw new Error(`Impossible to cast ${entity.Type} into ${typeName}`);

    var newPr = this.propertyRoute == null ? undefined : this.propertyRoute.typeReference().name == typeName ? this.propertyRoute : PropertyRoute.root(typeName);

    const result = new TypeContext<any>(this, undefined, newPr, new ReadonlyBinding(entity, ""));

    return result;
  }

  as<R extends T & ModifiableEntity>(type: Type<R>): TypeContext<R> | undefined {

    const entity = this.value as any as Entity;

    if (type.typeName != entity.Type)
      return undefined;

    var newPr = this.propertyRoute!.typeReference().name == type.typeName ? this.propertyRoute : PropertyRoute.root(type);

    const result = new TypeContext<any>(this, undefined, newPr, new ReadonlyBinding(entity, ""));

    return result;
  }

  niceName(property?: (val: T) => any): string {

    if (this.propertyRoute == undefined)
      throw new Error("No propertyRoute");

    if (property == undefined)
      return this.propertyRoute.member!.niceName;

    return this.propertyRoute.addLambda(property).member!.niceName;
  }

  tryMemberInfo(property?: (val: T) => any): MemberInfo | undefined {

    if (this.propertyRoute == undefined)
      throw new Error("No propertyRoute");

    if (property == undefined)
      return this.propertyRoute.member;

    return this.propertyRoute.tryAddLambda(property)?.member;
  }

  memberInfo(property?: (val: T) => any): MemberInfo {

    if (this.propertyRoute == undefined)
      throw new Error("No propertyRoute");

    if (property == undefined)
      return this.propertyRoute.member!;

    return this.propertyRoute.addLambda(property).member!;
  }

  getUniqueId(suffix?: string): string {
    var path = suffix == null ? this.prefix : (this.prefix + "." + suffix);

    return path.replace(/.\[\]/, "_");
  }

  tryFindRootEntity(): TypeContext<ModelEntity | Entity> | undefined {
    let current: TypeContext<any> = this;
    while (current) {
      const entity = current.value as ModifiableEntity;
      if (entity && entity.Type && tryGetTypeInfo(entity.Type))
        return current as TypeContext<ModifiableEntity>;

      current = current.parent as TypeContext<any>;
    }

    return undefined;
  }

  tryFindParentCtx<S extends ModifiableEntity>(type: Type<S>): TypeContext<S> | undefined;
  tryFindParentCtx(type: PseudoType): TypeContext<ModifiableEntity> | undefined;
  tryFindParentCtx(type: PseudoType): TypeContext<ModifiableEntity> | undefined {
    let current: TypeContext<any> = this;
    const typeName = getTypeName(type);
    while (current) {
      const entity = current.value as ModifiableEntity;
      if (entity && entity.Type == typeName)
        return current as TypeContext<ModifiableEntity>;

      current = current.parent as TypeContext<any>;
    }

    return undefined;
  }

  findParentCtx<S extends ModifiableEntity>(type: Type<S>): TypeContext<S>;
  findParentCtx(type: PseudoType): TypeContext<ModifiableEntity>;
  findParentCtx(type: PseudoType): TypeContext<ModifiableEntity> {
    const result = this.tryFindParentCtx(type);
    if (result == undefined)
      throw new Error(`No '${getTypeName(type)}' found in the parent chain`);

    return result;
  }

  tryFindParent<S extends ModifiableEntity>(type: Type<S>): S | undefined;
  tryFindParent(type: PseudoType): ModifiableEntity | undefined;
  tryFindParent(type: PseudoType): ModifiableEntity | undefined {
    var ctx = this.tryFindParentCtx(type);
    return ctx && ctx.value;
  }

  findParent<S extends ModifiableEntity>(type: Type<S>): S;
  findParent(type: PseudoType): ModifiableEntity;
  findParent(type: PseudoType): ModifiableEntity {
    var ctx = this.tryFindParentCtx(type);
    const result = ctx && ctx.value;
    if (result == undefined)
      throw new Error(`No '${getTypeName(type)}' found in the parent chain`);

    return result;
  }

  using(render: (ctx: this) => React.ReactNode): React.ReactNode {
    return render(this);
  }

  mlistItemCtxs<R>(property: (val: T) => MList<R>, styleOptions?: StyleOptions): TypeContext<R>[] {
    return mlistItemContext(this.subCtx(property, styleOptions));
  }

  get propertyPath(): string | undefined {
    return this.propertyRoute && this.propertyRoute.propertyRouteType != "Root" ? this.propertyRoute.propertyPath() : undefined;
  }

}

export interface ButtonsContext {
  pack: EntityPack<ModifiableEntity>;
  frame: EntityFrame<ModifiableEntity>;
  isOperationVisible?: (eoc: EntityOperationContext<any /*Entity*/>) => boolean;
  tag?: string;
}

export interface ButtonBarElement {
  button: React.ReactElement<any>;
  order?: number;
  shortcut?: (e: KeyboardEvent) => boolean;
}

export interface IRenderButtons {
  renderButtons(ctx: ButtonsContext): (ButtonBarElement | undefined)[];
}

export interface IOperationVisible {
  isOperationVisible(eoc: EntityOperationContext<any /*Entity*/>): boolean;
}

export interface IHasChanges {
  entityHasChanges?: () => boolean | undefined;
}

export interface FunctionalFrameComponent {
  forceUpdate(): void;
  type: Function;
}

export interface EntityFrame<T extends ModifiableEntity = ModifiableEntity> {
  frameComponent: FunctionalFrameComponent | React.Component;
  tabs: EmbeddedWidget[] | undefined;
  entityComponent: React.Component | null | undefined;
  pack: EntityPack<T>;
  onReload: (pack?: EntityPack<T>, reloadComponent?: boolean | string | ViewPromise<T>, callback?: () => void) => void;
  setError: (modelState: ModelState, initialPrefix?: string) => void;
  revalidate: () => void;
  onClose: (pack?: EntityPack<T>) => void;
  refreshCount: number;
  allowExchangeEntity: boolean;

  isExecuting(): boolean; 
  execute: (action: () => Promise<void>) => Promise<void>;

  createNew?: (oldPack: EntityPack<T>) => (Promise<EntityPack<T> | undefined>) | undefined;
  prefix: string;

  currentDate?: string;
  previousDate?: string;

  hideAndClose?: boolean;
}

export function mlistItemContext<T>(ctx: TypeContext<MList<T>>): TypeContext<T>[] {
  const elemPR = ctx.propertyRoute?.addMember("Indexer", "", true);

  if (ctx.previousVersion == null)
    return ctx.value!.map((mle, i) => new TypeContext<T>(ctx, undefined, elemPR,
      new MListElementBinding<T>(ctx.binding as IBinding<MList<T>>, i),
    ));

  var list = ctx.value!.map((mle, i) => {

    var eleCtx = new TypeContext<T>(ctx, undefined, elemPR,
      new MListElementBinding<T>(ctx.binding as IBinding<MList<T>>, i),
    );

    var index = mle.rowId == null ? undefined : ctx.previousVersion!.value.findIndex(oe => oe.rowId == mle.rowId);
    eleCtx.previousVersion = {
      value: index == -1 || index == null ? null! : ctx.previousVersion!.value[index].element,
      oldIndex: index == -1 ? undefined : index,
      isMoved: index == -1 ? undefined : index != i,
    };

    return eleCtx;
  });

  var currentRowIds = ctx.value.map(mle => mle.rowId).notNull();

  ctx.previousVersion.value.forEach((mle, i) => {
    if (!currentRowIds.contains(mle.rowId!)) {
      var newIndex = list.findIndex(a => a.previousVersion && a.previousVersion!.oldIndex && i < a.previousVersion.oldIndex);

      var removedCtx = new TypeContext<T>(ctx, undefined, elemPR, undefined!);
      removedCtx.previousVersion = { value: mle.element, oldIndex: i };
      list.insertAt(newIndex == -1 ? list.length : newIndex, removedCtx);
    }
  });

  return list;
}
