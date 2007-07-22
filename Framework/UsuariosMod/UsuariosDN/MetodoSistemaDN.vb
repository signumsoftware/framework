#Region "Importaciones"

Imports Framework.DatosNegocio
Imports Framework.TiposYReflexion.DN
#End Region

<Serializable()> _
    Public Class MetodoSistemaDN
    Inherits EntidadDN

#Region "Atributos"
    Protected mVinculoMetodo As Framework.TiposYReflexion.DN.VinculoMetodoDN ' persistencia contenida 
    'Protected mMetodo As String
    'Protected mClase As String
#End Region

#Region "Constructores"
    Public Sub New()
    End Sub
    Public Sub New(ByVal pMetodo As System.Reflection.MethodInfo)

        'If pnombre Is Nothing OrElse pnombre = "" Then
        '    Me.CambiarValorVal(Of String)(pMetodo.Name, Me.mNombre)

        'Else
        '    Me.CambiarValorVal(Of String)(pnombre, Me.mNombre)
        'End If
        Me.CambiarValorRef(Of Framework.TiposYReflexion.DN.VinculoMetodoDN)(New Framework.TiposYReflexion.DN.VinculoMetodoDN(pMetodo), mVinculoMetodo)
        Me.CambiarValorVal(Of String)(mVinculoMetodo.NombreEnsambladoClaseMetodo, Me.mNombre)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

    Public Sub New(ByVal pNombreEnsamblado As String, ByVal pNombreClase As String, ByVal pNombreMetodo As String)
        MyBase.New("", "", Nothing, False)

        Dim mensaje As String = String.Empty

        If (ValMetodo(pNombreMetodo, mensaje) = False) Then
            Throw New ApplicationException(mensaje)
        End If

        If (ValClase(pNombreClase, mensaje) = False) Then
            Throw New ApplicationException(mensaje)
        End If
        If (ValClase(pNombreEnsamblado, mensaje) = False) Then
            Throw New ApplicationException(mensaje)
        End If


        Me.CambiarValorRef(Of Framework.TiposYReflexion.DN.VinculoMetodoDN)(New Framework.TiposYReflexion.DN.VinculoMetodoDN(pNombreEnsamblado, pNombreClase, pNombreMetodo), mVinculoMetodo)
        Me.CambiarValorVal(Of String)(mVinculoMetodo.NombreEnsambladoClaseMetodo, Me.mNombre)

        Me.modificarEstado = EstadoDatosDN.SinModificar

    End Sub

    Public Sub New(ByVal pVinculoMetodo As VinculoMetodoDN)
        Me.CambiarValorRef(Of Framework.TiposYReflexion.DN.VinculoMetodoDN)(pVinculoMetodo, mVinculoMetodo)
        Me.CambiarValorVal(Of String)(mVinculoMetodo.NombreEnsambladoClaseMetodo, Me.mNombre)
        Me.modificarEstado = EstadoDatosDN.SinModificar

    End Sub

#End Region

#Region "Propiedades"
    'Public Property Metodo() As String
    '    Get
    '        Return mMetodo

    '    End Get
    '    Set(ByVal value As String)
    '        Dim mensaje As String = String.Empty

    '        If (ValMetodo(value, mensaje) = False) Then
    '            Throw New ApplicationException(mensaje)
    '        End If

    '        mMetodo = value
    '    End Set
    'End Property

    'Public Property Clase() As String
    '    Get
    '        Return mClase
    '    End Get
    '    Set(ByVal value As String)
    '        Dim mensaje As String = String.Empty

    '        If (ValClase(value, mensaje) = False) Then
    '            Throw New ApplicationException(mensaje)
    '        End If

    '        mClase = value
    '    End Set
    'End Property

    'Public ReadOnly Property ClaseMetodo() As String
    '    Get
    '        Return mClase & "." & mMetodo
    '    End Get
    'End Property

    Public Property VinculoMetodo() As VinculoMetodoDN
        Get
            Return mVinculoMetodo
        End Get
        Set(ByVal value As VinculoMetodoDN)
            Me.CambiarValorRef(Of Framework.TiposYReflexion.DN.VinculoMetodoDN)(value, mVinculoMetodo)
            Me.CambiarValorVal(Of String)(value.NombreEnsambladoClaseMetodo, Me.mNombre)
        End Set
    End Property

    Public ReadOnly Property NombreEnsambladoClaseMetodo() As String
        Get
            Return Me.mVinculoMetodo.NombreEnsambladoClaseMetodo
        End Get
    End Property
#End Region

#Region "Metodos Validacion"
    Public Shared Function ValMetodo(ByVal pMetodo As String, ByRef pMensaje As String) As Boolean
        If (pMetodo Is Nothing OrElse pMetodo.Equals(String.Empty)) Then
            pMensaje = "Error: el metodo no puede ser nulo o la cadena vacia"
            Return False
        End If

        Return True
    End Function

    Public Shared Function ValClase(ByVal pClase As String, ByRef pMensaje As String) As Boolean
        If (pClase Is Nothing OrElse pClase.Equals(String.Empty)) Then
            pMensaje = "Error: la clase no puede ser nula o la cadena vacia"
            Return False
        End If

        Return True
    End Function
#End Region

#Region "Metodos"
    Public Function MetodoSistemaAutorizado(ByVal pMs As System.Reflection.MethodInfo) As Boolean
        If Me.NombreEnsambladoClaseMetodo = VinculoMetodoDN.RecuperarNombreEnsambladoClaseMetodo(pMs) Then
            Return True
        Else
            Return False
        End If

    End Function
    Public Overrides Function ToString() As String
        Return Me.mVinculoMetodo.VinculoClase.NombreClase & " - " & Me.mVinculoMetodo.NombreMetodo
    End Function
    Public Function ToXml() As String
        Return "<Ms Nombre='" & Me.mVinculoMetodo.NombreEnsambladoClaseMetodo & "'/>"
    End Function
    Public Overrides Function Equals(ByVal objeto As Object) As Boolean
        Dim ms As MetodoSistemaDN
        If Not objeto.GetType Is Me.GetType Then
            Return False
        End If

        ms = objeto

        If ms.NombreEnsambladoClaseMetodo = Me.NombreEnsambladoClaseMetodo Then
            Return True
        End If

        Return False
    End Function
#End Region

End Class


<Serializable()> _
Public Class ColMetodosSistemaDN
    Inherits ArrayListValidable(Of MetodoSistemaDN)

    Public Function MetodoSistemaAutorizado(ByVal pMs As System.Reflection.MethodInfo) As Boolean
        Dim ms As MetodoSistemaDN
        For Each ms In Me
            If ms.MetodoSistemaAutorizado(pMs) Then
                Return True
            End If

        Next

        Return False
    End Function


    Public Function ToXml() As String

        Dim ms As MetodoSistemaDN
        Dim stb As New System.Text.StringBuilder
        stb.Append("<ColMs>" & ControlChars.NewLine)
        For Each ms In Me
            stb.Append(ms.ToXml & ControlChars.NewLine)
        Next
        stb.Append("</ColMs>" & ControlChars.NewLine)
        Return stb.ToString

    End Function

End Class