using System;
using System.Security.Cryptography;

namespace Application.Services.Security
{
    /// <summary>
    /// Hashing de senha com salt por usuário. Substitui o uso anterior de
    /// SHA256 sem salt (vulnerável a rainbow tables e ataques de dicionário).
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>Gera um hash auto-descritivo: "{iterations}.{saltBase64}.{hashBase64}".</summary>
        string Hash(string password);

        /// <summary>Verifica a senha contra o hash em tempo constante.</summary>
        bool Verify(string password, string hash);
    }

    /// <summary>
    /// Implementação PBKDF2 (HMAC-SHA256) usando primitivas do .NET, sem
    /// dependências externas. Formato self-contained permite evoluir
    /// iterações/algoritmo sem quebrar hashes existentes.
    /// </summary>
    public sealed class Pbkdf2PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;        // 128 bits
        private const int KeySize = 32;         // 256 bits
        private const int Iterations = 100_000;
        private const char Delimiter = '.';
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public string Hash(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Senha não pode ser vazia", nameof(password));

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

            return string.Join(Delimiter,
                Iterations,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(key));
        }

        public bool Verify(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            var parts = hash.Split(Delimiter);
            if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
                return false;

            try
            {
                var salt = Convert.FromBase64String(parts[1]);
                var key = Convert.FromBase64String(parts[2]);
                var attempted = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, key.Length);
                return CryptographicOperations.FixedTimeEquals(attempted, key);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
