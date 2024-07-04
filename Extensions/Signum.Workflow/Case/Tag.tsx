import * as React from 'react'
import { CaseTagTypeEntity } from '../Signum.Workflow'
import { Color } from '@framework/Basics/Color'
import "./Tag.css"

export default function Tag(p : { tag: CaseTagTypeEntity }): React.JSX.Element {
  const tag = p.tag;
  var color = Color.tryParse(tag.color!) ?? Color.Black;

  return (
    <span className="case-tag" style={{
      color: color.opositePole().toString(),
      borderColor: color.lerp(0.5, Color.Black).toString(),
      backgroundColor: color.toString(),
    }} title={tag.name ?? ""}>{tag.name}</span>
  );
}
