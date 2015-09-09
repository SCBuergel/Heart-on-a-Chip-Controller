using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using FTD2XX_NET;
using System.IO;
using System.Diagnostics;


namespace valves_data
{
    class Pump_ctrl
    {

        public int[] time_array = new int[6] { 0, 100, 200, 300, 400, 500 };
        public byte[] data_array = new byte[6] { 0x01, 0x00, 0x02, 0x00, 0x04, 0x00 };

        public int[] time_trunc = new int[6] { 0, 0, 0, 0, 0, 0 };
        public byte[] data_trunc = new byte[6] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        public int i_trunc = 6;
        int truncing_time = 20;

        public int[] delay_trunc = new int[6] { 0, 0, 0, 0, 0, 0 };

        private static Timer _timer;


        public int pointer = 0;
        FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
        // Create new instance of the FTDI device class
        FTDI myFtdiDevice = new FTDI();
        Int32 num_to_write = 1;
        UInt32 num_written = 0;
        public byte[] byte_write = new byte[1] { 0x00 };

        public Stream filehdl;
        public StreamWriter write_hdl;

        public System.Diagnostics.Stopwatch stop_watch = new Stopwatch();
 
        public bool file_isopen = false;

        public void set_values(int tadj, int tdelay, bool t_on_off)
        {

            if ((tdelay * 2) <= tadj)
            {
                time_array[0] = 0;
                time_array[1] = tdelay;
                time_array[2] = 2 * tdelay;
                time_array[3] = tadj;
                time_array[4] = tadj + tdelay;
                time_array[5] = tadj + (2 * tdelay);

                data_array[0] = 0x01;
                data_array[1] = 0x03;
                data_array[2] = 0x07;
                data_array[3] = 0x06;
                data_array[4] = 0x04;
                data_array[5] = 0x00;

            }

            else if ((tdelay * 2) > tadj && tdelay <= tadj)
            {

                time_array[0] = 0;
                time_array[1] = tdelay;
                time_array[2] = tadj;
                time_array[3] = 2 * tdelay;
                time_array[4] = tadj + tdelay;
                time_array[5] = tadj + (2 * tdelay);

                data_array[0] = 0x01;
                data_array[1] = 0x03;
                data_array[2] = 0x02;
                data_array[3] = 0x06;
                data_array[4] = 0x04;
                data_array[5] = 0x00;

            }

            else if (tdelay > tadj)
            {
                time_array[0] = 0;
                time_array[1] = tadj;
                time_array[2] = tdelay;
                time_array[3] = tdelay + tadj;
                time_array[4] = 2 * tdelay;
                time_array[5] = (2 * tdelay) + tadj;

                data_array[0] = 0x01;
                data_array[1] = 0x00;
                data_array[2] = 0x02;
                data_array[3] = 0x00;
                data_array[4] = 0x04;
                data_array[5] = 0x00;


            }


            if (t_on_off == true)
            {
                for (int i=0; i<=5;i++)
                    data_array[i] ^= 0x07; 
                    

            }

        }

        public void trunc_func()
        {

            int[] time_oppos = new int[6] { 0, 0, 0, 0, 0, 0 };
            byte[] data_oppos = new byte[6] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            int j = 0;

            time_oppos[0] = time_array[5];
            data_oppos[0] = data_array[5];



            for (int i = 5; i >= 0; i--)
            {
                if (time_oppos[j] - time_array[i] > truncing_time)
                {

                    j++;
                    time_oppos[j] = time_array[i];
                    data_oppos[j] = data_array[i];


                }

            }


            for (int k = j; k >= 0; k--)
            {
                time_trunc[j - k] = time_oppos[k];
                data_trunc[j - k] = data_oppos[k];

            }



            for (int k = j + 1; k <= 5; k++)
            {
                time_trunc[k] = 0;
                data_trunc[k] = 0x00;

            }

            i_trunc = j + 1;





        }


        public void event_to_delay()
        {

            int i;
            for (i = 0; i < i_trunc - 1; i++)
                delay_trunc[i] = time_trunc[i + 1] - time_trunc[i];

            delay_trunc[i] = 200000;

            for (int k = i + 1; k <= 5; k++)
                delay_trunc[k] = 0x00;



        }


        public void timer_init()
        {

            _timer = new Timer();
            _timer.Elapsed += timer_method;
            


        }

        public void timer_method(Object source, ElapsedEventArgs e)
        {
            
            byte_write[0] = data_trunc[pointer];

            
            if (myFtdiDevice.IsOpen == true && ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                ftStatus = myFtdiDevice.Write(byte_write, num_to_write, ref num_written);
            }

            _timer.Interval = delay_trunc[pointer];


            pointer++;

            if (pointer >= i_trunc)
            {
                pointer = 0;
                _timer.Enabled = false;
            }
            




        }



        public void PumpUpdate(int t_fixed, int t_delay, bool high_low)
        {
            set_values(t_fixed, t_delay, high_low);
            trunc_func();
            event_to_delay();
            pointer = 0;

            if (write_hdl != null && filehdl != null)
            {

                if (file_isopen == true)
                {
                    write_hdl.Write("t_fixed=");
                    write_hdl.Write(t_fixed);
                    write_hdl.Write("    t_delay=");
                    write_hdl.Write(t_delay);
                    write_hdl.Write("    ");
                    if (high_low == true)
                        write_hdl.Write("t_fixed is t_off");
                    else
                        write_hdl.Write("t_fixed is t_on");
                    write_hdl.WriteLine("  ");

                }

                stop_watch.Restart();
            }
        }

        
        public void PumpEvent()
        {
            
            pointer = 0;

            if(file_isopen ==true)
                write_hdl.WriteLine(stop_watch.Elapsed);
            
            _timer.Interval = 1;
            _timer.Enabled = true;
 

        }




        public bool Pump_USB_open(FTDI device,string ser_number)
        {
            myFtdiDevice = device;
            ftdi_open(ser_number);
            timer_init();
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
                return true;
            else
                return false;

        }

        public void Pump_file_open(Stream File_handle, StreamWriter Write_handle)
        {
            filehdl = File_handle;
            write_hdl = Write_handle;
            file_isopen = true;

        }

        private void ftdi_open(string serial_number)
        {

            if (ftStatus == FTDI.FT_STATUS.FT_OK && serial_number != "0")
                ftStatus = myFtdiDevice.OpenBySerialNumber(serial_number);


            if (ftStatus == FTDI.FT_STATUS.FT_OK)
                ftStatus = myFtdiDevice.SetBitMode(0xFF, FTDI.FT_BIT_MODES.FT_BIT_MODE_ASYNC_BITBANG);


        }



        public void Pump_USB_Close()
        {


            _timer.Enabled = false;
            _timer.Dispose();
            stop_watch.Stop();
            
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                if (myFtdiDevice.IsOpen == true)
                    myFtdiDevice.Close();
            }

        }

        public void Pump_File_Close()
        {


            _timer.Enabled = false;
            _timer.Dispose();
            stop_watch.Stop();

            if (write_hdl != null)
                write_hdl.Close();

            if (filehdl != null)
                filehdl.Close();

            file_isopen = false;

        }


    }
}
