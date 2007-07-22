#Region "Importaciones"

Imports Framework.DatosNegocio

#End Region

Namespace Transacciones

    ''' <summary>Esta clase proporciona la funcionalidad minima para implementar clases de logica de negocio.</summary>
    Public MustInherit Class BaseTransaccionLN

#Region "Atributos"
        'Transaccion logica en la que se embebe esta transaccion
        Protected mTL As ITransaccionLogicaLN

        'Recurso contra el que se ejecuta la transaccion logica
        Protected mRec As IRecursoLN
#End Region

#Region "Constructores"
        ''' <summary>Constructor que admite la transaccion logica y el recurso que utilizamos.</summary>
        ''' <param name="pTL" type="ITransaccionLogicaLN">
        ''' Transaccion logica en las que nos vamos a embeber. Si es nothing nosotros comenzamos la
        ''' transaccion.
        ''' </param>
        ''' <param name="pRec" type="IRecursoLN">
        ''' Recurso sobre el que vamos a ejecutar la transaccion.
        ''' </param>
        Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
            Dim mensaje As String = String.Empty

            If (ValRecurso(pRec, mensaje) = False) Then
                Throw New ApplicationException(mensaje)
            End If

            mTL = pTL
            mRec = pRec
        End Sub
#End Region

#Region "Metodos Validacion"
        ''' <summary>Metodo que valida si el recurso es correcto o no.</summary>
        ''' <param name="pRec" type="IRecursoLN">
        ''' Recurso que queremos validar.
        ''' </param>
        ''' <param name="pMensaje" type="String">
        ''' Parametro donde se devuelve el mensaje de error en caso de que el recurso sea invalido.
        ''' </param>
        ''' <returns>Si el recurso es valido o no.</returns>
        Public Shared Function ValRecurso(ByVal pRec As IRecursoLN, ByRef pMensaje As String) As Boolean
            If (pRec Is Nothing) Then
                pMensaje = "Error: el recurso no puede ser nulo."
                Return False
            End If

            Return True
        End Function
#End Region

#Region "Metodos"

        Protected Function ObtenerTransaccionDeProceso() As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN
            Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
            ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
            ObtenerTransaccionDeProceso = Nothing
            ctd.IniciarTransaccion(Me.mTL, ObtenerTransaccionDeProceso)
        End Function


        ''' <summary>Devuelve la operacion que se debe realizar con una entidad de datos al intentar ser guardada.</summary>
        ''' <param name="pEntidad" type="EntidadDN">
        ''' Entidad de la que queremos saber que operacion necesita operarse sobre ella.
        ''' </param>
        ''' <returns>Devuelve la operacion que hay que realizar para esta entidad.</returns>
        Protected Function OperacionARealizar(ByVal pEntidad As IEntidadBaseDN) As OperacionGuardarLN
            'Si es nothing no hay que hacer nada
            If (pEntidad Is Nothing) Then
                Return OperacionGuardarLN.Ninguna
            End If




            'Si no tiene id es que aun no se ha insertado
            If (pEntidad.ID Is Nothing OrElse pEntidad.ID = String.Empty) Then
                Return OperacionGuardarLN.Insertar
            End If



            'Si esta modificada hay que actualizarla
            If (pEntidad.Estado = EstadoDatosDN.Modificado) Then
                Return OperacionGuardarLN.Modificar
            End If




            'Si no es ningun caso no hacemos nada con ella
            Return OperacionGuardarLN.Ninguna
        End Function

        ''' <summary>Nos indica si un objeto es un EntidadBaseDN o no.</summary>
        ''' <param name="pElemento" type="Object">
        ''' Elemento del que queremos saber su tipo.
        ''' </param>
        ''' <returns>Si el objeto era una EntidadBaseDN (o una clase heredada) o no.</returns>
        Protected Function ParametroConObjeto(ByVal pElemento As Object) As Boolean
            If (pElemento Is Nothing) Then
                Return False
            End If

            If (TypeOf pElemento Is EntidadBaseDN) Then
                Return True
            End If
        End Function
#End Region

    End Class



  
End Namespace
