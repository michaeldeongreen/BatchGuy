﻿using BatchGuy.App.X264.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using BatchGuy.App.X264.Interfaces;
using BatchGuy.App.Shared.Models;

namespace BatchGuy.App.X264.Services
{
    public class X264ValidationService : IX264ValidationService
    {
        private X264FileSettings _x264FileSettings;
        private ErrorCollection _errors;
        private List<X264File> _x264Files;

        public X264ValidationService(X264FileSettings x264FileSettings, List<X264File> x264Files)
        {
            _x264FileSettings = x264FileSettings;
            _errors = new ErrorCollection();
            _x264Files = x264Files;
        }
        public ErrorCollection Validate()
        {
            this.IsValid();
            return _errors;
        }

        private bool IsValid()
        {
            if (!this.SettingsAndFilesNotNull())
                return false;
            if (!this.AllAviSynthFilesHaveEncodeName())
                return false;
            if (!this.IsDirectoryValidDirectory())
                return false;
            if (!this.X264EncodeAndLogFileOutputDirectoryPathNotNull())
                return false;
            return true;
        }

        private bool SettingsAndFilesNotNull()
        {
            if (_x264FileSettings == null || _x264Files == null)
            {
                this._errors.Add(new Error() { Description = "x264 Settings or x264 files not found" });
                return false;
            }
            return true;
        }

        private bool AllAviSynthFilesHaveEncodeName()
        {
            if (_x264Files.Where(f => f.EncodeName == null || f.EncodeName == string.Empty).Count() > 0)
            {
                this._errors.Add(new Error() { Description = "All AviSynth files must have a encode name" });
                return false;
            }
            return true;
        }

        private bool IsDirectoryValidDirectory()
        {
            if (!Directory.Exists(_x264FileSettings.AviSynthFilesPath))
            {
                this._errors.Add(new Error() { Description = "The Directory where the AviSynth files are located does not exist" });
                return false;
            }
            return true;
        }

        private bool X264EncodeAndLogFileOutputDirectoryPathNotNull()
        {
            if (string.IsNullOrEmpty(_x264FileSettings.X264EncodeAndLogFileOutputDirectoryPath))
            {
                this._errors.Add(new Error() { Description = "The x264 encode and (.log) file Output directory is required" });
                return false;                
            }
            return true;
        }
    }
}
