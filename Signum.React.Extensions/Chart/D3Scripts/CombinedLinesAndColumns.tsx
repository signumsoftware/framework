import * as React from 'react'
import * as d3 from 'd3'
import * as ChartUtils from './Components/ChartUtils';
import { translate, scale, rotate, skewX, skewY, matrix, scaleFor } from './Components/ChartUtils';
import { ChartTable, ChartColumn, ChartRow, ChartScriptProps } from '../ChartClient';
import { KeyCodes } from '@framework/Components';
import TextEllipsis from './Components/TextEllipsis';
import { XKeyTicks, YScaleTicks, YScaleTicksEnd } from './Components/Ticks';
import { XAxis, YAxis, YAxisEnd } from './Components/Axis';
import { Rule } from './Components/Rule';
import InitialMessage from './Components/InitialMessage';
import { ChartScriptHorizontalProps, paintLine } from './Line';
import { paintColumns } from './Columns';
import { ReactChartCombinedInfo } from './Components/ReactChartCombined';
import { D3ChartScript } from '../Signum.Entities.Chart';
import { MemoRepository } from './Components/ReactChart';

const supportedTypes = [
  D3ChartScript.Line.key,
  D3ChartScript.Columns.key
];

export function renderCombinedLinesAndColumns({ infos, width, height, initialLoad, useSameScale }: { infos: ReactChartCombinedInfo[], width: number, height: number, initialLoad: boolean, useSameScale: boolean }): React.ReactElement<any> {

  const firstParameters = infos[0].parameters;

  if (useSameScale == false && infos.length == 1)
    useSameScale = true;

  const notSupported = infos.filter(info => !supportedTypes.contains(info.chartRequest.chartScript.key));
  if (notSupported.length > 0)
    throw new Error(`Combined Chart only supports ${supportedTypes.joinComma(" and ")}, not ${notSupported.joinComma(" or ")}`);

  const xRule = Rule.create({
    _1: 5,
    title: 15,
    _2: 10,
    labels: parseInt(firstParameters["UnitMargin"]),
    _3: 5,
    ticks: 4,

    content: '*',

    ticks2: useSameScale ? 0 : 4,
    _4: useSameScale ? 0 : 5,
    labels2: useSameScale ? 0 : parseInt(infos[1].parameters["UnitMargin"]),
    _5: 10,
    title2: useSameScale ? 0 : 10,
    _6: useSameScale ? 0 : 5,
  }, width);
  //xRule.debugX(chart)

  const yRule = Rule.create({
    _2: parseFloat(firstParameters["NumberOpacity"]) > 0 ? 20 : 10,
    content: '*',
    ticks: 4,
    _3: 5,
    labels: 30,
    _4: 10,
    title: 15,
    _5: 5,
  }, height);
  //yRule.debugY(chart);

  if (infos[0].data == null)
    return (
      <svg direction="ltr" width={width} height={height}>
        <InitialMessage data={undefined} x={xRule.middle("content")} y={yRule.middle("content")} loading={true} />
        <XAxis xRule={xRule} yRule={yRule} />
        <YAxis xRule={xRule} yRule={yRule} />
      </svg>
    );

  const keyColumn = infos[0].data.columns.c0! as ChartColumn<unknown>;
  const valueColumn = infos[0].data.columns.c1! as ChartColumn<number>;

  const otherKeyColumn = infos.filter(a => a.data != null && a.data.columns.c0!.type != keyColumn.type);
  if (otherKeyColumn.length)
    throw new Error(`Incompatible key columns for the horizontal axis: ${otherKeyColumn.joinComma(" and ")} instead of ${keyColumn.type}`);

  const allKeyValues = infos.flatMap(info => info.data == null ? [] :
    info.data.rows.map(r => info.data!.columns.c0!.getValue(r))
  ).distinctBy(a => keyColumn.getKey(a));

  const keyValues = ChartUtils.completeValues(keyColumn, allKeyValues, firstParameters['CompleteValues'], infos[0].chartRequest.filterOptions, ChartUtils.insertPoint(keyColumn, valueColumn));

  const x = d3.scaleBand()
    .domain(keyValues.map(v => keyColumn.getKey(v)))
    .range([0, xRule.size('content')]);

  let yScales: (d3.ScaleContinuousNumeric<number, number> | null)[];
  if (useSameScale) {

    const allValues = infos.flatMap(info => info.data == null ? [] :
      info.data.rows.map(r => (info.data!.columns.c1 as ChartColumn<number>)!.getValue(r))
    ).distinctBy(a => valueColumn.getKey(a));

    var scale = scaleFor(valueColumn, allValues, 0, yRule.size('content'), firstParameters["VerticalScale"] ?? firstParameters["Scale"]);

    yScales = infos.map(info => info.data == null ? null : scale);

  } else {
    yScales = infos.map(info => {
      if (info.data == null)
        return null;
      const valColumn = info.data!.columns.c1 as ChartColumn<number>;
      return scaleFor(valColumn, info.data.rows.map(r => valColumn.getValue(r)), 0, yRule.size('content'), info.parameters["VerticalScale"] ?? info.parameters["Scale"]);
    });
  }

  const colCount = infos.filter(a => a.chartRequest.chartScript.key == D3ChartScript.Columns.key).length;  
  let colIndex = 0;
  return (
    <svg direction="ltr" width={width} height={height}>

      <XKeyTicks xRule={xRule} yRule={yRule} keyValues={keyValues} keyColumn={keyColumn} x={x} showLines={x.bandwidth() > 5} />
      <YScaleTicks xRule={xRule} yRule={yRule} valueColumn={valueColumn} y={yScales[0]!} />
      {!useSameScale && yScales[1] && <YScaleTicksEnd xRule={xRule} yRule={yRule} valueColumn={infos[1].data!.columns.c1 as ChartColumn<number>} y={yScales[1]!} />}

      {
        infos.map((info, i) => info.data == null ? null :
          <g key={i}>
            {
              info.chartRequest.chartScript.key == D3ChartScript.Line.key ? paintLine({ xRule, yRule, keyValues, data: info.data, initialLoad, onDrillDown: info.onDrillDown, parameters: info.parameters, hasHorizontalScale: false, x: x, y: yScales[i]!, memo: info.memo }) :
                info.chartRequest.chartScript.key == D3ChartScript.Columns.key ? paintColumns({ xRule, yRule, keyValues, data: info.data, initialLoad, onDrillDown: info.onDrillDown, parameters: info.parameters, hasHorizontalScale: false, x: x, y: yScales[i]!, colCount, colIndex: colIndex++, memo: info.memo }) :
                  null
            }
          </g>
        )
      }
      <InitialMessage data={infos.firstOrNull(a => a.data != null)?.data} x={xRule.middle("content")} y={yRule.middle("content")} loading={infos.some(a => a.data == null)} />
      <XAxis xRule={xRule} yRule={yRule} />
      <YAxis xRule={xRule} yRule={yRule} />
      {!useSameScale && <YAxisEnd xRule={xRule} yRule={yRule}/>}
    </svg>
  );
}

