Imports Framework.DatosNegocio

<Serializable()> _
Public Class IdentificacionDocumentoDN
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mTipoFichero As TipoFicheroDN

    Protected mIdentificacion As String
    Protected mIdentificacionIncorrecta As String

    Public ReadOnly Property IdentificacionIncorrecta() As String

        Get
            Return mIdentificacionIncorrecta
        End Get


    End Property
    Public Property Identificacion() As String

        Get
            Return mIdentificacion
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mIdentificacion)

        End Set
    End Property

    <RelacionPropCampoAtribute("mTipoFichero")> _
    Public Property TipoFichero() As TipoFicheroDN

        Get
            Return mTipoFichero
        End Get

        Set(ByVal value As TipoFicheroDN)
            CambiarValorRef(Of TipoFicheroDN)(value, mTipoFichero)
            If value IsNot Nothing Then
                CambiarValorVal(Of String)(Nothing, mIdentificacionIncorrecta)
            End If
        End Set
    End Property


    Public Sub MarcarComoIdentificacionIncorrecta()
        CambiarValorVal(Of String)(mIdentificacion, mIdentificacionIncorrecta)
        Identificacion = Nothing

    End Sub


    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN

        If mTipoFichero Is Nothing Then
            pMensaje = "IdentificacionDocumentoDN Debe de estar reolacioando con un  tipo de documento  "
            Return EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function




    Public Function Identificable(ByRef pMensaje As String) As Boolean

        If mTipoFichero Is Nothing Then
            pMensaje = "IdentificacionDocumentoDN Debe de estar reolacioando con un  tipo de documento  "
            Return False
        End If

        If String.IsNullOrEmpty(Me.mIdentificacion) Then
            pMensaje = "no dispone de valor de identificacion  "
            Return False
        End If

        Return True
    End Function

    Public Function Identificado(ByRef pMensaje As String) As Boolean

        If mTipoFichero Is Nothing Then
            pMensaje = "IdentificacionDocumentoDN Debe de estar reolacioando con un  tipo de documento  "
            Return False
        End If

        If String.IsNullOrEmpty(Me.mIdentificacion) Then
            pMensaje = "no dispone de valor de identificacion  "
            Return False
        End If

        If String.IsNullOrEmpty(Me.mID) Then
            pMensaje = "no dispone de identificador de sistema (id)  "
            Return False
        End If


        Return True
    End Function

End Class





<Serializable()> _
Public Class ColIdentificacionDocumentoDN
    Inherits ArrayListValidable(Of IdentificacionDocumentoDN)


    Public Function RecuperarIdentificables() As ColIdentificacionDocumentoDN


        Dim col As New ColIdentificacionDocumentoDN

        For Each idendoc As IdentificacionDocumentoDN In Me
            Dim mensaje As String

            If Not idendoc.Identificado(mensaje) AndAlso idendoc.Identificable(mensaje) Then
                col.Add(idendoc)

            End If

        Next


        Return col

    End Function

    Public Function CalcularGradoIdetificacion() As GradoIdetificacion



        If Me.Count = 0 Then
            Return GradoIdetificacion.ningunoIdentificable
        End If

        Dim resultado As GradoIdetificacion = GradoIdetificacion.ningunoIdentificable

        Dim algunoNoIdentificado As Boolean = False
        Dim algunoNoIdentificable As Boolean = False

        For Each idendoc As IdentificacionDocumentoDN In Me
            Dim mensaje As String


            If Not idendoc.Identificado(mensaje) Then

                algunoNoIdentificado = True
                If idendoc.Identificable(mensaje) Then
                    resultado = GradoIdetificacion.algunoIdentificable
                Else
                    algunoNoIdentificable = True

                End If

            End If

        Next

        If Not algunoNoIdentificado Then
            Return GradoIdetificacion.todosIdentificados
        End If

        If Not algunoNoIdentificable AndAlso resultado = GradoIdetificacion.algunoIdentificable Then
            resultado = GradoIdetificacion.todosIdentificable
        End If

        Return resultado

    End Function


End Class

Public Enum GradoIdetificacion
    ningunoIdentificable = 0
    algunoIdentificable = 10
    todosIdentificable = 20
    'algunoIdentificado = 20
    todosIdentificados = 30
End Enum


