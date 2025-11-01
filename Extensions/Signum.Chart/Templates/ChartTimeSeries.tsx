import { DateTime, DurationUnit } from 'luxon'
import * as React from 'react'
import { FormCheck } from 'react-bootstrap'
import { QueryTokenString, toLuxonFormat, toNumberFormat } from '@framework/Reflection';
import { JavascriptMessage } from '@framework/Signum.Entities';
import { TimeSeriesUnit } from '../../../Signum/React/Signum.DynamicQuery';
import { isNumberKey, NumberBox } from '../../../Signum/React/Lines/NumberLine';
import { AggregateFunction, QueryTokenDateMessage } from '../../../Signum/React/Signum.DynamicQuery.Tokens';
import { DateTimePicker } from 'react-widgets-up';
import { classes } from '../../../Signum/React/Globals';
import { useAPI, useForceUpdate } from '../../../Signum/React/Hooks';
import { ChartRequestModel, ChartTimeSeriesEmbedded } from '../Signum.Chart';
import { ChartClient } from '../ChartClient'
import { IChartBase, UserChartEntity } from '../UserChart/Signum.Chart.UserChart';
import UserQuery from '../../Signum.UserQueries/Templates/UserQuery';
import { UserQueryEntity } from '../../Signum.UserQueries/Signum.UserQueries';
import { UserAssetClient } from '../../Signum.UserAssets/UserAssetClient';


export default function ChartTimeSeries(p: { chartTimeSeries: ChartTimeSeriesEmbedded, chartBase: IChartBase, onChange: () => void }): React.JSX.Element {

  var ts = p.chartTimeSeries;

  function renderTimeSeriesUnit() {

    function handleTimeSeriesUnit(e: React.ChangeEvent<HTMLSelectElement>) {
      ts.timeSeriesUnit = e.currentTarget.value as TimeSeriesUnit;
      ts.timeSeriesStep = 1;
      p.onChange();
    }


    return (
      <select value={ts.timeSeriesUnit!} className="form-select form-select-xs ms-1" style={{ width: "auto" }} onChange={handleTimeSeriesUnit}>
        {TimeSeriesUnit.values().map((stm, i) => <option key={i} value={stm}>{TimeSeriesUnit.niceToString(stm)}</option>)}
      </select>
    );
  }

  function renderTimeSerieStep() {

    function handleTimeSerieStep(e: number | null | undefined) {
      ts.timeSeriesStep = e ?? 1;
      p.onChange();
    }

    var numberFormat = toNumberFormat("0");

    return (
      <NumberBox value={ts.timeSeriesStep} validateKey={isNumberKey} format={numberFormat}
        htmlAttributes={{ className: "form-control form-control-xs ms-1", style: { width: "40px" } }}
        onChange={handleTimeSerieStep} />
    );
  }

  function renderDateTime(field: "startDate" | "endDate") {

    const handleDatePickerOnChange = (date: Date | null | undefined, str: string) => {
      const m = date && DateTime.fromJSDate(date);
      ts[field] = m ? m.toISO()! : null;
      p.onChange();
    };

    var utcDate = ts[field];

    var m = utcDate == null ? null : DateTime.fromISO(utcDate);
    var luxonFormat = toLuxonFormat("G", "DateTime");
    return (
      <div className="d-flex ms-1">
        {AggregateFunction.niceToString(field == "startDate" ? "Min" : "Max")}
        <div className="rw-widget-xs ms-1">
          {UserChartEntity.isInstance(p.chartBase) ? <input type="text" defaultValue={utcDate!} 
          style={{width: 170}}
          onChange={e => {
            ts[field] = e.target.value;
            p.onChange();

          }} /> : <DateTimePicker value={m?.toJSDate()} onChange={handleDatePickerOnChange}
            inputProps={{ style: { width: "150px" } }}
            valueEditFormat={luxonFormat} valueDisplayFormat={luxonFormat} includeTime={true} messages={{ dateButton: JavascriptMessage.Date.niceToString() }} />}
        </div>
      </div>
    );
  }

  return (
    <div className={classes("sf-system-time-editor", "alert alert-primary")}>
      <span>Time series</span>
      <span className="ms-2 d-flex">{QueryTokenDateMessage.Every01.niceToString().formatHtml(renderTimeSerieStep(), renderTimeSeriesUnit())}</span>
      {renderDateTime("startDate")}
      {renderDateTime("endDate")}
      <TotalNumStepsAndRows chartTimeSeries={ts} chartBase={p.chartBase} onChange={p.onChange} />

      <FormCheck
        className="ms-2"
        checked={ts.splitQueries}
        onChange={e => { ts.splitQueries = e.currentTarget.checked; p.onChange(); }}
        label={QueryTokenDateMessage.SplitQueries.niceToString()}
        id={`split-queries`}
      />
    </div>
  );
}

function TotalNumStepsAndRows(p: { chartTimeSeries: ChartTimeSeriesEmbedded, chartBase: IChartBase, onChange: () => void }) {

  const isOneValue = ChartClient.hasAggregates(p.chartBase) && p.chartBase.columns.every(a => a.element.token == null || a.element.token.token?.fullKey == QueryTokenString.timeSeries.token || a.element.token.token?.queryTokenType == "Aggregate");

  var st = p.chartTimeSeries;

  const min = useAPI(() => !p.chartTimeSeries.startDate ? Promise.resolve(null) : 
    ChartRequestModel.isInstance(p.chartBase) ? Promise.resolve(DateTime.fromISO(p.chartTimeSeries.startDate)) : 
      UserAssetClient.API.parseDate(p.chartTimeSeries.startDate).then(date => DateTime.fromISO(date)), [p.chartTimeSeries.startDate]);
  const max = useAPI(() => !p.chartTimeSeries.endDate ? Promise.resolve(null) : 
      ChartRequestModel.isInstance(p.chartBase) ? Promise.resolve(DateTime.fromISO(p.chartTimeSeries.endDate)) : 
        UserAssetClient.API.parseDate(p.chartTimeSeries.endDate).then(date => DateTime.fromISO(date)), [p.chartTimeSeries.endDate]);
  
  React.useEffect(() => {
    if (isOneValue) {
      if (st.timeSeriesMaxRowsPerStep != 1) {
        st.timeSeriesMaxRowsPerStep = 1;
        p.onChange();
      }
    } else {
      if (st.timeSeriesMaxRowsPerStep == 1) {
        st.timeSeriesMaxRowsPerStep = 10;
        p.onChange();
      }
    }
  }, [isOneValue])

  if (min == null || max == null || st.timeSeriesStep == null || st.timeSeriesUnit == null)
    return null;

  const unit: DurationUnit = st.timeSeriesUnit.firstLower() as DurationUnit;
  const steps = Math.ceil(max.diff(min, unit).as(unit));

  const formatter = toNumberFormat("C0");

  return (
    <span className="ms-1">
      {QueryTokenDateMessage._0Steps1Rows2TotalRowsAprox.niceToString().formatHtml(
        <strong className={steps > 1000 ? "text-danger" : undefined}>{formatter.format(steps)}</strong>,
        <NumberBox validateKey={isNumberKey} value={st.timeSeriesMaxRowsPerStep} format={formatter} onChange={e => { st.timeSeriesMaxRowsPerStep = e ?? 10; p.onChange(); }}
          htmlAttributes={{
            className: classes("form-control form-control-xs ms-1", st.timeSeriesMaxRowsPerStep == null && "sf-mandatory"),
            style: { width: "40px", display: "inline-block" }
          }}
        />,
        <strong className={st.timeSeriesMaxRowsPerStep != null && steps * st.timeSeriesMaxRowsPerStep > 1000 ? "text-danger" : undefined}>
          {st.timeSeriesMaxRowsPerStep == null ? "" : formatter.format(steps * st.timeSeriesMaxRowsPerStep)}
        </strong>
      )}
    </span>
  );
}
