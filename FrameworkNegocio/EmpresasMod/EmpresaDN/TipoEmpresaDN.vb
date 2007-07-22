#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Clase con los tipos de empresa que puede haber (Ej. Concesionarios de Toyota)
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public Class TipoEmpresaDN
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
            pMensaje = "El nombre del tipo de empresa no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function

#End Region

End Class
