using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using System.Threading;

namespace SWXSTANDALONE
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        SldWorks swApp;
        // custom properties list
        BindingList<CustomPropertyObject> CustomProperties = new BindingList<CustomPropertyObject>();
        // directory
        string Directory = String.Empty;
        // files
        string[] files;
        // timer
        System.Windows.Forms.Timer timer;
        private void Form1_Load(object sender, EventArgs e)
        {
            SetFormText();
            swProgressBar.Step = 1;
            CustomProperties.AllowNew = true;
            CustomProperties.AllowEdit = true;
            CustomProperties.AllowRemove = true;
            dataGridView1.DataSource = CustomProperties;
        }
 

        private void SetButtonsEnableState(bool cancel)
        {
            swCancel.Enabled = cancel;
            swBrowse.Enabled = !cancel;
            swAdd.Enabled = !cancel;
            swRemove.Enabled = !cancel;
            swProcess.Enabled = !cancel;

        }

        private void SetFormText()
        {
            if (string.IsNullOrEmpty(Directory))
            {
                Directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }

            double percent = 0;

            if (files == null || files.Length == 0)
            percent = 0;
            else 
            percent = swProgressBar.Value * 1.0 / files.Length * 1.0;

            var FormTitle = string.Format("{0}% {1} - {2}", (percent * 100).ToString("F2"), Directory, "CADHero");
            this.Text = FormTitle;
        }

        private void swBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowseDialog = new FolderBrowserDialog();

            if (folderBrowseDialog.ShowDialog() == DialogResult.OK)
            {
                Directory = folderBrowseDialog.SelectedPath;
            }

            // set the form title text

            SetFormText();
        }

        private void swRemove_Click(object sender, EventArgs e)
        {
            foreach(DataGridViewRow row in dataGridView1.Rows)
            {
                if (!row.IsNewRow)
                {
                    dataGridView1.Rows.Remove(row);
                }
            }
        }

        private void swAdd_Click(object sender, EventArgs e)
        {
            var name = Interaction.InputBox("Add name");
            if (string.IsNullOrWhiteSpace(name))
            {                
                return;
            }

            var value = Interaction.InputBox("Add value");
            var CustomProperty = new CustomPropertyObject(name, value,string.Empty);
        // check if custom property exists
        var ExistingCustomProperty = CustomProperties.ToList().Find((x)=> x.Name == name);
            if (ExistingCustomProperty != null)
                CustomProperties.Remove(ExistingCustomProperty);
            CustomProperties.Add(CustomProperty);
        }

        private void ResetStatusStrip()
        {
            swProgressBar.Value = 0;
            if (files != null)
            swProgressBar.Maximum = files.Length;
        }
        private void CompleteStatusStrip()
        {
            swProgressBar.Value = files.Length;
        }


        private Task<bool> ProcessModelAsync(CancellationToken Token, string filename)
        {

            return  Task<bool>.Run(() => {

                return Helper.processModel(swApp, filename, CustomProperties.ToList(), Token);
            });

        }

        // start time
        DateTime startTime;
        Task<bool> processModelAsyncTask;
        // declare cancellation source
        CancellationTokenSource CancelSource;
        private async void swProcess_Click(object sender, EventArgs e)
        {

            files = Helper.getCADFilesFromDirectory(Directory);
            if (files == null)
                return;
            if (files.Length == 0)
                return;

            try
            {

                startTime = DateTime.Now;
                timer = new System.Windows.Forms.Timer();
                timer.Interval = 1000;
                timer.Tick += Timer_Tick;
                timer.Start();
                swApp = await SolidWorksSingleton.getApplicationAsync();
                swApp.Visible = true;
                
                 CancelSource = new CancellationTokenSource();
                SetButtonsEnableState(true);
                // reset the status strip 
                ResetStatusStrip();

                foreach (string file in files)
                {
                    // invoke processModel
                    processModelAsyncTask = ProcessModelAsync(CancelSource.Token, file);
                    bool ret = await processModelAsyncTask;
                    swFileName.Text = file;
                    swProgressBar.Value += 1;

                    SetFormText();
                }

                timer.Stop();
                timer.Tick -= Timer_Tick;
                SetButtonsEnableState(false);
                CompleteStatusStrip();                
                SolidWorksSingleton.Dipose();
               


            }
            catch (Exception ex)
            {
            
                swFileName.Text = "An error has occured." + ex.Message;
                if (swApp != null)
                {                   
                    SolidWorksSingleton.Dipose();
                    SetButtonsEnableState(false);
                    
                }

                timer.Stop();
                timer.Tick -= Timer_Tick;

            }

        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSpan difference = DateTime.Now - startTime;
            var hours = difference.Hours.ToString("00");
            var mins = difference.Minutes.ToString("00");
            var secs = difference.Seconds.ToString("00");

            var time = string.Format("{0}:{1}:{2}", hours, mins, secs);
            swTime.Text = time;
        }

        private void swCancel_Click(object sender, EventArgs e)
        {
           
           if (processModelAsyncTask != null)
            {

                switch (processModelAsyncTask.Status)
                {
                    case TaskStatus.Created:
                        break;
                    case TaskStatus.WaitingForActivation:
                        break;
                    case TaskStatus.WaitingToRun:
                        break;
                    case TaskStatus.Running:
                        {
                            if (CancelSource != null)
                                CancelSource.Cancel();
                        }
                        break;
                    case TaskStatus.WaitingForChildrenToComplete:
                        break;
                    case TaskStatus.RanToCompletion:
                        break;
                    case TaskStatus.Canceled:
                        break;
                    case TaskStatus.Faulted:
                        break;
                    default:
                        break;
                }
            }    

        }
      
    }
}
