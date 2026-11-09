using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Controls
{
    /// <summary>
    /// Line above or below an item, showing where a dragged column will be dropped
    /// </summary>
    internal class InsertionAdorner : Adorner
    {
        private readonly Pen _pen;
        private readonly bool _isAfter;

        public InsertionAdorner(UIElement adornedElement, bool isAfter, Brush brush)
            : base(adornedElement)
        {
            _isAfter = isAfter;
            _pen = new Pen(brush, 2);
            _pen.Freeze();
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var size = AdornedElement.RenderSize;
            var y = _isAfter ? size.Height - 1 : 1;
            drawingContext.DrawLine(_pen, new Point(0, y), new Point(size.Width, y));
            // Small ticks at both ends make the line easier to spot
            drawingContext.DrawLine(_pen, new Point(0, y - 4), new Point(0, y + 4));
            drawingContext.DrawLine(_pen, new Point(size.Width, y - 4), new Point(size.Width, y + 4));
        }
    }
}
