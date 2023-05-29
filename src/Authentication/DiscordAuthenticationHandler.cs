using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace OoLunar.PollMaster3000.Authentication
{
    public sealed class DiscordAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly byte[] _publicKey;

        public DiscordAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IConfiguration configuration) : base(options, logger, encoder, clock)
        {
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
            byte[] publicKey = new byte[32];
            FromHex(configuration["Discord:PublicKey"] ?? throw new ArgumentException("The passed configuration does not contain a public key.", nameof(configuration)), publicKey);
            _publicKey = publicKey;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Signature-Timestamp", out StringValues timestamp) || timestamp.Count != 1 || !Request.Headers.TryGetValue("X-Signature-Ed25519", out StringValues signature) || signature.Count != 1)
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing authentication headers"));
            }

            Request.EnableBuffering();
            // TODO: Make use of the PipelineReader instead of ReadToEnd
            StreamReader reader = new(Request.Body);
            string content = reader.ReadToEnd();
            Request.Body.Position = 0;

            Span<byte> signatureSpan = stackalloc byte[64];
            FromHex(signature[0], signatureSpan);
            return Task.FromResult(!Ed25519.Verify(signatureSpan, Encoding.UTF8.GetBytes($"{timestamp}{content}"), _publicKey)
                ? AuthenticateResult.Fail("Invalid authentication headers")
                : AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("Discord", timestamp[0]!) }, "Discord")), "Discord")));
        }

        private static void FromHex(ReadOnlySpan<char> hex, Span<byte> destination)
        {
            if ((hex.Length & 1) == 1)
            {
                throw new ArgumentException("Hex string must have an even number of characters.");
            }
            else if (destination.Length < hex.Length / 2)
            {
                throw new ArgumentException("Destination buffer is too small.");
            }

            for (int i = 0, j = 0; i < hex.Length; i += 2, j++)
            {
                byte highNibble = HexCharToByte(hex[i]);
                byte lowNibble = HexCharToByte(hex[i + 1]);
                destination[j] = (byte)((highNibble << 4) | lowNibble);
            }
        }

        private static byte HexCharToByte(char c) => c switch
        {
            >= '0' and <= '9' => (byte)(c - '0'),
            >= 'a' and <= 'f' => (byte)(c - 'a' + 10),
            >= 'A' and <= 'F' => (byte)(c - 'A' + 10),
            _ => throw new ArgumentException($"Invalid hex character '{c}'."),
        };
    }
}
