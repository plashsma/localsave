# Trusted publisher signing

The supplied 2.8.0 preview remains **unsigned**. Signing support is prepared;
no publicly trusted certificate has been supplied. A self-signed certificate
does not establish public Windows trust. Do not disable protections or install
a custom root certificate to run this app.

Obtain a trusted code-signing identity from an eligible certificate authority.
Check country eligibility, identity verification, hardware/cloud key requirements
and fees before purchasing. Azure Artifact Signing currently limits individual
eligibility to the USA and Canada; do not assume eligibility in Bangladesh.
This build supports keys available through the Windows certificate store,
including compatible hardware-token providers. Cloud-only signing clients need
their own integration. Never commit private keys, passwords or token PINs.

## Signed build

Install the official Windows SDK SignTool, configure the provider's protected
key storage, and find the certificate thumbprint in CurrentUser\My. Then run:

```powershell
.\scripts\build-installer.ps1 `
  -CompilerPath 'C:\Tools\Inno Setup\ISCC.exe' `
  -SignToolPath 'C:\Tools\Windows SDK\signtool.exe' `
  -CertificateThumbprint 'YOUR_40_HEX_CHARACTER_THUMBPRINT' `
  -RequireSigning
```

Use `-MachineCertificateStore` for LocalMachine\My. The build checks validity,
key access, code-signing usage, trust and revocation; rejects self-signed
certificates; runs app/UI checks; signs the app; and configures Inno Setup to
sign Setup and its uninstaller. It uses SHA-256 and RFC3161 timestamps, verifies
the final app/Setup signatures, signer and timestamps, and regenerates checksums
after signing. Any signing failure stops the build. Verify the installed
uninstaller's signature in an isolated acceptance install before publishing.

The default HTTPS timestamp service is DigiCert's. Set `-TimestampUrl` if your
provider requires another HTTPS RFC3161 endpoint. `-RequireSigning` prevents an
unsigned build when no identity is supplied. Without it, unsigned development
builds are allowed with an explicit warning. End-to-end trusted signing remains
untested until a certificate is available.

Authenticode identifies the publisher and protects executable integrity.
SmartScreen also evaluates reputation, and antivirus may still flag a signed
file. OV and EV do not guarantee warning-free installation. Consistent publisher
signing helps build reputation. Microsoft Store MSIX distribution is a separate
packaging/submission option, not part of this EXE installer.

References: [Microsoft signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options),
[SignTool](https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool),
[Inno Setup signing](https://jrsoftware.org/ishelp/topic_setup_signtool.htm).
