using System;
using System.Drawing;
using System.Windows.Forms;

namespace TrainCrewSAPController
{
    /// <summary>
    /// マウスカーソルのX位置で自動空気ブレーキ指令値を決定するウィンドウ
    /// 左端デッドゾーン=0 / アクティブエリア=0〜4（5段階） / ギャップ=4固定 / EBゾーン=非常ブレーキ
    /// </summary>
    public class AutomaticAirBrakeCommandForm : Form
    {
        // ---- 定数 ----
        private const int COMMAND_MIN = 0;
        private const int COMMAND_MAX = 4;
        /// <summary>ポーリング間隔(ms)。小さいほど追従が速い</summary>
        private const int POLL_INTERVAL_MS = 4;
        /// <summary>SAP圧変化タイマーの間隔(ms)</summary>
        private const int SAP_TIMER_INTERVAL_MS = 50;
        /// <summary>ユルメ・ブレーキ位置に入ってからSAP圧変化が始まるまでのラグ(ms)</summary>
        private const int SAP_CHANGE_DELAY_MS = 500;
        /// <summary>SAP圧の最小値(kPa)</summary>
        private const float SAP_MIN = 0.0f;
        /// <summary>SAP圧の最大値(kPa)</summary>
        private const float SAP_MAX = 400.0f;
        /// <summary>左端デッドゾーンの幅(px)</summary>
        private const int DEAD_ZONE_WIDTH = 75;
        /// <summary>各指令値セグメントの幅(px) - 指令値0～4の順</summary>
        private readonly int[] SEGMENT_WIDTHS = new int[] { 70, 120, 70, 120, 120 };
        /// <summary>各指令値に対応する名称 - 指令値0～4の順</summary>
        private readonly string[] SEGMENT_LABELS = new string[] { "ユルメ", "保ち", "抜取り", "重り", "ブレーキ" };
        /// <summary>EBゾーンとアクティブエリアの隙間の幅(px)</summary>
        private const int EB_GAP_WIDTH = 75;
        /// <summary>右端EBゾーンの幅(px)</summary>
        private const int EB_ZONE_WIDTH = 100;

        // ---- イベント ----
        /// <summary>自動空気ブレーキ指令値が変化したときに発火。引数は指令値(0〜4)</summary>
        public event Action<int> CommandValueChanged;
        /// <summary>非常ブレーキ状態が変化したときに発火。引数はEB中かどうか</summary>
        public event Action<bool> EmergencyBrakeChanged;
        /// <summary>SAP圧が変化したときに発火。引数はSAP圧(kPa)</summary>
        public event Action<float> SAPValueChanged;

        // ---- フィールド ----
        private int _currentCommandValue = 0;
        private bool _isEmergency = false;
        private float _currentSAPkPa = 0.0f;
        /// <summary>ユルメ時のSAP圧減算レート(kPa/秒)</summary>
        private float _decreaseRatePerSec = 0.0f;
        /// <summary>ブレーキ時のSAP圧加算レート(kPa/秒)</summary>
        private float _increaseRatePerSec = 0.0f;
        /// <summary>指令値が最後に変化した時刻。ラグ計算に使用</summary>
        private DateTime _commandChangedAt = DateTime.MinValue;

        // ---- コントロール ----
        private Label _labelTitle;
        private Label _labelValue;
        private Panel _panelBar;
        private Panel _panelDeadZone;
        private Panel[] _panelCommandSegments; // 指令値0～4の各段階パネル
        private Panel _panelEBGap;
        private Panel _panelEBZone;
        private Timer _pollTimer;
        private Timer _sapChangeTimer;

        public AutomaticAirBrakeCommandForm()
        {
            InitializeComponents();
            SetupTimer();
        }

        // ---- 公開メソッド ----

        /// <summary>現在のSAP圧を外部から設定する</summary>
        public void SetSAPValue(float kPa)
        {
            _currentSAPkPa = Clamp(kPa, SAP_MIN, SAP_MAX);
        }

        /// <summary>外部から自動空気ブレーキ指令値を設定してUIを同期する</summary>
        public void SetCommandValue(int command)
        {
            _currentCommandValue = Clamp(command, COMMAND_MIN, COMMAND_MAX);
            _isEmergency = false;
            UpdateUI();
        }

        /// <summary>
        /// ユルメ位置（指令値0）にある場合にSAP圧を減算するレートを設定する
        /// </summary>
        /// <param name="kPaPerSec">1秒あたりの減算量(kPa)。0以上の値を指定</param>
        public void SetDecreaseRate(float kPaPerSec)
        {
            _decreaseRatePerSec = Math.Max(0.0f, kPaPerSec);
        }

        /// <summary>
        /// ブレーキ位置（指令値4）にある場合にSAP圧を加算するレートを設定する
        /// </summary>
        /// <param name="kPaPerSec">1秒あたりの加算量(kPa)。0以上の値を指定</param>
        public void SetIncreaseRate(float kPaPerSec)
        {
            _increaseRatePerSec = Math.Max(0.0f, kPaPerSec);
        }

        // ---- 初期化 ----
        private void InitializeComponents()
        {
            this.Text = "自動空気ブレーキ指令入力";
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
                Text = "空気指令",
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
                Text = SEGMENT_LABELS[0],
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
                BackColor = Color.FromArgb(30, 60, 90),
                Left = 0,
                Top = 0,
                Width = DEAD_ZONE_WIDTH,
                Height = 0
            };
            _panelDeadZone.Controls.Add(new Label
            {
                Text = SEGMENT_LABELS[0],
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(120, 180, 255),
                Font = new Font("MS UI Gothic", 8, FontStyle.Bold)
            });

            // ギャップパネル（指令値4固定エリア）
            _panelEBGap = new Panel
            {
                BackColor = Color.FromArgb(80, 40, 40),
                Top = 0,
                Width = EB_GAP_WIDTH,
                Height = 0
            };
            _panelEBGap.Controls.Add(new Label
            {
                Text = SEGMENT_LABELS[COMMAND_MAX],
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(200, 120, 120),
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

            // 指令値0～4の段階パネルを作成
            _panelCommandSegments = new Panel[5];
            for (int i = 0; i < 5; i++)
            {
                _panelCommandSegments[i] = new Panel
                {
                    BackColor = Color.FromArgb(60, 60, 60),
                    Top = 0,
                    Height = 0
                };

                Label segmentLabel = new Label
                {
                    Text = SEGMENT_LABELS[i],
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
            this.Load += (s, e) => UpdateUI();
        }

        private void SetupTimer()
        {
            _pollTimer = new Timer { Interval = POLL_INTERVAL_MS };
            _pollTimer.Tick += PollTimer_Tick;
            _pollTimer.Start();

            _sapChangeTimer = new Timer { Interval = SAP_TIMER_INTERVAL_MS };
            _sapChangeTimer.Tick += SapChangeTimer_Tick;
            _sapChangeTimer.Start();
        }

        // ---- SAP圧自動増減処理 ----

        /// <summary>指令値が変化してから SAP_CHANGE_DELAY_MS 以上経過しているか</summary>
        private bool IsDelayElapsed()
        {
            return (DateTime.Now - _commandChangedAt).TotalMilliseconds >= SAP_CHANGE_DELAY_MS;
        }

        /// <summary>
        /// ユルメ位置にある場合にSAP圧を減算する。1秒あたり <see cref="_decreaseRatePerSec"/> kPa 減算。
        /// </summary>
        private void DecreaseSAPIfYurume()
        {
            if (_isEmergency || _currentCommandValue != COMMAND_MIN) return;
            if (_decreaseRatePerSec <= 0.0f) return;
            if (!IsDelayElapsed()) return;

            float delta = _decreaseRatePerSec * (SAP_TIMER_INTERVAL_MS / 1000.0f);
            float newSAP = Clamp(_currentSAPkPa - delta, SAP_MIN, SAP_MAX);
            if (Math.Abs(newSAP - _currentSAPkPa) < 0.001f) return;
            _currentSAPkPa = newSAP;
            SAPValueChanged?.Invoke(_currentSAPkPa);
        }

        /// <summary>
        /// ブレーキ位置にある場合にSAP圧を加算する。1秒あたり <see cref="_increaseRatePerSec"/> kPa 加算。
        /// </summary>
        private void IncreaseSAPIfBrake()
        {
            if (_isEmergency || _currentCommandValue != COMMAND_MAX) return;
            if (_increaseRatePerSec <= 0.0f) return;
            if (!IsDelayElapsed()) return;

            float delta = _increaseRatePerSec * (SAP_TIMER_INTERVAL_MS / 1000.0f);
            float newSAP = Clamp(_currentSAPkPa + delta, SAP_MIN, SAP_MAX);
            if (Math.Abs(newSAP - _currentSAPkPa) < 0.001f) return;
            _currentSAPkPa = newSAP;
            SAPValueChanged?.Invoke(_currentSAPkPa);
        }

        private void SapChangeTimer_Tick(object sender, EventArgs e)
        {
            DecreaseSAPIfYurume();
            IncreaseSAPIfBrake();
        }

        // ---- ポーリング処理 ----
        private void PollTimer_Tick(object sender, EventArgs e)
        {
            int barWidth = _panelBar.ClientSize.Width;
            if (barWidth <= 0) return;

            Point cursorOnPanel = _panelBar.PointToClient(Cursor.Position);

            if (cursorOnPanel.X < 0 || cursorOnPanel.X > barWidth) return;
            if (cursorOnPanel.Y < 0 || cursorOnPanel.Y > _panelBar.ClientSize.Height) return;

            int totalSegmentWidth = 0;
            for (int i = 0; i < SEGMENT_WIDTHS.Length; i++) totalSegmentWidth += SEGMENT_WIDTHS[i];
            int activeEnd = DEAD_ZONE_WIDTH + totalSegmentWidth;
            int ebStart = barWidth - EB_ZONE_WIDTH;

            if (cursorOnPanel.X <= DEAD_ZONE_WIDTH)
            {
                // 左デッドゾーン → 指令値0（ユルメ）
                bool wasEB = _isEmergency;
                _isEmergency = false;
                int newValue = COMMAND_MIN;
                if (newValue != _currentCommandValue || wasEB)
                    _commandChangedAt = DateTime.Now;
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
                // ギャップエリア → 指令値4（ブレーキ）固定
                bool wasEB = _isEmergency;
                _isEmergency = false;
                int newValue = COMMAND_MAX;
                if (newValue != _currentCommandValue || wasEB)
                    _commandChangedAt = DateTime.Now;
                if (newValue == _currentCommandValue && !wasEB) return;
                _currentCommandValue = newValue;
                UpdateUI();
                CommandValueChanged?.Invoke(_currentCommandValue);
                if (wasEB) EmergencyBrakeChanged?.Invoke(false);
            }
            else
            {
                // アクティブエリア → 指令値0〜4（各段階の幅は個別指定）
                bool wasEB = _isEmergency;
                _isEmergency = false;
                int positionInActive = cursorOnPanel.X - DEAD_ZONE_WIDTH;

                int cumulativeWidth = 0;
                int commandValue = 0;
                for (int i = 0; i < SEGMENT_WIDTHS.Length; i++)
                {
                    cumulativeWidth += SEGMENT_WIDTHS[i];
                    if (positionInActive < cumulativeWidth)
                    {
                        commandValue = i; // 0始まり
                        break;
                    }
                }

                if (commandValue != _currentCommandValue || wasEB)
                    _commandChangedAt = DateTime.Now;
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

            // デッドゾーン
            _panelDeadZone.Top = 0;
            _panelDeadZone.Height = barHeight;

            // 指令値0～4の各段階パネルを配置・着色
            int currentLeft = DEAD_ZONE_WIDTH;
            for (int i = 0; i < 5; i++)
            {
                int commandValue = i; // 0始まり
                _panelCommandSegments[i].Left = currentLeft;
                _panelCommandSegments[i].Top = 0;
                _panelCommandSegments[i].Width = SEGMENT_WIDTHS[i];
                _panelCommandSegments[i].Height = barHeight;

                currentLeft += SEGMENT_WIDTHS[i];

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

                    if (_panelCommandSegments[i].Controls.Count > 0)
                        ((Label)_panelCommandSegments[i].Controls[0]).ForeColor = Color.White;
                }
                else
                {
                    _panelCommandSegments[i].BackColor = Color.FromArgb(60, 60, 60);
                    if (_panelCommandSegments[i].Controls.Count > 0)
                        ((Label)_panelCommandSegments[i].Controls[0]).ForeColor = Color.Gray;
                }
            }

            // ギャップ
            _panelEBGap.Left = currentLeft;
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
                _labelValue.Text = SEGMENT_LABELS[_currentCommandValue];
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
            _sapChangeTimer.Stop();
            _sapChangeTimer.Dispose();
            base.OnFormClosed(e);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // AutomaticAirBrakeCommandForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "AutomaticAirBrakeCommandForm";
            this.ResumeLayout(false);
        }
    }
}