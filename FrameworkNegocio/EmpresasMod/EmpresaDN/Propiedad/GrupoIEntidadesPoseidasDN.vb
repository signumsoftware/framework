
''' <summary>
''' esta clase esta pensada para conetener juntos, el grupo  entidades poseidas de un mismo propietario
''' Garantias:
''' * todas las entidades ontenidas son poseidas  al menos por el mimo propietario
''' Riesgos:
''' * puede que no todas las propiedades del propietario estén referidas por esta entidad.
''' </summary>
''' <remarks></remarks>
Public Class GrupoIEntidadesPoseidasDN
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mPosesiones As ColIRelacionPropiedadDN
    Protected mPoseedor As FN.Localizaciones.DN.IEntidadFiscalDN


End Class
