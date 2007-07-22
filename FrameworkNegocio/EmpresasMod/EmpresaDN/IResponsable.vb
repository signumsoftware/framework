#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Esta interfaz es implementada por una entidad que es la responsable de entidades, que podrán ser
''' tanto colecciones de empresas (agrupación de empresas) como colección de personal
''' </summary>
''' <remarks></remarks>
Public Interface IResponsableDN

#Region "Propiedades"
    Property EntidadResponsableDN() As IEntidadDN
    'Property ColEntidadesACargoDN() As ArrayListValidable(Of IEntidadDN)

    'Devuelve un clon de la colección de entidades a cargo, por lo que no se podrán añadir o eliminar elementos a la lista
    ReadOnly Property ClonColEntidadesACargoDN() As IList(Of IEntidadDN)
#End Region

#Region "Métodos"
    Function ValidarDatosResponsable(ByRef mensaje As String, ByVal pResponsable As IEntidadDN) As Boolean
    Function ValidarEntidadesACargo(ByRef mensaje As String, ByVal pColEntidadesACargoDN As IList(Of IEntidadDN)) As Boolean
#End Region

End Interface
