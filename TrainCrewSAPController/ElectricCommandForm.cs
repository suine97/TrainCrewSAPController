using System;
using System.Drawing;
using System.Windows.Forms;

namespace TrainCrewSAPController
{
    /// <summary>
    /// マウスカーソルのX位置で電気ブレーキ指令値を決定するウィンドウ
    /// 左端デッドゾーン=0 / アクティブエリア=1〜7（7段階） / ギャップ=7固定 / EBゾーン=非常ブレーキ
    /// </summary>
    public class ElectricCommandForm : Form
    {
        // ---- 定数 ----
        private const int COMMAND_MIN = 0;
        private const int COMMAND_MAX = 7;
        /// <summary>ポーリング間隔(ms)。小さいほど追従が速い</summary>
        private const int POLL_INTERVAL_MS = 4;
        /// <summary>左端デッドゾーンの幅(px)</summary>
        private const int DEAD_ZONE_WIDTH = 75;
        /// <summary>EBゾーンとアクティブエリアの隙間の幅(px)</summary>
        private const int EB_GAP_WIDTH = 75;
        /// <summary>右端EBゾーンの幅(px)</summary>
        private const int EB_ZONE_WIDTH = 100;

        // ---- イベント ----
        /// <summary>電気ブレーキ指令値が変化したときに発火。引数は指令値(0〜7)</summary>
        public event Action<int> CommandValueChanged;
        /// <summary>非常ブレーキ状態が変化したときに発火。引数はEB中かどうか</summary>
        public event Action<bool> EmergencyBrakeChanged;

        // ---- フィールド ----
        private int _currentCommandValue = 0;
        private bool _isEmergency = false;

        // ---- コントロール ----
        private Label _labelTitle;
        private Label _labelValue;
        private Panel _panelBar;
        private Panel _panelDeadZone;
        private Panel[] _panelCommandSegments; // 指令値1～7の各段階パネル
        private Panel _panelEBGap;
        private Panel _panelEBZone;
        private Timer _pollTimer;

        public ElectricCommandForm()
        {
            InitializeComponents();
            SetupTimer();
        }


        /// <summary>外部から電気ブレーキ指令値を設定してUIを同期する</summary>
        public void SetCommandValue(int command)
        {
            _currentCommandValue = Clamp(command, COMMAND_MIN, COMMAND_MAX);
            _isEmergency = false;
            UpdateUI();
        }

        // ---- 初期化 ----
        private void InitializeComponents()
        {
            this.Text = "電気ブレーキ指令入力";
            this.Size = new Size(900, 200);
            this.MinimumSize = new Size(200, 100);
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.TopMost = true;

            // タイトルラベル（左端）
            _labelTitle = new Label
            {
                Text = "電気指令",
                Dock = DockStyle.Left,
                Width = 70,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("MS UI Gothic", 9, FontStyle.Bold),
                ForeColor = Color.LightGray,
                BackColor = Color.FromArgb(50, 50, 50)
            };

            // 現在値ラベル（右端）
            _labelValue = new Label
            {
                Text = "指令値: 0",
                Dock = DockStyle.Right,
                Width = 90,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("MS UI Gothic", 11, FontStyle.Bold),
                ForeColor = Color.Cyan,
                BackColor = Color.FromArgb(20, 20, 20)
            };

            // バーの背景パネル
            _panelBar = new Panel
            {
                BackColor = Color.FromArgb(60, 60, 60),
                Dock = DockStyle.Fill,
                Cursor = Cursors.SizeWE
            };

            // デッドゾーンパネル（左端）
            _panelDeadZone = new Panel
            {
                BackColor = Color.FromArgb(30, 60, 90), // ← 青系に変更
                Left = 0,
                Top = 0,
                Width = DEAD_ZONE_WIDTH,
                Height = 0
            };
            _panelDeadZone.Controls.Add(new Label
            {
                Text = "0",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(120, 180, 255), // ← 青系に変更
                Font = new Font("MS UI Gothic", 8, FontStyle.Bold)
            });

            // ギャップパネル（指令値7固定エリア）
            _panelEBGap = new Panel
            {
                BackColor = Color.FromArgb(80, 40, 40), // ← 赤系に変更
                Top = 0,
                Width = EB_GAP_WIDTH,
                Height = 0
            };
            _panelEBGap.Controls.Add(new Label
            {
                Text = "7",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(200, 120, 120), // ← 赤系に変更
                Font = new Font("MS UI Gothic", 8, FontStyle.Bold)
            });

            // EBゾーンパネル（右端）
            _panelEBZone = new Panel
            {
                BackColor = Color.FromArgb(160, 0, 0),
                Top = 0,
                Width = EB_ZONE_WIDTH,
                Height = 0
            };
            _panelEBZone.Controls.Add(new Label
            {
                Text = "EB",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("MS UI Gothic", 10, FontStyle.Bold)
            });

            // 指令値1～7の段階パネルを作成
            _panelCommandSegments = new Panel[7];
            for (int i = 0; i < 7; i++)
            {
                _panelCommandSegments[i] = new Panel
                {
                    BackColor = Color.FromArgb(60, 60, 60),
                    Top = 0,
                    Height = 0
                };

                Label segmentLabel = new Label
                {
                    Text = (i + 1).ToString(),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.Gray,
                    Font = new Font("MS UI Gothic", 10, FontStyle.Bold),
                    BackColor = Color.Transparent
                };
                _panelCommandSegments[i].Controls.Add(segmentLabel);
                _panelBar.Controls.Add(_panelCommandSegments[i]);
            }

            _panelBar.Controls.Add(_panelDeadZone);
            _panelBar.Controls.Add(_panelEBGap);
            _panelBar.Controls.Add(_panelEBZone);

            this.Controls.Add(_panelBar);
            this.Controls.Add(_labelTitle);
            this.Controls.Add(_labelValue);

            this.Resize += (s, e) => UpdateUI();
        }

        private void SetupTimer()
        {
            _pollTimer = new Timer { Interval = POLL_INTERVAL_MS };
            _pollTimer.Tick += PollTimer_Tick;
            _pollTimer.Start();
        }

        // ---- ポーリング処理 ----
        private void PollTimer_Tick(object sender, EventArgs e)
        {
            int barWidth = _panelBar.ClientSize.Width;
            if (barWidth <= 0) return;

            Point cursorOnPanel = _panelBar.PointToClient(Cursor.Position);

            if (cursorOnPanel.X < 0 || cursorOnPanel.X > barWidth) return;
            if (cursorOnPanel.Y < 0 || cursorOnPanel.Y > _panelBar.ClientSize.Height) return;

            int activeEnd = barWidth - EB_GAP_WIDTH - EB_ZONE_WIDTH;
            int ebStart = barWidth - EB_ZONE_WIDTH;

            if (cursorOnPanel.X <= DEAD_ZONE_WIDTH)
            {
                // 左デッドゾーン → 指令値0
                bool wasEB = _isEmergency;
                _isEmergency = false;
                int newValue = COMMAND_MIN;
                if (newValue == _currentCommandValue && !wasEB) return;
                _currentCommandValue = newValue;
                UpdateUI();
                CommandValueChanged?.Invoke(_currentCommandValue);
                if (wasEB) EmergencyBrakeChanged?.Invoke(false);
            }
            else if (cursorOnPanel.X >= ebStart)
            {
                // EBゾーン → 非常ブレーキ
                if (_isEmergency) return;
                _isEmergency = true;
                UpdateUI();
                EmergencyBrakeChanged?.Invoke(true);
            }
            else if (cursorOnPanel.X > activeEnd)
            {
                // ギャップエリア → 指令値7固定
                bool wasEB = _isEmergency;
                _isEmergency = false;
                int newValue = COMMAND_MAX;
                if (newValue == _currentCommandValue && !wasEB) return;
                _currentCommandValue = newValue;
                UpdateUI();
                CommandValueChanged?.Invoke(_currentCommandValue);
                if (wasEB) EmergencyBrakeChanged?.Invoke(false);
            }
            else
            {
                // アクティブエリア → 指令値1〜7（7段階）
                bool wasEB = _isEmergency;
                _isEmergency = false;
                int activeWidth = activeEnd - DEAD_ZONE_WIDTH;
                int positionInActive = cursorOnPanel.X - DEAD_ZONE_WIDTH;
                // 7等分して1〜7の指令値を決定
                int segmentWidth = activeWidth / 7;
                int commandValue = Math.Min(7, (positionInActive / segmentWidth) + 1);
                if (commandValue == _currentCommandValue && !wasEB) return;
                _currentCommandValue = commandValue;
                UpdateUI();
                CommandValueChanged?.Invoke(_currentCommandValue);
                if (wasEB) EmergencyBrakeChanged?.Invoke(false);
            }
        }

        // ---- UI更新 ----
        private void UpdateUI()
        {
            if (_panelBar == null || _panelCommandSegments == null) return;

            int barWidth = _panelBar.ClientSize.Width;
            int barHeight = _panelBar.ClientSize.Height;
            if (barWidth <= 0) return;

            int activeEnd = barWidth - EB_GAP_WIDTH - EB_ZONE_WIDTH;

            // デッドゾーン
            _panelDeadZone.Top = 0;
            _panelDeadZone.Height = barHeight;

            // 指令値1～7の各段階パネルを配置・着色
            int activeWidth = activeEnd - DEAD_ZONE_WIDTH;
            int segmentWidth = activeWidth / 7;

            for (int i = 0; i < 7; i++)
            {
                int commandValue = i + 1;
                _panelCommandSegments[i].Left = DEAD_ZONE_WIDTH + (i * segmentWidth);
                _panelCommandSegments[i].Top = 0;

                // 最後のパネル（指令値7）は残りの領域すべてを占める
                if (i == 6)
                {
                    _panelCommandSegments[i].Width = activeEnd - _panelCommandSegments[i].Left;
                }
                else
                {
                    _panelCommandSegments[i].Width = segmentWidth;
                }
                _panelCommandSegments[i].Height = barHeight;

                // 現在の指令値以下の段階を着色
                if (commandValue <= _currentCommandValue && !_isEmergency)
                {
                    float ratio = (float)commandValue / COMMAND_MAX;
                    int r = (int)(ratio * 255);
                    int g = (int)(100 - ratio * 60);
                    int b = (int)(255 - ratio * 200);
                    _panelCommandSegments[i].BackColor = Color.FromArgb(
                        Math.Min(255, r),
                        Math.Max(0, g),
                        Math.Max(0, b));

                    // ラベルの色を白に
                    if (_panelCommandSegments[i].Controls.Count > 0)
                    {
                        ((Label)_panelCommandSegments[i].Controls[0]).ForeColor = Color.White;
                    }
                }
                else
                {
                    // 非アクティブ領域は暗い色
                    _panelCommandSegments[i].BackColor = Color.FromArgb(60, 60, 60);
                    if (_panelCommandSegments[i].Controls.Count > 0)
                    {
                        ((Label)_panelCommandSegments[i].Controls[0]).ForeColor = Color.Gray;
                    }
                }
            }

            // ギャップ
            _panelEBGap.Left = activeEnd;
            _panelEBGap.Top = 0;
            _panelEBGap.Height = barHeight;

            // EBゾーン
            _panelEBZone.Left = barWidth - EB_ZONE_WIDTH;
            _panelEBZone.Top = 0;
            _panelEBZone.Height = barHeight;
            _panelEBZone.BackColor = _isEmergency
                ? Color.FromArgb(255, 50, 50)
                : Color.FromArgb(160, 0, 0);

            if (_isEmergency)
            {
                _labelValue.Text = "非常ブレーキ";
                _labelValue.ForeColor = Color.OrangeRed;
            }
            else
            {
                _labelValue.Text = "指令値: " + _currentCommandValue.ToString();
                _labelValue.ForeColor = Color.Cyan;
            }
        }


        // ---- ユーティリティ ----
        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();
            base.OnFormClosed(e);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ElectricCommandForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "ElectricCommandForm";
            this.ResumeLayout(false);

        }
    }
}