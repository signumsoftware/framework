import * as React from 'react'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { getTypeInfo, Binding, PropertyRoute } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { DynamicTypeDesignContext } from './DynamicTypeDefinitionComponent'


export interface ValueComponentProps {
    binding: Binding<any>;
    dc: DynamicTypeDesignContext;
    type: "number" | "string" | "boolean" | "textArea" | null;
    options?: (string | number)[];
    labelClass?: string;
    defaultValue: number | string | boolean | null;
    avoidDelete?: boolean;
    hideLabel?: boolean;
    labelColumns?: number;
    autoOpacity?: boolean;
    onBlur?: () => void;
    onChange?: () => void;
}

export default class ValueComponent extends React.Component<ValueComponentProps> {

    updateValue(value: string | boolean | undefined) {
        var p = this.props;

        var parsedValue = p.type != "number" ? value : (parseFloat(value as string) || null);

        if (parsedValue === "")
            parsedValue = null;

        if (parsedValue == p.defaultValue && !p.avoidDelete)
            p.binding.deleteValue();
        else
            p.binding.setValue(parsedValue);

        if (p.onChange)
            p.onChange();

        p.dc.refreshView();
    }

    handleChangeCheckbox = (e: React.ChangeEvent<any>) => {
        var sender = (e.currentTarget as HTMLInputElement);
        this.updateValue(sender.checked);
    }

    handleChangeSelectOrInput = (e: React.ChangeEvent<any>) => {
        var sender = (e.currentTarget as HTMLSelectElement | HTMLInputElement);
        this.updateValue(sender.value);
    }


    render() {
        const p = this.props;
        const value = p.binding.getValue();


        var opacity = p.autoOpacity && value == null ? { opacity: 0.5 } as React.CSSProperties: undefined; 

        if (this.props.hideLabel) {
            return (
                <div className="form-inline form-sm" style={opacity}>
                    {this.renderValue(value)}
                </div>
            );
        }

        const lc = this.props.labelColumns;

        return (
            <div className="form-group form-group-sm row" style={opacity}>
                <label className={classes("col-form-label col-form-label-sm", this.props.labelClass, "col-sm-" + (lc == null ? 2 : lc))}>
                    {this.props.binding.member}
                </label>
                <div className={"col-sm-" + (lc == null ? 10 : 12 -lc)}>
                    {this.renderValue(value)}
                </div>
            </div>
        );
    }

    renderValue(value: number | string | boolean | null | undefined) {
        
        const val = value === undefined ? this.props.defaultValue : value;

        const style = this.props.hideLabel ? { display: "inline-block" } as React.CSSProperties : undefined;

        if (this.props.options) {
            return (
                <select className="form-control form-control-sm" style={style} onBlur={this.props.onBlur}
                    value={val == null ? "" : val.toString()} onChange={this.handleChangeSelectOrInput} >
                    {val == null && <option value="">{" - "}</option>}
                    {this.props.options.map((o, i) =>
                        <option key={i} value={o.toString()}>{o.toString()}</option>)
                    }
                </select>);
        }
        else {

            if (this.props.type == "boolean") {
                return (<input
                    type="checkbox" onBlur={this.props.onBlur}
                    className="form-control"
                    checked={value == undefined ? this.props.defaultValue as boolean : value as boolean}
                    onChange={this.handleChangeCheckbox} />
                );
            }

            if (this.props.type == "textArea") {
                return (<textarea className="form-control form-control-sm" style={style} onBlur={this.props.onBlur}
                    value={val == null ? "" : val.toString()}
                    onChange={this.handleChangeSelectOrInput} />);
            }

            return (<input className="form-control form-control-sm" style={style} onBlur={this.props.onBlur}
                type="text"
                value={val == null ? "" : val.toString()}
                onChange={this.handleChangeSelectOrInput} />);
        }
    }
}