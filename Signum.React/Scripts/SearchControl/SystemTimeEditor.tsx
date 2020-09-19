import { DateTime } from 'luxon'
import * as React from 'react'
import * as Finder from '../Finder'
import { classes } from '../Globals';
import { SystemTime, FindOptionsParsed, QueryDescription } from '../FindOptions'
import { SystemTimeMode } from '../Signum.Entities.DynamicQuery'
import { JavascriptMessage } from '../Signum.Entities'
import { DateTimePicker } from 'react-widgets';
import { QueryTokenString } from '../Reflection';
import QueryTokenBuilder from './QueryTokenBuilder';
import { OperationLogEntity } from '../Signum.Entities.Basics';

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
        a.token.fullKey.startsWith(QueryTokenString.entity().systemValidFrom().toString()) ||
        a.token.fullKey.startsWith(QueryTokenString.entity().systemValidTo().toString())));
      this.props.onChanged();
    }
    else {

      Finder.parseColumnOptions([
        { token: QueryTokenString.entity().systemValidFrom() },
        { token: QueryTokenString.entity().systemValidTo() }
      ], fop.groupResults, this.props.queryDescription).then(cops => {
        fop.columnOptions = [...cops, ...fop.columnOptions];
        this.props.onChanged();
      }).done();
    }
  }

  isPeriodChecked() {
    var cos = this.props.findOptions.columnOptions;

    return cos.some(a => a.token != null && (
      a.token.fullKey.startsWith(QueryTokenString.entity().systemValidFrom().toString()) ||
      a.token.fullKey.startsWith(QueryTokenString.entity().systemValidTo().toString()))
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

    var prevLogToken = QueryTokenString.entity().expression<OperationLogEntity>("PreviousOperationLog");

    var fop = this.props.findOptions;
    if (this.isPreviousOperationChecked()) {
      fop.columnOptions.extract(a => a.token != null && a.token.fullKey.startsWith(prevLogToken.toString()));
      this.props.onChanged();
    }
    else {

      Finder.parseColumnOptions([
        { token: prevLogToken.append(a => a.start) },
        { token: prevLogToken.append(a => a.user) },
        { token: prevLogToken.append(a => a.operation) },
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

    st.startDate = st.mode == "All" ? undefined : (st.startDate || asUTC(DateTime.local().toISO()));
    st.endDate = st.mode == "All" || st.mode == "AsOf" ? undefined : (st.endDate || asUTC(DateTime.local().toISO()));

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
      const m = date && DateTime.fromJSDate(date);
      systemTime[field] = m ? asUTC(m.toISO()) : undefined;
    };

    var utcDate = systemTime[field]

    var m = utcDate == null ? null : DateTime.fromISO(asLocal(utcDate));
    var luxonFormat = "yyyy-MM-dd'T'HH:mm:ss";
    return (
      <div className="rw-widget-sm ml-1" style={{ width: "230px" }}>
        <DateTimePicker value={m?.toJSDate()} onChange={handleDatePickerOnChange}
          format={luxonFormat} time={true} />
      </div>
    );
  }
}

export function asUTC(date: string): string {

  if (date.contains("+"))
    return date.tryBefore("+") + "Z"; //Hack

  return date;
}

export function asLocal(date: string): string {
  if (date.contains("Z"))
    return date.before("Z");

  return date;
}


