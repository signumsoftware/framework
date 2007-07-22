Imports FN.Ficheros.FicherosDN
<Serializable()> _
Public Class EntNegocioDN
    Inherits Framework.DatosNegocio.EntidadDN


#Region "Atributos"
    Protected mNodoTENGuid As String

    Protected mTipoEntNegocioReferidora As TipoEntNegoioDN
    Protected mIdEntNeg As String
    Protected mHuellaNodoTipoEntNegoio As HuellaNodoTipoEntNegoioDN
    Protected mComentario As String


    Public Property Comentario() As String
        Get
            Return mComentario
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mComentario)
        End Set
    End Property


    Public Property HuellaNodoTipoEntNegoio() As HuellaNodoTipoEntNegoioDN
        Get
            Return Me.mHuellaNodoTipoEntNegoio
        End Get
        Set(ByVal value As HuellaNodoTipoEntNegoioDN)
            Me.CambiarValorRef(Of HuellaNodoTipoEntNegoioDN)(value, mHuellaNodoTipoEntNegoio)
            Me.CambiarValorRef(Of TipoEntNegoioDN)(Nothing, mTipoEntNegocioReferidora)

        End Set
    End Property

    Public Property TipoEntNegocioReferidora() As TipoEntNegoioDN
        Get
            Return mTipoEntNegocioReferidora
        End Get
        Set(ByVal value As TipoEntNegoioDN)
            Me.CambiarValorRef(Of TipoEntNegoioDN)(value, mTipoEntNegocioReferidora)
            Me.CambiarValorRef(Of HuellaNodoTipoEntNegoioDN)(Nothing, mHuellaNodoTipoEntNegoio)
        End Set
    End Property

    Public Property IdEntNeg() As String
        Get
            Return Me.mIdEntNeg
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mIdEntNeg)
        End Set
    End Property
    ''' <summary>
    ''' el el GUID del el nodo que contine el tipo de entidad denegocio a la que se vinculara
    ''' permite diferir la asociacuin con la entidad de negocio pero apuntar a un nodo en el arbol de tipos de entidades de negocio
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property NodoTENGuid() As String
        Get
            Return mNodoTENGuid
        End Get
    End Property


    Public Sub AsignarNodoTipoEntNegoio(ByVal pNodo As NodoTipoEntNegoioDN)

        Me.CambiarValorVal(Of String)(pNodo.GUID, mNodoTENGuid)

    End Sub


    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If Me.mTipoEntNegocioReferidora Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("mTipoEntNegocioReferidora no puede ser nothing en " & Me.ToString)
        End If


        Return MyBase.EstadoIntegridad(pMensaje)

    End Function
#End Region

End Class
<Serializable()> _
Public Class ColEntNegocioDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of EntNegocioDN)

End Class