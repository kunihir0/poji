using System.Windows.Forms;

namespace poji
{
    internal static class ControlExtensions
    {
        public static void SetRowPadding(this TableLayoutPanel panel, int row, int padding)
        {
            if (row < 0 || row >= panel.RowCount)
                return;

            foreach (Control control in panel.Controls)
            {
                if (panel.GetRow(control) == row)
                {
                    control.Margin = new Padding(
                        control.Margin.Left,
                        padding,
                        control.Margin.Right,
                        padding);
                }
            }
        }
    }
}
