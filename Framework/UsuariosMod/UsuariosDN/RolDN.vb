#Region "Importaciones"
Imports Framework.DatosNegocio
Imports Framework.Procesos.ProcesosDN
#End Region

<Serializable()> _
Public Class RolDN
    Inherits EntidadDN
    Implements IEjecutorOperacionRolDN


#Region "Atributos"

    Protected mColCasosUso As ColCasosUsoDN
    'Protected mColAutorizacion As ColAutorizacionClaseDN
    Protected mColPermisos As ColPermisoDN
    Protected mColRol As ColRolDN
#End Region

#Region "Constructores"
    Public Sub New()
        MyBase.New()
        Me.CambiarValorRef(Of ColCasosUsoDN)(New ColCasosUsoDN, mColCasosUso)
        Me.CambiarValorRef(Of ColPermisoDN)(New ColPermisoDN, mColPermisos)
        Me.CambiarValorRef(Of ColRolDN)(New ColRolDN, mColRol)

    End Sub

    Public Sub New(ByVal pNombre As String, ByVal pColCasosUsoDN As ColCasosUsoDN)
        Dim mensaje As String = ""

        If ValColCasosUsoDN(mensaje, pColCasosUsoDN) Then
            Me.CambiarValorRef(Of ColCasosUsoDN)(pColCasosUsoDN, mColCasosUso)
        Else
            Throw New Exception(mensaje)
        End If

        Me.CambiarValorVal(Of String)(pNombre, mNombre)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

    'Public Sub New(ByVal pNombre As String, ByVal pColCasosUsoDN As ColCasosUsoDN, ByVal colAutorizacion As ColAutorizacionClaseDN)
    '    Dim mensaje As String = ""

    '    If ValColCasosUsoDN(mensaje, pColCasosUsoDN) Then
    '        Me.CambiarValorRef(Of ColCasosUsoDN)(pColCasosUsoDN, mColCasosUso)
    '    Else
    '        Throw New Exception(mensaje)
    '    End If

    '    Me.CambiarValorVal(Of String)(pNombre, mNombre)
    '    CambiarValorCol(Of ColAutorizacionClaseDN)(colAutorizacion, mColAutorizacion)

    '    Me.modificarEstado = EstadoDatosDN.SinModificar
    'End Sub

#End Region

#Region "Propiedades"
    <RelacionPropCampoAtribute("mColCasosUso")> _
    Public Property ColCasosUsoDN() As ColCasosUsoDN
        Get
            Return mColCasosUso
        End Get
        Set(ByVal value As ColCasosUsoDN)
            Dim mensaje As String = ""
            If ValColCasosUsoDN(mensaje, value) Then
                Me.CambiarValorRef(Of ColCasosUsoDN)(value, mColCasosUso)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property

    'Public Property ColAutorizacion() As ColAutorizacionClaseDN
    '    Get
    '        Return mColAutorizacion
    '    End Get
    '    Set(ByVal value As ColAutorizacionClaseDN)
    '        CambiarValorCol(Of ColAutorizacionClaseDN)(value, mColAutorizacion)
    '    End Set
    'End Property

    Public Property ColPermisos() As ColPermisoDN Implements IEjecutorOperacionRolDN.ColPermisos
        Get
            Return mColPermisos.RecuperarColPermisosUnion(Me.mColRol.RecuperarColPermisosUnion)
        End Get
        Set(ByVal value As ColPermisoDN)
            Throw New NotImplementedException
        End Set
    End Property
    <RelacionPropCampoAtribute("mColPermisos")> _
    Public Property ColMisPermisos() As ColPermisoDN
        Get
            Return mColPermisos
        End Get
        Set(ByVal value As ColPermisoDN)
            CambiarValorCol(Of ColPermisoDN)(value, mColPermisos)
        End Set
    End Property
#End Region

#Region "Propiedades IEjecutorOperacionDN"
    <RelacionPropCampoAtribute("mColRol")> _
    Public Property ColRoles() As ColRolDN Implements IEjecutorOperacionRolDN.ColRoles
        Get
            Return mColRol
        End Get
        Set(ByVal value As ColRolDN)
            Me.CambiarValorRef(Of ColRolDN)(value, mColRol)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColCasosUso")> _
    Public ReadOnly Property ColOperaciones() As ColOperacionDN Implements IEjecutorOperacionDN.ColOperaciones
        Get
            If mColCasosUso IsNot Nothing Then
                Return mColCasosUso.RecuperarColOperaciones()
            Else
                Return Nothing
            End If
        End Get
        'Set(ByVal value As ColOperacionDN)
        '    Throw New NotImplementedException
        'End Set
    End Property



#End Region

#Region "Validaciones"

    Private Function ValColCasosUsoDN(ByRef mensaje As String, ByVal pColCasosUsoDN As ColCasosUsoDN) As Boolean
        If pColCasosUsoDN Is Nothing Then
            mensaje = "La colección de casos de uso de un rol no puede ser nula"
            Return False
        Else
            Return True
        End If
    End Function

#End Region

#Region "Metodos"

    Public Function RecuperarColMetodosSistema() As ColMetodosSistemaDN
        Return mColCasosUso.RecuperarColMetodosSistema
    End Function

    Public Function MetodoSistemaAutorizado(ByVal pMs As System.Reflection.MethodInfo) As Boolean
        Return Me.mColCasosUso.MetodoSistemaAutorizado(pMs)
    End Function

    Public Overrides Function ToString() As String
        Return Me.Nombre()
    End Function
    Public Function ToXml() As String

        Dim stb As New System.Text.StringBuilder
        stb.Append("<Rol Nombre='" & Me.Nombre & "'> " & ControlChars.NewLine)
        stb.Append(Me.mColCasosUso.ToXml & ControlChars.NewLine)
        stb.Append("</Rol>" & ControlChars.NewLine)
        Return stb.ToString

    End Function
#End Region


  


End Class

<Serializable()> _
Public Class ColRolDN
    Inherits ArrayListValidable(Of RolDN)

#Region "Metodos"

    Public Function ToXml() As String

        Dim rol As RolDN
        Dim stb As New System.Text.StringBuilder
        stb.Append("<ColRol>" & ControlChars.NewLine)
        For Each rol In Me
            stb.Append(rol.ToXml & ControlChars.NewLine)
        Next
        stb.Append("</ColRol>" & ControlChars.NewLine)
        Return stb.ToString

    End Function

    Public Function MetodoSistemaAutorizado(ByVal pMs As System.Reflection.MethodInfo) As Boolean
        Dim rol As RolDN

        For Each rol In Me
            If rol.MetodoSistemaAutorizado(pMs) Then
                Return True
            End If
        Next

        Return False
    End Function

    Public Function RecuperarColMetodoSistema() As ColMetodosSistemaDN
        Dim col As ColMetodosSistemaDN
        Dim rol As RolDN

        col = New ColMetodosSistemaDN

        For Each rol In Me
            col.AddRange(rol.ColCasosUsoDN.RecuperarColMetodosSistema)
        Next

        Return col
    End Function

    Public Function RecuperarXNombre(ByVal pNombreRol As String) As RolDN
        Dim rol As RolDN

        For Each rol In Me

            If rol.Nombre = pNombreRol Then
                Return rol
            End If

        Next


        Return Nothing
    End Function

    'Public Function Autorizado(ByVal tipoAutorizacion As TipoAutorizacionClase, ByVal tipo As System.Type) As Boolean
    '    For Each rol As RolDN In Me
    '        If rol.ColAutorizacion.Autorizado(tipoAutorizacion, tipo) Then
    '            Return True
    '        End If
    '    Next

    '    Return False

    'End Function

    Public Function RecuperarColOperaciones() As ColOperacionDN
        RecuperarColOperaciones = New ColOperacionDN

        For Each rol As RolDN In Me
            RecuperarColOperaciones.AddRange(rol.ColOperaciones)
        Next

    End Function

    ''' <summary>
    ''' Método que comprueba si la colección contiene todas las entidades de otra colección. Devuelve true
    ''' si la colección está totalmente contenida en mi
    ''' </summary>
    ''' <param name="colRoles">Colección que debe estar contenida en mi</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function ContieneColRoles(ByVal colRoles As ColRolDN) As Boolean
        If colRoles IsNot Nothing AndAlso colRoles.Count > 0 Then
            For Each rol As RolDN In colRoles
                If Not Me.Contiene(rol, CoincidenciaBusquedaEntidadDN.Todos) Then
                    Return False
                End If
            Next
        End If

        Return True
    End Function

    Public Function RecuperarColPermisos() As ColPermisoDN
        RecuperarColPermisos = New ColPermisoDN()

        For Each rol As RolDN In Me
            RecuperarColPermisos.AddRange(rol.ColPermisos)
        Next

    End Function
    Public Function RecuperarColPermisosUnion() As ColPermisoDN
        RecuperarColPermisosUnion = New ColPermisoDN()

        For Each rol As RolDN In Me
            RecuperarColPermisosUnion.RecuperarColPermisosUnion(rol.ColPermisos)
        Next

    End Function
#End Region

End Class


<Serializable()> _
Public Class HuellaRolDN
    Inherits Framework.DatosNegocio.HuellaEntidadTipadaDN(Of RolDN)
    Public Sub New(ByVal pRol As RolDN)
        MyBase.New(pRol, HuellaEntidadDNIntegridadRelacional.relacion, pRol.Nombre)
    End Sub
    Public Sub New()

    End Sub
End Class


<Serializable()> _
Public Class ColHuellaRolDN
    Inherits ArrayListValidable(Of HuellaRolDN)


End Class
