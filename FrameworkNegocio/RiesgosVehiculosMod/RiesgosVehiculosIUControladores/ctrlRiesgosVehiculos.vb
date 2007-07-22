Imports Framework.IU.IUComun
Public Class ctrlRiesgosVehiculos
    Inherits MotorIU.FormulariosP.ControladorFormBase

#Region "Constructores"

    Public Sub New()

    End Sub

    Public Sub New(ByVal pNavegador As MotorIU.Motor.INavegador)
        MyBase.New(pNavegador)
    End Sub

#End Region

#Region "Métodos"

    Public Function EjecutarOperacionNavegarResumenEconomicoPoliza(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object) As Framework.DatosNegocio.IEntidadBaseDN
        ' operacion 

        'Dim pce As New MotorBusquedaBasicasDN.ParametroCargaEstructuraDN
        'pce.NombreInstanciaMapVis = "FN.Seguros.Polizas.DN.PolizaDN-poliza-resumen-conta"
        'pce.TipodeEntidad = GetType(FN.Seguros.Polizas.DN.PolizaDN)
        'Dim lis As List(Of MotorBusquedaBasicasDN.ValorCampo) = Nothing
        'MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(pParametros, lis, pce)


        Dim pr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN = objeto
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarFormulario(pParametros, pr.Poliza, MotorIU.Motor.TipoNavegacion.Normal, "FN.Seguros.Polizas.DN.PolizaDN-poliza-resumen-conta")
        Return objeto

    End Function



    Public Function VerificarDatosPresupuesto(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As FN.Seguros.Polizas.DN.PresupuestoDN

        ' comando

        Dim control As IctrlBasicoDN = sender
        Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

        Dim presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN = control.DN
        Dim lnc As New FN.RiesgosVehiculos.LNC.RiesgosVehiculosLNC.RVPolizasLNC


        ' obteneos el principal del marco
        ' Dim prin As Framework.Usuarios.DN.PrincipalDN = fp.cMarco.Principal

        presupuesto = lnc.VerificarDatosPresupuesto(presupuesto)



        'Dim paquete As New Hashtable
        'paquete.Add("DN", nota)

        '' navegar al formaulario que permite editar la nota
        'fp.cMarco.Navegar("FG", fp, Nothing, MotorIU.Motor.TipoNavegacion.Normal, paquete)


        Return presupuesto

    End Function



    ''' <summary>
    '''  Metodo que crea una poliza desde un presupuesto
    ''' 
    ''' lanza un formulario  que requiere el cif del tomardor y la amtrigula del riesgo y busca mabos en la base de datos o genra nuevos tomando los datos del presupuesto
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="datos"></param>
    ''' <param name="vm"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function AltaPolizaDesdePresupuesto(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN

        ' comando

        Dim control As IctrlBasicoDN = sender

        ' obtenemos el formulario
        Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm



        ' solicitamos los datos adicionales
        Dim param As New FN.Seguros.Polizas.DN.AltaPolizaPr
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarFormulario(fp, param, MotorIU.Motor.TipoNavegacion.Modal)



        ' creamos la poliza rellena
        'Dim lnc As New FN.RiesgosVehiculos.LNC.RiesgosVehiculosLNC.RVPolizasLNC
        'Dim prsupuesto As FN.Seguros.Polizas.DN.PresupuestoDN = control.DN
        'Dim pr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN = lnc.AltaDePolizaDesdePresupuesto(prsupuesto, param.CifNif, param.Matricula)



        'Dim paquete As New Hashtable
        'paquete.Add("DN", pr)

        '' navegar al formaulario que permite editar la nota
        'fp.cMarco.Navegar("FG", fp, CType(fp, System.Windows.Forms.Form).ParentForm, MotorIU.Motor.TipoNavegacion.CerrarLanzador, paquete)




        Return datos
    End Function

#End Region

End Class
