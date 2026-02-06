using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TaskGX.ViewModels;

namespace TaskGX.Services
{
    public class EmailSender
    {
        private readonly EmailSettings _settings;
        private readonly RazorViewToStringRenderer _renderer;

        public EmailSender(
            IOptions<EmailSettings> settings,
            RazorViewToStringRenderer renderer)
        {
            _settings = settings.Value;
            _renderer = renderer;
        }

        public async Task EnviarEmailVerificacaoAsync(
            string email,
            string nome,
            string codigo,
            bool trocaSenha = false)
        {
            var expiracao = trocaSenha ? 1 : 24;

            var model = new EmailTemplateViewModel
            {
                Nome = nome,
                Codigo = codigo,
                AppName = string.IsNullOrWhiteSpace(_settings.AppName)
                    ? "TaskGX"
                    : _settings.AppName,
                TrocaSenha = trocaSenha,
                ExpiracaoHoras = expiracao
            };

            var corpoHtml = await _renderer.RenderViewToStringAsync(
                "Shared/_Email",
                model
            );

            var assunto = trocaSenha
                ? $"Verifique seu email para alterar senha - {model.AppName}"
                : $"Verifique seu email - {model.AppName}";

            var corpoTexto = trocaSenha
                ? $"Olá, {nome}!\n\n" +
                  $"Recebemos uma solicitação para alterar a senha da sua conta no {model.AppName}.\n\n" +
                  $"Código: {codigo}\n\n" +
                  $"Este código expira em {expiracao} hora(s)."
                : $"Olá, {nome}!\n\n" +
                  $"Obrigado por criar sua conta no {model.AppName}.\n\n" +
                  $"Código: {codigo}\n\n" +
                  $"Este código expira em {expiracao} hora(s).";

            await EnviarEmailAsync(
                email,
                nome,
                assunto,
                corpoHtml,
                corpoTexto
            );
        }

        private async Task EnviarEmailAsync(
            string email,
            string nome,
            string assunto,
            string corpoHtml,
            string corpoTexto)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host) ||
                string.IsNullOrWhiteSpace(_settings.Username) ||
                string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidOperationException(
                    "Configurações de email inválidas ou incompletas."
                );
            }

            using var message = new MailMessage
            {
                From = new MailAddress(
                    _settings.FromEmail,
                    _settings.FromName
                ),
                Subject = assunto,
                Body = corpoHtml,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(email, nome));
            message.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(
                    corpoTexto,
                    null,
                    "text/plain"
                )
            );

            using var client = new SmtpClient(
                _settings.Host,
                _settings.Port
            )
            {
                EnableSsl = _settings.EnableSsl,
                UseDefaultCredentials = false, // 🔴 essencial para Gmail
                Credentials = new NetworkCredential(
                    _settings.Username,
                    _settings.Password
                ),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            await client.SendMailAsync(message);
        }
    }
}
