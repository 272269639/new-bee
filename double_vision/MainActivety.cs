// Two Cameras Vision

//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Text.RegularExpressions;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Video;
using AForge.Video.DirectShow;
using AwokeKnowing.GnuplotCSharp;
using System.IO.Ports;


namespace TwoCamerasVision
{
    public partial class MainActivety : Form
    {  
        // 摄像头列表
        FilterInfoCollection videoDevices;
        //检测到的对象
        DetectedObjectsForm detectedObjectsForm;
        //调整目标颜色
        TuneObjectFilterForm tuneObjectFilterForm;
        SerialPort BluetoothConnection = new SerialPort();
        ColorFiltering colorFilter = new ColorFiltering( );
        GrayscaleBT709 grayFilter  = new GrayscaleBT709( );
        // use two blob counters, so the could run in parallel in two threads
        BlobCounter blobCounter1 = new BlobCounter( );
        BlobCounter blobCounter2 = new BlobCounter( );

        private AutoResetEvent camera1Acquired = null;
        private AutoResetEvent camera2Acquired = null;
        private Thread trackingThread = null;
        private string Status, BlueToothReceivedData;
        //两个摄像头对应物体的坐标
        private float x1, y1, x2, y2;
        private int i = 0;
        private int dely = 10000;
        private float T=26;
        private float F=420;
        private double targetZ=0;
        private string value = "";
        private double v;
        private double targetX = 0;
        private double targetY = 0;
        private String XX;
        public String YY;//缩短精度
        private double X;
        private double Y;
        public double[] Xs = new double[200];
        public double[] Ys = new double[200];
        public double[] Zs = new double[200];
        public double xInput;
        public double yInput;
        public double zInput;
        public double tempx;
        public double tempz;
        public int flag=1;
        /*-----------------------------------------------------------------------------------------------------
         * ---------------------串口部分开始-------------------------------------------------------------------
        ------------------------------------------------------------------------------------------------------*/
        private SerialPort port = null;
        private void MainActivety_Load(object sender, EventArgs e)
        {
            this.cmbSerials.Items.AddRange(SerialPort.GetPortNames());
            this.cmbSerials.SelectedIndex = this.cmbSerials.Items.Count - 1;
            //Arduino一般在最后一个串口
            comboBox2.Text = "9600";
        }
        private void MainActivety_Closing(object sender, FormClosingEventArgs e)
        {
            this.DisposeSerialPort();
        }

        /// <summary>
        /// 初始化串口实例
        /// </summary>
        private void InitialSerialPort()
        {           
            //获得所有的端口列表，并显示在列表内
            PortList.Items.Clear();
            string[] Ports = SerialPort.GetPortNames();

            for (int i = 0; i < Ports.Length; i++)
            {
                string s = Ports[i].ToUpper();
                Regex reg = new Regex("[^COM\\d]", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                s = reg.Replace(s, "");

                PortList.Items.Add(s);
            }
            if (Ports.Length > 1) PortList.SelectedIndex = 1;
            string str = comboBox2.Text;
            try
            {
                string portName = this.cmbSerials.SelectedItem.ToString();
                port = new SerialPort(portName,System.Int32.Parse(str) );
                port.Encoding = Encoding.ASCII;
                port.DataReceived += port_DataReceived;
                port.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("初始化串口发生错误：" + 
                    ex.Message, "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 关闭并销毁串口实例
        /// </summary>
        private void DisposeSerialPort()
        {
            if (port != null)
            {
                try
                {
                    if (port.IsOpen)
                    {
                        port.Close();
                    }
                    port.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("关闭串口发生错误：" +
                        ex.Message, "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        /// <summary>
        /// 从串口读取数据并转换为字符串形式
        /// </summary>
        /// <returns></returns>
        private string ReadSerialData()
        {
            
            try
            {
                if (port != null && port.BytesToRead > 0)
                {
                    value = port.ReadExisting();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取串口数据发生错误：" +
                    ex.Message, "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return value;
        }

        /// <summary>
        /// 在读取到数据时刷新文本框的信息
        /// </summary>
        private void RefreshInfoTextBox()
        {
            string value = this.ReadSerialData();
            Action<string> setValueAction = text => this.txtInfo.Text += text;

            if (this.txtInfo.InvokeRequired)
            {
                this.txtInfo.Invoke(setValueAction, value);
                try
                {

                    v = Convert.ToDouble(value);
                }
                catch
                {
                    value = "0";
                }

            }
            else
            {
                setValueAction(value);
            }
                    }
        void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            this.RefreshInfoTextBox();
   
        }

/*-----------------------------------------------------------------------------------------------------
 * ---------------------串口部分结束-----------------------------------------------------------------------
------------------------------------------------------------------------------------------------------*/
        public MainActivety( )
        {   
            InitializeComponent( );

            //Get all port list for selection
            //获得所有的端口列表，并显示在列表内
            PortList.Items.Clear();
            string[] Ports = SerialPort.GetPortNames();

            for (int i = 0; i < Ports.Length; i++)
            {
                string s = Ports[i].ToUpper();
                Regex reg = new Regex("[^COM\\d]", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                s = reg.Replace(s, "");

                PortList.Items.Add(s);
            }
            if (Ports.Length > 1) PortList.SelectedIndex = 1;

  
        
            // show device list
            try
            {
                // enumerate video devices
                videoDevices = new FilterInfoCollection( FilterCategory.VideoInputDevice );

                if ( videoDevices.Count == 0 )
                {
                    throw new Exception( );
                }

                for ( int i = 1, n = videoDevices.Count; i <= n; i++ )
                {
                    string cameraName = i + " : " + videoDevices[i - 1].Name;

                    camera1Combo.Items.Add( cameraName );
                    camera2Combo.Items.Add( cameraName );
                }

                // check cameras count
                if ( videoDevices.Count == 1 )
                {
                    camera2Combo.Items.Clear( );

                    camera2Combo.Items.Add( "只有一个摄像头被找到" );
                    camera2Combo.SelectedIndex = 0;
                    camera2Combo.Enabled = false;
                }
                else
                {
                    camera2Combo.SelectedIndex = 1;
                }
                camera1Combo.SelectedIndex = 0;
            }
            catch
            {
                startButton.Enabled = false;

                camera1Combo.Items.Add( "没有发现摄像头" );
                camera2Combo.Items.Add("没有发现摄像头");

                camera1Combo.SelectedIndex = 0;
                camera2Combo.SelectedIndex = 0;

                camera1Combo.Enabled = false;
                camera2Combo.Enabled = false;
            }
            //物体颜色设置
            colorFilter.Red = new IntRange(70,106);
            colorFilter.Green = new IntRange(21, 60);
            colorFilter.Blue = new IntRange(0,85);

            // configure blob counters
            blobCounter1.MinWidth = 20;
            blobCounter1.MinHeight = 20;
            blobCounter1.FilterBlobs = true;
            blobCounter1.ObjectsOrder = ObjectsOrder.Size;

            blobCounter2.MinWidth = 20;
            blobCounter2.MinHeight = 20;
            blobCounter2.FilterBlobs = true;
            blobCounter2.ObjectsOrder = ObjectsOrder.Size;
        }

        /// <summary>
        /// 蓝牙部分函数
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void BlueToothDataReceived(object o, SerialDataReceivedEventArgs e)
        {
            //int length = BluetoothConnection.ReadByte();
            Thread.Sleep(1000);
            int length = 13;
            BlueToothReceivedData = DateTime.Now.ToLongTimeString() + "\r\n";
            BlueToothReceivedData += "收到字节数：" + length + "\r\n";

            byte[] data = new byte[length];
            BluetoothConnection.Read(data, 0, length);
            for (int i = 0; i < length; i++)
            {
                BlueToothReceivedData += string.Format("data[{0}] = {1}\r\n", i, data[i]);
            }
            //receive close message
            if (length == 3 && data[0] == 255 && data[1] == 255 && data[2] == 255)
            {
                //Stop
                Status = "正在断开蓝牙设备";
                BluetoothConnection.Close();
                BluetoothConnection.Dispose();
                BluetoothConnection = null;
                button3.Enabled = true;
                Status = "蓝牙断开成功";
            }
        }

        private void BlueToothDataSend(string data)
        {

            BluetoothConnection.Write(data);
            //Status = "发送数据字节数：" + length;
        }
            //蓝牙部分结束
        // Main form closing - stop cameras停止相机
        private void MainForm_FormClosing( object sender, FormClosingEventArgs e )
        {
            StopCameras( );
        }
       /// <summary>
       /// 摄像机控制部分
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        //开始相机按钮
        private void startButton_Click( object sender, EventArgs e )
        {
            StartCameras( );
            timer1.Start();
            startButton.Enabled = false;
            stopButton.Enabled = true;
        }

        // On "Stop" button click - stop cameras停止相机
        private void stopButton_Click( object sender, EventArgs e )
        {
            StopCameras( );
            timer1.Stop();
            startButton.Enabled = true;
            stopButton.Enabled = false;
        }

        // Start cameras打开相机的函数
        private void StartCameras( )
        {
            // create first video source
            VideoCaptureDevice videoSource1 = new VideoCaptureDevice( videoDevices[camera1Combo.SelectedIndex].MonikerString );
            videoSource1.DesiredFrameRate = 10;

            videoSourcePlayer1.VideoSource = videoSource1;
            videoSourcePlayer1.Start( );

            // create second video source
            if ( camera2Combo.Enabled == true )
            {
                System.Threading.Thread.Sleep( 500 );

                VideoCaptureDevice videoSource2 = new VideoCaptureDevice( videoDevices[camera2Combo.SelectedIndex].MonikerString );
                videoSource2.DesiredFrameRate = 10;

                videoSourcePlayer2.VideoSource = videoSource2;
                videoSourcePlayer2.Start( );
            }

            camera1Acquired = new AutoResetEvent( false );
            camera2Acquired = new AutoResetEvent( false );
            // start tracking thread
            trackingThread = new Thread( new ThreadStart( TrackingThread ) );
            trackingThread.Start( );
        }

        // Stop cameras
        private void StopCameras( )
        {
            videoSourcePlayer1.SignalToStop( );
            videoSourcePlayer2.SignalToStop( );

            videoSourcePlayer1.WaitForStop( );
            videoSourcePlayer2.WaitForStop( );

            if ( detectedObjectsForm != null )
            {
                detectedObjectsForm.UpdateObjectPicture( 0, null );
                detectedObjectsForm.UpdateObjectPicture( 1, null );
            }

            if ( trackingThread != null )
            {
                // signal tracking thread to stop
                x1 = y1 = x2 = y2 = -1;
                camera1Acquired.Set( );
                camera2Acquired.Set( );

                trackingThread.Join( );
            }
        }

        //摄像机控制部分结束

        // 目标观测事件
        private void showDetectedObjectsButton_Click( object sender, EventArgs e )
        {
            if ( detectedObjectsForm == null )
            {
                detectedObjectsForm = new DetectedObjectsForm( );
            }

            detectedObjectsForm.Show( );
        }

        /// <summary>
        /// 颜色滤波器调节部分
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tuneObjectFilterButton_Click( object sender, EventArgs e )
        {
            if ( tuneObjectFilterForm == null )
            {
                tuneObjectFilterForm = new TuneObjectFilterForm( );
                tuneObjectFilterForm.OnFilterUpdate += new EventHandler( tuneObjectFilterForm_OnFilterUpdate );

                tuneObjectFilterForm.RedRange   = colorFilter.Red;
                tuneObjectFilterForm.GreenRange = colorFilter.Green;
                tuneObjectFilterForm.BlueRange  = colorFilter.Blue;
            }
            tuneObjectFilterForm.Show( );
        }

        
        private void tuneObjectFilterForm_OnFilterUpdate( object sender, EventArgs eventArgs )
        {
            colorFilter.Red   = tuneObjectFilterForm.RedRange;
            colorFilter.Green = tuneObjectFilterForm.GreenRange;
            colorFilter.Blue  = tuneObjectFilterForm.BlueRange;
        }

        private void objectDetectionCheck_CheckedChanged( object sender, EventArgs e )
        {
            if ( ( !objectDetectionCheck.Checked ) && ( detectedObjectsForm != null ) )
            {
                detectedObjectsForm.UpdateObjectPicture( 0, null );
                detectedObjectsForm.UpdateObjectPicture( 1, null );
            }
        }
        //颜色滤波器调节结束
        /// <summary>
        /// 找到目标并定时显示目标实时位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            label3.Text = "输出信息：" + "目标X位置=" + XX + "  cm;  " +
                ",目标Y位置=" + YY + "  cm;  " + "目标Z位置=" + targetZ + " cm;  " + ",摄像机转的角度=" + value + Status;
            label7.Text = "tempx=" + tempx + "  tempz=" + tempz;
            Xs[i] = targetX;
            Ys[i] = targetY;
            Zs[i] = targetZ;
            i++;
            if (i >= 200)
            { i = 0; }
            if (BluetoothConnection.IsOpen && cancel.Enabled == true)
            {
                traceAimPlace();
            }
            if (BluetoothConnection.IsOpen &&cancel.Enabled == false)
            {
                BlueToothDataSend("3");

            }
        }

        // 1号相机物体识别框
        private void videoSourcePlayer1_NewFrame( object sender, ref Bitmap image )
        {
            if ( objectDetectionCheck.Checked )
            {
                Bitmap objectImage = colorFilter.Apply( image );

                // lock image for further processing
                BitmapData objectData = objectImage.LockBits( new Rectangle( 0, 0, image.Width, image.Height ),
                    ImageLockMode.ReadOnly, image.PixelFormat );

                // grayscaling
                UnmanagedImage grayImage = grayFilter.Apply( new UnmanagedImage( objectData ) );

                // unlock image
                objectImage.UnlockBits( objectData );

                // locate blobs 
                blobCounter1.ProcessImage( grayImage );
                Rectangle[] rects = blobCounter1.GetObjectsRectangles( );

                if ( rects.Length > 0 )
                {
                    Rectangle objectRect = rects[0];

                    // draw rectangle around derected object
                    Graphics g = Graphics.FromImage( image );

                    using ( Pen pen = new Pen( Color.FromArgb( 160, 255, 160 ), 3 ) )
                    {
                        g.DrawRectangle( pen, objectRect );
                    }

                    g.Dispose( );

                    // get object's center coordinates relative to image center
                    lock ( this )
                    {
                        x1 = ( objectRect.Left + objectRect.Right - objectImage.Width ) / 2;
                        y1 = ( objectImage.Height - ( objectRect.Top + objectRect.Bottom ) ) / 2;
                        // map to [-1, 1] range
                        //x1 /= ( objectImage.Width / 2 );
                        //y1 /= ( objectImage.Height / 2 );

                        camera1Acquired.Set( );
                    }
                }
                    
                if ( detectedObjectsForm != null )
                    detectedObjectsForm.UpdateObjectPicture( 0, objectImage );
            }
        }

        // 2号相机物体识别框
        private void videoSourcePlayer2_NewFrame( object sender, ref Bitmap image )
        {
            if ( objectDetectionCheck.Checked )
            {
                Bitmap objectImage = colorFilter.Apply( image );

                // lock image for further processing
                BitmapData objectData = objectImage.LockBits( new Rectangle( 0, 0, image.Width, image.Height ),
                    ImageLockMode.ReadOnly, image.PixelFormat );

                // grayscaling
                UnmanagedImage grayImage = grayFilter.Apply( new UnmanagedImage( objectData ) );

                // unlock image
                objectImage.UnlockBits( objectData );

                // locate blobs 
                blobCounter2.ProcessImage( grayImage );
                Rectangle[] rects = blobCounter2.GetObjectsRectangles( );

                if ( rects.Length > 0 )
                {
                    Rectangle objectRect = rects[0];

                    // draw rectangle around derected object
                    Graphics g = Graphics.FromImage( image );

                    using ( Pen pen = new Pen( Color.FromArgb( 160, 255, 160 ), 3 ) )
                    {
                        g.DrawRectangle( pen, objectRect );
                    }

                    g.Dispose( );

                    // 将坐标转换为中央位置为零点
                    lock ( this )
                    {
                        x2 = ( objectRect.Left + objectRect.Right - objectImage.Width ) / 2;
                        y2 = ( objectImage.Height - ( objectRect.Top + objectRect.Bottom ) ) / 2;
                      //  x2 /= ( objectImage.Width / 2 );
                       // y2 /= ( objectImage.Height / 2 );

                        camera2Acquired.Set( );
                    }
                }

                if ( detectedObjectsForm != null )
                    detectedObjectsForm.UpdateObjectPicture( 1, objectImage );
            }
        }
        //目标识别结束

        //物体追踪线程
        private void TrackingThread()
        {
            
         
            
         
            while (true)
            {
                camera1Acquired.WaitOne();
                camera2Acquired.WaitOne();

                lock (this)
                {
                    // 线程停止
                    if ((x1 == -1) && (y1 == -1) && (x2 == -1) && (y2 == -1))
                    {
                        break;
                    }
                    //相机上的XY坐标
                    X = (x1 + x2) / 2;
                    Y = (y1 + y2) / 2;
                    //实际中XYZ的坐标
                    /* flag = F * T / (x1 - x2);
                     targetX = flag * 0.7 * (System.Math.Cos(v / 360 * 6.28));
                     targetY = flag * 0.7 * (System.Math.Sin(v / 360 * 6.28));
                     //转换为CM
                     targetZ = Y * 0.143;*/
                    if (!checkBox1.Checked)
                    {
                        targetX = colculateX(T, x1, x2);
                        targetY = colculateY(T, x1, x2, y1);
                        targetZ = colculateZ(T, x1, x2, F);
                    }
                    else
                    {
                        /*float temp;
                        //实际中XYZ的坐标
                         temp = F * T / (x1 - x2);
                         targetX = temp * 0.7 * (System.Math.Cos(v / 360 * 6.28));
                         targetY = temp * 0.7 * (System.Math.Sin(v / 360 * 6.28));
                         //转换为CM
                         targetZ = Y * 0.143;*/
                        float temp;
                        //实际中XYZ的坐标
                         temp = F * T / (x1 - x2);
                        targetX = polarX(temp, v, 1);
                        targetY = polarY(Y);
                        targetZ = polarZ(temp, v, 1);
                    }
                    XX = targetX.ToString("0.000");
                    YY = targetY.ToString("0.000"); 
                    
                    if (button2.Enabled == true&&port.IsOpen)
                    {
                        if (X < -15)
                        {
                            port.Write("0");

                        }
                        if (X > 15)
                        {
                            port.Write("1");
                        }
                    }

                }

                }

            }

        public void traceAimPlace()
        {

            tempx = Double.Parse(xin.Text) - targetX;
            if (tempx > 10)
            {
                BluetoothConnection.Write("2");
                label8.Text="输出2";
            }
            else if (tempx < -10)
            {
                BluetoothConnection.Write("1");
                label8.Text = "输出1";
            }
            else
            {
             
                
                tempz = Double.Parse(zin.Text) - targetZ;
                if (flag == 1)
                {
                    for (int i = 0; i < dely; i++)
                    {
                        label8.Text = "输出1";
                    }
                    if (tempz > 0)
                    {
                        BluetoothConnection.Write("4");
                        label8.Text = "输出4";
                    }
                    else
                    {
                        BluetoothConnection.Write("5");
                        label8.Text = "输出5";
                    }
                    flag = 0;
                }
                else 
                {
                    if (tempz > 1)
                    {
                        BluetoothConnection.Write("1");
                        label8.Text = "输出6";
                    }
                    else if (tempz < -1)
                    {
                        BluetoothConnection.Write("2");
                        label8.Text = "输出7";
                    }
                    else
                    {
                        BluetoothConnection.Write("3");
                        label8.Text = "输出3";
                    }
                }   
            }
        }
        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {
            string i = textBox1.Text;
            F = float.Parse(i);
        }

        private void cmbSerials_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void txtInfo_TextChanged(object sender, EventArgs e)
        {

        }

        private void videoSourcePlayer2_Click(object sender, EventArgs e)
        {
            
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void videoSourcePlayer1_Click(object sender, EventArgs e)
        {

        }

        public void button3_Click(object sender, EventArgs e)
        {
               GnuPlot.Set("xrange[-100:100]", "yrange[-100:100]", "zrange[-100:100]");
               GnuPlot.SPlot(Xs, Ys, Zs, "with points pointtype 8 lc rgb 'blue'");
 
        }

        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }

        private void textBox111_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void camera2Combo_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {

            BlueToothDataSend(SendMessage.Text);
        }

        private void start_Click(object sender, EventArgs e)
        {
            start.Enabled = false;
            cancel.Enabled = true;
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            start.Enabled = true;
            cancel.Enabled = false;
            flag = 1;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void SendMessage_TextChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click_1(object sender, EventArgs e)
        {
                if (!BluetoothConnection.IsOpen)
            {
                //Start
                Status = "正在连接蓝牙设备";
                BluetoothConnection = new SerialPort();
                button4.Enabled = false;
                BluetoothConnection.PortName = PortList.SelectedItem.ToString();
                BluetoothConnection.Open();
                BluetoothConnection.ReadTimeout = 10000;
                BluetoothConnection.DataReceived += new SerialDataReceivedEventHandler(BlueToothDataReceived);
                Status = "蓝牙连接成功";
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            string i = textBox2.Text;
            T = float.Parse(i);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = true;
            this.InitialSerialPort();
           port.Write("*");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = false;
            this.DisposeSerialPort();
        }
        public static double colculateX(float t, float x1, float x2)
        {

            return t * x1 / (x1 - x2);
        }
        public static double colculateY(float t, float x1, float x2, float y1)
        {
            return t * y1 / (x1 - x2);
        }

        private void xin_TextChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        public double colculateZ(float t, float x1, float x2, float f)
        {
            return t * f / (x1 - x2);
        }
        public double polarX(double z, double Angle, float temp)
        {
            return z * temp * (System.Math.Cos(Angle / 360 * 6.28));
        }
        public double polarZ(double z, double Angle, float temp)
        {
            return z * temp * (System.Math.Sin(Angle / 360 * 6.28));
        }
        public double polarY(double Y)
        {
            return Y;
        }

    }
}
