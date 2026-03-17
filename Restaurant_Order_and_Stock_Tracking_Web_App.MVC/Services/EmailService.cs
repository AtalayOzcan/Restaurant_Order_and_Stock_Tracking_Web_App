// ============================================================================
//  Services/EmailService.cs
//
//  SPRINT 5 — [OTP-2] E-posta Altyapısı
//
//  Kararlar:
//    • Channel<EmailMessage> + IHostedService — .NET native, sıfır paket
//    • Gmail SMTP — başlangıç için (ileride sadece IGmailEmailSender → IResendEmailSender)
//    • Retry yok — başarısız → logla, kullanıcı tekrar ister
//
//  Program.cs kaydı:
//    builder.Services.AddSingleton<IEmailChannel, EmailChannel>();
//    builder.Services.AddScoped<IEmailSender, GmailEmailSender>();
//    builder.Services.AddHostedService<EmailBackgroundService>();
//
//  appsettings.json / ortam değişkenleri:
//    Email:SenderAddress   → gönderici adres (örn: noreply@restaurantos.com)
//    Email:SenderName      → görünen ad (RestaurantOS)
//    Email:SmtpHost        → smtp.gmail.com
//    Email:SmtpPort        → 587
//    Email:SmtpUser        → Gmail adresi
//    Email:SmtpPassword    → Gmail App Password (hesap şifresi değil!)
// ============================================================================

using System.Net;
using System.Net.Mail;
using System.Threading.Channels;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Services
{
    // ── Mesaj DTO'su ─────────────────────────────────────────────────────────
    public record EmailMessage(
        string To,
        string Subject,
        string HtmlBody
    );

    // ── Channel interface — Singleton ─────────────────────────────────────────
    public interface IEmailChannel
    {
        void Enqueue(EmailMessage message);
        Channel<EmailMessage> Channel { get; }
    }

    public class EmailChannel : IEmailChannel
    {
        public Channel<EmailMessage> Channel { get; } =
            System.Threading.Channels.Channel.CreateBounded<EmailMessage>(
                new BoundedChannelOptions(200)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false
                });

        public void Enqueue(EmailMessage message)
        {
            // TryWrite başarısız olursa (kapasite doluysa) log DropOldest ile eski mesaj düşer
            Channel.Writer.TryWrite(message);
        }
    }

    // ── IEmailSender interface ───────────────────────────────────────────────
    public interface IEmailSender
    {
        /// <summary>
        /// E-postayı kuyruğa ekler. Anında döner — gönderim arka planda yapılır.
        /// </summary>
        void EnqueueEmail(string to, string subject, string htmlBody);

        /// <summary>
        /// Direkt SMTP ile gönderir (BackgroundService tarafından çağrılır).
        /// </summary>
        Task SendDirectAsync(EmailMessage message, CancellationToken ct = default);
    }

    // ── Gmail SMTP implementasyonu ────────────────────────────────────────────
    public class GmailEmailSender : IEmailSender
    {
        private readonly IEmailChannel _channel;
        private readonly IConfiguration _config;
        private readonly ILogger<GmailEmailSender> _logger;

        public GmailEmailSender(
            IEmailChannel channel,
            IConfiguration config,
            ILogger<GmailEmailSender> logger)
        {
            _channel = channel;
            _config = config;
            _logger = logger;
        }

        // ── Kuyruğa ekle (Controller'dan çağrılır) ───────────────────────────
        public void EnqueueEmail(string to, string subject, string htmlBody)
        {
            _channel.Enqueue(new EmailMessage(to, subject, htmlBody));
            _logger.LogDebug("[EMAIL] Kuyruğa eklendi → {To} | Konu: {Subject}", to, subject);
        }

        // ── Direkt gönderim (BackgroundService'ten çağrılır) ─────────────────
        public async Task SendDirectAsync(EmailMessage message, CancellationToken ct = default)
        {
            var host = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var user = _config["Email:SmtpUser"] ?? throw new InvalidOperationException("Email:SmtpUser eksik.");
            var password = _config["Email:SmtpPassword"] ?? throw new InvalidOperationException("Email:SmtpPassword eksik.");
            var from = _config["Email:SenderAddress"] ?? user;
            var fromName = _config["Email:SenderName"] ?? "RestaurantOS";

            // ── GEÇICI DEBUG ─────────────────────────────────────────────
            _logger.LogWarning(
                "[EMAIL-DEBUG] Host:{Host} Port:{Port} User:{User} PassLen:{Len} PassEmpty:{Empty}",
                host, port, user, password.Length, string.IsNullOrEmpty(password));
            // ─────────────────────────────────────────────────────────────

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, password),
                EnableSsl = true,
                DeliveryFormat = SmtpDeliveryFormat.International
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = message.Subject,
                Body = message.HtmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(message.To);

            await client.SendMailAsync(mail, ct);
            _logger.LogInformation("[EMAIL] Gönderildi → {To} | Konu: {Subject}", message.To, message.Subject);
        }
    }

    // ── BackgroundService — Channel'ı dinler ──────────────────────────────────
    public class EmailBackgroundService : BackgroundService
    {
        private readonly IEmailChannel _channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailBackgroundService> _logger;

        public EmailBackgroundService(
            IEmailChannel channel,
            IServiceScopeFactory scopeFactory,
            ILogger<EmailBackgroundService> logger)
        {
            _channel = channel;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[EMAIL] BackgroundService başladı.");

            // Channel kapanana veya uygulama durursa döngü biter
            await foreach (var message in _channel.Channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    // GmailEmailSender Scoped değil çünkü SmtpClient her seferinde yeni — direkt resolve
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                    await sender.SendDirectAsync(message, stoppingToken);
                }
                catch (Exception ex)
                {
                    // Retry yok — başarısız olursa logla
                    // Kullanıcı "Tekrar gönder"e basabilir
                    _logger.LogError(ex,
                        "[EMAIL] Gönderim başarısız → {To} | Konu: {Subject}",
                        message.To, message.Subject);
                }
            }
        }
    }

    // ── E-posta şablonları ────────────────────────────────────────────────────
    public static class EmailTemplates
    {
        public static string OtpEmail(string purpose, string code, string restaurantName = "")
        {
            var title = purpose == "register"
                ? "E-posta Doğrulama Kodu"
                : "Şifre Sıfırlama Kodu";

            var greeting = string.IsNullOrEmpty(restaurantName)
                ? "Merhaba,"
                : $"Merhaba <strong>{restaurantName}</strong>,";

            return $"""
                <!DOCTYPE html>
                <html lang="tr">
                <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
                <body style="margin:0;padding:0;background:#0F1117;font-family:'Helvetica Neue',Arial,sans-serif;">
                  <table width="100%" cellpadding="0" cellspacing="0">
                    <tr><td align="center" style="padding:40px 20px;">
                      <table width="480" cellpadding="0" cellspacing="0"
                             style="background:#161A21;border-radius:12px;border:1px solid #2A303C;overflow:hidden;">
                        <tr><td style="padding:32px 40px 24px;border-bottom:1px solid #2A303C;">
                          <span style="font-size:28px;">🍽️</span>
                          <span style="font-size:20px;font-weight:700;color:#E8EAF0;vertical-align:middle;margin-left:8px;">RestaurantOS</span>
                        </td></tr>
                        <tr><td style="padding:32px 40px;">
                          <h1 style="color:#E8EAF0;font-size:22px;font-weight:700;margin:0 0 8px;">{title}</h1>
                          <p style="color:#9CA3AF;font-size:14px;line-height:1.6;margin:0 0 24px;">{greeting}</p>
                          <p style="color:#D1D5DB;font-size:14px;line-height:1.6;margin:0 0 24px;">
                            Doğrulama kodunuz aşağıda. Bu kod <strong style="color:#F97316;">10 dakika</strong> geçerlidir.
                          </p>
                          <div style="background:#0D1117;border:1px solid #F97316;border-radius:10px;
                                      padding:24px;text-align:center;margin:0 0 24px;">
                            <span style="font-size:42px;font-weight:800;letter-spacing:12px;color:#F97316;
                                         font-family:'Courier New',monospace;">{code}</span>
                          </div>
                          <p style="color:#6B7280;font-size:12px;line-height:1.6;margin:0;">
                            Bu kodu kimseyle paylaşmayın. Eğer bu işlemi siz başlatmadıysanız bu e-postayı dikkate almayın.
                          </p>
                        </td></tr>
                        <tr><td style="padding:20px 40px;background:#0D1117;border-top:1px solid #2A303C;">
                          <p style="color:#4B5563;font-size:12px;margin:0;text-align:center;">
                            RestaurantOS · Restoranınızı Dijitalleştirin
                          </p>
                        </td></tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """;
        }
    }
}