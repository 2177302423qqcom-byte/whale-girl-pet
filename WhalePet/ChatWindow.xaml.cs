using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WhalePet
{
    public partial class ChatWindow : Window
    {
        private const string Api = "http://127.0.0.1:3080/api/whale";
        private static readonly string LogDir = Path.Combine(Path.GetTempPath(), "whalepet-logs");

        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(150) };
        private readonly DispatcherTimer _pollTimer = new() { Interval = TimeSpan.FromSeconds(8) };
        private readonly ObservableCollection<ChatMsg> _msgs = new();
        private bool _busy;
        private bool _serverUp;
        private bool _introShown;

        /// <summary>鲸鱼娘说了一句话(回复),让桌宠气泡同步显示。</summary>
        public event Action<string> PetLine;

        public ChatWindow()
        {
            InitializeComponent();
            ChatLog.ItemsSource = _msgs;
            Topmost = true; // 弹出一瞬间置顶,3 秒后取消
            Loaded += async (s, a) =>
            {
                try { await Task.Delay(3000); Topmost = false; } catch { }
            };
            PinBtn.Checked += (s, a) => Topmost = true;
            PinBtn.Unchecked += (s, a) => Topmost = false;
            ThemeBtn.Click += (s, a) => WhaleTheme.Toggle();
            WorkFull.Click += (s, a) =>
            {
                try { Process.Start(new ProcessStartInfo("http://127.0.0.1:3080") { UseShellExecute = true }); } catch { }
            };
            _pollTimer.Tick += (s, a) => _ = PollAsyncSafe();
            _pollTimer.Start();
            _ = PollAsyncSafe();
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            try { _pollTimer.Stop(); _http.Dispose(); } catch { }
        }

        private void Log(string kind, Exception ex)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                File.AppendAllText(Path.Combine(LogDir, "chat.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {kind}\n{ex}\n\n");
            }
            catch { }
        }

        private void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) _ = SendSafeAsync(switchToChat: true);
        }

        private async void ChatSend_Click(object sender, RoutedEventArgs e)
        {
            try { await SendSafeAsync(switchToChat: true); }
            catch (Exception ex) { Log("ChatSend_Click", ex); }
        }

        private void TaskInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) _ = SendTaskAsync();
        }

        private async void TaskSend_Click(object sender, RoutedEventArgs e) => await SendTaskAsync();

        // ── 页签 ──
        private void TabChat_Click(object sender, RoutedEventArgs e) => ShowTab(true);
        private void TabWork_Click(object sender, RoutedEventArgs e) => ShowTab(false);

        /// <summary>桌宠按钮直达工作台页签。</summary>
        public void OpenWorkTab() => ShowTab(false);

        private void ShowTab(bool chat)
        {
            TabChatBtn.Background = chat ? WhaleTheme.TabSelBrush : Brushes.Transparent;
            TabWorkBtn.Background = chat ? Brushes.Transparent : WhaleTheme.TabSelBrush;
            TabChatBtn.Foreground = chat ? WhaleTheme.PrimaryTextBrush : WhaleTheme.SecondaryTextBrush;
            TabWorkBtn.Foreground = chat ? WhaleTheme.SecondaryTextBrush : WhaleTheme.PrimaryTextBrush;
            ChatLog.Visibility = chat ? Visibility.Visible : Visibility.Collapsed;
            WorkView.Visibility = chat ? Visibility.Collapsed : Visibility.Visible;
            ChatInputRow.Visibility = chat ? Visibility.Visible : Visibility.Collapsed;
            if (!chat) { _ = LoadActivitiesAsync(); TaskInput.Focus(); }
        }

        // ── 工作台(简化版):任务下达 + 活动流 ──
        private async Task SendTaskAsync()
        {
            var text = TaskInput.Text.Trim();
            if (text.Length == 0 || _busy) return;
            TaskInput.Text = "";
            await RunChat(text, addUserMsg: true, showThinking: true, switchToChat: false);
            await LoadActivitiesAsync();
        }

        // ── 聊天 ──
        private async Task SendSafeAsync(bool switchToChat)
        {
            var text = ChatInput.Text.Trim();
            if (text.Length == 0 || _busy) return;
            ChatInput.Text = "";
            await RunChat(text, addUserMsg: true, showThinking: true, switchToChat: switchToChat);
        }

        private async Task RunChat(string text, bool addUserMsg, bool showThinking, bool switchToChat, bool retried = false)
        {
            if (_busy) return;
            if (switchToChat) ShowTab(true);
            if (addUserMsg) AddMsg("user", text);
            if (showThinking) StartThinking();
            _busy = true;
            ChatSend.IsEnabled = false;
            TaskSend.IsEnabled = false;
            SetState("· 思考中…", new SolidColorBrush(Color.FromRgb(0x5B, 0x9B, 0xD5)));
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, Api + "/chat");
                req.Content = new StringContent(JsonSerializer.Serialize(new { text }), System.Text.Encoding.UTF8, "application/json");
                using var resp = await _http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();
                string reply = "";
                string error = "";
                bool ok = false;
                if (!string.IsNullOrWhiteSpace(body))
                {
                    JsonDocument doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("ok", out var okProp)) ok = okProp.GetBoolean();
                    if (doc.RootElement.TryGetProperty("reply", out var rp)) reply = rp.GetString() ?? "";
                    if (doc.RootElement.TryGetProperty("error", out var ep)) error = ep.GetString() ?? "";
                    doc.Dispose();
                }
                StopThinking();
                if (ok && !string.IsNullOrEmpty(reply))
                {
                    AddMsg("pet", reply);
                    PetLine?.Invoke(reply); // 桌宠头顶气泡实时显示
                }
                else AddMsg("sys", string.IsNullOrEmpty(error) ? "鲸鱼娘没听清…再试一次?" : error);
            }
            catch (Exception ex)
            {
                StopThinking();
                _serverUp = false;
                Log("SendChat", ex);
                if (!retried && IsLoaded)
                {
                    // 服务可能正在被桌宠唤醒:4 秒后自动重试一次
                    AddMsg("sys", "深海信号断了…鲸鱼娘正在唤醒服务,4 秒后自动重试~");
                    _busy = false;
                    ChatSend.IsEnabled = true;
                    TaskSend.IsEnabled = true;
                    await Task.Delay(4000);
                    if (!IsLoaded) return;
                    await RunChat(text, addUserMsg: false, showThinking: true, switchToChat: switchToChat, retried: true);
                    return;
                }
                AddMsg("sys", "还是连不上…主人看看桌宠小鲸鱼在不在?她负责守着服务~");
            }
            finally
            {
                _busy = false;
                if (IsLoaded)
                {
                    ChatSend.IsEnabled = true;
                    TaskSend.IsEnabled = true;
                    if (switchToChat) ChatInput.Focus();
                    else TaskInput.Focus();
                }
                SetState(_serverUp ? "· 在线" : "· 离线",
                    _serverUp ? Brushes.LightGreen : Brushes.Tomato);
            }
        }

        private async Task PollAsyncSafe()
        {
            try { await PollAsync(); }
            catch (Exception ex) { Log("PollAsync", ex); }
        }

        private void AddMsg(string role, string text)
        {
            if (!IsLoaded) return;
            _msgs.Add(new ChatMsg
            {
                Role = role,
                Text = text,
                Meta = role == "sys" || role == "think" ? "" : (role == "user" ? "主人 · " : "鲸鱼娘 · ") + DateTime.Now.ToString("HH:mm"),
                Align = role == "user" ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            });
            try { ChatLog.ScrollIntoView(_msgs[_msgs.Count - 1]); } catch { }
        }

        private void RemoveThinking()
        {
            StopThinking();
            for (int i = _msgs.Count - 1; i >= 0; i--)
                if (_msgs[i].Role == "think") { _msgs.RemoveAt(i); break; }
        }

        // ── 俏皮思考动画 ──
        private static readonly string[] ThinkPhrases =
        {
            "🐳 鲸鱼娘在深海里翻找答案",
            "🫧 咕噜咕噜…打捞回复气泡中",
            "🐟 追着小鱼绕了一圈,马上回来",
            "🎣 正在从深海钓回复",
            "✍️ 用尾巴蘸墨写回信",
            "🧠 深海算力加载中",
            "🌊 顺着洋流找答案",
        };
        private readonly DispatcherTimer _thinkTimer = new() { Interval = TimeSpan.FromMilliseconds(1400) };
        private int _thinkIdx;
        private int _thinkDots;

        private void StartThinking()
        {
            StopThinking();
            _thinkIdx = new Random().Next(ThinkPhrases.Length);
            _thinkDots = 0;
            AddMsg("think", ThinkPhrases[_thinkIdx] + "…");
            _thinkTimer.Tick += ThinkTick;
            _thinkTimer.Start();
        }

        private void ThinkTick(object sender, EventArgs e)
        {
            ChatMsg last = null;
            for (int i = _msgs.Count - 1; i >= 0; i--)
                if (_msgs[i].Role == "think") { last = _msgs[i]; break; }
            if (last == null) { StopThinking(); return; }
            _thinkDots++;
            if (_thinkDots % 4 == 0)
            {
                _thinkIdx = (_thinkIdx + 1 + new Random().Next(ThinkPhrases.Length - 1)) % ThinkPhrases.Length;
                _thinkDots = 0;
            }
            last.Text = ThinkPhrases[_thinkIdx] + new string('…', 1 + (_thinkDots % 3));
        }

        private void StopThinking()
        {
            _thinkTimer.Stop();
            _thinkTimer.Tick -= ThinkTick;
        }

        private void SetState(string text, Brush brush)
        {
            if (!IsLoaded) return;
            StateText.Text = text;
            StateText.Foreground = brush;
        }

        private async Task LoadActivitiesAsync()
        {
            try
            {
                string body = null;
                foreach (var endpoint in new[] { Api + "/activity", "http://127.0.0.1:3080/api/whale2/activity" })
                {
                    try
                    {
                        using var resp = await _http.GetAsync(endpoint);
                        if (resp.IsSuccessStatusCode) { body = await resp.Content.ReadAsStringAsync(); break; }
                    }
                    catch { }
                }
                if (body == null)
                {
                    WorkState.Text = "工作台暂不可用(服务未就绪)";
                    return;
                }
                JsonDocument doc = JsonDocument.Parse(body);
                var list = new System.Collections.Generic.List<ActivityMsg>();
                if (doc.RootElement.TryGetProperty("activities", out var acts) && acts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in acts.EnumerateArray())
                    {
                        string kind = a.TryGetProperty("kind", out var k) ? k.GetString() : "";
                        string text = a.TryGetProperty("text", out var t) ? t.GetString() : "";
                        string time = a.TryGetProperty("time", out var tm) ? tm.GetString() : "";
                        if (string.IsNullOrEmpty(text)) continue;
                        string icon = kind switch
                        {
                            "user" => "🗨️ ",
                            "assistant" => "🐳 ",
                            "tool" => "🛠️ ",
                            "result" => "📦 ",
                            _ => "· ",
                        };
                        list.Add(new ActivityMsg { Time = time, Text = icon + text });
                    }
                }
                doc.Dispose();
                WorkLog.ItemsSource = list;
                WorkState.Text = "共 " + list.Count + " 条活动 · 每 8 秒刷新";
            }
            catch
            {
                WorkState.Text = "工作台加载失败";
            }
        }

        private async Task PollAsync()
        {
            bool up = false;
            try
            {
                using var resp = await _http.GetAsync(Api + "/status");
                if (resp.IsSuccessStatusCode) up = true;
            }
            catch { }
            _serverUp = up;
            if (_busy) { SetState("· 思考中…", new SolidColorBrush(Color.FromRgb(0x5B, 0x9B, 0xD5))); }
            else SetState(up ? "· 在线" : "· 离线", up ? Brushes.LightGreen : Brushes.Tomato);
            if (WorkView.Visibility == Visibility.Visible) await LoadActivitiesAsync();
        }

        /// <summary>桌宠转发来的主动消息/回复,追加到聊天记录。</summary>
        public void AppendPet(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            AddMsg("pet", text);
        }
    }
}
