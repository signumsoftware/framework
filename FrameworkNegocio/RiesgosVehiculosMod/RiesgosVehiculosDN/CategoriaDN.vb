Imports Framework.DatosNegocio


<Serializable()> _
Public Class CategoriaDN
    Inherits EntidadDN

#Region "Métodos"

    Public Overrides Function ToString() As String
        If Not String.IsNullOrEmpty(Me.mNombre) Then
            Return mNombre
        End If

        Return String.Empty
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If String.IsNullOrEmpty(mNombre) Then
            pMensaje = "El nombre de la categoría no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        Me.mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class


<Serializable()> _
Public Class ColCategoriaDN
    Inherits ArrayListValidable(Of CategoriaDN)

End Class


<Serializable()> _
Public Class CategoriaModDatosDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mColModelosDatos As ColModeloDatosDN
    Protected mCategoria As CategoriaDN

#End Region

#Region "Constructores"

    Public Sub New()
        CambiarValorCol(Of ColModeloDatosDN)(New ColModeloDatosDN, mColModelosDatos)
    End Sub

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mColModelosDatos")> _
    Public Property ColModelosDatos() As ColModeloDatosDN
        Get
            Return mColModelosDatos
        End Get
        Set(ByVal value As ColModeloDatosDN)
            CambiarValorCol(Of ColModeloDatosDN)(value, mColModelosDatos)
        End Set
    End Property

    <RelacionPropCampoAtribute("mCategoria")> _
    Public Property Categoria() As CategoriaDN
        Get
            Return mCategoria
        End Get
        Set(ByVal value As CategoriaDN)
            CambiarValorRef(Of CategoriaDN)(value, mCategoria)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Function RecuperarModeloDatos(ByVal pModelo As ModeloDN, ByVal pMatricualdo As Boolean, ByVal pFecha As Date) As ModeloDatosDN
        Return Me.mColModelosDatos.RecupearModeloDatos(pModelo, pMatricualdo, pFecha)
    End Function

    Public Function ContieneModeloDatos(ByVal pModelo As ModeloDN, ByVal pMatricualdo As Boolean, ByVal pFecha As Date) As Boolean
        If RecuperarModeloDatos(pModelo, pMatricualdo, pFecha) Is Nothing Then
            Return False
        Else
            Return True

        End If
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mCategoria Is Nothing Then
            pMensaje = "La categoría de CategoriaModDatos no puede ser nula"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mColModelosDatos IsNot Nothing Then

            If Not mColModelosDatos.VerificarCategoria(mCategoria) Then
                pMensaje = "La categoría del modelo datos no es consistente con la categoría de CategoriaModDatos"
                Return EstadoIntegridadDN.Inconsistente
            End If

        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class




<Serializable()> _
Public Class ColCategoriaModDatosDN
    Inherits ArrayListValidable(Of CategoriaModDatosDN)

    Public Function RecuperarxNombreCategoria(ByVal categoriaNombre As String) As CategoriaModDatosDN
        If Not String.IsNullOrEmpty(categoriaNombre) Then

            For Each catModD As CategoriaModDatosDN In Me
                If catModD.Categoria.Nombre = categoriaNombre Then
                    Return catModD
                End If
            Next

        End If

        Return Nothing
    End Function

    Public Function RecuperarxGUIDCategoria(ByVal categoriaGUID As String) As CategoriaModDatosDN
        If Not String.IsNullOrEmpty(categoriaGUID) Then

            For Each catModD As CategoriaModDatosDN In Me
                If catModD.Categoria.GUID = categoriaGUID Then
                    Return catModD
                End If
            Next

        End If

        Return Nothing
    End Function

End Class


<Serializable()> _
Public Class HECategoriaModDatosDN
    Inherits HuellaEntidadTipadaDN(Of CategoriaModDatosDN)

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal categoriaModDatos As CategoriaModDatosDN, ByVal relacionIntegridad As HuellaEntidadDNIntegridadRelacional)
        MyBase.New(categoriaModDatos, HuellaEntidadDNIntegridadRelacional.ninguna)
    End Sub

#End Region

End Class