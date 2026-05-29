using System;
using System.Drawing;
using System.Windows.Forms;

namespace NT8AutoLogin
{
    /// <summary>Collects NinjaTrader credentials on first use or after reset.</summary>
    internal sealed class CredentialsForm : Form
    {
        private readonly TextBox _user = new TextBox();
        private readonly TextBox _pass = new TextBox { UseSystemPasswordChar = true };

        internal string Username => _user.Text.Trim();
        internal string Password => _pass.Text;

        public CredentialsForm()
        {
            Text = "Enter Sign-In Credentials";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(360, 128);
            Padding = new Padding(12);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            table.Controls.Add(new Label { Text = "Username (optional)", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            table.Controls.Add(_user, 1, 0);
            table.Controls.Add(new Label { Text = "Password", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
            table.Controls.Add(_pass, 1, 1);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0),
            };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);

            AcceptButton = ok;
            CancelButton = cancel;

            Controls.Add(table);
            Controls.Add(buttons);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _user.Focus();
        }
    }
}
