Imports MotorIU.Motor

Module Module1

    Public Sub Main()
        Dim Navegador As Hashtable


        Dim cmarco As Marco

        'cargamos los datos para el navegador
        Navegador = New Hashtable

        Navegador.Add("Filtro", New Destino(GetType(MotorBusquedaIuWin.frmFiltro), GetType(MotorBusquedaIuWin.frmFiltroctrl))) ' "MotorBusquedaIuWin.frmFiltro", "MotorBusquedaIuWin.frmFiltroctrl"))

        'habilitamos aspecto XP
        Application.EnableVisualStyles()

        'instanciamos la clase que va a llevar el motor de formularios
        cmarco = New Marco(Navegador)

        Application.Run()

        'End Module
    End Sub

    Public Class Marco
        Inherits MotorIU.Motor.NavegadorBase

        Public Sub New(ByVal pTablaNavegacion As Hashtable)
            MyBase.New(pTablaNavegacion)

            Me.Navegar("Filtro", Me, Nothing, TipoNavegacion.Normal, Me.GenerarDatosIniciales, Nothing)
        End Sub
    End Class

End Module
