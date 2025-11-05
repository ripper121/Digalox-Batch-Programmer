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
        private bool writingInProgress = false;
        private bool autoArmed = false; // when true and Auto is checked, a single write will start on next tick

        public Form1()
        {
            InitializeComponent();
            // Configure the log appearance to use a high-contrast, easy-to-read background and font
            ConfigureLogAppearance();
        }

        /// <summary>
        /// Configure richTextBoxLog appearance for good readability (background, font, default colors).
        /// Called from the constructor after InitializeComponent.
        /// </summary>
        private void ConfigureLogAppearance()
        {
            try
            {
                if (richTextBoxLog != null)
                {
                    // Dark background with light foreground gives good contrast for colored messages
                    richTextBoxLog.BackColor = Color.FromArgb(18, 18, 18); // very dark gray
                    richTextBoxLog.ForeColor = Color.FromArgb(230, 230, 230); // near-white for default text

                    // Use a monospace font for better alignment and readability of log data
                    try { richTextBoxLog.Font = new Font("Consolas", 10F, FontStyle.Regular); } catch { /* fallback ignored */ }

                    // Ensure the control scrolls to caret and selection behavior remains predictable
                    richTextBoxLog.HideSelection = false;
                }
            }
            catch { }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void scanComPorts()
        {
            // Populate comboBoxComPorts with ports that respond with "TDE" to an identify? query
            int previousCount = 0;
            try
            {
                if (comboBoxComPorts != null)
                    previousCount = comboBoxComPorts.Items.Count;
            }
            catch { }

            comboBoxComPorts.Items.Clear();

            var ports = SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                SerialPort? testPort = null;
                var previous = _serialPort;
                try
                {
                    // Attempt to open the port for testing
                    testPort = openComPort(port);

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

            // If Auto is enabled and we discovered more ports than before, arm a single automatic write
            try
            {
                int newCount = comboBoxComPorts.Items.Count;
                if (checkBoxAuto != null && checkBoxAuto.Checked && newCount > previousCount)
                {
                    autoArmed = true;
                }
            }
            catch { }

            // Optionally select the first detected port
            if (comboBoxComPorts.Items.Count > 0)
            {
                comboBoxComPorts.SelectedIndex = 0;
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
                    Log($"TX-> {_serialPort.PortName}: '{data.Replace("\r", "\\r").Replace("\n", "\\n")}'", Color.Blue);
                }
                catch (Exception ex)
                {
                    Log($"Error writing to {_serialPort.PortName}: {ex.Message}", Color.Red);
                    throw new Exception($"Error writing to {_serialPort.PortName}: {ex.Message}");
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

                    Thread.Sleep(20);
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
        public SerialPort openComPort(string portName, int baudRate = 19200, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
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
            bool controlHasFocus = richTextBoxLog.Focused;

            try
            {
                // Move caret to end and append colored text
                richTextBoxLog.SelectionStart = richTextBoxLog.TextLength;
                richTextBoxLog.SelectionLength = 0;
                richTextBoxLog.SelectionColor = color;
                richTextBoxLog.AppendText(message + Environment.NewLine);

                // Ensure caret is at end and scroll to caret so newest log is visible
                richTextBoxLog.SelectionStart = richTextBoxLog.TextLength;
                richTextBoxLog.SelectionLength = 0;
                richTextBoxLog.ScrollToCaret();
            }
            finally
            {
                // restore previous selection (only if the user had focus), otherwise keep caret at end so the box stays autoscrolled
                try
                {
                    richTextBoxLog.SelectionColor = originalColor;
                    if (controlHasFocus)
                    {
                        // clamp original selection start to current text length
                        richTextBoxLog.SelectionStart = Math.Min(originalSelectionStart, richTextBoxLog.TextLength);
                        richTextBoxLog.SelectionLength = Math.Min(originalSelectionLength, richTextBoxLog.TextLength - richTextBoxLog.SelectionStart);
                    }
                    else
                    {
                        // keep caret at end for autoscroll
                        richTextBoxLog.SelectionStart = richTextBoxLog.TextLength;
                        richTextBoxLog.SelectionLength = 0;
                    }
                }
                catch { }
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
            if (!writingInProgress && !string.IsNullOrEmpty(path))
            {
                if (comboBoxComPorts.Items.Count > 0)
                {
                    writeFile();
                }
            }
        }

        private async void writeFile()
        {
            writingInProgress = true;
            try
            {                
                if (string.IsNullOrEmpty(path))
                {
                    Log("No file selected to write. Use Load File first.", Color.Orange);
                    throw new InvalidOperationException("No file selected to write. Use Load File first.");
                }

                if (!System.IO.File.Exists(path))
                {
                    Log($"File not found: {path}", Color.Red);
                    throw new InvalidOperationException($"File not found: {path}");
                }

                // Try to open the selected port if available
                var selectedPort = comboBoxComPorts?.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedPort))
                {
                    try
                    {
                        closeComPort();
                    }
                    catch (Exception ex)
                    {
                    }

                    try
                    {
                        _serialPort = openComPort(selectedPort);
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to open selected port {selectedPort}: {ex.Message}", Color.Red);
                        throw new Exception($"Failed to open selected port {selectedPort}: {ex.Message}");
                    }
                }
                else
                {
                    Log("No serial port open and no port selected. Open or select a port first.", Color.Red);
                    throw new InvalidOperationException("No serial port open and no port selected. Open or select a port first.");
                }

                // Prepare progress information
                string[] lines = System.IO.File.ReadAllLines(path);
                int totalLines = lines.Length;
                if (totalLines == 0)
                    totalLines = 1; // avoid zero maximum

                // Initialize progress bar on UI thread
                try
                {
                    if (progressBarWrite != null)
                    {
                        this.Invoke((Action)(() =>
                        {
                            progressBarWrite.Minimum = 0;
                            progressBarWrite.Maximum = totalLines;
                            progressBarWrite.Value = 0;
                        }));
                    }
                }
                catch { }

                // Run the send loop on a background thread so the UI thread remains responsive
                await System.Threading.Tasks.Task.Run(() =>
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        int lineNumber = i + 1;
                        var rawLine = lines[i];
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


                        try
                        {
                            var response = SendAndReceive(toSend, 2000);
                        }
                        catch (Exception ex)
                        {
                            Log($"Error sending line {lineNumber}: {ex.Message}", Color.Red);
                            // continue sending remaining lines (do not abort entire transfer)
                            // still update progress for the failed line
                        }

                        // Update progress bar on UI thread
                        try
                        {
                            if (progressBarWrite != null)
                            {
                                this.Invoke((Action)(() =>
                                {
                                    // clamp value to maximum
                                    int val = Math.Min(lineNumber, progressBarWrite.Maximum);
                                    progressBarWrite.Value = val;
                                }));
                            }
                        }
                        catch { }

                        // small delay to avoid overrunning device
                        Thread.Sleep(20);
                    }
                });

                // Ensure progress bar shows completion
                try
                {
                    if (progressBarWrite != null)
                    {
                        this.Invoke((Action)(() => progressBarWrite.Value = progressBarWrite.Maximum));
                    }
                }
                catch { }

                Log($"Finished sending file '{System.IO.Path.GetFileName(path)}'", Color.Yellow);
            }
            catch (Exception ex)
            {
                Log($"Error writing file to device: {ex.Message}", Color.Red);
            }
            finally
            {
                writingInProgress = false;
                // after finishing a write, do not immediately re-arm auto mode - wait for a new port connection
                autoArmed = false;
                closeComPort();
            }
        }

        private void timerSetButtons_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(path) && !writingInProgress)
            {
                buttonWriteFile.Enabled = true;
            }
            else
            {
                buttonWriteFile.Enabled = false;
            }

            if (!string.IsNullOrEmpty(path))
            {
                checkBoxAuto.Enabled = true;
            }
            else
            {
                checkBoxAuto.Enabled = false;
            }

            if (comboBoxComPorts.Items.Count > 0)
            {
                comboBoxComPorts.Enabled = true;
            }
            else
            {
                comboBoxComPorts.Enabled = false;
            }


            if (!writingInProgress)
            {
                var ports = SerialPort.GetPortNames();
                if (checkBoxAuto != null && checkBoxAuto.Checked)
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        // If system ports changed, update the detected devices list. This will arm auto if new ports are found.
                        if (ports.Length != comboBoxComPorts.Items.Count)
                        {
                            scanComPorts();
                        }

                        // Start a single automatic write only if we've been armed by a port change
                        if (autoArmed && comboBoxComPorts.Items.Count > 0)
                        {
                            // disarm before starting to prevent reentry
                            autoArmed = false;
                            writeFile();
                        }
                    }
                }
                else
                {
                    if (ports.Length != comboBoxComPorts.Items.Count)
                    {
                        scanComPorts();
                    }
                }
            }
        }
    }
}
