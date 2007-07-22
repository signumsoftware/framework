#Region "Importaciones"
Imports framework.DatosNegocio
#End Region
<Serializable()> Public Class DepartamentoDN
    Inherits EntidadTemporalDN

#Region "Atributos"
    'Protected mColSubDepDN As ColDepartamentosDN
    ' Protected mColRolDeEmpresaDN As ColRolDeEmpresaDN
    Protected mEmpresa As EmpresaDN

#End Region

#Region "Constructores"

    'Public Sub New()
    '    MyBase.New()
    'End Sub

    'Public Sub New(ByVal pEmpresaDN As EmpresaDN, ByVal pColRolDeEmpresaDN As ColRolDeEmpresaDN, ByVal pNombreDepartamento As String)
    '    Dim mensaje As String = ""

    '    'Validamos que tenga empresa
    '    If ValidarNombreDepartamento(mensaje, pNombreDepartamento) Then
    '        Me.CambiarValorVal(Of String)(pNombreDepartamento, mNombre)
    '    Else
    '        Throw New Exception(mensaje)
    '    End If

    '    If ValidarEmpresa(mensaje, pEmpresaDN) Then
    '        Me.CambiarValorRef(Of EmpresaDN)(pEmpresaDN, mEmpresaDN)
    '    Else
    '        Throw New Exception(mensaje)
    '    End If

    '    'Valido la coleccion de roles
    '    'If ValidarColRolDeEmpresa(mensaje, pColRolDeEmpresaDN) Then
    '    '    Me.CambiarValorRef(Of ColRolDeEmpresaDN)(pColRolDeEmpresaDN, mColRolDeEmpresaDN)
    '    'Else
    '    '    Throw New Exception(mensaje)
    '    'End If
    '    Me.ModificarEstado = EstadoDatosDN.SinModificar
    'End Sub

    'Public Sub New(ByVal pEmpresaDN As EmpresaDN, ByVal pColRolDeEmpresaDN As ColRolDeEmpresaDN, ByVal pColSubDepDN As ColDepartamentosDN, ByVal pNombreDepartamento As String)
    '    Dim mensaje As String = ""

    '    'Validamos que tenga empresa
    '    If ValidarNombreDepartamento(mensaje, pNombreDepartamento) Then
    '        Me.CambiarValorVal(Of String)(pNombreDepartamento, mNombre)
    '    Else
    '        Throw New Exception(mensaje)
    '    End If

    '    If ValidarEmpresa(mensaje, pEmpresaDN) Then
    '        Me.CambiarValorRef(Of EmpresaDN)(pEmpresaDN, mEmpresaDN)
    '    Else
    '        Throw New Exception(mensaje)
    '    End If

    '    ''Valido la coleccion de roles
    '    'If ValidarColRolDeEmpresa(mensaje, pColRolDeEmpresaDN) Then
    '    '    Me.CambiarValorRef(Of ColRolDeEmpresaDN)(pColRolDeEmpresaDN, mColRolDeEmpresaDN)
    '    'Else
    '    '    Throw New Exception(mensaje)
    '    'End If
    '    'Me.CambiarValorRef(Of ColDepartamentosDN)(pColSubDepDN, mColSubDepDN)
    '    'Me.CambiarValorPropiedadColEntidadRef(pColSubDepDN, mColSubDepDN)

    '    Me.ModificarEstado = EstadoDatosDN.SinModificar
    'End Sub

#End Region

#Region "Propiedades"
    Public Property Empresa() As EmpresaDN
        Get
            Return mEmpresa
        End Get
        Set(ByVal value As EmpresaDN)
            Dim mensaje As String = ""
            If String.IsNullOrEmpty(mID) Then
                Me.CambiarValorRef(Of EmpresaDN)(value, mEmpresa)
            Else
                Throw New ApplicationExceptionDN("No se puede modificar la empresa del departamento")
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

    Private Function ValidarEmpresa(ByRef mensaje As String, ByVal pEmpresaDN As EmpresaDN) As Boolean
        If pEmpresaDN Is Nothing Then
            mensaje = "El departamento debe contener una empresa"
            Return False
        End If
        Return True
    End Function

    Private Function ValidarNombreDepartamento(ByRef mensaje As String, ByVal pNombre As String)
        If String.IsNullOrEmpty(pNombre) Then
            mensaje = "No puede existir un departamento sin nombre"
            Return False
        End If
        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarNombreDepartamento(pMensaje, mNombre) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarEmpresa(pMensaje, mEmpresa) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Overrides Function ToString() As String
        Dim cadena As String = ""

        cadena = mNombre

        If mEmpresa IsNot Nothing Then
            cadena = cadena & " - " & mEmpresa.ToString()
        End If

        Return cadena
    End Function

#End Region

End Class
