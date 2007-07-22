#Region "Importaciones"
Imports Framework.DatosNegocio
''' <summary>
''' Esta clase agrupa todos los datos de una empresa, siendo estos los datos tanto de la sede
''' y todo lo que va con ella, y de empleados, y todo lo que va con ellos
''' </summary>
''' <remarks></remarks>
#End Region

<Serializable()> Public Class DatosCompletosEmpresaDN
    Inherits EntidadDN

#Region "Atributos"
    Protected mEmpresaDN As EmpresaDN
    Protected mColSedeEmpresaDN As ColSedeEmpresaDN
    Protected mColEmpleadoDN As ColEmpleadosDN
    Protected mColDepartamentos As FN.Empresas.DN.ColDepartamentosDN

#End Region

#Region "Constructores"
    Public Sub New()
        MyBase.New()
    End Sub

    'Public Sub New(ByVal pEmpresaDN As EmpresaDN)
    '    Dim mensaje As String = ""
    '    If ValidarEmpresaDN(mensaje, pEmpresaDN) Then
    '        Me.CambiarValorRef(Of EmpresaDN)(pEmpresaDN, mEmpresaDN)
    '    Else
    '        Throw New Exception
    '    End If

    '    Me.CambiarValorRef(Of ColEmpleadosDN)(New ColEmpleadosDN, mColEmpleadoDN)
    '    Me.CambiarValorRef(Of ColSedeEmpresaDN)(New ColSedeEmpresaDN, mColSedeEmpresaDN)

    '    Me.modificarEstado = EstadoDatosDN.SinModificar
    'End Sub

    'Public Sub New(ByVal pEmpresaDN As EmpresaDN, ByVal pColSedeEmpresaDN As ColSedeEmpresaDN)
    '    Dim mensaje As String = ""
    '    If ValidarEmpresaDN(mensaje, pEmpresaDN) Then
    '        Me.CambiarValorRef(Of EmpresaDN)(pEmpresaDN, mEmpresaDN)
    '    Else
    '        Throw New Exception
    '    End If

    '    If ValidarColSedeEmpresaDN(mensaje, pColSedeEmpresaDN) Then
    '        Me.CambiarValorRef(Of ColSedeEmpresaDN)(pColSedeEmpresaDN, mColSedeEmpresaDN)
    '    Else
    '        Throw New Exception(mensaje)
    '    End If

    '    Me.CambiarValorRef(Of ColEmpleadosDN)(New ColEmpleadosDN, mColEmpleadoDN)
    '    Me.modificarEstado = EstadoDatosDN.SinModificar
    'End Sub

    'Public Sub New(ByVal pEmpresaDN As EmpresaDN, ByVal pColEmpleadoDN As ColEmpleadosDN)
    '    Dim mensaje As String = ""
    '    If ValidarEmpresaDN(mensaje, pEmpresaDN) Then
    '        Me.CambiarValorRef(Of EmpresaDN)(pEmpresaDN, mEmpresaDN)
    '    End If

    '    If ValidarColEmpleadoDN(mensaje, pColEmpleadoDN) Then
    '        Me.CambiarValorRef(Of ColEmpleadosDN)(pColEmpleadoDN, mColEmpleadoDN)
    '    End If

    '    Me.CambiarValorRef(Of ColSedeEmpresaDN)(New ColSedeEmpresaDN, mColSedeEmpresaDN)
    '    Me.modificarEstado = EstadoDatosDN.SinModificar
    'End Sub

    Public Sub New(ByVal pEmpresaDN As EmpresaDN, ByVal pColSedeEmpresaDN As ColSedeEmpresaDN, ByVal pColEmpleadoDN As ColEmpleadosDN, ByVal pColDepartamentos As ColDepartamentosDN)
        Dim mensaje As String = ""


        If ValidarEmpresaDN(mensaje, pEmpresaDN) Then
            Me.CambiarValorRef(Of EmpresaDN)(pEmpresaDN, mEmpresaDN)
        Else
            Throw New Exception(mensaje)
        End If


        If ValidarColDepartamentosDN(mensaje, pColDepartamentos) Then
            Me.CambiarValorRef(Of ColDepartamentosDN)(pColDepartamentos, mColDepartamentos)
        Else
            Throw New Exception(mensaje)
        End If



        If ValidarColSedeEmpresaDN(mensaje, pColSedeEmpresaDN) Then
            Me.CambiarValorRef(pColSedeEmpresaDN, mColSedeEmpresaDN)
        Else
            Throw New Exception(mensaje)
        End If

        If ValidarColEmpleadoDN(mensaje, pColEmpleadoDN) Then
            Me.CambiarValorRef(Of ColEmpleadosDN)(pColEmpleadoDN, mColEmpleadoDN)
        Else
            Throw New Exception(mensaje)
        End If
        Me.modificarEstado = EstadoDatosDN.SinModificar

    End Sub
#End Region

#Region "Propiedades"

    Protected Overrides Property BajaPersistente() As Boolean
        Get
            Return MyBase.BajaPersistente
        End Get
        Set(ByVal value As Boolean)
            ' poner de baja a todas mis sedes, departamentos y empleados
            Dim idp As Framework.DatosNegocio.IDatoPersistenteDN

            Dim sede As SedeEmpresaDN
            For Each sede In Me.mColSedeEmpresaDN
                idp = sede
                idp.Baja = True
            Next

            Dim emple As EmpleadoDN
            For Each emple In Me.mColEmpleadoDN
                idp = emple
                idp.Baja = True
            Next

            Dim depa As DepartamentoDN
            For Each depa In Me.mColDepartamentos
                idp = depa
                idp.Baja = True
            Next

            idp = Me.mEmpresaDN
            idp.Baja = True

        End Set
    End Property

    Public Property EmpresaDN() As EmpresaDN
        Get
            Return mEmpresaDN
        End Get
        Set(ByVal value As EmpresaDN)
            Dim mensaje As String = ""
            If ValidarEmpresaDN(mensaje, value) Then
                Me.CambiarValorRef(Of EmpresaDN)(value, mEmpresaDN)
            Else
                Throw New Exception
            End If
        End Set
    End Property
    Public Property ColDepartamentos() As ColDepartamentosDN
        Get
            Return mColDepartamentos
        End Get
        Set(ByVal value As ColDepartamentosDN)
            Dim mensaje As String = ""
            If ValidarColDepartamentosDN(mensaje, value) Then
                Me.CambiarValorRef(Of ColDepartamentosDN)(value, mColDepartamentos)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property
    Public Property ColSedeEmpresaDN() As ColSedeEmpresaDN
        Get
            Return mColSedeEmpresaDN
        End Get
        Set(ByVal value As ColSedeEmpresaDN)
            Dim mensaje As String = ""
            If ValidarColSedeEmpresaDN(mensaje, value) Then
                Me.CambiarValorRef(Of ColSedeEmpresaDN)(value, mColSedeEmpresaDN)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property

    Public Property ColEmpleadosDN() As ColEmpleadosDN
        Get
            Return mColEmpleadoDN
        End Get
        Set(ByVal value As ColEmpleadosDN)
            Dim mensaje As String = ""
            If ValidarColEmpleadoDN(mensaje, value) Then
                Me.CambiarValorRef(Of ColEmpleadosDN)(value, mColEmpleadoDN)
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
    Private Function ValidarColDepartamentosDN(ByRef mensaje As String, ByVal pColDepartamentosDN As ColDepartamentosDN) As Boolean
        If pColDepartamentosDN Is Nothing Then
            mensaje = "Los datos completos de una empresa debe tener una colección de sedes"
            Return False
        ElseIf pColDepartamentosDN.Count > 0 AndAlso Not TodosDepartamentosEnEmpresa(pColDepartamentosDN) Then
            mensaje = "Las sedes de la colección deben pertenecer a la empresa de los datos completos de empresa"
            Return False
        Else
            Return True
        End If
    End Function
    Private Function ValidarColSedeEmpresaDN(ByRef mensaje As String, ByVal pColSedeEmpresaDN As ColSedeEmpresaDN) As Boolean
        If pColSedeEmpresaDN Is Nothing Then
            mensaje = "Los datos completos de una empresa debe tener una colección de sedes"
            Return False
        ElseIf pColSedeEmpresaDN.Count > 0 AndAlso Not TodasSedesEnEmpresa(pColSedeEmpresaDN) Then
            mensaje = "Las sedes de la colección deben pertenecer a la empresa de los datos completos de empresa"
            Return False
        Else
            Return True
        End If
    End Function

    Private Function ValidarColEmpleadoDN(ByRef mensaje As String, ByVal pColEmpleadoDN As ColEmpleadosDN) As Boolean
        If pColEmpleadoDN Is Nothing Then
            mensaje = "Los datos completos de una empresa debe tener una colección de empleados"
            Return False
        ElseIf pColEmpleadoDN.Count > 0 AndAlso Not TodosEmpleadosEnEmpresa(pColEmpleadoDN) Then
            mensaje = "Los empleados deben pertenecer a la empresa de los datos completos de empresa"
            Return False
        Else
            Return True
        End If
    End Function

    Protected Overridable Function ValidarEmpresaDN(ByRef mensaje As String, ByVal pEmpresaDN As EmpresaDN) As Boolean
        If pEmpresaDN Is Nothing Then
            mensaje = "Los datos completos de empresa deben tener una empresa"
            Return False
        Else
            Return True
        End If
    End Function
#End Region

#Region "Métodos"


    'Public Function RecuperarColRolDepartamentoDN() As colRolDepartamentoDN

    '    Return mColEmpleadoDN.RecuperarColRolDepartamentoDN

    'End Function


    ''' <summary>
    ''' Comprueba que todas las sedes pertenezcan a la empresa
    ''' </summary>
    ''' <param name="pColSedeEmpresaDN"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function TodasSedesEnEmpresa(ByVal pColSedeEmpresaDN As ColSedeEmpresaDN) As Boolean
        Dim e As SedeEmpresaDN
        For Each e In pColSedeEmpresaDN
            If Not SedeEnEmpresa(e) Then
                Return False
            End If
        Next
        Return True
    End Function


    Private Function TodosDepartamentosEnEmpresa(ByVal pColDepartamentosDN As ColDepartamentosDN) As Boolean
        Dim e As DepartamentoDN
        For Each e In pColDepartamentosDN
            If Not DepartamentoEnEmpresa(e) Then
                Return False
            End If
        Next
        Return True
    End Function

    ''' <summary>
    ''' Comprueba que la sede pertenezca a la empresa
    ''' </summary>
    ''' <param name="pDepartamento"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' 
    Public Function DepartamentoEnEmpresa(ByVal pDepartamento As DepartamentoDN) As Boolean
        If pDepartamento.Empresa Is Me.mEmpresaDN Then
            Return True
        Else
            Return False
        End If

    End Function

    Public Function SedeEnEmpresa(ByVal pSedeEmpresa As SedeEmpresaDN) As Boolean
        If pSedeEmpresa.Empresa Is Me.mEmpresaDN Then
            Return True
        Else
            Return False
        End If

    End Function

    Public Function SedeEnEmpresa(ByVal pHuellaSedeEmpresa As HuellaCacheSedeEmpresaDN) As Boolean
        If pHuellaSedeEmpresa.IDEmpresaPadre = Me.mEmpresaDN.ID Then
            Return True
        Else
            Return False
        End If

    End Function
    ''' <summary>
    ''' Comprueba que todos los empleados pertenezcan a la empresa
    ''' </summary>
    ''' <param name="pColEmpleadoDN"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function TodosEmpleadosEnEmpresa(ByVal pColEmpleadoDN As ColEmpleadosDN) As Boolean
        Dim e As EmpleadoDN
        For Each e In pColEmpleadoDN
            If Not e.EmpleadoEnEmpresa(mEmpresaDN) Then
                Return False
            End If
        Next
        Return True
    End Function




#End Region

#Region "Captura eventos"

    ''' <summary>
    ''' Se captura el evento del elemento a añadir a la colección de empleados, para comprobar la integridad
    ''' de los datos, de manera que el empleado a añadir pertenezca a la empresa.
    '''  Se captura el evento del elemento a añadir a la colección de sedes de empresa, para comprobar la integridad
    ''' de los datos, de manera que la sede a añadir pertenezca a la empresa.
    ''' </summary>
    ''' <param name="pSender"></param>
    ''' <param name="pElemento"></param>
    ''' <param name="pPermitido"></param>
    ''' <remarks></remarks>
    Public Overrides Sub ElementoaAñadir(ByVal pSender As Object, ByVal pElemento As Object, ByRef pPermitido As Boolean)
        If pSender Is Me.ColEmpleadosDN Then
            'Dim mensaje As String = ""
            Dim Empleado As EmpleadoDN
            Empleado = pElemento

            If Not Empleado.EmpleadoEnEmpresa(mEmpresaDN) Then
                'Throw New Exception(mensaje)
                pPermitido = False
            End If
        ElseIf pSender Is Me.mColSedeEmpresaDN Then
            'Dim mensaje As String = ""
            Dim SedeEmpresa As SedeEmpresaDN
            SedeEmpresa = pElemento

            If Not SedeEnEmpresa(SedeEmpresa) Then
                'Throw New Exception(mensaje)
                pPermitido = False
            End If
        End If

        MyBase.ElementoaAñadir(pSender, pElemento, pPermitido)
    End Sub

#End Region

End Class
