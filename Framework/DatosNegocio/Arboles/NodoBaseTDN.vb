Namespace Arboles
    <Serializable()> _
    Public Class NodoBaseTDN(Of T As {IEntidadBaseDN, Class})
        Inherits EntidadDN
        Implements INodoTDN(Of T)

        Implements IValidable
        Implements INodoDN



        Public Sub New()
            Me.CambiarValorCol(New ColINodoTDN(Of T), mHijos)
            ' Me.CambiarValorCol(New ColNodoBaseTDN(Of T), mHijos)
            Me.CambiarValorCol(New ArrayListValidable(Of T), mColHojas)
            Me.modificarEstado = EstadoDatosDN.SinModificar
        End Sub

#Region "Atributos"

        Protected mColHojas As ArrayListValidable(Of T)

        Protected mHijos As ColINodoTDN(Of T)
        ' Protected mHijos As ColNodoBaseTDN(Of T)
        Protected mPadre As INodoTDN(Of T)
        'Protected mPadre As NodoBaseDN

        Protected mValidadorp As IValidador
        Protected mValidadorh As IValidador
#End Region

#Region "Propeidades"

        Public Shared Function GetColHojasVacia() As ArrayListValidable(Of T)
            Return New ArrayListValidable(Of T)
        End Function
        Public Property ColHojas() As ArrayListValidable(Of T)
            Get
                Return Me.mColHojas
            End Get
            Set(ByVal value As ArrayListValidable(Of T))
                Me.CambiarValorCol(value, Me.mColHojas)
            End Set
        End Property

        Public Property HijosNcH() As ColINodoConHijosTDN(Of T) Implements INodoConHijosTDN(Of T).HijosNcH
            Get
                ' ojo devuelve una isntancia nueva no la misma
                Dim miHijosNcH As ColINodoConHijosTDN(Of T)
                miHijosNcH = New ColINodoConHijosTDN(Of T)
                miHijosNcH.AddRange(Me.Hijos)
                Return miHijosNcH

            End Get
            Set(ByVal value As ColINodoConHijosTDN(Of T))
                Me.Hijos.AddRange(value)
            End Set
        End Property
        Public Property PadreNcP() As INodoConPadreTDN(Of T) Implements INodoConPadreTDN(Of T).PadreNcP
            Get
                Return Me.mPadre
            End Get
            Set(ByVal value As INodoConPadreTDN(Of T))
                ' Me.CambiarValorRef(Of T)(value, value)
                Me.Padre = value
            End Set
        End Property
        Public Property Hijos() As IArrayListValidable(Of INodoTDN(Of T)) Implements INodoTDN(Of T).Hijos

            Get
                Return mHijos
            End Get

            Set(ByVal value As IArrayListValidable(Of INodoTDN(Of T)))
                Dim mensaje As String = String.Empty
                If (Not mValidadorh Is Nothing AndAlso mValidadorh.Validacion(mensaje, value) = False) Then
                    Throw New ApplicationException(mensaje)

                Else
                    '   Me.CambiarValorCol(Of ColNodoBaseTDN(Of T))(value, mHijos)
                    Me.CambiarValorCol(Of ColINodoTDN(Of T))(value, mHijos)
                    'mHijos = Value
                End If
            End Set



        End Property
        Public Property Padre() As INodoTDN(Of T) Implements INodoTDN(Of T).Padre


            Get
                Return mPadre
            End Get
            Set(ByVal value As INodoTDN(Of T))

                Me.CambiarValorRef(value, mPadre)


                If (Not value Is Nothing) Then

                    'Registrarme como su hijo si no me contine ya en su coleccion
                    If Not mPadre.Hijos.Contiene(Me, CoincidenciaBusquedaEntidadDN.MismaRef) Then
                        mPadre.AñadirHijo(Me)
                    End If

                End If
            End Set

        End Property
        Public ReadOnly Property Ruta() As String
            Get
                Dim nodo As INodoTDN(Of T)
                Dim rutaTotal As String

                nodo = Me

                rutaTotal = nodo.Nombre
                nodo = nodo.Padre

                Do While Not nodo Is Nothing
                    rutaTotal = nodo.Nombre & " / " & rutaTotal
                    nodo = nodo.Padre
                Loop

                Return rutaTotal
            End Get
        End Property
        Public ReadOnly Property ValidadorH() As IValidador Implements IValidable.Validador
            Get
                Return Me.mValidadorh
            End Get
        End Property

#End Region

        ''' <summary>
        ''' si me añaden un hijo yo debo ser su padre, este metodo lo asegura
        ''' </summary>
        ''' <param name="pSender"></param>
        ''' <param name="pElemento"></param>
        ''' <remarks></remarks>
        Public Overrides Sub ElementoAñadido(ByVal pSender As Object, ByVal pElemento As Object)
            If mHijos Is pSender Then
                Dim tipoHijo As NodoBaseTDN(Of T)
                tipoHijo = pElemento
                If Not tipoHijo.Padre Is Me Then
                    tipoHijo.Padre = Me
                End If
            End If
            MyBase.ElementoAñadido(pSender, pElemento)
        End Sub

        ''' <summary>
        ''' si me quitan un hijo yo no debo ser su padre, este metodo lo asegura
        ''' </summary>
        ''' <param name="pSender"></param>
        ''' <param name="pElemento"></param>
        ''' <remarks></remarks>
        Public Overrides Sub ElementoEliminado(ByVal pSender As Object, ByVal pElemento As Object)
            If mHijos Is pSender Then
                Dim tipoHijo As NodoBaseTDN(Of T)
                tipoHijo = pElemento
                If tipoHijo.Padre Is Me Then
                    tipoHijo.Padre = Nothing
                End If
            End If
            MyBase.ElementoEliminado(pSender, pElemento)
        End Sub

#Region "Metodos"

#Region "Busqueda"


        ''' <summary>
        ''' este metodo devuelve true si la instancia o un clon suyo esta referido directamene por el nodo actual
        ''' </summary>
        ''' <param name="pElemento"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function ContenidoEnNodo(ByVal pElemento As Object, ByRef pCoincidencia As CoincidenciaBusquedaEntidadDN) As Boolean

            If TypeOf pElemento Is T Then
                If Me.mColHojas.Contiene(pElemento, pCoincidencia) Then
                    Return True
                End If
            End If


            If TypeOf pElemento Is INodoTDN(Of T) Then
                If Me.mHijos.Contiene(pElemento, pCoincidencia) Then
                    Return True
                End If
            End If




        End Function


        ''' <summary>
        ''' este metodo devuelve true si la instancia o un clon suyo esta referido directamene por el nodo actual o uno de sus sub nodos
        ''' </summary>
        ''' <param name="pElemento"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function Contenido(ByVal pElemento As Object, ByRef pCoincidencia As CoincidenciaBusquedaEntidadDN) As Boolean

            If Me.ContenidoEnNodo(pElemento, pCoincidencia) Then
                Return True
            End If


            Dim subnodo As NodoBaseTDN(Of T)

            For Each subnodo In Me.mHijos
                If subnodo.Contenido(pElemento, pCoincidencia) Then
                    Return True
                End If
            Next

            Return False

        End Function


        ''' <summary>
        ''' este metodo devuelve true si la instancia o un clon suyo esta referido directamene por el arbol actual o uno de sus sub nodos
        ''' </summary>
        ''' <param name="pEntidad"></param>
        ''' <param name="pCoincidencia"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function ContenidoEnArbol(ByVal pEntidad As Object, ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As Boolean Implements INodoTDN(Of T).ContenidoEnArbol

            Dim nodo As NodoBaseTDN(Of T)

            nodo = Me.RaizArbol

            Return nodo.Contenido(pEntidad, pCoincidencia)


        End Function


        ''' <summary>
        ''' busca un objeto contenido en la intacia o en sus hijos
        ''' </summary>
        ''' <param name="phijo"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function ContieneHijo(ByVal phijo As Object, ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As Boolean Implements INodoConHijosTDN(Of T).Contiene
            'Dim miHijo As INodoTDN(Of T)
            'miHijo=phijo
            Return Contenido(phijo, pCoincidencia)
        End Function


        ''' <summary>
        ''' devuelve en nodo  que contine un nodo que es hijo de el nodo sobre el que se invoca el metodo o de alguno de sus nodos hijos
        ''' </summary>
        ''' <param name="phijo"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function NodoContenedorPorHijos(ByVal phijo As Object, ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As INodoConHijosTDN(Of T) Implements INodoConHijosTDN(Of T).NodoContenedor

            'TODO: ALEX - comprobar que este método funcion (throw comentado by luis)

            'Throw New NotImplementedException

            ' ojo esta linea puede dar resultados no esperados
            ' supone que silo que busca implementa INodoConPadreTDN(Of T) no es un T y portanto una "hoja"
            'If TypeOf phijo Is INodoConPadreTDN(Of T) Then
            '    Return Me.NodoContenedor(phijo)
            'End If


            ' si es un hoja directa y lo contenogo yo
            If Me.mColHojas.Contiene(phijo, pCoincidencia) Then
                Return Me
            End If

            ' si es un hijo directo y lo contenogo yo
            If Me.mHijos.Contiene(phijo, pCoincidencia) Then
                Return Me
            End If

            ' sino lo tendrá alguno de mos hijos
            Dim miHijo As NodoBaseTDN(Of T)
            For Each miHijo In Me.mHijos
                Dim miNodoContenedor As INodoConHijosTDN(Of T)
                miNodoContenedor = miHijo.NodoContenedorPorHijos(phijo, pCoincidencia)
                If miNodoContenedor IsNot Nothing Then
                    Return miNodoContenedor
                End If
            Next

            Return Nothing


        End Function

        ''' <summary>
        ''' devuelve en nodo  que contine un nodo que es hijo de el nodo sobre el que se invoca el metodo o de alguno de sus nodos hijos
        ''' la busqueda la realiza subiendo por la propeidad padre
        ''' </summary>
        ''' <param name="phijo"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function NodoContenedor(ByVal phijo As INodoConPadreTDN(Of T)) As INodoConPadreTDN(Of T) Implements INodoConPadreTDN(Of T).NodoContenedor


            If (phijo.PadreNcP Is Nothing) Then
                Return Nothing
            End If

            If (phijo.PadreNcP Is Me) Then
                Return Me

            Else
                Return NodoContenedor(phijo.PadreNcP)
            End If
        End Function



        Public Function RaizArbol() As INodoConPadreTDN(Of T) Implements INodoConPadreTDN(Of T).RaizArbol
            Dim p As INodoTDN(Of T)

            p = Me

            Do While Not p.Padre Is Nothing
                p = p.Padre
            Loop

            Return p
        End Function

        Public Function RecuperarColHojasConenidas() As ArrayListValidable(Of T) Implements INodoTDN(Of T).RecuperarColHojasConenidas
            Dim col As New ArrayListValidable(Of T)

            col.AddRange(Me.mColHojas)

            Dim miHijo As INodoTDN(Of T)


            For Each miHijo In Me.mHijos

                col.AddRange(miHijo.RecuperarColHojasConenidas)
            Next

            Return col

        End Function


#End Region

#Region "Adicion Eliminacion"


        Public Function PodarNodosHijosNoContenedoresHojas(ByVal phojas As ArrayListValidable(Of T), ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As ColINodoConHijosTDN(Of T)
            Dim colNodosContenedores As New ColINodoConHijosTDN(Of T)
            Dim mihoja As T
            Dim nh As INodoConHijosTDN(Of T)



            For Each mihoja In phojas
                nh = Me.NodoContenedorPorHijos(mihoja, pCoincidencia)
                If Not nh Is Nothing Then
                    colNodosContenedores.AddUnico(nh)
                End If
            Next

            Return PodarNodosHijosyHojasNoContenedoresHijos(phojas, colNodosContenedores, pCoincidencia)

        End Function

        Private Function PodarNodosHijosyHojasNoContenedoresHijos(ByVal phojas As ArrayListValidable(Of T), ByVal phijos As ColINodoConHijosTDN(Of T), ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As ColINodoConHijosTDN(Of T)
            Dim nh, nhb As INodoConHijosTDN(Of T)

            Dim nodosaElimar As New ColINodoConHijosTDN(Of T)
            Dim nodosEliminados As New ColINodoConHijosTDN(Of T)


            For Each nh In Me.mHijos
                Debug.WriteLine(nh.Nombre)

                Dim nodoSalvado As Boolean = False



                For Each nhb In phijos
                    Debug.WriteLine(nhb.Nombre)

                    If nh.GUID = nhb.GUID Then
                        nodoSalvado = True
                        nodosEliminados.AddRange(nh.PodarNodosHijosNoContenedoresHijos(phijos, pCoincidencia))
                        Exit For

                    ElseIf nh.Contiene(nhb, pCoincidencia) Then
                        ' el nodo no debe ser podado pero a lo mejor alguno de sus hijos si
                        nodoSalvado = True
                        nodosEliminados.AddRange(nh.PodarNodosHijosNoContenedoresHijos(phijos, pCoincidencia))
                        Exit For
                    End If
                Next

                If nodoSalvado Then
                    ' el nodo se salva pero eliminar las hojas no contenidas
                    If TypeOf nh Is NodoBaseTDN(Of T) Then
                        Dim nht As NodoBaseTDN(Of T)
                        nht = nh
                        nht.PodarHojasEnNodoMenos(phojas, pCoincidencia)
                    End If


                Else
                    ' podar el nodo
                    nodosaElimar.Add(nh)

                End If
            Next
            For Each nh In nodosaElimar
                Me.EliminarHijo(nh)
            Next

            nodosEliminados.AddRange(nodosaElimar)
            Return nodosEliminados
        End Function

        Public Function PodarNodosHijosNoContenedoresHijos(ByVal phijos As ColINodoConHijosTDN(Of T), ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As ColINodoConHijosTDN(Of T) Implements INodoTDN(Of T).PodarNodosHijosNoContenedoresHijos


            Return PodarNodosHijosyHojasNoContenedoresHijos(Nothing, phijos, pCoincidencia)


        End Function


        Public Function PodarHojasEnNodoMenos(ByVal hojasRespetadas As ArrayListValidable(Of T), ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As Generic.IList(Of T)
            Dim mihoja As T
            Dim hojasaEliminar As New Generic.List(Of T)
            If Not hojasRespetadas Is Nothing Then
                For Each mihoja In Me.mColHojas
                    If Not hojasRespetadas.Contiene(mihoja, pCoincidencia) Then
                        hojasaEliminar.Add(mihoja)
                    End If

                Next
                hojasaEliminar.AddRange(Me.mColHojas.EliminarEntidadDN(hojasaEliminar, pCoincidencia))
            End If
            Return hojasaEliminar

        End Function


        Public Sub AñadirHijo(ByVal phijos As ColINodoConHijosTDN(Of T)) Implements INodoConHijosTDN(Of T).AñadirHijo
            Dim nh As INodoTDN(Of T)

            If (Not phijos Is Nothing) Then
                For Each nh In phijos
                    Me.AñadirHijo(nh)
                Next
            End If
        End Sub

        Public Sub AñadirHijo(ByVal phijo As INodoConHijosTDN(Of T)) Implements INodoConHijosTDN(Of T).AñadirHijo

            '   Throw New NotImplementedException

            Dim mensaje As String = String.Empty

            ' tine que ser necesario porque yo solo acepto como hijos   NodoBaseTDN(Of T)
            Dim mihijo As INodoTDN(Of T)
            mihijo = phijo

            'TODO: ALEX-->ALEX ojo has cambiado Contiene por ContenidoEnArbol
            'If Not Me.ContenidoEnArbol(phijo) Then 'Esto asegura que se establece una estructura arborea y no un grafo
            If Not mValidadorh Is Nothing AndAlso mValidadorh.Validacion(mensaje, phijo) = False Then
                Throw New ApplicationException("Error: error de validacion")

            Else

                Me.Hijos.Add(phijo)
                mihijo.Padre = Me
                Me.RegistrarParte(phijo)
                Me.modificarEstado = EstadoDatosDN.Modificado

            End If
            'End If
        End Sub


        ''' <summary>
        '''          elimina todas las referencias a la instancia o clon de intancia pasada en el nodo o en cualquiera de sus hijos
        ''' </summary>
        ''' <param name="pElemetno"></param>
        ''' <remarks></remarks>
        Public Function Eliminar(ByVal pElemetno As Object, ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As ArrayList Implements INodoConHijosTDN(Of T).Eliminar
            'Throw New NotImplementedException

            'If TypeOf pElemetno Is T Then
            '    Throw New NotImplementedException
            '    Me.mColHojas.Remove(pElemetno)
            'End If


            'If TypeOf pElemetno Is INodoTDN(Of T) Then
            '    Dim miHijo As INodoTDN(Of T)
            '    miHijo = pElemetno

            '    If (Not miHijo.Padre Is Nothing) Then
            '        miHijo.Padre.Hijos.Remove(pElemetno)
            '        miHijo.Padre = Nothing
            '        Return pElemetno
            '    End If
            'End If

            Dim al As New ArrayList
            al.AddRange(Me.EliminarEnNodo(pElemetno, pCoincidencia))


            Dim nodohijo As INodoTDN(Of T)
            For Each nodohijo In Me.mHijos
                al.AddRange(nodohijo.Eliminar(pElemetno, pCoincidencia))
            Next


            Return al
        End Function

        ''' <summary>
        ''' elimina todas las intacia del elemeto en cualquier parte del arbol o sus clones
        ''' </summary>
        ''' <param name="pElemetno"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function EliminarEnArbol(ByVal pElemetno As Object, ByVal pEliminarClones As Boolean) As ArrayList
            Throw New NotImplementedException

            'If TypeOf pElemetno Is T Then
            '    Me.mColHojas.Remove(pElemetno)
            'End If


            'If TypeOf pElemetno Is INodoTDN(Of T) Then
            '    Dim miHijo As INodoTDN(Of T)
            '    miHijo = pElemetno

            '    If (Not miHijo.Padre Is Nothing) Then
            '        miHijo.Padre.Hijos.Remove(pElemetno)
            '        miHijo.Padre = Nothing
            '        Return pElemetno
            '    End If
            'End If

            Dim al As New ArrayList
            Dim nodoRaiz As INodoTDN(Of T)

            nodoRaiz = Me.RaizArbol
            al.AddRange(nodoRaiz.Eliminar(pElemetno, pEliminarClones))



            Return al
        End Function


        ''' <summary>
        '''  elimina  todas las referencias a un objeto dado o sus clones, referidos directamente por este nodo (col hijos o de hojas)
        '''  para el caso del padre no sera referido
        ''' </summary>
        ''' <param name="pElemetno"></param>
        ''' <returns>devulve una arralist de objetos eliminados</returns>
        ''' <remarks></remarks>
        Public Function EliminarEnNodo(ByVal pElemetno As Object, ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As ArrayList



            Dim al As New ArrayList

            If TypeOf pElemetno Is T Then

                al.AddRange(Me.mColHojas.EliminarEntidadDN(pElemetno, pCoincidencia))
            End If



            If TypeOf pElemetno Is INodoTDN(Of T) Then
                Dim miHijo As INodoTDN(Of T)
                miHijo = pElemetno

                al.AddRange(Me.mHijos.EliminarEntidadDN(pElemetno, pCoincidencia))

            End If




            Return al
        End Function

#End Region

#Region "Caracteristicas"
        Public Function ProfundidadMaxDescendenia() As Short Implements INodoConHijosTDN(Of T).ProfundidadMaxDescendenia
            Return ProfundidadMaxDescendeniap(0, Me)
        End Function

        Private Function ProfundidadMaxDescendeniap(ByVal pPprof As Int64, ByVal pNodo As INodoTDN(Of T)) As Int16
            Dim profmax, proaux As Int64
            Dim hijo As INodoTDN(Of T)

            If (pNodo.Hijos Is Nothing OrElse pNodo.Hijos.Count = 0) Then
                Return pPprof

            Else
                For Each hijo In pNodo.Hijos
                    proaux = ProfundidadMaxDescendeniap(pPprof + 1, hijo)

                    If (profmax < proaux) Then
                        profmax = proaux
                    End If
                Next
            End If

            Return profmax
        End Function

        Public Function Profundidad() As Long Implements INodoConPadreTDN(Of T).Profundidad
            Dim p As INodoTDN(Of T)
            Dim prof As Int64

            p = Me

            Do While Not p.Padre Is Nothing
                p = p.Padre
                prof += 1
            Loop

            Return prof
        End Function


        Public Function ProfundidadMaximaArbol() As Long Implements INodoTDN(Of T).ProfundidadMaximaArbol

            Dim prof As Int64
            Dim p As INodoTDN(Of T)

            '1º encontrar a mi padre
            p = RaizArbol()

            'Incrementar por la pofundidad de mis hijos
            prof = p.ProfundidadMaxDescendenia

            Return prof
        End Function
#End Region


#End Region

#Region "INODO"


        Private Function ValidacionIdentica(ByVal pValidador As IValidador) As Boolean Implements IValidable.ValidacionIdentica
            Return Me.ValidadorH.Formula = pValidador.Formula
        End Function





        Private Sub AñadirHijo1(ByVal hijos As ColNodosDN) Implements INodoConHijosDN.AñadirHijo
            Me.AñadirHijo(hijos)
        End Sub

        Private Sub AñadirHijo1(ByVal hijo As INodoDN) Implements INodoConHijosDN.AñadirHijo
            Me.AñadirHijo(CType(hijo, T))
        End Sub

        Private Function ContieneHijo1(ByVal hijo As INodoDN) As Boolean Implements INodoConHijosDN.ContieneHijo
            Return Me.ContieneHijo(hijo, CoincidenciaBusquedaEntidadDN.Todos)
        End Function

        Private Function EliminarHijo(ByVal hijo As INodoDN) As IList Implements INodoConHijosDN.EliminarHijo
            Return Me.EliminarEnNodo(hijo, CoincidenciaBusquedaEntidadDN.Todos)
        End Function

        Private Property HijosNcH1() As IColNodos Implements INodoConHijosDN.HijosNcH
            Get
                Return HijosNcH
            End Get
            Set(ByVal value As IColNodos)
                HijosNcH = value
            End Set
        End Property

        Private Function NodoContenedorHijo(ByVal hijo As INodoDN) As INodoDN Implements INodoConHijosDN.NodoContenedorHijo

        End Function

        Private Function ProfundidadMaxDescendenia1() As Short Implements INodoConHijosDN.ProfundidadMaxDescendenia
            Return ProfundidadMaxDescendenia()
        End Function

        Private Property PadreNcP1() As INodoConPadreDN Implements INodoConPadreDN.PadreNcP
            Get
                Return PadreNcP
            End Get
            Set(ByVal value As INodoConPadreDN)
                PadreNcP = value
            End Set
        End Property

        Private Function Profundidad1() As Long Implements INodoConPadreDN.Profundidad
            Return Profundidad()
        End Function

        Private Function RaizArbol1() As INodoDN Implements INodoConPadreDN.RaizArbol
            Return RaizArbol()
        End Function

        Private Property ColHojas1() As System.Collections.IList Implements INodoDN.ColHojas
            Get
                Return ColHojas
            End Get
            Set(ByVal value As System.Collections.IList)
                ColHojas = value
            End Set
        End Property

        Private Function ContenidoEnArbol1(ByVal hijo As INodoDN) As Boolean Implements INodoDN.ContenidoEnArbol
            Return Me.ContenidoEnArbol(hijo, CoincidenciaBusquedaEntidadDN.Todos)
        End Function

        Private Property Hijos1() As IColNodos Implements INodoDN.Hijos
            Get
                Return Me.Hijos
            End Get
            Set(ByVal value As IColNodos)
                Me.Hijos = value
            End Set
        End Property

        Private Property Padre1() As INodoDN Implements INodoDN.Padre
            Get
                Return Me.Padre
            End Get
            Set(ByVal value As INodoDN)
                Me.Padre = value
            End Set
        End Property

        Private Function ProfundidadMaximaArbol1() As Long Implements INodoDN.ProfundidadMaximaArbol
            Me.ProfundidadMaximaArbol()
        End Function

#End Region


        Public Function RecuperarNodoXGUID(ByVal pGUID As String) As INodoDN Implements INodoConHijosDN.RecuperarNodoXGUID
            If Me.GUID = pGUID Then
                Return Me
            End If



            Dim nodo, nodoSelecioando As INodoDN


            For Each nodo In Me.mHijos

                nodoSelecioando = nodo.RecuperarNodoXGUID(pGUID)
                If nodoSelecioando IsNot Nothing Then
                    Return nodoSelecioando
                End If

            Next


            Return Nothing
        End Function
    End Class

    <Serializable()> _
    Public Class ColNodoBaseTDN(Of T As {IEntidadDN, Class})
        Inherits Framework.DatosNegocio.Arboles.ColINodoTDN(Of NodoBaseTDN(Of T))

        Public Overridable Function RecuperarColHojasContenidas() As IList



            Return RecuperarColHojasContenidas(New ArrayList)
        End Function

        Protected Overridable Function RecuperarColHojasContenidas(ByVal pCol As IColDn) As IColDn

            Dim elemento As INodoTDN(Of T)

            For Each elemento In Me

                pCol.AddRange(elemento.RecuperarColHojasConenidas)

            Next

            Return pCol
        End Function

    End Class

End Namespace
