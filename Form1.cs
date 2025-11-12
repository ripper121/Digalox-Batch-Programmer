using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Digalox_Batch_Programmer
{
    public partial class Form1 : Form
    {
        private SerialPort? _serialPort;
        private readonly object _serialLock = new();
        private int sendIDCounter = 0;
        private bool writingInProgress = false;
        private bool autoArmed = false; // when true and Auto is checked, a single write will start on next tick

        // prevent overlapping scans
        private int _scanInProgress = 0;

        // cancellation for in-progress write operations
        private CancellationTokenSource? _writeCancellationSource;

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
            catch (Exception ex)
            {
                // log configuration errors
                try { Log($"ConfigureLogAppearance error: {ex.Message}", Color.Orange); } catch { }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private async Task ScanComPorts()
        {
            // Run the potentially slow port probing off the UI thread to avoid freezing the UI.
            int previousCount = 0;
            try
            {
                if (comboBoxComPorts != null)
                    previousCount = comboBoxComPorts.Items.Count;
            }
            catch { }

            var detected = new List<string>();

            var ports = SerialPort.GetPortNames();

            // Use Interlocked to set/clear the scan flag to prevent overlapping scans
            if (Interlocked.CompareExchange(ref _scanInProgress, 1, 0) == 0)
            {
                try
                {
                    await Task.Run(() =>
                    {
                        foreach (var port in ports)
                        {
                            SerialPort? testPort = null;
                            try
                            {
                                // Attempt to open the port for testing
                                testPort = OpenComPort(port);

                                var response = SendAndReceive(testPort, "identify?\r", 1000);
                                if (!string.IsNullOrEmpty(response) && response.IndexOf("TDE", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    detected.Add(port);
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
                                // close and dispose the test port if we created it
                                if (testPort != null)
                                {
                                    try
                                    {
                                        if (testPort.IsOpen)
                                        {
                                            try { testPort.Close(); } catch (Exception ex) { Log($"Error closing test port {port}: {ex.Message}", Color.Orange); }
                                            Log($"Closed test port {port}", Color.Yellow);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log($"Error closing test port {port}: {ex.Message}", Color.Orange);
                                    }
                                    finally
                                    {
                                        try { testPort.Dispose(); } catch { }
                                    }
                                }
                            }
                        }
                    });
                }
                finally
                {
                    // Ensure the scan flag is cleared after the scan completes
                    Interlocked.Exchange(ref _scanInProgress, 0);
                }
            }

            // Update UI controls on the UI thread
            try
            {
                if (comboBoxComPorts != null)
                {
                    this.Invoke((Action)(() =>
                    {
                        comboBoxComPorts.Items.Clear();
                        foreach (var p in detected)
                            comboBoxComPorts.Items.Add(p);

                        if (comboBoxComPorts.Items.Count > 0)
                        {
                            comboBoxComPorts.SelectedIndex = 0;
                        }
                        else
                        {
                            Log("No compatible COM ports detected.", Color.Orange);
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
                    }));
                }
                else
                {
                    // If combo box not present, still check for auto arming based on detected count
                    try
                    {
                        if (checkBoxAuto != null && checkBoxAuto.Checked && detected.Count > previousCount)
                        {
                            autoArmed = true;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log($"ScanComPorts UI update error: {ex.Message}", Color.Orange);
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
            SerialPort portCopy;
            lock (_serialLock)
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                    throw new InvalidOperationException("Serial port is not open.");

                portCopy = _serialPort;
            }

            return SendAndReceive(portCopy, data, readTimeoutMs);
        }

        /// <summary>
        /// Sends data using the provided SerialPort instance. Does not modify _serialPort.
        /// </summary>
        public string SendAndReceive(SerialPort port, string data, int readTimeoutMs = 1000)
        {
            if (port == null || !port.IsOpen)
                throw new InvalidOperationException("Serial port is not open.");

            var sb = new StringBuilder();
            var end = DateTime.UtcNow + TimeSpan.FromMilliseconds(readTimeoutMs);

            lock (_serialLock)
            {
                // clear buffers to avoid reading stale data
                try
                {
                    port.DiscardInBuffer();
                }
                catch (Exception ex)
                {
                    Log($"DiscardInBuffer error: {ex.Message}", Color.Orange);
                }
                try
                {
                    port.DiscardOutBuffer();
                }
                catch (Exception ex)
                {
                    Log($"DiscardOutBuffer error: {ex.Message}", Color.Orange);
                }

                // write
                try
                {
                    sendIDCounter++;
                    // NOTE: sendIDCounter is incremented by the caller so we do not increment here to avoid double-counting
                    port.Write(data);
                    Log($"TX-> {port.PortName}: '{data.Replace("\r", "\\r").Replace("\n", "\\n")}'", Color.Blue);
                }
                catch (Exception ex)
                {
                    Log($"Error writing to {port.PortName}: {ex.Message}", Color.Red);
                    throw new IOException($"Error writing to {port.PortName}", ex);
                }

                // poll for response until timeout
                while (DateTime.UtcNow < end)
                {
                    try
                    {
                        if (port.BytesToRead > 0)
                        {
                            sb.Append(port.ReadExisting());
                            // if the device uses newline-terminated responses, return early when newline seen
                            if (sb.ToString().IndexOfAny(new[] { '\r', '\n' }) >= 0)
                                break;
                        }
                    }
                    catch (TimeoutException)
                    {
                        // ignore and continue until overall timeout
                    }
                    catch (Exception ex)
                    {
                        Log($"Error reading from {port.PortName}: {ex.Message}", Color.Red);
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
                if (result.Contains("command_error"))
                {
                    Log($"RX<- {port.PortName}: '{result.Replace("\r", "\\r").Replace("\n", "\\n")}'", Color.Red);
                }
                else
                {
                    Log($"RX<- {port.PortName}: '{result.Replace("\r", "\\r").Replace("\n", "\\n")}'", Color.Green);
                }
            }

            return result;
        }

        /// <summary>
        /// Opens and returns an opened SerialPort for the specified port name.
        /// Throws an exception if the port is not available or cannot be opened.
        /// </summary>
        /// <param name="portName">The COM port name (e.g. "COM3").</param>
        /// <param name="baudRate">Baud rate (default19200).</param>
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
                WriteTimeout = 500,
                Encoding = Encoding.Latin1
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
            CloseComPort();
        }

        void CloseComPort()
        {
            lock (_serialLock)
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
        }

        /// <summary>
        /// Logs a message to the `richTextBoxLog` using the specified color.
        /// Thread-safe: can be called from background threads. Appends a newline automatically.
        /// </summary>
        /// <param name="message">Message to append.</param>
        /// <param name="color">Text color to use for this message.</param>
        public void Log(string message, Color color)
        {
            if (richTextBoxLog == null)
                return;

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

        private async void buttonWriteFile_Click(object sender, EventArgs e)
        {
            if (!writingInProgress && !string.IsNullOrEmpty(path))
            {
                sendIDCounter = 0;
                if (comboBoxComPorts.Items.Count > 0)
                {
                    await WriteFileAsync();
                }
                else
                {
                    Log("Nothing Connected!", Color.Orange);
                }
            }
            else
            {
                Log("No File selected!", Color.Orange);
            }
        }

        /// <summary>
        /// Request cancellation of the currently running write operation (if any).
        /// Can be wired to a Cancel button by the UI.
        /// </summary>
        public void CancelWrite()
        {
            try
            {
                _writeCancellationSource?.Cancel();
                Log("Write operation cancelled by user.", Color.Orange);
            }
            catch (Exception ex)
            {
                Log($"Error cancelling write: {ex.Message}", Color.Orange);
            }
        }

        private static byte calculateDpm72Checksum(string inputString, int startPos, bool output)
        {
            byte calculatedChecksum = 0;

            if (startPos >= 0)
            {
                // Versuche Windows-1252; falls nicht verfügbar, registriere Provider oder falle auf Latin1 zurück.
                Encoding encoding;
                try
                {
                    // Registrierung ist idempotent und sicher mehrfach aufzurufen.
                    Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    encoding = Encoding.GetEncoding(1252);
                }
                catch
                {
                    // Wenn Codepages nicht verfügbar sind, verwende Latin1 als robusten Fallback.
                    encoding = Encoding.Latin1;
                }

                byte[] bytes = encoding.GetBytes(inputString);

                for (int i = startPos; i < bytes.Length; i++)
                {
                    byte value = bytes[i];

                    // Apply XOR based on 'output' flag
                    value ^= (byte)(output ? 0x55 : 0xAA);

                    // Add to checksum (compound assignment wraps automatically)
                    calculatedChecksum += value;
                }
            }

            return calculatedChecksum;
        }

        private static string insertCrc(string line, bool output)
        {
            // Find colon (start of CRC field)
            int colonIndex = line.IndexOf(':');
            if (colonIndex < 0)
                throw new ArgumentException("Line must contain a ':' character");

            // Find first semicolon (end of CRC field)
            int semicolonIndex = line.IndexOf(';', colonIndex + 1);
            if (semicolonIndex < 0)
                throw new ArgumentException("Line must contain a ';' after CRC field");

            // Extract the substring after the colon (includes current CRC + rest of data)
            string crcPart = line.Substring(colonIndex + 1);

            // Calculate start position for checksum (after the first semicolon in crcPart)
            int startPos = crcPart.IndexOf(';') + 1;

            // Calculate correct CRC using validated algorithm
            byte crc = calculateDpm72Checksum(crcPart, startPos, output);

            // Rebuild the line:
            // Everything up to colon + new CRC + everything after semicolon
            string updatedLine = line.Substring(0, colonIndex + 1) + crc + line.Substring(semicolonIndex);

            return updatedLine;
        }


        private async Task WriteFileAsync()
        {
            writingInProgress = true;
            _writeCancellationSource?.Cancel();
            _writeCancellationSource?.Dispose();
            _writeCancellationSource = new CancellationTokenSource();
            var token = _writeCancellationSource.Token;

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
                        CloseComPort();
                    }
                    catch (Exception ex)
                    {
                        Log($"Error closing previous port: {ex.Message}", Color.Orange);
                    }

                    try
                    {
                        lock (_serialLock)
                        {
                            _serialPort = OpenComPort(selectedPort);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to open selected port {selectedPort}: {ex.Message}", Color.Red);
                        throw new InvalidOperationException($"Failed to open selected port {selectedPort}", ex);
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
                await Task.Run(async () =>
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        int lineNumber = i + 1;
                        var rawLine = lines[i];

                        if (checkBoxCRC.Checked)
                        {
                            rawLine = insertCrc(rawLine, false);
                        }

                        var trimmed = rawLine.TrimEnd('\r', '\n');

                        var toSend = trimmed;
                        int colonIndex = toSend.IndexOf(':');
                        if (colonIndex >= 0)
                        {
                            // insert the counter and a semicolon right after the first ':'
                            toSend = toSend.Insert(colonIndex + 1, sendIDCounter + ";");
                        }

                        // ensure CR at end as requested
                        toSend = toSend + "\r";



                        try
                        {
                            token.ThrowIfCancellationRequested();

                            SerialPort? portCopy;
                            lock (_serialLock)
                            {
                                portCopy = _serialPort;
                            }

                            if (portCopy == null)
                                throw new InvalidOperationException("Serial port was closed during write operation.");

                            var response = SendAndReceive(portCopy, toSend, 2000);
                            if (response.Contains("pin_required:") && response.Contains(";100;1"))
                            {
                                // Ask the user for PIN on the UI thread using the PromptDialog helper
                                string? pin = null;
                                try
                                {
                                    this.Invoke((Action)(() =>
                                    {
                                        pin = PromptDialog.Show(this, "PIN Required", "Device requested a PIN:", "");
                                    }));
                                }
                                catch (Exception ex)
                                {
                                    Log($"Failed to show PIN dialog: {ex.Message}", Color.Orange);
                                }

                                if (string.IsNullOrEmpty(pin))
                                {
                                    // User cancelled or entered empty PIN -> abort write
                                    Log("PIN entry cancelled or empty - aborting write.", Color.Orange);
                                    throw new OperationCanceledException("PIN entry cancelled by user");
                                }

                                // Follow same send ID insertion pattern as other lines
                                //pin_required: 65; 109; 2407
                                //pin_required: 65; 101; 0

                                var pinCmd = $"pin_required:{sendIDCounter};109;{pin}\r";

                                try
                                {
                                    var pinResponse = SendAndReceive(portCopy, pinCmd, 2000);
                                    // Optionally inspect pinResponse for success/failure pin_required:1;101;0\r
                                    if (pinResponse.Contains("pin_required:") && pinResponse.Contains(";101;"))
                                    {
                                        Log("Pin Accepted.", Color.Orange);
                                    }
                                    else
                                    {
                                        Log("Pin Error.", Color.Orange);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log($"Error sending PIN: {ex.Message}", Color.Red);
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            Log($"Write cancelled before sending line {lineNumber}.", Color.Orange);
                            break;
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
                        await Task.Delay(20, token).ContinueWith(_ => { });
                    }
                }, token);

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
            catch (OperationCanceledException)
            {
                Log("Write operation cancelled.", Color.Orange);
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
                try { _writeCancellationSource?.Dispose(); } catch { }
                _writeCancellationSource = null;
                CloseComPort();
            }
        }

        private int lastPortCount = 0;
        private async void timerSetButtons_Tick(object sender, EventArgs e)
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
                comboBoxComPorts.Enabled = !checkBoxAuto.Checked;
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
                        if (ports.Length != lastPortCount)
                        {
                            await ScanComPorts();
                        }

                        // Start a single automatic write only if we've been armed by a port change
                        if (autoArmed && comboBoxComPorts.Items.Count > 0)
                        {
                            // disarm before starting to prevent reentry
                            autoArmed = false;
                            await WriteFileAsync();
                        }
                    }
                }
                else
                {
                    if (ports.Length != lastPortCount)
                    {
                        await ScanComPorts();
                    }
                }
                lastPortCount = ports.Length;
            }
        }
    }
}
