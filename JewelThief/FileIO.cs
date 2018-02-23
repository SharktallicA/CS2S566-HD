using Microsoft.Win32;
using System.IO;

namespace WizardDungeon
{
    /// <summary>
    /// For flagging what type of dialog FileIO is
    /// </summary>
    enum DialogType { Open, Save }

    /// <summary>
    /// Reusable class for handling file input and output (load and save) operations to reduce code duplication
    /// </summary>
    class FileIO
    {
        private DialogType dlgType; //type of dialog FileIO is
        private string dlgTitle; //title displayed on dialog
        private string dlgFitler; //filter used on dialog
        private string dlgDirectory; //initial directory for dialog
        public string FileName = ""; //result of dialog

        /// <summary>
        /// FileIO constructor
        /// </summary>
        /// <param name="type">Dialog's type</param>
        /// <param name="title">Dialog's title</param>
        /// <param name="filter">Dialog's filter</param>
        public FileIO(DialogType type, string title = "File", string filter = "All files|*.*")
        {
            dlgType = type;
            dlgTitle = title;
            dlgFitler = filter;
            dlgDirectory = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).ToString()).ToString()).ToString();
        }

        /// <summary>
        /// Displays FileIO dialog
        /// </summary>
        /// <returns>Returns status of result from dialog</returns>
        public bool ShowDialog()
        {
            bool result = false; //stores result of dialog

            ////////////////////////////////////////////////////////////
            // Check type of dialog and construct and execute the
            // appropriate one

            if (dlgType == DialogType.Open)
            {
                OpenFileDialog dlgOpen = new OpenFileDialog();
                dlgOpen.Title = dlgTitle;
                dlgOpen.Filter = dlgFitler;
                dlgOpen.InitialDirectory = dlgDirectory;
                result = (bool)dlgOpen.ShowDialog(); //call dialog to retrieve result
                if (result) FileName = dlgOpen.FileName; //if successful, get file name
            }
            else if (dlgType == DialogType.Save)
            {
                SaveFileDialog dlgSave = new SaveFileDialog();
                dlgSave.Title = dlgTitle;
                dlgSave.Filter = dlgFitler;
                dlgSave.InitialDirectory = dlgDirectory;
                result = (bool)dlgSave.ShowDialog(); //call dialog to retrieve result
                if (result) FileName = dlgSave.FileName; //if successful, get file name
            }
            return result; //result end result
        }
    }
}