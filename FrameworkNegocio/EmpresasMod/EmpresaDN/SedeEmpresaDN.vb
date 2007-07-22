#Region "Importaciones"
Imports framework.DatosNegocio
Imports fn.Localizaciones.DN
#End Region

''' <summary>
''' Representa la ubicación física de la empresa, y por lo tanto, tiene que tener un nombre
''' al menos un departamento, y un contacto
''' TODO: Cuando se añade una nueva sede, habrá que actualizar mediante la LN de Empresas, al objeto
''' DatosCompletosConcesionarioInformeAnual, la colección de sedes de una empresa, y los adaptadores evaluables.
''' </summary>
''' <remarks>Hay que tener cuidado con el método de validación del contacto, ya que hay que
''' decirle el tipo de contacto que tiene que buscar, y esto hay que hacerlo cada vez que 
''' surja una nueva dirección</remarks>
<Serializable()> Public Class SedeEmpresaDN
    Inherits EntidadDN

#Region "Atributos"
    'protected mSubDepDN As ColDepartamentosDN
    Protected mEmpresa As EmpresaDN

    Protected mTipoSede As TipoSedeDN
    Protected mSedePrincipal As Boolean = False
    Protected mCodigo As String

    Protected mDireccion As DireccionNoUnicaDN

#End Region

#Region "Constructores"

    'Public Sub New()
    '    MyBase.New()
    'End Sub

    'Public Sub New(ByVal pEmpresaDN As EmpresaDN, ByVal pNombre As String, ByVal pContactoDN As ContactoDN, ByVal pSedePrincipal As Boolean, ByVal pCodigo As String)
    '    Me.New(pEmpresaDN, pNombre, pContactoDN, pSedePrincipal, Nothing, pCodigo)
    'End Sub

    'Public Sub New(ByVal pEmpresaDN As EmpresaDN, ByVal pNombre As String, ByVal pContactoDN As ContactoDN, ByVal pSedePrincipal As Boolean, ByVal pTipoSedeDN As TipoSedeDN, ByVal pCodigo As String)
    '    Dim mensaje As String = ""


    '    Me.CambiarValorVal(Of String)(pCodigo, Me.mCodigo)


    '    'valido que la sede tenga un contacto
    '    'TODO: Revisar
    '    'If Me.ValidarContacto(mensaje, pContactoDN) Then
    '    '    Me.CambiarValorRef(Of ContactoDN)(pContactoDN, mContactoDN)
    '    'Else
    '    '    Throw New Exception(mensaje)
    '    'End If

    '    'valido que la sede tenga nombre
    '    If Me.ValidarNombre(mensaje, pNombre) Then
    '        Me.CambiarValorVal(Of String)(pNombre, mNombre)
    '    Else
    '        Throw New Exception(mensaje)
    '    End If

    '    'valido la empresa

    '    If ValidarEmpresa(mensaje, pEmpresaDN) Then
    '        Me.CambiarValorRef(Of EmpresaDN)(pEmpresaDN, mEmpresaDN)
    '    Else
    '        Throw New Exception(mensaje)
    '    End If

    '    Me.CambiarValorVal(Of Boolean)(pSedePrincipal, mSedePrincipal)
    '    Me.CambiarValorRef(Of TipoSedeDN)(pTipoSedeDN, mTipoSedeDN)

    '    Me.modificarEstado = EstadoDatosDN.SinModificar
    'End Sub
#End Region

#Region "Propiedades"

    Public Property Codigo() As String
        Get
            Return Me.mCodigo
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mCodigo)
        End Set
    End Property

    Public Property Empresa() As EmpresaDN
        Get
            Return mEmpresa
        End Get
        Set(ByVal value As EmpresaDN)
            Dim mensaje As String = ""
            If String.IsNullOrEmpty(mID) Then
                Me.CambiarValorRef(Of EmpresaDN)(value, mEmpresa)
            Else
                Throw New ApplicationExceptionDN("No se puede modificar la empresa de la sede")
            End If
        End Set
    End Property

    Public Overrides Property Nombre() As String
        Get
            Return mNombre
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mNombre)
        End Set
    End Property

    Public Property SedePrincipal() As Boolean
        Get
            Return mSedePrincipal
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mSedePrincipal)
        End Set
    End Property


    Public Property TipoSede() As TipoSedeDN
        Get
            Return mTipoSede
        End Get
        Set(ByVal value As TipoSedeDN)
            Me.CambiarValorRef(Of TipoSedeDN)(value, mTipoSede)
        End Set
    End Property

    Public ReadOnly Property EstadoModificacion() As Framework.DatosNegocio.EstadoDatosDN
        Get
            Return MyBase.Estado
        End Get
    End Property

    Public Property Direccion() As DireccionNoUnicaDN
        Get
            Return mDireccion
        End Get
        Set(ByVal value As DireccionNoUnicaDN)
            CambiarValorRef(Of DireccionNoUnicaDN)(value, mDireccion)
        End Set
    End Property

#End Region

#Region "Validaciones"

    ''' <summary>
    ''' Valida que la sede de la empresa tenga una empresa, y que esta tenga nombre
    ''' </summary>
    ''' <param name="mensaje"></param>
    ''' <param name="pEmpresaDN"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' 
    Private Function ValidarEmpresa(ByRef mensaje As String, ByVal pEmpresaDN As EmpresaDN) As Boolean
        If pEmpresaDN Is Nothing Then
            mensaje = "Una sede debe pertenecer a una empresa"
            Return False
        End If
        Return True
    End Function

    'Private Function ValidarColDepartamentos(ByRef mensaje As String, ByVal pSubDepDN As ColDepartamentosDN) As Boolean
    '    If pSubDepDN Is Nothing OrElse pSubDepDN.Count < 1 Then
    '        mensaje = "Una sede debe tener al menos un departamento"
    '        Return False
    '    ElseIf pSubDepDN.Count < 1 Then
    '        mensaje = "Una sede debe tener al menos un departamento"
    '        Return False
    '    End If
    '    Return True
    'End Function

    ''' <summary>
    ''' Valida que el nombre exista y no este vacio
    ''' </summary>
    ''' <param name="mensaje"></param>
    ''' <param name="pNombre"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' 
    Private Function ValidarNombre(ByRef mensaje As String, ByVal pNombre As String) As Boolean
        If pNombre Is Nothing OrElse pNombre = String.Empty Then
            mensaje = "La sede de la empresa debe tener un nombre"
            Return False
        End If
        Return True
    End Function

    ''' <summary>
    ''' Valida que una empresa siempre tenga un contacto
    ''' </summary>
    ''' <param name="mensaje"></param>
    ''' <param name="pContactoDN"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' 

    Private Function ValidarDireccion(ByRef mensaje As String, ByVal direccion As DireccionNoUnicaDN) As Boolean
        If direccion Is Nothing Then
            mensaje = "La dirección de la sede no puede ser nula"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarNombre(pMensaje, mNombre) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarEmpresa(pMensaje, mEmpresa) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarDireccion(pMensaje, mDireccion) Then
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
<Serializable()> Public Class ColSedeEmpresaDN
    Inherits ArrayListValidable(Of SedeEmpresaDN)

  
    ' metodos de coleccion
    '
    Public Function RecuperarxCodigo(ByVal cod As String) As SedeEmpresaDN
        Dim sede As SedeEmpresaDN
        For Each sede In Me
            If sede.Codigo = cod Then
                Return sede
            End If
        Next

    End Function

End Class