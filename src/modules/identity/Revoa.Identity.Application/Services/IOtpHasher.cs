namespace Revoa.Identity.Application.Services;

// Hash de OTPs de uso único (login por e-mail, verificação de telefone).
// Implementação: HMAC-SHA256 com chave secreta do SERVIDOR (nunca no banco) —
// um OTP tem só 900k combinações: hash sem chave (SHA256 puro) é quebrável
// offline em segundos por quem tem leitura do banco. O domínio (User) não faz
// cripto: recebe o hash pronto e só compara (FixedTimeEquals).
public interface IOtpHasher
{
    string Hash(string otp);
}
