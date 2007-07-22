#Region "Importaciones"
Imports Framework.DatosNegocio
Imports Framework.Procesos.ProcesosDN
#End Region

<Serializable()> _
Public Class CasosUsoDN
    Inherits EntidadDN
    Implements IEjecutorOperacionDN


#Region "Atributos"
    Protected mColMetodosSistemaDN As ColMetodosSistemaDN
    Protected mColOperaciones As ColOperacionDN
#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()

        Me.CambiarValorRef(Of ColMetodosSistemaDN)(New ColMetodosSistemaDN, mColMetodosSistemaDN)
        Me.CambiarValorRef(Of ColOperacionDN)(New ColOperacionDN, mColOperaciones)


    End Sub

    Public Sub New(ByVal pNombre As String, ByVal pColMetodosSistemaDN As ColMetodosSistemaDN)
        Dim mensaje As String = ""

        If ValidarColMetodosSistemaDN(mensaje, pColMetodosSistemaDN) Then
            Me.CambiarValorRef(Of ColMetodosSistemaDN)(pColMetodosSistemaDN, mColMetodosSistemaDN)
        Else
            Throw New Exception(mensaje)
        End If

        Me.CambiarValorVal(Of String)(pNombre, mNombre)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"













    <RelacionPropCampoAtribute("mColMetodosSistemaDN")> _
    Public Property ColMetodosSistemaDN() As ColMetodosSistemaDN
        Get
            Return mColMetodosSistemaDN
        End Get
        Set(ByVal value As ColMetodosSistemaDN)
            Dim mensaje As String = ""
            If ValidarColMetodosSistemaDN(mensaje, value) Then
                Me.CambiarValorRef(Of ColMetodosSistemaDN)(value, mColMetodosSistemaDN)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property
    <RelacionPropCampoAtribute("mColOperaciones")> _
    Public Property ColOperaciones() As ColOperacionDN
        Get
            Return mColOperaciones
        End Get
        Set(ByVal value As ColOperacionDN)
            CambiarValorCol(Of ColOperacionDN)(value, mColOperaciones)
        End Set
    End Property

#End Region

#Region "Validaciones"
    Private Function ValidarColMetodosSistemaDN(ByRef mensaje As String, ByVal pColMetodosSistemaDN As ColMetodosSistemaDN) As Boolean
        If pColMetodosSistemaDN Is Nothing Then
            mensaje = "El caso de uso debe tener una colección de métodos de sistema"
            Return False
        End If
        Return True
    End Function
#End Region

#Region "Métodos"

    Public Function MetodoSistemaAutorizado(ByVal pMs As System.Reflection.MethodInfo) As Boolean

        Return Me.mColMetodosSistemaDN.MetodoSistemaAutorizado(pMs)
    End Function

    Public Overrides Function ToString() As String
        Return mNombre
    End Function
#End Region

    Public Function ToXml() As String

        Dim stb As New System.Text.StringBuilder
        stb.Append("<Cu Nombre='" & Me.Nombre & "'> " & ControlChars.NewLine)
        stb.Append(Me.mColMetodosSistemaDN.ToXml & ControlChars.NewLine)
        stb.Append("</Cu>" & ControlChars.NewLine)
        Return stb.ToString

    End Function

    Public ReadOnly Property ColOperaciones1() As Procesos.ProcesosDN.ColOperacionDN Implements Procesos.ProcesosDN.IEjecutorOperacionDN.ColOperaciones
        Get
            Return mColOperaciones
        End Get
    End Property













End Class



<Serializable()> _
Public Class ColCasosUsoDN
    Inherits ArrayListValidable(Of CasosUsoDN)

#Region "Métodos colección"

    Public Function RecuperarColMetodosSistema() As ColMetodosSistemaDN

        Dim cu As CasosUsoDN
        Dim col As ColMetodosSistemaDN
        col = New ColMetodosSistemaDN

        For Each cu In Me
            col.AddRange(cu.ColMetodosSistemaDN)
        Next
        Return col

    End Function

    Public Function MetodoSistemaAutorizado(ByVal pMs As System.Reflection.MethodInfo) As Boolean

        Dim cu As CasosUsoDN

        For Each cu In Me
            If cu.MetodoSistemaAutorizado(pMs) Then
                Return (True)
            End If
        Next

        Return (False)

    End Function

    Public Function ToXml() As String

        Dim cu As CasosUsoDN
        Dim stb As New System.Text.StringBuilder
        stb.Append("<ColCu>" & ControlChars.NewLine)
        For Each cu In Me
            stb.Append(cu.ToXml & ControlChars.NewLine)
        Next
        stb.Append("</ColCu>" & ControlChars.NewLine)
        Return stb.ToString

    End Function

    Public Function RecuperarColOperaciones() As ColOperacionDN
        RecuperarColOperaciones = New ColOperacionDN

        For Each casoUso As CasosUsoDN In Me
            RecuperarColOperaciones.AddRange(casoUso.ColOperaciones)
        Next

    End Function

#End Region

End Class