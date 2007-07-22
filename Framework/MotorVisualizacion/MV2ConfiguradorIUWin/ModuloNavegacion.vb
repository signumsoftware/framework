Module ModuloNavegacion

    Public Sub main()
        Dim mitabla As New Hashtable

        mitabla.Add("NAV1", New MotorIU.Motor.Destino(GetType(MV2ConfiguradorIUWin.Form1), GetType(MV2ConfiguradorIUWin.Form1ctrl))) ' "MV2ConfiguradorIUWin.Form1", "MV2ConfiguradorIUWin.Form1ctrl"))
        Dim marco As New Navegador(mitabla)
        Application.EnableVisualStyles()
        Application.Run()

    End Sub

End Module


Public Class Navegador
    Inherits MotorIU.Motor.NavegadorBase

    Public Sub New(ByVal pTablanavegacion As Hashtable)
        MyBase.New(pTablanavegacion)

        'navegar al inicial
        Me.Navegar("NAV1", Me, Nothing, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosIniciales, Nothing)
    End Sub
End Class