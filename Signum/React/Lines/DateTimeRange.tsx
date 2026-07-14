import * as React from 'react';
import { DateTime } from 'luxon';
import { CalendarProps } from 'react-widgets-up/Calendar'
import { RenderDayProp } from 'react-widgets-up/Month';
import { StyleContext, TypeContext } from '../TypeContext';
import { TypeReference } from '../Reflection';
import { DateTimeLine, DateTimeLineOptions, RenderDayAndTitle } from './DateTimeLine';
import { FormGroup } from './FormGroup';
import { ChangeEvent } from './LineBase';

export interface DateRangePartProps {
  ctx: TypeContext<string | null>;
  type?: TypeReference;
  format?: string;
  minDate?: Date;
  maxDate?: Date;
  calendarProps?: Partial<CalendarProps>;
  calendarAlignEnd?: boolean;
  onChange?: (e: ChangeEvent) => void;
  valueHtmlAttributes?: React.AllHTMLAttributes<any>;
  labelHtmlAttributes?: React.LabelHTMLAttributes<HTMLLabelElement>;
  formGroupHtmlAttributes?: React.HTMLAttributes<any>;
  initiallyFocused?: boolean | number;
  helpText?: React.ReactNode;
  mandatory?: boolean | "warning";
}

export interface DateTimeRangeProps {
  min: DateRangePartProps;
  max: DateRangePartProps;
  mainCtx?: StyleContext;
  label?: React.ReactNode;
  labelHtmlAttributes?: React.LabelHTMLAttributes<HTMLLabelElement>;
  formGroupHtmlAttributes?: React.HTMLAttributes<any>;
}

function earlierDate(a: Date | undefined, b: Date | undefined): Date | undefined {
  if (a == null) return b;
  if (b == null) return a;
  return a < b ? a : b;
}

function laterDate(a: Date | undefined, b: Date | undefined): Date | undefined {
  if (a == null) return b;
  if (b == null) return a;
  return a > b ? a : b;
}

function makeRangeRenderDay(
  base: RenderDayAndTitle,
  minIso: string | null,
  maxIso: string | null,
): RenderDayProp {
  return ({ date, label }) => {
    const iso = DateTime.fromJSDate(date).toISODate()!;

    const isStart = minIso != null && iso === minIso;
    const isEnd   = maxIso != null && iso === maxIso;
    const isBetween = minIso != null && maxIso != null && iso > minIso && iso < maxIso;

    const inner = base.renderDay({ date, label });

    if (!isStart && !isEnd && !isBetween)
      return inner;

    const cls = isStart && isEnd ? "sf-range-start sf-range-end"
      : isStart   ? "sf-range-start"
      : isEnd     ? "sf-range-end"
      : "sf-range-between";

    return <span className={cls}>{inner}</span>;
  };
}

export function DateTimeRange(p: DateTimeRangeProps): React.ReactElement | null {

  const rdat = DateTimeLineOptions.Options.useRenderDay();

  const minDt = p.min.ctx.value ? DateTime.fromISO(p.min.ctx.value) : null;
  const maxDt = p.max.ctx.value ? DateTime.fromISO(p.max.ctx.value) : null;

  const minIso = minDt?.isValid ? minDt.toISODate() : null;
  const maxIso = maxDt?.isValid ? maxDt.toISODate() : null;

  const minAsDate = minDt?.isValid ? minDt.toJSDate() : undefined;
  const maxAsDate = maxDt?.isValid ? maxDt.toJSDate() : undefined;

  const rangeRenderDay = makeRangeRenderDay(rdat, minIso, maxIso);

  function renderPart(
    part: DateRangePartProps,
    srOnly: boolean,
    constraintMinDate: Date | undefined,
    constraintMaxDate: Date | undefined,
  ): React.ReactElement {
    const ctx = srOnly ? part.ctx.subCtx({ formGroupStyle: "SrOnly" }) : part.ctx;

    // merge caller constraints with range constraints — be maximally restrictive
    const effectiveMinDate = laterDate(part.minDate, constraintMinDate);
    const effectiveMaxDate = earlierDate(part.maxDate, constraintMaxDate);

    return (
      <DateTimeLine
        ctx={ctx}
        type={part.type}
        format={part.format}
        minDate={effectiveMinDate}
        maxDate={effectiveMaxDate}
        calendarProps={{
          ...part.calendarProps,
          renderDay: rangeRenderDay,
        }}
        calendarAlignEnd={part.calendarAlignEnd}
        onChange={part.onChange}
        valueHtmlAttributes={part.valueHtmlAttributes}
        labelHtmlAttributes={part.labelHtmlAttributes}
        formGroupHtmlAttributes={part.formGroupHtmlAttributes}
        initiallyFocused={part.initiallyFocused}
        helpText={part.helpText}
        mandatory={part.mandatory}
      />
    );
  }

  if (p.mainCtx) {
    return (
      <FormGroup
        ctx={p.mainCtx}
        label={p.label}
        labelHtmlAttributes={p.labelHtmlAttributes}
        htmlAttributes={p.formGroupHtmlAttributes}
      >
        {() => (
          <div className="d-flex gap-2">
            {renderPart(p.min, true, undefined, maxAsDate)}
            {renderPart(p.max, true, minAsDate, undefined)}
          </div>
        )}
      </FormGroup>
    );
  }

  return (
    <>
      {renderPart(p.min, false, undefined, maxAsDate)}
      {renderPart(p.max, false, minAsDate, undefined)}
    </>
  );
}
