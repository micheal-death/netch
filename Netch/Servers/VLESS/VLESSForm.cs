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

        CreateTextBox("REALITYFingerprint", "REALITY Fingerprint", s => true, s => server.REALITYFingerprint = s, server.REALITYFingerprint);
        CreateTextBox("REALITYPublicKey",
            "REALITY Public Key",
            s => !IsRealitySelected() || !string.IsNullOrWhiteSpace(s),
            s => server.REALITYPublicKey = s,
            server.REALITYPublicKey);
        CreateTextBox("REALITYShortId",
            "REALITY Short ID",
            IsValidRealityShortId,
            s => server.REALITYShortId = s,
            server.REALITYShortId);
        CreateTextBox("REALITYSpiderX", "REALITY SpiderX", s => true, s => server.REALITYSpiderX = s, server.REALITYSpiderX);

        var tlsSecureComboBox = GetTLSSecureComboBox();
        if (tlsSecureComboBox != null)
        {
            tlsSecureComboBox.SelectedIndexChanged += OnTLSSecureChanged;
            OnTLSSecureChanged(null, EventArgs.Empty);
        }
    }

    protected override string TypeName { get; } = "VLESS";

    private void OnTLSSecureChanged(object? sender, EventArgs e)
    {
        bool isReality = IsRealitySelected();

        if (isReality && ConfigurationGroupBox.Controls.Find("REALITYFingerprintTextBox", true).FirstOrDefault() is TextBox fingerprintTextBox &&
            string.IsNullOrWhiteSpace(fingerprintTextBox.Text))
        {
            fingerprintTextBox.Text = "chrome";
        }

        var realityFieldBaseNames = new[] { "REALITYPublicKey", "REALITYFingerprint", "REALITYShortId", "REALITYSpiderX" };

        foreach (var baseName in realityFieldBaseNames)
        {
            string inputControlName = baseName + "TextBox";
            string labelName = baseName + "Label";

            var inputControl = ConfigurationGroupBox.Controls.Find(inputControlName, true).FirstOrDefault();
            var labelControl = ConfigurationGroupBox.Controls.Find(labelName, true).FirstOrDefault();

            if (inputControl != null)
            {
                inputControl.Visible = isReality;
            }

            if (labelControl != null)
            {
                labelControl.Visible = isReality;
            }
        }
    }

    private ComboBox? GetTLSSecureComboBox()
    {
        return ConfigurationGroupBox.Controls.Find("TLSSecureComboBox", true).FirstOrDefault() as ComboBox;
    }

    private bool IsRealitySelected()
    {
        return GetTLSSecureComboBox()?.SelectedItem?.ToString() == "reality";
    }

    private bool IsValidRealityShortId(string value)
    {
        if (!IsRealitySelected() || string.IsNullOrWhiteSpace(value))
            return true;

        return value.Length % 2 == 0 && value.All(Uri.IsHexDigit);
    }
}
