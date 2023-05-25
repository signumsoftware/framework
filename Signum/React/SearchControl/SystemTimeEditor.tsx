import { DateTime } from 'luxon'
import * as React from 'react'
import * as Finder from '../Finder'
import { classes } from '../Globals';
import { SystemTime, FindOptionsParsed, QueryDescription } from '../FindOptions'
import { SystemTimeJoinMode, SystemTimeMode } from '../Signum.DynamicQuery'
import { JavascriptMessage } from '../Signum.Entities'
import { DateTimePicker } from 'react-widgets';
import { QueryTokenString, toLuxonFormat } from '../Reflection';
import QueryTokenBuilder from './QueryTokenBuilder';
import { OperationLogEntity } from '../Signum.Operations';

interface SystemTimeEditorProps {
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
      });
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
      <label className="ms-3">
        <input className="form-check-input me-1" type="checkbox" checked={isPeriodChecked()} onChange={handlePeriodClicked} />
        {JavascriptMessage.showPeriod.niceToString()}
      </label>
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
      });
    }
  }

  function isPreviousOperationChecked() {
    var cos = p.findOptions.columnOptions;

    return cos.some(a => a.token != null && a.token.fullKey.startsWith("Entity.PreviousOperationLog"));
  }

  function renderShowOperations() {
    return (
      <label className="ms-3" >
        <input className="form-check-input me-1" type="checkbox" checked={isPreviousOperationChecked()} onChange={handlePreviousOperationClicked} />
        {JavascriptMessage.showPreviousOperation.niceToString()}
      </label>
    );
  }

  function handleChangeMode (e: React.ChangeEvent<HTMLSelectElement>){
    let st = p.findOptions.systemTime!;
    st.mode = e.currentTarget.value as SystemTimeMode;

    st.startDate = st.mode == "All" ? undefined : (st.startDate || DateTime.local().toISO());
    st.endDate = st.mode == "All" || st.mode == "AsOf" ? undefined : (st.endDate || DateTime.local().toISO());
    st.joinMode = isInterval(st.mode)  ? "FirstCompatible" : undefined;

    p.onChanged();
  }

  function handleChangeJoinMode(e: React.ChangeEvent<HTMLSelectElement>) {
    let st = p.findOptions.systemTime!;
    st.joinMode = e.currentTarget.value as SystemTimeJoinMode;

    p.onChanged();
  }

  function isInterval(mode: SystemTimeMode) {
    return mode == "All" || mode == "Between" || mode == "ContainedIn";
  }

  function renderMode() {

    return (
      <select value={p.findOptions.systemTime!.mode} className="form-select form-select-sm ms-1" style={{ width: "auto" }} onChange={handleChangeMode}>
        {SystemTimeMode.values().map((stm, i) => <option key={i} value={stm}>{SystemTimeMode.niceToString(stm)}</option>)}
      </select>
    );
  }

  function renderJoinMode() {
    
    return (
      <select value={p.findOptions.systemTime!.joinMode} className="form-select form-select-sm ms-1" style={{ width: "auto" }} onChange={handleChangeJoinMode}>
        {SystemTimeJoinMode.values().map((stjm, i) => <option key={i} value={stjm}>{SystemTimeJoinMode.niceToString(stjm)}</option>)}
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
      <div className="rw-widget-sm ms-1" style={{ width: "230px" }}>
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
      {isInterval(mode) && <>
        <span style={{ paddingTop: "3px" }} className="ms-3">{JavascriptMessage.joinMode.niceToString()}</span>
        {renderJoinMode()}
        </>}
      {renderShowPeriod()}
      {renderShowOperations()}
    </div>
  );
}


