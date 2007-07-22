
#Region "Importaciones"
Imports Framework.DatosNegocio
Imports Framework.Procesos.ProcesosDN
#End Region

<Serializable()> _
Public Class PrincipalDN
    Inherits EntidadDN
    Implements System.Security.Principal.IPrincipal
    Implements IEjecutorOperacionRolDN

#Region "Campos"
    Protected mUsuarioDN As UsuarioDN
    Protected mColRol As ColRolDN
    '    Protected mcolMInombre As Generic.List(Of String)

    'Este campo no se almacena en la base de datos, únicamente se emplea para cachear la clave antes de ser guardada
    Protected mClavePropuesta As String

#End Region

#Region "Constructores"
    Public Sub New()
        MyBase.New()
        Me.CambiarValorRef(Of ColRolDN)(New ColRolDN, mColRol)

    End Sub

    Public Sub New(ByVal pNombre As String, ByVal pUsuarioDN As UsuarioDN, ByVal pColRolDN As ColRolDN)
        Dim mensaje As String = ""

        If ValUsuarioDN(mensaje, pUsuarioDN) Then
            Me.CambiarValorRef(Of UsuarioDN)(pUsuarioDN, mUsuarioDN)
        Else
            Throw New Exception(mensaje)
        End If

        If ValColRolDN(mensaje, pColRolDN) Then
            Me.CambiarValorRef(Of ColRolDN)(pColRolDN, mColRol)
        Else
            Throw New Exception(mensaje)
        End If

        Me.CambiarValorVal(Of String)(pNombre, mNombre)

        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub
#End Region

#Region "Propiedades"

    Public ReadOnly Property Identity() As System.Security.Principal.IIdentity Implements System.Security.Principal.IPrincipal.Identity
        Get
            Return mUsuarioDN
        End Get
    End Property
    <RelacionPropCampoAtribute("mUsuarioDN")> _
    Public Property UsuarioDN() As UsuarioDN
        Get
            Return mUsuarioDN
        End Get
        Set(ByVal value As UsuarioDN)
            Dim mensaje As String = ""
            If ValUsuarioDN(mensaje, value) Then
                Me.CambiarValorRef(Of UsuarioDN)(value, mUsuarioDN)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property

    Public ReadOnly Property EstadoModificacion() As EstadoDatosDN
        Get
            Return MyBase.Estado
        End Get
    End Property

    Protected Overrides Property BajaPersistente() As Boolean
        Get
            Return MyBase.BajaPersistente
        End Get
        Set(ByVal value As Boolean)
            MyBase.BajaPersistente = value

            Dim idp As Framework.DatosNegocio.IDatoPersistenteDN

            'Cuando damos de baja a un principal, se da de baja al usuario. El objeto datos de identidad con
            'el nick asociado se dará de baja en la ln de usuarios
            idp = Me.mUsuarioDN
            idp.Baja = value
        End Set
    End Property

    Public Property ClavePropuesta() As String
        Get
            Return mClavePropuesta
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mClavePropuesta)
        End Set
    End Property
    <RelacionPropCampoAtribute("mColRol")> _
    Public Property ColRolesPrincipal() As ColRolDN
        Get
            Return mColRol
        End Get
        Set(ByVal value As ColRolDN)
            Dim mensaje As String = ""
            If ValColRolDN(mensaje, value) Then
                Me.CambiarValorRef(Of ColRolDN)(value, mColRol)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property

#End Region

#Region "Propiedades IEjecutorOperacionRolDN"


    Public ReadOnly Property ColOperaciones() As ColOperacionDN Implements IEjecutorOperacionDN.ColOperaciones
        Get
            Dim colOps As New ColOperacionDN()
            If mColRol IsNot Nothing Then
                colOps.AddRange(mColRol.RecuperarColOperaciones())
            End If

            'If mUsuarioDN IsNot Nothing Then
            '    colOps.AddRange(mUsuarioDN.RecuperarColOperacionesEntidad())
            'End If

            Return colOps
        End Get
    End Property

    <RelacionPropCampoAtribute("mColRol")> _
    Public Property ColRoles() As ColRolDN Implements IEjecutorOperacionRolDN.ColRoles

        Get
            'Dim colR As New ColRolDN()

            'If mColRol IsNot Nothing Then
            '    colR.AddRange(mColRol)
            'End If

            'If mUsuarioDN IsNot Nothing AndAlso mUsuarioDN.EntidadUser IsNot Nothing Then
            '    colR.AddRange(mUsuarioDN.EntidadUser.ColRoles)
            'End If

            Return mColRol
        End Get


        Set(ByVal value As ColRolDN)
            Me.CambiarValorRef(Of ColRolDN)(value, mColRol)
        End Set
    End Property

    Public Property ColPermisos() As ColPermisoDN Implements IEjecutorOperacionRolDN.ColPermisos

        Get
            Dim colP As New ColPermisoDN()
            Dim colPRol As New ColPermisoDN()

            If mColRol IsNot Nothing Then
                colPRol.AddRange(mColRol.RecuperarColPermisos())
            End If

            Return colP.RecuperarColPermisosUnion(colPRol)

        End Get


        Set(ByVal value As ColPermisoDN)
            Throw New NotImplementedException
        End Set
    End Property
#End Region

#Region "Validaciones"
    Private Function ValUsuarioDN(ByRef mensaje As String, ByVal pUsuario As UsuarioDN) As Boolean
        If pUsuario Is Nothing Then
            mensaje = "El objeto Principal debe contener un usuario"
            Return False
        Else
            Return True
        End If
    End Function

    Private Function ValColRolDN(ByRef mensaje As String, ByVal pColRolDN As ColRolDN) As Boolean
        If pColRolDN Is Nothing Then
            mensaje = "El objeto Principal debe contener una colección de roles"
            Return False
        Else
            Return True
        End If
    End Function
#End Region

#Region "Métodos IPrincipal"
    Public Function IsInRole(ByVal role As String) As Boolean Implements System.Security.Principal.IPrincipal.IsInRole
        Dim e As RolDN

        For Each e In Me.ColRoles
            If e.Nombre.Equals(role) Then
                Return True
            End If
        Next
        Return False
    End Function
#End Region

#Region "Metodos"

    Public Function Autorizado(ByVal operacion As OperacionDN) As Boolean
        'TODO: Hay que quitar esta linea
        Return True

        Dim colOpsPrincipal As ColOperacionDN
        colOpsPrincipal = ColOperaciones


    End Function

    'Public Function Autorizado(ByVal tipoAutorizacion As TipoAutorizacionClase, ByVal tipo As System.Type) As Boolean

    '    'TODO: Hay que quitar esta linea
    '    Return True

    '    If mColRol.Autorizado(tipoAutorizacion, tipo) Then
    '        Return True
    '    Else
    '        Throw New ApplicationExceptionDN("No autorizado por DN")
    '    End If
    'End Function

    ''' <summary>
    ''' Indica si el usuario está o no autorizado. Si no lo está lanza una excepción.
    ''' </summary>
    Public Function Autorizado() As Boolean
        'TODO: luis - hay que quitar esta línea para que funcione la autorización.
        Return True
        Return Autorizado(False)
    End Function

    ''' <summary>
    ''' Indica si el usuario está o no autorizado
    ''' </summary>
    ''' <param name="pNoExcepcion">Si es true y no está autorizado devuelve false. Si es false y no está autorizado lanza una excepción</param>
    Public Function Autorizado(ByVal pNoExcepcion As Boolean) As Boolean
        Return True
        Dim metodo As Reflection.MethodInfo
        Dim st As StackTrace
        Dim sf As StackFrame

        'Encontrar el metodo llamante de fachada
        st = New StackTrace()
        sf = st.GetFrame(2)
        metodo = sf.GetMethod()

        'Verificar que estoy autorizado para llama a ese metodo de fachada
        If (Me.MetodoSistemaAutorizado(metodo) = True) Then
            Return True

        Else
            If (pNoExcepcion = True) Then
                Return False

            Else
                Throw New Exception("No autorizado")
            End If
        End If
    End Function

    Public Function MetodoSistemaAutorizado(ByVal pMetodoSistema As System.Reflection.MethodInfo) As Boolean
        Return Me.mColRol.MetodoSistemaAutorizado(pMetodoSistema)
    End Function

    Public Overrides Function ToString() As String
        Dim miCadena As String = ""

        If mUsuarioDN IsNot Nothing Then
            miCadena = Me.mUsuarioDN.Name
            'If mUsuarioDN.EntidadUser IsNot Nothing Then
            '    miCadena = miCadena & " - " & mUsuarioDN.EntidadUser.ToString()
            'End If
        End If

        If Not Me.mColRol Is Nothing AndAlso Me.mColRol.Count > 0 Then
            Dim miRol As RolDN

            miCadena = miCadena & " ("
            For Each miRol In Me.mColRol
                miCadena = miCadena & miRol.Nombre & "; "
            Next
            miCadena = miCadena.Remove(miCadena.Length - 2, 2)

            miCadena = miCadena & ")"

        End If

        Return miCadena

    End Function

    'Public Function Actualizarcolmi()
    '    Dim colms As ColMetodosSistemaDN
    '    colms = mColRolDN.ReuperarColMetodoSistema()
    '    mcolMInombre = New Generic.List(Of String)


    '    Dim ms As MetodoSistemaDN


    '    For Each ms In colms
    '        If Not mcolMInombre.Contains(ms.NombreEnsambladoClaseMetodo) Then
    '            mcolMInombre.Add(ms.NombreEnsambladoClaseMetodo)
    '        End If
    '    Next

    'End Function
#End Region



End Class
