using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO.Ports;
using FTD2XX_NET;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Drawing.Imaging;




namespace valves_data
{
    public partial class Form1 : Form
    {

        Pump_ctrl oPump_ctrl = new Pump_ctrl();

        int tfix = 200;
        int tdelay = 200;
        bool init_value = true;
 
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVNODES_CHANGED = 0x0007;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEREMOVEPENDING = 0x8003;
        UInt32 ftdiDeviceCount = 0;
        FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
        // Create new instance of the FTDI device class
        FTDI myFtdiDevice = new FTDI();
        int device_detected = 0;
 
        public Stream filehdl;
        public StreamWriter write_hdl;

        FTDI.FT_DEVICE_INFO_NODE[] device_info = new FTDI.FT_DEVICE_INFO_NODE[4];
        string serial_number;

        bool pump_enabled = false;

        int threshold = 0;

        public Form1()
        {
            InitializeComponent();

            this.Deactivate += new EventHandler(Form1_Deactivate);

            device_detected = ftdi_detect();

            pictureBox1.Image = Properties.Resources.Red_indicator;
            pump_enabled = false;
            pictureBox2.Image = Properties.Resources.Red_indicator;

            this.FormClosing += form_Closing;



        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_DEVICECHANGE:
      
                    device_detected = ftdi_detect();
                    break;


            }
            base.WndProc(ref m);
        }




        private int ftdi_detect()
        {


            
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
                ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
                ftStatus = myFtdiDevice.GetDeviceList(device_info);



                if (ftdiDeviceCount >= 1)
                {

                 
                    for (int i = 0; i < ftdiDeviceCount; i++)
                    {
                        if (device_info[i].SerialNumber == "FTYKC8OQ" || device_info[i].SerialNumber == "FTYKC7N9")
                        {
                            serial_number = device_info[i].SerialNumber;
                            textBox_usb.Text = "Valve Controller Connected";
                            return 1;

                        }
                        else
                        {
                            textBox_usb.Text = " ";
                            serial_number = "0";
                 
                        }

                    }

                    return -1;

                }
                else
                {
                    textBox_usb.Text = " ";
                    serial_number = "0";
                    
                    return -1;
                }
            

        }



        private void form_Closing(object sender, CancelEventArgs e)
        {

            if (myFtdiDevice.IsOpen == true)
            {
                myFtdiDevice.Close();
            }

        }




        private void button_Pump_open_USB_Click(object sender, EventArgs e)
        {
            bool usb_status;
            if (serial_number != "0")
            {
                usb_status = oPump_ctrl.Pump_USB_open(myFtdiDevice, serial_number);
                if (usb_status == true)
                {
                    pictureBox1.Image = Properties.Resources.Green_indicator;
                    pump_enabled = true;
                }
                else
                {
                    pictureBox1.Image = Properties.Resources.Red_indicator;
                    pump_enabled = false;
                }
            }
        }

        private void button_Pump_file_save_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "txt files (*.txt)|*.txt";
            saveFileDialog1.FileName = "";
            saveFileDialog1.ShowDialog();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

            filehdl = saveFileDialog1.OpenFile();
            write_hdl = new StreamWriter(filehdl);
            oPump_ctrl.Pump_file_open(filehdl, write_hdl);
            pictureBox2.Image = Properties.Resources.Green_indicator;

        }

        private void button_Pump_close_USB_Click(object sender, EventArgs e)
        {
            oPump_ctrl.Pump_USB_Close();
            pictureBox1.Image = Properties.Resources.Red_indicator;
            pump_enabled = false;

        }

        private void button_Pump_close_file_Click(object sender, EventArgs e)
        {
            oPump_ctrl.Pump_File_Close();
            pictureBox2.Image = Properties.Resources.Red_indicator;
        }

        private void button_Pump_update_Click(object sender, EventArgs e)
        {

            if (checkBox_init_high.Checked == true)
                init_value = true;
            else
                init_value = false;
            tfix = int.Parse(textBox_tfix.Text);
            tdelay = int.Parse(textBox_tdelay.Text);

            oPump_ctrl.PumpUpdate(tfix, tdelay, init_value);

        }

        private void button_Pump_event_Click(object sender, EventArgs e)
        {
            if (pump_enabled==true)
                oPump_ctrl.PumpEvent();

        }





        // motion detection

        bool top_corner = false;
        bool bottom_corner = false;
        int x_top = 0;
        int y_top = 0;
        int x_bot = 100;
        int y_bot = 100;

        
        bool init_image = false;
        Bitmap b_init = new Bitmap(100, 100);

        //plotting
        int x_plot = 0;
        bool reset = true;

        //averaging
        int[] diff_array = new int[16];
        int i_array = 0;
        int average;
        int average_sub = 0;
        int average_sub_prev = 0;

        int zero_crosses = 0;

        bool graphics = true;


        void Form1_Deactivate(object sender, EventArgs e)
        {
            if (top_corner == true)
            {
                x_top = Cursor.Position.X;
                textBox1.Text = Cursor.Position.X.ToString();
                y_top = Cursor.Position.Y;
                textBox2.Text = Cursor.Position.Y.ToString();

                top_corner = false;
            }

            if (bottom_corner == true)
            {
                x_bot = Cursor.Position.X;
                textBox3.Text = Cursor.Position.X.ToString();
                y_bot = Cursor.Position.Y;
                textBox4.Text = Cursor.Position.Y.ToString();
                bottom_corner = false;
                capture_screen();

                if (timer1.Enabled == false)
                {
                    timer1.Enabled = true;
                }


            }


        }

        private void button_top_corner_Click(object sender, EventArgs e)
        {
            top_corner = true;
            init_image = false;
            if (timer1.Enabled == true)
                timer1.Stop();
        }

        private void button_bottom_corner_Click(object sender, EventArgs e)
        {
            bottom_corner = true;
            init_image = false;


        }

        private void capture_screen()
        {


            int width = x_bot - x_top;
            int height = y_bot - y_top;
            int thresh = trackBar1.Value;
            //Create the Bitmap
            Bitmap b = new Bitmap(width, height);


            int difference = 0;


            //Create the Graphic Variable with screen Dimensions
            Graphics g = Graphics.FromImage(b);
            //Copy Image from the screen
            g.CopyFromScreen(x_top, y_top, 0, 0, b.Size);



            for (int x = 0; x < b.Width; x++)
            {
                for (int y = 0; y < b.Height; y++)
                {
                    Color pxl = b.GetPixel(x, y);
                    Color b_w;
                    int avg = (pxl.R + pxl.G + pxl.B) / 3;
                    if (avg > thresh)
                        b_w = Color.FromArgb(255, 255, 255);
                    else
                        b_w = Color.FromArgb(0, 0, 0);

                    b.SetPixel(x, y, b_w);

                    // comapre with initial image pixs

                    if (init_image == true && b.GetPixel(x, y) != b_init.GetPixel(x, y))
                    {
                        difference++;


                    }



                }
            }

            if (graphics == true)
                pictureBox_frame.Image = b;



            if (init_image == false)
            {
                b_init = new Bitmap(width, height);
                b_init = b;
                init_image = true;
                pictureBox_background.Image = b_init;

            }






            // averaging
            if (i_array >= 16)
            {
                i_array = 0;

            }

            diff_array[i_array] = difference;
            i_array++;
            average = 0;
            for (int i = 0; i < 16; i++)
            {
                average = average + diff_array[i];
            }
            average = average / 16;



            // find crossing from zero


            average_sub = difference - average;
            if (average_sub >= threshold && average_sub_prev < threshold)
            {
                zero_crosses++;
                textBox5.Text = zero_crosses.ToString();
                
                if (pump_enabled==true)
                    oPump_ctrl.PumpEvent();

            }


            average_sub_prev = average_sub;


            // visualize the chart
            if (graphics == true)
                visualize(difference, average);



        }

        private void timer1_Tick(object sender, EventArgs e)
        {

            capture_screen();

        }

        private void button_save_subtract_Click(object sender, EventArgs e)
        {
            init_image = false;
        }


        private void visualize(int difference, int _average)
        {
            if (reset == true)
            {
                chart1.Series[0].Points.Clear();
                chart1.Series[1].Points.Clear();
                chart1.Series[2].Points.Clear();
                x_plot = 0;
                reset = false;
            }
            else
            {


                chart1.Series[0].Points.AddXY(x_plot, difference);
                chart1.Series[1].Points.AddXY(x_plot, _average);
                chart1.Series[2].Points.AddXY(x_plot, average_sub);
                x_plot++;
            }



        }

        private void button_reset_Click(object sender, EventArgs e)
        {
            reset = true;
        }

        private void checkBox_graphics_CheckedChanged(object sender, EventArgs e)
        {
            if (graphics == true)
                graphics = false;
            else
                graphics = true;

        }

        private void button_threshold_Click(object sender, EventArgs e)
        {

            if (!int.TryParse(textBox_threshold.Text, out threshold))
            {
                textBox_threshold.Text = "0";
                threshold = 0;
            }
            
                        
        }

        private void textBox_enter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button_threshold_Click((object)sender, (EventArgs)e);
            }
        }

 

    }
}
