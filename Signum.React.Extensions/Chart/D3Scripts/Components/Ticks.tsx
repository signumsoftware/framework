import * as React from 'react'
import * as d3 from 'd3'
import { ChartColumn } from '../../ChartClient';
import * as ChartUtils from '../../Templates/ChartUtils';
import { translate, rotate } from '../../Templates/ChartUtils';
import TextEllipsis from './TextEllipsis';

export function YScaleTicks({ xRule, yRule, valueColumn, y, format }: { xRule: ChartUtils.Rule, yRule: ChartUtils.Rule, valueColumn: ChartColumn<number>, y: d3.ScaleContinuousNumeric<number, number>, format?: (d: number) => string }) {

  var availableHeight = yRule.size("content");

  var yTicks = y.ticks(availableHeight / 50);
  var yTickFormat = format || y.tickFormat(availableHeight / 50);

  return (
    <>
      <g className="y-line" transform={translate(xRule.start('content'), yRule.end('content'))}>
        {yTicks.map(t => <line key={t} className="y-line"
          x2={xRule.size('content')}
          y1={-y(t)}
          y2={-y(t)}
          stroke="LightGray" />)}
      </g>

      <g className="y-tick" transform={translate(xRule.start('ticks'), yRule.end('content'))}>
        {yTicks.map(t => <line key={t} className="y-tick"
          x2={xRule.size('ticks')}
          y1={-y(t)}
          y2={-y(t)}
          stroke="Black" />)}
      </g>

      <g className="y-label" transform={translate(xRule.end('labels'), yRule.end('content'))}>
        {yTicks.map(t => <text key={t} className="y-label"
          y={-y(t)}
          dominant-baseline="middle"
          text-anchor="end">
          {yTickFormat}
        </text>)}
      </g>

      <g className="y-label" transform={translate(xRule.middle('title'), yRule.middle('content')) + rotate(270)}>
        <text className="y-label" text-anchor="middle" dominant-baseline="middle">
          {valueColumn.title}
        </text>
      </g>
    </>
  );
}

export function XScaleTicks({ xRule, yRule, valueColumn, x, format }: { xRule: ChartUtils.Rule, yRule: ChartUtils.Rule, valueColumn: ChartColumn<number>, x: d3.ScaleContinuousNumeric<number, number>, format?: (d: number)=> string }) {

  var availableWidth = yRule.size("content");
  
  var xTicks = x.ticks(availableWidth / 50);
  var xTickFormat = format || x.tickFormat(availableWidth / 50);

  return (
    <>
      <g className="x-lines" transform={translate(xRule.start('content'), yRule.start('content'))}>
        {xTicks.map(t => <line key={t} className="y-lines"
          x1={x(t)}
          x2={x(t)}
          y1={yRule.size('content')}
          stroke="LightGray" />)}
      </g>

      <g className="x-tick" transform={translate(xRule.start('content'), yRule.start('ticks'))}>
        {xTicks.map(t => <line key={t} className="x-tick"
          x1={x(t)}
          x2={x(t)}
          y2={yRule.size('ticks')}
          stroke="Black" />)}
      </g>

      <g className="x-label" transform={translate(xRule.start('content'), yRule.end('labels'))}>
        {xTicks.map(t => <text key={t} className="x-label"
          x={x(t)}
          textAnchor="middle">
          {xTickFormat}
        </text>)}
      </g>

      <g className="x-title" transform={translate(xRule.middle('content'), yRule.middle('title'))}>
        <text className="x-title" textAnchor="middle" dominantBaseline="central">
          {valueColumn.title || ""}
        </text>
      </g>
    </>
  );
}

export function XKeyTicks({ xRule, yRule, keyValues, keyColumn, x }: { xRule: ChartUtils.Rule, yRule: ChartUtils.Rule, keyValues: unknown[], keyColumn: ChartColumn<unknown>, x: d3.ScaleBand<string> }) {
  return (
    <>
      <g className="x-tick" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.start('ticks'))}>
        {keyValues.map((t, i) => <line key={keyColumn.getKey(t)} className="x-tick"
          y2={yRule.start('labels' + (i % 2)) - yRule.start('ticks')}
          x1={x(keyColumn.getKey(t))!}
          x2={x(keyColumn.getKey(t))!}
          stroke="Black" />)}
      </g>
      {
        (x.bandwidth() * 2) > 60 &&
        <g className="x-label" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.middle('labels0'))}>
          {keyValues.map((t, i) => <TextEllipsis key={keyColumn.getKey(t)} maxWidth={x.bandwidth() * 2} className="x-label"
            x={x(keyColumn.getKey(t))!}
            y={yRule.middle('labels' + (i % 2)) - yRule.middle('labels0')}
            dominant-baseline="middle"
            text-anchor="middle">
            {keyColumn.getNiceName(t)}
          </TextEllipsis>)}
        </g>
      }
      <g className="x-title" transform={translate(xRule.middle('content'), yRule.middle('title'))}>
        <text className="x-title" text-anchor="middle" dominant-baseline="middle">
          {keyColumn.title}
        </text>
      </g>
    </>
  );
}

export function YKeyTicks({ xRule, yRule, keyValues, keyColumn, y }: { xRule: ChartUtils.Rule, yRule: ChartUtils.Rule, keyValues: unknown[], keyColumn: ChartColumn<unknown>, y: d3.ScaleBand<string> }) {
  return (
    <>
      <g className="y-tick" transform={translate(xRule.start('ticks'), yRule.end('content'))}>
        {keyValues.map(t => <line key={keyColumn.getKey(t)} className="y-tick"
          x2={xRule.size('ticks')}
          y1={-y(keyColumn.getKey(t))!}
          y2={-y(keyColumn.getKey(t))!}
          stroke="Black" />)}
      </g>

      <g className="y-title" transform={translate(xRule.middle('title'), yRule.middle('content')) + rotate(270)}>
        <text className="y-title" textAnchor="middle" dominantBaseline="central">
          {keyColumn.title || ""}
        </text>
      </g>
    </>
  );
}
