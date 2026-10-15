using System.Windows;
using System.Windows.Controls;

namespace Modas.Grpc.Controls
{
    public static class TextBoxHelper
    {
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(TextBoxHelper),
                new PropertyMetadata(false, OnAutoScrollChanged));

        public static void SetAutoScroll(DependencyObject obj, bool value) => obj.SetValue(AutoScrollProperty, value);
        public static bool GetAutoScroll(DependencyObject obj) => (bool)obj.GetValue(AutoScrollProperty);

        private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                    textBox.TextChanged += (s, args) => textBox.ScrollToEnd();
            }
        }

        public static void SetAutoScrollToEnd(DependencyObject obj, bool value) => obj.SetValue(AutoScrollToEndProperty, value);
        public static bool GetAutoScrollToEnd(DependencyObject obj) => (bool)obj.GetValue(AutoScrollToEndProperty);

        public static readonly DependencyProperty AutoScrollToEndProperty =
            DependencyProperty.RegisterAttached("AutoScrollToEnd", typeof(bool), typeof(TextBoxHelper), new PropertyMetadata(false, (o, e) =>
            {
                var textBox = o as TextBox;
                if (textBox != null && (bool)e.NewValue)
                    textBox.TextChanged += (sender, args) =>
                    {
                        textBox.CaretIndex = textBox.Text.Length;
                        var rect = textBox.GetRectFromCharacterIndex(textBox.CaretIndex);
                        textBox.ScrollToHorizontalOffset(rect.Right);
                    };
            }));
    }
}