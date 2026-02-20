using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stripe;
using Stripe.Checkout;
using ClientSphere.Data;
using ClientSphere.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClientSphere.Services
{
    public class StripePaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public StripePaymentService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<string> CreatePaymentLinkAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
                throw new ArgumentException("Invoice not found");

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Invoice {invoice.InvoiceNumber}",
                                Description = $"Payment for invoice {invoice.InvoiceNumber}"
                            },
                            UnitAmount = (long)(invoice.Amount * 100), // Convert to cents
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = _configuration["Stripe:SuccessUrl"] ?? "https://localhost:7000/billing/payment-success",
                CancelUrl = _configuration["Stripe:CancelUrl"] ?? "https://localhost:7000/billing/payment-cancelled",
                Metadata = new Dictionary<string, string>
                {
                    { "invoice_id", invoiceId.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            // Store session ID in invoice for tracking
            invoice.PaymentMethod = $"Stripe:{session.Id}";
            await _context.SaveChangesAsync();

            return session.Url;
        }

        public async Task<bool> ProcessWebhookAsync(string payload, string signature)
        {
            try
            {
                var webhookSecret = _configuration["Stripe:WebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(
                    payload,
                    signature,
                    webhookSecret
                );

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session?.Metadata.ContainsKey("invoice_id") == true)
                    {
                        var invoiceId = int.Parse(session.Metadata["invoice_id"]);
                        var invoice = await _context.Invoices.FindAsync(invoiceId);
                        
                        if (invoice != null)
                        {
                            invoice.Status = "Paid";
                            invoice.PaidDate = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (StripeException)
            {
                return false;
            }
        }

        public async Task<string> GetPaymentStatusAsync(string paymentIntentId)
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);
            return paymentIntent.Status;
        }
    }
}
