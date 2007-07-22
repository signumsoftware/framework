Imports Framework.DatosNegocio

<Serializable()> _
Public Class ModeloDatosDN
    Inherits EntidadTemporalDN

#Region "Atributos"

    Protected mModelo As ModeloDN
    Protected mMatriculado As Boolean
    Protected mCategoria As CategoriaDN

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mModelo")> _
    Public Property Modelo() As ModeloDN
        Get
            Return mModelo
        End Get
        Set(ByVal value As ModeloDN)
            CambiarValorRef(Of ModeloDN)(value, mModelo)
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

    Public Property Matriculado() As Boolean
        Get
            Return mMatriculado
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mMatriculado)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String
        Dim cadena As String = String.Empty

        If Modelo IsNot Nothing Then
            cadena = Modelo.ToString()
        End If

        If mMatriculado Then
            cadena = cadena & " - Matriculado"
        Else
            cadena = cadena & " - No matriculado"
        End If

        If Categoria IsNot Nothing Then
            cadena = cadena & " - " & Categoria.ToString()
        End If

        Return cadena
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If mModelo Is Nothing Then
            pMensaje = "El modelo de un ModeloDatos no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mCategoria Is Nothing Then
            pMensaje = "La categoría de un ModeloDatos no puede ser nula"
            Return EstadoIntegridadDN.Inconsistente
        End If

        Me.mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region


End Class


<Serializable()> _
Public Class ColModeloDatosDN
    Inherits ArrayListValidableEntTemp(Of ModeloDatosDN)

    Public Function RecupearModeloDatos(ByVal pModelo As ModeloDN, ByVal pMatricualdo As Boolean, ByVal pFecha As Date) As ModeloDatosDN

        For Each md As ModeloDatosDN In Me
            If md.Matriculado = pMatricualdo AndAlso md.Modelo.GUID = pModelo.GUID AndAlso md.Contiene(pFecha) Then
                Return md
            End If

        Next
        Return Nothing

    End Function

    ''' <summary>
    ''' Verifica si todos los ModeloDatos de la colección pertenecen a la misma categoría que la pasada como parámetro
    ''' </summary>
    ''' <param name="categoria"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function VerificarCategoria(ByVal categoria As CategoriaDN) As Boolean
        If categoria Is Nothing Then
            Return False
        End If

        For Each md As ModeloDatosDN In Me
            If md.Categoria.GUID <> categoria.GUID Then
                Return False
            End If
        Next

        Return True

    End Function

    Public Function RecuperarColModelos() As ColModeloDN
        Dim colModelos As New ColModeloDN()

        For Each md As ModeloDatosDN In Me
            colModelos.AddUnico(md.Modelo)
        Next

        Return colModelos

    End Function

    Public Function RecuperarColModeloDatosxModelo(ByVal modelo As ModeloDN) As ColModeloDatosDN
        Dim colMD As New ColModeloDatosDN()

        If modelo IsNot Nothing Then
            For Each md As ModeloDatosDN In Me
                If md.Modelo.GUID = modelo.GUID Then
                    colMD.AddUnico(md)
                End If
            Next
        End If

        Return colMD

    End Function

End Class