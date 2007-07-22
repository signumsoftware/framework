#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Esta clase nos permite relacionar uno a uno las Sedes de una empresa y los departamentos de la misma
''' y además comprueba que los datos que nos suministran son reales
''' </summary>
''' <remarks></remarks>
Public Class RelacionSedeEmpresaDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mDepartamento As DepartamentoDN
    Protected mSedeEmpresa As SedeEmpresaDN

#End Region

#Region "Constructores"

    'Public Sub New()
    '    MyBase.New()
    'End Sub

    'Public Sub New(ByVal pDepartamento As DepartamentoDN, ByVal pSedeEmpresa As SedeEmpresaDN)       
    '    Try
    '        ValidarRelacionDepartamentoDNSedeEmpresaDN(pDepartamento, pSedeEmpresa)
    '        Me.CambiarValorRef(Of DepartamentoDN)(pDepartamento, mDepartamento)
    '        Me.CambiarValorRef(Of SedeEmpresaDN)(pSedeEmpresa, mSedeEmpresa)
    '    Catch ex As Exception
    '        Throw ex
    '    End Try

    '    Me.modificarEstado = EstadoDatosDN.SinModificar
    'End Sub

#End Region

#Region "Propiedades"

    Public Property Departamento() As DepartamentoDN
        Get
            Return mDepartamento
        End Get
        Set(ByVal value As DepartamentoDN)
            CambiarValorRef(Of DepartamentoDN)(value, mDepartamento)
        End Set
    End Property

    Public Property SedeEmpresa() As SedeEmpresaDN
        Get
            Return mSedeEmpresa
        End Get
        Set(ByVal value As SedeEmpresaDN)
            CambiarValorRef(Of SedeEmpresaDN)(value, mSedeEmpresa)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarRelacionDepartamentoDNSedeEmpresaDN(pMensaje, mDepartamento, mSedeEmpresa) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

#Region "Validaciones"

    Private Function ValidarRelacionDepartamentoDNSedeEmpresaDN(ByRef mensaje As String, ByVal pDepartamentoDN As DepartamentoDN, ByVal pSedeEmpresaDN As SedeEmpresaDN) As Boolean
        If pDepartamentoDN Is Nothing OrElse pSedeEmpresaDN Is Nothing OrElse pDepartamentoDN.Empresa IsNot pSedeEmpresaDN.Empresa Then
            mensaje = "El departamento y la sede de la empresa deben pertenecer a la misma empresa"
            Return False
        End If

        Return True
    End Function

#End Region

End Class
