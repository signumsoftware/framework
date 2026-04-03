import * as React from 'react'
import { CheckboxLine, EntityCombo, EntityLine, EntityTable, EnumLine, TextBoxLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { AgentSkillEntity, AgentSkillPropertyOverrideEmbedded, AgentSkillSubSkillEmbedded } from '../Signum.Agent'
import { AgentSkillClient, SkillPropertyMeta } from '../AgentSkillClient'
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { AgentSkillClient } from '../AgentSkillClient'
import { MarkdownLine } from '@framework/Lines/MarkdownLine'
import { DiffDocument } from '../../Signum.DiffLog/Templates/DiffDocument'
import { LinkButton } from '@framework/Basics/LinkButton'

export default function AgentSkill(p: { ctx: TypeContext<AgentSkillEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ labelColumns: 4 });
  const forceUpdate = useForceUpdate();

  const skillCode = ctx.value.skillCode;

  const skillCodeInfo = useAPI(
    () => skillCode ? AgentSkillClient.API.getSkillCodeInfo(skillCode.fullClassName) : Promise.resolve(null),
    [skillCode]
  );

  return (
    <div>
      <div className="row">
        <div className="col-sm-6">
          <TextBoxLine ctx={ctx4.subCtx(e => e.name)} />
          <EntityCombo ctx={ctx4.subCtx(e => e.skillCode)}
            onChange={() => {
              ctx.value.shortDescription = null;
              ctx.value.instructions = null;
              ctx.value.propertyOverrides = [];
              forceUpdate();
            }} />
          <CheckboxLine ctx={ctx4.subCtx(e => e.active)} inlineCheckbox />
          <EntityCombo ctx={ctx4.subCtx(e => e.useCase)} />
        </div>
        <div className="col-sm-6">
          <TextBoxLine ctx={ctx4.subCtx(e => e.shortDescription)}
            helpText={skillCodeInfo && ctx.value.shortDescription == null
              ? `Default: ${skillCodeInfo.defaultShortDescription}`
              : undefined} />
        </div>
      </div>

      <InstructionsField ctx={ctx} info={skillCodeInfo} />

      <fieldset className="mt-3">
        <legend className="fs-6 fw-semibold">Sub-Skills</legend>
        <EntityTable ctx={ctx.subCtx(e => e.subSkills)} columns={[
          { property: e => e.activation, template: ectx => <EnumLine ctx={ectx.subCtx(e => e.activation)} /> },
          { property: e => e.skill, template: ectx => <EntityLine ctx={ectx.subCtx(e => e.skill)} /> },
        ]} />
      </fieldset>

      {skillCodeInfo && skillCodeInfo.properties.length > 0 && (
        <fieldset className="mt-3">
          <legend className="fs-6 fw-semibold">Property Overrides</legend>
          <EntityTable ctx={ctx.subCtx(e => e.propertyOverrides)} columns={[
            {
              property: e => e.propertyName,
              template: ectx => (
                <EnumLine ctx={ectx.subCtx(e => e.propertyName)}
                  optionItems={skillCodeInfo.properties.map(m => m.propertyName)}
                  onChange={() => { ectx.value.value = null; forceUpdate(); }} />
              )
            },
            {
              property: e => e.value,
              template: ectx => <PropertyValueControl ctx={ectx.subCtx(e => e.value)} properties={skillCodeInfo.properties} propertyName={ectx.value.propertyName} />
            },
          ]} />
        </fieldset>
      )}
    </div>
  );
}

function InstructionsField(p: {
  ctx: TypeContext<AgentSkillEntity>,
  info: { defaultInstructions: string } | null | undefined
}): React.JSX.Element {
  const [showDiff, setShowDiff] = React.useState(false);
  const ctx = p.ctx;

  const label = (
    <span>
      Instructions
      {p.info && (
        <LinkButton className="ms-2 small" title={showDiff ? "Show editor" : "Show diff with default"}
          onClick={e => setShowDiff(v => !v)}>
          {showDiff ? "Show Editor" : "Show Diff"}
        </LinkButton>
      )}
    </span>
  );

  if (showDiff && p.info) {
    return (
      <div className="mb-3">
        <label className="form-label">{label}</label>
        <DiffDocument
          first={p.info.defaultInstructions}
          second={ctx.value.instructions ?? p.info.defaultInstructions}
        />
      </div>
    );
  }

  return (
    <MarkdownLine ctx={ctx.subCtx(e => e.instructions)}
      helpText={p.info && ctx.value.instructions == null ? "Using default from code (.md file)" : undefined}
      label={label as any}
    />
  );
}

function PropertyValueControl(p: {
  ctx: TypeContext<string | null>,
  properties: SkillPropertyMeta[],
  propertyName: string
}): React.JSX.Element {
  const meta = p.properties.find(m => m.propertyName === p.propertyName);

  if (!meta) {
    return <TextBoxLine ctx={p.ctx} />;
  }

  const factory = AgentSkillClient.getPropertyValueControl(meta.attributeName);
  if (factory) {
    return factory(p.ctx, meta);
  }

  return (
    <TextBoxLine ctx={p.ctx}
      helpText={meta.valueHint ?? undefined} />
  );
}
