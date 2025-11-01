import { DateTime, Duration, DurationUnit } from 'luxon'
import * as React from 'react'
import { FormCheck } from 'react-bootstrap'
import { Finder } from '../Finder'
import { classes } from '../Globals';
import { SystemTime, FindOptionsParsed, QueryDescription, SubTokensOptions } from '../FindOptions'
import { SystemTimeJoinMode, SystemTimeMode, TimeSeriesUnit } from '../Signum.DynamicQuery'
import { JavascriptMessage } from '../Signum.Entities'
import { DateTimePicker } from 'react-widgets-up';
import { QueryTokenString, toLuxonFormat, toNumberFormat } from '../Reflection';
import { OperationLogEntity } from '../Signum.Operations';
import { AggregateFunction, QueryTokenDateMessage } from '../Signum.DynamicQuery.Tokens';
import { isNumberKey, NumberBox } from '../Lines/NumberLine';
import SearchValue from './SearchValue';
import { useAPI, useForceUpdate } from '../Hooks';

interface SystemTimeEditorProps {
  findOptions: FindOptionsParsed;
  queryDescription: QueryDescription;
  onChanged: () => void;
}

export default function SystemTimeEditor(p : SystemTimeEditorProps): React.ReactElement{

  function renderShowPeriod() {

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

    return (
      <label className="ms-3">
        <input className="form-check-input me-1" type="checkbox" checked={isPeriodChecked()} onChange={handlePeriodClicked} />
        {JavascriptMessage.showPeriod.niceToString()}
      </label>
    );
  }

  function renderShowOperations() {

    function isPreviousOperationChecked() {
      var cos = p.findOptions.columnOptions;

      return cos.some(a => a.token != null && a.token.fullKey.startsWith("Entity.PreviousOperationLog"));
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

    return (
      <label className="ms-3" >
        <input className="form-check-input me-1" type="checkbox" checked={isPreviousOperationChecked()} onChange={handlePreviousOperationClicked} />
        {JavascriptMessage.showPreviousOperation.niceToString()}
      </label>
    );
  }

  function isInterval(mode: SystemTimeMode) {
    return mode == "All" || mode == "Between" || mode == "ContainedIn";
  }

  function renderMode() {

    async function handleChangeMode(e: React.ChangeEvent<HTMLSelectElement>) {
      let st = p.findOptions.systemTime!;
      st.mode = e.currentTarget.value as SystemTimeMode;

      st.startDate = st.mode == "All" ? undefined : (st.startDate || DateTime.local().toISO()!);
      st.endDate = st.mode == "All" || st.mode == "AsOf" ? undefined : (st.endDate || DateTime.local().toISO()!);
      st.joinMode = isInterval(st.mode) ? "FirstCompatible" : undefined;
      st.timeSeriesStep = st.mode == "TimeSeries" ? 1 : undefined;
      st.timeSeriesUnit = st.mode == "TimeSeries" ? "Day" : undefined;
      st.timeSeriesMaxRowsPerStep = st.mode == "TimeSeries" ? 10 : undefined;
      st.splitQueries = st.mode == "TimeSeries" ? true : undefined;

      if (st.mode == "TimeSeries") {
        var token = await Finder.parseSingleToken(p.findOptions.queryKey, QueryTokenString.timeSeries.token, SubTokensOptions.CanTimeSeries);

        p.findOptions.columnOptions.insertAt(0, { token: token, displayName: token.niceName });
        p.findOptions.orderOptions.insertAt(0, { token: token, orderType: "Ascending" });

      } else {
        p.findOptions.columnOptions.extract(a => a.token != null && a.token.fullKey == QueryTokenString.timeSeries.token);
        p.findOptions.orderOptions.extract(a => a.token != null && a.token.fullKey == QueryTokenString.timeSeries.token);
      }

      p.onChanged();
    }

    return (
      <select value={p.findOptions.systemTime!.mode} className="form-select form-select-xs ms-1" style={{ width: "auto" }} onChange={handleChangeMode}>
        {SystemTimeMode.values().map((stm, i) => <option key={i} value={stm}>{SystemTimeMode.niceToString(stm)}</option>)}
      </select>
    );
  }

  function renderJoinMode() {

    function handleChangeJoinMode(e: React.ChangeEvent<HTMLSelectElement>) {
      let st = p.findOptions.systemTime!;
      st.joinMode = e.currentTarget.value as SystemTimeJoinMode;

      p.onChanged();
    }

    return (
      <select value={p.findOptions.systemTime!.joinMode} className="form-select form-select-xs ms-1" style={{ width: "auto" }} onChange={handleChangeJoinMode}>
        {SystemTimeJoinMode.values().map((stjm, i) => <option key={i} value={stjm}>{SystemTimeJoinMode.niceToString(stjm)}</option>)}
      </select>
    );
  }

  function renderTimeSeriesUnit() {

    function handleTimeSeriesUnit(e: React.ChangeEvent<HTMLSelectElement>) {
      let st = p.findOptions.systemTime!;
      st.timeSeriesUnit = e.currentTarget.value as TimeSeriesUnit;
      st.timeSeriesStep = 1;

      p.onChanged();
    }

 
    return (
      <select value={p.findOptions.systemTime!.timeSeriesUnit} className="form-select form-select-xs ms-1" style={{ width: "auto" }} onChange={handleTimeSeriesUnit}>
        {TimeSeriesUnit.values().map((stm, i) => <option key={i} value={stm}>{TimeSeriesUnit.niceToString(stm)}</option>)}
      </select>
    );
  }

  function renderTimeSerieStep() {

    const st = p.findOptions.systemTime!;
    function handleTimeSerieStep(e: number | null | undefined) {
      st.timeSeriesStep = e ?? 1;

      p.onChanged();
    }

    var numberFormat = toNumberFormat("0");

    return (
      <NumberBox value={p.findOptions.systemTime!.timeSeriesStep} validateKey={isNumberKey} format={numberFormat}
        htmlAttributes={{ className: "form-control form-control-xs ms-1", style: { width: "40px" } }}
        onChange={handleTimeSerieStep} />
    );
  }

  function renderDateTime(field: "startDate" | "endDate") {

    var systemTime = p.findOptions.systemTime!;

    const handleDatePickerOnChange = (date: Date | null | undefined, str: string) => {
      const m = date && DateTime.fromJSDate(date);
      systemTime[field] = m ? m.toISO()! : undefined;
      p.onChanged();
    };

    var utcDate = systemTime[field]

    var m = utcDate == null ? null : DateTime.fromISO(utcDate);
    var luxonFormat = toLuxonFormat("G", "DateTime");
    return (
      <div className="d-flex ms-1">
        {AggregateFunction.niceToString(field == "startDate" ? "Min" : "Max")}
        <div className="rw-widget-xs ms-1">
          <DateTimePicker value={m?.toJSDate()} onChange={handleDatePickerOnChange}
            inputProps={{ style: { width: "150px" } }}
            valueEditFormat={luxonFormat} valueDisplayFormat={luxonFormat} includeTime={true} messages={{ dateButton: JavascriptMessage.Date.niceToString() }} />
        </div>
      </div>
  
    );
  }


  var mode = p.findOptions.systemTime!.mode;

  return (
    <div className={classes("sf-system-time-editor", "alert alert-primary")}>
      <span>{JavascriptMessage.showRecords.niceToString()}</span>
      {renderMode()}
      {mode == QueryTokenString.timeSeries.token ? <>
        <span className="ms-2 d-flex">{QueryTokenDateMessage.Every01.niceToString().formatHtml(renderTimeSerieStep(), renderTimeSeriesUnit())}</span>
        {renderDateTime("startDate")}
        {renderDateTime("endDate")}
        <TotalNumStepsAndRows findOptions={p.findOptions} />
      </> :
        <>
          {(mode == "Between" || mode == "ContainedIn" || mode == "AsOf") && renderDateTime("startDate")}
          {(mode == "Between" || mode == "ContainedIn") && renderDateTime("endDate")}
          {isInterval(mode) && <>
            <span className="ms-3">{JavascriptMessage.joinMode.niceToString()}</span>
            {renderJoinMode()}
          </>}
          {renderShowPeriod()}
          {renderShowOperations()}
      
        </>
      }
    </div>
  );
}

function TotalNumStepsAndRows(p: { findOptions: FindOptionsParsed }) {

  const st = p.findOptions.systemTime!;

  const isOneValue = p.findOptions.groupResults && p.findOptions.columnOptions.every(a => a.token == null || a.token.fullKey == QueryTokenString.timeSeries.token || a.token.queryTokenType == "Aggregate");

  const forceUpdate = useForceUpdate();
  React.useEffect(() => {
    if (isOneValue) {
      if (st.timeSeriesMaxRowsPerStep != 1) {
        st.timeSeriesMaxRowsPerStep = 1;
        forceUpdate();
      }
    } else {
      if (st.timeSeriesMaxRowsPerStep == 1) {
        st.timeSeriesMaxRowsPerStep = 10;
        forceUpdate();
      }
    }
  }, [isOneValue])

  if (st.startDate == null || st.endDate == null || st.timeSeriesStep == null || st.timeSeriesUnit == null)
    return null;

  const min = DateTime.fromISO(st.startDate!);
  const max = DateTime.fromISO(st.endDate!);
  const unit: DurationUnit = st.timeSeriesUnit.firstLower() as DurationUnit;
  const steps = Math.ceil(max.diff(min, unit).as(unit));

  const formatter = toNumberFormat("C0");

  return (
    <div className="ms-1 d-flex">
      <span>
        {QueryTokenDateMessage._0Steps1Rows2TotalRowsAprox.niceToString().formatHtml(
          <strong className={steps > 1000 ? "text-danger" : undefined}>{formatter.format(steps)}</strong>,
          <NumberBox validateKey={isNumberKey} value={st.timeSeriesMaxRowsPerStep} format={formatter} onChange={e => { st.timeSeriesMaxRowsPerStep = e ?? 10; forceUpdate(); }}
            htmlAttributes={{ className: "form-control form-control-xs ms-1", style: { width: "40px", display: "inline-block" } }}
          />,
          <strong className={st.timeSeriesMaxRowsPerStep != null && steps * st.timeSeriesMaxRowsPerStep > 1000 ? "text-danger" : undefined}>
            {st.timeSeriesMaxRowsPerStep == null ? "" : formatter.format(steps * st.timeSeriesMaxRowsPerStep)}
          </strong>
        )}
      </span>
      <FormCheck
        className="ms-2"
        checked={st.splitQueries}
        onChange={e => { st.splitQueries = e.currentTarget.checked; forceUpdate(); }}
        label={QueryTokenDateMessage.SplitQueries.niceToString()}
        id={`split-queries`}
      />
    </div>
  );
}
