using System.Windows;

namespace WhalePet
{
    public class ChatMsg
    {
        public string Role { get; set; }
        public string Text { get; set; }
        public string Meta { get; set; } = "";
        public HorizontalAlignment Align { get; set; } = HorizontalAlignment.Left;
    }

    public class ActivityMsg
    {
        public string Time { get; set; } = "";
        public string Text { get; set; } = "";
    }
}
