using System.Windows.Forms;

namespace poji.Controls.Extensions
{
    /// <summary>
    /// Extension methods for Windows Forms controls.
    /// </summary>
    internal static class ControlExtensions
    {
        /// <summary>
        /// Sets padding for all controls in a specific row of a TableLayoutPanel.
        /// </summary>
        /// <param name="panel">The TableLayoutPanel to modify.</param>
        /// <param name="row">The zero-based row index.</param>
        /// <param name="padding">The padding value to apply.</param>
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