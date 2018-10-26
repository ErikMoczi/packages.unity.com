using System.Linq;
using Semver;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageSampleItem
    {
        private PackageSample sample;

        public PackageSampleItem(PackageSample sample)
        {
            this.sample = sample;
            if (sample != null)
            {
                NameLabel.text = sample.displayName;
                SizeLabel.text = sample.Size;
                RefreshImportStatus();
                ImportButton.clickable.clicked += () =>
                {
                    string[] mismatchVersions;
                    if (sample.IsImportedToAssets)
                    {
                        if (EditorUtility.DisplayDialog("Unity Package Manager",
                            "The sample is already imported at\n\n" +
                            sample.importPath.Replace(Application.dataPath, "Assets") + 
                            "\n\nImporting again will override all changes you have made to it. Are you sure you want to continue?", "No", "Yes"))
                            return;
                        IOUtils.RemovePathAndMeta(sample.importPath, true);
                    }
                    else if((mismatchVersions = sample.MismatchedVersions).Length != 0)
                    {
                        var warningMessage = string.Empty;
                        if (mismatchVersions.Length > 1)
                        {
                            warningMessage = "Different versions of the sample are already imported at\n\n";
                            foreach(var v in mismatchVersions)
                                warningMessage += v.Replace(Application.dataPath, "Assets") + "\n";
                            warningMessage += "\nThey will be deleted when you update.";
                        }
                        else
                        {
                            warningMessage = "A different version of the sample is already imported at\n\n" + 
                                              mismatchVersions[0].Replace(Application.dataPath, "Assets") + 
                                              "\n\nIt will be deleted when you update.";
                        }
                        if (EditorUtility.DisplayDialog("Unity Package Manager",
                            warningMessage + " Are you sure you want to continue?", "No", "Yes"))
                            return;
                        foreach(var v in mismatchVersions)
                            IOUtils.RemovePathAndMeta(v, true);
                    }
                    sample.ImportToAssets();
                    RefreshImportStatus();
                };
            }
        }

        private void RefreshImportStatus()
        {
            if (sample.IsImportedToAssets)
            {
                ImportStatus.AddToClassList("imported");
                ImportButton.text = "Import again";
            }
            else if(sample.MismatchedVersions.Length != 0)
            {
                ImportStatus.AddToClassList("imported");
                ImportButton.text = "Update";
            }
            else
            {
                ImportStatus.RemoveFromClassList("imported");
                ImportButton.text = "Import in project";
            }
        }

        private Label _importStatus;
        internal Label ImportStatus { get { return _importStatus ?? (_importStatus = new Label()); } }
        private Label _nameLabel;
        internal Label NameLabel { get { return _nameLabel ?? (_nameLabel = new Label()); } }
        private Label _sizeLabel;
        internal Label SizeLabel { get { return _sizeLabel ?? ( _sizeLabel = new Label()); } }
        private Button _importButton;
        internal Button ImportButton { get { return _importButton ?? (_importButton = new Button()); } }
    }
}
