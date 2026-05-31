using System.Drawing;
using System.Windows.Forms;

namespace DriveRecorderConverter
{
    /// <summary>
    /// A CheckBox that fills the tick box with a custom colour when checked and enabled.
    /// </summary>
    internal class ColoredCheckBox : CheckBox
    {
        private static readonly Color CheckedFill = Color.FromArgb(0x31, 0x62, 0xA9);
        private static readonly Color CheckedBorder = Color.FromArgb(0x1A, 0x47, 0x80);
        private static readonly Color DisabledFill = Color.FromArgb(0xCC, 0xCC, 0xCC);
        private static readonly Color DisabledBorder = Color.FromArgb(0xAA, 0xAA, 0xAA);

        public ColoredCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            const int boxSize = 16;
            const int gap = 4;
            Size textSize = TextRenderer.MeasureText(Text, Font);
            return new Size(boxSize + gap + textSize.Width + 4, Math.Max(boxSize, textSize.Height) + 4);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);

            const int boxSize = 16;
            int boxLeft = 2;

            // Vertical centre alignment
            int centreOffset = (Height - boxSize) / 2;
            Rectangle boxRect = new Rectangle(boxLeft, centreOffset, boxSize, boxSize);

            if (Checked && Enabled)
            {
                // Filled background
                using var fillBrush = new SolidBrush(CheckedFill);
                e.Graphics.FillRectangle(fillBrush, boxRect);

                // Border
                using var borderPen = new Pen(CheckedBorder);
                e.Graphics.DrawRectangle(borderPen, boxRect);

                // White tick
                using var tickPen = new Pen(Color.White, 2f);
                e.Graphics.DrawLines(tickPen, new[]
                {
                    new Point(boxRect.Left + 3, boxRect.Top + boxSize / 2),
                    new Point(boxRect.Left + 6, boxRect.Bottom - 4),
                    new Point(boxRect.Right - 3, boxRect.Top + 3)
                });
            }
            else if (Checked && !Enabled)
            {
                using var fillBrush = new SolidBrush(DisabledFill);
                e.Graphics.FillRectangle(fillBrush, boxRect);

                using var borderPen = new Pen(DisabledBorder);
                e.Graphics.DrawRectangle(borderPen, boxRect);

                using var tickPen = new Pen(Color.White, 2f);
                e.Graphics.DrawLines(tickPen, new[]
                {
                    new Point(boxRect.Left + 3, boxRect.Top + boxSize / 2),
                    new Point(boxRect.Left + 6, boxRect.Bottom - 4),
                    new Point(boxRect.Right - 3, boxRect.Top + 3)
                });
            }
            else
            {
                // Unchecked — draw standard empty box
                Color borderCol = Enabled ? SystemColors.ActiveBorder : SystemColors.InactiveBorder;
                using var borderPen = new Pen(borderCol);
                e.Graphics.FillRectangle(SystemBrushes.Window, boxRect);
                e.Graphics.DrawRectangle(borderPen, boxRect);
            }

            // Draw label text
            Color textCol = Enabled ? ForeColor : SystemColors.GrayText;
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                new Rectangle(boxLeft + boxSize + 4, 0, Width - boxSize - 8, Height),
                textCol,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }
}
