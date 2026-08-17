using System.Security.Cryptography;
using MediCita.Application.Abstracciones;

namespace MediCita.Infrastructure.Seguridad;

/// <summary>
/// Hash de contraseñas con PBKDF2-SHA256 y sal aleatoria por usuario. El formato
/// almacenado es "iteraciones.sal.hash" en Base64, todo en una sola columna.
/// </summary>
public sealed class HasheadorPbkdf2 : IHasheadorDeContrasenas
{
    private const int Iteraciones = 100_000;
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;

    public string Hashear(string contrasena)
    {
        if (string.IsNullOrWhiteSpace(contrasena))
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(contrasena));

        var sal = RandomNumberGenerator.GetBytes(TamanoSal);
        var hash = Derivar(contrasena, sal, Iteraciones);

        return $"{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verificar(string contrasena, string hashAlmacenado)
    {
        if (string.IsNullOrWhiteSpace(contrasena) || string.IsNullOrWhiteSpace(hashAlmacenado))
            return false;

        var partes = hashAlmacenado.Split('.', 3);
        if (partes.Length != 3 || !int.TryParse(partes[0], out var iteraciones))
            return false;

        try
        {
            var sal = Convert.FromBase64String(partes[1]);
            var esperado = Convert.FromBase64String(partes[2]);
            var calculado = Derivar(contrasena, sal, iteraciones);

            // Comparación en tiempo constante para no filtrar información por el tiempo de respuesta.
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Derivar(string contrasena, byte[] sal, int iteraciones) =>
        Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA256, TamanoHash);
}
