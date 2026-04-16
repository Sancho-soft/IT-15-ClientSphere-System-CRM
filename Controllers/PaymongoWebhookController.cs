using ClientSphere.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using ClientSphere.Services;

namespace ClientSphere.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymongoWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymongoWebhookController> _logger;
        private readonly INotificationService _notificationService;

        public PaymongoWebhookController(ApplicationDbContext context, IConfiguration configuration, ILogger<PaymongoWebhookController> logger, INotificationService notificationService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<IActionResult> Receive()
        {
            // Read raw body
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                rawBody = await reader.ReadToEndAsync();
            }

            // Log receipt (safe - no secret key or raw body logged)
            _context.AuditLogs.Add(new ClientSphere.Models.AuditLog
            {
                Action = "Webhook Received",
                Description = $"PayMongo webhook received. Event header present: {Request.Headers.ContainsKey("Paymongo-Signature")}",
                Timestamp = DateTime.UtcNow,
                UserId = "Webhook",
                UserName = "System",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            });
            await _context.SaveChangesAsync();

            // Verify the webhook signature — fail CLOSED if secret is missing
            var webhookSecret = _configuration["Paymongo:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogError("Paymongo:WebhookSecret is not configured. Rejecting all webhook calls.");
                return StatusCode(500, "Webhook not configured.");
            }

            var signature = Request.Headers["Paymongo-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature) || !IsValidSignature(rawBody, signature, webhookSecret))
            {
                _logger.LogWarning("PayMongo webhook received with invalid signature.");

                _context.AuditLogs.Add(new ClientSphere.Models.AuditLog
                {
                    Action = "Webhook Failed Signature",
                    Description = "Expected match failed for signature.",
                    Timestamp = DateTime.UtcNow,
                    UserId = "Webhook",
                    UserName = "System",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
                });
                await _context.SaveChangesAsync();

                return Unauthorized("Invalid signature.");
            }

            // Parse the event
            JObject payload;
            try
            {
                payload = JObject.Parse(rawBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse PayMongo webhook payload.");
                return BadRequest("Invalid JSON.");
            }

            var eventType = payload["data"]?["attributes"]?["type"]?.ToString();
            _logger.LogInformation("PayMongo webhook received: {EventType}", eventType);

            // Handle link.payment.paid — fires when a Payment Link is paid
            if (eventType == "link.payment.paid")
            {
                var linkAttributes = payload["data"]?["attributes"]?["data"]?["attributes"];
                var description = linkAttributes?["description"]?.ToString(); // e.g., "Invoice INV-2025-1234"
                
                string methodString = "PayMongo";
                try
                {
                    var paymentsArray = linkAttributes?["payments"] as JArray;
                    if (paymentsArray != null && paymentsArray.Count > 0)
                    {
                        var firstPayment = paymentsArray[0];
                        var sourceType = firstPayment["data"]?["attributes"]?["source"]?["type"]?.ToString();
                        if (!string.IsNullOrEmpty(sourceType))
                        {
                            string neatSource = char.ToUpper(sourceType[0]) + sourceType.Substring(1);
                            if (neatSource.Equals("Gcash", StringComparison.OrdinalIgnoreCase)) neatSource = "GCash";
                            if (neatSource.Equals("Paymaya", StringComparison.OrdinalIgnoreCase)) neatSource = "PayMaya";
                            if (neatSource.Equals("Card", StringComparison.OrdinalIgnoreCase)) neatSource = "Credit Card";
                            methodString = neatSource;
                        }
                    }
                }
                catch { }

                // Extract the invoice number from description ("Invoice INV-2025-1234")
                if (!string.IsNullOrEmpty(description))
                {
                    // Try to get transaction ID from inside payments array
                    string? txId = null;
                    try {
                        var paymentsArr = linkAttributes?["payments"] as JArray;
                        if (paymentsArr != null && paymentsArr.Count > 0)
                            txId = paymentsArr[0]?["data"]?["id"]?.ToString();
                    } catch { }
                    await UpdateInvoiceStatusViaWebhookAsync(description, methodString, txId);
                }
            }
            // Handle payment.paid — fires when a Payment Intent is paid (GCash, cards, etc.)
            else if (eventType == "payment.paid")
            {
                var paymentAttributes = payload["data"]?["attributes"]?["data"]?["attributes"];
                var description = paymentAttributes?["description"]?.ToString();
                var sourceType = paymentAttributes?["source"]?["type"]?.ToString();
                
                string methodString = "PayMongo";
                if (!string.IsNullOrEmpty(sourceType)) {
                   string neatSource = char.ToUpper(sourceType[0]) + sourceType.Substring(1);
                   if (neatSource.Equals("Gcash", StringComparison.OrdinalIgnoreCase)) neatSource = "GCash";
                   if (neatSource.Equals("Paymaya", StringComparison.OrdinalIgnoreCase)) neatSource = "PayMaya";
                   if (neatSource.Equals("Card", StringComparison.OrdinalIgnoreCase)) neatSource = "Credit Card";
                   methodString = neatSource;
                }

                if (!string.IsNullOrEmpty(description))
                {
                    // Try to get transaction ID from payment data
                    var txId = payload["data"]?["attributes"]?["data"]?["id"]?.ToString();
                    await UpdateInvoiceStatusViaWebhookAsync(description, methodString, txId);
                }
            }

            return Ok();
        }

        private async Task UpdateInvoiceStatusViaWebhookAsync(string description, string paymentMethod, string? transactionId = null)
        {
            // Description is "Invoice INV-XXXX-XXXX" or "Payment for Invoice INV-XXXX-XXXX"
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => !string.IsNullOrEmpty(i.InvoiceNumber) && description.Contains(i.InvoiceNumber));

            if (invoice != null && invoice.Status != "Paid")
            {
                // Set to Processing — billing staff must confirm before marking as Paid
                invoice.Status = "Processing";
                if (!string.IsNullOrEmpty(paymentMethod))
                    invoice.PaymentMethod = paymentMethod;
                else if (string.IsNullOrEmpty(invoice.PaymentMethod))
                    invoice.PaymentMethod = "PayMongo";

                if (!string.IsNullOrEmpty(transactionId))
                    invoice.TransactionId = transactionId;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Invoice {InvoiceNumber} set to Processing via PayMongo webhook.", invoice.InvoiceNumber);

                // Notify all Billing Staff
                await _notificationService.CreateForRoleAsync(
                    "Billing Staff",
                    "Payment Received — Awaiting Confirmation",
                    $"Invoice {invoice.InvoiceNumber} for {invoice.Customer?.ContactName ?? "a customer"} was paid via {invoice.PaymentMethod}. Please confirm.",
                    "/Billing",
                    "Billing"
                );
                // Also notify admins
                await _notificationService.CreateForRoleAsync(
                    "Admin",
                    "Payment Received",
                    $"Invoice {invoice.InvoiceNumber} paid via {invoice.PaymentMethod} — awaiting billing confirmation.",
                    "/Billing",
                    "Billing"
                );
            }
            else if (invoice == null)
            {
                _logger.LogWarning("PayMongo webhook: Could not find invoice matching description: {Description}", description);
            }
        }

        private static bool IsValidSignature(string rawBody, string signatureHeader, string secret)
        {
            // PayMongo signature format: "t=TIMESTAMP,te=HASH,li=HASH,v1=HASH"
            // PayMongo sends the actual HMAC in 'v1=', with 'te=' and 'li=' sometimes empty.
            // We verify using HMAC-SHA256: sign "TIMESTAMP.BODY" with the webhook secret.
            try
            {
                var parts = signatureHeader.Split(',');
                var timestamp = parts.FirstOrDefault(p => p.StartsWith("t=") && !p.StartsWith("te="))?.Substring(2);

                // Check v1= first (current PayMongo format), then fall back to te= and li=
                var hash = parts.FirstOrDefault(p => p.StartsWith("v1=") && p.Length > 3)?.Substring(3)
                        ?? parts.FirstOrDefault(p => p.StartsWith("te=") && p.Length > 3)?.Substring(3)
                        ?? parts.FirstOrDefault(p => p.StartsWith("li=") && p.Length > 3)?.Substring(3);

                if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(hash))
                    return false;

                var toSign = $"{timestamp}.{rawBody}";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var computedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(toSign))).ToLower();

                return computedHash == hash;
            }
            catch
            {
                return false;
            }
        }
    }
}
