﻿import * as React from 'react'
import { PropertyRoute, PropertyRouteType, getLambdaMembers, IBinding, ReadonlyBinding, createBinding, LambdaMemberType, Type } from './Reflection'
import { ModelState, MList, ModifiableEntity, EntityPack } from './Signum.Entities'

export type FormGroupStyle =
    "None" |  /// Unaffected by FormGroupSize     
    "Basic" |   /// Requires form-vertical container
    "BasicDown" |  /// Requires form-vertical container
    "SrOnly" |    /// Requires form-vertical / form-inline container
    "LabelColumns"; /// Requires form-horizontal (default),  affected by LabelColumns / ValueColumns

export type FormGroupSize =
    "Normal" | //Raw Bootstrap default
    "Small" | //Signum default
    "ExtraSmall"; //Like in FilterBuilder
   

export class StyleContext {
    styleOptions: StyleOptions;
    parent: StyleContext;

    constructor(parent: StyleContext | undefined, styleOptions: StyleOptions | undefined) {
        this.parent = parent || StyleContext.default;
        this.styleOptions = styleOptions || {};

        if (this.styleOptions.labelColumns && !this.styleOptions.valueColumns)
            this.styleOptions.valueColumns = StyleContext.bsColumnsInvert(this.styleOptions.labelColumns);
    }

    static default: StyleContext = new StyleContext(undefined,
    {
        formGroupStyle : "LabelColumns",
        formGroupSize : "Small",
        labelColumns: { sm: 2 },
        readOnly : false,
        placeholderLabels : false,
        formControlStaticAsFormControlReadonly: false,
        frame: undefined,
    });

    get formGroupStyle(): FormGroupStyle {
        return this.styleOptions.formGroupStyle != undefined ? this.styleOptions.formGroupStyle : this.parent.formGroupStyle;
    }

    get formGroupSize(): FormGroupSize {
        return this.styleOptions.formGroupSize != undefined ? this.styleOptions.formGroupSize : this.parent.formGroupSize;
    }

    get formGroupSizeCss(): string {
        return this.formGroupSize == "Normal" ? "form-md" :
            this.formGroupSize == "Small" ? "form-sm" : "form-xs";
    }

    get placeholderLabels(): boolean {
        return this.styleOptions.placeholderLabels != undefined ? this.styleOptions.placeholderLabels : this.parent.placeholderLabels;
    }

    get formControlStaticAsFormControlReadonly(): boolean {
        return this.styleOptions.formControlStaticAsFormControlReadonly != undefined ? this.styleOptions.formControlStaticAsFormControlReadonly : this.parent.formControlStaticAsFormControlReadonly;
    }
    
    get labelColumns(): BsColumns {
        return this.styleOptions.labelColumns != undefined ? this.styleOptions.labelColumns : this.parent.labelColumns;
    }

    get labelColumnsCss(): string {
        return StyleContext.bsColumnsCss(this.labelColumns);
    }

    get valueColumns(): BsColumns {
        return this.styleOptions.valueColumns != undefined ? this.styleOptions.valueColumns : this.parent.valueColumns;
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

    get frame(): EntityFrame<ModifiableEntity> | undefined {
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

export interface StyleOptions {
    formGroupStyle?: FormGroupStyle;
    formGroupSize?: FormGroupSize;
    placeholderLabels?: boolean;
    formControlStaticAsFormControlReadonly?: boolean;
    labelColumns?: BsColumns;
    valueColumns?: BsColumns;
    readOnly?: boolean;
    frame?: EntityFrame<ModifiableEntity>;
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

    
    static root<T extends ModifiableEntity>(type: Type<T>, value: T, styleOptions?: StyleOptions): TypeContext<T> {
        return new TypeContext(undefined, styleOptions, PropertyRoute.root(type), new ReadonlyBinding<T>(value, ""));
    }

    constructor(parent: StyleContext | undefined, styleOptions: StyleOptions | undefined, propertyRoute: PropertyRoute /*| undefined*/, binding: IBinding<T>) {
        super(parent, styleOptions);
        this.propertyRoute = propertyRoute;
        this.binding = binding;
        
        this.prefix = compose(parent && (parent as TypeContext<any>).prefix, binding.suffix);

    }
  
    subCtx(styleOptions: StyleOptions): TypeContext<T>     
    subCtx<R>(property: (val: T) => R, styleOptions?: StyleOptions): TypeContext<R>
    subCtx(propertyOrStyleOptions: ((val: T) => any) | StyleOptions, styleOptions?: StyleOptions): TypeContext<any>
    {
        if (typeof propertyOrStyleOptions != "function")
            return new TypeContext<T>(this, propertyOrStyleOptions, this.propertyRoute, this.binding);
        
        const property = propertyOrStyleOptions as ((val: T) => any);

        const lambdaMembers = getLambdaMembers(property);

        const subRoute = lambdaMembers.reduce<PropertyRoute>((pr, m) => pr.addMember(m), this.propertyRoute);
        
        const binding = createBinding(this.value, property);

        const result = new TypeContext<any>(this, styleOptions, subRoute, binding);

        return result;
    }

    niceName(property?: (val: T) => any): string  {

        if (this.propertyRoute == undefined)
            throw new Error("No propertyRoute");

        if (property == undefined)
            return this.propertyRoute.member!.niceName;

        return this.propertyRoute.add(property).member!.niceName;
    }

    compose(suffix: string): string {
        return compose(this.prefix, suffix);
    }

    tryFindParent<S extends ModifiableEntity>(type: Type<S>): S | undefined {

        let current: TypeContext<any> = this;

        while (current) {
            const entity = current.value as ModifiableEntity;
            if (entity && entity.Type == type.typeName)
                return entity as S;

            current = current.parent as TypeContext<any>;
        }

        return undefined;
    }

    findParent<S extends ModifiableEntity>(type: Type<S>): S {
        const result = this.tryFindParent(type);
        if (result == undefined)
            throw new Error(`No '${type.typeName}' found in the parent chain`);

        return result;
    }


    using(render: (ctx: this) => React.ReactChild): React.ReactChild {
        return render(this);
    }

    mlistItemCtxs<R>(property: (val: T) => MList<R>, styleOptions?: StyleOptions): TypeContext<R>[] {
        return mlistItemContext(this.subCtx(property, styleOptions));
    }

    get propertyPath() {
        return this.propertyRoute ? this.propertyRoute.propertyPath() : undefined;
    }

    get errorClass(): string | undefined {
        return !!this.error ? "has-error" : undefined;
    }
}

export interface ButtonsContext {
    pack: EntityPack<ModifiableEntity>;
    frame: EntityFrame<ModifiableEntity>;
    showOperations: boolean;
    tag?: string;
}

export interface IRenderButtons {
    renderButtons(ctx: ButtonsContext): React.ReactElement<any>[];
}

export interface EntityFrame<T extends ModifiableEntity> {
    frameComponent: React.Component<any, any>;
    entityComponent: React.Component<any, any>;
    onReload: (pack: EntityPack<T>) => void;
    setError: (modelState: ModelState, initialPrefix?: string) => void;
    revalidate: () => void;
    onClose: () => void;
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
            ctx.propertyRoute.addMember({ name: "", type: "Indexer" }),
            new ReadonlyBinding(mle.element, i.toString())));
}




