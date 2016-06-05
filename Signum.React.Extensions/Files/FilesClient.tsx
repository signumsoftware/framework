
import * as React from 'react'
import { Route } from 'react-router'

import { Dic } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { FileEntity } from './Signum.Entities.Files'
import CellFormatter = Finder.CellFormatter;
import { Lite, Entity } from "../../../Framework/Signum.React/Scripts/Signum.Entities";

export function start(options: { routes: JSX.Element[] }) {

    Finder.formatRules.push({
        name: "Lite",
        isApplicable: col => col.token.type.name === "WebDownload",
        formatter: col => new CellFormatter((cell: WebDownload) =>
            !cell ? null : <a href={cell.FullWebPath} download={cell.FileName}>{cell.FileName}</a>)
    });

    Finder.formatRules.push({
        name: "Lite",
        isApplicable: col => col.token.type.name === "WebImage",
        formatter: col => new CellFormatter((cell: WebImage) =>
            !cell ? null : <img src={cell.FullWebPath}/>)
    });
}


export interface WebDownload {
    FileName: string;
    FullWebPath: string;
}

export interface WebImage {
    FullWebPath: string;
}
 