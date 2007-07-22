Imports Framework.Procesos.ProcesosDN

<Serializable()> _
Public Class EstructuraVistaDN
    Inherits Framework.DatosNegocio.EntidadDN


    Public mNombreVista As String
    Public mListaCampos As ColCalposDN
    Public Property NombreVista() As String
        Get
            Return Me.mNombreVista
        End Get
        Set(ByVal value As String)
            ' mNombreVista = value
            Me.CambiarValorVal(Of String)(value, mNombreVista)
        End Set
    End Property

    Public Property ListaCampos() As ColCalposDN
        Get
            Return Me.mListaCampos
        End Get
        Set(ByVal value As ColCalposDN)
            '   Me.mListaCampos = value
            Me.CambiarValorRef(Of ColCalposDN)(value, mListaCampos)

        End Set
    End Property
End Class

<Serializable()> _
Public Class CampoDN
    Inherits Framework.DatosNegocio.EntidadDN
    Public mNombreCampo As String
    Public mtipoCampo As tipocampo
    Public mValores As DataSet ' no procesar

    Public Property tipoCampo() As tipocampo
        Get
            Return mtipoCampo
        End Get
        Set(ByVal value As tipocampo)
            '  mtipoCampo = value
            Me.CambiarValorVal(Of Integer)(value, mtipoCampo)
        End Set
    End Property

    Public Property Valores() As DataSet
        Get
            Return Me.mValores
        End Get
        Set(ByVal value As DataSet)
            mValores = value
            '  Me.CambiarValorVal(Of Integer)(value, mtipoCampo)
        End Set
    End Property

    Property NombreCampo() As String
        Get
            Return mNombreCampo
        End Get
        Set(ByVal value As String)
            'mNombreCampo = value
            Me.CambiarValorVal(Of String)(value, mNombreCampo)
        End Set
    End Property

    Public ReadOnly Property TieneListaValores() As Boolean
        Get

            If Me.mValores Is Nothing Then
                Return False
            End If

            Return Me.mValores.Tables.Count > 0


        End Get
    End Property

End Class


<Serializable()> _
Public Class ColCalposDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of CampoDN)
    Public Function RecuperarxNombreCampo(ByVal pNombreCampo As String) As CampoDN
        Dim campo As CampoDN

        For Each campo In Me
            If campo.NombreCampo.ToLower = pNombreCampo.ToLower Then
                Return campo
            End If
        Next
    End Function



    Public Function RecuperarxNombreCampo(ByVal pNombreCampos As List(Of String)) As ColCalposDN
        Dim campo As CampoDN
        Dim NombreCampo As String
        Dim col As New ColCalposDN

        For Each NombreCampo In pNombreCampos
            For Each campo In Me
                If campo.NombreCampo = NombreCampo Then
                    col.Add(campo)
                End If
            Next
        Next
        Return col
    End Function

End Class

Public Enum tipocampo
    otros
    boleano
    texto
    numerico
    fecha
    Listado
End Enum
'<Serializable()> _
'Public Class ParametroCargaEstructuraDN
'    '  Inherits Framework.DatosNegocio.EntidadDN


'    Public EntidadReferidora As Framework.DatosNegocio.IEntidadBaseDN
'    Public PropiedadReferidora As Reflection.PropertyInfo

'    Public TipodeEntidad As System.Type
'    Public DestinoNavegacion As String
'    Public NombreInstanciaMapVis As String
'    Public NombreVistaSel As String
'    Public NombreVistaVis As String
'    Public ConsultaSQL As String

'    Public PropiedadDeInstancia As Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN




'    Private mListaValores As New List(Of ValorCampo)
'    Private mColOperacionDN As New ColOperacionDN


'    Public Titulo As String

'    Public Property ColOperacion() As ColOperacionDN
'        Get
'            Return Me.mColOperacionDN
'        End Get
'        Set(ByVal value As ColOperacionDN)
'            Me.mColOperacionDN = value
'        End Set
'    End Property

'    Public Property ListaValores() As List(Of ValorCampo)
'        Get
'            Return Me.mListaValores
'        End Get
'        Set(ByVal value As List(Of ValorCampo))
'            Me.mListaValores = value
'        End Set
'    End Property



'    Public CamposaCargarDatos As List(Of String)


'    ' nombrevistasel = nombreVistaVis
'    ' nombrevistasel/nombreVistaVis/campoos a cargar
'    Private Sub ProcesarlistaNombreVisYtexto(ByVal cadenaOrdenadaParametroCargaEstructuraDN As String)
'        Dim elementos As String() = cadenaOrdenadaParametroCargaEstructuraDN.Split("/")





'        Select Case elementos.Length

'            Case Is = 1
'                NombreVistaVis = elementos(0)
'                NombreVistaSel = NombreVistaVis

'            Case Is > 1
'                NombreVistaVis = elementos(0)
'                NombreVistaSel = elementos(1)




'                '   Throw New ApplicationException("numero de argumentos incorrectos en la cadena (NombreEnsamblado/nombreclaseCompleto/nombrevistasel = nombreVistaVis ) o (NombreEnsamblado/nombreclaseCompleto/nombrevistasel/nombreVistaVis/campoos a cargar)")



'        End Select

'        If elementos.Length = 1 Then

'        Else



'            Dim misCamposaCargarDatos() As String
'            ReDim misCamposaCargarDatos(elementos.Length - 3)

'            Array.Copy(elementos, 2, misCamposaCargarDatos, 0, misCamposaCargarDatos.Length)

'            CamposaCargarDatos = New List(Of String)
'            CamposaCargarDatos.AddRange(misCamposaCargarDatos)
'        End If





'    End Sub

'    Private Sub ProcesarlsitaValoresCondicion(ByVal cadenaOrdenadaParametroCargaEstructuraDN As String)
'        If String.IsNullOrEmpty(cadenaOrdenadaParametroCargaEstructuraDN) Then Exit Sub

'        Me.mListaValores = New List(Of ValorCampo)


'        Dim elementos As String() = cadenaOrdenadaParametroCargaEstructuraDN.Split("/")

'        Dim operadores As String() = {"=", "<>", "><", ">=", "<=", ">", "<"}
'        For Each cadenaCondicion As String In elementos

'            Dim subElemento As String() = cadenaCondicion.Split(operadores, StringSplitOptions.None)
'            Dim mivc As New ValorCampo
'            mivc.NombreCampo = subElemento(0)
'            mivc.Valor = subElemento(1)


'            If cadenaCondicion.Contains("=") Then
'                mivc.Operador = OperadoresAritmeticos.igual
'            ElseIf cadenaCondicion.Contains("<>") Then
'                mivc.Operador = OperadoresAritmeticos.distinto
'            ElseIf cadenaCondicion.Contains("><") Then
'                mivc.Operador = OperadoresAritmeticos.contener_texto
'            ElseIf cadenaCondicion.Contains(">=") Then
'                mivc.Operador = OperadoresAritmeticos.mayor_igual
'            ElseIf cadenaCondicion.Contains("<=") Then
'                mivc.Operador = OperadoresAritmeticos.menor_igual


'            ElseIf cadenaCondicion.Contains(">") Then
'                mivc.Operador = OperadoresAritmeticos.mayor
'            ElseIf cadenaCondicion.Contains("<") Then
'                mivc.Operador = OperadoresAritmeticos.menor
'            Else
'                Throw New ApplicationException("no se reconoce el operador")
'            End If


'            mListaValores.Add(mivc)


'        Next



'    End Sub


'    Public Sub CargarDesdeTipo(ByVal pTipo As System.Type)

'        Me.NombreVistaSel = "tl" & pTipo.Name
'        Me.NombreVistaVis = NombreVistaSel
'        ' ConsultaSQL = "select * from (select id, ToSt as DatosCampo From " & NombreVistaSel & ") as " & NombreVistaSel
'        ConsultaSQL = "select " & NombreVistaSel & ".id," & NombreVistaSel & ".ToSt as DatosCampo From " & NombreVistaSel


'    End Sub


'    Public Function CargarDesdeTexto(ByVal cadenaOrdenadaParametroCargaEstructuraDN As String) As Boolean

'        If String.IsNullOrEmpty(cadenaOrdenadaParametroCargaEstructuraDN) Then
'            Return False
'        End If


'        Dim Separador(0) As String
'        Separador(0) = "*Donde*"

'        Dim elementos As String() = cadenaOrdenadaParametroCargaEstructuraDN.Split(Separador, StringSplitOptions.None)
'        Select Case elementos.Length


'            Case Is = 1
'                ' no hay elementos de carga
'                ProcesarlistaNombreVisYtexto(elementos(0))
'                Return True

'            Case Is > 1 ' permite varias adiciones de la clausual *donde*

'                ProcesarlistaNombreVisYtexto(elementos(0))
'                For a As Int16 = 1 To elementos.Length - 1
'                    ProcesarlsitaValoresCondicion(elementos(a))
'                Next



'                Return True



'            Case Else
'                Return False
'        End Select


'    End Function

'    Public Function GenerarTexto(ByVal cadenaOrdenadaParametroCargaEstructuraDN As String, ByVal separador As String) As String

'        Dim stb As New Text.StringBuilder
'        stb.Append(DestinoNavegacion)
'        stb.Append("/")

'        stb.Append(NombreVistaSel)
'        stb.Append("/")
'        stb.Append(NombreVistaVis)
'        stb.Append("/")
'        stb.Append(DestinoNavegacion)
'        stb.Append("/")


'        For Each campo As String In CamposaCargarDatos
'            stb.Append(campo)
'            stb.Append("/")
'        Next

'        Dim cadena As String = stb.ToString


'        Return cadena.Substring(0, cadena.Length - 2)

'    End Function
'End Class