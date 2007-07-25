Public Class PropVinc

    Implements IVincElemento



    Protected mInstanciaVincReferida As InstanciaVinc
    Protected mInstanciaVinc As InstanciaVinc
    Protected mPi As Reflection.PropertyInfo
    Protected mPiTipoRepresentado As Reflection.PropertyInfo
    Protected mMap As PropMapDN

    'Private Function ObtenerPiTipoRepresentado() As Reflection.PropertyInfo

    '    Dim ruta As String() = mMap.NombreProp.Split(".")


    '    For Each paso As String In ruta





    '    Next

    'End Function
    Public Sub New(ByVal pInstanciaVinc As InstanciaVinc, ByVal pMap As PropMapDN)
        Poblar(pInstanciaVinc, pMap)
    End Sub



    Public ReadOnly Property PropertyInfoVinc() As Reflection.PropertyInfo
        Get
            Return Me.mPi
        End Get
    End Property

    Public ReadOnly Property EsPropiedadEncadenada() As Boolean
        Get
            Return mMap.EsPropiedadEncadenada
        End Get
    End Property

    Public ReadOnly Property RepresentarSubEntidad() As Boolean
        Get
            'If mMap.NombreProp.Contains(".") Then
            '    Return True
            'Else
            Return String.IsNullOrEmpty(Me.mMap.ControlAsignado) AndAlso Not String.IsNullOrEmpty(Me.mMap.DatosControlAsignado)

            ' End If
        End Get
    End Property

    ' ojo esto era read only
    Public Property InstanciaVincReferida() As InstanciaVinc
        Get
            Return Me.mInstanciaVincReferida
        End Get
        Set(ByVal value As InstanciaVinc)
            mInstanciaVincReferida = value
        End Set
    End Property


    'Public ReadOnly Property InstanciaVincReferida() As InstanciaVinc
    '    Get
    '        Return Me.mInstanciaVincReferida
    '    End Get
    'End Property

    Protected Sub Poblar(ByVal pInstanciaVinc As InstanciaVinc, ByVal pMap As PropMapDN)
        mMap = pMap
        mInstanciaVinc = pInstanciaVinc
        mPi = obtenerLaPropiedad(mInstanciaVinc.Tipo, pMap)
        If mPi Is Nothing Then
            'Debug.WriteLine(mPi)
        End If
        If mInstanciaVinc.Tipo Is Nothing Then
            mPiTipoRepresentado = Nothing
        Else
            If pMap.NombreProp.Contains(".") Then
                mPiTipoRepresentado = mInstanciaVinc.Tipo.GetProperty(pMap.NombreProp.Split(".")(0))
            Else
                mPiTipoRepresentado = Nothing
            End If
        End If


        ' recuperar el mapeado que podría tener asignado el control
        If RepresentarSubEntidad Then

            Dim imap As InstanciaMapDN = mInstanciaVinc.IRecuperadorInstanciaMap.RecuperarInstanciaMap(Me.mMap.DatosControlAsignado)
            If imap Is Nothing Then
                Throw New ApplicationException("a este nivel debe de haberse recuperado un a mapeado de instancia")
            End If
            If mPiTipoRepresentado Is Nothing Then

                If String.IsNullOrEmpty(Me.mMap.TipoImpuesto) Then

                    mInstanciaVincReferida = New InstanciaVinc(Me.TipoPropiedad, imap, mInstanciaVinc.IRecuperadorInstanciaMap, Me)

                Else
                    Dim tipo As System.Type
                    Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo(pMap.TipoImpuesto, Nothing, tipo)
                    mInstanciaVincReferida = New InstanciaVinc(tipo, imap, mInstanciaVinc.IRecuperadorInstanciaMap, Me)
                    '  mPiTipoRepresentado = tipo
                End If


            Else
                mInstanciaVincReferida = New InstanciaVinc(mPiTipoRepresentado, imap, mInstanciaVinc.IRecuperadorInstanciaMap, Me)
            End If

        End If

    End Sub


    Public Function TipoATratar() As System.Type


        '' seleccionar el tipo a buscar en el mapeado de persistencia
        If Not Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsRef(Me.mPi.PropertyType) Then
            ' si es tipo por vlor el tipo e el tipo contenido
            TipoATratar = Me.mPi.ReflectedType
        Else
            ' si es tipo por referencia sera el tipo de la peopiedad salvo que sea una col que sera el tiupo fijado en la col
            If Me.EsColeccion Then
                TipoATratar = Me.TipoFijadoColPropiedad
            Else
                TipoATratar = Me.TipoPropiedad
            End If
        End If


        Return TipoATratar
    End Function



    Private Function obtenerLaPropiedad(ByVal pTipo As Type, ByVal pMap As PropMapDN) As Reflection.PropertyInfo
  
        obtenerLaPropiedad = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.obtenerLaPropiedad(pTipo, pMap.NombreProp)


        If obtenerLaPropiedad Is Nothing Then
            ' verificar si se trata de un tipo impuesto
            ' esto es necesario cuando el tipo original de la propieda no es con el que uqeremos vincularnos
            ' por ejemplo es una interface muy generica y queremos vincular un tipo más complejo
            If Not String.IsNullOrEmpty(pMap.TipoImpuesto) Then
                Dim tipo As System.Type
                Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo(pMap.TipoImpuesto, Nothing, tipo)
                obtenerLaPropiedad = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.obtenerLaPropiedad(tipo, pMap.NombreProp)
            End If
        End If

    End Function

    'Private Function obtenerLaPropiedad(ByVal pTipo As Type, ByVal pNombreProp As String) As Reflection.PropertyInfo
    '    Dim mipi As Reflection.PropertyInfo
    '    If Not pTipo Is Nothing Then

    '        If pNombreProp.Contains(".") Then

    '            If pTipo Is Nothing Then
    '                Return Nothing
    '            End If

    '            Dim partes() As String
    '            partes = pNombreProp.Split(".")

    '            Dim miTipo As System.Type
    '            Dim pi As Reflection.PropertyInfo = pTipo.GetProperty(partes(0))
    '            If pi IsNot Nothing Then
    '                miTipo = pi.PropertyType
    '                mipi = obtenerLaPropiedad(miTipo, pNombreProp.Substring(partes(0).Length + 1))


    '            End If

    '            Return mipi

    '        Else
    '            mipi = pTipo.GetProperty(pNombreProp)
    '            If mipi Is Nothing Then
    '                mipi = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarPropiedad(pTipo, pNombreProp, Nothing)
    '            End If

    '            Return mipi
    '        End If
    '    End If

    'End Function

    Private Function obtenerLaInstanciaContenedoraDeProp(ByVal pInstancia As Object, ByVal pNombreProp As String) As Object

        If pNombreProp.Contains(".") Then

            Dim partes() As String
            partes = pNombreProp.Split(".")

            Dim miObjeto As Object
            miObjeto = pInstancia.GetType.GetProperty(partes(0)).GetValue(pInstancia, Nothing)
            If miObjeto Is Nothing Then
                Return Nothing
            End If

            Return obtenerLaInstanciaContenedoraDeProp(miObjeto, pNombreProp.Substring(partes(0).Length + 1))


        Else
            Return pInstancia
        End If
    End Function

    Public Property ValorObjetivo() As Object
        Get
            If Me.EsPropiedadEncadenada Then
                Return Me.ValueTipoRepresentado
            Else

                Return Me.Value
            End If
        End Get
        Set(ByVal value As Object)

            If Me.EsPropiedadEncadenada Then
                Me.ValueTipoRepresentado = value
            Else

                Me.Value = value
            End If

        End Set
    End Property


    Public Property ValueTipoRepresentado() As Object
        Get
            If Me.mInstanciaVinc.DN Is Nothing Then
                Return Nothing
            Else
                Return Me.mPiTipoRepresentado.GetValue(Me.mInstanciaVinc.DN, Nothing)

            End If
        End Get
        Set(ByVal value As Object)

            If Not Me.mMap.EsReadOnly Then
                mPiTipoRepresentado.SetValue(Me.mInstanciaVinc.DN, value, Nothing)
            End If

        End Set
    End Property



    Public Property Value() As Object
        Get
            Dim miobjeto As Object = obtenerLaInstanciaContenedoraDeProp(Me.mInstanciaVinc.DN, Me.mMap.NombreProp)
            If miobjeto Is Nothing Then
                Return Nothing
            Else
                Return mPi.GetValue(miobjeto, Nothing)
            End If

        End Get
        Set(ByVal value As Object)

            If ValorAsignable(value) Then
                'Debug.WriteLine(mPi.Name)
                Dim miobjeto As Object = obtenerLaInstanciaContenedoraDeProp(Me.mInstanciaVinc.DN, Me.mMap.NombreProp)
                If mPi.PropertyType Is GetType(DateTime) Then

                    Dim fecha As Date = Date.Parse(value)

                    mPi.SetValue(miobjeto, fecha, Nothing)

                Else
                    If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsRef(mPi.PropertyType) AndAlso Not (value Is Nothing OrElse Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsRef(value.GetType)) Then
                        'Debug.WriteLine("no sasignar tipo por valor a tipo por referencia")
                    Else




                        If mPi.PropertyType Is GetType(Single) Then
                            If mPi.GetSetMethod(True) IsNot Nothing Then
                                mPi.SetValue(miobjeto, CType(value, Single), Nothing)
                            End If

                        ElseIf mPi.PropertyType Is GetType(Double) Then
                            If mPi.GetSetMethod(True) IsNot Nothing Then
                                mPi.SetValue(miobjeto, CType(value, Double), Nothing)
                            End If

                        ElseIf mPi.PropertyType Is GetType(Boolean) Then
                            If mPi.GetSetMethod(True) IsNot Nothing Then
                                mPi.SetValue(miobjeto, CBool(value), Nothing)
                            End If

                        ElseIf mPi.PropertyType Is GetType(Int16) Then
                            If mPi.GetSetMethod(True) IsNot Nothing Then
                                mPi.SetValue(miobjeto, CType(value, Int16), Nothing)
                            End If

                        ElseIf mPi.PropertyType Is GetType(Int32) Then
                            If mPi.GetSetMethod(True) IsNot Nothing Then
                                mPi.SetValue(miobjeto, CType(value, Int32), Nothing)
                            End If

                        ElseIf mPi.PropertyType Is GetType(Int64) Then
                            If mPi.GetSetMethod(True) IsNot Nothing Then
                                mPi.SetValue(miobjeto, CType(value, Int64), Nothing)
                            End If

                        Else
                            If mPi.GetSetMethod(True) IsNot Nothing Then
                                mPi.SetValue(miobjeto, value, Nothing)
                            End If

                        End If


                    End If
                End If

                '  mPi.SetValue(miobjeto, value, Nothing)
            End If

        End Set
    End Property

    Private Function ValorAsignable(ByVal pvalue As Object) As Boolean

        If Me.mInstanciaVinc.DN Is Nothing Then
            Return False
        End If


        Dim miobjeto As Object = obtenerLaInstanciaContenedoraDeProp(Me.mInstanciaVinc.DN, Me.mMap.NombreProp)

        If miobjeto Is Nothing Then
            Return False
        End If

        If pvalue Is Nothing OrElse Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsRef(pvalue.GetType) Then

            If pvalue Is Nothing Then
                Return Me.mMap.EsEliminable AndAlso Not Me.mMap.EsReadOnly

            Else
                Return (Me.mMap.EsBuscable OrElse Me.mMap.Instanciable) AndAlso Not Me.mMap.EsReadOnly

            End If

        Else
            ' es tun tipo por vaor
            Return Me.mMap.Editable AndAlso Not Me.mMap.EsReadOnly

        End If



    End Function


    Public ReadOnly Property Correcta() As Boolean
        Get

            If Me.mMap.Virtual Then
                Return True
            Else
                Return Not mPi Is Nothing
            End If


        End Get

    End Property
    Public ReadOnly Property Map() As PropMapDN
        Get
            Return Me.mMap
        End Get
    End Property

    Public ReadOnly Property Vinculada() As Boolean
        Get
            Return Me.mInstanciaVinc.Vinculada AndAlso Correcta
        End Get
    End Property
    Public ReadOnly Property InstanciaVinc() As InstanciaVinc
        Get
            Return Me.mInstanciaVinc
        End Get
    End Property

    Public ReadOnly Property TipoFijadoColPropiedad() As System.Type
        Get
            Return Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(Me.mPi.PropertyType, Nothing)
        End Get

    End Property
    Public ReadOnly Property TipoPropiedad() As System.Type
        Get
            Return Me.mPi.PropertyType
        End Get

    End Property


    Public ReadOnly Property EsTipoPorReferencia() As Boolean
        Get
            ' Return False
            Return Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsRef(TipoPropiedad)
        End Get
    End Property

    Public ReadOnly Property EsColeccion() As Boolean
        Get
            Return Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsColeccion(TipoPropiedad)
        End Get
    End Property

    Public ReadOnly Property ElementoMap() As ElementoMapDN Implements IVincElemento.ElementoMap
        Get
            Return Me.Map
        End Get
    End Property

    Private ReadOnly Property InstanciaVinc1() As InstanciaVinc Implements IVincElemento.InstanciaVinc
        Get
            Return Me.InstanciaVinc
        End Get
    End Property



    Public Function TipoRepresentado() As System.Type

        ' es la primera propeidad que es referida en el nombre propiedad

        Return Me.mPiTipoRepresentado.PropertyType

    End Function

    Public Function RepresentaTipoPorReferencia() As Boolean


        Return Not Me.mPiTipoRepresentado Is Nothing



    End Function


    Public ReadOnly Property Eseditable() As Boolean Implements IVincElemento.Eseditable
        Get
            If Me.mPi IsNot Nothing AndAlso Not Me.mPi.CanWrite Then
                Return False
            End If

            Return Not Me.mMap.EsReadOnly AndAlso Me.mMap.Editable AndAlso Me.Vinculada AndAlso Me.mInstanciaVinc.Eseditable
        End Get
    End Property
End Class


Public Class ColPropVinc
    Inherits List(Of PropVinc)
    Public Function RecuperarxNombreProp(ByVal pNombreProp As String) As PropVinc

        For Each pv As PropVinc In Me
            If pv.Map.NombreProp = pNombreProp Then
                Return pv
            End If
        Next

        Return Nothing
    End Function
End Class