using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;

namespace server.Infrastructure;

/// <summary>
/// sifruje master klice DataProtection pred ulozenim do Redisu pomoci AES-256-GCM.
/// klic se bezpecne odvozuje pomoci HKDF z JWT_SECRET.
/// </summary>
public sealed class DataProtectionKeyEncryptor : IXmlEncryptor {
	private readonly byte[] encryptionKey;

	public DataProtectionKeyEncryptor(JwtAuthConfiguration jwtConfig) {
		encryptionKey = DeriveKeyFromJwt(jwtConfig);
	}

	public EncryptedXmlInfo Encrypt(XElement plaintextElement) {
		ArgumentNullException.ThrowIfNull(plaintextElement);

		var plaintextBytes = Encoding.UTF8.GetBytes(plaintextElement.ToString(SaveOptions.DisableFormatting));
		var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bajtu
		RandomNumberGenerator.Fill(nonce);

		var ciphertext = new byte[plaintextBytes.Length];
		var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bajtu

		using var aesGcm = new AesGcm(encryptionKey, AesGcm.TagByteSizes.MaxSize);
		aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

		var combined = new byte[nonce.Length + tag.Length + ciphertext.Length];
		Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
		Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
		Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length + tag.Length, ciphertext.Length);

		var encryptedElement = new XElement("encryptedKey",
			new XAttribute("decryptorType", typeof(DataProtectionKeyDecryptor).AssemblyQualifiedName!),
			new XElement("payload", Convert.ToBase64String(combined))
		);

		return new EncryptedXmlInfo(encryptedElement, typeof(DataProtectionKeyDecryptor));
	}

	internal static byte[] DeriveKeyFromJwt(JwtAuthConfiguration? jwtConfig) {
		byte[] rawSecret;
		if (jwtConfig != null) {
			rawSecret = jwtConfig.SigningKey.Key;
		} else if (Program.ENV.TryGetValue("JWT_SECRET", out var secretStr) && !string.IsNullOrWhiteSpace(secretStr)) {
			rawSecret = Convert.FromBase64String(secretStr);
		} else {
			throw new InvalidOperationException("Nelze odvodit DataProtection klic: JWT_SECRET neni dostupne.");
		}

		return HKDF.DeriveKey(
			HashAlgorithmName.SHA256,
			rawSecret,
			32,
			info: "DataProtection-Redis-Encryption"u8.ToArray()
		);
	}
}

/// <summary>
/// desifruje master klice DataProtection nactene z Redisu pomoci AES-256-GCM.
/// </summary>
public sealed class DataProtectionKeyDecryptor : IXmlDecryptor {
	private readonly byte[] encryptionKey;

	public DataProtectionKeyDecryptor() : this(null) { }

	public DataProtectionKeyDecryptor(IServiceProvider? services) {
		var jwtConfig = services?.GetService<JwtAuthConfiguration>();
		encryptionKey = DataProtectionKeyEncryptor.DeriveKeyFromJwt(jwtConfig);
	}

	public XElement Decrypt(XElement encryptedElement) {
		ArgumentNullException.ThrowIfNull(encryptedElement);

		var payloadNode = encryptedElement.Element("payload") ?? encryptedElement;
		var combined = Convert.FromBase64String(payloadNode.Value.Trim());

		const int nonceSize = 12;
		const int tagSize = 16;
		if (combined.Length < nonceSize + tagSize) {
			throw new CryptographicException("Neplatna delka sifrovanych dat DataProtection klice.");
		}

		var nonce = combined.AsSpan(0, nonceSize);
		var tag = combined.AsSpan(nonceSize, tagSize);
		var ciphertext = combined.AsSpan(nonceSize + tagSize);

		var plaintextBytes = new byte[ciphertext.Length];
		using var aesGcm = new AesGcm(encryptionKey, tagSize);
		aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);

		var xmlString = Encoding.UTF8.GetString(plaintextBytes);
		return XElement.Parse(xmlString);
	}
}
