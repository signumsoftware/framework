import { ModifiableEntity } from "@framework/Signum.Entities";
import { toFileEntity } from "../../../Signum.Files/Components/FileUploader";
import { FilePathEmbedded, IFile } from "../../../Signum.Files/Signum.Files";

export interface ImageConverter<T extends object> {
  convert(blob: Blob): Promise<T>;
}

export class AttachmentImageConverter implements ImageConverter<IFile & ModifiableEntity> {
  convert(blob: Blob): Promise<IFile & ModifiableEntity> {
    const file = blob instanceof File ? blob : new File([blob], "pastedImage." + blob.type.after("/"));

    return toFileEntity(file, {type: FilePathEmbedded, accept: "image/*" })
  } 
}
