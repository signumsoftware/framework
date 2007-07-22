#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Clase con los tipos de sede que puede tener una empresa
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public Class TipoSedeDN
    Inherits EntidadDN

#Region "Constructores"
    'Public Sub New()
    'End Sub

    'Public Sub New(ByVal pid As String, ByVal pdescripcion As String)
    '    MyBase.New(pid, pdescripcion)
    'End Sub
#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If String.IsNullOrEmpty(mNombre) Then
            pMensaje = "El nombre del tipo de sede no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function

#End Region

End Class