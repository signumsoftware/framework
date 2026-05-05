import * as React from 'react'
import { SkillCodeInfo, SubSkillInfo, ToolInfo } from '../AgentClient'
import { SkillActivation } from '../Signum.Agent'

export function SkillCodeView(p: { info: SkillCodeInfo }): React.JSX.Element {
  return (
    <div className="skill-code-view">
      {p.info.defaultShortDescription && (
        <p className="text-muted fst-italic">{p.info.defaultShortDescription}</p>
      )}
      <pre className="border rounded p-2 bg-light small" style={{ whiteSpace: 'pre-wrap' }}>
        {p.info.defaultInstructions}
      </pre>
      {p.info.properties.length > 0 && (
        <div className="mt-2">
          <strong className="small">Properties</strong>
          <table className="table table-sm table-borderless small mb-1">
            <tbody>
              {p.info.properties.map((prop, i) => (
                <tr key={i}>
                  <td className="fw-semibold text-nowrap">{prop.propertyName}</td>
                  <td className="text-muted text-nowrap">{prop.propertyType}</td>
                  <td>{prop.defaultValue ?? <span className="text-muted fst-italic">null</span>}</td>
                  {prop.valueHint && <td className="text-muted fst-italic">{prop.valueHint}</td>}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <ToolsView tools={p.info.tools} />
      {p.info.subSkills.length > 0 && (
        <div className="mt-2">
          {p.info.subSkills.map((ss, i) => <SubSkillView key={i} subSkill={ss} />)}
        </div>
      )}
    </div>
  );
}

export function ToolsView(p: { tools: ToolInfo[] }): React.JSX.Element | null {
  if (!p.tools || p.tools.length === 0) return null;
  return (
    <div className="mt-2">
      <strong className="small">Tools</strong>
      {p.tools.map((tool, i) => (
        <div key={i} className="border rounded px-2 py-1 mb-1 small font-monospace">
          <span className="fw-semibold text-primary">{tool.mcpName}</span>
          <span className="text-muted">(</span>
          {tool.parameters.map((p, j) => (
            <span key={j}>
              {j > 0 && <span className="text-muted">, </span>}
              <span className={p.isRequired ? '' : 'text-muted'}>{p.name}</span>
              <span className="text-muted">: {p.type}</span>
            </span>
          ))}
          <span className="text-muted">): {tool.returnType}</span>
          {tool.description && <div className="text-muted fst-italic fw-normal">{tool.description}</div>}
        </div>
      ))}
    </div>
  );
}

function SubSkillView(p: { subSkill: SubSkillInfo }): React.JSX.Element {
  const { subSkill } = p;
  return (
    <div className="border rounded p-2 mb-2">
      <div className="d-flex align-items-center gap-2 mb-1">
        <strong>{subSkill.className}</strong>
        <span className="badge bg-secondary">{SkillActivation.niceToString(subSkill.activation)}</span>
      </div>
      <SkillCodeView info={subSkill.info} />
    </div>
  );
}
