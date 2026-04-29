using System;
using System.Drawing;
using System.Windows.Forms;

namespace TrainCrewSAPController
{
    /// <summary>
    /// マウスカーソルのX位置でSAP圧を決定するウィンドウ
    /// 左端デッドゾーン=0kPa / アクティブエリア=0〜400kPa / ギャップ=400kPa固定 / EBゾーン=非常ブレーキ
    /// </summary>
    public class CursorInputForm : Form
    {
        // ---- 定数 ----
        private const float SAP_MIN = 0.0f;
        private const float SAP_MAX = 400.0f;
        /// <summary>ポーリング間隔(ms)。小さいほど追従が速い</summary>
        private const int POLL_INTERVAL_MS = 4;
        /// <summary>左端デッドゾーンの幅(px)</summary>
        private const int DEAD_ZONE_WIDTH = 100;
        /// <summary>EBゾーンとアクティブエリアの隙間の幅(px)</summary>
        private const int EB_GAP_WIDTH = 150;
        /// <summary>右端EBゾーンの幅(px)</summary>
        private const int EB_ZONE_WIDTH = 100;
        /// <summary>非常ブレーキのノッチ値</summary>
        public const int EB_NOTCH = -9;

        // ---- イベント ----
        /// <summary>SAP圧が変化したときに発火。引数はkPa値(0.0〜400.0)</summary>
        public event Action<float> SAPValueChanged;
        /// <summary>非常ブレーキ状態が変化したときに発火。引数はEB中かどうか</summary>
        public event Action<bool> EmergencyBrakeChanged;

        // ---- フィールド ----
        private float _currentSAPValue = 0.0f;
        private bool _isEmergency = false;

        // ---- コントロール ----
        private Label _labelTitle;
        private Label _labelValue;
        private Panel _panelBar;
        private Panel _panelDeadZone;
        private Panel _panelIndicator;
        private Panel _panelEBGap;
        private Panel _panelEBZone;
        private Timer _pollTimer;

        public CursorInputForm()
        {
            InitializeComponents();
            SetupTimer();
        }


        /// <summary>外部からSAP圧を設定してUIを同期する</summary>
        public void SetSAPValue(float kPa)
        {
            _currentSAPValue = Clamp(kPa, SAP_MIN, SAP_MAX);
            _isEmergency = false;
            UpdateUI();
        }

        // ---- 初期化 ----
        private void InitializeComponents()
        {
            this.Text = "カーソル入力";
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
                Text = "SAP圧入力",
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
                Text = "0.00 kPa",
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

            // ギャップパネル（400kPa固定エリア）
            _panelEBGap = new Panel
            {
                BackColor = Color.FromArgb(80, 40, 40), // ← 赤系に変更
                Top = 0,
                Width = EB_GAP_WIDTH,
                Height = 0
            };
            _panelEBGap.Controls.Add(new Label
            {
                Text = "400",
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

            // 現在値インジケーター
            _panelIndicator = new Panel
            {
                BackColor = Color.DodgerBlue,
                Top = 4,
                Left = DEAD_ZONE_WIDTH,
                Height = 0
            };

            _panelBar.Controls.Add(_panelIndicator);
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
                // 左デッドゾーン → 0kPa
                bool wasEB = _isEmergency;
                _isEmergency = false;
                float newValue = SAP_MIN;
                if (Math.Abs(newValue - _currentSAPValue) < 0.005f && !wasEB) return;
                _currentSAPValue = newValue;
                UpdateUI();
                SAPValueChanged?.Invoke(_currentSAPValue);
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
                // ギャップエリア → 400kPa固定
                bool wasEB = _isEmergency;
                _isEmergency = false;
                float newValue = SAP_MAX;
                if (Math.Abs(newValue - _currentSAPValue) < 0.005f && !wasEB) return;
                _currentSAPValue = newValue;
                UpdateUI();
                SAPValueChanged?.Invoke(_currentSAPValue);
                if (wasEB) EmergencyBrakeChanged?.Invoke(false);
            }
            else
            {
                // アクティブエリア → 0〜400kPa
                bool wasEB = _isEmergency;
                _isEmergency = false;
                int activeWidth = activeEnd - DEAD_ZONE_WIDTH;
                float ratio = (float)(cursorOnPanel.X - DEAD_ZONE_WIDTH) / activeWidth;
                float newValue = Clamp(SAP_MIN + ratio * (SAP_MAX - SAP_MIN), SAP_MIN, SAP_MAX);
                if (Math.Abs(newValue - _currentSAPValue) < 0.005f && !wasEB) return;
                _currentSAPValue = newValue;
                UpdateUI();
                SAPValueChanged?.Invoke(_currentSAPValue);
                if (wasEB) EmergencyBrakeChanged?.Invoke(false);
            }
        }

        // ---- UI更新 ----
        private void UpdateUI()
        {
            if (_panelBar == null || _panelIndicator == null) return;

            int barWidth = _panelBar.ClientSize.Width;
            int barHeight = _panelBar.ClientSize.Height;
            if (barWidth <= 0) return;

            int activeEnd = barWidth - EB_GAP_WIDTH - EB_ZONE_WIDTH;

            // デッドゾーン
            _panelDeadZone.Top = 0;
            _panelDeadZone.Height = barHeight;

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

            // インジケーター
            int activeWidth = activeEnd - DEAD_ZONE_WIDTH;
            float ratio = (_currentSAPValue - SAP_MIN) / (SAP_MAX - SAP_MIN);
            int indicatorWidth = (int)(activeWidth * ratio);

            _panelIndicator.Left = DEAD_ZONE_WIDTH;
            _panelIndicator.Top = 4;
            _panelIndicator.Width = Math.Max(0, indicatorWidth);
            _panelIndicator.Height = Math.Max(0, barHeight - 8);

            int r = (int)(ratio * 255);
            int g = (int)(100 - ratio * 60);
            int b = (int)(255 - ratio * 200);
            _panelIndicator.BackColor = Color.FromArgb(
                Math.Min(255, r),
                Math.Max(0, g),
                Math.Max(0, b));

            if (_isEmergency)
            {
                _labelValue.Text = "非常ブレーキ";
                _labelValue.ForeColor = Color.OrangeRed;
            }
            else
            {
                _labelValue.Text = _currentSAPValue.ToString("F2") + " kPa";
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();
            base.OnFormClosed(e);
        }
    }
}