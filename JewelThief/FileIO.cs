using Microsoft.Win32;
using System.IO;

namespace WizardDungeon
{
    /// <summary>
    /// Enumeration of flags to indicate available types of dialog operations
    /// </summary>
    enum DialogType { Open, Save }

    /// <summary>
    /// Universal file input and output handler (reduces code duplication)
    /// </summary>
    class FileIO
    {
        /// <summary>
        /// Type of dialog FileIO is initialised to be
        /// </summary>
        private DialogType dlgType;

        /// <summary>
        /// Title to be displayed on dialog
        /// </summary>
        private string dlgTitle;

        /// <summary>
        /// Filter to be used on dialog
        /// </summary>
        private string dlgFitler; 

        /// <summary>
        /// Starting directory dialog will use
        /// </summary>
        private string dlgDirectory;

        /// <summary>
        /// Filename and path of selected file
        /// </summary>
        public string FileName = ""; 

        /// <summary>
        /// FileIO constructor
        /// </summary>
        /// <param name="type">Enumerated type of dialog</param>
        /// <param name="title">Title for dialog</param>
        /// <param name="filter">Filter for dialog</param>
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