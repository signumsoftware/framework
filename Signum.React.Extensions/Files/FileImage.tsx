import * as React from 'react'
import { IFile, IFilePath } from "./Signum.Entities.Files";

export interface FileImageProps extends React.ImgHTMLAttributes<HTMLImageElement> {
    file?: IFile | null;
}

export class FileImage extends React.Component<FileImageProps> {
    static defaultProps = {
        className: "img-fluid"
    };

    render() {
        var { file, ...props } = this.props;
        var src = file == null ? undefined : (file as IFilePath).fullWebPath || "data:image/jpeg;base64," + file.binaryFile;
        return (
            <img {...props} src={src} />
        );
    }
}