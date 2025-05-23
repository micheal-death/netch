using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
internal class VLESSForm : ServerForm
{
    public VLESSForm(VLESSServer? server = default)
    {
        server ??= new VLESSServer();
        Server = server;
        CreateTextBox("Sni", "ServerName(Sni)", s => true, s => server.ServerName = s, server.ServerName);
        CreateTextBox("UUID", "UUID", s => true, s => server.UserID = s, server.UserID);
        CreateTextBox("EncryptMethod",
            "Encrypt Method",
            s => true,
            s => server.EncryptMethod = !string.IsNullOrWhiteSpace(s) ? s : "none",
            server.EncryptMethod);

        CreateComboBox("TransferProtocol",
            "Transfer Protocol",
            VLESSGlobal.TransferProtocols,
            s => server.TransferProtocol = s,
            server.TransferProtocol);
        CreateComboBox("PacketEncoding",
            "Packet Encoding",
            VMessGlobal.PacketEncodings,
            s => server.PacketEncoding = s,
            server.PacketEncoding);

        CreateComboBox("FakeType", "Fake Type", VLESSGlobal.FakeTypes, s => server.FakeType = s, server.FakeType);
        CreateTextBox("Host", "Host", s => true, s => server.Host = s, server.Host);
        CreateTextBox("Path", "Path", s => true, s => server.Path = s, server.Path);
        CreateComboBox("QUICSecurity", "QUIC Security", VLESSGlobal.QUIC, s => server.QUICSecure = s, server.QUICSecure);
        CreateTextBox("QUICSecret", "QUIC Secret", s => true, s => server.QUICSecret = s, server.QUICSecret);
        CreateComboBox("UseMux",
            "Use Mux",
            new List<string> { "", "true", "false" },
            s => server.UseMux = s switch { "" => null, "true" => true, "false" => false, _ => null },
            server.UseMux?.ToString().ToLower() ?? "");

        CreateComboBox("TLSSecure", "TLS Secure", VLESSGlobal.TLSSecure, s => server.TLSSecureType = s, server.TLSSecureType);

        CreateComboBox("REALITYFingerprint",
            "REALITY Fingerprint",
            new List<string> { "chrome", "firefox", "safari", "ios", "random" },
            s => server.REALITYFingerprint = s,
            server.REALITYFingerprint,
            true);
        CreateTextBox("REALITYPublicKey", "REALITY Public Key", s => true, s => server.REALITYPublicKey = s, server.REALITYPublicKey);
        CreateTextBox("REALITYShortId", "REALITY Short ID", s => true, s => server.REALITYShortId = s, server.REALITYShortId);
        CreateTextBox("REALITYSpiderX", "REALITY SpiderX", s => true, s => server.REALITYSpiderX = s, server.REALITYSpiderX);
    }

    protected override string TypeName { get; } = "VLESS";

    private void OnTLSSecureChanged(object? sender, EventArgs e)
    {
        var tlsSecureComboBox = ConfigurationGroupBox.Controls.Find("TLSSecureComboBox", true).FirstOrDefault() as ComboBox;
        if (tlsSecureComboBox == null) return;

        var selectedSecureType = tlsSecureComboBox.SelectedItem?.ToString() ?? string.Empty;
        bool isReality = selectedSecureType == "reality";

        var realityControls = new[] { "REALITYFingerprintComboBox", "REALITYPublicKeyTextBox", "REALITYShortIdTextBox", "REALITYSpiderXTextBox" };
        var realityLabels = new[] { "REALITYFingerprintLabel", "REALITYPublicKeyLabel", "REALITYShortIdLabel", "REALITYSpiderXLabel" };

        foreach (var controlName in realityControls)
        {
            var control = ConfigurationGroupBox.Controls.Find(controlName, true).FirstOrDefault();
            if (control != null)
            {
                control.Visible = isReality;
            }
        }

        foreach (var labelName in realityLabels)
        {
            var label = ConfigurationGroupBox.Controls.Find(labelName, true).FirstOrDefault();
            if (label != null)
            {
                label.Visible = isReality;
            }
        }
    }
}