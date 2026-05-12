import * as React from 'react'
import { CheckboxLine, EntityCombo, EntityLine, EntityTable, EnumLine, FontAwesomeIcon, TextBoxLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { SkillCustomizationEntity, SkillCodeEntity, SubSkillEmbedded } from '../Signum.Agent'
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { MarkdownLine } from '@framework/Lines/MarkdownLine'
import { DiffDocument } from '../../Signum.DiffLog/Templates/DiffDocument'
import { LinkButton } from '@framework/Basics/LinkButton'
import { AgentClient, SkillPropertyMeta } from '../AgentClient'
import { Navigator } from '@framework/Navigator'
import { ToolsView } from './SkillCode'

export default function SkillCustomization(p: { ctx: TypeContext<SkillCustomizationEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ labelColumns: 4 });
  const forceUpdate = useForceUpdate();

  const skillCode = ctx.value.skillCode;

  const skillCodeInfo = useAPI(
    () => skillCode ? AgentClient.API.getSkillCodeInfo(skillCode.className) : Promise.resolve(null),
    [skillCode]
  );

  async function handleConvertToSkillCustomization(ectx: TypeContext<SubSkillEmbedded>): Promise<void> {
    const skill = ectx.value.skill;
    if (!SkillCodeEntity.isInstance(skill)) return;
    const info = await AgentClient.API.getSkillCodeInfo(skill.className);
    const newEntity = SkillCustomizationEntity.New({
      skillCode: skill,
      shortDescription: info.defaultShortDescription,
      instructions: info.defaultInstructions,
    });
    const saved = await Navigator.view(newEntity);
    if (saved) {
      ectx.value.skill = saved;
      forceUpdate();
    }
  }

  return (
    <div>
      <div className="row">
        <div className="col-sm-6">
          <EntityCombo ctx={ctx4.subCtx(e => e.skillCode)}
            onChange={() => {
              ctx.value.shortDescription = null;
              ctx.value.instructions = null;
              ctx.value.properties = [];
              forceUpdate();
            }} />
        </div>
        <div className="col-sm-6">
          <TextBoxLine ctx={ctx4.subCtx(e => e.shortDescription)}
            helpText={skillCodeInfo && ctx.value.shortDescription == null
              ? `Default: ${skillCodeInfo.defaultShortDescription}`
              : undefined} />
        </div>
      </div>

      <InstructionsField ctx={ctx} info={skillCodeInfo} />

        <EntityTable ctx={ctx.subCtx(e => e.subSkills)} avoidFieldSet="h5" columns={[
          { property: e => e.activation, template: ectx => <EnumLine ctx={ectx.subCtx(e => e.activation)} /> },
          {
            property: e => e.skill, template: ectx =>
              <EntityLine ctx={ectx.subCtx(e => e.skill)} extraButtons={_c => {
                if (!SkillCodeEntity.isInstance(ectx.value.skill)) return null;
                return (
                  <LinkButton className="sf-line-button sf-extra input-group-text" title={SkillCustomizationEntity.niceName()} onClick={() => handleConvertToSkillCustomization(ectx)}>
                    <FontAwesomeIcon icon="pencil" />
                  </LinkButton>
                );
              }} />
          },
        ]} />

      {skillCodeInfo && <ToolsView tools={skillCodeInfo.tools} />}

      {skillCodeInfo && skillCodeInfo.properties.length > 0 && (
          <EntityTable ctx={ctx.subCtx(e => e.properties)} avoidFieldSet="h5" columns={[
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
              template: ectx => <PropertyValueControl ctx={ectx.subCtx(e => e.value)} 
                properties={skillCodeInfo.properties} 
                propertyName={ectx.value.propertyName} />
            },
          ]} />
      )}
    </div>
  );
}

function InstructionsField(p: {
  ctx: TypeContext<SkillCustomizationEntity>,
  info: { defaultInstructions: string } | null | undefined
}): React.JSX.Element {
  const [showDiff, setShowDiff] = React.useState(false);
  const ctx = p.ctx;

  const button = p.info && (
        <LinkButton className="ms-2 small" title={showDiff ? "Show editor" : "Show diff with default"}
          onClick={e => setShowDiff(v => !v)}>
          {showDiff ? "Show Editor" : "Show Diff"}
        </LinkButton>
      );

    return (
      <div className="mb-3">
      <div className="float-end">
      {button}
      </div>
        {showDiff && p.info ?<DiffDocument
            first={p.info?.defaultInstructions}
            second={ctx.value.instructions ?? ""}
          />:  
          <MarkdownLine ctx={ctx.subCtx(e => e.instructions)} /> }
      </div>
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

  const factory = AgentClient.getPropertyValueControl(meta.attributeName);
  if (factory) {
    return factory(p.ctx, meta);
  }

  const helpText = [
    meta.defaultValue != null ? `Default: ${meta.defaultValue}` : null,
    meta.valueHint,
  ].filter(Boolean).join(' — ') || undefined;

  return (
    <TextBoxLine ctx={p.ctx}
      helpText={helpText} />
  );
}
