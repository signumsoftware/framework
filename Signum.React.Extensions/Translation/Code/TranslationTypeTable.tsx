import * as React from 'react'
import { TranslationMessage } from '../Signum.Entities.Translation'
import { API, AssemblyResult, LocalizedType, LocalizableType, LocalizedMember } from '../TranslationClient'
import { Dic } from '@framework/Globals'
import TextArea from '@framework/Components/TextArea';
import { useForceUpdate } from '@framework/Hooks';

export function TranslationTypeTable(p : { type: LocalizableType, result: AssemblyResult, currentCulture: string }){

  function renderMembers(type: LocalizableType): React.ReactElement<any>[] {

    const members = Dic.getKeys(Dic.getValues(type.cultures).first().members);

    return members.flatMap(me =>
      [<tr key={me}>
        <th className="leftCell">
          {TranslationMessage.Member.niceToString()}
        </th>
        <th colSpan={4}>
          {me}
        </th>
      </tr>]
        .concat(Dic.getValues(type.cultures).filter(loc => loc.members[me] != null).map(loc =>
          <TranslationMember key={me + "-" + loc.culture} type={type} loc={loc} edit={editCulture(loc)} member={loc.members[me]} />
        ))
    );

  }

  function editCulture(loc: LocalizedType) {
    return p.currentCulture == undefined || p.currentCulture == loc.culture;
  }
  let { type, result } = p;

  return (
    <table style={{ width: "100%", margin: "10px 0" }} className="st" key={type.type}>
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
        {type.hasDescription && Dic.getValues(type.cultures).filter(loc => loc.typeDescription)
          .map(loc => <TranslationTypeDescription key={loc.culture} edit={editCulture(loc)} loc={loc} result={p.result} type={type} />)}
        {type.hasMembers && renderMembers(type)}
      </tbody>
    </table>
  );
}

export function TranslationMember({ type, member, loc, edit }: { type: LocalizableType, loc: LocalizedType; member: LocalizedMember; edit: boolean }) {

  const [avoidCombo, setAvoidCombo] = React.useState(false);
  const forceUpdate = useForceUpdate();

  const handleOnTextArea = React.useCallback(function (ta: HTMLTextAreaElement | null) {
    ta && avoidCombo && ta.focus();
  }, [avoidCombo]);

  return (
    <tr >
      <td className="leftCell">{loc.culture}</td>
      <td colSpan={4} className="monospaceCell">
        {edit ? renderEdit() : member.description}
      </td>
    </tr>
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
    if (e.keyCode == 32 || e.keyCode == 113) { //SPACE OR F2
      e.preventDefault();
      setAvoidCombo(true);
    }
  }

  function renderEdit() {

    const translatedMembers = Dic.getValues(type.cultures).map(lt => ({ culture: lt.culture, member: lt.members[member.name] })).filter(a => a.member != null && a.member.translatedDescription != null);
    if (!translatedMembers.length || avoidCombo)
      return (<TextArea style={{ height: "24px", width: "90%" }} minHeight="24px" value={member.description ?? ""}
        onChange={e => { member.description = e.currentTarget.value; forceUpdate(); }}
        onBlur={handleOnChange}
        innerRef={handleOnTextArea} />);

    return (
      <span>
        <select value={member.description ?? ""} onChange={handleOnChange} onKeyDown={handleKeyDown}>
          {initialElementIf(member.description == undefined).concat(
            translatedMembers.map(a => <option key={a.culture} value={a.member.translatedDescription}>{a.member.translatedDescription}</option>))}
        </select>
        &nbsp;
                <a href="#" onClick={handleAvoidCombo}>{TranslationMessage.Edit.niceToString()}</a>
      </span>
    );
  }
}

TranslationMember.normalizeString = (str: string): string => {
  return str;
};

function initialElementIf(condition: boolean) {
  return condition ? [<option key={""} value={""}>{" - "}</option>] : []
}

export interface TranslationTypeDescriptionProps {
  type: LocalizableType,
  loc: LocalizedType,
  edit: boolean,
  result: AssemblyResult
};

export function TranslationTypeDescription(p: TranslationTypeDescriptionProps) {

  const [avoidCombo, setAvoidCombo] = React.useState(false);

  const forceUpdate = useForceUpdate();
  


  function handleOnChange(e: React.ChangeEvent<HTMLTextAreaElement | HTMLSelectElement>) {
    const { loc } = p;
    const td = loc.typeDescription!;
    td.description = TranslationMember.normalizeString(e.currentTarget.value);

    API.pluralize(loc.culture, td.description).then(plural => {
      td.pluralDescription = plural;
      forceUpdate();
    }).done();

    API.gender(loc.culture, td.description).then(gender => {
      td.gender = gender;
      forceUpdate();
    }).done();

    forceUpdate();
  }

  const handleOnTextArea = React.useCallback(function (ta: HTMLTextAreaElement | null) {
    ta && avoidCombo && ta.focus();
  }, [avoidCombo]);

  function handleAvoidCombo(e: React.FormEvent<any>) {
    e.preventDefault();
    setAvoidCombo(true);
  }


  function handleKeyDown(e: React.KeyboardEvent<any>) {
    if (e.keyCode == 32 || e.keyCode == 113) { //SPACE OR F2
      e.preventDefault();
      setAvoidCombo(true);
    }
  }

  function renderEdit() {
    const { loc } = p;
    const td = loc.typeDescription!;

    const translatedTypes = Dic.getValues(p.type.cultures).filter(a => a.typeDescription != null && a.typeDescription.translatedDescription != null);
    if (!translatedTypes.length || avoidCombo)
      return (
        <TextArea style={{ height: "24px", width: "90%" }} minHeight="24px" value={td.description ?? ""}
          onChange={e => { loc.typeDescription!.description = e.currentTarget.value; forceUpdate(); }}
          onBlur={handleOnChange} innerRef={handleOnTextArea} />
      );

    return (
      <span>
        <select value={td.description ?? ""} onChange={handleOnChange} onKeyDown={handleKeyDown}>
          {initialElementIf(td.description == undefined).concat(
            translatedTypes.map(a => <option key={a.culture} value={a.typeDescription!.translatedDescription}>{a.typeDescription!.translatedDescription}</option>))}
        </select>
        &nbsp;
                <a href="#" onClick={handleAvoidCombo}>{TranslationMessage.Edit.niceToString()}</a>
      </span>
    );
  }
  const { type, loc, edit } = p;

  const td = loc.typeDescription!;

  const pronoms = p.result.cultures[loc.culture].pronoms ?? [];

  return (
    <tr>
      <th className="leftCell">{loc.culture}</th>
      <th className="smallCell monospaceCell">
        {type.hasGender && (edit ?
          <select value={td.gender ?? ""} onChange={(e) => { td.gender = e.currentTarget.value; forceUpdate(); }}>
            {initialElementIf(td.gender == undefined).concat(
              pronoms.map(a => <option key={a.gender} value={a.gender}>{a.singular}</option>))}
          </select> :
          (pronoms.filter(a => a.gender == td.gender).map(a => a.singular).singleOrNull()))
        }
      </th>
      <th className="monospaceCell">
        {edit ? renderEdit() : td.description
        }
      </th>
      <th className="smallCell">
        {type.hasPluralDescription && type.hasGender &&
          pronoms.filter(a => a.gender == td.gender).map(a => a.plural).singleOrNull()
        }
      </th>
      <th className="monospaceCell">
        {
          type.hasPluralDescription && (edit ?
            <TextArea style={{ height: "24px", width: "90%" }} minHeight="24px" value={td.pluralDescription ?? ""}
              onChange={e => { td.pluralDescription = e.currentTarget.value; forceUpdate(); }}
              onBlur={e => { td.pluralDescription = TranslationMember.normalizeString(e.currentTarget.value); forceUpdate(); }} /> :
            td.pluralDescription)
        }
      </th>
    </tr>
  );
}



