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
        CreateTextBox("REALITYPublicKey", "REALITY Public Key", s => true, s => server.REALITYPublicKey = s, server.REALITYPublicKey);
        CreateTextBox("REALITYShortId", "REALITY Short ID", s => true, s => server.REALITYShortId = s, server.REALITYShortId);
        CreateTextBox("REALITYSpiderX", "REALITY SpiderX", s => true, s => server.REALITYSpiderX = s, server.REALITYSpiderX);

        var tlsSecureComboBox = ConfigurationGroupBox.Controls.Find("TLSSecureComboBox", true).FirstOrDefault() as ComboBox;
        if (tlsSecureComboBox != null)
        {
            tlsSecureComboBox.SelectedIndexChanged += OnTLSSecureChanged;
            // Initial call to set visibility
            OnTLSSecureChanged(null, EventArgs.Empty);
        }
    }

    private void OnTLSSecureChanged(object? sender, EventArgs e)
    {
        var tlsSecureComboBox = ConfigurationGroupBox.Controls.Find("TLSSecureComboBox", true).FirstOrDefault() as ComboBox;
        if (tlsSecureComboBox == null) return; // Should not happen

        var selectedSecureType = tlsSecureComboBox.SelectedItem?.ToString() ?? string.Empty;
        bool isReality = selectedSecureType == "reality";

        var realityFieldBaseNames = new[] { "REALITYPublicKey", "REALITYFingerprint", "REALITYShortId", "REALITYSpiderX" };

        foreach (var baseName in realityFieldBaseNames)
        {
            // All REALITY input controls are TextBoxes now
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
}