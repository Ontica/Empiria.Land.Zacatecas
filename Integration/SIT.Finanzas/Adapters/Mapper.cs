﻿/* Empiria Land **********************************************************************************************
*                                                                                                            *
*  Module   : Transaction Management                     Component : Interface adapters                      *
*  Assembly : SIT.Finanzas.Connector.dll                 Pattern   : Mapper class                            *
*  Type     : Mapper                                     License   : Please read LICENSE.txt file            *
*                                                                                                            *
*  Summary  : Data Transfer Objects mapper.                                                                  *
*                                                                                                            *
************************* Copyright(c) La Vía Óntica SC, Ontica LLC and contributors. All rights reserved. **/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Empiria.Land.Integration.PaymentServices;

namespace Empiria.Zacatecas.Integration.SITFinanzasConnector.Adapters {

  /// <summary>Map DataTypes.</summary>
  internal class Mapper {

    #region Global Variables

    private static List<ServicioDto> services = new List<ServicioDto>();

    #endregion

    #region Internal Methods

    internal async static Task<PaymentOrderDto> GetPaymentRequest(PaymentOrderRequestDto request) {
      SolicitudDto sitRequest = MapPaymentRequestToSITRequest(request);

      OrdenPagoDto ordenPago = await ApiClient.CreatePaymentRequest(sitRequest);

      PaymentOrderDto paymentOrder = MapSITOrdenPagoToPaymentOrderRequest(ordenPago);

      var paymentFormatUrl = await GetFormatPaymentURL(paymentOrder.UID);

      paymentOrder.AddAttribute("PaymentFormatUrl", paymentFormatUrl);

      return paymentOrder;
    }

    internal async static Task<PaymentOrderRequestConceptDto> GetFixedConceptCost(string serviceUID, decimal quantity) {
      var service = await GetSITService(serviceUID);

      var concept = new PaymentOrderRequestConceptDto();
      concept.ConceptUID = service.idServicio.ToString();
      concept.Quantity = quantity;
      concept.UnitCost = Convert.ToDecimal(service.importe);
      concept.Total = (concept.UnitCost * concept.Quantity);

      return concept;
    }

    internal async static Task<decimal> GetVariableConceptCost(string electronicPaymentUId, string serviceUID, decimal taxableBase) {
      var presupuesto = new PresupuestoDto();

      presupuesto.cantidad = 1;
      presupuesto.idPagoElectronico = Convert.ToInt32(electronicPaymentUId);
      presupuesto.idServicio = Convert.ToInt32(serviceUID);
      presupuesto.valor = taxableBase;

      return await ApiClient.GetVariableCost(presupuesto);
    }

    internal async static Task<SITPaymentDto> GetPayment(string elctronicPaymentUID) {
      int idPagoElectronico = Convert.ToInt32(elctronicPaymentUID);
      var SITPayment = await ApiClient.ValidatePayment(idPagoElectronico);

      return MapSITPaymentToPayment(SITPayment);
    }

    #endregion Internal Methods

    #region Private Methods

    private static SolicitudDto MapPaymentRequestToSITRequest(PaymentOrderRequestDto paymentRequest) {
      SolicitudDto solicitud = new SolicitudDto();

      solicitud.contribuyente = paymentRequest.RequestedBy;
      solicitud.rfc = paymentRequest.RFC;
      solicitud.direccion = paymentRequest.Address;
      solicitud.servicios = MapConceptsToSITServices(paymentRequest.Concepts);
      solicitud.tramite = paymentRequest.BaseTransactionUID;

      return solicitud;
    }

    private static List<OrdenDto> MapConceptsToSITServices(IEnumerable<PaymentOrderRequestConceptDto> concepts) {
      List<OrdenDto> services = new List<OrdenDto>();

      foreach (PaymentOrderRequestConceptDto concept in concepts) {
        OrdenDto sitService = new OrdenDto();
        sitService.idServicio = Convert.ToInt32(concept.ConceptUID);
        sitService.cantidad = Convert.ToInt32(concept.Quantity);
        services.Add(sitService);
      }

      return services;
    }


    private static async Task<string> GetFormatPaymentURL(string electronicPaymentUIDaymentId) {
      int idPagoElectronico = Convert.ToInt32(electronicPaymentUIDaymentId);
      return await ApiClient.GetPaymentFormat(idPagoElectronico);
    }


    private static async Task<ServicioDto> GetSITService(string serviceUID) {
      List<ServicioDto> sitServices = await GetSITServices();

      var SITService = sitServices.Find(x => x.idServicio == Convert.ToInt32(serviceUID));

      if (SITService == null) {
        throw new Exception($"The services with UID={serviceUID} is not finded");
      }

      return SITService;
    }

    private static SITPaymentDto MapSITPaymentToPayment(PagoDto SITPayment) {
      SITPaymentDto payment = new SITPaymentDto();

      payment.PaymentUID = SITPayment.IdCobro.ToString();
      payment.PaymentDate = SITPayment.fechaCobro;
      payment.PaymentDocumentURL = SITPayment.urlRecibo;
      payment.Total = Convert.ToDecimal(SITPayment.total);
      payment.Status = SITPayment.estatus;

      return payment;
    }

    private static PaymentOrderDto MapSITOrdenPagoToPaymentOrderRequest(OrdenPagoDto ordenPago) {
      PaymentOrderDto paymentOrder = new PaymentOrderDto();

      paymentOrder.UID = ordenPago.idPagoElectronico.ToString();

      DateTime dateValue;

      if (DateTime.TryParse(ordenPago.fechaGeneracion, out dateValue)) {
        paymentOrder.IssueTime = dateValue;
      }

      if (DateTime.TryParse(ordenPago.fechaVencimiento, out dateValue)) {
        paymentOrder.DueDate = dateValue;
      }

      paymentOrder.Total = ordenPago.total;

      paymentOrder.Status = ordenPago.idEstatus.ToString();

      paymentOrder.AddAttribute("PaymentFormatUrl", ordenPago.urlFormatoPago);

      return paymentOrder;
    }


    private static async Task<List<ServicioDto>> GetSITServices() {
      if ((services == null) || (services.Count == 0)) {
        services = await ApiClient.GetServicesList();
      }

      return services;
    }

    #endregion Private Methods

  } // class Mapper

} // namespace Empiria.Zacatecas.Integration.SITFinanzasConnector.Adapters