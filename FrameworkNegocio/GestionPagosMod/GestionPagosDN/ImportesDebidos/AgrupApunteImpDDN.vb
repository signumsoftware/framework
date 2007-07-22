
Imports Framework.DatosNegocio
<Serializable()> _
Public Class AgrupApunteImpDDN
    Inherits Framework.DatosNegocio.EntidadDN
    'Implements IImporteDebidoDN

    Implements IOrigenIImporteDebidoDN



    Protected WithEvents mColHEDN As Framework.DatosNegocio.ColHEDN
    Protected WithEvents mColApunteImpDDN As ColApunteImpDDN
    Protected mPermiteCompensar As Boolean
    ' Protected mAcreedora As FN.Localizaciones.DN.EntidadFiscalGenericaDN
    ' Protected mDeudora As FN.Localizaciones.DN.EntidadFiscalGenericaDN
    ' Protected mFCreación As Date
    ' Protected mFEfecto As Date
    ' Protected mFAnulacion As Date
    ' Protected mImporte As Double



    Protected mApunteImpDDN As ApunteImpDDN

    Private mEliminadoAlgunApunteID As Boolean



    Public Sub New()
        Me.CambiarValorRef(Of ApunteImpDDN)(New ApunteImpDDN(), mApunteImpDDN)
        Me.CambiarValorRef(Of Framework.DatosNegocio.ColHEDN)(New Framework.DatosNegocio.ColHEDN, mColHEDN)
        Me.CambiarValorRef(Of ColApunteImpDDN)(New ColApunteImpDDN, Me.mColApunteImpDDN)
        mApunteImpDDN.HuellaIOrigenImpDebDN = New HuellaIOrigenImpDebDN(Me)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub


    ' TODO: alex
    ' permitir estados de integridad inconsistentes para algunoas dns
    ' podemos poner un boleano que permita guarar o no dn en estado incosistente.

    Public Property PermiteCompensar() As Boolean
        Get
            Return Me.mPermiteCompensar
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, Me.mPermiteCompensar)
        End Set
    End Property


    Public ReadOnly Property EliminadoAlgunApunteID() As Boolean
        Get
            Return mEliminadoAlgunApunteID
        End Get
    End Property


    Public Property ColApunteImpDDN() As ColApunteImpDDN
        Get
            Return Me.mColApunteImpDDN
        End Get
        Set(ByVal value As ColApunteImpDDN)
            Me.CambiarValorRef(Of ColApunteImpDDN)(value, Me.mColApunteImpDDN)

        End Set
    End Property




    Public Property FAnulacion() As Date Implements IOrigenIImporteDebidoDN.FAnulacion
        Get
            Return Me.mApunteImpDDN.FAnulacion
        End Get
        Set(ByVal value As Date)
            Me.mApunteImpDDN.FAnulacion = value
        End Set
    End Property


    Public Property ColHEDN() As Framework.DatosNegocio.ColHEDN Implements IOrigenIImporteDebidoDN.ColHEDN
        Get
            Return mColHEDN
        End Get
        Set(ByVal value As Framework.DatosNegocio.ColHEDN)
            Me.CambiarValorRef(Of Framework.DatosNegocio.ColHEDN)(value, mColHEDN)
        End Set
    End Property



    Public Property IImporteDebidoDN() As IImporteDebidoDN Implements IOrigenIImporteDebidoDN.IImporteDebidoDN
        Get
            Return mApunteImpDDN
        End Get
        Set(ByVal value As IImporteDebidoDN)
            Me.CambiarValorRef(Of ApunteImpDDN)(value, mApunteImpDDN)
        End Set
    End Property

    Private Sub mColApunteImpDDN_ElementoAñadido(ByVal sender As Object, ByVal elemento As Object) Handles mColApunteImpDDN.ElementoAñadido


        Dim apunte As ApunteImpDDN = elemento

        If Not apunte.GUIDAgrupacion = Nothing AndAlso Not apunte.GUIDAgrupacion = Me.mGUID Then
            Throw New ApplicationException("El importe debido ya pertenece a alguna otra agrupacion")
        End If

        If Not apunte.FAnulacion = Date.MinValue AndAlso Me.FAnulacion = Date.MinValue Then
            Throw New ApplicationException("El importe debido esta anulado y no puede ser añadido a una agrupacion no anulada")
        End If


        apunte.GUIDAgrupacion = Me.mGUID
        Dim he As New Framework.DatosNegocio.HEDN(apunte)
        If Not mColHEDN.Contiene(he, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
            mColHEDN.Add(he)
        End If

    End Sub





    Private Sub mColApunteImpDDN_ElementoEliminado(ByVal sender As Object, ByVal elemento As Object) Handles mColApunteImpDDN.ElementoEliminado


        Dim apunte As ApunteImpDDN = elemento
        apunte.GUIDAgrupacion = Nothing

        mEliminadoAlgunApunteID = True

        Dim he As New Framework.DatosNegocio.HEDN(apunte)
        If mColHEDN.Contiene(he, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
            mColHEDN.EliminarEntidadDNxGUID(he.GUID)
        End If



    End Sub

    Private Sub mColHEDN_ElementoAñadido(ByVal sender As Object, ByVal elemento As Object) Handles mColHEDN.ElementoAñadido
        Dim he As Framework.DatosNegocio.HEDN = elemento
        he = elemento
        If he.EntidadReferida Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("No permite agregar huellas no cargadas para esta clase")

        Else
            mColApunteImpDDN.AddUnico(he.EntidadReferida)
        End If
    End Sub

    Private Sub mColHEDN_ElementoEliminado(ByVal sender As Object, ByVal elemento As Object) Handles mColHEDN.ElementoEliminado
        Dim he As Framework.DatosNegocio.HEDN = elemento
        mColApunteImpDDN.EliminarEntidadDNxGUID(he.GUIDReferida)
    End Sub

    Public Function Actualizar() As Double
        Dim fefectoMaxima As Date
        Me.mApunteImpDDN.Importe = VerificrYCalcularImporte(fefectoMaxima)
        Me.mApunteImpDDN.FEfecto = fefectoMaxima
        Return Me.mApunteImpDDN.Importe
    End Function


    Private Function VerificrYCalcularImporte(ByRef pfefectoMaxima As Date) As Double
        Dim importe As Double = 0




        If Not Me.mApunteImpDDN.HuellaIOrigenImpDebDN.GUIDReferida = Me.GUID Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("la agrupacion debe ser el origen de importe debido referido por el apunte debidop producto")
        End If


        If ColApunteImpDDN.Count = 0 Then
            pfefectoMaxima = Date.MinValue
            Me.mApunteImpDDN.Importe = 0
            Me.mApunteImpDDN.FEfecto = pfefectoMaxima
            Return 0
        End If


        pfefectoMaxima = ColApunteImpDDN(0).FEfecto

        For Each aid As ApunteImpDDN In Me.ColApunteImpDDN
            ' vertificar la referencia
            If Not aid.GUIDAgrupacion = Me.GUID Then
                Throw New Framework.DatosNegocio.ApplicationExceptionDN("Todos los importes debidos agrupados deben referir a la arupacion en su GUID de agrupacion")
            End If




            ' verificar el estado de anulacion

            If Me.FAnulacion = Date.MinValue Then
                ' si la arupación esta activa todos los importes debidos deben estr activos
                If Not aid.FAnulacion = Date.MinValue Then
                    Throw New Framework.DatosNegocio.ApplicationExceptionDN("No pueden extir importes debidos anulados en una agrupacion activa aid id:" & aid.ID & " " & aid.ToString)
                End If

            Else
                ' si la agrupacion esta anulada todos los imports debidos agrupados deben estar anulados
                If Not aid.FAnulacion = Date.MinValue Then
                    Throw New Framework.DatosNegocio.ApplicationExceptionDN("No pueden extir importes debidos anulados en una agrupacion activa aid id:" & aid.ID & " " & aid.ToString)
                End If

            End If



            ' varificar las entidades fiscales referidas

            If Me.mPermiteCompensar Then
                If Not Me.mApunteImpDDN.AcrredoyDeudorCompatibles(aid) Then
                    Throw New Framework.DatosNegocio.ApplicationExceptionDN("no todos los acrredores y deudores son comaptibles en los importes debidos referidos")

                End If

            Else
                If Not Me.mApunteImpDDN.AcrredoyDeudorIguales(aid) Then
                    Throw New Framework.DatosNegocio.ApplicationExceptionDN("no todos los acrredores y deudores son iguales en los importes debidos referidos")
                End If
            End If

            ' calcualr el importe
            If Me.mApunteImpDDN.Acreedora.GUID = aid.Acreedora.GUID Then
                importe += aid.Importe
            Else
                importe -= aid.Importe

            End If

            ' actualizar la fecha efecto

            If pfefectoMaxima < aid.FEfecto Then
                pfefectoMaxima = aid.FEfecto
            End If

        Next


        Return importe

    End Function


    Public Function Anulable(ByRef pMensaje As String) As Boolean Implements IOrigenIImporteDebidoDN.Anulable


        Return Me.mApunteImpDDN.Anulable(pMensaje)
    End Function



    Public Function EliminarAIDReferidos() As ColApunteImpDDN




        Dim col As New ColApunteImpDDN
        col.AddRangeObject(Me.mColApunteImpDDN)

        Do While Me.mColApunteImpDDN.Count > 0
            Me.mColApunteImpDDN.RemoveAt(0)
        Loop


        Return col
    End Function




    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If Me.mApunteImpDDN Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("Una agrupacion requiere un objeto de apunte debido producto")
        End If

        Dim fefectoMaxima As Date
        Dim importe As Double = VerificrYCalcularImporte(fefectoMaxima)


        ' verificar el importe

        If Not Me.mApunteImpDDN.Importe = importe Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("los importes son discordantes")
        End If

        ' verificar la fecha de efecto
        If Me.mApunteImpDDN.FEfecto > fefectoMaxima Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("la fecha de efecto es menor que la minima posible")
        End If


        Return MyBase.EstadoIntegridad(pMensaje)
    End Function


    Protected Function Anular1(ByVal fAnulacion As Date) As Object Implements IOrigenIImporteDebidoDN.Anular
        Dim Mensaje As String
        If Not Anulable(Mensaje) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN(Mensaje)
        End If
        Me.IImporteDebidoDN.FAnulacion = fAnulacion
        Return Me.EliminarAIDReferidos()
    End Function
End Class




<Serializable()> _
Public Class ColAgrupApunteImpDDN
    Inherits ArrayListValidable(Of AgrupApunteImpDDN)

End Class




