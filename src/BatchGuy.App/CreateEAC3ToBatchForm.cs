﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BatchGuy.App.Eac3to.Models;
using BatchGuy.App.Eac3to.Services;
using BatchGuy.App.Parser.Models;
using BatchGuy.App.Parser.Services;
using BatchGuy.App.Eac3to.Interfaces;
using BatchGuy.App.Parser.Interfaces;
using BatchGuy.App.Enums;
using BatchGuy.App.Helpers;
using BatchGuy.App.Eac3To.Interfaces;
using BatchGuy.App.Extensions;
using System.Linq.Dynamic;
using BatchGuy.App.Shared.Models;
using BatchGuy.App.Shared.Interfaces;
using BatchGuy.App.Shared.Services;
using BatchGuy.App.ThirdParty.FolderSelectDialog;
using BatchGuy.App.Settings.Models;
using System.IO;

namespace BatchGuy.App
{
    public partial class CreateEAC3ToBatchForm : Form
    {
        private BluRayDiscInfo _currentBluRayDiscInfo;
        private CommandLineProcessStartInfo _commandLineProcessStartInfo;
        private BindingList<BluRayDiscInfo> _bindingListBluRayDiscInfo = new BindingList<BluRayDiscInfo>();
        private BindingList<BluRaySummaryInfo> _bindingListBluRaySummaryInfo;
        private int _currentBluRayDiscGridRowIndex;
        private SortConfiguration _bluRaySummaryGridSortConfiguration = new SortConfiguration();
        private SortConfiguration _bluRayDiscGridSortConfiguration = new SortConfiguration();
        private string _eac3ToPath = string.Empty;
        private EAC3ToConfiguration _eac3toConfiguration;

        public CreateEAC3ToBatchForm()
        {
            InitializeComponent();
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            setDirectoryUserControl.ComboBoxCaptionText = "eac3to";
            setDirectoryUserControl.LabelDirectoryCaptionText = @"eac3to Output Directory (example: e01, e02): {0}\e##";
            this.SetToolTips();
        }

        private void CreateEAC3ToBatchForm_Load(object sender, EventArgs e)
        {
            if (!this.IsEac3ToPathSetInSettings())
            {
                MessageBox.Show("Please go to the settings screen and set the eac3to.exe path", "eac3to path not set", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                gbScreen.SetEnabled(false);
            }
            else
            {
                Setting setting = Program.ApplicationSettingsService.GetSettingByName("eac3to");
                _eac3ToPath = setting.Path;
                this.SetEac3ToConfiguration();
            }
        }

        private void SetToolTips()
        {
            ttBatchFileOutputDirectory.SetToolTip(txtBatFilePath, "Directory where eac3to batch file will be saved");
            ttDirectoryUserControl.SetToolTip(setDirectoryUserControl, "eac3to stream extract directory");
        }

        private bool IsEac3ToPathSetInSettings()
        {
            Setting setting = Program.ApplicationSettingsService.GetSettingByName("eac3to");
            if (setting == null)
                return false;
            else
                return true;
        }

        private void btnWriteToBatFile_Click(object sender, EventArgs e)
        {
            this.SetEac3ToConfiguration();
            if (this.IsAtLeastOneDiscLoaded() && this.IsScreenValid())
            {
                this.WriteToBatchFile();                
            }
        }

        private void WriteToBatchFile()
        {
            gbScreen.SetEnabled(false);
            List<BluRayDiscInfo> discs = this.GetBluRayDiscInfoList();
            IBatchFileWriteService batchFileWriteService = new BatchFileWriteService(_eac3toConfiguration,discs);
            bgwEac3toWriteBatchFile.RunWorkerAsync(batchFileWriteService);
        }

        private void BindDgvBluRayDiscInfoGrid()
        {
            bsBluRayDiscInfo.DataSource = _bindingListBluRayDiscInfo;
            bsBluRayDiscInfo.ResetBindings(false);
            _bindingListBluRayDiscInfo.AllowEdit = true;
        }

        private void dgvBluRayDiscInfo_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
            {
                this.SortBluRayDiscGrid(e.ColumnIndex);
            }
            else
            {
                this.HandleDgvBluRayDiscInfoCellClick(e);
                if (_currentBluRayDiscInfo.BluRaySummaryInfoList == null)
                {
                    gbScreen.SetEnabled(false);
                    this.HandleLoadBluRay();
                }
                else
                {
                    _bindingListBluRaySummaryInfo = new BindingList<BluRaySummaryInfo>();
                    foreach (BluRaySummaryInfo info in _currentBluRayDiscInfo.BluRaySummaryInfoList)
                    {
                        _bindingListBluRaySummaryInfo.Add(info);
                    }
                    this.UpdateUIForBluRaySummary();
                }
            }
        }

        private void HandleDgvBluRayDiscInfoCellClick(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
                return;
            var id = dgvBluRayDiscInfo.Rows[e.RowIndex].Cells[1].Value;
            _currentBluRayDiscInfo = _bindingListBluRayDiscInfo.SingleOrDefault(d => d.Id == id.ToString().StringToInt());
            _currentBluRayDiscGridRowIndex = e.RowIndex;
        }

        private void HandleLoadBluRay()
        {
            _bindingListBluRaySummaryInfo = new BindingList<BluRaySummaryInfo>();

            //Blu ray streams
            _commandLineProcessStartInfo = new CommandLineProcessStartInfo()
            {
                FileName = _eac3ToPath,
                Arguments = string.Format("\"{0}\"", _currentBluRayDiscInfo.BluRayPath)
            };

            ICommandLineProcessService commandLineProcessService = new CommandLineProcessService(_commandLineProcessStartInfo);
            if (commandLineProcessService.Errors.Count() == 0)
            {
                bgwEac3toLoadSummary.RunWorkerAsync(commandLineProcessService);
            }
            else
            {
                MessageBox.Show(commandLineProcessService.Errors.GetErrorMessage(), "Errors Occurred.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateUIForBluRaySummary()
        {
            this.BindDgvBluRaySummaryGrid();
            gbDiscSummary.Text = string.Format("Disc Summary: {0}", _currentBluRayDiscInfo.DiscName);

            if (_currentBluRayDiscGridRowIndex != -1)
                dgvBluRayDiscInfo.Rows[_currentBluRayDiscGridRowIndex].Selected = true;
            gbScreen.SetEnabled(true);
        }


        private void BindDgvBluRaySummaryGrid()
        {
            bsBluRaySummaryInfo.DataSource = _bindingListBluRaySummaryInfo;
            bsBluRaySummaryInfo.ResetBindings(false);
            _bindingListBluRaySummaryInfo.AllowEdit = true;
        }

        private void dgvBluRaySummary_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            this.HandleDgvBluRaySummaryCellDoubleClick(e);
        }

        private void HandleDgvBluRaySummaryCellDoubleClick(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
                return;
            var id = dgvBluRaySummary.Rows[e.RowIndex].Cells[1].Value;
            BluRaySummaryInfo summaryInfo = _currentBluRayDiscInfo.BluRaySummaryInfoList.SingleOrDefault(s => s.Id == id.ToString());

            BluRayTitleInfoForm form = new BluRayTitleInfoForm();
            form.SetBluRayTitleInfo(_eac3toConfiguration,_currentBluRayDiscInfo.BluRayPath,summaryInfo);
            form.ShowDialog();
            this.BindDgvBluRaySummaryGrid();
        }

        private bool IsScreenValid()
        {
            if (_eac3toConfiguration.BatchFilePath == null || _eac3toConfiguration.BatchFilePath == string.Empty)
            {
                MessageBox.Show("Please enter the path the batch file should be created!", "Error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;    
            }
            if (_eac3toConfiguration.EAC3ToPath == null || _eac3toConfiguration.EAC3ToPath == string.Empty)
            {
                MessageBox.Show("Please enter the eac3to.exe path with the exe in the path!", "Error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (_eac3toConfiguration.EAC3ToOutputPath == null || _eac3toConfiguration.EAC3ToOutputPath == string.Empty)
            {
                MessageBox.Show("Please choose an eac3to output path!", "Error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;                
            }
            return true;
        }

        private bool IsAtLeastOneDiscLoaded()
        {
            if (_bindingListBluRayDiscInfo == null || _bindingListBluRayDiscInfo.Count() == 0)
            {
                MessageBox.Show("Please load at least 1 blu-ray disck!", "Error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private void dgvBluRaySummary_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
            {
                this.SortBluRaySummaryGrid(e.ColumnIndex);
            }
            else
            {
                dgvBluRaySummary.Rows[e.RowIndex].Selected = true;
            }
        }

        private void btnOpenBatchFilePathDialog_Click(object sender, EventArgs e)
        {
            this.HandleBtnOpenBatchFilePathDialogClick();
        }

        private void HandleBtnOpenBatchFilePathDialogClick()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Batch File|*.bat";
            sfd.Title = "Save eac3to Batch File";
            sfd.InitialDirectory = @"C:\temp";
#if DEBUG
            sfd.InitialDirectory = @"C:\temp\My Encodes\Blu-ray";
#endif
            sfd.ShowDialog();

            if (!string.IsNullOrEmpty(sfd.FileName))
            {
                using (FileStream fs = File.Create(sfd.FileName))
                {
                }
                txtBatFilePath.Text = sfd.FileName;
            }
        }

        private void bgwEac3toLoadSummary_DoWork(object sender, DoWorkEventArgs e)
        {
            //Get line items
            ICommandLineProcessService commandLineProcessService = e.Argument as CommandLineProcessService;
            List<ProcessOutputLineItem> processOutputLineItems = commandLineProcessService.GetProcessOutputLineItems();

            e.Result = processOutputLineItems;
        }

        private void bgwEac3toLoadSummary_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            List<ProcessOutputLineItem> processOutputLineItems = e.Result as List<ProcessOutputLineItem>;
            ILineItemIdentifierService lineItemService = new BluRaySummaryLineItemIdentifierService();
            IBluRaySummaryParserService parserService = new BluRaySummaryParserService(lineItemService, processOutputLineItems);
            List<BluRaySummaryInfo> bluRaySummaries = parserService.GetSummaryList();

            if (parserService.Errors.Count() == 0)
            {
                _currentBluRayDiscInfo.BluRaySummaryInfoList = bluRaySummaries;
                foreach (BluRaySummaryInfo info in _currentBluRayDiscInfo.BluRaySummaryInfoList)
                {
                    _bindingListBluRaySummaryInfo.Add(info);
                }
                this.UpdateUIForBluRaySummary();         
            }
            else
            {
                MessageBox.Show(parserService.Errors.GetErrorMessage(), "Error Occurred.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                BluRayDiscInfo disc = _bindingListBluRayDiscInfo.SingleOrDefault(d => d.Id == _currentBluRayDiscInfo.Id);
                _bindingListBluRayDiscInfo.Remove(disc);
                this.BindDgvBluRayDiscInfoGrid();
                gbScreen.SetEnabled(true);
                dgvBluRaySummary.Rows.Clear();
            }
      
        }

        private void bgwEac3toWriteBatchFile_DoWork(object sender, DoWorkEventArgs e)
        {
            IBatchFileWriteService batchFileWriteService = e.Argument as BatchFileWriteService;
            batchFileWriteService.Write();
            e.Result = batchFileWriteService;
        }

        private void bgwEac3toWriteBatchFile_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IBatchFileWriteService batchFileWriteService = e.Result as BatchFileWriteService;
            if (batchFileWriteService.Errors.Count() == 0)
            {
                MessageBox.Show("Batch File created!", "Process Complete", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                this.Close();
            }
            else
            {
                MessageBox.Show(string.Format("Error: {0}", batchFileWriteService.Errors[0].Description), "Error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            gbScreen.SetEnabled(true);
        }

        private void SortBluRayDiscGrid(int sortColumnNumber)
        {
            if (_bindingListBluRayDiscInfo.Count() == 0)
                return;

            string sortColumnName = dgvBluRayDiscInfo.Columns[sortColumnNumber].DataPropertyName;
            _bluRayDiscGridSortConfiguration.SortByColumnName = sortColumnName;
            List<BluRayDiscInfo> discs = this.GetBluRayDiscInfoList();
            ISortService<BluRayDiscInfo> sortService = new SortService<BluRayDiscInfo>(_bluRayDiscGridSortConfiguration, discs);

            IBindingListSortService<BluRayDiscInfo> bindingListSortService = new BindingListSortService<BluRayDiscInfo>(discs, dgvBluRayDiscInfo, _bluRayDiscGridSortConfiguration, sortService);
            _bindingListBluRayDiscInfo = bindingListSortService.Sort();

            this.BindDgvBluRayDiscInfoGrid();

        }

        private void SortBluRaySummaryGrid(int sortColumnNumber)
        {
            if (_currentBluRayDiscInfo == null)
                return;

            string sortColumnName = dgvBluRaySummary.Columns[sortColumnNumber].DataPropertyName;
            _bluRaySummaryGridSortConfiguration.SortByColumnName = sortColumnName;
            ISortService<BluRaySummaryInfo> sortService = new SortService<BluRaySummaryInfo>(_bluRaySummaryGridSortConfiguration, _currentBluRayDiscInfo.BluRaySummaryInfoList);

            IBindingListSortService<BluRaySummaryInfo> bindingListSortService = new BindingListSortService<BluRaySummaryInfo>(_currentBluRayDiscInfo.BluRaySummaryInfoList,
                dgvBluRaySummary, _bluRaySummaryGridSortConfiguration, sortService);

            _bindingListBluRaySummaryInfo = bindingListSortService.Sort();

            this.BindDgvBluRaySummaryGrid();
        }

        private void SetEac3ToConfiguration()
        {
            _eac3toConfiguration = new EAC3ToConfiguration()
            {
                BatchFilePath = txtBatFilePath.Text,
                EAC3ToPath = _eac3ToPath,
                EAC3ToOutputPath = setDirectoryUserControl.CLIDirectory,
                OutputDirectoryType = setDirectoryUserControl.OutputDirectoryType
            };
        }

        private void dgvBluRayDiscInfo_DragDrop(object sender, DragEventArgs e)
        {
            this.HandleDgvBluRayDiscInfoDragDrop(e);
        }

        private void HandleDgvBluRayDiscInfoDragDrop(DragEventArgs e)
        {
            foreach (string folder in (Array)e.Data.GetData(DataFormats.FileDrop))
            {
                if (this.IsADirectory(folder) && this.NotADuplicate(folder))
                {
                   _bindingListBluRayDiscInfo.Add(new BluRayDiscInfo() {Id = _bindingListBluRayDiscInfo.Count() + 1, IsSelected = false, BluRayPath = folder});
                }
            }
            if (_bindingListBluRayDiscInfo.Count() > 0)
            {
                this.BindDgvBluRayDiscInfoGrid();
            }
        }

        private bool IsADirectory(string folder)
        {
            if (Directory.Exists(folder))
                return true;
            else
                return false;
        }

        private bool NotADuplicate(string folder)
        {
            if (_bindingListBluRayDiscInfo.Where(d => d.BluRayPath == folder).Count() == 0)
                return true;
            else
                return false;
        }

        private void dgvBluRayDiscInfo_DragEnter(object sender, DragEventArgs e)
        {
            this.HandleDgvBluRayDiscInfoDragEnter(e);
        }

        private void HandleDgvBluRayDiscInfoDragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private List<BluRayDiscInfo> GetBluRayDiscInfoList()
        {
            List<BluRayDiscInfo> discs = new List<BluRayDiscInfo>();
            foreach (BluRayDiscInfo disc in _bindingListBluRayDiscInfo)
            {
                discs.Add(disc);
            }
            return discs;
        }

    }
}
