using System.IO;
using System.Windows;
using System.Windows.Media;

namespace WhalePet
{
    /// <summary>鲸鱼娘主题:深海蓝 / 纯黑,通过 Application.Resources + DynamicResource 全局换肤。</summary>
    public static class WhaleTheme
    {
        public const string ThemeFile = "whale-theme.txt";
        public static int Current { get; private set; }

        public static void Load()
        {
            int idx = 0;
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, ThemeFile);
                if (File.Exists(path) && int.TryParse(File.ReadAllText(path).Trim(), out var v)) idx = v;
            }
            catch { }
            Apply(idx);
        }

        public static int Toggle()
        {
            Apply(Current == 0 ? 1 : 0);
            return Current;
        }

        public static void Apply(int idx)
        {
            Current = idx == 0 ? 0 : 1;
            bool dark = Current == 1;
            var r = Application.Current.Resources;
            r["WhBgWindow"] = B(dark ? "#0B0B0E" : "#101A2E");
            r["WhBgHeader"] = B(dark ? "#16161A" : "#19314F");
            r["WhBgTabs"] = B(dark ? "#101014" : "#0D1726");
            r["WhBgInputRow"] = B(dark ? "#0E0E12" : "#101E33");
            r["WhBgInput"] = B(dark ? "#1C1C22" : "#16324F");
            r["WhBorderInput"] = B(dark ? "#4A4A55" : "#4A90D9");
            r["WhBgWorkCard"] = B(dark ? "#1A1A20" : "#1B2C47");
            r["WhBgPetBubble"] = B(dark ? "#232330" : "#253654");
            r["WhBgUserBubble"] = B(dark ? "#F0F0F2" : "#FFFFFF");
            r["WhTextPrimary"] = B(dark ? "#EDEDF2" : "#EAF2FB");
            r["WhTextSecondary"] = B(dark ? "#A0A0AA" : "#8FA3C4");
            r["WhTextOnLight"] = B(dark ? "#101014" : "#1C2B45");
            r["WhAccent"] = B(dark ? "#4A90D9" : "#3A6EA5");
            r["WhTabSel"] = B(dark ? "#2A2A35" : "#2A4E73");
            r["WhCardText"] = B(dark ? "#D8D8E0" : "#DCE7FA");
            r["WhTimeText"] = B(dark ? "#80808C" : "#6E82A8");
            r["WhTitleText"] = B(dark ? "#EDEDF2" : "#EAF2FB");
            r["WhThink"] = B(dark ? "#5B9BD5" : "#5B9BD5");
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, ThemeFile), Current.ToString()); } catch { }
        }

        public static SolidColorBrush AccentBrush => (SolidColorBrush)Application.Current.Resources["WhAccent"];
        public static SolidColorBrush TabSelBrush => (SolidColorBrush)Application.Current.Resources["WhTabSel"];
        public static SolidColorBrush PrimaryTextBrush => (SolidColorBrush)Application.Current.Resources["WhTextPrimary"];
        public static SolidColorBrush SecondaryTextBrush => (SolidColorBrush)Application.Current.Resources["WhTextSecondary"];

        private static SolidColorBrush B(string hex) =>
            (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
    }
}
