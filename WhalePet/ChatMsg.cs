using System.ComponentModel;
using System.Windows;

namespace WhalePet
{
    public class ChatMsg : INotifyPropertyChanged
    {
        private string _text = "";

        public string Role { get; set; }
        public string Text
        {
            get => _text;
            set { _text = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text))); }
        }
        public string Meta { get; set; } = "";
        public HorizontalAlignment Align { get; set; } = HorizontalAlignment.Left;

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ActivityMsg
    {
        public string Time { get; set; } = "";
        public string Text { get; set; } = "";
    }
}
