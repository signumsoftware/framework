import * as React from 'react'
import { PropertyRoute, PropertyRouteType, getLambdaMembers, IBinding, ReadonlyBinding, createBinding, MemberType, Type, PseudoType, getTypeName, Binding, getFieldMembers, LambdaMember, IType, isType } from './Reflection'
import { ModelState, MList, ModifiableEntity, EntityPack, Entity, MixinEntity } from './Signum.Entities'
import { EntityOperationContext } from './Operations'
import { MListElementBinding } from "./Reflection";

export type FormGroupStyle =
    "None" |  /// Only the value is rendered. Unaffected by FormGroupSize     
    "Basic" |   /// Label on top, value below. Requires form-vertical container
    "BasicDown" |  /// Value on top, label below. Requires form-vertical container
    "SrOnly" |    /// Label visible only for Screen-Readers. Requires form-vertical / form-inline container
    "LabelColumns"; /// (default) Label on the left, value on the right (exept RTL). Requires form-horizontal, affected by LabelColumns / ValueColumns

export type FormSize =
    "ExtraSmall" |
    "Small" |
    "Normal" |
    "Large";
   

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
        formGroupStyle : "LabelColumns",
        formSize : "Small",
        labelColumns: { sm: 2 },
        readOnly : false,
        placeholderLabels : false,
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
            case "ExtraSmall": return "form-group form-group-xs";
            case "Small": return "form-group form-group-sm";
            case "Normal": return "form-group";
            case "Large": return "form-group form-group-lg";
            default: throw new Error("Unexpected formSize " + this.formSize);
        }
    }

    get colFormLabelClass(): string | undefined {
        switch (this.formSize) {
            case "ExtraSmall": return "col-form-label col-form-label-xs";
            case "Small": return "col-form-label col-form-label-sm";
            case "Normal": return "col-form-label";
            case "Large": return "col-form-label col-form-label-lg";
            default: throw new Error("Unexpected formSize " + this.formSize);
        }
    }

    get labelClass(): string | undefined {
        switch (this.formSize) {
            case "ExtraSmall": return "label-xs";
            case "Small": return "label-sm";
            case "Normal": return undefined;
            case "Large": return undefined;
            default: throw new Error("Unexpected formSize " + this.formSize);
        }
    }

    get rwWidgetClass(): string | undefined {
        switch (this.formSize) {
            case "ExtraSmall": return "rw-widget-xs";
            case "Small": return "rw-widget-sm";
            case "Normal": return "";
            case "Large": return "rw-widget-lg";
            default: throw new Error("Unexpected formSize " + this.formSize);
        }
    }

    get inputGroupClass(): string | undefined {
        switch (this.formSize) {
            case "ExtraSmall": return "input-group input-group-xs";
            case "Small": return "input-group input-group-sm";
            case "Normal": return "input-group";
            case "Large": return "input-group input-group-lg";
            default: throw new Error("Unexpected formSize " + this.formSize);
        }
    }

    get formControlClass(): string | undefined {
        switch (this.formSize) {
            case "ExtraSmall": return "form-control form-control-xs";
            case "Small": return "form-control form-control-sm";
            case "Normal": return "form-control";
            case "Large": return "form-control form-control-lg";
            default: throw new Error("Unexpected formSize " + this.formSize);
        }
    }

    get formControlPlainTextClass(): string | undefined {
        switch (this.formSize) {
            case "ExtraSmall": return "form-control-plaintext form-control-xs";
            case "Small": return "form-control-plaintext form-control-sm";
            case "Normal": return "form-control-plaintext";
            case "Large": return "form-control-plaintext form-control-lg";
            default: throw new Error("Unexpected formSize " + this.formSize);
        }
    }

    get buttonClass(): string | undefined {
        switch (this.formSize) {
            case "ExtraSmall": return "btn-xs";
            case "Small": return "btn-sm";
            case "Normal": return undefined;
            case "Large": return "btn-lg";
            default: throw new Error("Unexpected formSize " + this.formSize);
        }
    }

    get placeholderLabels(): boolean {
        return this.styleOptions.placeholderLabels != undefined ? this.styleOptions.placeholderLabels : this.parent.placeholderLabels;
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


    static bsColumnsCss(bsColumns: BsColumns) {
        return [
            (bsColumns.xs ? "col-xs-" + bsColumns.xs : ""),
            (bsColumns.sm ? "col-sm-" + bsColumns.sm : ""),
            (bsColumns.md ? "col-md-" + bsColumns.md : ""),
            (bsColumns.lg ? "col-lg-" + bsColumns.lg : ""),
        ].filter(a=> a != "").join(" ");
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
    
    propertyRoute: PropertyRoute;
    binding: IBinding<T>;
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


    static root<T extends ModifiableEntity>(value: T, styleOptions?: StyleOptions, parent?: StyleContext): TypeContext<T> {
        return new TypeContext(parent, styleOptions, PropertyRoute.root(value.Type), new ReadonlyBinding<T>(value, ""));
    }

    constructor(parent: StyleContext | undefined, styleOptions: StyleOptions | undefined, propertyRoute: PropertyRoute /*| undefined*/, binding: IBinding<T>) {
        super(parent, styleOptions);
        this.propertyRoute = propertyRoute;
        this.binding = binding;
        
        this.prefix = compose(parent && (parent as TypeContext<any>).prefix, binding.suffix);
    }
  
    subCtx(styleOptions: StyleOptions): TypeContext<T>     
    subCtx<R>(property: (val: T) => R, styleOptions?: StyleOptions): TypeContext<R>
    subCtx<M extends MixinEntity>(mixin: Type<M>, styleOptions?: StyleOptions): TypeContext<M> //Only id T extends Entity!
    subCtx(field: string, styleOptions?: StyleOptions): TypeContext<any>
    subCtx(arg: ((val: T) => any) | IType | string | StyleOptions , styleOptions?: StyleOptions): TypeContext<any>
    {
        if (typeof arg == "object" && !isType(arg))
            return new TypeContext<T>(this, arg, this.propertyRoute, this.binding);
        
        const lambdaMembers =
            typeof arg == "function" ? getLambdaMembers(arg) :
                isType(arg) ? [{ type: "Mixin", name: arg.typeName } as LambdaMember] :
                    getFieldMembers(arg);

        const subRoute = lambdaMembers.reduce<PropertyRoute>((pr, m) => pr.addLambdaMember(m), this.propertyRoute);
        
        const binding = createBinding(this.value, lambdaMembers);

        const result = new TypeContext<any>(this, styleOptions, subRoute, binding);

        return result;
    }

    cast<R extends T & ModifiableEntity>(type: Type<R>): TypeContext<R> {

        const entity = this.value as any as Entity;

        if (type.typeName != entity.Type)
            throw new Error(`Impossible to cast ${entity.Type} into ${type.typeName}`);

        var newPr = this.propertyRoute.typeReference().name == type.typeName ? this.propertyRoute : PropertyRoute.root(type);

        const result = new TypeContext<any>(this, undefined, newPr, new ReadonlyBinding(entity, this.binding.suffix + "_" + type.typeName));

        return result;
    }

    as<R extends T & ModifiableEntity>(type: Type<R>): TypeContext<R> | undefined {

        const entity = this.value as any as Entity;

        if (type.typeName != entity.Type)
            return undefined;

        var newPr = this.propertyRoute.typeReference().name == type.typeName ? this.propertyRoute : PropertyRoute.root(type);

        const result = new TypeContext<any>(this, undefined, newPr, new ReadonlyBinding(entity, this.binding.suffix + "_" + type.typeName));

        return result;
    }

    niceName(property?: (val: T) => any): string  {

        if (this.propertyRoute == undefined)
            throw new Error("No propertyRoute");

        if (property == undefined)
            return this.propertyRoute.member!.niceName;

        return this.propertyRoute.addLambda(property).member!.niceName;
    }

    compose(suffix: string): string {
        return compose(this.prefix, suffix);
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
    findParentCtx(type: PseudoType): TypeContext<ModifiableEntity >{
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

    using(render: (ctx: this) => React.ReactChild): React.ReactChild {
        return render(this);
    }

    mlistItemCtxs<R>(property: (val: T) => MList<R>, styleOptions?: StyleOptions): TypeContext<R>[] {
        return mlistItemContext(this.subCtx(property, styleOptions));
    }

    get propertyPath() {
        return this.propertyRoute && this.propertyRoute.propertyRouteType != "Root" ? this.propertyRoute.propertyPath() : undefined;
    }

    get errorClass(): string | undefined {
        return !!this.error ? "has-error" : undefined;
    }
}

export interface ButtonsContext {
    pack: EntityPack<ModifiableEntity>;
    frame: EntityFrame;
    isOperationVisible?: (eoc: EntityOperationContext<any /*Entity*/>) => boolean;
    tag?: string;
}

export interface IRenderButtons {
    renderButtons(ctx: ButtonsContext): (React.ReactElement<any> | undefined)[];
}

export interface IOperationVisible {
    isOperationVisible(eoc: EntityOperationContext<any /*Entity*/>): boolean;
}

export interface IHasChanges {
    componentHasChanges?: ()=> boolean;
}

export interface EntityFrame {
    frameComponent: React.Component<any, any>;
    entityComponent: React.Component<any, any> | null | undefined;
    onReload: (pack?: EntityPack<ModifiableEntity>) => void;
    setError: (modelState: ModelState, initialPrefix?: string) => void;
    revalidate: () => void;
    onClose: (ok?: boolean) => void;
    refreshCount: number;
}


function compose(prefix: string | undefined, suffix: string | undefined): string {
    if (!prefix || prefix == "")
        return suffix || "";

    if (!suffix || suffix == "")
        return prefix || "";

    return prefix + "_" + suffix;
}

export function mlistItemContext<T>(ctx: TypeContext<MList<T>>): TypeContext<T>[] {
    
    return ctx.value!.map((mle, i) =>
        new TypeContext<T>(ctx, undefined,
            ctx.propertyRoute.addMember("Indexer", ""),
            new MListElementBinding<T>(ctx.binding, i)));
}




