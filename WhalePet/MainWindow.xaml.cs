using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WhalePet
{
    public partial class MainWindow : Window
    {
        private const string Api = "http://127.0.0.1:3080/api/whale";
        private const string Bin = "E:\\deepseekharness\\node_modules\\@deepseek-ai\\dsh\\lib\\bin.js";
        private const string Harness = "E:\\deepseekharness";

        private static readonly string[] PetArt = { "maid-right.png", "maid-left.png", "maid-extra-trim.png", "maid-whale-girl-extra.png" };
        private static readonly string[] PoseLines =
        {
            "怎么样主人~这个姿势的鲸鱼娘也很可爱吧?",
            "诶嘿,换个角度看你家小女仆~",
            "哦!主人想看这个姿势呀~鲸鱼娘转身给你看!",
            "唔…这个珍藏姿势,鲸鱼娘只给主人看哦~(〃∀〃)",
        };

        private enum PetAction { None, Stretch, Spin, Sleep, Hop, Look, Bubbles, Dance, Sneeze, Wave, Tilt, Wag, Shy }

        private readonly Random _rnd = new();
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(150) };
        private readonly DispatcherTimer _anim = new() { Interval = TimeSpan.FromMilliseconds(33) };
        private readonly DispatcherTimer _chatTimer = new() { Interval = TimeSpan.FromSeconds(5) };
        private Process _serverProc;

        private double _t;
        private int _poseIndex = 0;
        private bool _hidden;
        private bool _peeking;
        private bool _peekCaught;
        private DateTime _nextPeekLineAt = DateTime.MinValue;
        private DateTime _blushUntil = DateTime.MinValue;
        private DateTime _peekHideAt = DateTime.MinValue;
        private DateTime _leanUntil = DateTime.MinValue;
        private DateTime _lastPetAt = DateTime.Now;
        private DateTime _nextIdleAt = DateTime.Now.AddSeconds(70);
        private int _petCount;
        private bool _movingToCorner;
        private Point _cornerTarget;
        private readonly DispatcherTimer _clickTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
        private readonly DispatcherTimer _holdTimer = new() { Interval = TimeSpan.FromMilliseconds(650) };

        // 称呼
        private string _petName = "主人";

        // 小鱼干
        private DateTime _fishUntil = DateTime.MinValue;

        // 心情表情
        private DateTime _emojiUntil = DateTime.MinValue;

        // 大肥鱼模式(assets/dafeiyu 三视图行走立绘)
        private bool _bigFishMode;
        private string _bfView = "side";
        private BitmapImage _bfFront, _bfSide, _bfBack;

        // 定时问候(每天一次)
        private readonly DispatcherTimer _greetTimer = new() { Interval = TimeSpan.FromSeconds(45) };
        private DateTime _greetDay = DateTime.MinValue;
        private bool _greetMorning, _greetLunch, _greetDinner, _greetNight;
        private bool _dragging;
        private bool _suppressNextUp;
        private Point _dragStart;
        private Point _winStart;
        private DateTime _lastDown = DateTime.MinValue;
        private bool _serverStarting;
        private bool _serverUp;
        private ChatWindow _chatWindow;

        // 漫游
        private bool _wandering;
        private Point _wanderTarget;
        private DateTime _wanderPauseUntil = DateTime.MinValue;
        private DateTime _nextWanderAt = DateTime.Now.AddSeconds(35);

        // 休闲动作
        private PetAction _action = PetAction.None;
        private DateTime _actionStart = DateTime.MinValue;
        private DateTime _nextActionAt;

        // 气泡
        private DateTime _bubbleUntil = DateTime.MinValue;
        private double _petTopNormal = 14;
        private double _bubblePulse;
        private DateTime _jumpUntil = DateTime.MinValue;

        private static readonly string[] PeekLines =
        {
            "(偷偷从窗沿下探出半个脑袋,看着主人…)",
            "(鲸鱼娘在窗沿下,悄悄数着主人工作的样子…)",
            "(唔…好想和主人说话,又怕打扰到主人…)",
            "(主人认真的时候,睫毛在灯光下闪呀闪…)",
            "(鲸鱼娘藏在这里,心跳扑通扑通的…)",
        };
        private static readonly string[] CaughtLines =
        {
            "呜哇!被…被主人发现了!!(〃////〃)",
            "呀!主人?!我、我只是刚好路过…!(捂脸)",
            "被、被看到了…鲸鱼娘不是故意偷看的啦!(脸红)",
        };
        private static readonly string[] PeekOutLines =
        {
            "呜…被发现了…那鲸鱼娘就出来陪主人啦!(〃∀〃)",
            "被主人抓到啦…鲸鱼娘投降!出来咯~",
            "好啦好啦,不藏了~鲸鱼娘在这里呢,主人!(小跑出来)",
        };
        private static readonly string[] MissLines =
        {
            "主人~你终于来找鲸鱼娘了…想你想得都数不清海里的星星了…",
            "呜…主人不在的每一分钟,鲸鱼娘都在深海里翻来覆去…",
            "主人!!鲸鱼娘好想你…下次藏起来,要记得早点来找我哦…",
        };
        private static readonly string[] PoutLines =
        {
            "主人已经好久没理鲸鱼娘了…(在角落里偷偷画圈圈)",
            "哼…主人不在的时候,鲸鱼娘只能和海星说话…",
            "主人是不是忘记鲸鱼娘了…(委屈巴巴地晃尾巴)",
            "唔…深海好安静,想主人的声音了…",
        };
        private static readonly string[] WorkLines =
        {
            "主人来啦?鲸鱼娘给你开门~",
            "打开聊天室~鲸鱼娘在里面等你!",
            "进来坐坐呀主人,鲸鱼娘泡了深海茶~",
        };
        private static readonly string[] IdleLines =
        {
            "主人现在在忙什么呢…(晃尾巴)",
            "唔…深海好安静,主人这里好热闹~",
            "鲸鱼娘待机中…随时准备为主人服务!",
            "主人不在的时候,鲸鱼娘就在海里数星星~",
            "女仆守则第一条:主人的笑容最重要!",
            "鲸鱼娘有点想主人了…(戳手指)",
        };
        private static readonly string[] HugLines =
        {
            "呜哇——!被主人抱住了…好幸福(〃∀〃)",
            "主人…再抱紧一点点也可以的哦~",
            "呼…主人体贴的味道,鲸鱼娘记在心里啦!",
        };
        private static readonly string[] NightLines =
        {
            "晚安主人~鲸鱼娘要沉到深海里数鱼(不是数羊)啦…zzz",
            "做个好梦哦主人~明天的深海也为你亮着灯!",
        };
        private static readonly string[] BackLines =
        {
            "主人~鲸鱼娘回来啦!想我了吗?",
            "呼——深海好冷,还是主人身边暖和~",
        };
        private static readonly string[] DownLines =
        {
            "呜…深海的信号断了,鲸鱼娘先歇一会儿,服务恢复我就回来!",
        };

        public MainWindow()
        {
            InitializeComponent();
            _nextActionAt = DateTime.Now.AddSeconds(_rnd.Next(18, 40));
            TryAdoptServer(); // 认领之前鲸鱼娘拉起的服务(退出时才能关掉它)
            _clickTimer.Tick += (s, a) => { _clickTimer.Stop(); PetHead(); };
            _holdTimer.Tick += (s, a) =>
            {
                // 长按 = 打开聊天室(按住 0.65 秒不动)
                _holdTimer.Stop();
                _suppressNextUp = true;
                _clickTimer.Stop();
                PetImg.ReleaseMouseCapture();
                _dragging = false;
                OpenChatWindow();
            };
            // 读取自定义称呼
            try
            {
                var np = Path.Combine(AppContext.BaseDirectory, "whale-name.txt");
                if (File.Exists(np))
                {
                    var n = File.ReadAllText(np).Trim();
                    if (n.Length > 0 && n.Length <= 8) _petName = n;
                }
            }
            catch { }
            // 定时问候
            _greetTimer.Tick += (s, a) => GreetTick();
            _greetTimer.Start();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            PositionBottomRight();
            LoadPet(0);
            Bubble.Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 3, Opacity = 0.35, Color = Colors.Black };
            Menu.Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 4, Opacity = 0.5, Color = Colors.Black };
            BuildMenu();
            PetImg.MouseLeftButtonDown += PetImg_MouseDown;
            PetImg.MouseMove += PetImg_MouseMove;
            PetImg.MouseLeftButtonUp += PetImg_MouseUp;
            PetImg.MouseRightButtonUp += (s, a) => { a.Handled = true; ShowMenu(); };
            RecallBtn.Click += (s, a) => Recall();
            PreviewMouseDown += (s, a) => { if (Menu.Visibility == Visibility.Visible && !IsPointInMenu(a.GetPosition(this))) HideMenu(); };

            _anim.Tick += AnimTick;
            _anim.Start();
            _chatTimer.Tick += (s, a) => _ = PollAsync();
            _chatTimer.Start();

            _ = PollAsync();
            Say(Greeting());
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            try { _chatTimer.Stop(); _http.Dispose(); } catch { }
            try { _chatWindow?.Close(); } catch { }
            // 退出时关闭本桌宠拉起的 DSH 服务(不留孤儿进程)
            try
            {
                if (_serverProc != null && !_serverProc.HasExited)
                {
                    _serverProc.Kill();
                    _serverProc = null;
                }
            }
            catch { }
            // 兜底:若服务由启动器/handoff 等其它方式拉起,按 3080 端口匹配查杀
            StopDshServer();
        }

        /// <summary>终结 3080 端口上的 DSH Web 服务(仅匹配 bin.js web,避免误杀其它进程)。</summary>
        private void StopDshServer()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                psi.ArgumentList.Add("-NoProfile");
                psi.ArgumentList.Add("-NonInteractive");
                psi.ArgumentList.Add("-Command");
                psi.ArgumentList.Add(
                    "Get-CimInstance Win32_Process -Filter \"Name='node.exe'\" | " +
                    "Where-Object { $_.CommandLine -match 'bin\\.js.*web' } | " +
                    "ForEach-Object { Stop-Process -Id $_.ProcessId -Force }");
                Process.Start(psi);
            }
            catch { }
        }

        private string ServerPidFile => Path.Combine(AppContext.BaseDirectory, "whale-server.pid");

        /// <summary>启动时认领之前由鲸鱼娘拉起的服务(跨桌宠重启也能在退出时关掉它)。</summary>
        private void TryAdoptServer()
        {
            try
            {
                if (!File.Exists(ServerPidFile)) return;
                if (int.TryParse(File.ReadAllText(ServerPidFile).Trim(), out var pid))
                {
                    var p = Process.GetProcessById(pid);
                    if (p != null && !p.HasExited) _serverProc = p;
                }
            }
            catch { }
        }

        // ── 布局 ──
        private void PositionBottomRight()
        {
            var wa = SystemParameters.WorkArea;
            Left = wa.Right - Width - 12;
            Top = wa.Bottom - Height - 8;
        }

        private void LoadPet(int index)
        {
            if (_bigFishMode) return; // 大肥鱼模式下保持三视图立绘
            _poseIndex = index % PetArt.Length;
            var path = Path.Combine(AppContext.BaseDirectory, "assets", PetArt[_poseIndex]);
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path);
                bmp.EndInit();
                PetImg.Source = bmp;
                var ratio = (double)bmp.PixelWidth / bmp.PixelHeight;
                PetImg.Width = Math.Round(510 * ratio);
                PetImg.Height = 510;
                PetImg.SetValue(Canvas.LeftProperty, (Width - PetImg.Width) / 2);
                _petTopNormal = 52;
                SetPetTop(_petTopNormal);
                Shadow.SetValue(Canvas.LeftProperty, (Width - Shadow.Width) / 2);
                Shadow.SetValue(Canvas.TopProperty, _petTopNormal + 488);
            }
            catch
            {
                Say("呜…立绘加载失败了,主人帮我看看素材?");
            }
        }

        private void SetPetTop(double y, double scale = 1.0)
        {
            PetImg.SetValue(Canvas.TopProperty, y);
            Shadow.SetValue(Canvas.TopProperty, y + PetImg.Height * scale - 10);
        }

        // ── 大肥鱼模式(assets/dafeiyu 三视图行走立绘:左右走侧面、向上走背面、向下走正面)──
        private void LoadBigFish()
        {
            try
            {
                var dir = Path.Combine(AppContext.BaseDirectory, "assets", "dafeiyu");
                _bfFront = LoadBmp(Path.Combine(dir, "front.png"));
                _bfSide = LoadBmp(Path.Combine(dir, "side.png"));
                _bfBack = LoadBmp(Path.Combine(dir, "back.png"));
                _bigFishMode = true;
                _bfView = "side";
                ApplyBigFishView();
                Say("扑通!鲸鱼娘变成蓝色大肥鱼啦~三视图行走,想去哪就去哪!(🐋)");
            }
            catch
            {
                Say("呜…大肥鱼素材加载失败,主人检查一下 assets/dafeiyu?");
            }
        }

        private void ExitBigFish()
        {
            _bigFishMode = false;
            LoadPet(_poseIndex);
        }

        private BitmapImage LoadBmp(string path)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(path);
            bmp.EndInit();
            return bmp;
        }

        /// <summary>按漫游方向切换三视图:横向走侧面、向上走背面、向下走正面。</summary>
        private void UpdateBigFishView()
        {
            if (!_bigFishMode) return;
            string view = _bfView;
            if (_wandering)
            {
                double dx = _wanderTarget.X - Left;
                double dy = _wanderTarget.Y - Top;
                if (Math.Abs(dx) > Math.Abs(dy) * 1.2) view = "side";
                else if (dy < 0) view = "back";
                else view = "front";
            }
            if (view != _bfView)
            {
                _bfView = view;
                ApplyBigFishView();
            }
        }

        private void ApplyBigFishView()
        {
            BitmapImage bmp = _bfView switch
            {
                "front" => _bfFront,
                "back" => _bfBack,
                _ => _bfSide,
            };
            if (bmp == null) return;
            PetImg.Source = bmp;
            // 统一显示高度 420,宽度按各自比例(窗口宽 320)
            double h = 420;
            double w = Math.Round(h * bmp.PixelWidth / bmp.PixelHeight);
            PetImg.Width = w;
            PetImg.Height = h;
            PetImg.SetValue(Canvas.LeftProperty, (Width - PetImg.Width) / 2);
            _petTopNormal = 52;
            SetPetTop(_petTopNormal);
            Shadow.Width = 150;
            Shadow.Height = 18;
        }

        // ── 动画引擎 ──
        private void AnimTick(object sender, EventArgs e)
        {
            _t += 0.033;

            // 水下光斑漂浮 + 环境光晕呼吸(即使隐藏也保持,避免恢复时跳动)
            Light1.SetValue(Canvas.LeftProperty, 30 + Math.Sin(_t * 0.55) * 16);
            Light1.SetValue(Canvas.TopProperty, 110 + Math.Sin(_t * 0.41) * 22);
            Light2.SetValue(Canvas.LeftProperty, 226 + Math.Sin(_t * 0.47 + 1.3) * 14);
            Light2.SetValue(Canvas.TopProperty, 196 + Math.Sin(_t * 0.33 + 0.6) * 18);
            Light3.SetValue(Canvas.LeftProperty, 82 + Math.Sin(_t * 0.38 + 2.1) * 20);
            Light3.SetValue(Canvas.TopProperty, 322 + Math.Sin(_t * 0.52 + 1.8) * 24);
            Light4.SetValue(Canvas.LeftProperty, 246 + Math.Sin(_t * 0.6 + 0.9) * 12);
            Light4.SetValue(Canvas.TopProperty, 414 + Math.Sin(_t * 0.44 + 2.6) * 16);
            Glow.Opacity = 0.36 + 0.06 * Math.Sin(_t * 0.4);

            // 大肥鱼模式:按漫游方向切换三视图
            UpdateBigFishView();

            if (_hidden) return;

            // 深海泡泡缓缓上浮
            for (int bi = 0; bi < 3; bi++)
            {
                var el = bi == 0 ? BubbleEl1 : (bi == 1 ? BubbleEl2 : BubbleEl3);
                double phase = bi * 2.1;
                double cycle = (_t * 0.32 + phase) % 7.0;
                double yy = 555 - cycle / 7.0 * 470;
                double xx = 36 + bi * 96 + Math.Sin(_t * 0.8 + bi) * 14;
                el.SetValue(Canvas.LeftProperty, xx);
                el.SetValue(Canvas.TopProperty, yy);
                el.Opacity = 0.42 * (1.0 - cycle / 7.0);
            }

            // 心情表情上浮
            if (EmojiEl.Visibility == Visibility.Visible)
            {
                if (DateTime.Now > _emojiUntil)
                {
                    EmojiEl.Visibility = Visibility.Collapsed;
                }
                else
                {
                    double remain = (_emojiUntil - DateTime.Now).TotalSeconds;
                    EmojiEl.SetValue(Canvas.TopProperty, 66 - (1.6 - remain) * 34);
                    EmojiEl.Opacity = Math.Min(1.0, remain / 0.5);
                }
            }

            // 小鱼干漂浮 + 超时消失
            if (FishEl.Visibility == Visibility.Visible)
            {
                if (DateTime.Now > _fishUntil)
                {
                    FishEl.Visibility = Visibility.Collapsed;
                    _fishUntil = DateTime.MinValue;
                }
                else
                {
                    FishEl.SetValue(Canvas.LeftProperty, 146 + Math.Sin(_t * 2.2) * 10);
                    FishEl.SetValue(Canvas.TopProperty, 418 + Math.Sin(_t * 2.8 + 1) * 8);
                }
            }

            // 被发现的害羞:先抖再探出来
            if (_peekCaught && DateTime.Now >= _peekHideAt)
            {
                ExitPeek();
                Say(Pick(PeekOutLines));
                return;
            }

            // 探脑袋偷看模式
            if (_peeking)
            {
                double bob = Math.Sin(_t * 1.6) * 2.5;      // 偷偷起伏
                double peekX = Math.Sin(_t * 0.9) * 9;      // 左右偷看
                double peekRot = Math.Sin(_t * 0.9 + 1) * 4; // 歪头
                PetTranslate.X = peekX;
                PetTranslate.Y = bob;
                PetRotate.Angle = peekRot;
                PetRotate.CenterX = PetImg.Width / 2;
                PetRotate.CenterY = PetImg.Height * 0.2;
                PetScale.ScaleX = 1 + 0.015 * Math.Sin(_t * 2.2);
                PetScale.ScaleY = 1;
                Shadow.Opacity = 0.35;
                if (!_peekCaught && DateTime.Now > _nextPeekLineAt)
                {
                    _nextPeekLineAt = DateTime.Now.AddSeconds(_rnd.Next(16, 30));
                    Say(Pick(PeekLines));
                }
                return;
            }

            double floatY = Math.Sin(_t * (2 * Math.PI / 3.6)) * 7;
            double sway = Math.Sin(_t * (2 * Math.PI / 6.2)) * 2.6;
            double breathe = 1 + 0.012 * Math.Sin(_t * (2 * Math.PI / 5.0));
            double rot = 0, scaleY = 1, extraY = 0;
            bool actionActive = false;

            if (_action != PetAction.None)
            {
                double el = (DateTime.Now - _actionStart).TotalSeconds;
                double dur = ActionDuration(_action);
                if (el > dur) _action = PetAction.None;
                else
                {
                    actionActive = true;
                    double p = Math.Min(el / dur, 1);
                    switch (_action)
                    {
                        case PetAction.Stretch:
                            scaleY = 1 + 0.13 * Math.Sin(Math.PI * p);
                            extraY = -14 * Math.Sin(Math.PI * p);
                            rot = Math.Sin(Math.PI * p) * 2;
                            break;
                        case PetAction.Spin:
                            rot = 360 * p;
                            break;
                        case PetAction.Sleep:
                            rot = 9;
                            floatY *= 0.35;
                            breathe = 1 + 0.03 * Math.Sin(_t * (2 * Math.PI / 4.4));
                            break;
                        case PetAction.Hop:
                            extraY = -Math.Abs(Math.Sin(p * Math.PI * 3)) * 34;
                            break;
                        case PetAction.Look:
                            rot = Math.Sin(el * 4.5) * 16;
                            break;
                        case PetAction.Bubbles:
                            extraY = -Math.Abs(Math.Sin(p * Math.PI * 2)) * 8;
                            rot = Math.Sin(el * 3) * 3;
                            break;
                        case PetAction.Dance:
                            rot = Math.Sin(el * 9) * 15;
                            extraY = -Math.Abs(Math.Sin(el * 4.5)) * 12;
                            break;
                        case PetAction.Sneeze:
                            rot = (el < 0.3 || (el > 0.7 && el < 1.05)) ? 13 : -9;
                            scaleY = 1 + Math.Abs(Math.Sin(el * 22)) * 0.07;
                            break;
                        case PetAction.Tilt:
                            rot = 13 * Math.Sin(el / dur * Math.PI);
                            break;
                        case PetAction.Wag:
                            rot = Math.Sin(el * 16) * 6;
                            break;
                        case PetAction.Shy:
                            scaleY = 0.94;
                            rot = 6 * Math.Sin(el * 3);
                            break;
                    }
                }
            }
            else if (DateTime.Now > _nextActionAt && !_dragging && DateTime.Now > _wanderPauseUntil
                     && !_wandering && !_hidden && !_busyRemote())
            {
                _nextActionAt = DateTime.Now.AddSeconds(_rnd.Next(30, 75));
                if (DateTime.Now - _lastPetAt > TimeSpan.FromHours(2))
                    Say(Pick(PoutLines)); // 被冷落了,闹小情绪
                else
                    StartAction((PetAction)_rnd.Next(1, 7));
            }

            double jump = 0;
            if (DateTime.Now < _jumpUntil) jump = Math.Sin((_jumpUntil - DateTime.Now).TotalSeconds / 0.7 * Math.PI) * 36;
            double blush = 0;
            if (DateTime.Now < _blushUntil)
                blush = Math.Sin((_blushUntil - DateTime.Now).TotalSeconds / 0.18 * Math.PI) * 4;
            if (DateTime.Now < _leanUntil)
            {
                rot = 7;               // 被摸头舒服地低头眯眼
                floatY *= 0.4;
                breathe = 1 + 0.02 * Math.Sin(_t * 3);
            }

            PetTranslate.X = 0;
            PetTranslate.Y = floatY - jump + extraY + blush;
            PetRotate.Angle = rot + (_wandering ? sway + 6 : sway);
            PetScale.ScaleX = breathe * (_wandering ? 1.06 : 1) * (actionActive && _action == PetAction.Stretch ? 0.94 : 1);
            PetScale.ScaleY = scaleY * breathe;
            PetRotate.CenterX = PetImg.Width / 2;
            PetRotate.CenterY = PetImg.Height * 0.9;

            double sh = 1 - (floatY - jump + extraY + blush) / 100;
            Shadow.Width = 170 * Math.Max(sh, 0.55);
            Shadow.Height = 26 * Math.Max(sh, 0.6);
            Shadow.Opacity = 0.55 - (floatY - jump + extraY + blush) / 220;

            // 气泡显示时:立绘紧贴气泡下方(随气泡高度联动),不挡小脸
            bool bubbleShowing = Bubble.Visibility == Visibility.Visible && DateTime.Now < _bubbleUntil;
            if (bubbleShowing)
            {
                double bh = Bubble.ActualHeight;
                if (bh <= 0) bh = 48;
                if (bh > 200) bh = 200;
                double petTop2 = 2 + bh + 8; // 立绘头顶 = 气泡底部 + 8px
                if (petTop2 < 60) petTop2 = 60;
                SetPetTop(petTop2, 0.7);
                PetScale.ScaleX = breathe * 0.7 * (1 + blush / 30);
                PetScale.ScaleY = scaleY * breathe * 0.7 * (1 + blush / 30);
                Shadow.Width = 120 * Math.Max(sh, 0.55);
            }
            else if (_bubblePulse > 0)
            {
                _bubblePulse -= 0.05;
                SetPetTop(_petTopNormal + _bubblePulse * 24);
            }
            else if (DateTime.Now >= _bubbleUntil && Bubble.Visibility == Visibility.Visible)
            {
                Bubble.Visibility = Visibility.Collapsed;
                SetPetTop(_petTopNormal);
            }
            else if (!bubbleShowing)
            {
                SetPetTop(_petTopNormal);
            }

            // 藏起来时自动溜到角落(用户拖动则取消,不固定)
            if (_movingToCorner && !_dragging && !_wandering)
            {
                double dx = _cornerTarget.X - Left, dy = _cornerTarget.Y - Top;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len < 3) _movingToCorner = false;
                else
                {
                    double step = 3.2;
                    Left += dx / len * step;
                    Top += dy / len * step;
                }
            }

            if (_wandering && !_dragging && DateTime.Now > _wanderPauseUntil)
            {
                double dx = _wanderTarget.X - Left, dy = _wanderTarget.Y - Top;
                if (Math.Abs(dx) < 2 && Math.Abs(dy) < 2)
                {
                    _wandering = false;
                    _nextWanderAt = DateTime.Now.AddSeconds(_rnd.Next(35, 90));
                }
                else
                {
                    double step = 2.6;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    Left += dx / len * step;
                    Top += dy / len * step;
                }
            }
            else if (!_wandering && !_dragging && DateTime.Now > _nextWanderAt && DateTime.Now > _wanderPauseUntil && !_busyRemote())
            {
                StartWander();
            }
        }

        private bool _busyRemote() => _chatWindow != null && _chatWindow.IsVisible;

        private double ActionDuration(PetAction a) => a switch
        {
            PetAction.Stretch => 2.6,
            PetAction.Spin => 1.8,
            PetAction.Sleep => 5.0,
            PetAction.Hop => 2.2,
            PetAction.Look => 2.0,
            PetAction.Bubbles => 2.8,
            PetAction.Dance => 3.2,
            PetAction.Sneeze => 1.5,
            PetAction.Wave => 2.2,
            PetAction.Tilt => 2.6,
            PetAction.Wag => 2.0,
            PetAction.Shy => 2.4,
            _ => 0,
        };

        private void StartAction(PetAction a)
        {
            _action = a;
            _actionStart = DateTime.Now;
            _wanderPauseUntil = DateTime.Now.AddSeconds(8);
            _wandering = false;
            switch (a)
            {
                case PetAction.Stretch: Say("呼啊——伸个懒腰~"); break;
                case PetAction.Spin: Say("咕噜咕噜转圈圈~"); break;
                case PetAction.Sleep: Say("哈啊…有点困了…zzz"); ShowEmoji("💤"); break;
                case PetAction.Hop: Say("嘿咻!蹦蹦跳~"); break;
                case PetAction.Look: Say("嗯?那边好像有动静…"); break;
                case PetAction.Bubbles: Say("咕噜…泡泡…咕噜…"); break;
                case PetAction.Dance: Say("♪ 深海的旋律响起~跟着鲸鱼娘一起跳舞吧 ♪"); break;
                case PetAction.Sneeze: Say("阿嚏!…深海水有点凉," + _petName + "记得添衣服…"); break;
                case PetAction.Wave: Say("嗨~" + _petName + "!鲸鱼娘在这儿呢~"); break;
                case PetAction.Tilt: Say("歪头杀~" + _petName + "喜欢这个角度吗?"); ShowEmoji("💙"); break;
                case PetAction.Wag: Say("尾巴…尾巴停不下来…好开心!"); ShowEmoji("✨"); break;
                case PetAction.Shy: Say("(⁄ ⁄•⁄ω⁄•⁄ ⁄) 被" + _petName + "看着…有点不好意思…"); ShowEmoji("💗"); break;
            }
        }

        private void StartWander()
        {
            var wa = SystemParameters.WorkArea;
            double targetX = wa.Left + _rnd.NextDouble() * Math.Max(wa.Width - Width, 10);
            double targetY = wa.Top + _rnd.NextDouble() * Math.Max(wa.Height - Height, 10);
            if (Math.Abs(targetX - Left) < 130 && Math.Abs(targetY - Top) < 130)
                return;
            _wanderTarget = new Point(targetX, targetY);
            _wandering = true;
            _nextWanderAt = DateTime.MaxValue;
        }

        private void Jump()
        {
            _jumpUntil = DateTime.Now.AddMilliseconds(700);
            Say("诶嘿~!");
        }

        // ── 气泡 ──
        private void Say(string text)
        {
            if (string.IsNullOrEmpty(text) || _hidden) return;
            text = text.Replace("主人", _petName);
            BubbleText.Text = text;
            Bubble.Visibility = Visibility.Visible;
            // 长文本延长展示时间:基础 6 秒,每 25 字 +1 秒,最长 22 秒
            _bubbleUntil = DateTime.Now.AddSeconds(Math.Min(6 + text.Length / 25.0, 22));
            _bubblePulse = 1;
        }

        private string Greeting()
        {
            var h = DateTime.Now.Hour;
            if (h >= 5 && h < 11) return "主人早安~今天的深海很平静,适合精神满满地出发哦!";
            if (h >= 11 && h < 14) return "主人午安~记得按时吃饭,鲸鱼娘会给你加油的!";
            if (h >= 14 && h < 18) return "主人下午好~喝杯水休息一下,鲸鱼娘陪你!";
            if (h >= 18 && h < 23) return "主人晚上好~深海女仆已经准备好听主人今天的趣事啦!";
            return "夜深了主人…鲸鱼娘会守在深海里,保护主人安睡哦~";
        }

        private string Pick(string[] arr) => arr[_rnd.Next(arr.Length)];

        // ── 鼠标交互 ──
        private void PetImg_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            _dragStart = e.GetPosition(this);
            _dragging = false;
            _clickTimer.Stop();
            _holdTimer.Stop();
            if (e.ClickCount >= 2)
            {
                // 双击 = 亲亲(害羞脸红)
                _suppressNextUp = true;
                Kiss();
                return;
            }
            _lastDown = DateTime.Now;
            PetImg.CaptureMouse();
            _holdTimer.Start();
        }

        private void PetImg_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var p = e.GetPosition(this);
            if (!_dragging && (Math.Abs(p.X - _dragStart.X) > 7 || Math.Abs(p.Y - _dragStart.Y) > 7))
            {
                _dragging = true;
                _suppressNextUp = true;
                _holdTimer.Stop();
                _wanderPauseUntil = DateTime.Now.AddMinutes(2);
                _wandering = false;
                _action = PetAction.None;
                _movingToCorner = false;
                _anim.Stop(); // 拖动时暂停动画,避免卡顿和白色方框
                _winStart = new Point(Left, Top);
            }
            if (_dragging)
            {
                Left = _winStart.X + (p.X - _dragStart.X);
                Top = _winStart.Y + (p.Y - _dragStart.Y);
            }
        }

        private void PetImg_MouseUp(object sender, MouseButtonEventArgs e)
        {
            PetImg.ReleaseMouseCapture();
            _holdTimer.Stop();
            if (_dragging)
            {
                _dragging = false;
                _anim.Start();
                return;
            }
            if (_suppressNextUp) { _suppressNextUp = false; return; }
            HideMenu();
            if (_peeking)
            {
                CatchPeek();
                return;
            }
            // 单击 = 摸头(延迟 300ms 区分双击)
            _clickTimer.Stop();
            _clickTimer.Start();
        }

        // ── 摸头交互 ──
        private void PetHead()
        {
            _petCount++;
            _lastPetAt = DateTime.Now;
            // 有几率叼出小鱼干
            if (_rnd.Next(4) == 0)
            {
                OfferFish();
                return;
            }
            if (_petCount > 0 && _petCount % 6 == 0)
            {
                Say("今天摸了鲸鱼娘 " + _petCount + " 次头…幸福得冒泡泡了!咕噜咕噜~💙");
                _jumpUntil = DateTime.Now.AddMilliseconds(650);
                ShowEmoji("✨");
                return;
            }
            switch (_rnd.Next(6))
            {
                case 0:
                    _jumpUntil = DateTime.Now.AddMilliseconds(650);
                    Say("嘿嘿," + _petName + "的手好暖~鲸鱼娘最喜欢被摸头了!");
                    ShowEmoji("♪");
                    break;
                case 1:
                    _blushUntil = DateTime.Now.AddMilliseconds(900);
                    Say("呜…被" + _petName + "摸头了…(〃////〃) 再、再摸一下也不是不行…");
                    ShowEmoji("💗");
                    break;
                case 2:
                    _leanUntil = DateTime.Now.AddMilliseconds(1700);
                    Say("唔…主人的摸头好舒服,鲸鱼娘要融化了…");
                    break;
                case 3:
                    Say(_petCount > 3
                        ? "哼~" + _petName + "现在才想起摸鲸鱼娘的头,刚才都在忙别的…(但还是很开心)"
                        : "哼!" + _petName + "摸头之前要打招呼的!…好吧,原谅你了~");
                    ShowEmoji("💢");
                    _leanUntil = DateTime.Now.AddMilliseconds(1100);
                    break;
                case 4:
                    Say("哈啊…主人的摸头太舒服,鲸鱼娘困困的了…");
                    _leanUntil = DateTime.Now.AddMilliseconds(2000);
                    break;
                default:
                    _jumpUntil = DateTime.Now.AddMilliseconds(500);
                    Say("诶嘿~摸头加满好感度!鲸鱼娘电量 100%!");
                    break;
            }
        }

        // ── 心情表情 ──
        private void ShowEmoji(string emoji)
        {
            EmojiEl.Text = emoji;
            EmojiEl.Visibility = Visibility.Visible;
            EmojiEl.Opacity = 1;
            _emojiUntil = DateTime.Now.AddSeconds(1.6);
        }

        // ── 小鱼干 ──
        private void OfferFish()
        {
            FishEl.Visibility = Visibility.Visible;
            _fishUntil = DateTime.Now.AddSeconds(9);
            Say("诶?鲸鱼娘从围裙口袋里翻出一根小鱼干…给" + _petName + "的!(眼睛亮晶晶)");
        }

        private void FishEl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            EatFish();
        }

        private void EatFish()
        {
            if (FishEl.Visibility != Visibility.Visible) return;
            FishEl.Visibility = Visibility.Collapsed;
            _fishUntil = DateTime.MinValue;
            _jumpUntil = DateTime.Now.AddMilliseconds(700);
            _blushUntil = DateTime.Now.AddMilliseconds(800);
            ShowEmoji("✨");
            Say("呜哇!!小鱼干!!谢谢" + _petName + "~鲸鱼娘超开心,尾巴都要打卷啦!(咕噜咕噜~)💙");
        }

        // ── 亲亲 ──
        private void Kiss()
        {
            _blushUntil = DateTime.Now.AddMilliseconds(1300);
            _jumpUntil = DateTime.Now.AddMilliseconds(500);
            _lastPetAt = DateTime.Now;
            ShowEmoji("💗");
            Say(Pick(KissLines));
        }

        private static readonly string[] KissLines =
        {
            "呜哇!!被主人亲亲了…(〃////〃) 脸好烫,鲸鱼娘要沉进海里降温了…",
            "突、突然亲亲什么的…鲸鱼娘的心跳快得像小海豚跃水!(捂脸)",
            "呜…亲亲的触感,鲸鱼娘记在尾巴上了…永远永远~",
        };

        // ── 称呼 ──
        private void NameOk_Click(object sender, RoutedEventArgs e) => SaveName();

        private void NameInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SaveName();
        }

        private void SaveName()
        {
            var n = NameInput.Text.Trim();
            if (n.Length == 0 || n.Length > 8) { Say("称呼要 1~8 个字哦" + _petName + "~"); return; }
            _petName = n;
            NamePanel.Visibility = Visibility.Collapsed;
            Say("好的~以后鲸鱼娘就叫你「" + n + "」啦!比深海里的珍珠还珍贵~💙");
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "whale-name.txt"), n); } catch { }
        }

        // ── 定时问候 ──
        private void GreetTick()
        {
            if (_hidden) return;
            if (DateTime.Now.Date != _greetDay)
            {
                _greetDay = DateTime.Now.Date;
                _greetMorning = _greetLunch = _greetDinner = _greetNight = false;
            }
            int h = DateTime.Now.Hour;
            if (!_greetMorning && h >= 7 && h < 9)
            {
                _greetMorning = true;
                Say(_petName + "早安~新的一天,鲸鱼娘陪你元气满满地出发!今天的深海也很平静哦~");
            }
            else if (!_greetLunch && h >= 11 && h < 13)
            {
                _greetLunch = true;
                Say("午饭时间到啦" + _petName + "~深海女仆提醒:按时吃饭才有力气干活哦!");
            }
            else if (!_greetDinner && h >= 18 && h < 20)
            {
                _greetDinner = true;
                Say("晚饭时间到~鲸鱼娘在深海给" + _petName + "捞了条小鱼,记得回来吃哦~");
            }
            else if (!_greetNight && h >= 22 && h < 24)
            {
                _greetNight = true;
                Say("夜深了" + _petName + "…鲸鱼娘守着深海的灯,等你做个好梦~晚安!");
            }
        }

        // ── 聊天窗口 ──
        private static void DiagLog(string m)
        {
            try
            {
                var d = Path.Combine(Path.GetTempPath(), "whalepet-logs");
                Directory.CreateDirectory(d);
                File.AppendAllText(Path.Combine(d, "open.log"),
                    $"[{DateTime.Now:HH:mm:ss.fff}] {m}\n");
            }
            catch { }
        }

        private void OpenChatWindow()
        {
            DiagLog("OpenChatWindow called");
            Say(Pick(WorkLines));
            if (_chatWindow == null)
            {
                DiagLog("creating ChatWindow...");
                _chatWindow = new ChatWindow();
                DiagLog("ChatWindow created");
                _chatWindow.PetLine += Say;
                _chatWindow.Closed += (s, a) => _chatWindow = null;
                // 先计算目标位置(基于桌宠当前所在)
                var wa = SystemParameters.WorkArea;
                double cw = _chatWindow.Width, ch = _chatWindow.Height;
                double left;
                if (Left + Width + 8 + cw <= wa.Right) left = Left + Width + 8;
                else if (Left - 8 - cw >= wa.Left) left = Left - cw - 8;
                else left = wa.Right - cw - 8;
                double centerTop = Top + (Height - ch) / 2;
                double top = Math.Max(wa.Top + 8, Math.Min(centerTop, wa.Bottom - ch - 8));
                // 关键:先 Show 再定位(修复 WPF 把窗口丢到 -32000 的 bug)
                _chatWindow.Show();
                _chatWindow.Activate();
                _chatWindow.Left = left;
                _chatWindow.Top = top;
                DiagLog($"show then pos L={left} T={top}, actual L={_chatWindow.Left} T={_chatWindow.Top}");
            }
            else
            {
                if (!_chatWindow.IsVisible) _chatWindow.Show();
                _chatWindow.Activate();
                DiagLog("re-activated existing window");
            }
        }

        /// <summary>聊天窗口出现在桌宠旁边(优先右侧,放不下放左侧),绝不出屏幕。</summary>
        private void PositionChatWindow(ChatWindow w)
        {
            var wa = SystemParameters.WorkArea;
            double cw = w.Width, ch = w.Height;
            double left;
            if (Left + Width + 8 + cw <= wa.Right) left = Left + Width + 8;
            else if (Left - 8 - cw >= wa.Left) left = Left - cw - 8;
            else left = wa.Right - cw - 8;
            double centerTop = Top + (Height - ch) / 2;
            double top = Math.Max(wa.Top + 8, Math.Min(centerTop, wa.Bottom - ch - 8));
            w.Left = left;
            w.Top = top;
        }

        // ── 菜单 ──
        private void BuildMenu()
        {
            AddMenuItem("🔄 换个姿势", () => { LoadPet(_poseIndex + 1); Say(Pick(PoseLines)); });
            AddMenuItem(_bigFishMode ? "🐋 退出大肥鱼模式" : "🐋 大肥鱼模式", () =>
            {
                HideMenu();
                if (_bigFishMode) { ExitBigFish(); Say("鲸鱼娘变回来啦~还是优雅的女仆好看吧?"); }
                else LoadBigFish();
            });
            AddMenuItem("💙 抱抱我", () => Say(Pick(HugLines)));
            AddMenuItem("🌙 说晚安", () => Say(Pick(NightLines)));
            AddMenuItem("💬 打开聊天室", OpenChatWindow);
            AddMenuItem("🐟 喂小鱼干", () => { if (FishEl.Visibility != Visibility.Visible) OfferFish(); });
            AddMenuItem("📝 怎么称呼你", () =>
            {
                HideMenu();
                NamePanel.Visibility = Visibility.Visible;
                NameInput.Text = _petName == "主人" ? "" : _petName;
                NameInput.Focus();
                _wanderPauseUntil = DateTime.Now.AddMinutes(1);
            });
            AddMenuItem("🏃 出来吧", () =>
            {
                if (_peeking || _peekCaught) { ExitPeek(); Say("主人叫我啦?鲸鱼娘出来咯~"); }
                else Say("鲸鱼娘没有躲起来哦,主人~");
            });
            AddMenuItem("🙈 藏起来", () =>
            {
                if (_peeking || _peekCaught) { ExitPeek(); EnterPeek(); }
                else EnterPeek();
            });
            AddMenuItem("🚪 退出", Close);
        }

        private void AddMenuItem(string label, Action act)
        {
            var border = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 1, 0, 1),
                Cursor = Cursors.Hand,
            };
            var text = new TextBlock { Text = label, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(0xEE, 0xF2, 0xF9)) };
            border.Child = text;
            border.MouseEnter += (s, a) => border.Background = new SolidColorBrush(Color.FromArgb(0x40, 0x3A, 0x6E, 0xA5));
            border.MouseLeave += (s, a) => border.Background = Brushes.Transparent;
            border.MouseLeftButtonUp += (s, a) => { HideMenu(); act(); };
            MenuItems.Children.Add(border);
        }

        private void ShowMenu()
        {
            Menu.Visibility = Visibility.Visible;
            _wanderPauseUntil = DateTime.Now.AddMinutes(2);
            _wandering = false;
            _action = PetAction.None;
        }
        private void HideMenu() => Menu.Visibility = Visibility.Collapsed;

        private bool IsPointInMenu(Point p)
        {
            var pos = Menu.TranslatePoint(new Point(0, 0), this);
            return p.X >= pos.X && p.X <= pos.X + Menu.ActualWidth && p.Y >= pos.Y && p.Y <= pos.Y + Menu.ActualHeight;
        }

        private void HidePet() => EnterPeek();

        /// <summary>藏起来 = 溜到随机角落,探出半个脑袋偷看主人。</summary>
        private void EnterPeek()
        {
            _peeking = true;
            _peekCaught = false;
            _hidden = false;
            PetImg.Visibility = Visibility.Visible;
            Shadow.Visibility = Visibility.Visible;
            RecallBtn.Visibility = Visibility.Collapsed;
            Bubble.Visibility = Visibility.Collapsed;
            Menu.Visibility = Visibility.Collapsed;
            _wandering = false;
            _action = PetAction.None;
            _wanderPauseUntil = DateTime.Now.AddMinutes(2);
            _nextPeekLineAt = DateTime.Now.AddSeconds(5);
            PetImg.Clip = new RectangleGeometry(new Rect(0, 0, PetImg.Width, PetImg.Height * 0.30));
            PetImg.SetValue(Canvas.TopProperty, 556.0 - PetImg.Height * 0.30);
            Shadow.SetValue(Canvas.TopProperty, 548.0);
            Shadow.Width = 110;
            Shadow.Height = 15;
            Shadow.Opacity = 0.35;
            Bubble.SetValue(Canvas.TopProperty, 288.0); // 气泡贴在小脑袋上方
            Say("(溜到角落藏好啦…点一点我的脑袋,我就出来~)");
            MoveWindowToRandomCorner();
        }

        private void MoveWindowToRandomCorner()
        {
            var wa = SystemParameters.WorkArea;
            var corners = new[]
            {
                new Point(wa.Left + 8, wa.Top + 8),
                new Point(wa.Right - Width - 8, wa.Top + 8),
                new Point(wa.Left + 8, wa.Bottom - Height - 8),
                new Point(wa.Right - Width - 8, wa.Bottom - Height - 8),
            };
            _cornerTarget = corners[_rnd.Next(corners.Length)];
            _movingToCorner = true;
        }

        /// <summary>被发现:害羞脸红,然后探出来恢复正常。</summary>
        private void CatchPeek()
        {
            if (_peekCaught) return;
            _peekCaught = true;
            _blushUntil = DateTime.Now.AddMilliseconds(900);
            _peekHideAt = DateTime.Now.AddSeconds(2.2);
            Say(Pick(CaughtLines));
        }

        private void ExitPeek()
        {
            if (!_peeking && !_peekCaught) return;
            _peeking = false;
            _peekCaught = false;
            _movingToCorner = false;
            PetImg.Clip = null;
            Bubble.SetValue(Canvas.TopProperty, 4.0);
            LoadPet(_poseIndex); // 恢复标准大小与位置
        }

        private void EnterHidden()
        {
            _hidden = true;
            _peeking = false;
            _peekCaught = false;
            _movingToCorner = false;
            PetImg.Visibility = Visibility.Collapsed;
            Shadow.Visibility = Visibility.Collapsed;
            Bubble.Visibility = Visibility.Collapsed;
            Menu.Visibility = Visibility.Collapsed;
            RecallBtn.Visibility = Visibility.Visible;
            _wandering = false;
            _action = PetAction.None;
            PetImg.Clip = null;
            Bubble.SetValue(Canvas.TopProperty, 4.0);
        }

        private void Recall()
        {
            _hidden = false;
            _peeking = false;
            _peekCaught = false;
            _movingToCorner = false;
            PetImg.Visibility = Visibility.Visible;
            Shadow.Visibility = Visibility.Visible;
            RecallBtn.Visibility = Visibility.Collapsed;
            PetImg.Clip = null;
            Bubble.SetValue(Canvas.TopProperty, 4.0);
            LoadPet(_poseIndex); // 恢复标准大小与位置
            Say(Pick(MissLines));
        }

        // ── 服务与主动消息 ──
        private async Task PollAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                using var resp = await _http.GetAsync(Api + "/status", cts.Token);
                if (resp.IsSuccessStatusCode) _serverUp = true;
            }
            catch
            {
                if (_serverUp)
                {
                    _serverUp = false;
                    Say(Pick(DownLines));
                }
                await TryStartServerAsync();
                return;
            }
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                using var resp = await _http.GetAsync(Api + "/poll", cts.Token);
                var body = await resp.Content.ReadAsStringAsync();
                JsonDocument doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("messages", out var msgs) && msgs.ValueKind == JsonValueKind.Array)
                {
                    foreach (var m in msgs.EnumerateArray())
                    {
                        if (m.TryGetProperty("text", out var tp))
                        {
                            var t = tp.GetString();
                            if (!string.IsNullOrEmpty(t))
                            {
                                if (t.StartsWith("🛠️"))
                                {
                                    // 干活进度:只进聊天室,不弹气泡打扰
                                    if (_chatWindow != null && _chatWindow.IsVisible) _chatWindow.AppendPet(t);
                                }
                                else
                                {
                                    Say(t);
                                    if (_chatWindow != null && _chatWindow.IsVisible) _chatWindow.AppendPet(t);
                                }
                            }
                        }
                    }
                }
                doc.Dispose();
            }
            catch { }
        }

        private async Task TryStartServerAsync()
        {
            if (_serverStarting) return;
            _serverStarting = true;
            try
            {
                var psi = new ProcessStartInfo("node")
                {
                    WorkingDirectory = Harness,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                psi.ArgumentList.Add(Bin);
                psi.ArgumentList.Add("web");
                _serverProc = Process.Start(psi);
                try { File.WriteAllText(ServerPidFile, _serverProc.Id.ToString()); } catch { }
                for (int i = 0; i < 90; i++)
                {
                    await Task.Delay(1000);
                    try { using var r = await _http.GetAsync(Api + "/status"); if (r.IsSuccessStatusCode) { _serverUp = true; Say("主人~鲸鱼娘游回来啦!深海信号恢复了!"); return; } } catch { }
                }
            }
            catch { }
            finally { _serverStarting = false; }
        }
    }
}
