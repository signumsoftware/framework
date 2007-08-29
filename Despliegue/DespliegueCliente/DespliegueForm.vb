Imports System.IO
Imports Despliegue.DN
Imports Despliegue.Compartido
Imports System.Threading


Public Class DespliegueForm
    Private ntc As ClienteLNC

    Private bandera As Boolean = True
    Private mithread As Thread
    Private correcto As Boolean = False

    Public Sub ActualizaArchivo(ByVal archivo As String, ByVal ord As Orden, ByVal pTam As Int32)
        If (Thread.CurrentThread.Name = "WS") Then
            Me.Invoke(New DelActuArchivo(AddressOf ActualizaArchivoSync), New _
   Object() {archivo, ord, pTam})
        Else
            ActualizaArchivoSync(archivo, ord, pTam)
        End If
    End Sub

    Public Sub ActualizaArchivoSync(ByVal archivo As String, ByVal ord As Orden, ByVal pTam As Int32)
        ListBox1.Items.Add("Orden  " & ord.ToString() & " sobre el archivo " & archivo & " de " & ToComputerSize(pTam))
        ListBox1.SelectedIndex = ListBox1.Items.Count - 1
        lbInf.Text = "Descargado archivo " & archivo & " de " & ToComputerSize(pTam)

        pbTotal.Value += pTam

        pbArchivo.Maximum = pTam
        pbArchivo.Minimum = 0
        pbArchivo.Value = 0

    End Sub


    Public Sub ActualizaProgressArch(ByVal incVal As Int32)
        If (Thread.CurrentThread.Name = "WS") Then
            Me.Invoke(New DelProgressFile(AddressOf ActualizaProgressArchSync), New Object() {incVal})
        Else
            ActualizaProgressArchSync(incVal)
        End If
    End Sub

    Public Sub ActualizaProgressArchSync(ByVal incVal As Int32)
        pbArchivo.Value += incVal
    End Sub


    ''-----------------------------------
    Public Sub EstablecerNumero(ByVal totArch As Int32)
        If (Thread.CurrentThread.Name = "WS") Then
            Me.Invoke(New DelEstNumArch(AddressOf EstablecerNumeroSync), New Object() {totArch})
        Else
            EstablecerNumeroSync(totArch)
        End If
    End Sub

    Public Sub EstablecerNumeroSync(ByVal totArch As Int32)
        If (totArch <> 0) Then Me.Show()
        pbTotal.Maximum = totArch
        pbTotal.Value = 0
        pbTotal.Minimum = 0
        ListBox1.Items.Add("Hay " & ToComputerSize(totArch) & " en archivos que descargar:")

        lbInf.Text = "Comenzando descarga de Archivos: " & ToComputerSize(totArch)
    End Sub


    ''-----------------------------------
    Public Sub EjecutarYMorir(ByVal totArch As String)
        If (Thread.CurrentThread.Name = "WS") Then
            Me.Invoke(New DelEjecutarYMorir(AddressOf EjecutarYMorirSync), New Object() {totArch})
        Else
            EjecutarYMorirSync(totArch)
        End If
    End Sub


    Public Sub EjecutarYMorirSync(ByVal ejecutable As String)

        lbInf.Text = "Correctamente actualizado, lanzando aplicación"

        Dim s As String = My.Settings.RutaLocal

        correcto = True
        mithread.Abort()
        Me.Hide()
        Dim ruta As String
        ruta = Path.Combine(s, ejecutable.TrimStart("\"c))
        If (File.Exists(ruta)) Then
            Process.Start(ruta)
        End If
        Application.Exit()
    End Sub
    ''-----------------------------------



    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Me.Hide()

        ListBox1.Items.Clear()
        lbInf.Text = "Verificando conexión con el servidor"
        ntc = New ClienteLNC

    End Sub


    Public Sub MiHilo()
        Try
            Dim s As String = My.Settings.RutaLocal

            ntc.ActualizarTodo(s, AddressOf EstablecerNumero, _
                                  AddressOf ActualizaArchivo, _
                                  AddressOf ActualizaProgressArch, _
                                  AddressOf EjecutarYMorir)

        Catch ex As Exception
            If (Not correcto) Then
                MessageBox.Show("Actualización Terminada Forzosamente: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If

        End Try
    End Sub

    Private Shared Function ToComputerSize(ByVal value As Int32) As String
        Dim valor As Double = value
        Dim colas As String() = {"Bytes", "KBytes", "MBytes", "GBytes", "TBytes", "PByte", "EByte", "ZByte", "YByte"}
        Dim i As Int32 = 0
        While valor >= 1024
            valor /= 1024.0
            i += 1
        End While
        Return valor.ToString("#,###.00") + " " + colas(i)
    End Function


    Private Sub Form1_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.Activated
        If bandera Then
            Dim mithreadst As ThreadStart = New ThreadStart(AddressOf MiHilo)
            mithread = New Thread(mithreadst)
            mithread.Name = "WS"
            mithread.Start()
            bandera = False
        End If
    End Sub

    Private Sub Form1_Closing(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
        If (Not mithread Is Nothing) Then mithread.Abort()
    End Sub

End Class
