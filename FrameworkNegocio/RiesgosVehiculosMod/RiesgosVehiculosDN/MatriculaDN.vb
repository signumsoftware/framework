Imports Framework.DatosNegocio

<Serializable()> _
Public Class MatriculaDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mTipoMatricula As TipoMatricula
    Protected mTipoMatriculaCodigo As String
    Protected mValorMatricula As String
    Protected mValorMatriculaFIVA As String
    Protected mMatriculaTipoCorrecta As Boolean

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mTipoMatricula")> _
    Public Property TipoMatricula() As TipoMatricula
        Get
            Return mTipoMatricula
        End Get
        Set(ByVal value As TipoMatricula)
            If Not String.IsNullOrEmpty(Me.ID) Then
                Throw New ApplicationExceptionDN("Una vez creada, la matrícula no puede modificada")
            End If
            CambiarValorVal(Of TipoMatricula)(value, mTipoMatricula)
            mMatriculaTipoCorrecta = ComprobarMatriculaxTipo(mValorMatricula, mTipoMatricula)
            mTipoMatriculaCodigo = ObtenerCodTipoMatricula(mTipoMatricula)
        End Set
    End Property

    Public ReadOnly Property TipoMatriculaCodigo() As String
        Get
            Return mTipoMatriculaCodigo
        End Get
    End Property

    Public Property ValorMatricula() As String
        Get
            Return mValorMatricula
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mValorMatricula)
            mMatriculaTipoCorrecta = ComprobarMatriculaxTipo(mValorMatricula, mTipoMatricula)
        End Set
    End Property

    Public ReadOnly Property MatriculaTipoCorrecta() As Boolean
        Get
            Return mMatriculaTipoCorrecta
        End Get
    End Property

    Public ReadOnly Property ValorMatriculaFIVA() As String
        Get
            Return mValorMatriculaFIVA
        End Get
    End Property

#End Region

#Region "Métodos"


    Public Shared Function GenerarMatriculaAleatoriaDelTipo(ByVal ptipomatricula As TipoMatricula) As MatriculaDN


        Dim rndnumero As New Random
        Dim valormatricula As String = rndnumero.Next(1, 9999) & "ddd"
        Dim ausencias As Int16 = 7 - valormatricula.Length
        For a As Integer = 1 To ausencias
            valormatricula = "0" & valormatricula
        Next




        Dim ma As New MatriculaDN
        ma.TipoMatricula = DN.TipoMatricula.NormalTM
        ma.ValorMatricula = valormatricula

        Return ma

    End Function

    Public Function CalcularTipoMatricula(ByVal matricula As String) As TipoMatricula

        If String.IsNullOrEmpty(matricula) Then
            Return DN.TipoMatricula.LibreTMK
        End If

        matricula = matricula.ToUpper()

        ' matricula de ciclomotor tipo "C"
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.CiclomotoresTMC) Then
            Return DN.TipoMatricula.CiclomotoresTMC
        End If

        'Vehículos automóviles (O.M. 15-9-00) (matrícula actual y antigua) tipo " "
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.NormalTM) Then
            Return DN.TipoMatricula.NormalTM
        End If

        'TIPO "E". Vehículos Especiales.
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.VehiculosEspecialesTME) Then
            Return DN.TipoMatricula.VehiculosEspecialesTME
        End If

        'TIPO "E1". Vehículos Especiales.
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.VehiculosEspeciales2TME1) Then
            Return DN.TipoMatricula.VehiculosEspeciales2TME1
        End If

        'Tipo "T". Turística.
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.TuristicaTMT) Then
            Return DN.TipoMatricula.TuristicaTMT
        End If

        'Tipo "T1". Turística nueva.
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.Turistica2TMT1) Then
            Return DN.TipoMatricula.Turistica2TMT1
        End If

        'Tipo "D". Diplomática.
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.DiplomaticoTMD) Then
            Return DN.TipoMatricula.DiplomaticoTMD
        End If

        'Tipo "R". Remolques y semi-remolques.
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.RemolquesTMR) Then
            Return DN.TipoMatricula.RemolquesTMR
        End If

        'Tipo "R1". Remolques y semi-remolques nueva.
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.Remolques2TMR1) Then
            Return DN.TipoMatricula.Remolques2TMR1
        End If

        'Tipo "H". Vehículo histórico.
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.HistoricaTMH) Then
            Return DN.TipoMatricula.HistoricaTMH
        End If

        'Tipo "TE". Temporal.
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.TemporalTMTE) Then
            Return DN.TipoMatricula.TemporalTMTE
        End If

        'Tipo "TT". Temporal particulares y empresas
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.Temporal2TMTT) Then
            Return DN.TipoMatricula.Temporal2TMTT
        End If

        'Tipo "P". Pruebas y transporte
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.PruebasTransporteTMP) Then
            Return DN.TipoMatricula.PruebasTransporteTMP
        End If

        'Tipo "I". Inspección técnica de vehículos.
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.InspeccionTecnicaVehiculosTMI) Then
            Return DN.TipoMatricula.InspeccionTecnicaVehiculosTMI
        End If

        'Tipo "M". Vehículos del estado.
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.VehiculosEstadoTMM) Then
            Return DN.TipoMatricula.VehiculosEstadoTMM
        End If

        'Tipo "M1". Vehículos del estado.
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.VehiculosEstado2TMM1) Then
            Return DN.TipoMatricula.VehiculosEstado2TMM1
        End If

        'Tipo "K". Libre.
        If ComprobarMatriculaxTipo(matricula, DN.TipoMatricula.LibreTMK) Then
            Return DN.TipoMatricula.LibreTMK
        End If

        Return Nothing

    End Function

    Public Function ComprobarMatriculaxTipo(ByVal matricula As String, ByVal tipoMatricula As TipoMatricula) As Boolean
        Dim restoCodigo As String = String.Empty


        If String.IsNullOrEmpty(matricula) Then
            Return False
        End If


        matricula = matricula.ToUpper()
        If matricula.Length > 12 Then
            Return False
        End If
        If tipoMatricula = Nothing Then
            Return False
        End If


        If tipoMatricula = DN.TipoMatricula.CiclomotoresTMC Then
            If matricula.Trim.Length = 8 And matricula.Substring(0, 1) = "C" Then
                If ComprobacionCodigo4N3X(matricula.Substring(1, 7)) Then
                    Return True
                End If
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.NormalTM Then
            If matricula.Trim.Length = 7 AndAlso ComprobacionCodigo4N3X(matricula.Substring(0, 7)) Then
                Return True
            End If

            restoCodigo = ObtenerRestoCadCodProvincia(matricula)

            If restoCodigo Is Nothing OrElse restoCodigo.Length <> 6 Then
                Return False
            End If

            If System.Text.RegularExpressions.Regex.IsMatch(restoCodigo, "[0-9]{6}") OrElse _
                    System.Text.RegularExpressions.Regex.IsMatch(restoCodigo, "[0-9]{4}[A-Z]{2}") OrElse _
                    System.Text.RegularExpressions.Regex.IsMatch(restoCodigo, "[0-9]{5}[A-Z]{1}") Then
                Return True
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.VehiculosEspecialesTME Then

            restoCodigo = ObtenerRestoCadCodProvincia(matricula)

            If restoCodigo IsNot Nothing AndAlso restoCodigo.Length = 8 AndAlso System.Text.RegularExpressions.Regex.IsMatch(restoCodigo, "[0-9]{6}VE") Then
                Return True
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.VehiculosEspeciales2TME1 Then
            If matricula.Trim.Length = 8 Then
                If matricula.Substring(0, 1) = "E" Then
                    If ComprobacionCodigo4N3X(matricula.Substring(1, 7)) Then
                        Return True
                    End If
                End If
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.TuristicaTMT Then
            If System.Text.RegularExpressions.Regex.IsMatch(matricula.Substring(0, 2), "[0-9]{2}") Then

                restoCodigo = ObtenerRestoCadCodProvincia(matricula.Substring(2))

                If System.Text.RegularExpressions.Regex.IsMatch(restoCodigo, "[0-9]{4}") Then
                    If matricula.Length > 8 Then
                        If System.Text.RegularExpressions.Regex.IsMatch(restoCodigo.Substring(8, 4), "[0-1]{1}[0-9]{1}") Then
                            Return True
                        End If
                    Else
                        Return True
                    End If
                End If
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.Turistica2TMT1 Then
            If matricula.Trim.Length = 8 Then
                If matricula.Substring(0, 1) = "T" Then
                    If ComprobacionCodigo4N3X(matricula.Substring(1, 7)) Then
                        Return True
                    End If
                End If
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.DiplomaticoTMD Then
            If matricula.Trim.Length = 8 Then
                If matricula.Substring(0, 2) = "CD" Or matricula.Substring(0, 2) = "OI" Or matricula.Substring(0, 2) = "CC" _
                    Or matricula.Substring(0, 2) = "TA" Then

                    If System.Text.RegularExpressions.Regex.IsMatch(matricula.Substring(2, 6), "[0-9]{6}") Then
                        Return True
                    End If
                End If
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.RemolquesTMR Then
            If matricula.Trim.Length = 9 AndAlso matricula.Substring(8, 1) = "R" Then

                restoCodigo = ObtenerRestoCadCodProvincia(matricula.Substring(2))

                If restoCodigo IsNot Nothing AndAlso restoCodigo.Length = 6 AndAlso System.Text.RegularExpressions.Regex.IsMatch(restoCodigo, "[0-9]{6}") Then
                    Return True
                End If
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.Remolques2TMR1 Then
            If matricula.Trim.Length = 9 AndAlso matricula.Substring(0, 1) = "R" Then
                If ComprobacionCodigo4N3X(matricula.Substring(1, 7)) Then
                    Return True
                End If
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.HistoricaTMH Then
            If matricula.Trim.Length = 8 Then
                If matricula.Substring(0, 1) = "H" Then
                    If ComprobacionCodigo4N3X(matricula.Substring(1, 7)) Then
                        Return True
                    End If
                End If
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.TemporalTMTE Then

            restoCodigo = ObtenerRestoCadCodProvincia(matricula.Substring(2))

            If restoCodigo IsNot Nothing AndAlso restoCodigo.Length = 7 AndAlso System.Text.RegularExpressions.Regex.IsMatch(restoCodigo, "[0-9]{4}R|T[0-9]{2}") Then
                Return True
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.Temporal2TMTT Then
            If matricula.Trim.Length = 9 AndAlso (matricula.Substring(0, 1) = "P" OrElse matricula.Substring(0, 1) = "V" OrElse matricula.Substring(0, 1) = "S") Then
                If ComprobacionCodigo4N3X(matricula.Substring(1, 7)) Then
                    Return True
                End If
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.PruebasTransporteTMP Then

            restoCodigo = ObtenerRestoCadCodProvincia(matricula.Substring(2))

            If restoCodigo IsNot Nothing AndAlso System.Text.RegularExpressions.Regex.IsMatch(restoCodigo.Substring(0, 6), "[0-9]{2}T|P[0-9]{4}") Then
                If restoCodigo.Length > 6 Then
                    If System.Text.RegularExpressions.Regex.IsMatch(matricula.Substring(9, 3), "1|2[0-9]{2}") Then
                        Return True
                    End If
                Else
                    Return True
                End If
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.InspeccionTecnicaVehiculosTMI Then
            If matricula.Trim.Length = 9 AndAlso matricula.Substring(2, 3) = "ITV" Then

                restoCodigo = ObtenerRestoCadCodProvincia(matricula.Substring(2))

                If restoCodigo IsNot Nothing AndAlso restoCodigo.Length = 4 AndAlso System.Text.RegularExpressions.Regex.IsMatch(restoCodigo, "[0-9]{4}") Then
                    Return True
                End If
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.VehiculosEstadoTMM Then
            If matricula.Trim.Length = 11 Then
                If ComprobacionCodigoVehiculosEstado(matricula.Substring(0, 3)) Then
                    If System.Text.RegularExpressions.Regex.IsMatch(matricula.Substring(2, 4), "[0-9]{6}") Then
                        If System.Text.RegularExpressions.Regex.IsMatch(matricula.Substring(2, 6), "00[0-9]{4}") Then
                            If System.Text.RegularExpressions.Regex.IsMatch(matricula.Substring(8, 2), "[A-Z]{2}") Then
                                Return True
                            End If
                        ElseIf System.Text.RegularExpressions.Regex.IsMatch(matricula.Substring(2, 6), "[0-9]{6}") Then
                            Return True
                        End If
                    End If
                End If
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.VehiculosEstado2TMM1 Then
            If matricula.Trim.Length = 11 Then
                If ComprobacionCodigoVehiculosEstado(matricula.Substring(0, 3)) Then
                    If matricula.Substring(3, 2) = "  " OrElse matricula.Substring(3, 2) = "VE" Then
                        If System.Text.RegularExpressions.Regex.IsMatch(matricula.Substring(2, 4), "[0-9]{6}") Then
                            Return True
                        End If
                    End If
                End If
            End If

            Return False
        End If

        If tipoMatricula = DN.TipoMatricula.LibreTMK Then
            Return True
        End If

        Return False

    End Function

    Private Function ComprobacionCodigoProvincia2X(ByVal texto As String) As Boolean
        texto = texto.ToUpper()

        If System.Text.RegularExpressions.Regex.IsMatch(texto, "A " & _
            "|AB|AL|AV|B |BA|BI|BU|C |CA|CC|CE|CO|CR|CS|CU|GC|GE|GI|GR|GU|H |HU|J |L " & _
            "|LE|LO|LU|M |MA|ML|MU|NA|O |OR|OU|P |PM|IB|PO|S |SA|SE|SG|SO|SS|T " & _
            "|TE|TF|TO|V |VA|VI|Z |ZA") Then
            Return True
        Else
            Return False
        End If
    End Function

    Private Function ObtenerRestoCadCodProvincia(ByVal cadena As String) As String
        If String.IsNullOrEmpty(cadena) Then
            Return Nothing
        End If

        If ComprobacionCodigoProvincia2X(cadena.Substring(0, 2)) Then
            Return cadena.Substring(2, cadena.Length - 2).Trim()
        End If

        If ComprobacionCodigoProvincia2X(cadena.Substring(0, 1) & " ") Then
            Return cadena.Substring(1, cadena.Length - 1).Trim()
        End If

        Return Nothing
    End Function

    Private Function ComprobacionCodigo4N3X(ByVal texto As String) As Boolean
        texto = texto.ToUpper()

        If System.Text.RegularExpressions.Regex.IsMatch(texto, "[0-9]{4}[B-Z]{3}") Then
            If texto.Contains("Ñ") OrElse texto.Contains("Q") OrElse texto.Contains("A") OrElse _
                 texto.Contains("E") OrElse texto.Contains("I") OrElse texto.Contains("O") OrElse _
                 texto.Contains("CH") OrElse texto.Contains("LL") OrElse texto.Contains("U") Then

                Return False
            Else
                Return True
            End If

        End If

    End Function

    Private Function ComprobacionCodigoVehiculosEstado(ByVal texto As String) As Boolean
        texto = texto.ToUpper()

        If System.Text.RegularExpressions.Regex.IsMatch(texto, "A  " & _
            "|DGP|EA |ET |FN |MF |MMA|MOP|PGC|PME|PMM|") Then
            Return True
        Else
            Return False
        End If
    End Function

    Private Function ObtenerCodTipoMatricula(ByVal tipoM As TipoMatricula) As String
        Dim arrayTM As Array
        arrayTM = Split(tipoM, "TM")
        If arrayTM.GetLength(0) = 2 Then
            Return arrayTM(1)
        ElseIf arrayTM.GetLength(0) = 1 Then
            Return " "
        End If

        Return Nothing

    End Function

#End Region

End Class


Public Enum TipoMatricula
    NormalTM = 1
    VehiculosEspecialesTME = 2
    VehiculosEspeciales2TME1 = 3
    RemolquesTMR = 4
    Remolques2TMR1 = 5
    CiclomotoresTMC = 6
    DiplomaticoTMD = 7
    TuristicaTMT = 8
    Turistica2TMT1 = 9
    HistoricaTMH = 10
    TemporalTMTE = 11
    Temporal2TMTT = 12
    PruebasTransporteTMP = 13
    InspeccionTecnicaVehiculosTMI = 14
    VehiculosEstadoTMM = 15
    VehiculosEstado2TMM1 = 16
    LibreTMK = 17
End Enum


