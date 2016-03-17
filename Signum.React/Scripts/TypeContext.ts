import { PropertyRoute, PropertyRouteType, getLambdaMembers, IBinding, ReadonlyBinding, createBinding, LambdaMemberType, Type } from './Reflection'
import { ModelState, MList, ModifiableEntity } from './Signum.Entities'

export enum FormGroupStyle {
    /// Unaffected by FormGroupSize
    None,
    
    /// Requires form-vertical container
    Basic,

    /// Requires form-vertical container
    BasicDown,

    /// Requires form-vertical / form-inline container
    SrOnly,

    /// Requires form-horizontal (default),  affected by LabelColumns / ValueColumns
    LabelColumns,
}

export enum FormGroupSize {
    Normal,
    Small,
    ExtraSmall,
}

export class StyleContext {
    styleOptions: StyleOptions;
    parent: StyleContext;

    constructor(parent: StyleContext, styleOptions: StyleOptions) {
        this.parent = parent || StyleContext.default;
        this.styleOptions = styleOptions || {};

        if (this.styleOptions.labelColumns && !this.styleOptions.valueColumns)
            this.styleOptions.valueColumns = StyleContext.bsColumnsInvert(this.styleOptions.labelColumns);
    }

    static default: StyleContext = new StyleContext(null,
    {
        formGroupStyle : FormGroupStyle.LabelColumns,
        formGroupSize : FormGroupSize.Small,
        labelColumns: { sm: 2 },
        readOnly : false,
        placeholderLabels : false,
        formControlStaticAsFormControlReadonly : false,
    });

    get formGroupStyle(): FormGroupStyle {
        return this.styleOptions.formGroupStyle != null ? this.styleOptions.formGroupStyle : this.parent.formGroupStyle;
    }

    get formGroupSize(): FormGroupSize {
        return this.styleOptions.formGroupSize != null ? this.styleOptions.formGroupSize : this.parent.formGroupSize;
    }

    get formGroupSizeCss(): string {
        return this.formGroupSize == FormGroupSize.Normal ? "form-md" :
            this.formGroupSize == FormGroupSize.Small ? "form-sm" : "form-xs";
    }

    get placeholderLabels(): boolean {
        return this.styleOptions.placeholderLabels != null ? this.styleOptions.placeholderLabels : this.parent.placeholderLabels;
    }

    get formControlStaticAsFormControlReadonly(): boolean {
        return this.styleOptions.formControlStaticAsFormControlReadonly != null ? this.styleOptions.formControlStaticAsFormControlReadonly : this.parent.formControlStaticAsFormControlReadonly;
    }
    
    get labelColumns(): BsColumns {
        return this.styleOptions.labelColumns != null ? this.styleOptions.labelColumns : this.parent.labelColumns;
    }

    get labelColumnsCss(): string {
        return StyleContext.bsColumnsCss(this.labelColumns);
    }

    get valueColumns(): BsColumns {
        return this.styleOptions.valueColumns != null ? this.styleOptions.valueColumns : this.parent.valueColumns;
    }

    get valueColumnsCss(): string {
        return StyleContext.bsColumnsCss(this.valueColumns);
    }

    get readOnly(): boolean {
        return this.styleOptions.readOnly != null ? this.styleOptions.readOnly :
            this.parent ? this.parent.readOnly : false;
    }

    set readOnly(value: boolean) {
        this.styleOptions.readOnly = value;
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
            sm: bs.sm ? (12 - bs.sm) : undefined,
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
        return this.binding.getValue();
    }

    set value(val: T) {
        this.binding.setValue(val);
    }

    static root<T extends ModifiableEntity>(type: Type<T>, value: T) {
        return new TypeContext(null, null, PropertyRoute.root(type), new ReadonlyBinding(value, ""));
    }

    constructor(parent: StyleContext, styleOptions: StyleOptions, propertyRoute: PropertyRoute, binding: IBinding<T>) {
        super(parent, styleOptions);
        this.propertyRoute = propertyRoute;
        this.binding = binding;
        
        this.prefix = compose(parent && (parent as TypeContext<any>).prefix, binding.suffix);

    }

    subCtx(styleOptions?: StyleOptions): TypeContext<T>     
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

    niceName(property?: (val: T) => any) {

        if (property == null)
            return this.propertyRoute.member.niceName;

        return this.propertyRoute.add(property).member.niceName;
    }

    compose(suffix: string) {
        return compose(this.prefix, suffix);
    }

    tryFindParent<S extends ModifiableEntity>(type: Type<S>): S {

        var current: TypeContext<any> = this;

        while (current) {
            var entity = current.value as ModifiableEntity;
            if (entity && entity.Type == type.typeName)
                return entity as S;

            current = current.parent as TypeContext<any>;
        }

        return null;
    }

    findParent<S extends ModifiableEntity>(type: Type<S>): S {
        var result = this.tryFindParent(type);
        if (result == null)
            throw new Error(`No '${type.typeName}' found in the parent chain`);

        return result;
    }
}


function compose(prefix: string, suffix: string){
    if (!prefix || prefix == "")
        return suffix;

    if (!suffix || suffix == "")
        return prefix;

    return prefix + "_" + suffix;
}

export function mlistItemContext<T>(ctx: TypeContext<MList<T>>): TypeContext<T>[] {
    
    return ctx.value.map((mle, i) =>
        new TypeContext<T>(ctx, null,
            ctx.propertyRoute.addMember({ name: "", type: LambdaMemberType.Indexer }),
            new ReadonlyBinding(mle.element, i.toString())));
}




