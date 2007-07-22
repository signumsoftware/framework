<Serializable()> _
Public Class AgrupacionMapDN
    Inherits ElementoMapDN
    Implements IElemtoMapContenedor




    Protected mTipoAgrupacionMap As TipoAgrupacionMapDN
    Protected mColPropMap As ColPropMapDN
    Protected mColAgrupacionMap As ColAgrupacionMapDN
    Protected mInstanciaContenedora As InstanciaMapDN


    Public Function AgruparCon(ByVal pAgrupacionMap As AgrupacionMapDN) As String


        If pAgrupacionMap.TipoAgrupacionMap.Nombre = MV2DN.TiposAgrupacionMap.Elemento.ToString Then
            ' el sera contenido


            If Me.TipoAgrupacionMap.Nombre = MV2DN.TiposAgrupacionMap.Elemento.ToString Or Me.TipoAgrupacionMap.Nombre = "Basica" Then

            Else
                pAgrupacionMap.InstanciaContenedora = Me.InstanciaContenedora
                Me.ColAgrupacionMap.Add(pAgrupacionMap)

            End If


        Else
            ' será un elemento contendor de me
            Dim elemento As ElementoMapDN

            pAgrupacionMap.InstanciaContenedora = Me.InstanciaContenedora
            elemento = Me.Contenedor
            Me.InstanciaContenedora.EliminarElementoMap(Me) 'eliminacion recursiva


            If TypeOf elemento Is InstanciaMapDN Then
                Dim imap As InstanciaMapDN = elemento
                imap.ColAgrupacionMap.Add(pAgrupacionMap)
            Else
                Dim agmap As AgrupacionMapDN = elemento
                If agmap Is Nothing Then
                    agmap = Me.InstanciaContenedora.ColAgrupacionMap.Item(0)
                End If
                agmap.ColAgrupacionMap.Add(pAgrupacionMap)
            End If

            pAgrupacionMap.ColAgrupacionMap.Add(Me)




        End If



    End Function

    Public Function Contenedor() As IElemtoMap
        Return Me.InstanciaContenedora.ElementoContenedor(Me)
    End Function

    Public Sub New()
        Me.mNombreVis = Now.Ticks.ToString
        Me.CambiarValorRef(Of ColPropMapDN)(New ColPropMapDN, mColPropMap)
        Me.CambiarValorRef(Of ColAgrupacionMapDN)(New ColAgrupacionMapDN, mColAgrupacionMap)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar

    End Sub

    Public Sub New(ByVal pInstanciaContenedora As InstanciaMapDN, ByVal pTipoAgrupacionMap As TipoAgrupacionMapDN)

        Me.CambiarValorRef(Of ColPropMapDN)(New ColPropMapDN, mColPropMap)
        Me.CambiarValorRef(Of ColAgrupacionMapDN)(New ColAgrupacionMapDN, mColAgrupacionMap)
        Me.CambiarValorRef(Of TipoAgrupacionMapDN)(pTipoAgrupacionMap, mTipoAgrupacionMap)
        Me.CambiarValorRef(Of InstanciaMapDN)(pInstanciaContenedora, mInstanciaContenedora)

        mInstanciaContenedora.ColAgrupacionMap.Add(Me)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub

    Public Sub New(ByVal pAgrupacionContenedora As AgrupacionMapDN, ByVal pTipoAgrupacionMap As TipoAgrupacionMapDN)

        Me.CambiarValorRef(Of ColPropMapDN)(New ColPropMapDN, mColPropMap)
        Me.CambiarValorRef(Of ColAgrupacionMapDN)(New ColAgrupacionMapDN, mColAgrupacionMap)
        Me.CambiarValorRef(Of TipoAgrupacionMapDN)(pTipoAgrupacionMap, mTipoAgrupacionMap)
        Me.CambiarValorRef(Of InstanciaMapDN)(pAgrupacionContenedora.InstanciaContenedora, mInstanciaContenedora)


        pAgrupacionContenedora.ColAgrupacionMap.Add(Me)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub
    Public Property InstanciaContenedora() As InstanciaMapDN
        Get
            Return Me.mInstanciaContenedora
        End Get
        Set(ByVal value As InstanciaMapDN)
            Me.CambiarValorRef(Of InstanciaMapDN)(value, mInstanciaContenedora)
            ActualizarHijas()

        End Set
    End Property


    Private Sub ActualizarHijas()
        For Each ag As AgrupacionMapDN In Me.mColAgrupacionMap
            ag.InstanciaContenedora = Me.mInstanciaContenedora
        Next
    End Sub



    Public Function PosicionElementoMap(ByVal pIElemtoMap As IElemtoMap) As Integer



        If TypeOf pIElemtoMap Is ElementoMapDN Then

            Return Me.mColPropMap.IndexOf(pIElemtoMap)


        End If

        If TypeOf pIElemtoMap Is AgrupacionMapDN Then

            Return Me.mColAgrupacionMap.IndexOf(pIElemtoMap)


        End If


    End Function
    Public Sub MoverNElementoMap(ByVal pN As Integer, ByVal pElementoMap As ElementoMapDN)

        Dim ag As AgrupacionMapDN = ElementoContenedor(pElementoMap)


        If TypeOf pElementoMap Is AgrupacionMapDN Then
            Dim posicion As Int16
            posicion = ag.ColAgrupacionMap.IndexOf(pElementoMap)
            ag.ColAgrupacionMap.RemoveAt(posicion)
            ag.ColAgrupacionMap.Insert(posicion + pN, pElementoMap)

        End If

        If TypeOf pElementoMap Is PropMapDN Then

            Dim posicion As Int16
            posicion = ag.ColPropMap.IndexOf(pElementoMap)
            ag.ColPropMap.RemoveAt(posicion)
            ag.ColPropMap.Insert(posicion + pN, pElementoMap)

        End If



    End Sub
    Public Function EliminarElementoMap(ByVal pElementoMap As IElemtoMap) As Boolean Implements IElemtoMapContenedor.EliminarElementoMap



        Dim ag As AgrupacionMapDN = ElementoContenedor(pElementoMap)

        If ag Is Nothing Then
            Return Nothing

        Else
            If TypeOf pElementoMap Is AgrupacionMapDN Then
                Return (ag.ColAgrupacionMap.Remove(pElementoMap))

            End If

            If TypeOf pElementoMap Is PropMapDN Then

                Return ag.ColPropMap.Remove(pElementoMap)

            End If


        End If


        Return Nothing

    End Function
    Public Overrides Function ElementoContenedor(ByVal pElementoMap As ElementoMapDN) As IElemtoMap





        For Each map As PropMapDN In Me.mColPropMap
            If map Is pElementoMap Then
                Return Me
            End If

        Next

        For Each map As AgrupacionMapDN In Me.mColAgrupacionMap
            Dim micontenedor As ElementoMapDN

            micontenedor = map.ElementoContenedor(pElementoMap)
            If Not micontenedor Is Nothing Then
                Return micontenedor
            End If

        Next


        Return Nothing

    End Function

    Public Property ColAgrupacionMap() As ColAgrupacionMapDN
        Get
            Return mColAgrupacionMap
        End Get
        Set(ByVal value As ColAgrupacionMapDN)
            Me.CambiarValorRef(Of ColAgrupacionMapDN)(value, mColAgrupacionMap)

        End Set
    End Property

    Public Property ColPropMap() As ColPropMapDN
        Get
            Return mColPropMap
        End Get
        Set(ByVal value As ColPropMapDN)
            Me.CambiarValorRef(Of ColPropMapDN)(value, mColPropMap)

        End Set
    End Property


    Public Property TipoAgrupacionMap() As TipoAgrupacionMapDN
        Get
            Return mTipoAgrupacionMap
        End Get
        Set(ByVal value As TipoAgrupacionMapDN)
            Me.CambiarValorRef(Of TipoAgrupacionMapDN)(value, mTipoAgrupacionMap)
        End Set
    End Property

    Public Overrides Function ToXML() As String



        Return ToXML(GetType(AgrupacionMapXML))

    End Function
    Public Overrides Function FromXML(ByVal ptr As System.IO.TextReader) As Object
        Return FromXML(GetType(AgrupacionMapXML), ptr)
    End Function

    Public Property ColIElemtoMap() As System.Collections.Generic.List(Of IElemtoMap) Implements IElemtoMapContenedor.ColIElemtoMap
        Get
            ColIElemtoMap = New List(Of IElemtoMap)
            ColIElemtoMap.AddRange(Me.ColPropMap.ToArray)

        End Get
        Set(ByVal value As System.Collections.Generic.List(Of IElemtoMap))

        End Set
    End Property

    Public Property ColIElemtoMapContenedor() As System.Collections.Generic.List(Of IElemtoMapContenedor) Implements IElemtoMapContenedor.ColIElemtoMapContenedor
        Get
            ColIElemtoMapContenedor = New List(Of IElemtoMapContenedor)
            ColIElemtoMapContenedor.AddRange(Me.ColAgrupacionMap.ToArray)

        End Get
        Set(ByVal value As System.Collections.Generic.List(Of IElemtoMapContenedor))

        End Set
    End Property

    Public Property InstanciaContenedora1() As IElemtoMap Implements IElemtoMapContenedor.InstanciaContenedora
        Get

        End Get
        Set(ByVal value As IElemtoMap)

        End Set
    End Property

    Public Function AñadirElementoMap(ByVal pElementoMap As IElemtoMap) As Boolean Implements IElemtoMapContenedor.AñadirElementoMap


        If TypeOf pElementoMap Is AgrupacionMapDN Then
            Me.mColAgrupacionMap.Add(pElementoMap)
        End If


        If TypeOf pElementoMap Is PropMapDN Then
            Me.mColPropMap.Add(pElementoMap)

        End If


    End Function

    Public Function AñadirElementoMapEnRelacion(ByVal pElementoMap As IElemtoMap, ByVal pElementoMapRelacioando As IElemtoMap, ByVal pPosicion As Posicion) As Boolean Implements IElemtoMapContenedor.AñadirElementoMapEnRelacion

        Dim posi As Integer = PosicionElementoMap(pElementoMapRelacioando)
        If pPosicion = Posicion.Despues Then
            posi += 1
        End If


        If TypeOf pElementoMap Is AgrupacionMapDN Then
            Me.mColAgrupacionMap.Insert(posi, pElementoMap)
        End If


        If TypeOf pElementoMap Is PropMapDN Then
            Me.mColPropMap.Insert(posi, pElementoMap)

        End If

    End Function
End Class


Public Class ColAgrupacionMapDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of AgrupacionMapDN)

End Class


Public Class AgrupacionMapXML
    Inherits ElementoMapXML


    Public TipoAgrupacionMap As New TipoAgrupacionMapXML
    Public ColPropMap As New List(Of PropMapXML)
    Public ColAgrupacionMap As New List(Of AgrupacionMapXML)


  

    Public Overrides Sub ObjetoToXMLAdaptador(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN)

        Dim entidad As AgrupacionMapDN = pEntidad
        ' TipoAgrupacionMap = entidad.TipoAgrupacionMap.ID & "-" & entidad.TipoAgrupacionMap.Nombre
        TipoAgrupacionMap = New TipoAgrupacionMapXML
        TipoAgrupacionMap.ObjetoToXMLAdaptador(entidad.TipoAgrupacionMap)
        entidad.ColPropMap.ToListIXMLAdaptador(New PropMapXML, ColPropMap)
        entidad.ColAgrupacionMap.ToListIXMLAdaptador(New AgrupacionMapXML, ColAgrupacionMap)
        MyBase.ObjetoToXMLAdaptador(pEntidad)

    End Sub

    Public Overrides Sub XMLAdaptadorToObjeto(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN)

        MyBase.XMLAdaptadorToObjeto(pEntidad)

        Dim entidad As AgrupacionMapDN = pEntidad

        Dim tag As New TipoAgrupacionMapDN
        entidad.TipoAgrupacionMap = tag
        TipoAgrupacionMap.XMLAdaptadorToObjeto(tag)

        For Each pmxml As PropMapXML In ColPropMap
            Dim pm As New PropMapDN
            pmxml.XMLAdaptadorToObjeto(pm)
            entidad.ColPropMap.Add(pm)
        Next


        For Each AGxml As AgrupacionMapXML In ColAgrupacionMap
            Dim ag As New AgrupacionMapDN
            AGxml.XMLAdaptadorToObjeto(ag)
            entidad.ColAgrupacionMap.Add(ag)
        Next


    End Sub
End Class