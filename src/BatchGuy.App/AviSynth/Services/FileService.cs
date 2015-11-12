﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BatchGuy.App.Helpers;
using BatchGuy.App.AviSynth;
using BatchGuy.App.AviSynth.Models;
using BatchGuy.App.AviSynth.Interfaces;

namespace BatchGuy.App.AviSynth.Services
{
    public class FileService : IFileService
    {
        private AVSBatchSettings _avsBatchSettings;
        private List<AVSFile> _avsFiles;
        private AVSTemplateScript _avsTemplateScript;

        public FileService(AVSBatchSettings avsBatchSettings, AVSTemplateScript avsTemplateScript)
        {
            _avsBatchSettings = avsBatchSettings;
            _avsTemplateScript = avsTemplateScript;
            _avsFiles = new List<AVSFile>();
        }

        public List<AVSFile> CreateAVSFileList()
        {
            CreateList();
            CreateAVSScript();
            return _avsFiles;
        }

        private void CreateList()
        {
            for (int i = 1; i <= _avsBatchSettings.NumberOfFiles; i++)
            {
                string fileNameOnly = string.Format("{0}{1}.avs", _avsBatchSettings.NamingConvention, HelperFunctions.PadNumberWithZeros(_avsBatchSettings.NumberOfFiles, i));
                string directoryPath = String.Format("{0}\\{1}", _avsBatchSettings.BatchDirectoryPath, fileNameOnly);
                AVSFile avsFile = new AVSFile() { FileNameOnly =  fileNameOnly, FullPath = directoryPath};
                avsFile.Number = i;
                _avsFiles.Add(avsFile);
            }
        }

        private void CreateAVSScript()
        {
            foreach (AVSFile file in _avsFiles)
            {
                StringBuilder sb = new StringBuilder();
                string paddedNumber = HelperFunctions.PadNumberWithZeros(_avsBatchSettings.NumberOfFiles, file.Number);
                string encodeFileFolder = string.Format("e{0}", paddedNumber);
                string encodeFile = string.Format("encode{0}.mkv", paddedNumber); //hardcoded to mkv
                sb.Append(string.Format("{0}(\"{1}\\{2}\\{3}\")",_avsBatchSettings.VideoFilter, _avsBatchSettings.BatchDirectoryPath, encodeFileFolder, encodeFile));
                sb.Append(string.Format("{0}{1}",Environment.NewLine,_avsTemplateScript.Script));
                file.AVSScript = sb.ToString();
            }
        }
    }
}