

<Serializable()> _
Public Class OrigenIdevBaseDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN
    Implements FN.GestionPagos.DN.IOrigenIImporteDebidoDN



    Protected mFAnulacion As Date
    Protected mIImporteDebidoDN As FN.GestionPagos.DN.IImporteDebidoDN
    Protected mColHEDN As Framework.DatosNegocio.ColHEDN

    Public Sub New()
        Me.CambiarValorRef(Of Framework.DatosNegocio.ColHEDN)(New Framework.DatosNegocio.ColHEDN, mColHEDN)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub

    Public Overridable Property IImporteDebidoDN() As FN.GestionPagos.DN.IImporteDebidoDN Implements FN.GestionPagos.DN.IOrigenIImporteDebidoDN.IImporteDebidoDN
        Get
            Return mIImporteDebidoDN
        End Get
        Set(ByVal value As FN.GestionPagos.DN.IImporteDebidoDN)
            Me.CambiarValorRef(Of FN.GestionPagos.DN.IImporteDebidoDN)(value, mIImporteDebidoDN)
        End Set
    End Property


    Public Overridable Property ColHEDN() As Framework.DatosNegocio.ColHEDN Implements IOrigenIImporteDebidoDN.ColHEDN
        Get
            Return mColHEDN
        End Get
        Set(ByVal value As Framework.DatosNegocio.ColHEDN)
            Me.CambiarValorRef(Of Framework.DatosNegocio.ColHEDN)(value, mColHEDN)
        End Set
    End Property

    Public Overridable Property FAnulacion() As Date Implements IOrigenIImporteDebidoDN.FAnulacion
        Get
            Return mFAnulacion
        End Get
        Set(ByVal value As Date)
            Me.CambiarValorVal(Of Date)(value, mFAnulacion)
        End Set
    End Property

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If mFAnulacion = Date.MinValue AndAlso Me.mIImporteDebidoDN.FAnulacion <> Date.MinValue Then
            pMensaje = "Un origen de importe debido activo no puede referir a un importe debido anulado"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If
        If mFAnulacion <> Date.MinValue AndAlso Me.mIImporteDebidoDN.FAnulacion = Date.MinValue Then
            pMensaje = "Un origen de importe debido anulado no puede referir a un importe debido activo"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If


        If Not Me.mFAnulacion = Me.mIImporteDebidoDN.FAnulacion Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("las fechas de anulacion estan desincronizadas")
        End If

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function

    Public Function Anulable(ByRef pMensaje As String) As Boolean Implements IOrigenIImporteDebidoDN.Anulable
        If Not Me.mFAnulacion = Date.MinValue Then

            pMensaje = "el origen de importedebido ya esta nulado"

            Return False
        Else
            Return mIImporteDebidoDN.Anulable(pMensaje)

        End If
    End Function

    Public Function Anular(ByVal fAnulacion As Date) As Object Implements IOrigenIImporteDebidoDN.Anular

        Dim mensaje As String
        If Not Me.Anulable(mensaje) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN(mensaje)
        End If


        Me.mFAnulacion = fAnulacion
        Me.mIImporteDebidoDN.FAnulacion = fAnulacion

        Return mIImporteDebidoDN

    End Function
End Class