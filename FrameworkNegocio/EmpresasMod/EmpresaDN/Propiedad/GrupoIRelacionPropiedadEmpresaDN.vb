''' <summary>
''' esta clase esta pensada para conetener jutnos, el grupo de propietarios que possen una mima entidad poseida
''' Garantias:
''' * todos lo propietarios possen la misma entidad poseida.
''' Riesgos:
''' * puede que no todos los propietarios de la entidad poseida sean referidos en esta entidad.
''' </summary>
''' <remarks></remarks>
Public Class GrupoIRelacionPropiedadEmpresaDN
    Inherits Framework.DatosNegocio.EntidadDN

    'Private mEntidadPoseida As IEmpresaDN
    Private mColPoseedores As ColIRelacionPropiedadDN

End Class
