using System;
using System.Windows.Forms;
using TrainCrew;

namespace TrainCrewSAPController
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// SAP圧制御有効判定
        /// </summary>
        private bool IsSendSAPEnable = false;

        /// <summary>
        /// カーソル入力ウィンドウ
        /// </summary>
        private CursorInputForm _cursorInputForm = null;

        /// <summary>
        /// 電気ブレーキ指令入力ウィンドウ
        /// </summary>
        private ElectricCommandForm _electricCommandForm = null;

        /// <summary>
        /// 自動空気ブレーキ指令入力ウィンドウ
        /// </summary>
        private AutomaticAirBrakeCommandForm _automaticAirBrakeCommandForm = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            FormClosing += MainForm_FormClosing;

            // 初期化。起動時のみの呼び出しで大丈夫です。
            TrainCrewInput.Init();

            // 最小値、最大値を設定
            TrackBar_SAPValue.Minimum = 0;
            TrackBar_SAPValue.Maximum = 40000;
            // 初期値を設定
            TrackBar_SAPValue.Value = 30000;
            // 描画される目盛りの刻みを設定
            TrackBar_SAPValue.TickFrequency = 5000;
            // スライダーをキーボードやマウス、
            // PageUp,Downキーで動かした場合の移動量設定
            TrackBar_SAPValue.SmallChange = 1000;
            TrackBar_SAPValue.LargeChange = 1000;
            // 値が変更された際のイベントハンドラを追加
            TrackBar_SAPValue.ValueChanged += new EventHandler(TrackBar_SAPValue_ValueChanged);
            TrackBar_SAPValue.MouseWheel += new MouseEventHandler(TrackBar_SAPValue_MouseWheel);
        }

        /// <summary>
        /// TrackBar_SAPValue_ValueChangedイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackBar_SAPValue_ValueChanged(object sender, EventArgs e)
        {
            var strValue = TrackBar_SAPValue.Value / 100.0f;
            Label_SAPValue.Text = "SAP圧：" + (strValue).ToString("F2") + "kPa";
            // SAP圧を送信
            if (IsSendSAPEnable) SendSAPValueFromTrackBar();
        }

        /// <summary>
        /// TrackBar_SAPValue_MouseWheelイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackBar_SAPValue_MouseWheel(object sender, MouseEventArgs e)
        {
            // マウスホイールのスクロール量を1ずつに変更
            // Deltaが正の場合は上方向のスクロール、負の場合は下方向のスクロール
            if (e.Delta > 0)
            {
                if (TrackBar_SAPValue.Value + 1 <= TrackBar_SAPValue.Maximum)
                {
                    TrackBar_SAPValue.Value += 1;
                }
            }
            else
            {
                if (TrackBar_SAPValue.Value - 1 >= TrackBar_SAPValue.Minimum)
                    TrackBar_SAPValue.Value -= 1;
            }
            // イベントをハンドルしたことを通知
            ((HandledMouseEventArgs)e).Handled = true;
        }

        /// <summary>
        /// Button_SendSAP_Clickイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_SendSAP_Click(object sender, EventArgs e)
        {
            // SAP圧を送信
            SendSAPValueFromTextBox();
        }

        /// <summary>
        /// TextBox_SAPValue_KeyDownイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_SAPValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // Beep音が鳴らないように設定
                e.SuppressKeyPress = true;
                // SAP圧を送信
                SendSAPValueFromTextBox();
            }
        }

        /// <summary>
        /// MainForm_FormClosingイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // TrainCrewInputのリソース解放
            TrainCrewInput.Dispose();

            // 開いているウィンドウがあれば閉じる
            if (_cursorInputForm != null && !_cursorInputForm.IsDisposed)
            {
                _cursorInputForm.Dispose();
            }
            if (_electricCommandForm != null && !_electricCommandForm.IsDisposed)
            {
                _electricCommandForm.Dispose();
            }
            if (_automaticAirBrakeCommandForm != null && !_automaticAirBrakeCommandForm.IsDisposed)
            {
                _automaticAirBrakeCommandForm.Dispose();
            }
        }

        /// <summary>
        /// CheckBox_TopMost_CheckedChangedイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_TopMost_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = CheckBox_TopMost.Checked;
        }

        /// <summary>
        /// CheckBox_SendSAPEnable_CheckedChangedイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_SendSAPEnable_CheckedChanged(object sender, EventArgs e)
        {
            IsSendSAPEnable = CheckBox_SendSAPEnable.Checked;
            // SAP圧を送信
            if (IsSendSAPEnable) SendSAPValueFromTrackBar();
        }

        /// <summary>
        /// Button_OpenCursorInput_Clickイベント：カーソル入力ウィンドウを開く
        /// </summary>
        private void Button_OpenCursorInput_Click(object sender, EventArgs e)
        {
            // 電気ブレーキ指令入力ウィンドウが開いていたら閉じる
            if (_electricCommandForm != null && !_electricCommandForm.IsDisposed)
            {
                _electricCommandForm.Close();
                _electricCommandForm = null;
            }
            // 自動空気ブレーキ指令入力ウィンドウが開いていたら閉じる
            if (_automaticAirBrakeCommandForm != null && !_automaticAirBrakeCommandForm.IsDisposed)
            {
                _automaticAirBrakeCommandForm.Close();
                _automaticAirBrakeCommandForm = null;
            }

            // 既に開いている場合は前面に出す
            if (_cursorInputForm != null && !_cursorInputForm.IsDisposed)
            {
                _cursorInputForm.BringToFront();
                return;
            }

            _cursorInputForm = new CursorInputForm();
            _cursorInputForm.StartPosition = FormStartPosition.Manual;
            _cursorInputForm.Location = new System.Drawing.Point(this.Left, this.Bottom + 8);

            _cursorInputForm.SAPValueChanged += OnCursorInputSAPValueChanged;
            _cursorInputForm.EmergencyBrakeChanged += OnCursorInputEmergencyBrakeChanged;

            // 現在のTrackBar値をカーソル入力ウィンドウに同期
            _cursorInputForm.SetSAPValue(TrackBar_SAPValue.Value / 100.0f);
            _cursorInputForm.Show(this);
        }

        /// <summary>
        /// Button_OpenElectricCommand_Clickイベント：電気ブレーキ指令入力ウィンドウを開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_OpenElectricCommand_Click(object sender, EventArgs e)
        {
            // カーソル入力ウィンドウが開いていたら閉じる
            if (_cursorInputForm != null && !_cursorInputForm.IsDisposed)
            {
                _cursorInputForm.Close();
                _cursorInputForm = null;
            }
            // 自動空気ブレーキ指令入力ウィンドウが開いていたら閉じる
            if (_automaticAirBrakeCommandForm != null && !_automaticAirBrakeCommandForm.IsDisposed)
            {
                _automaticAirBrakeCommandForm.Close();
                _automaticAirBrakeCommandForm = null;
            }

            // 既に開いている場合は前面に出す
            if (_electricCommandForm != null && !_electricCommandForm.IsDisposed)
            {
                _electricCommandForm.BringToFront();
                return;
            }

            _electricCommandForm = new ElectricCommandForm();
            _electricCommandForm.StartPosition = FormStartPosition.Manual;
            _electricCommandForm.Location = new System.Drawing.Point(this.Left, this.Bottom + 8);

            _electricCommandForm.CommandValueChanged += OnElectricCommandValueChanged;
            _electricCommandForm.EmergencyBrakeChanged += OnElectricCommandEmergencyBrakeChanged;

            // 現在のTrackBar値から電気ブレーキ指令値を計算して同期（0～400kPa → 0～7）
            float currentSAP = TrackBar_SAPValue.Value / 100.0f;
            int commandValue = (int)Math.Round(currentSAP / 400.0f * 7.0f);
            _electricCommandForm.SetCommandValue(commandValue);
            _electricCommandForm.Show(this);
        }

        /// <summary>
        /// Button_OpenAutomaticAirBraleCommand_Clickイベント：自動空気ブレーキ指令入力ウィンドウを開く
        /// </summary>
        private void Button_OpenAutomaticAirBraleCommand_Click(object sender, EventArgs e)
        {
            // カーソル入力ウィンドウが開いていたら閉じる
            if (_cursorInputForm != null && !_cursorInputForm.IsDisposed)
            {
                _cursorInputForm.Close();
                _cursorInputForm = null;
            }
            // 電気ブレーキ指令入力ウィンドウが開いていたら閉じる
            if (_electricCommandForm != null && !_electricCommandForm.IsDisposed)
            {
                _electricCommandForm.Close();
                _electricCommandForm = null;
            }

            // 既に開いている場合は前面に出す
            if (_automaticAirBrakeCommandForm != null && !_automaticAirBrakeCommandForm.IsDisposed)
            {
                _automaticAirBrakeCommandForm.BringToFront();
                return;
            }

            _automaticAirBrakeCommandForm = new AutomaticAirBrakeCommandForm();
            _automaticAirBrakeCommandForm.StartPosition = FormStartPosition.Manual;
            _automaticAirBrakeCommandForm.Location = new System.Drawing.Point(this.Left, this.Bottom + 8);

            _automaticAirBrakeCommandForm.CommandValueChanged += OnAutomaticAirBrakeCommandValueChanged;
            _automaticAirBrakeCommandForm.EmergencyBrakeChanged += OnAutomaticAirBrakeEmergencyBrakeChanged;
            _automaticAirBrakeCommandForm.SAPValueChanged += OnAutomaticAirBrakeSAPValueChanged;

            // レートを設定
            _automaticAirBrakeCommandForm.SetDecreaseRate(70.0f);
            _automaticAirBrakeCommandForm.SetIncreaseRate(60.0f);

            // 現在のSAP圧を同期
            _automaticAirBrakeCommandForm.SetSAPValue(TrackBar_SAPValue.Value / 100.0f);
            _automaticAirBrakeCommandForm.SetCommandValue(3);
            _automaticAirBrakeCommandForm.Show(this);
        }

        /// <summary>
        /// カーソル入力ウィンドウからSAP圧変更通知を受け取る
        /// </summary>
        private void OnCursorInputSAPValueChanged(float kPa)
        {
            // TrackBarに反映（ValueChangedイベント経由でラベルも更新される）
            TrackBar_SAPValue.Value = (int)(kPa * 100);
            if (IsSendSAPEnable) TrainCrewInput.SetBrakeSAP(kPa);
        }

        /// <summary>
        /// カーソル入力ウィンドウから非常ブレーキ変更通知を受け取る
        /// </summary>
        private void OnCursorInputEmergencyBrakeChanged(bool isEB)
        {
            if (!IsSendSAPEnable) return;
            if (isEB)
                TrainCrewInput.SetNotch(CursorInputForm.EB_NOTCH);
            else
                SendSAPValueFromTrackBar();
        }

        /// <summary>
        /// 電気ブレーキ指令入力ウィンドウから指令値変更通知を受け取る
        /// </summary>
        private void OnElectricCommandValueChanged(int command)
        {
            // 電気ブレーキ指令の場合はTrackBarを0に設定
            TrackBar_SAPValue.Value = 0;
            // 電気ブレーキ指令値を直接送信
            if (IsSendSAPEnable) TrainCrewInput.SetBrakeNotch(command);
        }

        /// <summary>
        /// 電気ブレーキ指令入力ウィンドウから非常ブレーキ変更通知を受け取る
        /// </summary>
        private void OnElectricCommandEmergencyBrakeChanged(bool isEB)
        {
            if (!IsSendSAPEnable) return;
            if (isEB)
            {
                // EBゾーンに入った場合は指令値8を送信
                TrainCrewInput.SetBrakeNotch(8);
            }
            // EB→段階への変化時は何もしない（CommandValueChangedで現在値が送信される）
        }

        /// <summary>
        /// 自動空気ブレーキ指令入力ウィンドウから指令値変更通知を受け取る
        /// </summary>
        private void OnAutomaticAirBrakeCommandValueChanged(int command)
        {
            // 指令値変化時はSAP圧の自動増減に任せるため、ここでは何もしない
        }

        /// <summary>
        /// 自動空気ブレーキ指令入力ウィンドウから非常ブレーキ変更通知を受け取る
        /// </summary>
        private void OnAutomaticAirBrakeEmergencyBrakeChanged(bool isEB)
        {
            if (!IsSendSAPEnable) return;
            if (isEB)
                TrainCrewInput.SetNotch(CursorInputForm.EB_NOTCH);
            else
                SendSAPValueFromTrackBar();
        }

        /// <summary>
        /// 自動空気ブレーキ指令入力ウィンドウからSAP圧変更通知を受け取る
        /// </summary>
        private void OnAutomaticAirBrakeSAPValueChanged(float kPa)
        {
            // TrackBarに反映（ValueChangedイベント経由でラベルも更新される）
            TrackBar_SAPValue.Value = (int)(kPa * 100);
            if (IsSendSAPEnable) TrainCrewInput.SetBrakeSAP(kPa);
        }

        /// <summary>
        /// SAP値送信メソッド(TextBox)
        /// </summary>
        private void SendSAPValueFromTextBox()
        {
            float strValue = float.Parse(TextBox_SAPValue.Text);
            if (0.0f >= strValue) strValue = 0.0f;
            if (strValue >= 400.0f) strValue = 400.0f;

            // TrackBarに反映
            TrackBar_SAPValue.Value = (int)(strValue * 100);
            // SAP圧を送信
            if (IsSendSAPEnable) TrainCrewInput.SetBrakeSAP(strValue);
        }

        /// <summary>
        /// SAP値送信メソッド(TrackBar)
        /// </summary>
        private void SendSAPValueFromTrackBar()
        {
            float strValue = (float)(TrackBar_SAPValue.Value / 100.0f);
            if (0.0f >= strValue) strValue = 0.0f;
            if (strValue >= 400.0f) strValue = 400.0f;

            // SAP圧を送信
            if (IsSendSAPEnable) TrainCrewInput.SetBrakeSAP(strValue);
        }
    }
}
