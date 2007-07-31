Imports Framework.Procesos.ProcesosDN

Public Class OperacionesAS
    Inherits Framework.AS.BaseAS

#Region "Métodos"

    ''' <summary>
    ''' Recupera el objeto ejecutor del cliente cuyo nombre se pasa como parámetro. Este ejecutor
    ''' contiene la colección de ejecutores para los verbos de dicho cliente.
    ''' </summary>
    ''' <param name="nombreCliente"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarEjecutorCliente(ByVal nombreCliente As String) As EjecutoresDeClienteDN
        Dim servicio As ProcesosWS.ProcesosWS
        Dim respuesta As Byte()
        Dim ejecutor As EjecutoresDeClienteDN

        'crear y redirigir a la url del servicio
        servicio = New ProcesosWS.ProcesosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        respuesta = servicio.RecuperarEjecutorCliente(nombreCliente)
        ejecutor = Framework.Utilidades.Serializador.DesSerializar(respuesta)

        Return ejecutor

    End Function


    ''' <summary>
    ''' Pasamos un proceso de ejecución en bloque y nos devuelven el ticket para identificar
    ''' el proceso a los 2 lados (cliente y servidor)
    ''' </summary>
    ''' <param name="pProcesoEjecucion">El Proceso de Ejecución ya formado</param>
    ''' <returns>el ticket para identificar el proceso a los 2 lados (cli - serv)</returns>
    Public Function EjecutarOperacionEnBloque(ByVal pProcesoEjecucion As Framework.Procesos.ProcesosDN.ProcesoDeEjecucionDN) As String

    End Function

    ''' <summary>
    ''' A partir del Ticket de identificación, recuperamos el proceso correspondiente
    ''' </summary>
    ''' <param name="pTicket">el GUID del proceso</param>
    Public Function RecuperarProceso(ByVal pTicket As String) As ProcesoDeEjecucionDN

    End Function

    ''' <summary>
    ''' permite la ejecución de la oepracion con la dn modificada o sin modificar
    ''' </summary>
    ''' <param name="objeto"></param>
    ''' <param name="pOperacionRealizadaDN"></param>
    ''' <param name="pParametros"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function EjecutarOperacionOpr(ByVal objeto As Object, ByVal pOperacionRealizadaDN As ProcesosDN.OperacionRealizadaDN, ByVal pParametros As Object) As Framework.DatosNegocio.IEntidadBaseDN


        Dim servicio As ProcesosWS.ProcesosWS
        Dim respuesta, parametros As Byte()
        Dim pr As New Framework.Procesos.ProcesosDN.ParametroOperacionPr
        If pParametros IsNot Nothing AndAlso pParametros.GetType.IsSerializable Then
            pr.Parametros = pParametros
        End If
        pr.OperacionRealizada = pOperacionRealizadaDN
        pr.IEntidadDN = objeto
        parametros = Framework.Utilidades.Serializador.Serializar(pr)




        'crear y redirigir a la url del servicio
        servicio = New ProcesosWS.ProcesosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Dim colclones As Framework.DatosNegocio.ColIEntidadDN
        Dim ht As Hashtable = pOperacionRealizadaDN.ToHtGUIDs(Nothing, colclones)
        Debug.WriteLine(colclones.Count)
        Dim al As New ArrayList
        al.Add(pOperacionRealizadaDN)

        respuesta = servicio.EjecutarOperacion(parametros)
        EjecutarOperacionOpr = Framework.Utilidades.Serializador.DesSerializar(respuesta)


    End Function

    ''' <summary>
    ''' No permite modificar la entidad en esta operacion
    ''' </summary>
    ''' <param name="objeto"></param>
    ''' <param name="pTransicionRealizada"></param>
    ''' <param name="pParametros"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function EjecutarOperacion(ByVal objeto As Object, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object) As Framework.DatosNegocio.IEntidadBaseDN
        Dim servicio As ProcesosWS.ProcesosWS

        Dim objEnt As Framework.DatosNegocio.IEntidadBaseDN = objeto
        If objEnt.Estado <> DatosNegocio.EstadoDatosDN.SinModificar Then
            Throw New ApplicationException("La entidad no puede ser modificada con esta operación")
        End If

        'crear y redirigir a la url del servicio
        servicio = New ProcesosWS.ProcesosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()


        Dim respuesta, parametros As Byte()
        Dim pr As New Framework.Procesos.ProcesosDN.ParametroOperacionPr
        If pParametros IsNot Nothing AndAlso pParametros.GetType.IsSerializable Then
            pr.Parametros = pParametros
        End If
        pr.TransicionRealizada = pTransicionRealizada
        ' pr.IEntidadDN = objeto
        parametros = Framework.Utilidades.Serializador.Serializar(pr)


        Dim colclones As Framework.DatosNegocio.ColIEntidadDN
        Dim ht As Hashtable = pTransicionRealizada.ToHtGUIDs(Nothing, colclones)
        Debug.WriteLine(colclones.Count)
        Dim al As New ArrayList
        al.Add(pTransicionRealizada)


        respuesta = servicio.EjecutarOperacion(parametros)
        EjecutarOperacion = Framework.Utilidades.Serializador.DesSerializar(respuesta)


    End Function



    ''' <summary>
    ''' pemite la ejecución de la operacion solo si la dn esta modificada
    ''' </summary>
    ''' <param name="objeto"></param>
    ''' <param name="pTransicionRealizada"></param>
    ''' <param name="pParametros"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function EjecutarOperacionModificarObjeto(ByVal objeto As Object, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object) As Framework.DatosNegocio.IEntidadBaseDN
        Dim servicio As ProcesosWS.ProcesosWS

        ' para que solo se ejecute si el objeto esta modificado ' TODO: alex esto debiera de estar en un lnc no en un as 
        'Dim objEnt As Framework.DatosNegocio.IEntidadBaseDN = objeto
        'If objEnt.Estado <> DatosNegocio.EstadoDatosDN.SinModificar Then
        '    Throw New ApplicationException("La entidad no puede ser modificada con esta operación")
        'End If


        Dim respuesta, parametros As Byte()
        Dim pr As New Framework.Procesos.ProcesosDN.ParametroOperacionPr
        'no introducir el parametro si es serializable
        If pParametros IsNot Nothing AndAlso pParametros.GetType.IsSerializable Then
            pr.Parametros = pParametros
        End If
        pr.TransicionRealizada = pTransicionRealizada
        pr.IEntidadDN = objeto


        parametros = Framework.Utilidades.Serializador.Serializar(pr)



        'crear y redirigir a la url del servicio
        servicio = New ProcesosWS.ProcesosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Dim colclones As Framework.DatosNegocio.ColIEntidadDN
        Dim ht As Hashtable = pTransicionRealizada.ToHtGUIDs(Nothing, colclones)
        Debug.WriteLine(colclones.Count)
        Dim al As New ArrayList
        al.Add(pTransicionRealizada)
        respuesta = servicio.EjecutarOperacion(parametros)
        EjecutarOperacionModificarObjeto = Framework.Utilidades.Serializador.DesSerializar(respuesta)


    End Function

    Public Function RecuperarOperacionesAutorizadasSobre(ByVal pHuellaEntidad As Framework.DatosNegocio.HEDN) As Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN

        Dim servicio As ProcesosWS.ProcesosWS
        Dim respuesta, objetoBA As Byte()

        'crear y redirigir a la url del servicio
        servicio = New ProcesosWS.ProcesosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        objetoBA = Framework.Utilidades.Serializador.Serializar(pHuellaEntidad)

        respuesta = servicio.RecuperarTransicionesAutorizadasSobre(objetoBA)

        RecuperarOperacionesAutorizadasSobre = Framework.Utilidades.Serializador.DesSerializar(respuesta)


    End Function

    Public Function RecuperarTransicionesDeInicio(ByVal pTipoDN As System.Type) As ColTransicionDN
        Dim servicio As ProcesosWS.ProcesosWS
        Dim respuesta, objetoBA As Byte()

        'crear y redirigir a la url del servicio
        servicio = New ProcesosWS.ProcesosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        objetoBA = Framework.Utilidades.Serializador.Serializar(pTipoDN)

        respuesta = servicio.RecuperarTransicionesDeInicio(objetoBA)

        RecuperarTransicionesDeInicio = Framework.Utilidades.Serializador.DesSerializar(respuesta)

    End Function

#End Region

End Class
