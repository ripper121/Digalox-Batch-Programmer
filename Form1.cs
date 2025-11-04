using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Digalox_Batch_Programmer
{
    public partial class Form1 : Form
    {
        private SerialPort? _serialPort;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            /*
            try
            {
                _serialPort = OpenComPort("COM8");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to open COM8: {ex.Message}", "Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            */
        }

        /// <summary>
        /// Sends a string to the currently opened serial port and returns the response string.
        /// This method polls the port until the provided timeout elapses.
        /// Throws if the port is not open.
        /// </summary>
        /// <param name="data">String data to send.</param>
        /// <param name="readTimeoutMs">How long to wait for a response in milliseconds (default1000ms).</param>
        /// <returns>Response string received from the port (may be empty if nothing received).</returns>
        public string SendAndReceive(string data, int readTimeoutMs = 1000)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("data must not be null or empty", nameof(data));

            if (_serialPort == null || !_serialPort.IsOpen)
                throw new InvalidOperationException("Serial port is not open.");

            var sb = new StringBuilder();
            var end = DateTime.UtcNow + TimeSpan.FromMilliseconds(readTimeoutMs);

            lock (_serialPort)
            {
                // clear buffers to avoid reading stale data
                try
                {
                    _serialPort.DiscardInBuffer();
                }
                catch { }
                try
                {
                    _serialPort.DiscardOutBuffer();
                }
                catch { }

                // write
                _serialPort.Write(data);

                // poll for response until timeout
                while (DateTime.UtcNow < end)
                {
                    try
                    {
                        if (_serialPort.BytesToRead > 0)
                        {
                            sb.Append(_serialPort.ReadExisting());
                            // if the device uses newline-terminated responses, return early when newline seen
                            if (sb.ToString().IndexOfAny(new[] { '\r', '\n' }) >= 0)
                                break;
                        }
                    }
                    catch (TimeoutException)
                    {
                        // ignore and continue until overall timeout
                    }

                    Thread.Sleep(20);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Opens and returns an opened SerialPort for the specified port name.
        /// Throws an exception if the port is not available or cannot be opened.
        /// </summary>
        /// <param name="portName">The COM port name (e.g. "COM3").</param>
        /// <param name="baudRate">Baud rate (default9600).</param>
        /// <param name="parity">Parity (default None).</param>
        /// <param name="dataBits">Data bits (default8).</param>
        /// <param name="stopBits">Stop bits (default One).</param>
        /// <returns>An opened SerialPort instance. Caller is responsible for disposing it.</returns>
        public SerialPort OpenComPort(string portName, int baudRate = 19200, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            if (string.IsNullOrWhiteSpace(portName))
                throw new ArgumentException("portName must be a valid COM port name like 'COM3'.", nameof(portName));

            var available = SerialPort.GetPortNames();
            bool exists = Array.Exists(available, p => string.Equals(p, portName, StringComparison.OrdinalIgnoreCase));
            if (!exists)
            {
                throw new InvalidOperationException($"Port '{portName}' not found. Available ports: {string.Join(", ", available)}");
            }

            var sp = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            try
            {
                sp.Open();
                if (!sp.IsOpen)
                {
                    sp.Dispose();
                    throw new InvalidOperationException($"Failed to open port '{portName}'.");
                }

                return sp;
            }
            catch
            {
                sp.Dispose();
                throw;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            closeComPort();
        }

        void closeComPort()
        {
            if (_serialPort != null)
            {
                try
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }
        }


        private void buttonOpen_Click(object sender, EventArgs e)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                    _serialPort = OpenComPort("COM8");

                var response = SendAndReceive("identify?\r",1000);
                MessageBox.Show(this, $"Response: {response}", "Serial Response", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error sending to COM port: {ex.Message}", "Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
