
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.TiposYReflexion.DN
Imports MNavegacionDatosDN


Imports Framework.LogicaNegocios.Transacciones

Public Class MNavDatosAD
    'Public Function RecuperarColHuellas(ByVal pRelEntNavVinc As MNavegacionDatosDN.RelEntNavVincDN) As Framework.DatosNegocio.ColIHuellaEntidadDN



    '    If pRelEntNavVinc.DireccionLectura = DireccionesLectura.Reversa Then

    '        Dim pr As New Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN(pRelEntNavVinc.PropVinc.PropertyInfoVinc, pRelEntNavVinc.PropVinc.InstanciaVincReferida.DN.ID, pRelEntNavVinc.PropVinc.InstanciaVincReferida.DN.GUID)
    '        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

    '        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
    '        RecuperarColHuellas = gi.RecuperarColHuellasRelInversa(pr, pRelEntNavVinc.RelacionEntidadesNav.TipoDestino)


    '    Else

    '        Dim pr As New Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN(pRelEntNavVinc.PropVinc.PropertyInfoVinc, pRelEntNavVinc.PropVinc.InstanciaVincReferida.DN.ID, pRelEntNavVinc.PropVinc.InstanciaVincReferida.DN.GUID)
    '        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

    '        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
    '        RecuperarColHuellas = gi.RecuperarColHuellasRelDirecta(pr, pRelEntNavVinc.RelacionEntidadesNav.TipoDestino)

    '    End If



    'End Function




    'Public Function ModificarSQLRelInversa(ByVal psqlBuscador As String, ByVal pRelEntNavVinc As MNavegacionDatosDN.RelEntNavVincDN) As String





    '    Using tr As New Transaccion


    '        If pRelEntNavVinc.DireccionLectura = DireccionesLectura.Reversa Then

    '            Dim pr As New Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN(pRelEntNavVinc.PropVinc.PropertyInfoVinc, pRelEntNavVinc.PropVinc.InstanciaVincReferida.DN.ID, pRelEntNavVinc.PropVinc.InstanciaVincReferida.DN.GUID)
    '            Dim ad As New Framework.AccesoDatos.MotorAD.AD.AccesorMotorAD(Transaccion.Actual, Recurso.Actual, New Framework.AccesoDatos.MotorAD.AD.ConstructorSQLSQLsAD)
    '            ModificarSQLRelInversa = ad.ModificarSQLRelInversa(psqlBuscador, Nothing, Nothing, Nothing, parametros, Nothing)


    '            tr.Confirmar()

    '        Else

    '            Throw New ApplicationException

    '        End If




    '    End Using


    'End Function




    Public Overloads Function ModificarSQLRelInversa(ByVal psqlBuscador As String, ByVal pParametros As List(Of System.Data.IDataParameter), ByVal PropiedadDeInstanciaDN As Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN, ByVal pTipoReferido As Type) As String


        ' crear un ad especifico para sql que 
        ' identifique la relacion o tabla relacional que esta en juego
        ' contrulla una sql que realice el join de las dos o tres tablas
        ' y recupere un dataset con informacion para motar la coleccion de huellas no tipadas (tipo + id + guid) de todos los tipos que pudieran estar implciados
        ' leugo esas huellas se pueden usar pare recuperar dichos objetos si fuera necesaario




        Using tr As New Transaccion

            Dim infoReferidora, infoReferido As InfoTypeInstClaseDN
            Dim ginfoi As New InfoTypeInstLN
            Dim camporef As Framework.TiposYReflexion.DN.InfoTypeInstCampoRefDN

            infoReferidora = ginfoi.Generar(PropiedadDeInstanciaDN.Propiedad.ReflectedType, Nothing, "")

            Dim constructor As New Framework.AccesoDatos.MotorAD.AD.ConstructorSQLSQLsAD
            Dim adaptador As Framework.AccesoDatos.MotorAD.AD.AccesorMotorAD
            adaptador = New Framework.AccesoDatos.MotorAD.AD.AccesorMotorAD(Transaccion.Actual, Recurso.Actual, constructor)


            If infoReferidora.CamposRef.Count > 1 Then
                Beep() ' de momento soolo soporta una entidad en la col
            End If

            For Each camporef In infoReferidora.CamposRef

                ' esto es para encontrar el campo o campos una vez aplicada las directrices del mapeado por ejem plo en una interface
                If camporef.Campo.Name = PropiedadDeInstanciaDN.NombreCampoRel Then

                    ' ojo que pasa si es una interface (podrian devolver vaios campos o modificarse el nombre)
                    ' cuando encuntre el campo hacer una sql inversa que te daria una col de id para el mimo tipo con lo que podriamos contruir hurllas y meterlas en una colleccion
                    infoReferido = ginfoi.Generar(pTipoReferido, Nothing, "")
                    ModificarSQLRelInversa = adaptador.ModificarSQLRelInversa(psqlBuscador, infoReferidora, infoReferido, PropiedadDeInstanciaDN, pParametros, PropiedadDeInstanciaDN.IdInstancia, PropiedadDeInstanciaDN.GUIDInstancia)

                    'TODO: ojo este probelma es de dificil resolucion porque supone que hay varias clases que pueden ser los origenes
                    'sin embrago con el sistema de huellas si es facil de resolver 
                    'o con la modificacion para el tratamiento de interfaces en una sola tabla


                    ' una posible solución seria montar en el arbol las posivilidad des de relacion a modo de colecciones para que el usuario abra la que quiera.



                End If
            Next



            tr.Confirmar()

        End Using



    End Function


End Class
