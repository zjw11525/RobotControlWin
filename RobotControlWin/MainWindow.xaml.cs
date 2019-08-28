using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TwinCAT.Ads;
using System.IO;
using System.ComponentModel;
using System.Timers;
using System.Threading;
using MQTTnet;
using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;


namespace RobotControlWin
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //通讯数据定义
        private TcAdsClient tcclient;//定义通讯协议
        private MqttClient mqttClient = null;

        public VariableInfo variables;
        public MainWindow()
        {
            InitializeComponent();
            Task.Run(async () => { await ConnectMqttServerAsync(); });
            this.Closing += Window_Closing;
            //通讯协议
            tcclient = new TcAdsClient();
            tcclient.Connect(851);
            //System.Timers.Timer timer = new System.Timers.Timer(100);
            //timer.AutoReset = true;
            //timer.Elapsed += DataUpdate;
            //timer.Start();

            //写入句柄的结构体
            variables.indexGroup = (int)AdsReservedIndexGroups.SymbolValueByHandle;
            variables.indexOffset = tcclient.CreateVariableHandle("MAIN.Pos_arr1");
            variables.length = 48 * 5000;

            //double[,] Joint_states = new double[2000, 6];
            //double t = 0;
            //for (int i = 0; i < 2000; i++)
            //{
            //    for (int j = 0; j < 6; j++)
            //    {
            //        Joint_states[i, j] = Math.Sin(t / 1000 * Math.PI);
            //    }
            //    t = t + 1;
            //}
            //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            //sw.Start();
            ////同步到TwinCAT
            //BinaryReader reader = new BinaryReader(BlockWrite(variables, Joint_states));
            //sw.Stop();
            //TimeSpan ts = sw.Elapsed;
            //Console.WriteLine("DateTime costed for Shuffle function is: {0}ms", ts.TotalMilliseconds);
        }
        //句柄的结构体
        public struct VariableInfo
        {
            public int indexGroup;
            public int indexOffset;
            public int length;
        }

        //0xF081指令，将Stream写入PLC的内存地址，注意方法名为ReadWrite
        private AdsStream BlockWrite(VariableInfo variables, double[,] joint_states)
        {
            //分配内存
            int rdLength = 4;
            int wrLength = 12 + 48 * 5000;

            BinaryWriter writer = new BinaryWriter(new AdsStream(wrLength));

            // Write data for handles into the ADS stream
            writer.Write(variables.indexGroup);
            writer.Write(variables.indexOffset);
            writer.Write(variables.length);


            // Write data to send to PLC behind the structure
            for (int i = 0; i < 5000; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    writer.Write(joint_states[i, j]);
                }
            }

            // Sum command to write the data into the PLC
            AdsStream rdStream = new AdsStream(rdLength);
            tcclient.ReadWrite(0xF081, 1, rdStream, (AdsStream)writer.BaseStream);
            return rdStream;
        }
        private async Task ConnectMqttServerAsync()
        {
            if (mqttClient == null)
            {
                mqttClient = new MqttClientFactory().CreateMqttClient() as MqttClient;
                mqttClient.ApplicationMessageReceived += MqttClient_ApplicationMessageReceived;
                mqttClient.Connected += MqttClient_Connected;
                mqttClient.Disconnected += MqttClient_Disconnected;
            }

            try
            {
                var options = new MqttClientTcpOptions
                {
                    Server = "127.0.0.1",
                    ClientId = "win7-PC",
                    UserName = "u001",
                    Password = "p001",
                    CleanSession = true
                };

                await mqttClient.ConnectAsync(options);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke((new Action(() =>
                {
                    MQTT.AppendText($"连接到MQTT服务器失败！" + Environment.NewLine + ex.Message + Environment.NewLine);
                })));
            }
        }

        private void DataUpdate(object sender, ElapsedEventArgs e)
        {
            //double[] ActualAngle = new double[6];
            //double[] ActualPos = new double[3];
            //int hvar = new int(); //定义句柄变量
            //for (int i = 0; i < 6; i++)
            //{
            //    string str = $"GVL.OutActPos[{i}]";
            //    hvar = tcclient.CreateVariableHandle(str);
            //    ActualAngle[i] = (double)(tcclient.ReadAny(hvar, typeof(double)));
            //    tcclient.DeleteVariableHandle(hvar);
            //}
            //for (int i = 0; i < 3; i++)
            //{
            //    string str = $"GVL.Pos_Now[{i}]";
            //    hvar = tcclient.CreateVariableHandle(str);
            //    ActualPos[i] = (double)(tcclient.ReadAny(hvar, typeof(double)));
            //    tcclient.DeleteVariableHandle(hvar);
            //}
            //Application.Current.Dispatcher.Invoke(new Action(() => 
            //{
            //    string anglestr = null;
            //    for (int i = 0; i < 6; i++) 
            //        anglestr += ActualAngle[i].ToString("F4") + Environment.NewLine;

            //    anglestr += Environment.NewLine;
            //    for (int i = 0; i < 3; i++)
            //        anglestr += ActualPos[i].ToString("F4") + Environment.NewLine;
            //    DataFromPLC.Text = anglestr;
            //}));
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (tcclient != null)
                tcclient.Dispose();
        }
        private void MqttClient_Connected(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke((new Action(() =>
            {
                MQTT.AppendText("已连接到MQTT服务器！" + Environment.NewLine);
            })));

            string topic = "/ros_joint_states";

            if (!mqttClient.IsConnected)
            {
                MessageBox.Show("MQTT客户端尚未连接！");
                return;
            }
            mqttClient.SubscribeAsync(new List<TopicFilter> {
                new TopicFilter(topic, MqttQualityOfServiceLevel.AtMostOnce)
            });
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                MQTT.AppendText($"已订阅[{topic}]主题" + Environment.NewLine);
            }));
        }

        private void MqttClient_Disconnected(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke((new Action(() =>
            {
                MQTT.AppendText("已断开MQTT连接！" + Environment.NewLine);
            })));
        }

        private void MqttClient_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            string str = null;
            double[,] Joint_states = new double[5000, 6];

            Application.Current.Dispatcher.Invoke((new Action(() =>
            {

                str = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                string[] strarray = str.Split(',');

                str = null;

                double[] Time   = new double[(strarray.Length - 1) / 7];
                double[] Joint0 = new double[(strarray.Length - 1) / 7];
                double[] Joint1 = new double[(strarray.Length - 1) / 7];
                double[] Joint2 = new double[(strarray.Length - 1) / 7];
                double[] Joint3 = new double[(strarray.Length - 1) / 7];
                double[] Joint4 = new double[(strarray.Length - 1) / 7];
                double[] Joint5 = new double[(strarray.Length - 1) / 7];

                for (int i = 0; i < (strarray.Length - 1) / 7; i++)
                {
                    Time[i]   = Convert.ToDouble(strarray[i * 7]) / 1000.0;
                    Joint0[i] = Convert.ToDouble(strarray[i * 7 + 1]);
                    Joint1[i] = Convert.ToDouble(strarray[i * 7 + 2]);
                    Joint2[i] = Convert.ToDouble(strarray[i * 7 + 3]);
                    Joint3[i] = Convert.ToDouble(strarray[i * 7 + 4]);
                    Joint4[i] = Convert.ToDouble(strarray[i * 7 + 5]);
                    Joint5[i] = Convert.ToDouble(strarray[i * 7 + 6]);

                    str += Time[i].ToString() + "  " + Joint0[i].ToString() + "  " + Joint1[i].ToString() + 
                           "  " + Joint2[i].ToString() + "  " +Joint3[i].ToString() + 
                           "  " + Joint4[i].ToString() + "  " + Joint5[i].ToString() + Environment.NewLine;
                }
                MQTT.Text = str;
                //三次样条插补
                //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                //sw.Start();

                SPLine sp = new SPLine();
                sp.Init(Time, Joint0);
                int num = 0;
                for(double i = 0;i<Time[((strarray.Length - 1) / 7)-1];i+=0.002)
                    Joint_states[num++,0] = sp.Interpolate(i);
                for (int i = num; i < 5000; i++)
                    Joint_states[i, 0] = sp.Interpolate(Time[((strarray.Length - 1) / 7) - 1]);

                sp.Init(Time, Joint1);num = 0;
                for (double i = 0; i < Time[((strarray.Length - 1) / 7) - 1]; i += 0.002)
                    Joint_states[num++, 1] = sp.Interpolate(i);
                for (int i = num; i < 5000; i++)
                    Joint_states[i, 1] = sp.Interpolate(Time[((strarray.Length - 1) / 7) - 1]);

                sp.Init(Time, Joint2);num = 0;
                for (double i = 0; i < Time[((strarray.Length - 1) / 7) - 1]; i += 0.002)
                    Joint_states[num++, 2] = sp.Interpolate(i);
                for (int i = num; i < 5000; i++)
                    Joint_states[i, 2] = sp.Interpolate(Time[((strarray.Length - 1) / 7) - 1]);

                sp.Init(Time, Joint3);num = 0;
                for (double i = 0; i < Time[((strarray.Length - 1) / 7) - 1]; i += 0.002)
                    Joint_states[num++, 3] = sp.Interpolate(i);
                for (int i = num; i < 5000; i++)
                    Joint_states[i, 3] = sp.Interpolate(Time[((strarray.Length - 1) / 7) - 1]);

                sp.Init(Time, Joint4);num = 0;
                for (double i = 0; i < Time[((strarray.Length - 1) / 7) - 1]; i += 0.002)
                    Joint_states[num++, 4] = sp.Interpolate(i);
                for (int i = num; i < 5000; i++)
                    Joint_states[i, 4] = sp.Interpolate(Time[((strarray.Length - 1) / 7) - 1]);

                sp.Init(Time, Joint5);num = 0;
                for (double i = 0; i < Time[((strarray.Length - 1) / 7) - 1]; i += 0.002)
                    Joint_states[num++, 5] = sp.Interpolate(i);
                for (int i = num; i < 5000; i++)
                    Joint_states[i, 5] = sp.Interpolate(Time[((strarray.Length - 1) / 7) - 1]);
                //同步到TwinCAT
                BinaryReader reader = new BinaryReader(BlockWrite(variables, Joint_states));
                //sw.Stop();
                //TimeSpan ts = sw.Elapsed;
                //Console.WriteLine("DateTime costed for Shuffle function is: {0}ms", ts.TotalMilliseconds);

                //int error = reader.ReadInt32();
                //if (error != (int)AdsErrorCode.NoError)
                //    System.Diagnostics.Debug.WriteLine(String.Format("Unable to read variable", error));


                //double[] PosAccess = new double[4];
                //double[] PosIN = new double[4];//{ -0.24032,-0.163511, 1.37,1 };


                //for (int i = 0; i < 3; i++)
                //{
                //    PosIN[i] = Convert.ToDouble(strarray[i]);
                //}
                //PosIN[3] = 1;


                //double[] RT = new double[16]{ 0.0059, -0.2663, -0.9811, 1.7265,
                //                              0.9354,0.0942,0.1211,0.0697,
                //                              0.0101,-0.9493,0.2592,0.1217,
                //                              0,    0,   0,   1};

                //for (int i = 0; i < 4; i++)
                //{
                //    double temp = RT[i] * PosIN[i];
                //    PosAccess[0] += temp;
                //}
                //for (int i = 0; i < 4; i++)
                //{
                //    double temp = RT[i+4] * PosIN[i];
                //    PosAccess[1] += temp;
                //}
                //for (int i = 0; i < 4; i++)
                //{
                //    double temp = RT[i + 8] * PosIN[i];
                //    PosAccess[2] += temp;
                //}
                //PosAccess[2] += 0.05;

                //PosAccess[0] = Convert.ToDouble(PosX.Text);
                //PosAccess[1] = Convert.ToDouble(PosY.Text);
                //PosAccess[2] = Convert.ToDouble(PosZ.Text);

                //CartesianMovePlan(PosAccess);
            })));
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {          
            double[] PosAccess = new double[3];
            PosAccess[0] = Convert.ToDouble(PosX.Text);
            PosAccess[1] = Convert.ToDouble(PosY.Text);
            PosAccess[2] = Convert.ToDouble(PosZ.Text);
            CartesianMovePlan(PosAccess);
        }
        public void CartesianMovePlan(double[] PosAccessAbsolute)
        {
            int hvar = new int();

            for (int i = 0; i < 3; i++)
            {
                hvar = tcclient.CreateVariableHandle($"GVL.OutCMovePosString[{i}]");
                tcclient.WriteAny(hvar, PosAccessAbsolute[i].ToString(), new int[] { 5 });
            }
            hvar = tcclient.CreateVariableHandle("MAIN.CurrentJob");
            tcclient.WriteAny(hvar, (short)13);

            hvar = tcclient.CreateVariableHandle("MAIN.CurrentJob");
            tcclient.WriteAny(hvar, (short)12);

            ThreadStart CMoveMonitor = new ThreadStart(CMoveMonitorThread);
            Thread cMoveMonitorThread = new Thread(CMoveMonitor);
            cMoveMonitorThread.Start();
            tcclient.DeleteVariableHandle(hvar);
        }

        private void CMoveMonitorThread()
        {
            int hvar = new int();
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                KineStatus.Text = "CMoving..." + Environment.NewLine;
            }));

            hvar = tcclient.CreateVariableHandle("GVL.ExtPosReady");
            while ((bool)tcclient.ReadAny(hvar, typeof(bool)) == true);

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                KineStatus.Text = "CMoveOK!" + Environment.NewLine;
            }));

            //double[] PosAccess = new double[3];
            //PosAccess[0] = 0;
            //PosAccess[1] = 0;
            //PosAccess[2] = -0.1;
            //LineMovePlan(PosAccess);
        }
        public void LineMovePlan(double[] PosAccessRelative)
        {
            int hvar = new int();

            for (int i = 0; i < 3; i++)
            {
                hvar = tcclient.CreateVariableHandle($"GVL.OutMovePosString[{i}]");
                tcclient.WriteAny(hvar, PosAccessRelative[i].ToString(), new int[] { 5 });
            }

            hvar = tcclient.CreateVariableHandle("MAIN.CurrentJob");
            tcclient.WriteAny(hvar, (short)10);

            hvar = tcclient.CreateVariableHandle("MAIN.CurrentJob");
            tcclient.WriteAny(hvar, (short)2);

            ThreadStart LineMoveMonitor = new ThreadStart(LineMoveMonitorThread);
            Thread lineMoveMonitorThread = new Thread(LineMoveMonitor);
            lineMoveMonitorThread.Start();
            tcclient.DeleteVariableHandle(hvar);
        }

        private void LineMoveMonitorThread()
        {
            int hvar = new int();
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                KineStatus.Text = "LineMoving..." + Environment.NewLine;
            }));

            hvar = tcclient.CreateVariableHandle("GVL.ExtPosReady");
            while ((bool)tcclient.ReadAny(hvar, typeof(bool)) == true);

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                KineStatus.Text = "LineMoveOK!" + Environment.NewLine;
            }));

            //double[] PosAccess = new double[3];
            //PosAccess[0] = 0;
            //PosAccess[1] = 0;
            //PosAccess[2] = 0.1;
            //LineMovePlan(PosAccess);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MQTT.Text = null;
        }
    }
}
