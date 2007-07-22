#Region "Importaciones"
Imports Framework.DatosNegocio
Imports Framework.DatosNegocio.Localizaciones.Temporales

#End Region

<Serializable()> _
Public Class ResponsableDePersonalDN
    Inherits EntidadDN
    Implements IResponsableDN

#Region "Atributos"
    Protected mEntidadResponsable As EmpleadoDN
    Protected mColEmpleadosACargoDN As ColEmpleadosDN
    Protected mDepartamentoACargoDN As DepartamentoDN
    Protected mFijadoDepartamentoResponsable As Boolean = False
    Protected mFijadoDepartamentoSubordinados As Boolean = False
    Protected mPeriodo As IntervaloFechasDN
#End Region

#Region "Constructores"
    Public Sub New()
        Me.CambiarValorRef(Of ColEmpleadosDN)(New ColEmpleadosDN, mColEmpleadosACargoDN)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

    Public Sub New(ByVal pResponsable As EmpleadoDN)
        Dim mensaje As String = ""
        If ValidarDatosResponsable(mensaje, pResponsable) Then
            Me.CambiarValorRef(Of EmpleadoDN)(pResponsable, mEntidadResponsable)
        Else
            Throw New Exception(mensaje)
        End If

        Me.CambiarValorRef(Of ColEmpleadosDN)(New ColEmpleadosDN, mColEmpleadosACargoDN)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub


    Public Sub New(ByVal pResponsable As EmpleadoDN, ByVal pColEmpleadosACargoDN As ColEmpleadosDN, ByVal pDepartamentoACargoDN As DepartamentoDN, ByVal pFijadoDepartamentoResponsable As Boolean, ByVal pFijadoDepartamentoSubordinados As Boolean, ByVal pPeriodo As IntervaloFechasDN)
        Dim mensaje As String = ""
        If ValidarDatosResponsable(mensaje, pResponsable) Then
            Me.CambiarValorRef(Of EmpleadoDN)(pResponsable, mEntidadResponsable)
        Else
            Throw New Exception(mensaje)
        End If

        Me.CambiarValorVal(Of Boolean)(pFijadoDepartamentoResponsable, mFijadoDepartamentoResponsable)
        Me.CambiarValorVal(Of Boolean)(pFijadoDepartamentoSubordinados, mFijadoDepartamentoSubordinados)

        If ValidarEntidadesACargo(mensaje, pColEmpleadosACargoDN.ToArray()) Then
            Me.CambiarValorRef(Of ColEmpleadosDN)(pColEmpleadosACargoDN, mColEmpleadosACargoDN)
        Else
            Throw New Exception(mensaje)
        End If

        If ValidarDepartamentoACargo(mensaje, pDepartamentoACargoDN) Then
            Me.CambiarValorRef(Of DepartamentoDN)(pDepartamentoACargoDN, Me.DepartamentoACargoDN)
        Else
            Throw New Exception(mensaje)
        End If
        Me.CambiarValorRef(Of IntervaloFechasDN)(pPeriodo, mPeriodo)

        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub
#End Region

#Region "Propiedades"

    Protected Overrides Property BajaPersistente() As Boolean
        Get
            Return MyBase.BajaPersistente
        End Get
        Set(ByVal value As Boolean)

            If value Then
                Me.mPeriodo.FFinal = Now
            Else
                Throw New ApplicationException("el proceso de baja no es reversible")
            End If
            MyBase.BajaPersistente = value
        End Set
    End Property
    Public ReadOnly Property ClonColEntidadesACargoDN() As System.Collections.Generic.IList(Of Framework.DatosNegocio.IEntidadDN) Implements IResponsableDN.ClonColEntidadesACargoDN
        Get
            Return New List(Of Framework.DatosNegocio.IEntidadDN)(mColEmpleadosACargoDN.ToArray())
        End Get
    End Property

    Public Property EntidadResponsableDN() As Framework.DatosNegocio.IEntidadDN Implements IResponsableDN.EntidadResponsableDN
        Get
            Return mEntidadResponsable
        End Get
        Set(ByVal value As Framework.DatosNegocio.IEntidadDN)
            Dim mensaje As String = ""
            If ValidarDatosResponsable(mensaje, value) Then
                Me.CambiarValorRef(Of EmpleadoDN)(value, mEntidadResponsable)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property

    Public Property ColEmpleadosACargo() As ColEmpleadosDN
        Get
            Return mColEmpleadosACargoDN
        End Get
        Set(ByVal value As ColEmpleadosDN)
            Dim mensaje As String = ""
            If ValidarEntidadesACargo(mensaje, value.ToArray()) Then
                Me.CambiarValorRef(Of ColEmpleadosDN)(value, mColEmpleadosACargoDN)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property

    Public Property DepartamentoACargoDN() As DepartamentoDN
        Get
            Return mDepartamentoACargoDN
        End Get
        Set(ByVal value As DepartamentoDN)
            Dim mensaje As String = ""
            If ValidarDepartamentoACargo(mensaje, value) Then
                Me.CambiarValorRef(Of DepartamentoDN)(value, mDepartamentoACargoDN)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property

    Public Property FijadoDepartamentoResponsable() As Boolean
        Get
            Return mFijadoDepartamentoResponsable
        End Get
        Set(ByVal value As Boolean)
            Dim mensaje As String = ""

            If value Then
                mFijadoDepartamentoResponsable = True
                If ValidarDepartamentoACargo(mensaje, Me.mDepartamentoACargoDN) Then
                    mFijadoDepartamentoResponsable = False
                    Me.CambiarValorVal(Of Boolean)(value, mFijadoDepartamentoResponsable)
                Else
                    mFijadoDepartamentoResponsable = False
                    Throw New Exception(mensaje)
                End If
            End If

        End Set
    End Property

    Public Property FijadoDepartamentoSubordinados() As Boolean
        Get
            Return mFijadoDepartamentoSubordinados
        End Get
        Set(ByVal value As Boolean)
            Dim mensaje As String = ""

            If value Then
                mFijadoDepartamentoSubordinados = True
                If ValidarEntidadesACargo(mensaje, Me.ClonColEntidadesACargoDN) Then
                    mFijadoDepartamentoSubordinados = False
                    Me.CambiarValorVal(Of Boolean)(value, mFijadoDepartamentoSubordinados)
                Else
                    mFijadoDepartamentoSubordinados = False
                    Throw New Exception(mensaje)
                End If
            End If

        End Set
    End Property

    Public ReadOnly Property FijadoDepartamento() As Boolean
        Get
            If mFijadoDepartamentoResponsable AndAlso mFijadoDepartamentoSubordinados Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    Public ReadOnly Property EstadoModificacion() As Framework.DatosNegocio.EstadoDatosDN
        Get
            Return MyBase.Estado
        End Get
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarDatosResponsable(ByRef mensaje As String, ByVal pResponsable As Framework.DatosNegocio.IEntidadDN) As Boolean Implements IResponsableDN.ValidarDatosResponsable
        If pResponsable Is Nothing Then
            mensaje = "Todo responsable debe ser un empleado"
            Return False
        End If

        'Si FijadoDepartamentoResponsable está a true, hay que comprobar que el departamento del responsable sea
        'el mismo que su departamento a cargo
        Dim Empleado As EmpleadoDN
        Empleado = pResponsable
        'If Me.FijadoDepartamentoResponsable AndAlso Empleado.colRolDepartamentoDN Is Nothing AndAlso Not Empleado.colRolDepartamentoDN.DepartamentoEnCol(Me.mDepartamentoACargoDN) Then
        '    mensaje = "El departamento del responsable de personal debe ser el mismo que su departamento a cargo"
        '    Return False
        'End If

        Return True
    End Function

    Public Function ValidarEntidadesACargo(ByRef mensaje As String, ByVal pColEntidadesACargoDN As System.Collections.Generic.IList(Of Framework.DatosNegocio.IEntidadDN)) As Boolean Implements IResponsableDN.ValidarEntidadesACargo
        If pColEntidadesACargoDN Is Nothing Then
            mensaje = "Todo responsable debe tener una colección de entidades a cargo"
            Return False
        End If

        'Si FijadoDepartamentoSubordinados está a true, hay que comprobar que los empleados de la colección de
        'subordinados pertenecen al mismo departamento que el responsable
        If Me.FijadoDepartamentoSubordinados Then
            'Dim ColEmpleados As ColEmpleadosDN
            'ColEmpleados = pColEntidadesACargoDN
            'Dim Empleado As EmpleadoDN
            'For Each Empleado In ColEmpleados
            '    If Empleado.colRolDepartamentoDN Is Nothing OrElse Not Empleado.colRolDepartamentoDN.DepartamentoEnCol(Me.mDepartamentoACargoDN) Then
            '        mensaje = "Las empleados deben pertenecer al mismo departemento del responsable de personal"
            '        Return False
            '    End If
            'Next

            Dim elto As Object
            Dim Empleado As EmpleadoDN
            For Each elto In pColEntidadesACargoDN
                Empleado = elto
                'If Empleado.colRolDepartamentoDN Is Nothing OrElse Not Empleado.colRolDepartamentoDN.DepartamentoEnCol(Me.mDepartamentoACargoDN) Then
                '    mensaje = "Las empleados deben pertenecer al mismo departemento del responsable de personal"
                '    Return False
                'End If
            Next
            
        End If

        Return True
    End Function


    ''' <summary>
    ''' Si el atributo mFijadoDepartamentoResponsable es true, validar que el departamento no sea nulo, y
    ''' comprobar que el departamento del responsable coincide con el departamento suyo como empleado
    ''' </summary>
    ''' <param name="mensaje"></param>
    ''' <param name="pDepartamentoACargo"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function ValidarDepartamentoACargo(ByRef mensaje As String, ByVal pDepartamentoACargo As DepartamentoDN) As Boolean

        If mFijadoDepartamentoResponsable Then
            If pDepartamentoACargo Is Nothing Then
                mensaje = "El responsable de personal debe contener el departamento del cual es responsable"
                Return False
            End If
            'If Me.mEntidadResponsable.colRolDepartamentoDN Is Nothing OrElse Not Me.mEntidadResponsable.colRolDepartamentoDN.DepartamentoEnCol(pDepartamentoACargo) Then
            '    mensaje = "El departamento debe ser el mismo que el departamento del responsable como empleado"
            '    Return False
            'End If
        End If

        Return True
    End Function


    Private Function ValidarEmpleadoEnDepartamentoResp(ByRef mensaje As String, ByVal pEmpleado As EmpleadoDN) As Boolean
        'If Me.FijadoDepartamentoSubordinados AndAlso (pEmpleado.colRolDepartamentoDN Is Nothing OrElse Not pEmpleado.colRolDepartamentoDN.DepartamentoEnCol(Me.mDepartamentoACargoDN)) Then
        '    mensaje = "El empleado debe estar en el departamento a cargo de su responsable"
        '    Return False
        'Else
        '    Return True
        'End If
    End Function

#End Region

#Region "Eventos"

    Public Overrides Sub ElementoaAñadir(ByVal pSender As Object, ByVal pElemento As Object, ByRef pPermitido As Boolean)
        If pSender Is Me.mColEmpleadosACargoDN Then
            Dim mensaje As String = ""
            If Not ValidarEmpleadoEnDepartamentoResp(mensaje, pElemento) Then
                Throw New Exception(mensaje)
            End If
        End If
        MyBase.ElementoaAñadir(pSender, pElemento, pPermitido)
    End Sub
#End Region

End Class
