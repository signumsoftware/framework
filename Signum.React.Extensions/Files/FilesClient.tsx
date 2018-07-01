
import * as React from 'react'
import { Route } from 'react-router'

import { Dic } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { Type } from '../../../Framework/Signum.React/Scripts/Reflection'
import DynamicComponent from '../../../Framework/Signum.React/Scripts/Lines/DynamicComponent'
import { FileEntity, FilePathEntity, FileEmbedded, FilePathEmbedded, IFile } from './Signum.Entities.Files'
import FileLine from './FileLine'
import CellFormatter = Finder.CellFormatter;
import { Lite, Entity, ModifiableEntity } from "../../../Framework/Signum.React/Scripts/Signum.Entities";
import FileImageLine from './FileImageLine';

export function start(options: { routes: JSX.Element[] }) {

    registerAutoFileLine(FileEntity);
    registerAutoFileLine(FileEmbedded);

    registerAutoFileLine(FilePathEntity);
    registerAutoFileLine(FilePathEmbedded);


    Finder.formatRules.push({
        name: "WebDownload",
        isApplicable: col => col.token!.type.name === "WebDownload",
        formatter: col => new CellFormatter((cell: WebDownload) =>
            !cell ? undefined : <a href={cell.FullWebPath} download={cell.FileName}>{cell.FileName}</a>)
    });

    Finder.formatRules.push({
        name: "WebImage",
        isApplicable: col => col.token!.type.name === "WebImage",
        formatter: col => new CellFormatter((cell: WebImage) =>
            !cell ? undefined : <img src={cell.FullWebPath}/>)
    });
}


function registerAutoFileLine(type: Type<IFile & ModifiableEntity>) {
    DynamicComponent.customTypeComponent[type.typeName] = ctx => {
        const tr = ctx.propertyRoute.typeReference();
        if (tr.isCollection)
            return "continue";

        var m = ctx.propertyRoute.member;
        if (m && m.defaultFileTypeInfo && m.defaultFileTypeInfo.onlyImages)
            return <FileImageLine ctx={ctx} />;

        return <FileLine ctx={ctx}/>;
    };
}


export interface WebDownload {
    FileName: string;
    FullWebPath: string;
}

export interface WebImage {
    FullWebPath: string;
}


declare module '../../../Framework/Signum.React/Scripts/Reflection' {

    export interface MemberInfo {
        defaultFileTypeInfo?: {
            key: string,
            onlyImages: boolean,
            maxSizeInBytes: number | null,
        };
    }
}