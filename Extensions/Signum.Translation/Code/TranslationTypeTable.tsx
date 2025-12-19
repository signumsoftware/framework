import * as React from 'react'
import { TranslationMessage } from '../Signum.Translation'
import { TranslationClient } from '../TranslationClient'
import { Dic } from '@framework/Globals'
import TextArea from '@framework/Components/TextArea';
import { useForceUpdate } from '@framework/Hooks';
import { KeyNames } from '@framework/Components';
import { AccessibleRow, AccessibleTable } from '../../../Signum/React/Basics/AccessibleTable';
import { LinkButton } from '@framework/Basics/LinkButton';

export function TranslationTypeTable(p: { type: TranslationClient.LocalizableType, result: TranslationClient.AssemblyResult, currentCulture: string }): React.JSX.Element{

  function RenderMembers(p: { type: TranslationClient.LocalizableType }): React.ReactElement<any>[] {

    const members = Dic.getKeys(Dic.getValues(type.cultures).first().members);

    return members.flatMap(me =>
      [<AccessibleRow key={me}>
        <th className="leftCell">
          {TranslationMessage.Member.niceToString()}
        </th>
        <th colSpan={4}>
          {me}
        </th>
      </AccessibleRow>]
        .concat(Dic.getValues(type.cultures).filter(loc => loc.members[me] != null).map(loc =>
          <TranslationMember key={me + "-" + loc.culture} type={type} loc={loc} edit={editCulture(loc)} member={loc.members[me]} />
        ))
    );
  }
  function editCulture(loc: TranslationClient.LocalizedType) {
    return p.currentCulture == undefined || p.currentCulture == loc.culture;
  }

  let { type, result } = p;

  return (
    <AccessibleTable
      aria-label={TranslationMessage.TranslationsOverview.niceToString()}
      className="table st"
      mapCustomComponents={new Map<React.JSXElementConstructor<any>, string>([[TranslationTypeDescription, "tr"], [RenderMembers, "tr"]])}
      multiselectable={false}
      key={type.type}
      style={{ width: "100%", margin: "10px 0" }}>
      <thead>
        <tr>
          <th className="leftCell"> {TranslationMessage.Type.niceToString()} </th>
          <th colSpan={4} className="titleCell">
            {type.type} ( {
              [
                type.hasDescription ? "Sigular" : undefined,
                type.hasPluralDescription ? "Plural" : undefined,
                type.hasGender ? "Gender" : undefined,
                type.hasMembers ? "Members" : undefined
              ].filter(a => !!a).join(" / ")} )
          </th>
        </tr>
      </thead>
      <tbody>
        {Dic.getValues(type.cultures).filter(loc => type.hasDescription && loc.typeDescription)
          .map(loc => (
            <TranslationTypeDescription key={loc.culture} edit={editCulture(loc)} loc={loc} result={result} type={type} />
          ))}
        <RenderMembers type={type} />
      </tbody>
    </AccessibleTable>
  );
}

export function TranslationMember({ type, member, loc, edit }: { type: TranslationClient.LocalizableType, loc: TranslationClient.LocalizedType; member: TranslationClient.LocalizedMember; edit: boolean }): React.JSX.Element {

  const [avoidCombo, setAvoidCombo] = React.useState(false);
  const forceUpdate = useForceUpdate();

  const handleOnTextArea = React.useCallback(function (ta: HTMLTextAreaElement | null) {
    ta && avoidCombo && ta.focus();
  }, [avoidCombo]);

  return (
    <AccessibleRow>
      <td className="leftCell">{loc.culture}</td>
      <td colSpan={4} className="monospaceCell">
        {edit ? renderEdit() : member.description}
      </td>
    </AccessibleRow>
  );


  function handleOnChange(e: React.FormEvent<any>) {
    member.description = TranslationMember.normalizeString((e.currentTarget as HTMLSelectElement).value);
    forceUpdate();
  }

  function handleAvoidCombo(e: React.FormEvent<any>) {
    e.preventDefault();
    setAvoidCombo(true);
  }

  function handleKeyDown(e: React.KeyboardEvent<any>) {
    if (e.key == KeyNames.space || e.key == "F2") { 
      e.preventDefault();
      setAvoidCombo(true);
    }
  }

  function renderEdit() {

    const translatedMembers = Dic.getValues(type.cultures)
      .filter(lt => lt.members[member.name]?.automaticTranslations)
      .flatMap(lt => lt.members[member.name].automaticTranslations.map(at => ({ culture: lt.culture, translatorName: at.translatorName, text: at.text })));

    if (!translatedMembers.length || avoidCombo)
      return (<TextArea style={{ height: "24px", width: "90%" }} minHeight="24px" value={member.description ?? ""}
        onChange={e => { member.description = e.currentTarget.value; forceUpdate(); }}
        onBlur={handleOnChange}
        innerRef={handleOnTextArea} />);

    return (
      <span>
        <select value={member.description ?? ""} onChange={handleOnChange} onKeyDown={handleKeyDown}>
          {initialElementIf(!member.description).concat(
            translatedMembers.map(a => <option key={a.culture + a.translatorName} title={`from '${a.culture}' using ${a.translatorName}`} value={a.text}>{a.text}</option>))}
        </select>
        &nbsp;
        <LinkButton title={undefined} onClick={handleAvoidCombo}>{TranslationMessage.Edit.niceToString()}</LinkButton>
      </span>
    );
  }
}

export namespace TranslationMember {
  export function normalizeString(str: string | undefined): string | undefined {
    return str;
  };
}

export function initialElementIf(condition: boolean): React.JSX.Element[] {
  return condition ? [<option key={""} value={""}>{" - "}</option>] : []
}

export interface TranslationTypeDescriptionProps {
  type: TranslationClient.LocalizableType,
  loc: TranslationClient.LocalizedType,
  edit: boolean,
  result: TranslationClient.AssemblyResult
};

export function TranslationTypeDescription(p: TranslationTypeDescriptionProps): React.ReactElement {

  const [avoidCombo, setAvoidCombo] = React.useState(false);

  const forceUpdate = useForceUpdate();
  
  const translatedTypes = Dic.getValues(p.type.cultures)
    .filter(a => a.typeDescription?.automaticTranslations)
    .flatMap(a => a.typeDescription!.automaticTranslations.map(at => ({
      singular: at.singular,
      plural: at.plural,
      gender: at.gender,
      translatorName: at.translatorName,
      culture: a.culture
    })));

  function handleOnChangeTextArea(e: React.ChangeEvent<HTMLTextAreaElement | HTMLSelectElement>) {
    const { loc } = p;
    const td = loc.typeDescription!;
    td.description = TranslationMember.normalizeString(e.currentTarget.value);

    TranslationClient.API.pluralize(loc.culture, td.description!).then(plural => {
      td.pluralDescription = plural;
      forceUpdate();
    });

    TranslationClient.API.gender(loc.culture, td.description!).then(gender => {
      td.gender = gender;
      forceUpdate();
    });

    forceUpdate();
  }

  function handleOnSelect(e: React.ChangeEvent<HTMLTextAreaElement | HTMLSelectElement>) {
    const { loc } = p;

    var val = e.currentTarget.value;
    var line = !val ? null : translatedTypes.first(a => a.singular == val);
    td.description = TranslationMember.normalizeString(line?.singular);
    td.pluralDescription = TranslationMember.normalizeString(line?.plural);
    td.gender = line?.gender;
    forceUpdate();
  }

  const handleOnTextArea = React.useCallback(function (ta: HTMLTextAreaElement | null) {
    ta && avoidCombo && ta.focus();
  }, [avoidCombo]);

  function handleAvoidCombo(e: React.FormEvent<any>) {
    setAvoidCombo(true);
  }


  function handleKeyDown(e: React.KeyboardEvent<any>) {
    if (e.key == KeyNames.space || e.key == "F2") {
      e.preventDefault();
      setAvoidCombo(true);
    }
  }

  function renderEdit() {
    const { loc } = p;
    const td = loc.typeDescription!;

    if (!translatedTypes.length || avoidCombo)
      return (
        <TextArea style={{ height: "24px", width: "90%" }} minHeight="24px" value={td.description ?? ""}
          onChange={e => { loc.typeDescription!.description = e.currentTarget.value; forceUpdate(); }}
          onBlur={handleOnChangeTextArea} innerRef={handleOnTextArea} />
      );

    return (
      <span>
        <select value={td.description ?? ""} onChange={handleOnSelect} onKeyDown={handleKeyDown}>
          {initialElementIf(!td.description).concat(
            translatedTypes.map(a => <option key={a.culture + a.translatorName} title={`from '${a.culture}' using ${a.translatorName}`} value={a.singular}>{a.singular}</option>))}
        </select>
        &nbsp;
        <LinkButton title={undefined} onClick={handleAvoidCombo}>{TranslationMessage.Edit.niceToString()}</LinkButton>
      </span>
    );
  }
  const { type, loc, edit } = p;

  const td = loc.typeDescription!;

  const pronoms = p.result.cultures[loc.culture].pronoms ?? [];

  function safeCell(content: React.ReactNode) {
    if (content === null || content === undefined || content === false)
      return <span aria-hidden="true">&nbsp;</span>;
    return content;
  }

  return (
    <AccessibleRow>
      <th className="leftCell">{loc.culture}</th>
      <th className="smallCell monospaceCell">
        {safeCell(type.hasGender && pronoms.length > 0 && (edit ?
          <select value={td.gender ?? ""} onChange={(e) => { td.gender = e.currentTarget.value; forceUpdate(); }} className={!td.gender && Boolean(td.description) ? "sf-mandatory" : undefined}>
            {initialElementIf(!td.gender).concat(
              pronoms.map(a => <option key={a.gender} value={a.gender}>{a.singular}</option>))}
          </select> :
          (pronoms.filter(a => a.gender == td.gender).map(a => a.singular).singleOrNull())
        ))}
      </th>
      <th className="monospaceCell">
        {safeCell(edit ? renderEdit() : td.description)
        }
      </th>
      <th className="smallCell">
        {safeCell(type.hasPluralDescription && type.hasGender &&
          pronoms.filter(a => a.gender == td.gender).map(a => a.plural).singleOrNull())
        }
      </th>
      <th className="monospaceCell">
        {
          safeCell(type.hasPluralDescription && (edit ?
            <TextArea style={{ height: "24px", width: "90%" }} minHeight="24px" value={td.pluralDescription ?? ""}
              className={!td.pluralDescription && Boolean(td.description) ? "sf-mandatory" : undefined}
              onChange={e => { td.pluralDescription = e.currentTarget.value; forceUpdate(); }}
              onBlur={e => { td.pluralDescription = TranslationMember.normalizeString(e.currentTarget.value); forceUpdate(); }} /> :
            td.pluralDescription))
        }
      </th>
    </AccessibleRow>
  );
}



