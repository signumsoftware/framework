import { DateTime } from 'luxon'
import * as React from 'react'
import * as Finder from '../Finder'
import { classes } from '../Globals';
import { SystemTime, FindOptionsParsed, QueryDescription } from '../FindOptions'
import { SystemTimeMode } from '../Signum.Entities.DynamicQuery'
import { JavascriptMessage } from '../Signum.Entities'
import { DateTimePicker } from 'react-widgets';
import { QueryTokenString, toLuxonFormat } from '../Reflection';
import QueryTokenBuilder from './QueryTokenBuilder';
import { OperationLogEntity } from '../Signum.Entities.Basics';

interface SystemTimeEditorProps extends React.Props<SystemTime> {
  findOptions: FindOptionsParsed;
  queryDescription: QueryDescription;
  onChanged: () => void;
}

export default function SystemTimeEditor(p : SystemTimeEditorProps){


  function handlePeriodClicked() {
    var fop = p.findOptions;
    if (isPeriodChecked()) {
      fop.columnOptions.extract(a => a.token != null && (
        a.token.fullKey.startsWith(QueryTokenString.entity().systemValidFrom().toString()) ||
        a.token.fullKey.startsWith(QueryTokenString.entity().systemValidTo().toString())));
      p.onChanged();
    }
    else {

      Finder.parseColumnOptions([
        { token: QueryTokenString.entity().systemValidFrom() },
        { token: QueryTokenString.entity().systemValidTo() }
      ], fop.groupResults, p.queryDescription).then(cops => {
        fop.columnOptions = [...cops, ...fop.columnOptions];
        p.onChanged();
      }).done();
    }
  }

  function isPeriodChecked() {
    var cos = p.findOptions.columnOptions;

    return cos.some(a => a.token != null && (
      a.token.fullKey.startsWith(QueryTokenString.entity().systemValidFrom().toString()) ||
      a.token.fullKey.startsWith(QueryTokenString.entity().systemValidTo().toString()))
    );
  }

  function renderShowPeriod() {
    return (
      <div className="form-check form-check-inline ml-3">
        <label className="form-check-label" >
          <input className="form-check-input" type="checkbox" checked={isPeriodChecked()} onChange={handlePeriodClicked} />
          {JavascriptMessage.showPeriod.niceToString()}
        </label>
      </div>
    );
  }

  function handlePreviousOperationClicked() {

    var prevLogToken = QueryTokenString.entity().expression<OperationLogEntity>("PreviousOperationLog");

    var fop = p.findOptions;
    if (isPreviousOperationChecked()) {
      fop.columnOptions.extract(a => a.token != null && a.token.fullKey.startsWith(prevLogToken.toString()));
      p.onChanged();
    }
    else {

      Finder.parseColumnOptions([
        { token: prevLogToken.append(a => a.start) },
        { token: prevLogToken.append(a => a.user) },
        { token: prevLogToken.append(a => a.operation) },
      ], fop.groupResults, p.queryDescription).then(cops => {
        fop.columnOptions = [...cops, ...fop.columnOptions];
        p.onChanged();
      }).done();
    }
  }

  function isPreviousOperationChecked() {
    var cos = p.findOptions.columnOptions;

    return cos.some(a => a.token != null && a.token.fullKey.startsWith("Entity.PreviousOperationLog"));
  }

  function renderShowOperations() {
    return (
      <div className="form-check form-check-inline ml-3">
        <label className="form-check-label" >
          <input className="form-check-input" type="checkbox" checked={isPreviousOperationChecked()} onChange={handlePreviousOperationClicked} />
          {JavascriptMessage.showPreviousOperation.niceToString()}
        </label>
      </div>
    );
  }

  function handleChangeMode (e: React.ChangeEvent<HTMLSelectElement>){
    let st = p.findOptions.systemTime!;
    st.mode = e.currentTarget.value as SystemTimeMode;

    st.startDate = st.mode == "All" ? undefined : (st.startDate || DateTime.local().toISO());
    st.endDate = st.mode == "All" || st.mode == "AsOf" ? undefined : (st.endDate || DateTime.local().toISO());

    p.onChanged();
  }

  function renderMode() {
    var st = p.findOptions.systemTime!;

    return (
      <select value={st.mode} className="form-control form-control-sm ml-1" style={{ width: "auto" }} onChange={handleChangeMode}>
        {SystemTimeMode.values().map((st, i) => <option key={i} value={st}>{SystemTimeMode.niceToString(st)}</option>)}
      </select>
    );
  }

  function renderDateTime(field: "startDate" | "endDate") {

    var systemTime = p.findOptions.systemTime!;

    const handleDatePickerOnChange = (date: Date | null | undefined, str: string) => {
      const m = date && DateTime.fromJSDate(date);
      systemTime[field] = m ? m.toISO() : undefined;
      p.onChanged();
    };

    var utcDate = systemTime[field]

    var m = utcDate == null ? null : DateTime.fromISO(utcDate);
    var luxonFormat = toLuxonFormat("o", "DateTime");
    return (
      <div className="rw-widget-sm ml-1" style={{ width: "230px" }}>
        <DateTimePicker value={m?.toJSDate()} onChange={handleDatePickerOnChange}
          valueEditFormat={luxonFormat} valueDisplayFormat={luxonFormat} includeTime={true} messages={{ dateButton: JavascriptMessage.Date.niceToString() }} />
      </div>
    );
  }


  var mode = p.findOptions.systemTime!.mode;

  return (
    <div className={classes("sf-system-time-editor", "alert alert-primary")}>
      <span style={{ paddingTop: "3px" }}>{JavascriptMessage.showRecords.niceToString()}</span>
      {renderMode()}
      {(mode == "Between" || mode == "ContainedIn" || mode == "AsOf") && renderDateTime("startDate")}
      {(mode == "Between" || mode == "ContainedIn") && renderDateTime("endDate")}
      {renderShowPeriod()}
      {renderShowOperations()}
    </div>
  );
}


