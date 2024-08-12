import { DateTime, DurationUnit } from 'luxon'
import * as React from 'react'
import { FindOptionsParsed } from '../../../Signum/React/Search';
import { QueryDescription } from '../../../Signum/React/FindOptions';
import { QueryTokenString, toLuxonFormat, toNumberFormat } from '@framework/Reflection';
import { JavascriptMessage } from '@framework/Signum.Entities';
import { TimeSeriesUnit } from '../../../Signum/React/Signum.DynamicQuery';
import { isNumberKey, NumberBox } from '../../../Signum/React/Lines/NumberLine';
import { AggregateFunction, QueryTokenDateMessage } from '../../../Signum/React/Signum.DynamicQuery.Tokens';
import { DateTimePicker } from 'react-widgets';
import { classes } from '../../../Signum/React/Globals';
import { useForceUpdate } from '../../../Signum/React/Hooks';

interface ChartTimeSeriesEditorProps {
  findOptions: FindOptionsParsed;
  queryDescription: QueryDescription;
  onChanged: () => void;
}

export default function ChartTimeSeriesEditor(p: ChartTimeSeriesEditorProps): React.JSX.Element {

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

  return (
    <div className={classes("sf-system-time-editor", "alert alert-primary")}>
      <span>{JavascriptMessage.showRecords.niceToString()}</span>
      <span className="ms-2 d-flex">{QueryTokenDateMessage.Every01.niceToString().formatHtml(renderTimeSerieStep(), renderTimeSeriesUnit())}</span>
      {renderDateTime("startDate")}
      {renderDateTime("endDate")}
      <TotalNumStepsAndRows findOptions={p.findOptions} />
    </div>
  );
}

function TotalNumStepsAndRows(p: { findOptions: FindOptionsParsed }) {

  const st = p.findOptions.systemTime!;

  const isOneValue = p.findOptions.groupResults && p.findOptions.columnOptions.every(a => a.token == null || a.token.fullKey == QueryTokenString.timeSeries().token || a.token.queryTokenType == "Aggregate");

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
    <span className="ms-1">
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
  );
}
