import * as React from 'react'
import * as d3 from 'd3'
import { ChartColumn } from '../../ChartClient';
import * as ChartUtils from './ChartUtils';
import { translate, rotate } from './ChartUtils';
import TextEllipsis from './TextEllipsis';
import { Rule } from './Rule';

export function YScaleTicks({ xRule, yRule, valueColumn, y, format }: { xRule: Rule, yRule: Rule, valueColumn: ChartColumn<number>, y: d3.ScaleContinuousNumeric<number, number>, format?: (d: number) => string }) {

  var availableHeight = yRule.size("content");

  var yTicks = y.ticks(availableHeight / 50);
  var yTickFormat = format || y.tickFormat(availableHeight / 50);

  return (
    <>
      <g className="y-line-group" transform={translate(xRule.start('content'), yRule.end('content'))}>
        {yTicks.map(t => <line key={t} className="y-line sf-transition"
          transform={translate(0, -y(t))}
          x2={xRule.size('content')}
          stroke="LightGray" />)}
      </g>

      <g className="y-tick-group" transform={translate(xRule.start('ticks'), yRule.end('content'))}>
        {yTicks.map(t => <line key={t} className="y-tick sf-transition"
          transform={translate(0, -y(t))}
          x2={xRule.size('ticks')}
          stroke="Black" />)}
      </g>

      <g className="y-label-group" transform={translate(xRule.end('labels'), yRule.end('content'))}>
        {yTicks.map(t => <text key={t} className="y-label sf-transition"
          transform={translate(0, -y(t))}
          dominantBaseline="middle"
          textAnchor="end">
          {yTickFormat(t)}
        </text>)}
      </g>

      <g className="y-title-group" transform={translate(xRule.middle('title'), yRule.middle('content')) + rotate(270)}>
        <text className="y-title"
          textAnchor="middle"
          dominantBaseline="middle">
          {valueColumn.title}
        </text>
      </g>
    </>
  );
}

export function XScaleTicks({ xRule, yRule, valueColumn, x, format }: { xRule: Rule, yRule: Rule, valueColumn: ChartColumn<number>, x: d3.ScaleContinuousNumeric<number, number>, format?: (d: number) => string }) {

  var availableWidth = yRule.size("content");

  var xTicks = x.ticks(availableWidth / 50);
  var xTickFormat = format || x.tickFormat(availableWidth / 50);

  return (
    <>
      <g className="x-line-group" transform={translate(xRule.start('content'), yRule.start('content'))}>
        {xTicks.map(t => <line key={t} className="y-line-group sf-transition"
          transform={translate(x(t), 0)}
          y1={yRule.size('content')}
          stroke="LightGray" />)}
      </g>

      <g className="x-tick-group" transform={translate(xRule.start('content'), yRule.start('ticks'))}>
        {xTicks.map(t => <line key={t} className="x-tick-group sf-transition"
          transform={translate(x(t), 0)}
          y2={yRule.size('ticks')}
          stroke="Black" />)}
      </g>

      <g className="x-label-group" transform={translate(xRule.start('content'), yRule.end('labels'))}>
        {xTicks.map(t => <text key={t} className="x-label sf-transition"
          transform={translate(x(t), 0)}
          textAnchor="middle">
          {xTickFormat(t)}
        </text>)}
      </g>

      <g className="x-title-group" transform={translate(xRule.middle('content'), yRule.middle('title'))}>
        <text className="x-title"
          textAnchor="middle"
          dominantBaseline="middle">
          {valueColumn.title || ""}
        </text>
      </g>
    </>
  );
}

export function XKeyTicks({ xRule, yRule, keyValues, keyColumn, x, showLines }: { xRule: Rule, yRule: Rule, keyValues: unknown[], keyColumn: ChartColumn<unknown>, x: d3.ScaleBand<string>, showLines?: boolean }) {

  var orderedKeys = keyValues.orderBy(keyColumn.getKey);
  return (
    <>
      {
        showLines && <g className="x-key-line-group" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.start('content'))}>
          {orderedKeys.map(t => <line key={keyColumn.getKey(t)} className="y-key-line-group sf-transition"
            transform={translate(x(keyColumn.getKey(t))!, 0)}
            y1={yRule.size('content')}
            stroke="LightGray" />)}
        </g>
      }

      <g className="x-key-tick-group" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.start('ticks'))}>
        {orderedKeys.map((t, i) => <line key={keyColumn.getKey(t)} className="x-key-tick sf-transition"
          transform={translate(x(keyColumn.getKey(t))!, 0)}
          y2={yRule.start('labels' + (i % 2)) - yRule.start('ticks')}
          stroke="Black" />)}
      </g>
      {
        (x.bandwidth() * 2) > 60 &&
        <g className="x-key-label-group" transform={translate(xRule.start('content') + (x.bandwidth() / 2), yRule.middle('labels0'))}>
          {orderedKeys.map((t, i) => <TextEllipsis key={keyColumn.getKey(t)} maxWidth={x.bandwidth() * 2} className="x-key-label sf-transition"
            transform={translate(x(keyColumn.getKey(t))!, 0)}
            y={yRule.middle('labels' + (i % 2)) - yRule.middle('labels0')}
            dominantBaseline="middle"
            textAnchor="middle">
            {keyColumn.getNiceName(t)}
          </TextEllipsis>)}
        </g>
      }
      <XTitle xRule={xRule} yRule={yRule} keyColumn={keyColumn} />
    </>
  );
}

export function XTitle({ xRule, yRule, keyColumn }: { xRule: Rule, yRule: Rule, keyColumn: ChartColumn<unknown> }) {
  return (
    <g className="x-title-group" transform={translate(xRule.middle('content'), yRule.middle('title'))}>
      <text className="x-title"
        textAnchor="middle"
        dominantBaseline="middle">
        {keyColumn.title}
      </text>
    </g>
  );
}

export function YKeyTicks({ xRule, yRule, keyValues, keyColumn, y, showLabels, showLines }: { xRule: Rule, yRule: Rule, keyValues: unknown[], keyColumn: ChartColumn<unknown>, y: d3.ScaleBand<string>, showLabels: boolean, showLines?: boolean }) {
  var orderedKeys = keyValues.orderBy(keyColumn.getKey);

  return (
    <>
      {showLines &&
        <g className="y-line-group" transform={translate(xRule.start('content'), yRule.end('content') - (y.bandwidth() / 2))}>
        {orderedKeys.map(t => <line key={keyColumn.getKey(t)} className="y-line sf-transition"
            transform={translate(0, -y(keyColumn.getKey(t))!)}
            x2={xRule.size('content')}
            stroke="LightGray" />)}
        </g>
      }
      <g className="y-key-tick-group" transform={translate(xRule.start('ticks'), yRule.end('content') - (y.bandwidth() / 2))}>
        {orderedKeys.map(t => <line key={keyColumn.getKey(t)} className="y-key-tick sf-transition"
          transform={translate(0, -y(keyColumn.getKey(t))!)}
          x2={xRule.size('ticks')}
          stroke="Black" />)}
      </g>
      {showLabels && y.bandwidth() > 15 &&
        <g className="y-label" transform={translate(xRule.end('labels'), yRule.end('content') - (y.bandwidth() / 2))}>
        {orderedKeys.map(t => <TextEllipsis maxWidth={xRule.size('labels')} key={keyColumn.getKey(t)} className="y-label sf-transition"
            transform={translate(0, -y(keyColumn.getKey(t))!)}
            dominantBaseline="middle"
            textAnchor="end">
            {keyColumn.getNiceName(t)}
          </TextEllipsis>)}
        </g>
      }

      <g className="y-title-group" transform={translate(xRule.middle('title'), yRule.middle('content')) + rotate(270)}>
        <text className="y-title" textAnchor="middle" dominantBaseline="middle">
          {keyColumn.title || ""}
        </text>
      </g>
    </>
  );
}
