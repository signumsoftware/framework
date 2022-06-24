import * as React from 'react'
import { IFile, IFilePath } from "./Signum.Entities.Files";
import { configurtions } from "./FileDownloader";
import { Entity, isLite, isModifiableEntity, Lite, ModifiableEntity } from '@framework/Signum.Entities';
import * as Services from '@framework/Services'
import { PropertyRoute } from '@framework/Lines';
import { useFetchInState } from '../../Signum.React/Scripts/Navigator';

interface FileImageProps extends React.ImgHTMLAttributes<HTMLImageElement> {
  file?: IFile & ModifiableEntity | Lite<IFile & Entity> | null;
}

export function FileImage(p: FileImageProps) {

  var [objectUrl, setObjectUrl] = React.useState<string | undefined>(undefined);
  var { file, ...rest } = p;

  React.useEffect(() => {
    if (file) {

      if (isModifiableEntity(file) && (file.fullWebPath || file.binaryFile))
        return;

      var url =
        isLite(file) ?
          configurtions[file.EntityType].fileLiteUrl!(file) :
          configurtions[file.Type].fileUrl!(file);

      Services.ajaxGetRaw({ url: url })
        .then(resp => resp.blob())
        .then(blob => setObjectUrl(URL.createObjectURL(blob)))
        .done();

    }
    return () => { objectUrl && URL.revokeObjectURL(objectUrl) };
  }, [p.file]);

  var src = file == null ? undefined :
    isModifiableEntity(file) && file.fullWebPath ? file.fullWebPath :
      isModifiableEntity(file) && file.binaryFile ? "data:image/jpeg;base64," + file.binaryFile :
        objectUrl;

  return (
    <img {...rest} src={src} />
  );
}
