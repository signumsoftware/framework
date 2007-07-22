#Region "Importaciones"
Imports Framework.DatosNegocio
Imports Framework.DatosNegocio.Localizaciones.Temporales
#End Region
<Serializable()> Public Class RolDepartamentoDN
    Inherits EntidadDN

#Region "Atributos"
    Protected mRolDeEmpresaDN As RolDeEmpresaDN
    Protected mDepartamentoDN As DepartamentoDN
    Protected mPeriodo As IntervaloFechasDN

    'protected mHDepartamentoDN As HuellaEntidadTipadaDepartamento
#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pRolDeEmpresaDN As RolDeEmpresaDN, ByVal pDepartamentoDN As DepartamentoDN, ByVal pPeriodo As IntervaloFechasDN)
        Dim mensaje As String = ""
        If Me.ValidarRolEmpresa(mensaje, pRolDeEmpresaDN) Then
            Me.CambiarValorRef(Of RolDeEmpresaDN)(pRolDeEmpresaDN, mRolDeEmpresaDN)
        Else
            Throw New Exception(mensaje)
        End If
        If Me.ValidarDepartamento(mensaje, pDepartamentoDN) Then
            Me.CambiarValorRef(Of DepartamentoDN)(pDepartamentoDN, mDepartamentoDN)
        Else
            Throw New Exception(mensaje)
        End If
        Me.CambiarValorRef(Of IntervaloFechasDN)(pPeriodo, mPeriodo)

        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"

    Public Property RolDeEmpresaDN() As RolDeEmpresaDN
        Get
            Return mRolDeEmpresaDN
        End Get
        Set(ByVal value As RolDeEmpresaDN)
            Dim mensaje As String = ""
            If Me.ValidarRolEmpresa(mensaje, value) Then
                Me.CambiarValorRef(Of RolDeEmpresaDN)(value, mRolDeEmpresaDN)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property

    Public Property DepartamentoDN() As DepartamentoDN
        Get
            Return mDepartamentoDN
        End Get
        Set(ByVal value As DepartamentoDN)
            Dim mensaje As String = ""
            If Me.ValidarDepartamento(mensaje, value) Then
                Me.CambiarValorRef(Of DepartamentoDN)(value, mDepartamentoDN)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property

    Public ReadOnly Property EstadoModificacion() As Framework.DatosNegocio.EstadoDatosDN
        Get
            Return MyBase.Estado
        End Get
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarRolEmpresa(ByRef mensaje As String, ByVal pRolDeEmpresaDN As RolDeEmpresaDN) As Boolean
        If pRolDeEmpresaDN Is Nothing OrElse pRolDeEmpresaDN.Nombre = String.Empty Then
            mensaje = "Debe establecer un rol"
            Return False
        End If
        Return True
    End Function

    ''' <summary>
    ''' Valido que existe un departamento, y que contiene datos para poder crear su huella
    ''' </summary>
    ''' <param name="mensaje"></param>
    ''' <param name="pDepartamentoDN"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' 
    Private Function ValidarDepartamento(ByRef mensaje As String, ByVal pDepartamentoDN As DepartamentoDN) As Boolean
        If pDepartamentoDN Is Nothing OrElse pDepartamentoDN.Nombre Is Nothing Then
            mensaje = "Debe establecer un departamento"
            Return False
        End If
        Return True
    End Function
#End Region

End Class


