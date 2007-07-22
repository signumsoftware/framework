<Serializable()> Public Class ParametrosBusquedaDN
    Inherits Framework.DatosNegocio.EntidadBaseDN
    Public NombreTipoDN As String
    Private mNombreFiltro As String
    Public NombreVistaVis As String
    Public NombreVistaSel As String

    Public NombresCaposListado As String 'cadena de texto compuesta por nombres de campos cullos datos distientos poblaran combos de las condiciones del filtro ejemplo id\nombre\edad ...
    Public ListaCamposListados As New Generic.List(Of String)

    Public Property NombreFiltro() As String
        Get
            Return Me.mNombreFiltro
        End Get
        Set(ByVal value As String)
            mNombreFiltro = value
        End Set
    End Property


    Public Property CadenaCodificada() As String
        Get
            If NombresCaposListado Is Nothing OrElse NombresCaposListado = "" Then
                Return NombreTipoDN & "\" & mNombreFiltro & "\" & NombreVistaVis & "\" & NombreVistaSel

            Else
                Return NombreTipoDN & "\" & mNombreFiltro & "\" & NombreVistaVis & "\" & NombreVistaSel & "\" & NombresCaposListado
            End If
        End Get
        Set(ByVal value As String)
            'Dim gmb As MotorBusquedaLN.GestorBusquedaLN
            'gmb = New MotorBusquedaLN.GestorBusquedaLN(Nothing, Application.Item("recurso"))

            '        ValoresFiltro = "Modelo\tlModeloDN\tlModeloDN"

            Dim valores As String()
            valores = value.Split("\")




            Dim a As Int16
            For a = 4 To valores.Length - 1
                ListaCamposListados.Add(valores(a))
            Next

            Me.NombreVistaSel = valores(3)
            Me.NombreVistaVis = valores(2)
            Me.mNombreFiltro = valores(1)
            Me.NombreTipoDN = valores(0)



        End Set
    End Property
End Class
