import * as d3 from 'd3';

export function buildDateScale(isoStrings: string[], max24Hours?: boolean): (d: string) => number {
  const dates = isoStrings.map(d => new Date(d));
  const minDate = d3.min(dates) ?? new Date();
  const maxDate = new Date(Math.max((d3.max(dates) ?? new Date()).getTime(), minDate.getTime() + 1));

  if (max24Hours && maxDate.getTime() - minDate.getTime() > 24 * 60 * 60 * 1000) {
    const toTimeOfDay = (d: Date) => new Date(1970, 0, 1, d.getHours(), d.getMinutes(), d.getSeconds(), d.getMilliseconds());
    const scale = d3.scaleTime<number>().domain([new Date(1970, 0, 1, 0, 0, 0), new Date(1970, 0, 1, 23, 59, 0)]).range([0, 100]).clamp(true);
    return d => scale(toTimeOfDay(new Date(d)));
  }

  const scale = d3.scaleTime<number>().domain([minDate, maxDate]).range([0, 100]).clamp(true);
  return d => scale(new Date(d));
}
