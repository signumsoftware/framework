import * as React from 'react'
import * as d3 from 'd3'
import { ChartClient, ChartColumn } from '../../ChartClient';
import * as ChartUtils from './ChartUtils';
import { translate, rotate } from './ChartUtils';
import TextEllipsis from './TextEllipsis';
import { Rule } from './Rule';


function getTicks(availableSize: number, valueColumn: ChartColumn<number>,  scale: d3.ScaleContinuousNumeric<number, number>, format?: (d: number) => string): {ticks: number[], ticksFormat: (n: number) => string} {

  let ticksCount = availableSize / 50;
  if (valueColumn.type == "Number") {
    var domain = scale.domain();
    const domainSize = domain[1] - domain[0];

    if (ticksCount > domainSize)
      ticksCount = domainSize;
  }

  var ticks = scale.ticks(ticksCount);

  var isDate = valueColumn.type == "Date" || valueColumn.type == "DateTime";

  var ticksFormat = format ?? (isDate ? scale.tickFormat(ticksCount) : valueColumn.getNiceName);

  return { ticks, ticksFormat }
}

export function YScaleTicks({ xRule, yRule, valueColumn, y, format }:
  { xRule: Rule<"title" | "labels" | "ticks" | "content">, yRule: Rule<"content">, valueColumn: ChartColumn<number>, y: d3.ScaleContinuousNumeric<number, number>, format?: (d: number) => string }): React.JSX.Element {

  const { ticks: yTicks, ticksFormat: yTickFormat } = getTicks(yRule.size("content"), valueColumn, y, format); 

  return (
    <>
      <g className="y-line-group" transform={translate(xRule.start('content'), yRule.end('content'))}>
        {yTicks.map(t => <line key={t} className="y-line sf-transition"
          transform={translate(0, -y(t)!)}
          x2={xRule.size('content')}
          stroke="LightGray" />)}
      </g>

      <g className="y-tick-group" transform={translate(xRule.start('ticks'), yRule.end('content'))}>
        {yTicks.map(t => <line key={t} className="y-tick sf-transition"
          transform={translate(0, -y(t)!)}
          x2={xRule.size('ticks')}
          stroke="var(--bs-body-color)" />)}
      </g>

      <g className="y-label-group" transform={translate(xRule.end('labels'), yRule.end('content'))}>
        {yTicks.map(t => <TextEllipsis key={t} className="y-label sf-transition"
          transform={translate(0, -y(t)!)}
          maxWidth={xRule.end('labels')}
          dominantBaseline="middle"
          textAnchor="end">
          {yTickFormat(t)}
        </TextEllipsis>)}
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

export function YScaleTicksEnd({ xRule, yRule, valueColumn, y, format }: { xRule: Rule<"content" | "ticks2" | "labels2" | "title2">, yRule: Rule<"content">, valueColumn: ChartColumn<number>, y: d3.ScaleContinuousNumeric<number, number>, format?: (d: number) => string }): React.JSX.Element {

  const { ticks: yTicks, ticksFormat: yTickFormat } = getTicks(yRule.size("content"), valueColumn, y, format);

  return (
    <>
      <g className="y-tick-group" transform={translate(xRule.start('ticks2'), yRule.end('content'))}>
        {yTicks.map(t => <line key={t} className="y-tick sf-transition"
          transform={translate(0, -y(t)!)}
          x2={xRule.size('ticks2')}
          stroke="var(--bs-body-color)" />)}
      </g>

      <g className="y-label-group" transform={translate(xRule.end('labels2'), yRule.end('content'))}>
        {yTicks.map(t => <text key={t} className="y-label sf-transition"
          transform={translate(0, -y(t)!)}
          dominantBaseline="middle"
          textAnchor="end">
          {yTickFormat(t)}
        </text>)}
      </g>

      <g className="y-title-group" transform={translate(xRule.middle('title2'), yRule.middle('content')) + rotate(270)}>
        <text className="y-title"
          textAnchor="middle"
          dominantBaseline="middle">
          {valueColumn.title}
        </text>
      </g>
    </>
  );
}


export function XScaleTicks({ xRule, yRule, valueColumn, x, format }: { xRule: Rule<"content" | "title">, yRule: Rule<"labels" | "ticks" | "content" | "title">, valueColumn: ChartColumn<number>, x: d3.ScaleContinuousNumeric<number, number>, format?: (d: number) => string }): React.JSX.Element {

  const { ticks: xTicks, ticksFormat: xTickFormat } = getTicks(xRule.size("content"), valueColumn, x, format);

  return (
    <>
      <g className="x-line-group" transform={translate(xRule.start('content'), yRule.start('content'))}>
        {xTicks.map(t => <line key={t} className="y-line-group sf-transition"
          transform={translate(x(t)!, 0)}
          y1={yRule.size('content')}
          stroke="LightGray" />)}
      </g>

      <g className="x-tick-group" transform={translate(xRule.start('content'), yRule.start('ticks'))}>
        {xTicks.map(t => <line key={t} className="x-tick-group sf-transition"
          transform={translate(x(t)!, 0)}
          y2={yRule.size('ticks')}
          stroke="var(--bs-body-color)" />)}
      </g>

      <g className="x-label-group" transform={translate(xRule.start('content'), yRule.end('labels'))}>
        {xTicks.map(t => <text key={t} className="x-label sf-transition"
          transform={translate(x(t)!, 0)}
          textAnchor="middle">
          {xTickFormat(t)}
        </text>)}
      </g>

      <g className="x-title-group" transform={translate(xRule.middle('content'), yRule.middle('title'))}>
        <text className="x-title"
          textAnchor="middle"
          dominantBaseline="middle">
          {valueColumn.title ?? ""}
        </text>
      </g>
    </>
  );
}

export function XKeyTicks({ xRule, yRule, keyValues, keyColumn, x, showLines, onDrillDown, isActive }: {
  xRule: Rule<"content">, yRule: Rule<"content" | "ticks" | "labels" | "title">, keyValues: unknown[], keyColumn: ChartColumn<unknown>, x: d3.ScaleBand<string>, showLines?: boolean,
  isActive?: (value: unknown) => boolean;
  onDrillDown?: (value: unknown, e: React.MouseEvent<any> | MouseEvent) => void;
}): React.JSX.Element {

  const bandwith = x.bandwidth();
  var stableKeys = keyValues.orderBy(keyColumn.getKey);
  var keyInOrder = keyValues.orderBy(v => x(keyColumn.getKey(v)));
  return (
    <>
      {
        showLines && <g className="x-key-line-group" transform={translate(xRule.start('content') + (bandwith / 2), yRule.start('content'))}>
          {stableKeys.map(t => <line key={keyColumn.getKey(t)} className="x-key-line-group sf-transition"
            opacity={isActive?.(t) == false ? 0.5 : undefined}
            transform={translate(x(keyColumn.getKey(t))!, 0)}
            y1={yRule.size('content')}
            stroke="LightGray" />)}
        </g>
      }

      <g className="x-key-tick-group" transform={translate(xRule.start('content') + (bandwith / 2), yRule.start('ticks'))}>
        {stableKeys.map((t) => <line key={keyColumn.getKey(t)} className="x-key-tick sf-transition"
          opacity={isActive?.(t) == false ? 0.5 : undefined}
          transform={translate(x(keyColumn.getKey(t))!, 0)}
          y2={(keyInOrder.indexOf(t) % 2) * yRule.size('labels') / 2}
          stroke="var(--bs-body-color)" />)}
      </g>
      {
        (bandwith * 2) > 60 &&
        <g className="x-key-label-group" transform={translate(xRule.start('content') + (bandwith / 2), yRule.middle('ticks'))}>
            {stableKeys.map((t) => <TextEllipsis key={keyColumn.getKey(t)} maxWidth={bandwith * 2} className="x-key-label sf-transition"
            onClick={e => onDrillDown?.(t, e)}
            opacity={isActive?.(t) == false ? 0.5 : undefined}
            style={{ fontWeight: isActive?.(t) == true ? "bold" : undefined, cursor: onDrillDown ? "pointer" : undefined }}
            transform={translate(x(keyColumn.getKey(t))!, 0)}
              y={yRule.size('labels') / 4 + (keyInOrder.indexOf(t) % 2) * yRule.size('labels') / 2}
            dominantBaseline="middle"
            textAnchor="middle">
            {keyColumn.getNiceName(t, x.bandwidth())}
          </TextEllipsis>)}
        </g>
      }
      <XTitle xRule={xRule} yRule={yRule} keyColumn={keyColumn} />
    </>
  );
}

export function XTitle({ xRule, yRule, keyColumn }: { xRule: Rule<"content">, yRule: Rule<"title">, keyColumn: ChartColumn<unknown> }): React.JSX.Element {
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

export function YKeyTicks({ xRule, yRule, keyValues, keyColumn, y, showLabels, showLines, isActive, onDrillDown }: {
  xRule: Rule<"title" | "labels" | "ticks" | "content">, yRule: Rule<"content">,
  keyValues: unknown[], keyColumn: ChartColumn<unknown>, y: d3.ScaleBand<string>, showLabels: boolean, showLines?: boolean,
  isActive?: (value: unknown) => boolean;
  onDrillDown?: (value: unknown, e: React.MouseEvent<any> | MouseEvent) => void;
}): React.JSX.Element {
  var orderedKeys = keyValues.orderBy(keyColumn.getKey);

  return (
    <>
      {showLines &&
        <g className="y-line-group" transform={translate(xRule.start('content'), yRule.end('content') - (y.bandwidth() / 2))}>
        {orderedKeys.map(t => <line key={keyColumn.getKey(t)} className="y-line sf-transition"
            opacity={isActive?.(t) == false ? 0.5 : undefined}
            transform={translate(0, -y(keyColumn.getKey(t))!)}
            x2={xRule.size('content')}
            stroke="LightGray" />)}
        </g>
      }
      <g className="y-key-tick-group" transform={translate(xRule.start('ticks'), yRule.end('content') - (y.bandwidth() / 2))}>
        {orderedKeys.map(t => <line key={keyColumn.getKey(t)} className="y-key-tick sf-transition"
          opacity={isActive?.(t) == false ? 0.5 : undefined}
          transform={translate(0, -y(keyColumn.getKey(t))!)}
          x2={xRule.size('ticks')}
          stroke="var(--bs-body-color)" />)}
      </g>
      {showLabels && y.bandwidth() > 15 &&
        <g className="y-label" transform={translate(xRule.end('labels'), yRule.end('content') - (y.bandwidth() / 2))}>
          {orderedKeys.map(t => <TextEllipsis maxWidth={xRule.size('labels')} key={keyColumn.getKey(t)} className="y-label sf-transition"
            onClick={e => onDrillDown?.(t, e)}
            opacity={isActive?.(t) == false ? 0.5 : undefined}
            style={{ fontWeight: isActive?.(t) == true ? "bold" : undefined, cursor: onDrillDown ? "pointer" : undefined }}
            transform={translate(0, -y(keyColumn.getKey(t))!)}
            dominantBaseline="middle"
            textAnchor="end">
            {keyColumn.getNiceName(t)}
          </TextEllipsis>)}
        </g>
      }

      <g className="y-title-group" transform={translate(xRule.middle('title'), yRule.middle('content')) + rotate(270)}>
        <text className="y-title" textAnchor="middle" dominantBaseline="middle">
          {keyColumn.title ?? ""}
        </text>
      </g>
    </>
  );
}
