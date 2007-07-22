
Imports Framework.DatosNegocio

''' <summary>
''' Es parecida a un apunte en cuenta.
''' 
''' Representa unos derechos de de una entidad fiscal acrredora sobre otra deudora
''' 
''' cualquier movimiento de dinero como un pago implicará un importe debido
''' cualquier prestación de servicios o de cesión de mercaderias implicará un importe debido
''' 
''' dadas dos entidades fiscales si se saldan sus importes debidos se podrá verificar si una le debe dinero a la otra
'''  
''' para que un origen debido pueda genererse es necesaria un origen de importe debido que es la clase que le relaciona con su causa
''' </summary>
''' <remarks></remarks>
Public Interface IImporteDebidoDN
    Inherits Framework.DatosNegocio.IEntidadDN

    Property Acreedora() As FN.Localizaciones.DN.EntidadFiscalGenericaDN
    Property Deudora() As FN.Localizaciones.DN.EntidadFiscalGenericaDN
    Property Importe() As Double
    Property FCreación() As Date
    Property FEfecto() As Date

    Property HuellaIOrigenImpDebDN() As HuellaIOrigenImpDebDN
    ' Property HuellaIOrigenImpDebDN() As Framework.DatosNegocio.HEDN
    Property FAnulacion() As Date
    ''' <summary>
    ''' el guid de la entidad dn agrupadora  (puede ser 0 o 1)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Property GUIDAgrupacion() As String

    Function CrearImpDebCompesatorio(ByVal origen As IOrigenIImporteDebidoDN) As IImporteDebidoDN
    Function Anulable(ByRef pMensaje As String) As Boolean
End Interface




<Serializable()> _
Public Class ColIImporteDebidoDN
    Inherits ArrayListValidable(Of IImporteDebidoDN)



    Public Function SoloDosEntidadesFiscales() As Boolean


        Dim ief1, ief2 As FN.Localizaciones.DN.IEntidadFiscalDN
        ief1 = Me.Item(0).Deudora
        ief2 = Me.Item(0).Acreedora


        For Each ie As IImporteDebidoDN In Me

            If Not (ie.Deudora.GUID = ief1.GUID OrElse ie.Deudora.GUID = ief2.GUID) AndAlso (ie.Acreedora.GUID = ief1.GUID OrElse ie.Acreedora.GUID = ief2.GUID) Then
                Return False
            End If

        Next

        Return True

    End Function


    ''' <summary>
    ''' genera una nueva coleccion donde todos los elementos refierer a las dos entidades fiscales indistientamente como deudora y acreedora
    ''' </summary>
    ''' <param name="ief1"></param>
    ''' <param name="ief2"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function SeleccioanrSoloDosEntidadesFiscales(ByVal ief1 As FN.Localizaciones.DN.IEntidadFiscalDN, ByVal ief2 As FN.Localizaciones.DN.IEntidadFiscalDN) As ColIImporteDebidoDN

        Dim col As New ColIImporteDebidoDN


        For Each ie As IImporteDebidoDN In Me

            If (ie.Deudora.GUID = ief1.GUID OrElse ie.Deudora.GUID = ief2.GUID) AndAlso (ie.Acreedora.GUID = ief1.GUID OrElse ie.Acreedora.GUID = ief2.GUID) Then
                col.Add(ie)
            End If

        Next

        Return col

    End Function






End Class




