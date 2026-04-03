import * as React from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { OmniboxClient, OmniboxResult, OmniboxMatch } from './OmniboxClient';


export abstract class OmniboxProvider<T extends OmniboxResult> {
  abstract getProviderName(): string;
  abstract renderItem(result: T): React.ReactNode[];
  abstract navigateTo(result: T): Promise<string | undefined> | undefined;
  abstract toString(result: T): string;
  abstract icon(): React.ReactNode;

  renderMatch(match: OmniboxMatch, array: React.ReactNode[]): void {

    const regex = /#+/g;

    let last = 0;
    let m: RegExpExecArray;
    while (m = regex.exec(match.boldMask)!) {
      if (m.index > last)
        array.push(<span>{match.text.substr(last, m.index - last)}</span>);

      array.push(<strong>{match.text.substr(m.index, m[0].length)}</strong>);

      last = m.index + m[0].length;
    }

    if (last < match.text.length)
      array.push(<span>{match.text.substr(last)}</span>);
  }

  coloredSpan(text: string, colorName: string): React.ReactElement {
    return <span style={{ color: colorName, lineHeight: "1.6em" }}>{text}</span>;
  }

  coloredIcon(icon: IconProp, color: string): React.ReactElement {
    return <FontAwesomeIcon aria-hidden={true} icon={icon} color={color} className="icon" />;
  }
}
