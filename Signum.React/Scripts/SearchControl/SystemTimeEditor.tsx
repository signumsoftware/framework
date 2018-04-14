import * as moment from 'moment'
import * as React from 'react'
import * as Finder from '../Finder'
import { classes } from '../Globals';
import { openModal, IModalProps } from '../Modals';
import { SystemTime, QueryToken, SubTokensOptions, FilterType, FindOptionsParsed, QueryDescription } from '../FindOptions'
import { SystemTimeMode } from '../Signum.Entities.DynamicQuery'
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../Signum.Entities'
import { ValueLine, EntityLine, EntityCombo } from '../Lines'
import { Binding, IsByAll, getTypeInfos } from '../Reflection'
import { TypeContext, FormGroupStyle } from '../TypeContext'
import QueryTokenBuilder from './QueryTokenBuilder'
import { DateTimePicker } from 'react-widgets';


interface SystemTimeEditorProps extends React.Props<SystemTime> {
    findOptions: FindOptionsParsed;
    queryDescription: QueryDescription;
    onChanged: () => void;
}

export default class SystemTimeEditor extends React.Component<SystemTimeEditorProps>{

    render() {

        var mode = this.props.findOptions.systemTime!.mode;

        return (
            <div className={classes("sf-system-time-editor", "alert alert-primary")}>
                <span style={{ paddingTop: "3px" }}>{JavascriptMessage.showRecords.niceToString()}</span>
                {this.renderMode()}
                {(mode == "Between" || mode == "ContainedIn" || mode == "AsOf") && this.renderDateTime("startDate")}
                {(mode == "Between" || mode == "ContainedIn") && this.renderDateTime("endDate")}
                {this.renderShowPeriod()}
                {this.renderShowOperations()}
            </div>
        );
    }


    handlePeriodClicked = () => {
        var fop = this.props.findOptions;
        if (this.isPeriodChecked()) {
            fop.columnOptions.extract(a => a.token != null && (
                a.token.fullKey.startsWith("Entity.SystemValidFrom") ||
                a.token.fullKey.startsWith("Entity.SystemValidTo")));
            this.props.onChanged();
        }
        else {

            Finder.parseColumnOptions([
                { columnName: "Entity.SystemValidFrom" },
                { columnName: "Entity.SystemValidTo" }
            ], fop.groupResults, this.props.queryDescription).then(cops => {
                fop.columnOptions = [...cops, ...fop.columnOptions];
                this.props.onChanged();
            }).done();
        }
        
    }

    isPeriodChecked() {
        var cos = this.props.findOptions.columnOptions;

        return cos.some(a => a.token != null && (
            a.token.fullKey.startsWith("Entity.SystemValidFrom") ||
            a.token.fullKey.startsWith("Entity.SystemValidTo"))
        );
    }

    renderShowPeriod() {
        return (
            <div className="form-check form-check-inline ml-3">
                <label className="form-check-label" >
                    <input className="form-check-input" type="checkbox" checked={this.isPeriodChecked()} onChange={this.handlePeriodClicked} />
                    {JavascriptMessage.showPeriod.niceToString()}
                </label>
            </div>
        );
    }

    handlePreviousOperationClicked = () => {
        var fop = this.props.findOptions;
        debugger;
        if (this.isPreviousOperationChecked()) {
            fop.columnOptions.extract(a => a.token != null && a.token.fullKey.startsWith("Entity.PreviousOperationLog"));
            this.props.onChanged();
        }
        else {
            
            Finder.parseColumnOptions([
                { columnName: "Entity.PreviousOperationLog.Start" },
                { columnName: "Entity.PreviousOperationLog.User" },
                { columnName: "Entity.PreviousOperationLog.Operation" },
            ], fop.groupResults, this.props.queryDescription).then(cops => {
                fop.columnOptions = [...cops, ...fop.columnOptions];
                this.props.onChanged();
            }).done();
        }

    }

    isPreviousOperationChecked() {
        var cos = this.props.findOptions.columnOptions;

        return cos.some(a => a.token != null && a.token.fullKey.startsWith("Entity.PreviousOperationLog"));
    }

    renderShowOperations() {
        return (
            <div className="form-check form-check-inline ml-3">
                <label className="form-check-label" >
                    <input className="form-check-input" type="checkbox" checked={this.isPreviousOperationChecked()} onChange={this.handlePreviousOperationClicked} />
                    {JavascriptMessage.showPreviousOperation.niceToString()}
                </label>
            </div>
        );
    }

    handleChangeMode = (e: React.ChangeEvent<HTMLSelectElement>) => {
        let st = this.props.findOptions.systemTime!;
        st.mode = e.currentTarget.value as SystemTimeMode;

        st.startDate = st.mode == "All" ? undefined : (st.startDate || moment().format());
        st.endDate = st.mode == "All" || st.mode == "AsOf" ? undefined : (st.endDate || moment().format());

        this.forceUpdate();
    }

    renderMode() {
        var st = this.props.findOptions.systemTime!;

        return (
            <select value={st.mode} className="form-control form-control-sm ml-1" style={{ width: "auto" }} onChange={this.handleChangeMode}>
                {SystemTimeMode.values().map((st, i) => <option key={i} value={st}>{SystemTimeMode.niceToString(st)}</option>)}
            </select>
        );
    }

    renderDateTime(field: "startDate" | "endDate") {

        var systemTime = this.props.findOptions.systemTime!;

        const handleDatePickerOnChange = (date?: Date, str?: string) => {
            const m = moment(date);
            systemTime[field] = m.isValid() ? m.format() : undefined;
        };

        var m = moment(systemTime[field], moment.ISO_8601)
        var momentFormat = "YYYY-MM-DDTHH:mm:ss";
        return (
            <div className="rw-widget-sm ml-1" style={{ width: "230px" }}>
                <DateTimePicker value={m && m.toDate()} onChange={handleDatePickerOnChange}
                    format={momentFormat} time={true} />
            </div>
        );
    }
}



