''' <summary>
''' esta clase agrupa el conjuento de entidades con las que a modo de contacto se relaciona una empresa
''' debe de validar que todas las relaciones contenidas en la coleccion sean de la misma empresa
''' </summary>
''' <remarks></remarks>
Public Class GrupoRelacionEmpresaEntidadDN
    Inherits Framework.DatosNegocio.EntidadDN
    'Protected mEntidad As IEmpresaDN
    Protected mEntidadesDeContacto As colRelacionEmpresaEntidadDN
End Class

''' <summary>
''' esta clase permite mapear la relacion de entre una empresa y otras entidades cuallesquiera del sistema que deben ser mapeadas.
''' por lo tanto debe de restringir que la entidad referidora sea una empresa
''' </summary>
''' <remarks></remarks>
Public Class RelacionEmpresaEntidadDN
    Inherits Framework.DatosNegocio.RelacionEspecificableDN

End Class

Public Class colRelacionEmpresaEntidadDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of RelacionEmpresaEntidadDN)
End Class