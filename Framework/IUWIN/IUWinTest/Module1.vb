Imports Framework.IU.IUComun

Module Module1

    Public Sub main()
        Dim tabla As New Hashtable

        tabla.Add("prueba", New MotorIU.Motor.Destino(GetType(IUWinTest.Form1), GetType(IUWinTest.ctrlGenerico))) ' "IUWinTest.Form1", "IUWinTest.ctrlGenerico"))

        Dim minavegador As New navegador(tabla)

        Application.EnableVisualStyles()

        Application.Run()

    End Sub

End Module

Public Class navegador
    Inherits MotorIU.Motor.NavegadorBase

    Public Sub New(ByVal ptabla As Hashtable)
        MyBase.New(ptabla)

        Me.Navegar("prueba", Me, Nothing, TipoNavegacion.Normal, Me.GenerarDatosIniciales, Nothing)
    End Sub
End Class

Public Class ctrlGenerico
    Inherits MotorIU.FormulariosP.ControladorFormBase

    Public Sub New()

    End Sub
End Class

