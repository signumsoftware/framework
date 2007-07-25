Imports Framework.DatosNegocio

<Serializable()> _
Public Class ModuladorRVSVDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN


    Protected mIRecSumiValorLN As Framework.Operaciones.OperacionesDN.IRecSumiValorLN ' este no debe guardarse en base de datos
    Protected mColModuladorRV As ColModuladorRVDN
    Protected mCobertura As FN.Seguros.Polizas.DN.CoberturaDN
    Protected mCaracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN


    Protected mModulador As ModuladorDN

    Protected mValorCacheado As ModuladorRVDN ' este valor no debe guardarse en la base de datos a priori

    Public Overrides Function ToString() As String

        Return Me.mCobertura.Nombre & " " & mCaracteristica.Nombre & mModulador.ToString
    End Function

    <RelacionPropCampoAtribute("mModulador")> _
    Public Property Modulador() As ModuladorDN

        Get
            Return mModulador
        End Get

        Set(ByVal value As ModuladorDN)
            CambiarValorRef(Of ModuladorDN)(value, mModulador)

        End Set
    End Property

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        ' todos los moduladores deen tener igual caracteristica y cobertura que no son libres

        For Each ModuladorRV As ModuladorRVDN In Me.mColModuladorRV

            If ModuladorRV.Modulador.GUID <> Me.mModulador.GUID OrElse ModuladorRV.Caracteristica.GUID <> mCaracteristica.GUID OrElse ModuladorRV.Cobertura.GUID <> mCobertura.GUID Then
                pMensaje = "lataracteristica y la cobertura de algun ModuladorRVDN no es la misma a la del ModuladorRVSVDN"
                Return EstadoIntegridadDN.Inconsistente
            End If

        Next



        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    <RelacionPropCampoAtribute("mCaracteristica")> _
    Public Property Caracteristica() As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN

        Get
            Return mCaracteristica
        End Get

        Set(ByVal value As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)
            CambiarValorRef(Of Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)(value, mCaracteristica)

        End Set
    End Property


    Public Function RecuperarModelodatos() As ModeloDatosDN
        ' si un modelo cambia de categoria y la nueva categoria perjudica a un cliente en el momento de su renovacion
        ' el cleinte tine que conservar la categoria que tubiera antes de la renovacion


        For Each o As Object In mIRecSumiValorLN.DataSoucers
            If TypeOf o Is ModeloDatosDN Then
                Return o
            End If

        Next
        Return Nothing

    End Function

    Public Function GetValor() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.GetValor

        Dim mitarif As FN.Seguros.Polizas.DN.TarifaDN = RecupearTarifa()
        If mitarif Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("tarifa no puede ser nothing para PrimabaseRVSVDN")
        End If

        If mitarif.RecuperarCobertura(Me.mCobertura.GUID) Is Nothing Then
            ' dado que los productos no continen esta cobertura este modulador no debe contar y devuelve 1 como valor neutro de coeficiente
            Return 1
        End If

        ' verificar si el riesgo de la tarifa es una moto, 
        ' no --> excepción porque debiera de serlo para que este grafo sea aplicable
        ' si --> buscar la categoria en base a los datos del riesgo (modelo y matriculado)

        Dim miRiesgoMotor As FN.RiesgosVehiculos.DN.RiesgoMotorDN = Nothing
        If TypeOf mitarif.Riesgo Is FN.RiesgosVehiculos.DN.RiesgoMotorDN Then
            miRiesgoMotor = mitarif.Riesgo
        End If

        If miRiesgoMotor Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("el riesgo debiera ser un riesgomotor")
        End If



        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        'Recuperar el modelodatos sobre el que se debe tarificar teniendo encuenta las restricciones que impiden perjudicar al cliente por que un modelo cambie de modelo datos
        Dim miModeloDatos As ModeloDatosDN = RecuperarModelodatos()

        If miModeloDatos Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("el ModeloDatos debiera ser un riesgomotor")
        End If




        ' debe de buscar en su coleccion de primas base aquella que coincide con la categoria 
        '  Dim modulador As ModuladorRVDN = mColModuladorRV.Recuperar(mCobertura, miRiesgoMotor.Modelo, miRiesgoMotor.Matriculado, Me.mCaracteristica, mitarif.FEfecto)
        Dim modulador As ModuladorRVDN = mColModuladorRV.Recuperar(mCobertura, miModeloDatos, Me.mCaracteristica)
        If modulador Is Nothing Then
            'Throw New Framework.DatosNegocio.ApplicationExceptionDN("debiera existir un modulador para el modelo")
        Else
            If modulador.Cobertura.GUID <> Me.mCobertura.GUID Then
                Throw New Framework.DatosNegocio.ApplicationExceptionDN("la cobertura del modulador y del recuperador de valor debieran ser iguales")
            End If
        End If

        ' recuperar el objeto valor categoria del cuestionario

        Dim CuestionarioResuelto As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN = RecupearCuestionarioResuelto()
        If CuestionarioResuelto Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("debiera existir un CuestionarioResuelto ")

        End If

        Dim valor As Double
        Dim respuesta As Framework.Cuestionario.CuestionarioDN.RespuestaDN = CuestionarioResuelto.ColRespuestaDN.RecuperarxCaracteristica(Me.mCaracteristica)


        If respuesta Is Nothing Then
            ' debo de lanzar excepción si soy un valor obligatorio 

            If Me.mModulador.NoRequerido Then
                'Debug.WriteLine(Me.mModulador.Nombre & " -- Respuesta no presente")
                valor = 1
            Else
                ' el modulador es requerido
                Throw New ApplicationException("No exite respuesta para el modulador requerido: " & Me.mModulador.Nombre)

            End If

        Else
            If modulador Is Nothing Then
                Throw New Framework.DatosNegocio.ApplicationExceptionDN("debiera existir un modulador para el modelo , dado que es requerido")

            End If

            Dim valorCaracteristica As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN = respuesta.IValorCaracteristicaDN
            mValorCacheado = modulador

            'traducir el valor caracteristica
            valor = modulador.TraducirValor(valorCaracteristica)
        End If

        'Debug.WriteLine(Me.mModulador.Nombre & " -- " & valor)
        Return valor



    End Function



    Public Property Cobertura() As FN.Seguros.Polizas.DN.CoberturaDN
        Get
            Return Me.mCobertura
        End Get
        Set(ByVal value As FN.Seguros.Polizas.DN.CoberturaDN)
            Me.CambiarValorRef(Of FN.Seguros.Polizas.DN.CoberturaDN)(value, mCobertura)
        End Set
    End Property

    Public Property ColModuladorRV() As ColModuladorRVDN
        Get
            Return Me.mColModuladorRV
        End Get
        Set(ByVal value As ColModuladorRVDN)
            Me.CambiarValorRef(Of ColModuladorRVDN)(value, mColModuladorRV)
        End Set
    End Property

    Public Property IRecSumiValorLN() As Framework.Operaciones.OperacionesDN.IRecSumiValorLN Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.IRecSumiValorLN
        Get
            Return mIRecSumiValorLN
        End Get
        Set(ByVal value As Framework.Operaciones.OperacionesDN.IRecSumiValorLN)
            mIRecSumiValorLN = value
        End Set
    End Property
    Private Function RecupearTarifa() As FN.Seguros.Polizas.DN.TarifaDN
        For Each o As Object In mIRecSumiValorLN.DataSoucers
            If TypeOf o Is FN.Seguros.Polizas.DN.TarifaDN Then
                Return o
            End If

        Next
        Return Nothing
    End Function

    Private Function RecupearCuestionarioResuelto() As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        For Each o As Object In mIRecSumiValorLN.DataSoucers
            If TypeOf o Is Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN Then
                Return o
            End If

        Next
        Return Nothing
    End Function

    Public ReadOnly Property ValorCacheado() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.ValorCacheado
        Get
            Return Me.mValorCacheado
        End Get
    End Property

    Public Function RecuperarOrden() As Integer Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.RecuperarOrden
        Throw New NotImplementedException("Recuperar orden no está implementado para esta clase")
    End Function

    Public Sub Limpiar() Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.Limpiar
        mIRecSumiValorLN = Nothing
    End Sub
End Class




<Serializable()> _
Public Class ColModuladorRVSVDN
    Inherits ArrayListValidable(Of ModuladorRVSVDN)



    Public Function Recuperar(ByVal pCobertura As FN.Seguros.Polizas.DN.CoberturaDN) As ColModuladorRVSVDN


        Dim col As New ColModuladorRVSVDN
        For Each modulador As ModuladorRVSVDN In Me
            If modulador.Cobertura.Nombre = pCobertura.Nombre Then
                col.Add(modulador)
            End If
        Next

        Return col
    End Function

    Public Function Recuperar(ByVal pNombreCobertura As String, ByVal pNombreCaracteristica As String) As ColModuladorRVSVDN


        Dim col As New ColModuladorRVSVDN
        For Each modulador As ModuladorRVSVDN In Me
            If modulador.Cobertura.Nombre = pNombreCobertura AndAlso modulador.Caracteristica.Nombre = pNombreCaracteristica Then
                col.Add(modulador)
            End If
        Next

        Return col
    End Function

    Public Function RecuperarxNombreCoberturaYModulador(ByVal pNombreCobertura As String) As ModuladorRVSVDN

        For Each modulador As ModuladorRVSVDN In Me
            If modulador.Cobertura.Nombre = pNombreCobertura Then
                Return modulador
            End If
        Next

        Return Nothing
    End Function

    Public Function RecuperarColModuladorRV() As ColModuladorRVDN

        Dim col As New ColModuladorRVDN

        For Each modulador As ModuladorRVSVDN In Me
            col.AddRange(modulador.ColModuladorRV)
        Next

        Return col
    End Function

End Class




