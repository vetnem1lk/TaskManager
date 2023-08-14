using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace Processes
{
    public partial class MainForm : Form
    {
        private ProcessUpdater processUpdater;
        private StreamWriter logStreamWriter;

        public MainForm()
        {
            InitializeComponent();
            processUpdater = new ProcessUpdater(processGridView, this);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            string logFilePath = Path.Combine(Application.StartupPath, "Logs", "user_actions.log");
            logStreamWriter = new StreamWriter(logFilePath, true); // Append to the existing log file
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            try
            {
                processUpdater.StartUpdating();
            }
            catch (Exception ex)
            {
                HandleError("Ошибка при запуске обновления процессов: " + ex.Message);
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            try
            {
                processUpdater.StopUpdating();
            }
            catch (Exception ex)
            {
                HandleError("Ошибка при остановке обновления процессов: " + ex.Message);
            }
        }

        private void ShowProcessInfoDialog(int processId)
        {
            try
            {
                Process selectedProcess = Process.GetProcessById(processId);
                string processInfo = $"Process Name: {selectedProcess.ProcessName}\n" +
                                     $"Process ID: {selectedProcess.Id}\n" +
                                     $"Memory Usage (MB): {selectedProcess.WorkingSet64 / (1024.0 * 1024.0):F2}\n" +
                                     $"Start Time: {selectedProcess.StartTime}\n" +
                                     $"Total Processor Time: {selectedProcess.TotalProcessorTime}";

                MessageBox.Show(processInfo, "Process Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                HandleError("Ошибка при отображении информации о процессе: " + ex.Message);
            }
        }

        private void processGridView_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                processUpdater.currentRowIndex = processGridView.FirstDisplayedScrollingRowIndex;
                processUpdater.verticalScrollValue = processGridView.VerticalScrollingOffset;
            }
        }

        private void processGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0)
                {
                    DataGridViewRow selectedRow = processGridView.Rows[e.RowIndex];
                    int processId = (int)selectedRow.Cells["Id"].Value;
                    LogUserAction($"Нажатие на процесс с ID {processId}");
                    ShowProcessInfoDialog(processId);
                }
            }
            catch (Exception ex)
            {
                HandleError("Ошибка при отображении информации о процессе: " + ex.Message);
            }
        }

        private void HandleError(string errorMessage)
        {
            MessageBox.Show(errorMessage, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                processUpdater.StopUpdating();
                logStreamWriter.Close();
            }
            catch (Exception ex)
            {
                HandleError("Ошибка при остановке обновления процессов: " + ex.Message);
            }
        }

        public void LogUserAction(string action)
        {
            try
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {action}";
                logStreamWriter.WriteLine(logMessage);
                logStreamWriter.Flush();
            }
            catch (Exception ex)
            {
                HandleError("Ошибка при записи действия пользователя в лог: " + ex.Message);
            }
        }

        private void openLogFileLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string filePath = Path.Combine(Application.StartupPath, "Logs", "user_actions.log");
            try
            {
                Process.Start(filePath);
                LogUserAction($"Открытие файла логов");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        }
    }
}
