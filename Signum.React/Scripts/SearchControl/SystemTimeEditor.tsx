import * as moment from 'moment'
import * as React from 'react'
import * as Finder from '../Finder'
import { classes } from '../Globals';
import { openModal, IModalProps } from '../Modals';
import { SystemTime, QueryToken, SubTokensOptions, FilterType } from '../FindOptions'
import { SystemTimeMode } from '../Signum.Entities.DynamicQuery'
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../Signum.Entities'
import { ValueLine, EntityLine, EntityCombo } from '../Lines'
import { Binding, IsByAll, getTypeInfos } from '../Reflection'
import { TypeContext, FormGroupStyle } from '../TypeContext'
import QueryTokenBuilder from './QueryTokenBuilder'
import { DateTimePicker } from 'react-widgets';


interface SystemTimeEditorProps extends React.Props<SystemTime> {
    systenTime: SystemTime;
}

export default class SystemTimeEditor extends React.Component<SystemTimeEditorProps>{
    
    render() {

        var mode = this.props.systenTime.mode;
        
        return (
            <div className={classes("sf-system-time-editor", "alert alert-primary")}>
                <span style={{ paddingTop: "3px" }}>{JavascriptMessage.showRecords.niceToString()}</span>
                {this.renderMode()}
                {(mode == "Between" || mode == "ContainedIn" || mode == "AsOf") && this.renderDateTime("startDate")}
                {(mode == "Between" || mode == "ContainedIn") && this.renderDateTime("endDate")}
            </div>
        );
    }

    handleChangeMode = (e: React.ChangeEvent<HTMLSelectElement>) => {
        let st = this.props.systenTime;
        st.mode = e.currentTarget.value as SystemTimeMode;

        st.startDate = st.mode == "All" ? undefined : (st.startDate || moment().format());
        st.endDate = st.mode == "All" || st.mode == "AsOf" ? undefined : (st.endDate || moment().format());

        this.forceUpdate();
    }

    renderMode() {
        var st = this.props.systenTime;

        return (
            <select value={SystemTimeMode.niceToString(st.mode)} className="form-control form-control-sm ml-1" style={{ width: "auto" }} onChange={this.handleChangeMode}>
                {SystemTimeMode.values().map((st, i) => <option key={i} value={st}>{SystemTimeMode.niceToString(st)}</option>)}
            </select>
        );
    }

    renderDateTime(field: "startDate" | "endDate") {

        const handleDatePickerOnChange = (date?: Date, str?: string) => {
            const m = moment(date);
            this.props.systenTime[field] = m.isValid() ? m.format() : undefined;
        };

        var m = moment(this.props.systenTime[field], moment.ISO_8601)
        var momentFormat = "YYYY-MM-DDTHH:mm:ss";
        return (
            <div className="rw-widget-sm ml-1" style={{ width: "230px" }}>
                <DateTimePicker value={m && m.toDate()} onChange={handleDatePickerOnChange}
                    format={momentFormat} time={true} />
            </div>
        );
    }
}



