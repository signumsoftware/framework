import * as React from 'react'
import { IFile, IFilePath } from "./Signum.Entities.Files";
import { configurtions } from "./FileDownloader";
import { ModifiableEntity } from '@framework/Signum.Entities';
import * as Services from '@framework/Services'

interface FileImageProps extends React.ImgHTMLAttributes<HTMLImageElement> {
  file?: IFile & ModifiableEntity | null;
}

interface FileImageState {
  objectUrl: string | undefined;
}

export function FileImage(p: FileImageProps) {

  var [objectUrl, setObjectUrl] = React.useState<string | undefined>(undefined);
  var { file, ...rest } = p;

  React.useEffect(() => {
    if (file && !file.fullWebPath && !file.binaryFile) {
      var url = configurtions[file.Type].fileUrl!(file);
      var oldFile = file;

      Services.ajaxGetRaw({ url: url })
        .then(resp => resp.blob())
        .then(blob => setObjectUrl(URL.createObjectURL(blob)))
        .done();
    }
    return () => { objectUrl && URL.revokeObjectURL(objectUrl) };
  }, [p.file]);

  var src = file == null ? undefined :
    (file as IFilePath).fullWebPath || file.binaryFile != null ? "data:image/jpeg;base64," + file.binaryFile :
      objectUrl;
  return (
    <img {...rest} src={src} />
  );
}
