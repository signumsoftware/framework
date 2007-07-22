Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports Framework.DatosNegocio
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Usuarios.DN
Imports Framework.Procesos.ProcesosLN

Imports GestionSegurosAMV.AD

Imports Framework.Cuestionario.CuestionarioDN

<TestClass()> Public Class utCuestionario

    Dim mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN

    Public Sub New()
        ObtenerRecurso()
    End Sub

    Private Sub ObtenerRecurso()

        Dim connectionstring As String
        Dim htd As New Dictionary(Of String, Object)

        connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd='sa'"
        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GSAMV.AD.GestorMapPersistenciaCamposGSAMV


    End Sub

    Private Sub CrearPregunta(ByVal c As CuestionarioDN, ByVal nombre As String, ByVal tipo As TipoCaracteristica, ByVal textoPregunta As String, ByVal colCaract As ColCaracteristicaDN)
        Dim caracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
        Dim pregunta As PreguntaDN

        caracteristica = colCaract.RecuperarPrimeroXNombre(nombre)

        If caracteristica Is Nothing Then
            caracteristica = New CaracteristicaDN()
        End If
        caracteristica.Nombre = nombre
        caracteristica.TipoCaracteristica = tipo

        pregunta = New PreguntaDN()
        pregunta.CaracteristicaDN = caracteristica
        pregunta.Nombre = nombre
        pregunta.TextoPregunta = textoPregunta
        c.ColPreguntaDN.Add(pregunta)
    End Sub

    <TestMethod()> Public Sub GenerarCuestionarioPlantilla()

        Dim c As New Framework.Cuestionario.CuestionarioDN.CuestionarioDN()

        c.Nombre = "Cuestionario tarificación AMV"
        c.FI = New Date(2003, 1, 1)

        c.ColPreguntaDN = New Framework.Cuestionario.CuestionarioDN.ColPreguntaDN()

        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)

            Dim bLN As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            bLN.RecuperarLista(GetType(CaracteristicaDN))
            Dim colCaracteristicas As New ColCaracteristicaDN()

            bLN = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colCaracteristicas = New ColCaracteristicaDN()
            Dim listaC As System.Collections.IList = bLN.RecuperarLista(GetType(CaracteristicaDN))
            For Each caract As CaracteristicaDN In listaC
                colCaracteristicas.Add(caract)
            Next

            'creaMos las caracteristicas y las preguntas asociadas a ellas
            CrearPregunta(c, "CodigoConcesionario", TipoCaracteristica.Texto, "Código Concesionario", colCaracteristicas)
            CrearPregunta(c, "CodigoVendedor", TipoCaracteristica.Texto, "Código Vendedor", colCaracteristicas)
            CrearPregunta(c, "FechaEfecto", TipoCaracteristica.Fecha, "Fecha efecto", colCaracteristicas)
            CrearPregunta(c, "EsCliente", TipoCaracteristica.Booleano, "¿Está usted asegurado en AMV?", colCaracteristicas)
            CrearPregunta(c, "IDCliente", TipoCaracteristica.Texto, "", colCaracteristicas)
            CrearPregunta(c, "Nombre", TipoCaracteristica.Texto, "Nombre", colCaracteristicas)
            CrearPregunta(c, "Apellido1", TipoCaracteristica.Texto, "Apellidos", colCaracteristicas)
            CrearPregunta(c, "Apellido2", TipoCaracteristica.Texto, "", colCaracteristicas)
            CrearPregunta(c, "NIF", TipoCaracteristica.Texto, "NIF", colCaracteristicas)
            CrearPregunta(c, "Sexo", TipoCaracteristica.Objeto, "Sexo", colCaracteristicas)
            CrearPregunta(c, "FechaNacimiento", TipoCaracteristica.Fecha, "Fecha de Nacimiento", colCaracteristicas)
            CrearPregunta(c, "EDAD", TipoCaracteristica.Numerica, "Edad", colCaracteristicas)
            CrearPregunta(c, "Telefono", TipoCaracteristica.Texto, "Teléfono", colCaracteristicas)
            CrearPregunta(c, "Fax", TipoCaracteristica.Texto, "Fax", colCaracteristicas)
            CrearPregunta(c, "Email", TipoCaracteristica.Texto, "E-mail", colCaracteristicas)
            CrearPregunta(c, "DireccionEnvio", TipoCaracteristica.Objeto, "Dirección de envío", colCaracteristicas)
            CrearPregunta(c, "ZONA", TipoCaracteristica.Numerica, "Código Postal circulación", colCaracteristicas)
            CrearPregunta(c, "Circulacion-Localidad", TipoCaracteristica.Objeto, "", colCaracteristicas)
            CrearPregunta(c, "Marca", TipoCaracteristica.Objeto, "Marca", colCaracteristicas)
            CrearPregunta(c, "Modelo", TipoCaracteristica.Objeto, "Modelo", colCaracteristicas)
            CrearPregunta(c, "CYLD", TipoCaracteristica.Numerica, "Cilindrada(cm3)", colCaracteristicas)
            CrearPregunta(c, "EstaMatriculado", TipoCaracteristica.Booleano, "¿Está matriculado el vehículo?", colCaracteristicas)
            CrearPregunta(c, "FechaMatriculacion", TipoCaracteristica.Fecha, "Fecha de 1ª Matriculación", colCaracteristicas)
            CrearPregunta(c, "ANTG", TipoCaracteristica.Numerica, "Antiguedad", colCaracteristicas)
            CrearPregunta(c, "FechaFabricacion", TipoCaracteristica.Booleano, "Fecha de Fabricación", colCaracteristicas)
            CrearPregunta(c, "TieneCarnet", TipoCaracteristica.Booleano, "¿Tiene carné de conducir?", colCaracteristicas)
            CrearPregunta(c, "FechaCarnet", TipoCaracteristica.Fecha, "Fecha del carné", colCaracteristicas)
            CrearPregunta(c, "CARN", TipoCaracteristica.Numerica, "Años de carné", colCaracteristicas)
            CrearPregunta(c, "TipoCarnet", TipoCaracteristica.Numerica, "Tipo de carné", colCaracteristicas)
            CrearPregunta(c, "ColConductoresAdicionales", TipoCaracteristica.Objeto, "Puede designar de 1 a 4 conductor(es) ocasional(es)." & Chr(10) & Chr(13) & "Estos conductores deben ser miembros de su familia y disponer del carné necesario para el vehículo a asegurar", colCaracteristicas)
            CrearPregunta(c, "MCND", TipoCaracteristica.Numerica, "EdadMCND", colCaracteristicas)
            CrearPregunta(c, "ConductoresAdicionalesConCarnet", TipoCaracteristica.Booleano, "¿Los conductores adicionales son titulares del carné B desde hace más de 3 años o del carné A o A1?", colCaracteristicas)
            CrearPregunta(c, "SiniestroResponsable3años", TipoCaracteristica.Numerica, "¿Cuántos siniestros con responsabilidad parcial o total ha tenido usted en el transcurso de los últimos 3 años (coche y moto)?", colCaracteristicas)
            CrearPregunta(c, "SiniestroSinResponsabilidad3años", TipoCaracteristica.Numerica, "¿Cuántos siniestros sin responsabilidad ha tenido usted en el transcurso de los últimos 3 años (coche y moto)?", colCaracteristicas)
            CrearPregunta(c, "RetiradaCarnet3años", TipoCaracteristica.Booleano, "¿Ha cometido usted alguna infracción que conllevó la retirada del carné de conducir en los últimos 3 años?", colCaracteristicas)
            CrearPregunta(c, "ConduccionEbrio3años", TipoCaracteristica.Booleano, "¿Ha cometido alguna infracción por conducir en estado ebrio en los últimos 3 años?", colCaracteristicas)
            CrearPregunta(c, "VehículoTransporteRemunerado", TipoCaracteristica.Booleano, "¿Utiliza su vehículo para el transporte remunerado de personas y/o de mercancías?", colCaracteristicas)
            CrearPregunta(c, "CanceladoSeguro3años", TipoCaracteristica.Booleano, "¿Le ha cancelado su seguro alguna compañía de seguros en los últimos 3 años?", colCaracteristicas)
            CrearPregunta(c, "PermisoCirculacionEspañol", TipoCaracteristica.Booleano, "¿Dispone de un permiso de circulación español?", colCaracteristicas)
            CrearPregunta(c, "TitularPermisoCirculación", TipoCaracteristica.Booleano, "¿Es usted, su cónyuge o sus padres titular del permiso de circulación del vehículo?", colCaracteristicas)
            CrearPregunta(c, "AseguradoActualmente", TipoCaracteristica.Booleano, "Actualmente está asegurado con una moto o un coche", colCaracteristicas)
            CrearPregunta(c, "VencimientoSeguroActual", TipoCaracteristica.Fecha, "Vencimiento", colCaracteristicas)
            CrearPregunta(c, "AñosSinSiniestro", TipoCaracteristica.Numerica, "¿Cuántos años lleva sin un siniestro (como responsable o no)?", colCaracteristicas)
            CrearPregunta(c, "Justificantes", TipoCaracteristica.Numerica, "¿Cuáles son los justificantes que puede aportar?", colCaracteristicas)

            Using tr As New Transaccion()
                Dim lng As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                lng.GuardarGenerico(c)

                tr.Confirmar()
            End Using
        End Using

    End Sub

End Class
