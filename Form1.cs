using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

namespace Digalox_Batch_Programmer
{
    public partial class Form1 : Form
    {
        private SerialPort? _serialPort;
        private int sendIDCounter = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Populate comboBoxComPorts with ports that respond with "TDE" to an identify? query
            comboBoxComPorts.Items.Clear();

            var ports = SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                SerialPort? testPort = null;
                var previous = _serialPort;
                try
                {
                    // Attempt to open the port for testing
                    testPort = OpenComPort(port);

                    // Temporarily use the test port for SendAndReceive
                    _serialPort = testPort;

                    var response = SendAndReceive("identify?\r", 1000);
                    if (!string.IsNullOrEmpty(response) && response.IndexOf("TDE", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        comboBoxComPorts.Items.Add(port);
                        Log($"Detected TDE device on {port}", Color.Yellow);
                    }
                    else
                    {
                        Log($"No TDE response from {port} (response: '{response}')", Color.Orange);
                    }
                }
                catch (Exception ex)
                {
                    // log ports that can't be opened or don't respond
                    Log($"Failed to test port {port}: {ex.Message}", Color.Red);
                }
                finally
                {
                    // restore previous port (do not close it here)
                    _serialPort = previous;

                    // close and dispose the test port if we created it
                    if (testPort != null)
                    {
                        try
                        {
                            if (testPort.IsOpen)
                            {
                                testPort.Close();
                                Log($"Closed test port {port}", Color.Yellow);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Error closing test port {port}: {ex.Message}", Color.Orange);
                        }
                        finally
                        {
                            testPort.Dispose();
                        }
                    }
                }
            }

            // Optionally select the first detected port
            if (comboBoxComPorts.Items.Count > 0)
            {
                comboBoxComPorts.SelectedIndex = 0;
                Log($"Selected port {comboBoxComPorts.SelectedItem}", Color.Yellow);
            }
            else
            {
                Log("No compatible COM ports detected.", Color.Orange);
            }
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
                catch (Exception ex)
                {
                    Log($"DiscardInBuffer error: {ex.Message}", Color.Orange);
                }
                try
                {
                    _serialPort.DiscardOutBuffer();
                }
                catch (Exception ex)
                {
                    Log($"DiscardOutBuffer error: {ex.Message}", Color.Orange);
                }

                // write
                try
                {
                    // NOTE: sendIDCounter is incremented by the caller so we do not increment here to avoid double-counting
                    _serialPort.Write(data);
                    Log($"TX-> {_serialPort.PortName}: '{data.Replace("\r","\\r").Replace("\n", "\\n")}'", Color.Blue);
                }
                catch (Exception ex)
                {
                    Log($"Error writing to {_serialPort.PortName}: {ex.Message}", Color.Red);
                    throw;
                }

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
                        // do not spam the log for each small timeout; we'll log if no response after overall timeout
                    }
                    catch (Exception ex)
                    {
                        Log($"Error reading from {_serialPort.PortName}: {ex.Message}", Color.Red);
                        break;
                    }

                    Thread.Sleep(200);
                }
            }

            var result = sb.ToString();
            if (string.IsNullOrEmpty(result))
            {
                Log($"SendAndReceive timed out waiting for response to '{data.Trim()}' after {readTimeoutMs}ms", Color.Orange);
            }
            else
            {
                Log($"RX<- {_serialPort?.PortName}: '{result.Replace("\r", "\\r").Replace("\n", "\\n")}'", Color.Green);
            }

            return result;
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
                Log($"Port '{portName}' not found. Available ports: {string.Join(", ", available)}", Color.Red);
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
                    Log($"Failed to open port '{portName}'.", Color.Red);
                    throw new InvalidOperationException($"Failed to open port '{portName}'.");
                }

                Log($"Opened port {portName} at {baudRate}bps", Color.Yellow);
                return sp;
            }
            catch (Exception ex)
            {
                try { sp.Dispose(); } catch { }
                Log($"Failed to open port '{portName}': {ex.Message}", Color.Red);
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
                    {
                        _serialPort.Close();
                        Log($"Closed port {_serialPort.PortName}", Color.Yellow);
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error closing port: {ex.Message}", Color.Orange);
                }
                finally
                {
                    try { _serialPort.Dispose(); } catch { }
                    _serialPort = null;
                }
            }
        }

        /// <summary>
        /// Logs a message to the `richTextBoxLog` using the specified color.
        /// Thread-safe: can be called from background threads. Appends a newline automatically.
        /// </summary>
        /// <param name="message">Message to append.</param>
        /// <param name="color">Text color to use for this message.</param>
        public void Log(string message, Color color)
        {
            if (richTextBoxLog.InvokeRequired)
            {
                richTextBoxLog.Invoke(new Action(() => Log(message, color)));
                return;
            }

            // Preserve current selection and color
            int originalSelectionStart = richTextBoxLog.SelectionStart;
            int originalSelectionLength = richTextBoxLog.SelectionLength;
            Color originalColor = richTextBoxLog.SelectionColor;

            try
            {
                richTextBoxLog.SelectionStart = richTextBoxLog.TextLength;
                richTextBoxLog.SelectionLength = 0;
                richTextBoxLog.SelectionColor = color;
                richTextBoxLog.AppendText(message + Environment.NewLine);
                richTextBoxLog.SelectionColor = originalColor;
                richTextBoxLog.SelectionStart = richTextBoxLog.TextLength;
                richTextBoxLog.SelectionLength = 0;
                richTextBoxLog.ScrollToCaret();
            }
            finally
            {
                // restore previous selection (keeps caret behavior predictable)
                try
                {
                    richTextBoxLog.SelectionStart = originalSelectionStart;
                    richTextBoxLog.SelectionLength = originalSelectionLength;
                    richTextBoxLog.SelectionColor = originalColor;
                }
                catch { }
            }
        }


        private void buttonOpen_Click(object sender, EventArgs e)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                    _serialPort = OpenComPort(comboBoxComPorts.Text);
                var response = SendAndReceive("identify?\r", 1000);
            }
            catch (Exception ex)
            {
                Log($"Error sending to COM port: {ex.Message}", Color.Red);
            }
        }

        string path = "";
        private void buttonLoadFile_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog1 == null)
                {
                    Log("openFileDialog1 control is not available.", Color.Orange);
                    return;
                }

                var result = openFileDialog1.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    path = openFileDialog1.FileName;
                    Log($"Selected file: {path}", Color.Yellow);

                    try
                    {
                        var bytes = System.IO.File.ReadAllBytes(path);
                        Log($"Loaded file '{System.IO.Path.GetFileName(path)}' ({bytes.Length} bytes)", Color.Green);

                        // TODO: process the file contents as needed (e.g., parse firmware)
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to read file '{path}': {ex.Message}", Color.Red);
                    }
                }
                else
                {
                    Log("File selection cancelled by user.", Color.Orange);
                }
            }
            catch (Exception ex)
            {
                Log($"Error opening file dialog: {ex.Message}", Color.Red);
            }
        }

        private void buttonWriteFile_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    Log("No file selected to write. Use Load File first.", Color.Orange);
                    return;
                }

                if (!System.IO.File.Exists(path))
                {
                    Log($"File not found: {path}", Color.Red);
                    return;
                }

                // Ensure serial port is open; try to open the selected port if available
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    var selectedPort = comboBoxComPorts?.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(selectedPort))
                    {
                        try
                        {
                            _serialPort = OpenComPort(selectedPort);
                        }
                        catch (Exception ex)
                        {
                            Log($"Failed to open selected port {selectedPort}: {ex.Message}", Color.Red);
                            return;
                        }
                    }
                    else
                    {
                        Log("No serial port open and no port selected. Open or select a port first.", Color.Red);
                        return;
                    }
                }

                int lineNumber = 0;
                foreach (var rawLine in System.IO.File.ReadLines(path))
                {
                    lineNumber++;
                    var trimmed = rawLine.TrimEnd('\r', '\n');

                    // increment send ID for this outgoing line and insert it after the first ':' if present
                    sendIDCounter++;

                    var toSend = trimmed;
                    int colonIndex = toSend.IndexOf(':');
                    if (colonIndex >= 0)
                    {
                        // insert a space, the counter and a semicolon right after the first ':'
                        toSend = toSend.Insert(colonIndex + 1, " " + sendIDCounter + ";");
                    }

                    // ensure CR at end as requested
                    toSend = toSend + "\r";


                    string response;
                    try
                    {
                        response = SendAndReceive(toSend, 2000);
                    }
                    catch (Exception ex)
                    {
                        Log($"Error sending line {lineNumber}: {ex.Message}", Color.Red);
                        // continue sending remaining lines (do not abort entire transfer)
                        continue;
                    }

                    // small delay to avoid overrunning device
                    Thread.Sleep(200);
                }

                Log($"Finished sending file '{System.IO.Path.GetFileName(path)}'", Color.Yellow);
            }
            catch (Exception ex)
            {
                Log($"Error writing file to device: {ex.Message}", Color.Red);
            }
        }
    }
}
