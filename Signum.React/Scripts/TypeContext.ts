import { PropertyRoute, PropertyRouteType, getLambdaMembers, createSetter } from 'Framework/Signum.React/Scripts/Reflection'

export class StyleContext {
    private styleOptions: StyleOptions;
    parent: StyleContext;

    constructor(parent: StyleContext, styleOptions: StyleOptions) {
        this.parent = parent;
        this.styleOptions = styleOptions;

        if (styleOptions.labelColumns && !styleOptions.valueColumns)
            styleOptions.valueColumns = StyleContext.bsColumnsInvert(styleOptions.labelColumns);
    }

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
        return this.styleOptions.readOnly != null ? this.styleOptions.readOnly : this.parent.readOnly;
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

export interface BsColumns {
    xs?: number;
    sm?: number;
    md?: number;
    lg?: number;
}

export enum FormGroupSize {
    Normal,
    Small,
    ExtraSmall,
}

export class TypeContext<T> extends StyleContext {

    propertyRoute: PropertyRoute;
    setValue: (val: T) => void;
    private _value: T;

    get value() {
        return this._value;
    }

    constructor(parent: StyleContext, styleOptions: StyleOptions, propertyRoute: PropertyRoute, value: T) {
        super(parent, styleOptions);
        this.propertyRoute = propertyRoute;
        this.value = value;
    }

    
    subCtx<R>(property: (val: T) => R, styleOptions?: StyleOptions): TypeContext<R> {

        var route = this.propertyRoute.add(property);

        var value = property(this.value);

        var result = new TypeContext<R>(this, styleOptions, route, value);

        result.setValue = value => createSetter(property)(this.value, value);

        return result;
    }
}




