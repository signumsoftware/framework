import { PropertyRoute } from 'Framework/Signum.React/Scripts/Meta'


export class TypeContext<T> {

    constructor(parent: TypeContext<any>, value: T, propertyRoute: PropertyRoute, styleOptions: StyleOptions) {
        this.value = value;
        this.propertyRoute = propertyRoute;
        this.styleOptions = styleOptions;
    }

    parent: TypeContext<any>;
    value: T;
    propertyRoute: PropertyRoute;
    private styleOptions: StyleOptions;

    get formGroupStyle(): FromGroupStyle {
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

    get valueColumns(): BsColumns {
        return this.styleOptions.valueColumns != null ? this.styleOptions.valueColumns : this.parent.valueColumns;
    }

    get readOnly(): boolean {
        return this.styleOptions.readOnly != null ? this.styleOptions.readOnly : this.parent.readOnly;
    }

    subCtx<R>(property: (val: T) => R, styleOptions?: StyleOptions): TypeContext<R> {

        if (styleOptions && styleOptions.labelColumns && !styleOptions.valueColumns)
            styleOptions.valueColumns = TypeContext.invert(styleOptions.labelColumns);

        return new TypeContext<R>(this, property(this.value), this.propertyRoute.add(property), styleOptions);
    }

    private static invert(bs: BsColumns): BsColumns {
        return {
            xs: bs.xs ? (12 - bs.xs) : undefined,
            sm: bs.sm ? (12 - bs.sm) : undefined,
            md: bs.md ? (12 - bs.md) : undefined,
            lg: bs.lg ? (12 - bs.lg) : undefined,
        };
    }
}

export interface StyleOptions {

    formGroupStyle?: FromGroupStyle;
    formGroupSize?: FormGroupSize;
    placeholderLabels?: boolean;
    formControlStaticAsFormControlReadonly?: boolean;
    labelColumns?: BsColumns;
    valueColumns?: BsColumns;
    readOnly?: boolean;
}

export enum FromGroupStyle {
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
