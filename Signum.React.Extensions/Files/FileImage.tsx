import * as React from 'react'
import { IFile, IFilePath } from "./Signum.Entities.Files";
import FileDownloader from "./FileDownloader";
import { ModifiableEntity } from '@framework/Signum.Entities';
import * as Services from '@framework/Services'


interface FileImageProps extends React.ImgHTMLAttributes<HTMLImageElement>{
    file?: IFile & ModifiableEntity | null;
}

interface FileImageState {
    objectUrl: string | undefined;
}

export class FileImage extends React.Component<FileImageProps, FileImageState> {

    constructor(props: FileImageProps) {
        super(props);
        this.state = { objectUrl: undefined };
    }

    componentWillMount() {
        this.loadData(this.props.file);
    }

    componentWillReceiveProps(newProps: FileImageProps) {
        if (newProps.file != this.props.file)
            this.loadData(newProps.file);
    }

    loadData(file: IFile & ModifiableEntity | null | undefined) {

        this.setState({ objectUrl: undefined }, () => {
            if (file && !file.fullWebPath && !file.binaryFile) {
                var url = FileDownloader.configurtions[file.Type].fileUrl!(file);

                Services.ajaxGetRaw({ url: url })
                    .then(resp => resp.blob())
                    .then(blob => this.setState({ objectUrl: URL.createObjectURL(blob) }))
                    .done();
            }
        });
    }

    componentWillUnmount() {
        if (this.state.objectUrl)
            URL.revokeObjectURL(this.state.objectUrl);
    }

    render() {
        var { file, ...props } = this.props;
        var src = file == null ? undefined :
            (file as IFilePath).fullWebPath ||
                file.binaryFile != null ? "data:image/jpeg;base64," + file.binaryFile :
                this.state.objectUrl;
        return (
            <img {...props} src={src} />
        );
    }
}
