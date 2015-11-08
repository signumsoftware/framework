// Type definitions for React Widgets 3.1.0
// Project: https://github.com/jquense/react-widgets
// Definitions by: Olmo del Corral <https://github.com/olmobrutall>
// Definitions: https://github.com/borisyankov/DefinitelyTyped

///<reference path='../react/react.d.ts' />

declare module ReactWidgets {
    import React = __React;
    
    export interface DropDownListProps {
        value?: any;
        onChange?: (value: any) => void;
        onSelect?: (value: any) => void;
        data?: any[];
        valueField?: string;
        textField?: string | ((value: any) => string);
        valueComponent?: React.ReactElement<any>;
        itemComponent?: React.ReactElement<any>;
        disabled?: boolean | any[];
        readonly?: boolean | any[];
        groupBy?: string | ((value: any) => string);
        groupComponent?: React.ReactElement<any>;
        placeholder?: string;
        searchTerm?: string;
        onSearch?: (searchTerm: string) => void;
        open?: boolean;
        onToggle?: (isOpen: boolean) => void;
        filter?: string | ((dataItem: any, searchTerm: string) => boolean);
        caseSensitive?: boolean;
        minLength?: number;
        busy?: boolean;
        duration?: number; //default 250, in milliseconds
        isRtl?: boolean;
        messages?: {
            open?: string | ((props: DropDownListProps) => string); //Default "Open Dropwdown";
            filterPlaceholder?: string | ((props: DropDownListProps) => string);
            emptyList?: string | ((props: DropDownListProps) => string); //Default "There are no items in the list"
            emptyFilter?: string | ((props: DropDownListProps) => string); //Default "The filter returned no results"
        };
    }

    export interface DropdownList extends React.ReactElement<DropDownListProps> {

    }

    export interface ComboboxProps {
        value?: any;
        onChange?: (value: any) => void;
        onSelect?: (value: any) => void;
        data?: any[];
        valueField?: string;
        textField?: string | ((value: any) => string);
        itemComponent?: React.ReactElement<any>;
        disabled?: boolean | any[];
        readonly?: boolean | any[];
        groupBy?: string | ((value: any) => string);
        groupComponent?: React.ReactElement<any>;
        suggest?: boolean;
        filter?: boolean | string | ((dataItem: any, searchTerm: string) => boolean);
        caseSensitive?: boolean;
        minLength?: number;
        open?: boolean;
        onToggle?: (isOpen: boolean) => void;
        busy?: boolean;
        duration?: number; //default 250, in milliseconds
        isRtl?: boolean;
        messages?: {
            open?: string | ((props: ComboboxProps) => string); //Default "Open Combobox";
            emptyList?: string | ((props: ComboboxProps) => string); //Default "There are no items in the list"
            emptyFilter?: string | ((props: ComboboxProps) => string); //Default "The filter returned no results"
        };
    }

    export interface Combobox  extends React.ReactElement<ComboboxProps> {

    }

    export interface NumberPickerProps {
        value?: number;
        onChange?: (value: number) => void;
        parse?: ((string: string, culture: string) => number) | string[];
        min?: number;
        max?: number;
        step?: number;
        precission?: number;
        isRtl?: boolean;
       
        messages?: {
            increment?: string;
            decrement?: string;
        };
    }

    export interface NumberPicker extends React.ReactElement<NumberPickerProps> {

    }

    export interface MultiselectProps {
        value?: any[];
        onChange?: (values: any[]) => void;
        onSelect?: (value: any) => void;
        onCreate?: (value: any) => void;
        data?: any[];
        valueField?: string; //id
        textField?: string | ((value: any) => string); //description
        tagComponent?: React.ReactElement<any>;
        itemComponent?: React.ReactElement<any>;
        groupBy?: string | ((value: any) => string); 
        groupComponent?: React.ReactElement<any>;
        placeholder?: string;
        searchTerm?: string;
        onSearch?: (searchTerm: string) => void;
        open?: boolean;
        onToggle?: (isOpen: boolean) => void;
        filter?: string | ((dataItem: any, searchTerm: string) => boolean);
        caseSensitive?: boolean;
        minLength?: boolean;
        busy?: boolean;
        duration?: number;
        disabled?: boolean | any[];
        readonly?: boolean | any[];
        isRtl?: boolean;
        messages?: {
            createNew?: string | ((props: DropDownListProps) => string); //Default "(create new tag)";
            emptyList?: string | ((props: DropDownListProps) => string); //Default "There are no items in the list"
            emptyFilter?: string | ((props: DropDownListProps) => string); //Default "The filter returned no results"
        };
    }

    export interface Multiselect extends React.ReactElement<MultiselectProps> {

    }

    export interface SelectListProps {
        value?: any|any[];
        onChange?: (values: any | any[]) => void;
        data?: any[];
        valueField?: string; //id
        textField?: string | ((value: any) => string); //description
        multiple?: boolean;
        itemComponent?: React.ReactElement<any>;
        groupBy?: string | ((value: any) => string);
        groupComponent?: React.ReactElement<any>;
        onMove?: (list: HTMLElement, focusedNode: HTMLElement, focusedItem: any) => void;
        busy?: boolean;
        duration?: number;
        disabled?: boolean | any[];
        readonly?: boolean | any[];
        isRtl?: boolean;
        messages?: {
            emptyList?: string | ((props: SelectListProps) => string); //Default "There are no items in the list"
        };
    }

    export interface SelectList extends React.ReactElement<SelectListProps> {

    }


    export interface CalendarProps {
        value?: Date;
        onChange?: (value: Date) => void;
        onNavigate?: (value: Date, direction: string, view: string) => void;
        min?: Date;
        max?: Date;
        footer?: boolean;
        dayComponent?: React.ReactElement<any>;
        initalView?: string; //"month" | "year" | "decade" | "century";
        finalView?: string; //"month" | "year" | "decade" | "century";
        headerFormat?: string; 
        footerFormat?: string; 
        dayFormat?: string; 
        dateFormat?: string; 
        monthFormat?: string; 
        yearFormat?: string; 
        decadeFormat?: string; 
        centuryFormat?: string; 
        isRtl?: boolean;
        messages?: {
            moveBack?: string ; //Default "navigate back";
            moveForward?: string; //Default "navigate forward";
        };
    }

    export class Calendar extends React.Component<CalendarProps, any> {

    }

    export interface DatePickeProps  {
        value?: Date;
        onChange?: (value: Date, dateStr: string) => void;
        onSelect?: (value: Date) => void;
        onNavigate?: (value: Date, direction: string, view: string) => void;
        calendar?: boolean;
        time?: boolean;
        min?: Date;
        max?: Date;
        format?: string;
        editFormat?: string;
        timeFormat?: string;
        step?: number; //minutes
        parse?: ((str: string) => Date) | string[];
        footer?: boolean;
        dayComponent?: React.ReactElement<any>;
        initalView?: string; //"month" | "year" | "decade" | "century";
        finalView?: string; //"month" | "year" | "decade" | "century";
        open?: boolean | string; //false "calendar" "time"
        onToggle?: (isOpen: boolean) => void;
        duration?: number;
        headerFormat?: string;
        footerFormat?: string;
        dayFormat?: string;
        dateFormat?: string;
        monthFormat?: string;
        yearFormat?: string;
        decadeFormat?: string;
        centuryFormat?: string;
        isRtl?: boolean;
        messages?: {
            calendarButton?: string; //Default "Select Date";
            timeButton?: string; //Default "Select Time";
            moveBack?: string; //Default "navigate back";
            moveForward?: string; //Default "navigate forward";
        };
    }

    export interface DatePicker extends React.ReactElement<DatePickeProps> {

    }
}

declare module "react-widgets" {
    export = ReactWidgets;
}
