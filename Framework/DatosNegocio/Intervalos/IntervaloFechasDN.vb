Imports Framework.DatosNegocio


Namespace Localizaciones.Temporales


    <Serializable()> Public Class ColIntervaloFechasSubordinadoDN
        Inherits ArrayListValidable(Of IntervaloFechasSubordinadoDN)



        Public Function FechaMaxima() As Date
            Dim it As IntervaloFechasSubordinadoDN
            Dim maxima As Date
            For Each it In Me
                If maxima < it.FFinal Then
                    maxima = it.FFinal
                End If
            Next
            Return maxima
        End Function
        Public Function FechaMinima() As Date
            Dim it As IntervaloFechasSubordinadoDN
            Dim minima As Date
            minima = Date.MaxValue
            For Each it In Me
                If minima > it.FInicio Then
                    minima = it.FInicio
                End If
            Next
            Return minima
        End Function

        Public Sub ElementosExtremos(ByRef fmax As Date, ByRef fMin As Date, ByRef itMax As IntervaloFechasSubordinadoDN, ByRef itMin As IntervaloFechasSubordinadoDN)
            Dim it As IntervaloFechasSubordinadoDN
            fmax = Date.MinValue
            fMin = Date.MaxValue
            For Each it In Me
                If fmax < it.FFinal Then
                    fmax = it.FFinal
                    itMax = it
                End If

                If fMin > it.FInicio Then
                    fMin = it.FInicio
                    itMin = it
                End If

            Next
        End Sub

    End Class

    <Serializable()> Public Class IntervaloFechasSubordinadoDN
        Inherits Framework.DatosNegocio.EntidadDN
        ' esta clase es un intervalo de fecha que esta dentro de otro llamado padre
        ' si un intervlo hijo amplia alguno de sus rangos y estos sobrepasan a los de su padre
        ' el rango de su padre se amplia automticamente y emite un evento

        ' este comporamiento es tanto para la creacion de una tarea nueva como para la modificacion de una tarea

        Protected WithEvents mIntervaloFechas As IntervaloFechasDN
        Protected mIFPadre As IntervaloFechasSubordinadoDN
        Protected mColHijos As ColIntervaloFechasSubordinadoDN
        Protected mIntervaloAmpliablePorHijo As Boolean = True
        Protected mPermitirEstrechamiento As Boolean = True

        Public ReadOnly Property IntervaloFechasClon() As IntervaloFechasDN
            Get
                Return Me.mIntervaloFechas.Clone
            End Get
        End Property
        Public Property PermitirEstrechamiento() As Boolean
            Get
                Return mPermitirEstrechamiento
            End Get
            Set(ByVal value As Boolean)
                mPermitirEstrechamiento = value
            End Set
        End Property


        Public Sub New()
            Me.CambiarValorRef(Of IntervaloFechasDN)(New IntervaloFechasDN(Date.MinValue, Date.MinValue), mIntervaloFechas)
            Me.CambiarValorRef(Of ColIntervaloFechasSubordinadoDN)(New ColIntervaloFechasSubordinadoDN, mColHijos)

            Me.modificarEstado = EstadoDatosDN.SinModificar

        End Sub

        Public Sub New(ByVal pFInicio As Date, ByVal pFFinal As Date, ByVal padre As IntervaloFechasSubordinadoDN)
            Me.CambiarValorRef(Of IntervaloFechasDN)(New IntervaloFechasDN(pFInicio, pFFinal), mIntervaloFechas)
            Me.CambiarValorRef(Of ColIntervaloFechasSubordinadoDN)(New ColIntervaloFechasSubordinadoDN, mColHijos)


            AsignarPadre(padre)
            Me.modificarEstado = EstadoDatosDN.SinModificar
        End Sub
        Public Sub AsignarPadre(ByVal padre As IntervaloFechasSubordinadoDN)

            Me.CambiarValorRef(Of IntervaloFechasSubordinadoDN)(padre, mIFPadre)

            ' _IFPadre = padre
            If Not padre Is Nothing Then
                padre.AddHijo(Me)
            End If

            ModificarPadre()
        End Sub
        Public Sub AddHijo(ByVal pif As IntervaloFechasSubordinadoDN)
            mColHijos.Add(pif)
        End Sub
        Public Function FechaEnPeriodo(ByVal pFecha As Date) As Boolean
            Return Me.mIntervaloFechas.FechaEnPeriodo(pFecha)
        End Function
        Public ReadOnly Property Dias() As Int64
            Get
                Return Me.mIntervaloFechas.Dias

            End Get
        End Property
        Public Property IntervaloAmpliablePorHijo() As Boolean
            Get
                Return Me.mIntervaloAmpliablePorHijo
            End Get
            Set(ByVal value As Boolean)
                mIntervaloAmpliablePorHijo = value
            End Set
        End Property
        Public Property FFinal() As Date
            Get
                Return Me.mIntervaloFechas.FFinal
            End Get
            Set(ByVal Value As Date)
                Me.mIntervaloFechas.FFinal = Value
                ' actualizar a tu padre        
                ModificarPadre()
            End Set
        End Property


        Public Property FInicio() As Date
            Get
                Return Me.mIntervaloFechas.FInicio
            End Get
            Set(ByVal Value As Date)
                Me.mIntervaloFechas.FInicio = Value
                ' actualizar a tu padre        
                ModificarPadre()
            End Set
        End Property
        Public Function ModificarPadre() As Boolean

            If Not Me.mIFPadre Is Nothing Then

                If Me.FInicio.Ticks < Me.mIFPadre.FInicio.Ticks Then
                    If mIntervaloAmpliablePorHijo Then
                        Me.mIFPadre.FInicio = FInicio
                    Else
                        Throw New ApplicationException("El intervalo no cabe en su padre")
                    End If

                End If

                If Me.FFinal.Ticks > Me.mIFPadre.FFinal.Ticks Then

                    If mIntervaloAmpliablePorHijo Then
                        Me.mIFPadre.FFinal = FFinal
                    Else
                        Throw New ApplicationException("El intervalo no cabe en su padre")
                    End If
                End If


            End If


        End Function

        Private Function ValModificacionIntervalo(ByRef mensaje As String, ByVal it As IntervaloFechasSubordinadoDN) As Boolean
            Dim itmax, itmin As IntervaloFechasSubordinadoDN
            Dim fechaMax, FechaMin As Date
            Me.mColHijos.ElementosExtremos(fechaMax, FechaMin, itmax, itmin)


            ' un intervalo temporal no puede modificarse en valores inferiores a los que conitnen a sus hijos

            If it.FFinal < fechaMax OrElse it.FInicio > FechaMin Then
                mensaje = "los limites del intervalo temporal no pueden onetener a todos los hijos"
                Return False
            Else
                Return True
            End If



        End Function


        Private Function ValModificacionFechaMaxima(ByRef mensaje As String, ByVal pFechaMaxima As Date) As Boolean
            Dim itmax, itmin As IntervaloFechasSubordinadoDN
            Dim fechaMax, FechaMin As Date
            Me.mColHijos.ElementosExtremos(fechaMax, FechaMin, itmax, itmin)


            ' un intervalo temporal no puede modificarse en valores inferiores a los que conitnen a sus hijos
            If pFechaMaxima >= fechaMax Then
                Return True
            Else
                mensaje = "La fecha minima no puede ser mayor la la menor fecha de sus elementos contenidos"
                Return False
            End If

        End Function

        Private Function ValModificacionFechaMinima(ByRef mensaje As String, ByVal pFechaMinima As Date) As Boolean
            Dim itmax, itmin As IntervaloFechasSubordinadoDN
            Dim fechaMax, FechaMin As Date
            Me.mColHijos.ElementosExtremos(fechaMax, FechaMin, itmax, itmin)

            ' un intervalo temporal no puede modificarse en valores inferiores a los que conitnen a sus hijos
            If pFechaMinima <= FechaMin Then
                Return True
            Else
                mensaje = "La fecha minima no puede ser mayor la la menor fecha de sus elementos contenidos"
                Return False
            End If


        End Function

        Private Sub mIntervaloFechas_FFModificando(ByVal sender As Object, ByVal nuevoval As Date, ByRef cancelar As Boolean) Handles mIntervaloFechas.FFModificando
            Dim mensaje As String

            If ValModificacionFechaMaxima(mensaje, nuevoval) Then
                'If Me.mPermitirEstrechamiento Then
                '    MinimizarPeriodoPlanificadoRamaAscendente()
                'End If
            Else
                cancelar = True
                Throw New ApplicationException(mensaje)
            End If
        End Sub

        Private Sub mIntervaloFechas_FFModificdo(ByVal sender As Object, ByVal e As System.EventArgs) Handles mIntervaloFechas.FFModificdo
            'If Me.mPermitirEstrechamiento Then
            '    MinimizarPeriodoPlanificadoRamaAscendente()
            'End If
            'If Me.mIFPadre Is Nothing AndAlso Me.mColHijos.Count > 0 Then
            '    Me.MinimizarPeriodoPlanificado()
            'End If
        End Sub

        Private Sub mIntervaloFechas_FIModificando(ByVal sender As Object, ByVal nuevoval As Date, ByRef cancelar As Boolean) Handles mIntervaloFechas.FIModificando
            Dim mensaje As String

            If Me.ValModificacionFechaMinima(mensaje, nuevoval) Then
                'If Me.mPermitirEstrechamiento Then
                '    MinimizarPeriodoPlanificadoRamaAscendente()
                'End If
            Else
                cancelar = True
                Throw New ApplicationException(mensaje)
            End If
        End Sub


        Public Property arastramiento() As Boolean
            Get
                Return Me.mIntervaloFechas.Arrastramiento
            End Get
            Set(ByVal value As Boolean)
                Me.mIntervaloFechas.Arrastramiento = True
            End Set
        End Property

        Public Sub MinimizarPeriodoPlanificado()
            Dim fmax, fmin As Date

            Dim colifs As ColIntervaloFechasSubordinadoDN
            If Me.mColHijos IsNot Nothing AndAlso Me.mColHijos.Count > 0 Then
                colifs = Me.mColHijos
                colifs.ElementosExtremos(fmax, fmin, Nothing, Nothing)
                Me.mIntervaloFechas.FInicio = fmin
                Me.mIntervaloFechas.FFinal = fmax
            End If

        End Sub
        Public Sub MinimizarPeriodoPlanificadoRamaAscendente()
            ' MinimizarPeriodoPlanificado()
            'If Not Me.mIFPadre Is Nothing Then
            '    Me.mIFPadre.MinimizarPeriodoPlanificado()
            'End If

            'If Me.mIFPadre Is Nothing AndAlso Me.mColHijos.Count > 0 Then
            '    Me.MinimizarPeriodoPlanificado()
            'End If
        End Sub

        Private Sub mIntervaloFechas_FIModificdo(ByVal sender As Object, ByVal e As System.EventArgs) Handles mIntervaloFechas.FIModificdo
            'If Me.mPermitirEstrechamiento Then
            '    MinimizarPeriodoPlanificadoRamaAscendente()
            'End If
            'If Me.mIFPadre Is Nothing AndAlso Me.mColHijos.Count > 0 Then
            '    Me.MinimizarPeriodoPlanificado()
            'End If
        End Sub
    End Class

    <Serializable()> Public Class IntervaloFechasDN
        Inherits EntidadDN
        Implements IIntervaloTemporal



        Protected mFFinal As Date
        Protected mFInicio As Date
        Protected mArrastramiento As Boolean

        Public Event FFModificando(ByVal sender As Object, ByVal nuevoval As Date, ByRef cancelar As Boolean)
        Public Event FIModificando(ByVal sender As Object, ByVal nuevoval As Date, ByRef cancelar As Boolean)
        Public Event FFModificdo(ByVal sender As Object, ByVal e As EventArgs)
        Public Event FIModificdo(ByVal sender As Object, ByVal e As EventArgs)

#Region "Constructores"

        Public Sub New()
            MyBase.New()
        End Sub

        Public Sub New(ByVal pFInicio As Date, ByVal pFFinal As Date)
            Dim mensaje As String = String.Empty
            If ValFechas(pFFinal, pFInicio, mensaje) Then
                Me.CambiarValorVal(Of Date)(pFFinal, mFFinal)
                Me.CambiarValorVal(Of Date)(pFInicio, mFInicio)
            Else
                Throw New ApplicationException(mensaje)
            End If
        End Sub

#End Region

#Region "Propiedades"

        Public Property Arrastramiento() As Boolean
            Get
                Return mArrastramiento
            End Get
            Set(ByVal Value As Boolean)
                mArrastramiento = Value
            End Set
        End Property

        Public Overridable Property FFinal() As Date Implements IIntervaloTemporal.FF
            Get
                Return mFFinal
            End Get
            Set(ByVal Value As Date)
                Dim cacelar As Boolean
                RaiseEvent FFModificando(Me, Value, cacelar)
                If cacelar Then
                    Exit Property
                End If

                If Value < Me.mFInicio AndAlso Value <> Date.MinValue Then

                    If Me.mArrastramiento = True Then
                        Me.CambiarValorVal(Of Date)(Value, mFInicio)
                        ' arrastramiento e fechas
                    Else
                        Throw New ApplicationException("FF no puede ser menor que FI")
                    End If

                End If

                Me.CambiarValorVal(Of Date)(Value, mFFinal)

                RaiseEvent FFModificdo(Me, Nothing)

            End Set
        End Property

        Public Overridable Property FInicio() As Date Implements IIntervaloTemporal.FI
            Get
                Return mFInicio
            End Get
            Set(ByVal Value As Date)

                Dim cacelar As Boolean
                RaiseEvent FIModificando(Me, Value, cacelar)
                If cacelar Then
                    Exit Property
                End If

                If Value > Me.mFFinal AndAlso mFFinal <> Date.MinValue Then

                    If Me.mArrastramiento = True Then
                        Me.CambiarValorVal(Of Date)(Value, Me.mFFinal) ' arrastramiento e fechas
                    Else
                        Throw New ApplicationException("FI no puede ser mayor que FF")
                    End If

                End If

                Me.CambiarValorVal(Of Date)(Value, mFInicio)

                '  mFInicio = Value
                RaiseEvent FIModificdo(Me, Nothing)

            End Set
        End Property

        Public ReadOnly Property Horas() As Int64
            Get
                Return Me.mFFinal.Subtract(Me.mFInicio).TotalHours

            End Get
        End Property

        Public ReadOnly Property Dias() As Int64
            Get
                Return Me.mFFinal.Subtract(Me.mFInicio).TotalDays()
            End Get
        End Property

        Public ReadOnly Property ToString1() As String

            Get

                Dim cadena As String

                If mFInicio = Date.MinValue Then
                    cadena = "FI: -- "
                Else
                    cadena = "FI:" & mFInicio.ToShortDateString
                End If
                If mFFinal = Date.MinValue Then
                    cadena += "  FF: -- "
                Else
                    cadena += "  FF:" & mFFinal.ToShortDateString
                End If

                Return cadena
            End Get
        End Property

#End Region

#Region "Métodos"

        Public Function Contiene(ByVal pFecha As Date) As Boolean Implements IIntervaloTemporal.Contiene

            Return Me.mFInicio <= pFecha AndAlso (pFecha <= Me.mFFinal OrElse Me.mFFinal = Date.MinValue)
        End Function

        Public Overrides Function ToString() As String

            'If Me.mFInicio = Date.MinValue Then
            '    Return "Sin establecer"
            'Else

            '    Dim cadena As String

            '    cadena = "Fi: " & mFInicio.ToString

            '    If Me.mFFinal > Date.MinValue Then
            '        cadena += " Ff: " & mFFinal.ToString
            '    End If
            '    Return cadena
            'End If
            Me.mToSt = Me.ToString1
            Return Me.mToSt
        End Function

        Public Function FechaEnPeriodo(ByVal pFecha As Date) As Boolean
            If pFecha >= Me.mFInicio AndAlso pFecha <= Me.mFFinal Then
                Return True
            Else
                Return False
            End If
        End Function

        Public Shared Function ValFechas(ByVal ff As Date, ByVal fi As Date, ByRef mensaje As String) As Boolean

            If fi <= ff OrElse ff = Date.MinValue Then
                Return True
            Else
                mensaje = "Marcas temporales inconsistentes. feha inicial no es menor o igual que fecha final"
                Return False
            End If
        End Function

        Public Overrides Function EstadoIntegridad(ByRef mensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

            If ValFechas(Me.mFFinal, Me.mFInicio, mensaje) Then
                Return EstadoIntegridad.Consistente
            Else
                Return EstadoIntegridad.Inconsistente
            End If

        End Function

    

        Public Shared Function DimeLaFechaMayor(ByVal pFecha1 As Date, ByVal pFecha2 As Date) As Date
            If pFecha1 >= pFecha2 Then
                Return pFecha1
            End If
            Return pFecha2
        End Function

        Public Shared Function DimeLaFechaMenor(ByVal pFecha1 As Date, ByVal pFecha2 As Date) As Date
            If pFecha1 <= pFecha2 Then
                Return pFecha1
            End If
            Return pFecha2
        End Function

        Public Function IntervaloSolapado(ByVal pIntervalo As IntervaloFechasDN) As Boolean
            If Me.FechaEnPeriodo(pIntervalo.FInicio) OrElse Me.FechaEnPeriodo(pIntervalo.FFinal) Then
                Return True
            End If

            Return False
        End Function

#End Region


        Public Function BienFormado() As Boolean Implements IIntervaloTemporal.BienFormado

            Return Me.mFInicio <= Me.FFinal OrElse (Me.mFInicio > Date.MinValue AndAlso Me.mFFinal = Date.MinValue)
        End Function

 



        Public Function SolapadoOContenido(ByVal pIntervalo As IIntervaloTemporal) As IntSolapadosOContenido Implements IIntervaloTemporal.SolapadoOContenido

            If Not (Me.BienFormado AndAlso pIntervalo.BienFormado) Then
                Throw New ApplicationExceptionDN("alguno de los intervalos no esta bien formado")
            End If

            If Me.mFInicio = pIntervalo.FI AndAlso Me.mFFinal = pIntervalo.FF Then
                Return IntSolapadosOContenido.Iguales
            End If

            If (Me.mffinal <= pIntervalo.FI AndAlso Me.mffinal > Date.MinValue) OrElse (Me.mfinicio >= pIntervalo.Ff AndAlso pIntervalo.Ff > Date.MinValue) Then
                Return IntSolapadosOContenido.Libres

            Else


                If Me.mFInicio <= pIntervalo.FI AndAlso (Me.mFFinal >= pIntervalo.FF AndAlso Not (pIntervalo.FF = Date.MinValue AndAlso Me.mFFinal > Date.MinValue)) Then ' yo le contego
                    Return IntSolapadosOContenido.Contenedor

                ElseIf Me.mFInicio >= pIntervalo.FI AndAlso Me.mFFinal <= pIntervalo.FF AndAlso Not (Me.mFFinal = Date.MinValue AndAlso pIntervalo.FF > Date.MinValue) Then ' el me contine
                    Return IntSolapadosOContenido.Contenido

                Else
                    ' tenemos que estar solapados
                    Return IntSolapadosOContenido.Solapados


                End If

            End If

        End Function
    End Class

    <Serializable()> _
    Public Class ColIntervaloFechasDN
        Inherits ArrayListValidable(Of IntervaloFechasDN)


#Region "Métodos"


        Public Function PrimeroNoCumple(ByVal pIntSolapadosOContenido As IntSolapadosOContenido) As ParIntervalos

            For Each IntervaloFechas1 As IntervaloFechasDN In Me

                For Each IntervaloFechas2 As IntervaloFechasDN In Me

                    If Not IntervaloFechas1 Is IntervaloFechas2 AndAlso Not IntervaloFechas1.SolapadoOContenido(IntervaloFechas2) = pIntSolapadosOContenido Then

                        Dim par As New ParIntervalos
                        par.Int1 = IntervaloFechas1
                        par.Int2 = IntervaloFechas2

                        Return par
                    End If

                Next

            Next

            Return Nothing

        End Function




        Public Function TodosNoCumplen(ByVal pIntSolapadosOContenido As IntSolapadosOContenido) As System.Collections.Generic.List(Of ParIntervalos)


            Dim al As New System.Collections.Generic.List(Of ParIntervalos)


            For Each IntervaloFechas1 As IntervaloFechasDN In Me

                For Each IntervaloFechas2 As IntervaloFechasDN In Me

                    If Not IntervaloFechas1 Is IntervaloFechas2 AndAlso Not IntervaloFechas1.SolapadoOContenido(IntervaloFechas2) = pIntSolapadosOContenido Then

                        Dim par As New ParIntervalos
                        par.Int1 = IntervaloFechas1
                        par.Int2 = IntervaloFechas2

                        al.Add(par)
                    End If

                Next

            Next

            Return al

        End Function
        Public Function IntervalosFechaSolapados() As Boolean
            Dim colIntervalos As New ColIntervaloFechasDN()

            For Each intervalo As IntervaloFechasDN In Me
                For Each intervalo2 As IntervaloFechasDN In colIntervalos
                    If intervalo.IntervaloSolapado(intervalo2) Then
                        Return True
                    End If
                Next
                colIntervalos.Add(intervalo)
            Next

            Return False

        End Function

        Public Function ColIFechasContenidoIntervalo(ByVal intervaloPadre As IntervaloFechasDN) As Boolean
            Dim intervaloContenedor As IntervaloFechasDN

            intervaloContenedor = RecuperarIntervaloContengaColIntervalo()

            If intervaloPadre.FInicio > intervaloContenedor.FInicio OrElse intervaloPadre.FFinal < intervaloContenedor.FFinal Then
                Return False
            End If

            Return True

        End Function

        Public Function RecuperarIntervaloContengaColIntervalo() As IntervaloFechasDN
            Dim fechaMenor As Date = Date.MaxValue
            Dim fechaMayor As Date = Date.MinValue

            For Each intF As IntervaloFechasDN In Me
                fechaMenor = IntervaloFechasDN.DimeLaFechaMenor(intF.FInicio, fechaMenor)
            Next

            For Each intF As IntervaloFechasDN In Me
                fechaMayor = IntervaloFechasDN.DimeLaFechaMayor(intF.FFinal, fechaMayor)
            Next

            Return New IntervaloFechasDN(fechaMenor, fechaMayor)

        End Function

#End Region

    End Class



    Public Class ParIntervalos
        Public Int1 As IntervaloFechasDN
        Public Int2 As IntervaloFechasDN

    End Class

End Namespace

