Namespace Arboles
    <Serializable()> Public Class NodoBaseDN
        Inherits EntidadDN
        Implements INodoDN


        Implements IValidable

#Region "Atributos"
        Protected mHijos As ColNodosDN
        Protected mPadre As INodoDN
        'Protected mPadre As NodoBaseDN

        Protected mValidadorp As IValidador
        Protected mValidadorh As IValidador
#End Region

#Region "Constructores"
        Public Sub New()
            MyBase.New()
            Me.CambiarValorCol(Of ColNodosDN)(New ColNodosDN, mHijos)
            Me.modificarEstado = EstadoDatosDN.Inconsistente
        End Sub

        Public Sub New(ByVal pId As String, ByVal pNombre As String, ByVal pFechaModificacion As DateTime, ByVal pPadre As INodoDN)
            MyBase.New(pId, pNombre, pFechaModificacion, False)
            Me.CambiarValorCol(Of ColNodosDN)(New ColNodosDN, mHijos)

            If (Not pPadre Is Nothing) Then
                pPadre.AñadirHijo(Me)
            End If
            Me.modificarEstado = EstadoDatosDN.SinModificar
        End Sub

        Public Sub New(ByVal pId As String, ByVal pNombre As String, ByVal pFechaModificacion As DateTime, ByVal pPadre As INodoDN, ByVal pValidador As IValidador)
            MyBase.New(pId, pNombre, pFechaModificacion, False)
            Me.CambiarValorCol(Of ColNodosDN)(New ColNodosDN, Me.mHijos)
            If (Not pPadre Is Nothing) Then
                pPadre.AñadirHijo(Me)
            End If

            Me.CambiarValorRef(pValidador, mValidadorh)
            Me.modificarEstado = EstadoDatosDN.SinModificar
        End Sub
#End Region

#Region "Propiedades"
        Public Property Hijos() As IColNodos Implements INodoDN.Hijos
            Get
                Return mHijos
            End Get
            Set(ByVal Value As IColNodos)
                Dim mensaje As String = String.Empty
                If (Not mValidadorh Is Nothing AndAlso mValidadorh.Validacion(mensaje, Value) = False) Then
                    Throw New ApplicationException(mensaje)

                Else
                    Me.CambiarValorCol(Of ColNodosDN)(Value, mHijos)
                    'mHijos = Value
                End If
            End Set
        End Property

        Public Overridable Property Padre() As INodoDN Implements INodoDN.Padre
            Get
                Return mPadre
            End Get
            Set(ByVal Value As INodoDN)

                If (Not Value Is Nothing) Then
                    Me.CambiarValorRef(Value, mPadre)
                    'Registrarme como su hijo si no me contine ya en su coleccion
                    mPadre.Hijos.AddUnico(Me)

                Else
                    Me.CambiarValorRef(Nothing, mPadre)
                End If
            End Set
        End Property

        Public ReadOnly Property Ruta() As String
            Get
                Dim zona As INodoDN
                Dim rutaTotal As String

                zona = Me

                rutaTotal = zona.Nombre
                zona = zona.Padre

                Do While Not zona Is Nothing
                    rutaTotal = zona.Nombre & " / " & rutaTotal
                    zona = zona.Padre
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

#Region "Metodos"
        Public Function ContieneHijo(ByVal pHijo As INodoDN) As Boolean Implements INodoDN.ContieneHijo
            Return ContenidoEnMi(pHijo)
        End Function

        Public Sub AñadirHijo(ByVal pHijos As ColNodosDN) Implements INodoDN.AñadirHijo
            Dim nh As INodoDN

            If (Not pHijos Is Nothing) Then
                For Each nh In pHijos
                    Me.AñadirHijo(nh)
                Next
            End If
        End Sub

        Public Overridable Sub AñadirHijo(ByVal pHijo As INodoDN) Implements INodoDN.AñadirHijo
            Dim mensaje As String = String.Empty

            'TODO: ALEX-->ALEX ojo has cambiado Contiene por ContenidoEnArbol
            If Not Me.ContenidoEnArbol(pHijo) Then 'Esto asegura que se establece una estructura arborea y no un grafo
                If Not mValidadorh Is Nothing AndAlso mValidadorh.Validacion(mensaje, pHijo) = False Then
                    Throw New ApplicationException("Error: error de validacion")

                Else
                    Me.Hijos.Add(pHijo)
                    pHijo.Padre = Me
                    Me.RegistrarParte(pHijo)
                    Me.modificarEstado = EstadoDatosDN.Modificado
                End If
            End If
        End Sub

        Public Shared Function EliminarNodo(ByVal pNodo As INodoDN) As Boolean
            Try
                If (Not pNodo Is Nothing) Then
                    If (pNodo.EliminarHijo(pNodo) Is Nothing) Then
                        Return False
                    End If

                    Return True
                End If

                Return False

            Catch ex As Exception
                Throw
            End Try
        End Function

        Public Function EliminarHijo(ByVal pHijo As INodoDN) As IList Implements INodoDN.EliminarHijo
            Dim al As New ArrayList
            If (Not pHijo.Padre Is Nothing) Then
                pHijo.Padre.Hijos.Remove(pHijo)
                pHijo.Padre = Nothing
                al.Add(pHijo)
            End If

            Return al
        End Function

        Private Function ContenidoEnMi(ByVal pHijo As INodoDN) As Boolean
            If Me.mHijos.Contains(pHijo) Then
                Return True
            End If

            If (pHijo.Padre Is Nothing) Then
                Return False
            End If

            If (pHijo.Padre Is Me) Then
                Return True

            Else
                Return ContenidoEnMi(pHijo.Padre)
            End If
        End Function

        Public Function ValidacionIdentica(ByVal pValidador As IValidador) As Boolean Implements IValidable.ValidacionIdentica
            Throw New NotImplementedException
        End Function

        Protected Shared Function PrimerAscendienteValido(ByVal pNodo As INodoDN, ByVal pValidador As IValidador) As INodoDN
            Dim mensaje As String = String.Empty

            If (pNodo.Padre Is Nothing) Then
                Return Nothing

            Else
                If pValidador.Validacion(mensaje, pNodo.Padre) Then
                    Return pNodo.Padre

                Else
                    Return PrimerAscendienteValido(pNodo.Padre, pValidador)
                End If
            End If
        End Function



        Public Function ObtenerPrimerAscendente(ByVal pTipo As Type, ByVal pAceptarHerencia As Boolean, ByVal pImplementaInterface As Boolean) As NodoBaseDN
            Dim op As Object
            op = Me.Padre
            If op.GetType Is pTipo Then
                Return op
            Else
                Return ObtenerPrimerAscendente(Me, pTipo, pAceptarHerencia, pImplementaInterface)
            End If

        End Function

        Private Function ObtenerPrimerAscendente(ByVal pnodobase As NodoBaseDN, ByVal pTipo As Type, ByVal pAceptarHerencia As Boolean, ByVal pImplementaInterface As Boolean) As NodoBaseDN
            Dim op As Object
            op = pnodobase.Padre
            If op.GetType Is pTipo Then
                Return op
            Else
                Return ObtenerPrimerAscendente(pnodobase, pTipo, pAceptarHerencia, pImplementaInterface)
            End If

        End Function
        Public Function ObtenerColEntidadesContenidas(ByVal pTipo As Type, ByVal pAceptarHerencia As Boolean, ByVal pImplementaInterface As Boolean) As ArrayList
            Dim nh As NodoBaseDN
            Dim al As New ArrayList

            For Each nh In Me.mHijos
                If (pImplementaInterface AndAlso (Not nh.GetType.GetInterface(pTipo.Name) Is Nothing)) OrElse (nh.GetType Is pTipo) OrElse (pAceptarHerencia AndAlso nh.GetType.IsSubclassOf(pTipo)) Then
                    al.Add(nh)
                End If

                al.AddRange(nh.ObtenerColEntidadesContenidas(pTipo, pAceptarHerencia, pImplementaInterface))
            Next

            Return al
        End Function



        'Private Function HeredaDe(ByVal pTipo As System.Type, ByVal pTipoBase As System.Type) As Boolean

        '    If pTipo.BaseType Is Nothing OrElse pTipo.BaseType Is GetType(Object) Then
        '        Return False
        '    Else
        '        If pTipo.BaseType Is pTipoBase Then
        '            Return True
        '        Else
        '            Return HeredaDe(pTipo.BaseType, pTipoBase)
        '        End If
        '    End If
        'End Function

        Public Function Profundidad() As Long Implements INodoDN.Profundidad
            Dim p As INodoDN
            Dim prof As Int64

            p = Me

            Do While Not p.Padre Is Nothing
                p = p.Padre
                prof += 1
            Loop

            Return prof
        End Function

        Private Function ProfundidadMaxDescendeniap(ByVal pPprof As Int64, ByVal pNodo As INodoDN) As Int16
            Dim profmax, proaux As Int64
            Dim hijo As INodoDN

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

        Public Function ProfundidadMaximaArbol() As Long Implements INodoDN.ProfundidadMaximaArbol
            Dim prof As Int64
            Dim p As INodoDN

            '1º encontrar a mi padre
            p = RaizArbol()

            'Incrementar por la pofundidad de mis hijos
            prof = p.ProfundidadMaxDescendenia

            Return prof
        End Function

        Public Function ProfundidadMaxDescendenia() As Short Implements INodoDN.ProfundidadMaxDescendenia
            Return ProfundidadMaxDescendeniap(0, Me)
        End Function

        Public Function RaizArbol() As INodoDN Implements INodoDN.RaizArbol
            Dim p As INodoDN

            p = Me

            Do While Not p.Padre Is Nothing
                p = p.Padre
            Loop

            Return p
        End Function
        Public Function ContenidoEnArbol(ByVal hijo As INodoDN) As Boolean Implements INodoDN.ContenidoEnArbol
            Return ContenidoEnMi(hijo)
        End Function

#End Region



        Public Overrides Sub ElementoAñadido(ByVal pSender As Object, ByVal pElemento As Object)
            If mHijos Is pSender Then
                Dim tipoHijo As NodoBaseDN
                tipoHijo = pElemento
                If Not tipoHijo.Padre Is Me Then
                    tipoHijo.Padre = Me
                End If
            End If
            MyBase.ElementoAñadido(pSender, pElemento)
        End Sub

        Public Overrides Sub ElementoEliminado(ByVal pSender As Object, ByVal pElemento As Object)
            If mHijos Is pSender Then
                Dim tipoHijo As NodoBaseDN
                tipoHijo = pElemento
                If tipoHijo.Padre Is Me Then
                    tipoHijo.Padre = Nothing
                End If
            End If
            MyBase.ElementoEliminado(pSender, pElemento)
        End Sub

        Public Property HijosNcH() As IColNodos Implements INodoConHijosDN.HijosNcH
            Get
                Return Me.Hijos
            End Get
            Set(ByVal value As IColNodos)

            End Set
        End Property

        Public Property PadreNcP() As INodoConPadreDN Implements INodoConPadreDN.PadreNcP
            Get

            End Get
            Set(ByVal value As INodoConPadreDN)

            End Set
        End Property

        Public Function NodoContenedorHijo(ByVal phijo As INodoDN) As INodoDN Implements INodoConHijosDN.NodoContenedorHijo


            If Me.mHijos.Contains(phijo) Then
                Return Me
            End If

            If (phijo.Padre Is Nothing) Then
                Return Me
            End If

            If (phijo.Padre Is Me) Then
                Return Me

            Else
                Return NodoContenedorHijo(phijo.Padre)
            End If
        End Function

        Public Property ColHojas() As System.Collections.IList Implements INodoDN.ColHojas
            Get
                Throw New NotImplementedException
            End Get
            Set(ByVal value As System.Collections.IList)
                Throw New NotImplementedException
            End Set
        End Property

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
End Namespace
