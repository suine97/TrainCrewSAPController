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
        /// コンストラクタ
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            FormClosing += MainForm_FormClosing;

            //初期化。起動時のみの呼び出しで大丈夫です。
            TrainCrewInput.Init();

            //最小値、最大値を設定
            TrackBar_SAPValue.Minimum = 0;
            TrackBar_SAPValue.Maximum = 40000;
            //初期値を設定
            TrackBar_SAPValue.Value = 30000;
            //描画される目盛りの刻みを設定
            TrackBar_SAPValue.TickFrequency = 5000;
            //スライダーをキーボードやマウス、
            //PageUp,Downキーで動かした場合の移動量設定
            TrackBar_SAPValue.SmallChange = 1000;
            TrackBar_SAPValue.LargeChange = 1000;
            //値が変更された際のイベントハンドラを追加
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
            //SAP圧を送信
            if (IsSendSAPEnable) SendSAPValueFromTrackBar();
        }

        /// <summary>
        /// TrackBar_SAPValue_MouseWheelイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackBar_SAPValue_MouseWheel(object sender, MouseEventArgs e)
        {
            //マウスホイールのスクロール量を1ずつに変更
            //Deltaが正の場合は上方向のスクロール、負の場合は下方向のスクロール
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
                {
                    TrackBar_SAPValue.Value -= 1;
                }
            }
            //イベントをハンドルしたことを通知
            ((HandledMouseEventArgs)e).Handled = true;
        }

        /// <summary>
        /// Button_SendSAP_Clickイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_SendSAP_Click(object sender, EventArgs e)
        {
            //SAP圧を送信
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
                //Beep音が鳴らないように設定
                e.SuppressKeyPress = true;
                //SAP圧を送信
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
            TrainCrewInput.Dispose();
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
            //SAP圧を送信
            if (IsSendSAPEnable) SendSAPValueFromTrackBar();
        }

        /// <summary>
        /// SAP値送信メソッド(TextBox)
        /// </summary>
        private void SendSAPValueFromTextBox()
        {
            float strValue = float.Parse(TextBox_SAPValue.Text);
            if (0.0f >= strValue) strValue = 0.0f;
            if (strValue >= 400.0f) strValue = 400.0f;

            //TrackBarに反映
            TrackBar_SAPValue.Value = (int)(strValue * 100);
            //SAP圧を送信
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

            //SAP圧を送信
            if (IsSendSAPEnable) TrainCrewInput.SetBrakeSAP(strValue);
        }
    }
}
