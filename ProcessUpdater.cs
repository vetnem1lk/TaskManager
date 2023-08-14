using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace Processes
{
    internal class ProcessUpdater
    {
        private MainForm mainForm;
        private Thread updateThread;
        private DataGridView dataGridView;
        private Dictionary<int, ProcessInfo> previousProcesses = new Dictionary<int, ProcessInfo>();
        private bool isRunning;
        public int currentRowIndex;
        public int verticalScrollValue;

        public ProcessUpdater(DataGridView dgv, MainForm form)
        {
            dataGridView = dgv;
            isRunning = false;
            mainForm = form;
        }

        public void StartUpdating()
        {
            if (!isRunning)
            {
                isRunning = true;
                updateThread = new Thread(UpdateProcessInfo);
                updateThread.Start();
                mainForm.LogUserAction("Запуск потока процессов");
            }
        }

        public void StopUpdating()
        {
            if (isRunning)
            {
                isRunning = false;
                updateThread.Join();
                mainForm.LogUserAction("Остановка потока процессов");
            }
        }

        private void UpdateProcessInfo()
        {
            try
            {
                while (isRunning)
                {
                    List<ProcessInfo> processInfoList = GetProcessInfo();
                    findOldNewProcesses(processInfoList);
                    setGridScroll();

                    dataGridView.Invoke(new Action(() =>
                    {
                        dataGridView.DataSource = processInfoList;
                    }));

                    getGridScroll();
                    mainForm.LogUserAction("Обновление данных процессов");
                    Thread.Sleep(1000);
                }
            }
            catch (ThreadInterruptedException)
            {
                // Обработка исключения прерывания потока
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<ProcessInfo> GetProcessInfo()
        {
            List<ProcessInfo> processInfoList = new List<ProcessInfo>();

            Process[] processes = Process.GetProcesses();
            var executableProcesses = Array.FindAll(processes, p => !string.IsNullOrEmpty(p.MainWindowTitle));
            foreach (Process process in executableProcesses)
            {
                double memoryUsageMB = process.WorkingSet64 / (1024f * 1024f); // Convert bytes to MB
                processInfoList.Add(new ProcessInfo
                {
                    Name = process.ProcessName,
                    Id = process.Id,
                    MemoryUsage = Math.Round(memoryUsageMB, 2) + " MB"
                });
            }

            return processInfoList;
        }

        private List<ProcessInfo> findOldNewProcesses(List<ProcessInfo> processInfoList)
        {
            // Создать словарь с текущими процессами для сравнения
            Dictionary<int, ProcessInfo> currentProcesses = new Dictionary<int, ProcessInfo>();

            foreach (ProcessInfo processInfo in processInfoList)
            {
                currentProcesses[processInfo.Id] = processInfo;
            }
            // Сравнить с предыдущими процессами и записать в лог
            foreach (int processId in previousProcesses.Keys)
            {
                if (!currentProcesses.ContainsKey(processId))
                {

                    mainForm.LogUserAction($"Процесс завершен: {previousProcesses[processId].Name} (ID: {processId})");
                }
            }
            foreach (int processId in currentProcesses.Keys)
            {
                if (!previousProcesses.ContainsKey(processId))
                {
                    mainForm.LogUserAction($"Новый процесс: {currentProcesses[processId].Name} (ID: {processId})");
                }
            }
            previousProcesses = currentProcesses;
            return processInfoList;
        }
        private void setGridScroll()
        {
            // Сохранить текущее положение
            dataGridView.Invoke(new Action(() =>
            {
                currentRowIndex = dataGridView.FirstDisplayedScrollingRowIndex;
                verticalScrollValue = dataGridView.VerticalScrollingOffset;
            }));
        }

        private void getGridScroll()
        {
            // Восстановить положение
            dataGridView.Invoke(new Action(() =>
            {
                if (currentRowIndex >= 0 && currentRowIndex < dataGridView.RowCount)
                {
                    dataGridView.FirstDisplayedScrollingRowIndex = currentRowIndex;
                    //dataGridView.VerticalScrollingOffset = verticalScrollValue;
                }
            }));
        }
    }
}
